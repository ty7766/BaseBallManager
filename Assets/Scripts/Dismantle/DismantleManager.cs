using UnityEngine;

/// <summary>
/// 카드 분해 (기획서 9.2)
/// </summary>
/// <remarks>
/// 포인트·훈련 카드의 유일한 획득 경로이자, 보유 한도(200장)가 찼을 때 인벤토리를 비우는 수단이다.
/// </remarks>
public class DismantleManager : MonoBehaviour
{
    public static DismantleManager Instance { get; private set; }

    [Header("기본 포인트 보상 (카드 등급별)")]
    [SerializeField]
    private int _basePointStar3 = 100;
    [SerializeField]
    private int _basePointStar4 = 300;
    [SerializeField]
    private int _basePointStar5 = 800;

    [Header("카드 종류 배수 (기획서 9.2 - 시그·골글은 고보상)")]
    [SerializeField]
    private float _signatureMultiplier = 3f;
    [SerializeField]
    private float _goldenGloveMultiplier = 5f;

    [Header("투자분 회수 (기획서 9.2 - 강화·훈련 레벨이 높을수록 증가)")]
    [SerializeField, Tooltip("강화 1레벨당 추가 포인트")]
    private int _pointPerEnhanceLevel = 100;
    [SerializeField, Tooltip("훈련 1레벨당 추가 포인트")]
    private int _pointPerTrainLevel = 20;

    [Header("훈련 카드 확률 드랍")]
    [SerializeField, Range(0f, 1f)]
    private float _trainCardDropChance = 0.3f;
    [SerializeField]
    private int _trainCardDropAmount = 1;

    [Header("골든글러브 포인트 (골글 카드 분해 시)")]
    [SerializeField]
    private int _goldenGlovePointReward = 50;

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

    //해당 카드를 분해할 수 있는지 (기획서 9.2 - 잠금·라인업 편성 카드는 불가)
    public bool CanDismantle(int instanceId)
    {
        CardInstance card = InventoryManager.Instance.GetCard(instanceId);

        if (card == null)
        {
            Debug.LogWarning($"[DismantleManager] : 분해하려는 카드가 없습니다 (instanceId {instanceId})");
            return false;
        }

        //실수 방지용 잠금 (기획서 9.2)
        if (card.IsLocked)
        {
            Debug.LogWarning("[DismantleManager] : 잠금된 카드는 분해할 수 없습니다");
            return false;
        }

        //라인업에 편성된 카드는 빼야 분해 가능 (기획서 9.2)
        if (LineUpManager.Instance.IsCardAssigned(instanceId))
        {
            Debug.LogWarning("[DismantleManager] : 라인업에 편성된 카드는 분해할 수 없습니다");
            return false;
        }

        return true;
    }

    //분해했을 때 받을 확정 보상 (훈련 카드는 확률이라 제외). 실패 시 null
    public DismantleResult GetPreview(int instanceId)
    {
        CardInstance card = InventoryManager.Instance.GetCard(instanceId);

        if (card == null)
            return null;

        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(card.CardId);

        if (masterData == null)
            return null;

        return new DismantleResult(CalculatePoint(masterData, card), 0, GetGoldenGlovePoint(masterData));
    }

    //카드 분해. 성공하면 보상을 지급하고 카드를 소멸시킨다. 실패 시 null
    public DismantleResult Dismantle(int instanceId)
    {
        if (!CanDismantle(instanceId))
            return null;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[DismantleManager] : CurrencyManager가 씬에 없습니다");
            return null;
        }

        CardInstance card = InventoryManager.Instance.GetCard(instanceId);
        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(card.CardId);

        if (masterData == null)
        {
            Debug.LogError($"[DismantleManager] : 마스터 데이터를 찾지 못했습니다 (cardId {card.CardId})");
            return null;
        }

        int point = CalculatePoint(masterData, card);
        int goldenGlovePoint = GetGoldenGlovePoint(masterData);
        int trainCard = Random.value < _trainCardDropChance ? _trainCardDropAmount : 0;

        //보상보다 카드 소멸을 먼저 처리한다. 반대로 하면 제거가 실패했을 때 보상만 남아 카드가 복제된다
        if (!InventoryManager.Instance.RemoveCard(instanceId))
            return null;

        CurrencyManager.Instance.Add(CurrencyType.Point, point);

        //0 지급은 Add가 경고를 내므로 실제 획득이 있을 때만 호출한다
        if (trainCard > 0)
            CurrencyManager.Instance.Add(CurrencyType.TrainCard, trainCard);

        if (goldenGlovePoint > 0)
            CurrencyManager.Instance.Add(CurrencyType.GoldenGlovePoint, goldenGlovePoint);

        return new DismantleResult(point, trainCard, goldenGlovePoint);
    }

    //등급 기본값 x 종류 배수 + 강화·훈련 투자분
    private int CalculatePoint(CardMasterData masterData, CardInstance card)
    {
        int basePoint = masterData.CardGrade switch
        {
            CardGrade.Star3 => _basePointStar3,
            CardGrade.Star4 => _basePointStar4,
            CardGrade.Star5 => _basePointStar5,
            _ => _basePointStar3
        };

        float typeMultiplier = masterData.CardType switch
        {
            CardType.Signature => _signatureMultiplier,
            CardType.GoldenGlove => _goldenGloveMultiplier,
            _ => 1f
        };

        //성장분은 배수를 타지 않는다. 배수까지 곱하면 5성 골글을 풀강해 분해하는 쪽이 압도적으로 이득이 됨
        int growthPoint = card.EnhanceLevel * _pointPerEnhanceLevel + (card.TrainLevel - 1) * _pointPerTrainLevel;

        return Mathf.RoundToInt(basePoint * typeMultiplier) + growthPoint;
    }

    //골든글러브 카드만 골글 포인트를 준다 (기획서 9.1)
    private int GetGoldenGlovePoint(CardMasterData masterData)
    {
        return masterData.CardType == CardType.GoldenGlove ? _goldenGlovePointReward : 0;
    }
}
