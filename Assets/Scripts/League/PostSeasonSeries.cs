using System.Collections.Generic;

/// <summary>
/// 포스트시즌 시리즈 1개 (대진 · 승수 · 경기 기록)
/// </summary>
public class PostSeasonSeries
{
    public PostSeasonRound Round { get; }

    //정규시즌 순위가 높은 팀 (홈 어드밴티지 보유)
    public string HigherSeedTeamName { get; }
    public string LowerSeedTeamName { get; private set; }

    //이 승수에 먼저 도달하면 시리즈 승리
    public int WinsToClinch { get; }

    public int HigherSeedWins { get; private set; }
    public int LowerSeedWins { get; private set; }

    public IReadOnlyList<LeagueGameScore> Scores => _scores;

    public bool IsFinished => HigherSeedWins >= WinsToClinch || LowerSeedWins >= WinsToClinch;

    //시리즈가 끝나지 않았으면 null
    public string WinnerTeamName
    {
        get
        {
            if (HigherSeedWins >= WinsToClinch)
                return HigherSeedTeamName;

            if (LowerSeedWins >= WinsToClinch)
                return LowerSeedTeamName;

            return null;
        }
    }

    private readonly List<LeagueGameScore> _scores = new List<LeagueGameScore>();

    public PostSeasonSeries(PostSeasonRound round, string higherSeedTeamName, string lowerSeedTeamName,
        int winsToClinch, int higherSeedAdvantageWins)
    {
        Round = round;
        HigherSeedTeamName = higherSeedTeamName;
        LowerSeedTeamName = lowerSeedTeamName;
        WinsToClinch = winsToClinch;

        //와일드카드의 4위 1승 어드밴티지 (기획서 7.5)
        HigherSeedWins = higherSeedAdvantageWins;
    }

    /// <summary>
    /// 세이브 복원 전용 생성자 (기획서 7.6)
    /// </summary>
    /// <remarks>
    /// 누적 경로(<see cref="AddScore"/>)는 그대로 두고 별도 입구를 낸다.
    /// 승수 프로퍼티의 private set을 열지 않으므로 일반 코드에서는 여전히 수정할 수 없다.
    /// </remarks>
    public PostSeasonSeries(PostSeasonRound round, string higherSeedTeamName, string lowerSeedTeamName,
        int winsToClinch, int higherSeedWins, int lowerSeedWins, IReadOnlyList<LeagueGameScore> scores)
    {
        Round = round;
        HigherSeedTeamName = higherSeedTeamName;
        LowerSeedTeamName = lowerSeedTeamName;
        WinsToClinch = winsToClinch;
        HigherSeedWins = higherSeedWins;
        LowerSeedWins = lowerSeedWins;

        if (scores != null)
            _scores.AddRange(scores);
    }

    //상대가 아직 정해지지 않은 시리즈에 승자를 채워 넣음
    public void SetLowerSeedTeam(string teamName)
    {
        LowerSeedTeamName = teamName;
    }

    //경기 1건 반영. 무승부면 승수를 올리지 않아 재경기로 이어짐
    public void AddScore(LeagueGameScore score)
    {
        _scores.Add(score);

        string winnerTeamName = GetGameWinner(score);

        if (winnerTeamName == null)
            return;

        if (winnerTeamName == HigherSeedTeamName)
            HigherSeedWins++;
        else
            LowerSeedWins++;
    }

    //경기 승리 팀 (무승부면 null)
    private static string GetGameWinner(LeagueGameScore score)
    {
        if (score.HomeScore > score.AwayScore)
            return score.Game.HomeTeamName;

        if (score.AwayScore > score.HomeScore)
            return score.Game.AwayTeamName;

        return null;
    }
}
