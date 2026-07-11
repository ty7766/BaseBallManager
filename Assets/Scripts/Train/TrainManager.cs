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
    [SerializeField, Tooltip("기본 포인트 비용")]
    private int _basePointCost;
    [SerializeField, Tooltip("기본 훈련 카드 비용")]
    private int _baseTrainCardCost;
    
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

        int[] delta = new int[4];

        for(int i = 0; i < 2; i++)
        {
            delta[Random.Range(0, 4)]++;
        }

        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);
        cardInstance.ApplyTrain(delta);

        return true;
    }
}
