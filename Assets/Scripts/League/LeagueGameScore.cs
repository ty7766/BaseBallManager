/// <summary>
/// 경기 1건의 최종 점수 (순위표 반영 · 결과 표시용)
/// </summary>
public readonly struct LeagueGameScore
{
    public LeagueGame Game { get; }
    public int HomeScore { get; }
    public int AwayScore { get; }

    public LeagueGameScore(LeagueGame game, int homeScore, int awayScore)
    {
        Game = game;
        HomeScore = homeScore;
        AwayScore = awayScore;
    }
}
