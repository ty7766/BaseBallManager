using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 골든글러브 제작 (기획서 4장)
/// </summary>
/// <remarks>
/// 골든글러브 카드는 뽑기로 나오지 않고 제작으로만 얻는다.
/// 제작 재료는 골든글러브 포인트 + 포인트 + 훈련 카드 3종이며, 하나라도 모자라면 아무것도 차감되지 않는다.
/// </remarks>
public class GoldenGloveCraftManager : MonoBehaviour
{
    public static GoldenGloveCraftManager Instance { get; private set; }

    [Header("일반 제작 - 전체 골글 풀에서 랜덤")]
    [SerializeField]
    private int _randomGoldenGlovePoint = 300;
    [SerializeField]
    private int _randomPoint = 15000;
    [SerializeField]
    private int _randomTrainCard = 20;

    [Header("팀 선택 제작 - 팀만 지정 (기획서 4장 - 더 비쌈)")]
    [SerializeField]
    private int _teamSelectGoldenGlovePoint = 750;
    [SerializeField]
    private int _teamSelectPoint = 40000;
    [SerializeField]
    private int _teamSelectTrainCard = 50;

    //제작 때마다 새 List를 만들지 않도록 재사용하는 후보 버퍼
    private readonly List<int> _candidateBuffer = new List<int>(64);

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

    /// <summary>
    /// 제작 1회 비용 (UI 표기용)
    /// </summary>
    public CurrencyCost[] GetCost(GoldenGloveCraftType craftType)
    {
        if (craftType == GoldenGloveCraftType.TeamSelect)
        {
            return new[]
            {
                new CurrencyCost(CurrencyType.GoldenGlovePoint, _teamSelectGoldenGlovePoint),
                new CurrencyCost(CurrencyType.Point, _teamSelectPoint),
                new CurrencyCost(CurrencyType.TrainCard, _teamSelectTrainCard)
            };
        }

        return new[]
        {
            new CurrencyCost(CurrencyType.GoldenGlovePoint, _randomGoldenGlovePoint),
            new CurrencyCost(CurrencyType.Point, _randomPoint),
            new CurrencyCost(CurrencyType.TrainCard, _randomTrainCard)
        };
    }

    /// <summary>
    /// 일반 제작 - 전체 골글 풀에서 랜덤 1장 (기획서 4장). 실패 시 null
    /// </summary>
    public GoldenGloveCraftResult CraftRandom()
    {
        return Craft(GoldenGloveCraftType.Random, null);
    }

    /// <summary>
    /// 팀 선택 제작 - 지정한 팀의 골글 중 랜덤 1장 (포지션은 지정 불가). 실패 시 null
    /// </summary>
    public GoldenGloveCraftResult CraftForTeam(string teamName)
    {
        if (string.IsNullOrEmpty(teamName))
        {
            Debug.LogError("[GoldenGloveCraftManager] : 제작할 팀명이 비어 있습니다");
            return null;
        }

        return Craft(GoldenGloveCraftType.TeamSelect, teamName);
    }

    //제작 공통 흐름 (풀 확인 -> 인벤 공간 -> 재화 차감 -> 카드 지급)
    private GoldenGloveCraftResult Craft(GoldenGloveCraftType craftType, string teamName)
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[GoldenGloveCraftManager] : CurrencyManager가 씬에 없습니다");
            return null;
        }

        //① 인벤토리 공간 (기획서 9.3 - 한도가 차면 제작도 차단)
        if (InventoryManager.Instance.IsFull)
        {
            Debug.LogWarning("[GoldenGloveCraftManager] : 인벤토리가 꽉 차서 제작할 수 없습니다");
            return null;
        }

        //② 후보 풀. 재화를 차감하기 전에 확인해야 카드를 못 주고 재료만 먹는 일이 없다
        CollectCandidates(teamName);

        if (_candidateBuffer.Count == 0)
        {
            if (teamName == null)
                Debug.LogWarning("[GoldenGloveCraftManager] : 제작 가능한 골든글러브 카드가 없습니다 (마스터 데이터 미입력)");
            else
                Debug.LogWarning($"[GoldenGloveCraftManager] : '{teamName}' 팀의 골든글러브 카드가 없습니다");

            return null;
        }

        //③ 재화 차감. 부족 사유는 CurrencyManager가 로그로 남김
        if (!CurrencyManager.Instance.SpendAll(GetCost(craftType)))
            return null;

        //④ 카드 지급
        int cardId = _candidateBuffer[Random.Range(0, _candidateBuffer.Count)];
        int instanceId = InventoryManager.Instance.AddCard(cardId);

        if (instanceId == -1)
        {
            Debug.LogError($"[GoldenGloveCraftManager] : 재화를 차감했으나 카드 지급에 실패했습니다 (cardId {cardId})");
            return null;
        }

        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(cardId);

        return new GoldenGloveCraftResult(cardId, instanceId, masterData.Name, masterData.TeamName);
    }

    //골든글러브 카드 후보 수집. teamName이 null이면 전체 풀
    private void CollectCandidates(string teamName)
    {
        _candidateBuffer.Clear();

        foreach (HitterMasterData hitter in CardDataManager.Instance.GetAllHitters())
        {
            if (IsCandidate(hitter, teamName))
                _candidateBuffer.Add(hitter.CardId);
        }

        foreach (PitcherMasterData pitcher in CardDataManager.Instance.GetAllPitchers())
        {
            if (IsCandidate(pitcher, teamName))
                _candidateBuffer.Add(pitcher.CardId);
        }
    }

    //골든글러브 카드이면서 (팀 지정이 있으면) 그 팀 소속인지
    private static bool IsCandidate(CardMasterData masterData, string teamName)
    {
        if (masterData.CardType != CardType.GoldenGlove)
            return false;

        return teamName == null || masterData.TeamName == teamName;
    }
}
