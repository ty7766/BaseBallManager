using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 강화 로직 - 동일 카드 1장 또는 강화 전용 카드 5장(1묶음)마다 +1.
/// 여러 장을 한 번에 넣어 여러 단계를 올릴 수 있다
/// </summary>
public class EnhanceManager : SingletonBehaviour<EnhanceManager>
{
    /// <summary>
    /// 최대 강화 레벨
    /// </summary>
    public int MaxEnhanceLevel => _maxEnhanceLevel;

    [Header("최대 강화 레벨 설정")]
    [SerializeField]
    private int _maxEnhanceLevel = 10;
    [SerializeField, Tooltip("전용 카드로 1레벨 올리는 데 드는 장 수")]
    private int _enhanceMaterialCount = 5;

    /// <summary>
    /// 동일 카드 여러 장과 전용 카드 묶음을 한 번에 써서 강화한다. 동일 카드 1장 · 전용 카드 1묶음마다 +1.
    /// 재료를 모두 검사한 뒤에 소모하므로 중간에 실패해 재료만 사라지는 일은 없다
    /// </summary>
    public bool Enhance(int targetInstanceId, IReadOnlyList<int> materialInstanceIds, int enhanceCardSets)
    {
        CardInstance target = InventoryManager.Instance.GetCard(targetInstanceId);

        if (target == null)
            return false;

        int materialCount = materialInstanceIds == null ? 0 : materialInstanceIds.Count;
        int levels = materialCount + enhanceCardSets;

        if (enhanceCardSets < 0 || levels <= 0)
        {
            Debug.LogWarning($"[EnhanceManager] : 재료가 없습니다 (instanceId {targetInstanceId})");
            return false;
        }

        if (target.EnhanceLevel + levels > _maxEnhanceLevel)
        {
            Debug.LogWarning($"[EnhanceManager] : 최대 강화 레벨을 넘습니다 (instanceId {targetInstanceId} / +{target.EnhanceLevel} + {levels})");
            return false;
        }

        if (!ValidateMaterials(target, materialInstanceIds))
            return false;

        int enhanceCardCost = enhanceCardSets * _enhanceMaterialCount;
        CurrencyType enhanceCardType = CurrencyType.None;

        if (enhanceCardSets > 0 && !TryGetAffordableEnhanceCard(target, enhanceCardCost, out enhanceCardType))
            return false;

        if (enhanceCardSets > 0 && !CurrencyManager.Instance.Spend(enhanceCardType, enhanceCardCost))
            return false;

        int gainedLevels = enhanceCardSets;

        for (int i = 0; i < materialCount; i++)
        {
            if (InventoryManager.Instance.RemoveCard(materialInstanceIds[i]))
                gainedLevels++;
            else
                Debug.LogError($"[EnhanceManager] : 검사를 통과한 재료를 지우지 못했습니다 (instanceId {materialInstanceIds[i]})");
        }

        for (int i = 0; i < gainedLevels; i++)
            target.ApplyEnhance();

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
    /// 강화에 필요한 전용 카드 종류와 1레벨당 장수. 조회 실패 시 <c>CurrencyType.None</c>
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

    //대상에 맞는 전용 카드를 찾아 cost 장 이상 있는지 검사
    private bool TryGetAffordableEnhanceCard(CardInstance target, int cost, out CurrencyType enhanceCardType)
    {
        enhanceCardType = CurrencyType.None;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[EnhanceManager] : CurrencyManager가 씬에 없습니다");
            return false;
        }

        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(target.CardId);

        if (masterData == null)
        {
            Debug.LogError($"[EnhanceManager] : 마스터 데이터를 찾지 못했습니다 (cardId {target.CardId})");
            return false;
        }

        enhanceCardType = GetRequiredEnhanceCardType(masterData);

        if (enhanceCardType == CurrencyType.None)
        {
            Debug.LogError($"[EnhanceManager] : 알 수 없는 카드 등급입니다 (cardId {target.CardId} / {masterData.CardGrade})");
            return false;
        }

        if (!CurrencyManager.Instance.CanAfford(enhanceCardType, cost))
        {
            Debug.LogWarning($"[EnhanceManager] : 전용 카드가 부족합니다 ({enhanceCardType} {cost}장 필요)");
            return false;
        }

        return true;
    }

    //재료 목록 전체를 소모 전에 검사 - 대상 자신 · 중복 지정 · 없는 카드 · 조건 불일치
    private bool ValidateMaterials(CardInstance target, IReadOnlyList<int> materialInstanceIds)
    {
        if (materialInstanceIds == null || materialInstanceIds.Count == 0)
            return true;

        HashSet<int> seen = new HashSet<int>();

        for (int i = 0; i < materialInstanceIds.Count; i++)
        {
            int materialInstanceId = materialInstanceIds[i];

            if (materialInstanceId == target.InstanceId)
            {
                Debug.LogWarning($"[EnhanceManager] : 대상 카드를 재료로 지정할 수 없습니다 (instanceId {materialInstanceId})");
                return false;
            }

            if (!seen.Add(materialInstanceId))
            {
                Debug.LogWarning($"[EnhanceManager] : 같은 재료를 두 번 지정했습니다 (instanceId {materialInstanceId})");
                return false;
            }

            CardInstance material = InventoryManager.Instance.GetCard(materialInstanceId);

            if (material == null || !ValidateMaterial(target, material))
                return false;
        }

        return true;
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
