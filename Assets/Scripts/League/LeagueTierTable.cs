using System;
using UnityEngine;
/// <summary>
/// 티어 -> 로스터 세트 + 능력치 보정 테이블
/// </summary>

[CreateAssetMenu(fileName = "LeagueTierTable", menuName = "BaseBallManager/League Tier Table")]
public class LeagueTierTable : ScriptableObject
{
    public const int TierCount = 21;

    [Header("배열 인덱스 = LeagueTier (0 = Basic1)")]
    [SerializeField]
    private LeagueTierEntry[] _entries = new LeagueTierEntry[TierCount];

    private void OnValidate()
    {
        if (_entries.Length != TierCount)
        {
            Array.Resize(ref _entries, TierCount);
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
