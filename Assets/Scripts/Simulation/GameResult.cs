using System.Collections.Generic;

/// <summary>
/// 경기 종료 후 결과 저장
/// </summary>
public class GameResult
{
    public int HomeScore { get; }
    public int AwayScore { get; }
    public List<SimulationBatterLog> BatterLogs { get; }

    public GameResult(int homeScore, int awayScore, List<SimulationBatterLog> batterLogs)
    {
        HomeScore = homeScore;
        AwayScore = awayScore;
        BatterLogs = batterLogs;
    }
}
