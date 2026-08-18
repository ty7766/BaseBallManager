using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 강화 로직 (기획서 2.1)
/// </summary>
/// <remarks>
/// 재료는 둘 중 하나를 고른다 - <b>동일 카드 1장</b>(<see cref="EnhanceWithIdenticalCard"/>) 또는
/// <b>강화 전용 카드 5장</b>(<see cref="EnhanceWithEnhanceCards"/>).
/// 전용 카드는 인벤토리 카드가 아니라 <see cref="CurrencyType"/> 재화로 다룬다.
/// 카드로 만들면 마스터 CSV에 스탯·포지션이 없는 유령 카드가 200장 한도를 잡아먹고,
/// 라인업·분해·필터가 전부 예외 분기를 갖게 된다.
/// </remarks>
public class EnhanceManager : MonoBehaviour
{
    public static EnhanceManager Instance { get; private set; }

    [Header("최대 강화 레벨 설정")]
    [SerializeField]
    private int _maxEnhanceLevel = 10;
    [SerializeField, Tooltip("전용 카드로 1레벨 올리는 데 드는 장수 (기획서 2.1)")]
    private int _enhanceMaterialCount = 5;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //동일 카드 1장을 재료로 강화 (기획서 2.1 - 동일 종류·이름·팀, 년도는 달라도 됨)
    public bool EnhanceWithIdenticalCard(int targetInstanceId, List<int> materialInstanceIds)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(targetInstanceId);
        if (cardInstance == null)
        {
            return false;
        }
        if (!CanEnhance(targetInstanceId))
        {
            return false;
        }

        List<CardInstance> materials = new List<CardInstance>();
        foreach (int instanceId in materialInstanceIds) 
        {
            CardInstance material = InventoryManager.Instance.GetCard(instanceId);
            if(material == null)
            {
                return false;
            }
            materials.Add(material);
        }
        if (!ValidateMaterials(cardInstance, materials))
        {
            return false;
        }
        
        //카드 소멸 후 강화
        foreach(CardInstance material in materials)
        {
            InventoryManager.Instance.RemoveCard(material.InstanceId);
        }
        cardInstance.ApplyEnhance();
        return true;
    }

    /// <summary>
    /// 강화 전용 카드 5장을 재료로 강화 (기획서 2.1)
    /// </summary>
    /// <remarks>
    /// 대상 카드의 종류·등급에 맞는 전용 카드만 쓸 수 있다(3성 카드에 4성 전용 카드 불가).
    /// 전용 카드는 재화이므로 인벤토리를 건드리지 않는다.
    /// </remarks>
    public bool EnhanceWithEnhanceCards(int targetInstanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(targetInstanceId);

        if (cardInstance == null)
            return false;

        if (!CanEnhance(targetInstanceId))
            return false;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[EnhanceManager] : CurrencyManager가 씬에 없습니다");
            return false;
        }

        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);

        if (masterData == null)
        {
            Debug.LogError($"[EnhanceManager] : 마스터 데이터를 찾지 못했습니다 (cardId {cardInstance.CardId})");
            return false;
        }

        CurrencyType enhanceCardType = GetRequiredEnhanceCardType(masterData);

        //부족 사유는 CurrencyManager가 로그로 남김
        if (!CurrencyManager.Instance.Spend(enhanceCardType, _enhanceMaterialCount))
            return false;

        cardInstance.ApplyEnhance();

        return true;
    }

    /// <summary>
    /// 이 카드를 강화하는 데 필요한 전용 카드 종류와 장수. 조회 실패 시 <c>CurrencyType.None</c>
    /// </summary>
    /// <remarks>UI가 "5성 전용 카드 5장 필요 (보유 3장)" 같은 표기를 만들 때 쓴다</remarks>
    public (CurrencyType enhanceCardType, int count) GetRequiredEnhanceCard(int targetInstanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(targetInstanceId);

        if (cardInstance == null)
            return (CurrencyType.None, 0);

        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);

        if (masterData == null)
            return (CurrencyType.None, 0);

        return (GetRequiredEnhanceCardType(masterData), _enhanceMaterialCount);
    }

    //카드 종류·등급 -> 대응하는 강화 전용 카드 재화 (기획서 2.1 전용 카드 표)
    private static CurrencyType GetRequiredEnhanceCardType(CardMasterData masterData)
    {
        //시그니쳐·골든글러브는 5성 고정이라 등급을 보지 않는다 (기획서 1.2)
        if (masterData.CardType == CardType.Signature)
            return CurrencyType.EnhanceCardSignature;

        if (masterData.CardType == CardType.GoldenGlove)
            return CurrencyType.EnhanceCardGoldenGlove;

        return masterData.CardGrade switch
        {
            CardGrade.Star3 => CurrencyType.EnhanceCardStar3,
            CardGrade.Star4 => CurrencyType.EnhanceCardStar4,
            CardGrade.Star5 => CurrencyType.EnhanceCardStar5,
            _ => throw new ArgumentException($"알 수 없는 카드 등급: {masterData.CardGrade}")
        };
    }

    //해당 카드가 강화 가능한지 검사
    public bool CanEnhance(int targetInstanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(targetInstanceId);
        
        //카드를 못찾는 경우 제외
        if (cardInstance == null)
        {
            return false;
        }

        //최대 강화가 되어있는 카드는 제외
        if (cardInstance.EnhanceLevel >= _maxEnhanceLevel)
        {
            return false;
        }

        return true;
    }

    //대상 카드의 동일 카드(동일 종류*이름*팀, 다른 년도 가능) 목록 반환
    //재료로 쓸 수 있는 카드 표시용
    public List<CardInstance> GetIdenticalCards(int targetInstanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(targetInstanceId);
        if (cardInstance == null) return new List<CardInstance>();
        CardMasterData cardMasterData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);
        if (cardMasterData == null) return new List<CardInstance>();

        IReadOnlyList<CardInstance> allCards = InventoryManager.Instance.GetAllCards();

        List<CardInstance> result = new List<CardInstance>();
        foreach(CardInstance card in allCards)
        {
            CardMasterData data = CardDataManager.Instance.GetCardMasterData(card.CardId);
            if(data == null)
                continue;

            if (card.InstanceId == targetInstanceId)
                continue;
            if (card.IsLocked)
                continue;
            if (data.CardType != cardMasterData.CardType)
                continue;
            if (data.Name != cardMasterData.Name)
                continue;
            if (data.TeamName != cardMasterData.TeamName)
                continue;

            result.Add(card);
        }

        return result;

    }

    //재료 카드 목록이 강화 조건을 만족하는지 검사
    private bool ValidateMaterials(CardInstance target, List<CardInstance> materials)
    {
        //강화는 한 번에 한 카드만 사용
        if (materials == null || materials.Count != 1)
        {
            return false;
        }

        CardMasterData data = CardDataManager.Instance.GetCardMasterData(target.CardId);
        if (data == null)
        {
            return false;
        }

        foreach (CardInstance material in materials)
        {
            //자기 자신 제외
            if (target.InstanceId == material.InstanceId)
            {
                return false;
            }
            //잠금 제외
            if (material.IsLocked)
            {
                return false;
            }
            //데이터가 존재하지 않는 카드 제외
            if (material.CardId <= 0)
            {
                return false;
            }

            CardMasterData materialData = CardDataManager.Instance.GetCardMasterData(material.CardId);
            if (materialData == null)
            {
                return false;
            }

            //카드 타입, 이름, 팀이름 비교
            if (data.CardType != materialData.CardType)
            {
                return false;
            }
            if (data.Name != materialData.Name)
            {
                return false;
            }
            if (data.TeamName != materialData.TeamName)
            {
                return false;
            }
        }
        return true;
    }
}
