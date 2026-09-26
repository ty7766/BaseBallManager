using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 골든글러브 제작 (기획서 4장)
/// </summary>
public class GoldenGloveCraftManager : SingletonBehaviour<GoldenGloveCraftManager>
{
    [Header("일반 제작 - 전체 골글 풀에서 랜덤")]
    [SerializeField]
    private int _randomGoldenGlovePoint = 50000;
    [SerializeField]
    private int _randomPoint = 2000000;
    [SerializeField]
    private int _randomTrainCard = 100;

    [Header("팀 선택 제작 - 팀만 지정 (기획서 4장 - 더 비쌈)")]
    [SerializeField]
    private int _teamSelectGoldenGlovePoint = 100000;
    [SerializeField]
    private int _teamSelectPoint = 5000000;
    [SerializeField]
    private int _teamSelectTrainCard = 300;

    //제작 때마다 새 List를 만들지 않도록 재사용하는 후보 버퍼
    private readonly List<int> _candidateBuffer = new List<int>(64);

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

        if (InventoryManager.Instance.IsFull)
        {
            Debug.LogWarning("[GoldenGloveCraftManager] : 인벤토리가 꽉 차서 제작할 수 없습니다");
            return null;
        }

        CollectCandidates(teamName);

        if (_candidateBuffer.Count == 0)
        {
            if (teamName == null)
                Debug.LogWarning("[GoldenGloveCraftManager] : 제작 가능한 골든글러브 카드가 없습니다 (마스터 데이터 미입력)");
            else
                Debug.LogWarning($"[GoldenGloveCraftManager] : '{teamName}' 팀의 골든글러브 카드가 없습니다");

            return null;
        }

        if (!CurrencyManager.Instance.SpendAll(GetCost(craftType)))
            return null;

        int cardId = _candidateBuffer[Random.Range(0, _candidateBuffer.Count)];
        int instanceId = InventoryManager.Instance.AddCard(cardId);

        if (instanceId == InventoryManager.InvalidInstanceId)
        {
            Debug.LogError($"[GoldenGloveCraftManager] : 재화를 차감했으나 카드 지급에 실패했습니다 (cardId {cardId})");
            return null;
        }

        CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(cardId);

        //후보는 마스터 풀에서 뽑았으므로 여기서 실패하면 카드 데이터가 도중에 바뀐 것이다
        if (masterData == null)
        {
            Debug.LogError($"[GoldenGloveCraftManager] : 지급한 카드의 마스터 데이터를 찾지 못했습니다 (cardId {cardId})");
            return null;
        }

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
