using System;
using UnityEngine;
/// <summary>
/// 티어 -> 로스터 세트 + 능력치 보정 테이블
/// </summary>

[CreateAssetMenu(fileName = "LeagueTierTable", menuName = "BaseBallManager/League Tier Table")]
public class LeagueTierTable : ScriptableObject
{
    public const int TierCount = 21;

    //순위표에 오르는 팀 수 (기획서 7.8 - 나 + AI 9팀)
    public const int TeamCount = 10;

    [Header("배열 인덱스 = LeagueTier (0 = Basic1)")]
    [SerializeField]
    private LeagueTierEntry[] _entries = new LeagueTierEntry[TierCount];

    [Header("배열 인덱스 = 최종 순위 - 1 (0 = 1위). 티어 기준 골드에 곱한다 (기획서 7.9)")]
    [SerializeField]
    private float[] _rankGoldMultipliers = new float[TeamCount]
    { 1.0f, 0.8f, 0.6f, 0.5f, 0.4f, 0.35f, 0.3f, 0.25f, 0.2f, 0.15f };

    private void OnValidate()
    {
        if (_entries.Length != TierCount)
        {
            Array.Resize(ref _entries, TierCount);
        }

        if (_rankGoldMultipliers.Length != TeamCount)
        {
            Array.Resize(ref _rankGoldMultipliers, TeamCount);
        }
    }

    public AiRosterSet GetRosterSet(LeagueTier tier) 
    { 
        if (!TryGetEntry(tier, out LeagueTierEntry entry))
        {
            return null;
        }

        return entry.RosterSet;
    }

    public int GetStatBonus(LeagueTier tier)
    {
        if (!TryGetEntry(tier, out LeagueTierEntry entry))
        {
            return 0;
        }

        return entry.StatBonus;
    }

    public int GetGameCount(LeagueTier tier)
    {
        if (!TryGetEntry(tier, out LeagueTierEntry entry))
        {
            return 0;
        }

        return entry.GameCount;
    }

    public int GetSeriesLength(LeagueTier tier)
    {
        if (!TryGetEntry(tier, out LeagueTierEntry entry))
        {
            return 0;
        }

        return entry.SeriesLength;
    }

    //리그 종료 보상 골드의 기준값 (기획서 7.9)
    public int GetGoldReward(LeagueTier tier)
    {
        if (!TryGetEntry(tier, out LeagueTierEntry entry))
        {
            return 0;
        }

        return entry.GoldReward;
    }

    //최종 순위별 골드 배수. 범위를 벗어난 순위는 0배 (보상 없음)
    public float GetRankGoldMultiplier(int rank)
    {
        int index = rank - 1;

        if (index < 0 || index >= _rankGoldMultipliers.Length)
        {
            Debug.LogError($"[LeagueTierTable]: {rank}위는 순위 배수 표의 범위를 벗어났습니다. 길이 : {_rankGoldMultipliers.Length}");
            return 0f;
        }

        return _rankGoldMultipliers[index];
    }

    private bool TryGetEntry(LeagueTier tier, out LeagueTierEntry entry)
    {
        int index = (int)tier;

        if (index < 0 || index >= _entries.Length)
        {
            Debug.LogError($"[LeagueTierTable]: {tier} 번 티어 검색 도중 배열의 범위를 벗어났습니다. 길이 : {_entries.Length}");
            entry = default;
            return false;
        }

        entry = _entries[index];
        return true;
    }
}
