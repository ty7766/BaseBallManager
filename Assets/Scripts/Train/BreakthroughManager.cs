using UnityEngine;

public class BreakthroughManager : MonoBehaviour
{
    public static BreakthroughManager Instance { get; private set; }

    [SerializeField]
    private int _breakthroughCardCost_GoldenGlove = 50;
    [SerializeField]
    private int _breakthroughCardCost_Signature = 20;
    [SerializeField]
    private int _breakthroughCardCost_Normal5 = 10;
    [SerializeField]
    private int _breakthroughCardCost_Normal4 = 5;
    [SerializeField]
    private int _breakthroughCardCost_Normal3 = 3;

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

    public bool CanBreakthrough(int instanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);
        if (cardInstance == null)
        {
            return false;
        }
        if (cardInstance.BreakthroughUsed)
        {
            return false;
        }
        //훈련이 만렙이어야 돌파 가능
        if (cardInstance.TrainLevel < TrainManager.Instance.GetMaxTrainLevel())
        {
            return false;
        }
        return true;
    }

    public int GetBreakthroughCost(int instanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);
        if (cardInstance == null) 
        {
            return -1;
        }

        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);
        if (masterData == null)
        {
            return -1;
        }

        return masterData.CardType switch
        {
            CardType.GoldenGlove => _breakthroughCardCost_GoldenGlove,
            CardType.Signature => _breakthroughCardCost_Signature,
            CardType.Normal => masterData.CardGrade switch
            { CardGrade.Star5 => _breakthroughCardCost_Normal5,
            CardGrade.Star4 => _breakthroughCardCost_Normal4,
            CardGrade.Star3 => _breakthroughCardCost_Normal3, 
                _=> -1},
                _=> -1
        };
    }

    public bool Breakthrough(int instanceId)
    {
        if (!CanBreakthrough(instanceId))
        {
            return false;
        }

        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);
        cardInstance.ApplyBreakthrough();
        return true;
    }
}
