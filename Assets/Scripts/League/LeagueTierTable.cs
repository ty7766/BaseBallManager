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

    [Header("배열 인덱스 = 최종 순위 - 1 (0 = 1위). 티어 기준 골드에 곱한다")]
    [SerializeField]
    private float[] _rankGoldMultipliers = new float[AiRosterSet.TeamCount]
    { 1.0f, 0.8f, 0.6f, 0.5f, 0.4f, 0.35f, 0.3f, 0.25f, 0.2f, 0.15f };

    private void OnValidate()
    {
        if (_entries.Length != TierCount)
        {
            Array.Resize(ref _entries, TierCount);
        }

        if (_rankGoldMultipliers.Length != AiRosterSet.TeamCount)
        {
            Array.Resize(ref _rankGoldMultipliers, AiRosterSet.TeamCount);
        }
    }

    /// <summary>
    /// 티어 값이 enum 범위 안인지. 세이브에서 읽은 값을 캐스팅하기 전에 쓴다
    /// </summary>
    public static bool IsValidTier(LeagueTier tier)
    {
        return (int)tier >= 0 && (int)tier < TierCount;
    }

    /// <summary>
    /// 해당 티어의 AI 로스터 세트. 없으면 null
    /// </summary>
    public AiRosterSet GetRosterSet(LeagueTier tier)
        => TryGetEntry(tier, out LeagueTierEntry entry) ? entry.RosterSet : null;

    /// <summary>
    /// AI 능력치 보정치
    /// </summary>
    public int GetStatBonus(LeagueTier tier)
        => TryGetEntry(tier, out LeagueTierEntry entry) ? entry.StatBonus : 0;

    /// <summary>
    /// 정규시즌 경기 수
    /// </summary>
    public int GetGameCount(LeagueTier tier)
        => TryGetEntry(tier, out LeagueTierEntry entry) ? entry.GameCount : 0;

    /// <summary>
    /// 같은 팀과 연속으로 치르는 경기 수
    /// </summary>
    public int GetSeriesLength(LeagueTier tier)
        => TryGetEntry(tier, out LeagueTierEntry entry) ? entry.SeriesLength : 0;

    /// <summary>
    /// 리그 종료 보상 골드의 기준값
    /// </summary>
    public int GetGoldReward(LeagueTier tier)
        => TryGetEntry(tier, out LeagueTierEntry entry) ? entry.GoldReward : 0;

    /// <summary>
    /// 리그 종료 보상 골든글러브 포인트의 기준값
    /// </summary>
    public int GetGoldenGlovePointReward(LeagueTier tier)
        => TryGetEntry(tier, out LeagueTierEntry entry) ? entry.GoldenGlovePointReward : 0;

    /// <summary>
    /// 리그 종료 보상 시그니쳐 뽑기권의 기준값
    /// </summary>
    public int GetSignatureTicketReward(LeagueTier tier)
        => TryGetEntry(tier, out LeagueTierEntry entry) ? entry.SignatureTicketReward : 0;

    /// <summary>
    /// 경기 1건당 훈련돌파 카드 획득 확률 (%)
    /// </summary>
    public int GetPerGameBreakthroughCardChance(LeagueTier tier)
        => TryGetEntry(tier, out LeagueTierEntry entry) ? entry.PerGameBreakthroughCardChance : 0;

    /// <summary>
    /// 경기 1건당 강화 전용 카드 획득 확률 (%)
    /// </summary>
    public int GetPerGameEnhanceCardChance(LeagueTier tier)
        => TryGetEntry(tier, out LeagueTierEntry entry) ? entry.PerGameEnhanceCardChance : 0;

    /// <summary>
    /// 최종 순위별 골드 배수. 범위를 벗어난 순위는 0배 (보상 없음)
    /// </summary>
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
