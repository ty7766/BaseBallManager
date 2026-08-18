using UnityEngine;

/// <summary>
/// 리그 종료 보상 지급과 다음 티어 해금 (기획서 7.1 · 7.9)
/// </summary>
/// <remarks>
/// 순수 C# 클래스. 튜닝 수치는 인스펙터를 가진 LeagueManager가 생성자로 넘긴다 (세션 22와 같은 판단).
/// </remarks>
public class LeagueRewardService
{
    private readonly LeagueTierTable _tierTable;
    private readonly int _unlockRankThreshold;
    private readonly int _goldenGloveEnhanceCardReward;

    public LeagueRewardService(LeagueTierTable tierTable, int unlockRankThreshold, int goldenGloveEnhanceCardReward)
    {
        _tierTable = tierTable;
        _unlockRankThreshold = unlockRankThreshold;
        _goldenGloveEnhanceCardReward = goldenGloveEnhanceCardReward;
    }

    //정규시즌 종료 보상 수령. 실패하거나 이미 받았으면 null
    public LeagueRewardResult Grant(LeagueSeason season)
    {
        if (season == null)
        {
            Debug.LogError("[LeagueRewardService]: 보상을 지급할 시즌이 없습니다");
            return null;
        }

        //기획서 7.9 - 보상은 리그 "종료 시"에만
        if (!season.IsFinished)
        {
            Debug.LogWarning($"[LeagueRewardService]: 시즌이 아직 진행 중입니다 ({season.CurrentDayIndex}/{season.TotalDayCount})");
            return null;
        }

        if (season.RewardsGranted)
        {
            Debug.LogWarning("[LeagueRewardService]: 이미 보상을 수령한 시즌입니다");
            return null;
        }

        if (_tierTable == null || CurrencyManager.Instance == null || PlayerDataManager.Instance == null)
        {
            Debug.LogError("[LeagueRewardService]: 티어 테이블 또는 매니저가 없습니다 (Currency / Player)");
            return null;
        }

        int rank = FindPlayerRank(season);

        if (rank == -1)
        {
            Debug.LogError($"[LeagueRewardService]: 순위표에서 '{season.PlayerTeamName}'을(를) 찾지 못했습니다");
            return null;
        }

        //① 골드 - 티어 기준값 x 순위 배수
        int gold = Mathf.RoundToInt(_tierTable.GetGoldReward(season.Tier) * _tierTable.GetRankGoldMultiplier(rank));

        if (gold > 0)
            CurrencyManager.Instance.Add(CurrencyType.Gold, gold);

        //② 골카 전용 카드 - 우승 시에만 (기획서 9.1 "골카 전용은 리그 보상". 조건·수량은 TBD)
        int goldenGloveCard = rank == 1 ? _goldenGloveEnhanceCardReward : 0;

        if (goldenGloveCard > 0)
            CurrencyManager.Instance.Add(CurrencyType.EnhanceCardGoldenGlove, goldenGloveCard);

        //③ 해금 - 정규시즌 2위 이상 (기획서 7.1). 포스트시즌 성적과는 무관
        bool tierUnlocked = false;
        LeagueTier nextTier = season.Tier;

        if (rank <= _unlockRankThreshold && TryGetNextTier(season.Tier, out nextTier))
        {
            tierUnlocked = PlayerDataManager.Instance.UnlockTier(nextTier);
        }

        season.MarkRewardsGranted();

        return new LeagueRewardResult(rank, gold, goldenGloveCard, tierUnlocked, nextTier);
    }

    //순위표에서 플레이어 팀의 순위를 찾는다. 없으면 -1
    private static int FindPlayerRank(LeagueSeason season)
    {
        LeagueStandingRow[] ranking = season.Standings.GetRanking();

        foreach (LeagueStandingRow row in ranking)
        {
            if (row.Record.TeamName == season.PlayerTeamName)
                return row.Rank;
        }

        return -1;
    }

    //다음 티어. 마지막 티어(Legend3)면 false
    private static bool TryGetNextTier(LeagueTier tier, out LeagueTier nextTier)
    {
        int nextIndex = (int)tier + 1;

        if (nextIndex >= LeagueTierTable.TierCount)
        {
            nextTier = tier;
            return false;
        }

        nextTier = (LeagueTier)nextIndex;
        return true;
    }
}
