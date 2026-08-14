using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
/// <summary>
/// 카드 CSV를 1회 읽어 에디터용 표시 목록으로 캐싱 (검색용)
/// </summary>
public static class CardCatalog
{
    public static IReadOnlyList<CardEntry> Hitters
    {
        get { EnsureLoaded(); return _hitters; }
    }

    public static IReadOnlyList<CardEntry> Pitchers
    {
        get { EnsureLoaded(); return _pitchers; }
    }

    private static List<CardEntry> _hitters;
    private static List<CardEntry> _pitchers;
    private static Dictionary<int, CardEntry> _byCardId;

    //cardId로 카드 1장 조회
    public static bool TryGet(int cardId, out CardEntry entry)
    {
        EnsureLoaded();
        return _byCardId.TryGetValue(cardId, out entry);
    }

    //캐시 비우기
    [MenuItem("Tools/BaseBallManager/카드 카탈로그 새로고침")]
    public static void Refresh()
    {
        _hitters = null;
        _pitchers = null;
        _byCardId = null;
        Debug.Log("카드 카탈로그 캐시를 성공적으로 비웠습니다");
    }

    //캐시가 비어있으면 CSV 읽기
    private static void EnsureLoaded()
    {
        if (_hitters != null)
            return;

        try
        {
            CardCSVLoader loader = new CardCSVLoader();

            List<HitterMasterData> hitterData = loader.LoadHitters();
            _hitters = new List<CardEntry>(hitterData.Count);

            foreach (CardMasterData data in hitterData)
            {
                _hitters.Add(ToEntry(data));
            }

            List<PitcherMasterData> pitcherData = loader.LoadPitchers();
            _pitchers = new List<CardEntry>(pitcherData.Count);

            foreach (CardMasterData data in pitcherData)
            {
                _pitchers.Add(ToEntry(data));
            }

            _byCardId = new Dictionary<int, CardEntry>(_hitters.Count + _pitchers.Count);
            foreach (CardEntry entryHitter in _hitters)
            {
                _byCardId[entryHitter.CardId] = entryHitter;
            }
            foreach (CardEntry entryPitcher in _pitchers)
            {
                _byCardId[entryPitcher.CardId] = entryPitcher;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CardCatalog] 카드 CSV 로드 실패: {e.Message}");
            Debug.LogException(e);

            _hitters = new List<CardEntry>();
            _pitchers = new List<CardEntry>();
            _byCardId = new Dictionary<int, CardEntry>();
        }        
    }

    //마스터 데이터 -> 표시용 항목 변환
    private static CardEntry ToEntry(CardMasterData data)
    {
        string label = $"{data.Year} {data.Name} | {data.TeamName} | {data.Position} | OVR {data.OVR} | { GetCardTypeLabel(data.CardType)}";
        return new CardEntry(data.CardId, data.Name, data.TeamName, data.Position, label);
    }

    //CardType -> 한글 라벨
    private static string GetCardTypeLabel(CardType cardType)
    {
        switch (cardType)
        {
            case CardType.Normal:
                return "일반";
            case CardType.Signature:
                return "시그";
            case CardType.GoldenGlove:
                return "골글";
            default:
                throw new ArgumentException($"알 수 없는 CardType: {cardType}");
        }
    }
}
