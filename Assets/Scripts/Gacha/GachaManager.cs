using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 뽑기 시스템 전반 로직
/// 일반 뽑기, 시그니쳐 뽑기 카운터 관리, 천장 시스템 관리
/// 뽑기 1회마다 GachaResult 하나가 나온다
/// </summary>
public class GachaManager : SingletonBehaviour<GachaManager>
{
    /// <summary>
    /// 카드를 고르지 못했을 때 돌려주는 cardId
    /// </summary>
    public const int NoCardPicked = -1;

    /// <summary>
    /// 세이브 저장용 (기획서 3장 - 천장 카운터는 세션을 넘어가도 유지)
    /// </summary>
    public int NormalPityCount => _normalPityCount;
    public int SignaturePityCount => _signaturePityCount;

    [Header("일반 뽑기 확률 구간 설정")]
    [SerializeField, Tooltip("3성 확률")]
    private float _grade3ProbabilityNor = 0.70f;
    [SerializeField, Tooltip("4성 확률")]
    private float _grade4ProbabilityNor = 0.25f;
    [SerializeField, Tooltip("5성 확률. 판정은 나머지 전부라 합이 1이어야 이 값과 일치한다")]
    private float _grade5ProbabilityNor = 0.05f;

    [Header("시그니쳐 뽑기 확률 구간 설정")]
    [SerializeField, Tooltip("4성 확률")]
    private float _grade4ProbabilitySig = 0.80f;
    [SerializeField, Tooltip("5성 확률. 판정은 나머지 전부라 합이 1이어야 이 값과 일치한다")]
    private float _grade5ProbabilitySig = 0.20f;
    [SerializeField, Tooltip("5성이 떴을 때 그것이 시그니쳐 카드일 확률")]
    private float _signatureChanceOnStar5 = 0.15f;

    [Header("천장")]
    [SerializeField, Tooltip("천장이 발동하는 뽑기 횟수")]
    private int _pityLimit = 50;

    private int _normalPityCount;
    private int _signaturePityCount;

    private readonly Dictionary<(CardGrade, CardType), List<int>> _cardPools
        = new Dictionary<(CardGrade, CardType), List<int>>();

    //인스펙터 값 변경 시 에디터가 호출
    private void OnValidate()
    {
        WarnIfProbabilitySumInvalid("일반",
            _grade3ProbabilityNor + _grade4ProbabilityNor + _grade5ProbabilityNor);
        WarnIfProbabilitySumInvalid("시그니쳐",
            _grade4ProbabilitySig + _grade5ProbabilitySig);
    }

    /// <summary>
    /// 1연차 뽑기 (카드 데이터 1개 반환)
    /// </summary>
    public GachaResult Roll1(GachaType gachaType)
    {
        if (!CanRoll(gachaType, 1, out CurrencyType ticketType))
            return null;

        GachaResult gachaResult = RollOnce(gachaType);

        if (gachaResult == null)
            return null;

        CurrencyManager.Instance.Spend(ticketType, 1);
        InventoryManager.Instance.AddCard(gachaResult.CardId);

        return gachaResult;
    }

    /// <summary>
    /// 10연차 뽑기
    /// </summary>
    public List<GachaResult> Roll10(GachaType gachaType)
    {
        if (!CanRoll(gachaType, 10, out CurrencyType ticketType))
            return new List<GachaResult>();

        List<GachaResult> gachaResults = new List<GachaResult>(10);

        for (int i = 0; i < 10; i++)
        {
            GachaResult result = RollOnce(gachaType);

            if (result != null)
                gachaResults.Add(result);
        }

        if (gachaResults.Count == 0)
            return gachaResults;

        ApplyTenRollPity(gachaType, gachaResults);

        CurrencyManager.Instance.Spend(ticketType, gachaResults.Count);

        foreach (GachaResult result in gachaResults)
        {
            InventoryManager.Instance.AddCard(result.CardId);
        }

        return gachaResults;
    }

    /// <summary>
    /// 세이브 복원용 천장 카운터 주입
    /// </summary>
    public void RestorePityCounts(int normalPityCount, int signaturePityCount)
    {
        if (normalPityCount < 0 || signaturePityCount < 0)
        {
            Debug.LogError($"[GachaManager] : 복원할 천장 카운터가 음수입니다 (일반 {normalPityCount} / 시그 {signaturePityCount})");
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

    //뽑기 가능 여부 확인 + 소모할 뽑기권 종류 반환
    private bool CanRoll(GachaType gachaType, int count, out CurrencyType ticketType)
    {
        ticketType = GetTicketType(gachaType);

        if (InventoryManager.Instance == null || CurrencyManager.Instance == null)
        {
            Debug.LogError("[GachaManager] : InventoryManager 또는 CurrencyManager가 씬에 없습니다");
            return false;
        }

        if (InventoryManager.Instance.Count + count > InventoryManager.Instance.MaxCapacity)
        {
            Debug.LogWarning($"[GachaManager] : 인벤토리에 {count}장을 넣을 공간이 없습니다");
            return false;
        }

        if (!CurrencyManager.Instance.CanAfford(ticketType, count))
        {
            Debug.LogWarning($"[GachaManager] : 뽑기권이 부족합니다 (보유 {CurrencyManager.Instance.GetAmount(ticketType)} / 필요 {count})");
            return false;
        }

        return true;
    }

    //1회 뽑기 - 천장 확인 후 일반 뽑기
    private GachaResult RollOnce(GachaType gachaType)
    {
        if (IsPityReached(gachaType))
        {
            int confirmedId = PickTeamConfirmedCard(gachaType);

            if (confirmedId != NoCardPicked)
            {
                GachaResult confirmed = BuildResult(confirmedId, CardGrade.Star5);

                //카드를 실제로 만들어낸 뒤에 천장을 소모한다
                if (confirmed != null)
                {
                    GetPityCounter(gachaType) = 0;
                    return confirmed;
                }
            }

            Debug.LogWarning($"[GachaManager] : 천장 확정 카드를 찾지 못했습니다 ({gachaType}). 천장을 유지합니다");
        }

        CardGrade grade = DecideGrade(gachaType);
        int cardId = PickCardFromPool(gachaType, grade);

        if (cardId == NoCardPicked)
            return null;

        if (!IsPityReached(gachaType))
            GetPityCounter(gachaType)++;

        return BuildResult(cardId, grade);
    }

    //10연 천장 - 4성 이상이 하나도 없으면 마지막 결과를 교체
    private void ApplyTenRollPity(GachaType gachaType, List<GachaResult> gachaResults)
    {
        foreach (GachaResult result in gachaResults)
        {
            if (result.CardGrade >= CardGrade.Star4)
                return;
        }

        bool isNormal = gachaType == GachaType.Normal;
        float star4 = isNormal ? _grade4ProbabilityNor : _grade4ProbabilitySig;
        float star5 = isNormal ? _grade5ProbabilityNor : _grade5ProbabilitySig;

        float gradeSum = star4 + star5;

        //둘 다 0이면 나눗셈이 NaN이 되어 비교가 조용히 5성으로 굳는다
        CardGrade forcedGrade = gradeSum <= 0f || Random.value < star4 / gradeSum
            ? CardGrade.Star4
            : CardGrade.Star5;

        int forcedId = PickCardFromPool(gachaType, forcedGrade);

        if (forcedId == NoCardPicked)
            return;

        GachaResult forced = BuildResult(forcedId, forcedGrade);

        if (forced == null)
            return;

        gachaResults[gachaResults.Count - 1] = forced;
    }

    //카드 id로 마스터 데이터를 찾아 뽑기 결과 생성
    private GachaResult BuildResult(int cardId, CardGrade grade)
    {
        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(cardId);

        //풀은 마스터 데이터에서 만들었으므로 여기서 실패하면 카드 데이터가 도중에 바뀐 것이다
        if (masterData == null)
        {
            Debug.LogError($"[GachaManager] : 뽑은 카드의 마스터 데이터를 찾지 못했습니다 (cardId {cardId})");
            return null;
        }

        return new GachaResult(cardId, grade, masterData.CardType);
    }

    //뽑기 종류에 맞는 천장 카운터를 참조로 반환
    private ref int GetPityCounter(GachaType gachaType)
    {
        if (gachaType == GachaType.Normal)
            return ref _normalPityCount;

        return ref _signaturePityCount;
    }

    //이번 뽑기가 천장 회차인지 확인 (카운터는 직전까지의 횟수)
    private bool IsPityReached(GachaType gachaType)
    {
        if (_pityLimit <= 0)
            return false;

        return GetPityCounter(gachaType) + 1 >= _pityLimit;
    }

    //천장 시 자팀 5성 확정 카드 id를 반환
    private int PickTeamConfirmedCard(GachaType gachaType)
    {
        string teamName = PlayerDataManager.Instance.PlayerTeamName;

        if (string.IsNullOrEmpty(teamName))
        {
            Debug.LogWarning("[GachaManager] : 플레이어 팀 이름이 설정되지 않았습니다");
            return NoCardPicked;
        }

        CardType targetType = gachaType == GachaType.Normal ? CardType.Normal : CardType.Signature;

        CardDataManager dataManager = CardDataManager.Instance;
        List<int> teamPool = new List<int>();

        foreach (int cardId in GetCardPool(CardGrade.Star5, targetType))
        {
            CardMasterData masterData = dataManager.GetCardMasterData(cardId);

            if (masterData != null && masterData.TeamName == teamName)
                teamPool.Add(cardId);
        }

        if (teamPool.Count == 0)
        {
            Debug.LogWarning($"[GachaManager] : {teamName}의 {targetType} 5성 카드가 없습니다");
            return NoCardPicked;
        }

        return teamPool[Random.Range(0, teamPool.Count)];
    }

    //확률 기반으로 등급 결정
    private CardGrade DecideGrade(GachaType gachaType)
    {
        float roll = Random.value;

        if (gachaType == GachaType.Normal)
        {
            if (roll < _grade3ProbabilityNor)
                return CardGrade.Star3;

            if (roll < _grade3ProbabilityNor + _grade4ProbabilityNor)
                return CardGrade.Star4;

            return CardGrade.Star5;
        }

        if (roll < _grade4ProbabilitySig)
            return CardGrade.Star4;

        return CardGrade.Star5;
    }

    //GachaType에 맞는 랜덤 카드 id를 반환
    private int PickCardFromPool(GachaType gachaType, CardGrade grade)
    {
        CardType targetType = CardType.Normal;

        if (gachaType == GachaType.Signature && grade == CardGrade.Star5)
            targetType = Random.value < _signatureChanceOnStar5 ? CardType.Signature : CardType.Normal;

        List<int> pool = GetCardPool(grade, targetType);

        if (pool.Count == 0)
        {
            Debug.LogWarning($"[GachaManager] : {grade} {targetType} 카드 풀이 비어 있습니다");
            return NoCardPicked;
        }

        return pool[Random.Range(0, pool.Count)];
    }

    //등급·카드종류 조합별 카드 id 풀 반환 (없으면 생성 후 캐시)
    private List<int> GetCardPool(CardGrade grade, CardType cardType)
    {
        if (_cardPools.TryGetValue((grade, cardType), out List<int> cached))
            return cached;

        CardDataManager dataManager = CardDataManager.Instance;
        List<int> pool = new List<int>();

        foreach (HitterMasterData hitter in dataManager.GetAllHitters())
        {
            if (hitter.CardGrade == grade && hitter.CardType == cardType)
                pool.Add(hitter.CardId);
        }

        foreach (PitcherMasterData pitcher in dataManager.GetAllPitchers())
        {
            if (pitcher.CardGrade == grade && pitcher.CardType == cardType)
                pool.Add(pitcher.CardId);
        }

        //빈 풀도 캐시한다. 마스터 데이터는 앱 시작 시 1회만 로드되므로
        //캐시하지 않으면 그 조합을 뽑을 때마다 카드 전체를 다시 훑는다
        _cardPools[(grade, cardType)] = pool;

        return pool;
    }

    //등급 확률 합이 1이 아니면 경고
    private static void WarnIfProbabilitySumInvalid(string label, float sum)
    {
        if (Mathf.Abs(sum - 1f) > 0.0001f)
            Debug.LogWarning($"[GachaManager] : {label} 뽑기 확률 합이 {sum:F3}입니다. 1이 되어야 인스펙터 값과 실제 확률이 일치합니다");
    }
}
