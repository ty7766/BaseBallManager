using UnityEngine;

/// <summary>
/// 훈련 전반 설정
/// </summary>
public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance { get; private set; }

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

    //훈련을 할 수 있는 상태인지를 반환
    public bool CanTrain(int instanceId)
    {
        CardInstance card = InventoryManager.Instance.GetCard(instanceId);
        if (card == null)
        {
            return false;
        }

        int maxLevel;
        
        if (!card.BreakthroughUsed)
        {
            maxLevel = _maxTrainLevel;
        }
        else
        {
            maxLevel = _maxTrainLevelAfterBreakthrough;
        }

        //훈련이 만렙이 아니면 true 반환
        return card.TrainLevel < maxLevel;
        
    }

    //현재 훈련 레벨 기준 포인트 비용과 훈련 카드 비용 반환
    public (int pointCost, int trainCardCost) GetTrainCost(int trainLevel)
    {
        int pointCost = trainLevel * _basePointCost;
        int trainCardCost = trainLevel * _baseTrainCardCost;

        return (pointCost, trainCardCost);
    }

    public bool Train(int instanceId)
    {
        if (!CanTrain(instanceId))
        {
            return false;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[TrainManager] : CurrencyManager가 씬에 없습니다");
            return false;
        }

        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);

        (int pointCost, int trainCardCost) = GetTrainCost(cardInstance.TrainLevel);

        //포인트와 훈련 카드를 한 번에 소모한다. 하나라도 모자라면 아무것도 차감되지 않음
        CurrencyCost[] costs =
        {
            new CurrencyCost(CurrencyType.Point, pointCost),
            new CurrencyCost(CurrencyType.TrainCard, trainCardCost)
        };

        //부족 사유는 CurrencyManager가 로그로 남김
        if (!CurrencyManager.Instance.SpendAll(costs))
            return false;

        int[] delta = new int[4];

        for(int i = 0; i < 2; i++)
        {
            delta[Random.Range(0, 4)]++;
        }

        cardInstance.ApplyTrain(delta);

        return true;
    }

    public int GetMaxTrainLevel()
    {
        return _maxTrainLevel;
    }
}
