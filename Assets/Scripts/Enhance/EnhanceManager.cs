using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 강화 로직
/// </summary>
/// <remarks>
/// 재료는 둘 중 하나를 고른다 - <b>동일 카드 1장</b>(<see cref="EnhanceWithIdenticalCard"/>) 또는
/// <b>강화 전용 카드 5장</b>(<see cref="EnhanceWithEnhanceCards"/>).
/// 전용 카드는 인벤토리 카드가 아니라 <see cref="CurrencyType"/> 재화로 다룬다.
/// 카드로 만들면 마스터 CSV에 스탯·포지션이 없는 유령 카드가 200장 한도를 잡아먹고,
/// 라인업·분해·필터가 전부 예외 분기를 갖게 된다.
/// </remarks>
public class EnhanceManager : SingletonBehaviour<EnhanceManager>
{
    [Header("최대 강화 레벨 설정")]
    [SerializeField]
    private int _maxEnhanceLevel = 10;
    [SerializeField, Tooltip("전용 카드로 1레벨 올리는 데 드는 장 수")]
    private int _enhanceMaterialCount = 5;

    //동일 카드 1장을 재료로 강화 (동일 종류·이름·팀, 년도는 달라도 됨)
    public bool EnhanceWithIdenticalCard(int targetInstanceId, int materialInstanceId)
    {
        CardInstance target = InventoryManager.Instance.GetCard(targetInstanceId);

        if (target == null)
            return false;

        if (!CanEnhance(target))
        {
            Debug.LogWarning($"[EnhanceManager] : 더 강화할 수 없는 카드입니다 (instanceId {targetInstanceId})");
            return false;
        }

        if (targetInstanceId == materialInstanceId)
        {
            Debug.LogWarning($"[EnhanceManager] : 대상 카드를 재료로 지정할 수 없습니다 (instanceId {targetInstanceId})");
            return false;
        }

        CardInstance material = InventoryManager.Instance.GetCard(materialInstanceId);

        if (material == null)
            return false;

        if (!ValidateMaterial(target, material))
            return false;

        if (!InventoryManager.Instance.RemoveCard(materialInstanceId))
            return false;

        target.ApplyEnhance();
        return true;
    }

    /// <summary>
    /// 강화 전용 카드 5장을 재료로 강화
    /// </summary>
    /// <remarks>
    /// 대상 카드의 종류·등급에 맞는 전용 카드만 쓸 수 있다
    /// 전용 카드는 재화이므로 인벤토리를 건드리지 않는다
    /// </remarks>
    public bool EnhanceWithEnhanceCards(int targetInstanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(targetInstanceId);

        if (cardInstance == null)
            return false;

        if (!CanEnhance(cardInstance))
        {
            Debug.LogWarning($"[EnhanceManager] : 더 강화할 수 없는 카드입니다 (instanceId {targetInstanceId})");
            return false;
        }

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

        if (enhanceCardType == CurrencyType.None)
        {
            Debug.LogError($"[EnhanceManager] : 알 수 없는 카드 등급입니다 (cardId {cardInstance.CardId} / {masterData.CardGrade})");
            return false;
        }

        //부족 사유는 CurrencyManager가 로그로 남김
        if (!CurrencyManager.Instance.Spend(enhanceCardType, _enhanceMaterialCount))
            return false;

        cardInstance.ApplyEnhance();

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

        Dictionary<int, CardInstance>.ValueCollection allCards = InventoryManager.Instance.GetAllCards();

        List<CardInstance> result = new List<CardInstance>();
        foreach (CardInstance card in allCards)
        {
            if (card.InstanceId == targetInstanceId)
                continue;
            if (card.IsLocked)
                continue;
            if (LineUpManager.Instance.IsCardAssigned(card.InstanceId))
                continue;

            //비용이 큰 마스터 조회를 마지막에 둔다
            CardMasterData data = CardDataManager.Instance.GetCardMasterData(card.CardId);
            if (data == null || !IsIdenticalCard(cardMasterData, data))
                continue;

            result.Add(card);
        }
        return result;
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

    //해당 카드가 강화 가능한지 검사 (UI버튼 활성화용)
    public bool CanEnhance(int targetInstanceId)
    {
        return CanEnhance(InventoryManager.Instance.GetCard(targetInstanceId));
    }

    //카드 종류·등급 -> 대응하는 강화 전용 카드 재화
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
            _ => CurrencyType.None
        };
    }

    //조회를 이미 끝낸 호출부용
    private bool CanEnhance(CardInstance cardInstance)
    {
        return cardInstance != null && cardInstance.EnhanceLevel < _maxEnhanceLevel;
    }

    //재료 카드가 강화 조건을 만족하는지 검사
    private bool ValidateMaterial(CardInstance target, CardInstance material)
    {
        //실수 방지용 잠금
        if (material.IsLocked)
        {
            Debug.LogWarning($"[EnhanceManager] : 잠금된 카드는 재료로 쓸 수 없습니다 (instanceId {material.InstanceId})");
            return false;
        }

        if (LineUpManager.Instance.IsCardAssigned(material.InstanceId))
        {
            Debug.LogWarning($"[EnhanceManager] : 라인업에 편성된 카드는 재료로 쓸 수 없습니다 (instanceId {material.InstanceId})");
            return false;
        }

        CardMasterData targetData = CardDataManager.Instance.GetCardMasterData(target.CardId);
        CardMasterData materialData = CardDataManager.Instance.GetCardMasterData(material.CardId);

        if (targetData == null || materialData == null)
        {
            Debug.LogError($"[EnhanceManager] : 마스터 데이터를 찾지 못했습니다 (cardId {target.CardId} / {material.CardId})");
            return false;
        }
        
        if (!IsIdenticalCard(targetData, materialData))
        {
            Debug.LogWarning($"[EnhanceManager] : 동일 카드가 아닙니다 (instanceId {material.InstanceId})");
            return false;
        }

        return true;
    }

    //동일 카드 판정 - 종류·이름·팀이 같으면 년도가 달라도 동일 카드
    private static bool IsIdenticalCard(CardMasterData a, CardMasterData b)
    {
        return a.CardType == b.CardType && a.Name == b.Name && a.TeamName == b.TeamName;
    }
}
