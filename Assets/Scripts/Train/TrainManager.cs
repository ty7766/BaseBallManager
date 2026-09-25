using UnityEngine;

/// <summary>
/// 훈련 전반 설정
/// </summary>
public class TrainManager : SingletonBehaviour<TrainManager>
{
    /// <summary>
    /// 훈련돌파 전 최대 훈련 레벨. 돌파 해금 기준이기도 하다
    /// </summary>
    public int MaxTrainLevel => _maxTrainLevel;

    /// <summary>
    /// 훈련돌파 후 최대 훈련 레벨
    /// </summary>
    public int MaxTrainLevelAfterBreakthrough => _maxTrainLevelAfterBreakthrough;

    [Header("훈련 레벨 설정")]
    [SerializeField, Tooltip("훈련돌파 전 최대 훈련 레벨")]
    private int _maxTrainLevel = 30;
    [SerializeField, Tooltip("훈련돌파 후 최대 훈련 레벨")]
    private int _maxTrainLevelAfterBreakthrough = 50;

    [Header("훈련 비용 설정")]
    [SerializeField, Tooltip("기본 포인트 비용 (실제 비용 = 현재 훈련 레벨 x 이 값)")]
    private int _basePointCost = 100;
    [SerializeField, Tooltip("기본 훈련 카드 비용 (실제 비용 = 현재 훈련 레벨 x 이 값)")]
    private int _baseTrainCardCost = 1;
    [SerializeField, Tooltip("1레벨 상승당 4개 스탯에 랜덤 분배할 포인트 수")]
    private int _statPointPerTrain = 2;

    /// <summary>
    /// 해당 카드를 더 훈련할 수 있는지 검사한다
    /// </summary>
    public bool CanTrain(int instanceId)
    {
        return CanTrain(InventoryManager.Instance.GetCard(instanceId));
    }

    /// <summary>
    /// 현재 훈련 레벨 기준 포인트 비용과 훈련 카드 비용을 반환한다
    /// </summary>
    public (int pointCost, int trainCardCost) GetTrainCost(int trainLevel)
    {
        return (trainLevel * _basePointCost, trainLevel * _baseTrainCardCost);
    }

    /// <summary>
    /// 재화를 소모해 훈련 레벨을 1 올리고 4개 스탯에 랜덤 분배한다
    /// </summary>
    public bool Train(int instanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);

        if (cardInstance == null)
            return false;

        if (!CanTrain(cardInstance))
        {
            Debug.LogWarning($"[TrainManager] : 더 훈련할 수 없는 카드입니다 (instanceId {instanceId})");
            return false;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[TrainManager] : CurrencyManager가 씬에 없습니다");
            return false;
        }

        (int pointCost, int trainCardCost) = GetTrainCost(cardInstance.TrainLevel);

        CurrencyCost[] costs =
        {
            new CurrencyCost(CurrencyType.Point, pointCost),
            new CurrencyCost(CurrencyType.TrainCard, trainCardCost)
        };

        if (!CurrencyManager.Instance.SpendAll(costs))
            return false;

        if (!cardInstance.ApplyTrain(RollTrainDelta()))
        {
            Debug.LogError($"[TrainManager] : 훈련 분배 적용에 실패해 재화만 소모되었습니다 (instanceId {instanceId})");
            return false;
        }

        return true;
    }

    //조회를 이미 끝낸 호출부용
    private bool CanTrain(CardInstance cardInstance)
    {
        return cardInstance != null && cardInstance.TrainLevel < GetMaxTrainLevel(cardInstance);
    }

    //돌파 여부에 따른 훈련 레벨 상한
    private int GetMaxTrainLevel(CardInstance cardInstance)
    {
        return cardInstance.BreakthroughUsed ? _maxTrainLevelAfterBreakthrough : _maxTrainLevel;
    }

    //1레벨 상승분을 4개 스탯에 랜덤 분배
    private int[] RollTrainDelta()
    {
        int[] delta = new int[CardInstance.TrainStatCount];

        for (int i = 0; i < _statPointPerTrain; i++)
            delta[Random.Range(0, CardInstance.TrainStatCount)]++;

        return delta;
    }
}
