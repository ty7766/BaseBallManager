using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 뽑기 시스템 전반 로직
/// 일반 뽑기, 시그니쳐 뽑기 카운터 관리, 천장 시스템 관리
/// * 뽑기를 할 때마다 GachaResult Data가 나옴 *
/// </summary>
public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance {  get; private set; }

    //세이브 저장용 (기획서 3장 - 천장 카운터는 세션을 넘어가도 유지)
    public int NormalPityCount => _normalPityCount;
    public int SignaturePityCount => _signaturePityCount;

    [Header("일반 뽑기 확률 구간 설정")]
    [SerializeField]
    private float _grade3ProbabilityNor = 0.70f;
    [SerializeField]
    private float _grade4ProbabilityNor = 0.25f;
    [SerializeField]
    private float _grade5ProbabilityNor = 0.05f;

    [Header("시그니쳐 뽑기 확률 구간 설정")]
    [SerializeField]
    private float _grade4ProbabilitySig = 0.80f;
    [SerializeField]
    private float _grade5ProbabilitySig = 0.20f;
    [SerializeField]
    private float _gradeSigProbabilitySig = 0.15f;

    private int _normalPityCount;
    private int _signaturePityCount;

    private const int PityLimit = 50;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            //메소드
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //1연차 뽑기 (카드 데이터 1개 반환)
    public GachaResult Roll1(GachaType gachaType)
    {
        if (InventoryManager.Instance.IsFull)
        {
            Debug.LogWarning("[GachaManager] 인벤토리가 꽉 차서 뽑기를 진행할 수 없습니다!");
            return null;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[GachaManager] CurrencyManager가 씬에 없습니다");
            return null;
        }

        CurrencyType ticketType = GetTicketType(gachaType);

        //뽑기권부터 확인한다. 뽑은 뒤에 차감하므로 실패 시 환불 처리가 필요 없음
        if (!CurrencyManager.Instance.CanAfford(ticketType, 1))
        {
            Debug.LogWarning($"[GachaManager] 뽑기권이 부족합니다 (보유 {CurrencyManager.Instance.GetAmount(ticketType)})");
            return null;
        }

        GachaResult gachaResult = RollOnce(gachaType);
        if (gachaResult != null)
        {
            CurrencyManager.Instance.Spend(ticketType, 1);
            InventoryManager.Instance.AddCard(gachaResult.CardId);
        }

        return gachaResult;
    }

    //10연차 뽑기
    public List<GachaResult> Roll10(GachaType gachaType)
    {
        if (InventoryManager.Instance.Count + 10 > InventoryManager.Instance.GetMaxCapacity())
        {
            Debug.LogWarning("[GachaManager] 인벤토리에 공간이 없어 뽑기를 진행할 수 없습니다!");
            return new List<GachaResult>();
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[GachaManager] CurrencyManager가 씬에 없습니다");
            return new List<GachaResult>();
        }

        CurrencyType ticketType = GetTicketType(gachaType);

        if (!CurrencyManager.Instance.CanAfford(ticketType, 10))
        {
            Debug.LogWarning($"[GachaManager] 뽑기권이 부족합니다 (보유 {CurrencyManager.Instance.GetAmount(ticketType)} / 필요 10)");
            return new List<GachaResult>();
        }

        List<GachaResult> gachaResults = new List<GachaResult>(10);

        for (int i = 0; i < 10; i++)
        {
            GachaResult result = RollOnce(gachaType);
            if (result != null)
                gachaResults.Add(result);
        }

        //10연 천장 : 4성 이상이 하나도 없으면 마지막 결과를 교체
        bool hasFourStarOrAbove = false;
        foreach (GachaResult result in gachaResults)
        {
            if (result.Grade >= CardGrade.Star4)
            {
                hasFourStarOrAbove = true;
                break;
            }
        }

        if (!hasFourStarOrAbove && gachaResults.Count > 0)
        {
            CardGrade forcedGrade;
            if (gachaType == GachaType.Normal)
            {
                // Star4 : Star5 = 0.25 : 0.05 → Star4가 83%, Star5가 17%
                float star4Ratio = _grade4ProbabilityNor / (_grade4ProbabilityNor +
            _grade5ProbabilityNor);
                forcedGrade = Random.value < star4Ratio ? CardGrade.Star4 : CardGrade.Star5;
            }
            else
            {
                // Star4 : Star5 = 0.80 : 0.20 → Star4가 80%, Star5가 20%
                float star4Ratio = _grade4ProbabilitySig / (_grade4ProbabilitySig +
            _grade5ProbabilitySig);
                forcedGrade = Random.value < star4Ratio ? CardGrade.Star4 : CardGrade.Star5;
            }

            int forcedId = PickCardFromPool(gachaType, forcedGrade);

            if (forcedId != -1)
            {
                CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(forcedId);
                gachaResults[gachaResults.Count - 1] = new GachaResult(forcedId, forcedGrade, masterData.CardType);
            }
        }

        //실제로 나온 장수만큼만 차감한다 (카드 풀이 비어 결과가 모자란 경우 과금 방지)
        if (gachaResults.Count > 0)
            CurrencyManager.Instance.Spend(ticketType, gachaResults.Count);

        foreach (GachaResult result in gachaResults)
        {
            InventoryManager.Instance.AddCard(result.CardId);
        }

        return gachaResults;
    }

    //세이브 복원용 천장 카운터 주입
    public void RestorePityCounts(int normalPityCount, int signaturePityCount)
    {
        if (normalPityCount < 0 || signaturePityCount < 0)
        {
            Debug.LogError($"[GachaManager] 복원할 천장 카운터가 음수입니다 (일반 {normalPityCount} / 시그 {signaturePityCount})");
            return;
        }

        _normalPityCount = normalPityCount;
        _signaturePityCount = signaturePityCount;
    }

    //뽑기 종류 -> 소모 뽑기권 (기획서 3장)
    private static CurrencyType GetTicketType(GachaType gachaType)
    {
        return gachaType switch
        {
            GachaType.Normal => CurrencyType.NormalTicket,
            GachaType.Signature => CurrencyType.SignatureTicket,
            _ => CurrencyType.None
        };
    }

    private GachaResult RollOnce(GachaType gachaType)
    {
        CardGrade grade = DecideGrade(gachaType);
        int cardId = PickCardFromPool(gachaType, grade);

        bool isPity = IncrementAndCheckPity(gachaType);

        if (isPity)
        {
            int confirmedId = PickTeamConfirmedCard(gachaType);
            if (confirmedId != -1)
            {
                cardId = confirmedId;
                grade = CardGrade.Star5;
            }
        }

        if (cardId == -1)
            return null;

        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(cardId);
        CardType cardType = masterData.CardType;

        return new GachaResult(cardId, grade, cardType);
    }

    private int PickTeamConfirmedCard(GachaType gachaType)
    {
        string teamName = PlayerDataManager.Instance.PlayerTeamName;
        CardType targetType;
        CardGrade targetGrade;

        if (string.IsNullOrEmpty(teamName))
        {
            Debug.LogWarning("[GachaManager] 플레이어 팀 이름이 설정되지 않았습니다.");
            return -1;
        }

        //일반 뽑기인 경우 천장 시 자팀 노말 5성 확정
        if (gachaType == GachaType.Normal)
        {
            targetGrade = CardGrade.Star5;
            targetType = CardType.Normal;
        }
        else
        {
            targetGrade = CardGrade.Star5;
            targetType = CardType.Signature;
        }

        List<int> pool = new List<int>();
        CardDataManager dataManager = CardDataManager.Instance;
        
        foreach(HitterMasterData hitter in dataManager.GetAllHitters())
        {
            if (hitter.TeamName == teamName && hitter.CardGrade == targetGrade && hitter.CardType == targetType)
            {
                pool.Add(hitter.CardId);
            }
        }
        foreach (PitcherMasterData pitcher in dataManager.GetAllPitchers())
        {
            if (pitcher.TeamName == teamName && pitcher.CardGrade == targetGrade && pitcher.CardType == targetType)
            {
                pool.Add(pitcher.CardId);
            }
        }

        //리스트가 비었음을 방지
        if (pool.Count == 0)
        {
            Debug.LogWarning("[GachaManager] : 현재 가챠 리스트가 비어있습니다.");
            return -1;
        }

        //랜덤으로 리스트에서 하나 선택
        return pool[Random.Range(0, pool.Count)];
    }

    //확률 기반으로 등급 결정
    private CardGrade DecideGrade(GachaType gachaType)
    {
        //Random.value(0.0 ~ 1.0)로 뽑은 난수를 누적 확률 구간과 비교해서 등급 결정
        float roll = Random.value;

        if (gachaType == GachaType.Normal)
        {
            //일반 뽑기 (3성 ~ 5성)
            if (roll < _grade3ProbabilityNor)
                return CardGrade.Star3;
            else if (roll < _grade3ProbabilityNor + _grade4ProbabilityNor)
                return CardGrade.Star4;
            else
                return CardGrade.Star5;
        }
        else
        {
            //시그니쳐 뽑기 (4성 ~ 5성)
            if (roll < _grade4ProbabilitySig)
                return CardGrade.Star4;
            else
                return CardGrade.Star5;
        }
    }

    //GachaType에 맞는 랜덤 카드 id를 반환
    private int PickCardFromPool(GachaType gachaType, CardGrade grade)
    {
        CardType targetType = CardType.Normal;
        //일반 뽑기인 경우
        if (gachaType == GachaType.Signature && grade == CardGrade.Star5)
        {
            targetType = Random.value < _gradeSigProbabilitySig ? CardType.Signature : CardType.Normal;
        }

        CardDataManager dataManager = CardDataManager.Instance;
        
        //조건에 맞는 카드 ID 목록을 담을 리스트
        List<int> pool = new List<int>();

        //전체 타자 순회하며 조건에 맞는 카드만 리스트에 담기
        foreach(HitterMasterData hitter in dataManager.GetAllHitters())
        {
            if (hitter.CardGrade == grade && hitter.CardType == targetType)
                pool.Add(hitter.CardId);
        }

        //전체 투수 순회하며 조건에 맞는 카드만 리스트에 담기
        foreach(PitcherMasterData pitcher in dataManager.GetAllPitchers())
        {
            if (pitcher.CardGrade == grade && pitcher.CardType == targetType)
                pool.Add(pitcher.CardId);
        }

        //리스트가 비었음을 방지
        if (pool.Count == 0)
        {
            Debug.LogWarning("[GachaManager] : 현재 가챠 리스트가 비어있습니다.");
            return -1;
        }

        //랜덤으로 리스트에서 하나 선택
        return pool[Random.Range(0, pool.Count)];
    }

    //뽑기 카운터 관리
    private bool IncrementAndCheckPity(GachaType gachaType)
    {
        //뽑기 천장 카운터를 1 올리기
        if (gachaType == GachaType.Normal)
        {
            _normalPityCount++;

            //천장에 도달하면 true반환, 카운터 리셋
            if (_normalPityCount >= PityLimit)
            {
                _normalPityCount = 0;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            _signaturePityCount++;

            //천장에 도달하면 true반환, 카운터 리셋
            if (_signaturePityCount >= PityLimit)
            {
                _signaturePityCount = 0;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
