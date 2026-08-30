using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 
/// </summary>
public class CardDataManager : SingletonBehaviour<CardDataManager>
{
    private Dictionary<int, HitterMasterData> _hitters;
    private Dictionary<int, PitcherMasterData> _pitchers;

    protected override void OnSingletonAwake()
    {
        LoadCardMasterData();
    }

    //전체 타자 목록 반환
    public IEnumerable<HitterMasterData> GetAllHitters()
    {
        return _hitters.Values;
    }

    //전체 투수 목록 반환
    public IEnumerable<PitcherMasterData> GetAllPitchers()
    {
        return _pitchers.Values;
    }

    //cardId로 타자 검색
    public HitterMasterData GetHitter(int cardId)
    {
        if (_hitters.TryGetValue(cardId, out HitterMasterData data))
            return data;

        Debug.LogWarning($"[CardDataManager] : HitterMasterData not found : {cardId}");
        return null;
    }

    //cardId로 투수 검색
    public PitcherMasterData GetPitcher(int cardId)
    {
        if (_pitchers.TryGetValue(cardId, out PitcherMasterData data))
            return data;

        Debug.LogWarning($"[CardDataManager] : PitcherMasterData not found : {cardId}");
        return null;
    }

    //Card 데이터를 반환
    public CardMasterData GetCardMasterData(int cardId)
    {
        if (_hitters.TryGetValue(cardId, out HitterMasterData hdata))
            return hdata;
        else if (_pitchers.TryGetValue(cardId, out PitcherMasterData pdata))
            return pdata;
        else
        {
            Debug.LogWarning("[CardDataManager] : 필터에 해당하는 카드가 없습니다.");
            return null;
        }
    }

    //CSV에서 마스터데이터를 로드 -> Dictionary로 초기화
    //cardId와 나머지 데이터를 저장하여 cardId로 확인할 수 있게 동작
    private void LoadCardMasterData()
    {
        CardCSVLoader loader = new CardCSVLoader();

        List<HitterMasterData> hitterList = loader.LoadHitters();
        _hitters = new Dictionary<int, HitterMasterData>();
        foreach (HitterMasterData hitter in hitterList)
        {
            if (!_hitters.TryAdd(hitter.CardId, hitter))
            {
                Debug.LogError($"[CardDataManager]: CSV 파일에서 cardId가 중복된 카드가 있습니다. -> {hitter.CardId}");
            }
        }

        List<PitcherMasterData> pitcherList = loader.LoadPitchers();
        _pitchers = new Dictionary<int, PitcherMasterData>();
        foreach (PitcherMasterData pitcher in pitcherList)
        {
            if (!_pitchers.TryAdd(pitcher.CardId, pitcher))
            {
                Debug.LogError($"[CardDataManager]: CSV 파일에서 cardId가 중복된 카드가 있습니다. -> {pitcher.CardId}");
            }
        }
    }
}
