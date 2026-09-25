using UnityEngine;

/// <summary>
/// 리그 종료 보상 지급과 다음 티어 해금 (기획서 7.1 · 7.9)
/// </summary>
public class LeagueRewardService
{
    private const int PercentMax = 100;

    //순위표에서 플레이어 팀을 찾지 못했다는 표시
    private const int RankNotFound = -1;

    private readonly LeagueTierTable _tierTable;
    private readonly int _unlockRankThreshold;
    private readonly int _goldenGloveEnhanceCardReward;
    private readonly int _star3RewardPercent;
    private readonly int _star4RewardPercent;

    public LeagueRewardService(LeagueTierTable tierTable, int unlockRankThreshold,
        int goldenGloveEnhanceCardReward, int star3RewardPercent, int star4RewardPercent)
    {
        _tierTable = tierTable;
        _unlockRankThreshold = unlockRankThreshold;
        _goldenGloveEnhanceCardReward = goldenGloveEnhanceCardReward;
        _star3RewardPercent = star3RewardPercent;
        _star4RewardPercent = star4RewardPercent;
    }

    /// <summary>
    /// 정규시즌 종료 보상 수령. 실패하거나 이미 받았으면 null
    /// </summary>
    public LeagueRewardResult Grant(LeagueSeason season)
    {
        if (season == null)
        {
            Debug.LogError("[LeagueRewardService]: 보상을 지급할 시즌이 없습니다");
            return null;
        }

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

        if (rank == RankNotFound)
        {
            Debug.LogError($"[LeagueRewardService]: 순위표에서 '{season.PlayerTeamName}'을(를) 찾지 못했습니다");
            return null;
        }

        float rankMultiplier = _tierTable.GetRankGoldMultiplier(rank);

        int gold = Mathf.RoundToInt(_tierTable.GetGoldReward(season.Tier) * rankMultiplier);

        if (gold > 0)
            CurrencyManager.Instance.Add(CurrencyType.Gold, gold);

        int goldenGloveCard = rank == 1 ? _goldenGloveEnhanceCardReward : 0;

        if (goldenGloveCard > 0)
            CurrencyManager.Instance.Add(CurrencyType.EnhanceCardGoldenGlove, goldenGloveCard);

        int goldenGlovePoint = Mathf.RoundToInt(_tierTable.GetGoldenGlovePointReward(season.Tier) * rankMultiplier);

        if (goldenGlovePoint > 0)
            CurrencyManager.Instance.Add(CurrencyType.GoldenGlovePoint, goldenGlovePoint);

        int signatureTicket = Mathf.RoundToInt(_tierTable.GetSignatureTicketReward(season.Tier) * rankMultiplier);

        if (signatureTicket > 0)
            CurrencyManager.Instance.Add(CurrencyType.SignatureTicket, signatureTicket);

        bool tierUnlocked = false;
        LeagueTier nextTier = season.Tier;

        if (rank <= _unlockRankThreshold && TryGetNextTier(season.Tier, out nextTier))
        {
            tierUnlocked = PlayerDataManager.Instance.UnlockTier(nextTier);
        }

        season.MarkRewardsGranted();

        return new LeagueRewardResult(rank, gold, goldenGloveCard, goldenGlovePoint, signatureTicket, tierUnlocked, nextTier);
    }

    /// <summary>
    /// 경기 1건 종료 시 보상 (기획서 9.1 - 훈련돌파 카드 · 강화 전용 카드의 획득 경로)
    /// </summary>
    public void GrantPerGameRewards(LeagueTier tier)
    {
        if (_tierTable == null || CurrencyManager.Instance == null)
            return;

        if (Roll(_tierTable.GetPerGameBreakthroughCardChance(tier)))
            CurrencyManager.Instance.Add(CurrencyType.BreakthroughCard, 1);

        if (Roll(_tierTable.GetPerGameEnhanceCardChance(tier)))
            CurrencyManager.Instance.Add(PickEnhanceCardType(), 1);
    }

    //백분율 확률 판정
    private static bool Roll(int percent)
    {
        if (percent <= 0)
            return false;

        return Random.Range(0, PercentMax) < percent;
    }

    //경기 보상으로 줄 강화 전용 카드 등급. 남은 확률이 5성 몫이다
    private CurrencyType PickEnhanceCardType()
    {
        int roll = Random.Range(0, PercentMax);

        if (roll < _star3RewardPercent)
            return CurrencyType.EnhanceCardStar3;

        if (roll < _star3RewardPercent + _star4RewardPercent)
            return CurrencyType.EnhanceCardStar4;

        return CurrencyType.EnhanceCardStar5;
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

        return RankNotFound;
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
