using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CSV에서 읽은 카드 마스터 데이터 보관·조회
/// </summary>
public class CardDataManager : SingletonBehaviour<CardDataManager>
{
    private readonly Dictionary<int, HitterMasterData> _hitters = new Dictionary<int, HitterMasterData>();
    private readonly Dictionary<int, PitcherMasterData> _pitchers = new Dictionary<int, PitcherMasterData>();

    protected override void OnSingletonAwake()
    {
        LoadCardMasterData();
    }

    /// <summary>
    /// 전체 타자 목록
    /// </summary>
    public Dictionary<int, HitterMasterData>.ValueCollection GetAllHitters()
    {
        return _hitters.Values;
    }

    /// <summary>
    /// 전체 투수 목록
    /// </summary>
    public Dictionary<int, PitcherMasterData>.ValueCollection GetAllPitchers()
    {
        return _pitchers.Values;
    }

    /// <summary>
    /// cardId로 타자 조회. 없으면 null
    /// </summary>
    public HitterMasterData GetHitter(int cardId)
    {
        if (_hitters.TryGetValue(cardId, out HitterMasterData data))
            return data;

        Debug.LogWarning($"[CardDataManager] : 타자 마스터 데이터를 찾지 못했습니다 (cardId {cardId})");
        return null;
    }

    /// <summary>
    /// cardId로 투수 조회. 없으면 null
    /// </summary>
    public PitcherMasterData GetPitcher(int cardId)
    {
        if (_pitchers.TryGetValue(cardId, out PitcherMasterData data))
            return data;

        Debug.LogWarning($"[CardDataManager] : 투수 마스터 데이터를 찾지 못했습니다 (cardId {cardId})");
        return null;
    }

    /// <summary>
    /// cardId로 카드 종류를 가리지 않고 조회. 없으면 null
    /// </summary>
    public CardMasterData GetCardMasterData(int cardId)
    {
        if (_hitters.TryGetValue(cardId, out HitterMasterData hitterData))
            return hitterData;

        if (_pitchers.TryGetValue(cardId, out PitcherMasterData pitcherData))
            return pitcherData;

        Debug.LogWarning($"[CardDataManager] : 마스터 데이터를 찾지 못했습니다 (cardId {cardId})");
        return null;
    }

    //CSV -> cardId 기준 딕셔너리 2개
    private void LoadCardMasterData()
    {
        CardCSVLoader loader = new CardCSVLoader();

        _hitters.Clear();
        _pitchers.Clear();

        foreach (HitterMasterData hitter in loader.LoadHitters())
        {
            if (!_hitters.TryAdd(hitter.CardId, hitter))
                Debug.LogError($"[CardDataManager]: 타자 CSV에 cardId가 중복된 카드가 있습니다 -> {hitter.CardId}");
        }

        foreach (PitcherMasterData pitcher in loader.LoadPitchers())
        {
            //타자와 투수는 딕셔너리가 달라 각자의 TryAdd로는 교차 중복을 못 잡는다.
            //같은 cardId가 양쪽에 있으면 GetCardMasterData가 늘 타자를 돌려줘 조회 경로마다 다른 카드가 된다
            if (_hitters.ContainsKey(pitcher.CardId))
            {
                Debug.LogError($"[CardDataManager]: cardId {pitcher.CardId}가 타자·투수 CSV에 모두 있습니다. 투수 카드를 버립니다");
                continue;
            }

            if (!_pitchers.TryAdd(pitcher.CardId, pitcher))
                Debug.LogError($"[CardDataManager]: 투수 CSV에 cardId가 중복된 카드가 있습니다 -> {pitcher.CardId}");
        }

        if (_hitters.Count == 0 || _pitchers.Count == 0)
        {
            Debug.LogError($"[CardDataManager]: 카드 마스터 데이터가 비어 있습니다 (타자 {_hitters.Count} / 투수 {_pitchers.Count})");
            return;
        }

        Debug.Log($"[CardDataManager]: 카드 마스터 데이터 로드 완료 (타자 {_hitters.Count} / 투수 {_pitchers.Count})");
    }
}
