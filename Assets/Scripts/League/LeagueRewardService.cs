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

        //순위 배수는 골드·골글 포인트·시그권에 공통으로 적용한다
        float rankMultiplier = _tierTable.GetRankGoldMultiplier(rank);

        //① 골드 - 티어 기준값 x 순위 배수
        int gold = Mathf.RoundToInt(_tierTable.GetGoldReward(season.Tier) * rankMultiplier);

        if (gold > 0)
            CurrencyManager.Instance.Add(CurrencyType.Gold, gold);

        //② 골카 전용 카드 - 우승 시에만 (기획서 9.1 "골카 전용은 리그 보상")
        int goldenGloveCard = rank == 1 ? _goldenGloveEnhanceCardReward : 0;

        if (goldenGloveCard > 0)
            CurrencyManager.Instance.Add(CurrencyType.EnhanceCardGoldenGlove, goldenGloveCard);

        //③ 골든글러브 포인트 - 골글 제작(기획서 4장)의 유일한 대량 획득 경로.
        //골글 카드 분해로도 나오지만 골글 카드 자체가 제작으로만 나오므로 순환이 된다
        int goldenGlovePoint = Mathf.RoundToInt(_tierTable.GetGoldenGlovePointReward(season.Tier) * rankMultiplier);

        if (goldenGlovePoint > 0)
            CurrencyManager.Instance.Add(CurrencyType.GoldenGlovePoint, goldenGlovePoint);

        //④ 시그니쳐 뽑기권 (기획서 9.1 - "리그 보상 등")
        int signatureTicket = Mathf.RoundToInt(_tierTable.GetSignatureTicketReward(season.Tier) * rankMultiplier);

        if (signatureTicket > 0)
            CurrencyManager.Instance.Add(CurrencyType.SignatureTicket, signatureTicket);

        //⑤ 해금 - 정규시즌 2위 이상 (기획서 7.1). 포스트시즌 성적과는 무관
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
    /// <remarks>
    /// 시즌 종료 보상과 달리 <b>매 경기 확률적으로</b> 지급된다.
    /// 승패와 무관하게 지급하는 이유는, 이 두 재화가 육성의 필수 재료라서
    /// 못 이기는 구간에서 보급이 끊기면 영영 따라잡을 수 없게 되기 때문이다.
    /// </remarks>
    public void GrantPerGameRewards(LeagueTier tier)
    {
        if (_tierTable == null || CurrencyManager.Instance == null)
            return;

        //훈련돌파 카드 (기획서 2.2 - 훈련 30 -> 50 해금)
        if (Roll(_tierTable.GetPerGameBreakthroughCardChance(tier)))
            CurrencyManager.Instance.Add(CurrencyType.BreakthroughCard, 1);

        //강화 전용 카드 (기획서 2.1). 등급은 낮은 쪽이 흔하게 나오도록 가중치를 둔다
        if (Roll(_tierTable.GetPerGameEnhanceCardChance(tier)))
            CurrencyManager.Instance.Add(PickEnhanceCardType(), 1);
    }

    //백분율 확률 판정
    private static bool Roll(int percent)
    {
        if (percent <= 0)
            return false;

        return Random.Range(0, 100) < percent;
    }

    /// <summary>
    /// 경기 보상으로 줄 강화 전용 카드 등급을 고른다
    /// </summary>
    /// <remarks>
    /// 시그니쳐 전용은 여기서 나오지 않는다(시그 카드 자체가 뽑기 한정이라 보급이 앞서면 안 됨).
    /// 골카 전용은 기획서 9.1에 따라 리그 종료 보상 전용이다.
    /// </remarks>
    private static CurrencyType PickEnhanceCardType()
    {
        int roll = Random.Range(0, 100);

        if (roll < 60)
            return CurrencyType.EnhanceCardStar3;

        if (roll < 90)
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
