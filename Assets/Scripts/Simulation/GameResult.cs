using System.Collections.Generic;

/// <summary>
/// 경기 종료 후 결과 저장
/// </summary>
public class GameResult
{
    public int HomeScore { get; }
    public int AwayScore { get; }
    public List<SimulationBatterLog> BatterLogs { get; }

    //도루 시도 기록 (기획서 8.3.1). 타석과 별개 이벤트라 목록을 따로 둔다
    public List<SimulationStealLog> StealLogs { get; }

    public GameResult(int homeScore, int awayScore, List<SimulationBatterLog> batterLogs,
        List<SimulationStealLog> stealLogs)
    {
        HomeScore = homeScore;
        AwayScore = awayScore;
        BatterLogs = batterLogs;
        StealLogs = stealLogs;
    }
}
