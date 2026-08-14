using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 강화 로직
/// 1. 재료 검증, 재료 소모, 강화 적용
/// </summary>
public class EnhanceManager : MonoBehaviour
{
    public static EnhanceManager Instance { get; private set; }

    [Header("최대 강화 레벨 설정")]
    [SerializeField]
    private int _maxEnhanceLevel = 10;
    [SerializeField]
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

    //카드 강화
    public bool Enhance(int targetInstanceId, List<int> materialInstanceIds)
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
