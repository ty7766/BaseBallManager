using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 강화 로직 - 동일 카드 1장 또는 강화 전용 카드 5장을 재료로 쓴다
/// </summary>
public class EnhanceManager : SingletonBehaviour<EnhanceManager>
{
    [Header("최대 강화 레벨 설정")]
    [SerializeField]
    private int _maxEnhanceLevel = 10;
    [SerializeField, Tooltip("전용 카드로 1레벨 올리는 데 드는 장 수")]
    private int _enhanceMaterialCount = 5;

    /// <summary>
    /// 동일 카드 1장을 재료로 강화한다 (동일 종류·이름·팀, 년도는 달라도 됨)
    /// </summary>
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
    /// 강화 전용 카드를 재료로 강화한다. 대상의 종류·등급에 맞는 전용 카드만 쓸 수 있다
    /// </summary>
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

        if (!CurrencyManager.Instance.Spend(enhanceCardType, _enhanceMaterialCount))
            return false;

        cardInstance.ApplyEnhance();

        return true;
    }

    /// <summary>
    /// 재료로 쓸 수 있는 동일 카드 목록을 반환한다 (잠금·라인업 편성 카드는 제외)
    /// </summary>
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

            CardMasterData data = CardDataManager.Instance.GetCardMasterData(card.CardId);
            if (data == null || !IsIdenticalCard(cardMasterData, data))
                continue;

            result.Add(card);
        }
        return result;
    }

    /// <summary>
    /// 강화에 필요한 전용 카드 종류와 장수. 조회 실패 시 <c>CurrencyType.None</c>
    /// </summary>
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

    /// <summary>
    /// 해당 카드를 더 강화할 수 있는지 검사한다
    /// </summary>
    public bool CanEnhance(int targetInstanceId)
    {
        return CanEnhance(InventoryManager.Instance.GetCard(targetInstanceId));
    }

    //카드 종류·등급 -> 대응하는 강화 전용 카드 재화
    private static CurrencyType GetRequiredEnhanceCardType(CardMasterData masterData)
    {
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
