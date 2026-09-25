using UnityEngine;

/// <summary>
/// 훈련돌파 - 훈련 만렙 카드의 훈련 레벨 상한을 여는 1회성 처리
/// </summary>
public class BreakthroughManager : SingletonBehaviour<BreakthroughManager>
{
    [Header("카드 종류·등급별 훈련돌파 카드 소모량")]
    [SerializeField, Tooltip("골든글러브")]
    private int _goldenGloveCost = 50;
    [SerializeField, Tooltip("시그니쳐")]
    private int _signatureCost = 20;
    [SerializeField, Tooltip("노말 5성")]
    private int _normalStar5Cost = 10;
    [SerializeField, Tooltip("노말 4성")]
    private int _normalStar4Cost = 5;
    [SerializeField, Tooltip("노말 3성")]
    private int _normalStar3Cost = 3;

    /// <summary>
    /// 해당 카드를 돌파할 수 있는지 검사한다 (미돌파 + 훈련 만렙)
    /// </summary>
    public bool CanBreakthrough(int instanceId)
    {
        return CanBreakthrough(InventoryManager.Instance.GetCard(instanceId));
    }

    /// <summary>
    /// 돌파에 필요한 훈련돌파 카드 수. 조회 실패 시 -1
    /// </summary>
    public int GetBreakthroughCost(int instanceId)
    {
        return GetBreakthroughCost(InventoryManager.Instance.GetCard(instanceId));
    }

    /// <summary>
    /// 훈련돌파 카드를 소모해 훈련 레벨 상한을 연다
    /// </summary>
    public bool Breakthrough(int instanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);

        if (cardInstance == null)
            return false;

        if (!CanBreakthrough(cardInstance))
        {
            Debug.LogWarning($"[BreakthroughManager] : 돌파할 수 없는 카드입니다 (instanceId {instanceId})");
            return false;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[BreakthroughManager] : CurrencyManager가 씬에 없습니다");
            return false;
        }

        int cost = GetBreakthroughCost(cardInstance);

        if (cost <= 0)
        {
            Debug.LogError($"[BreakthroughManager] : 돌파 비용을 구하지 못했습니다 (instanceId {instanceId})");
            return false;
        }

        if (!CurrencyManager.Instance.Spend(CurrencyType.BreakthroughCard, cost))
            return false;

        cardInstance.ApplyBreakthrough();

        return true;
    }

    //조회를 이미 끝낸 호출부용
    private bool CanBreakthrough(CardInstance cardInstance)
    {
        if (cardInstance == null)
            return false;

        if (cardInstance.BreakthroughUsed)
            return false;

        if (TrainManager.Instance == null)
        {
            Debug.LogError("[BreakthroughManager] : TrainManager가 씬에 없습니다");
            return false;
        }

        return cardInstance.TrainLevel >= TrainManager.Instance.MaxTrainLevel;
    }

    //카드 종류별 돌파 비용. 조회 실패 시 -1
    private int GetBreakthroughCost(CardInstance cardInstance)
    {
        if (cardInstance == null)
            return -1;

        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);

        if (masterData == null)
        {
            Debug.LogError($"[BreakthroughManager] : 마스터 데이터를 찾지 못했습니다 (cardId {cardInstance.CardId})");
            return -1;
        }

        return masterData.CardType switch
        {
            CardType.GoldenGlove => _goldenGloveCost,
            CardType.Signature => _signatureCost,
            CardType.Normal => GetNormalBreakthroughCost(masterData.CardGrade),
            _ => -1
        };
    }

    //노말 카드 등급별 돌파 비용. 조회 실패 시 -1
    private int GetNormalBreakthroughCost(CardGrade cardGrade)
    {
        return cardGrade switch
        {
            CardGrade.Star5 => _normalStar5Cost,
            CardGrade.Star4 => _normalStar4Cost,
            CardGrade.Star3 => _normalStar3Cost,
            _ => -1
        };
    }
}
