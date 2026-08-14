using System.Collections.Generic;

/// <summary>
/// 하루치 경기 결과 (5경기 점수 + 플레이어 경기 상세)
/// </summary>
public class LeagueDayResult
{
    public int DayIndex { get; }

    //0번이 플레이어 경기 (LeagueGameDay와 같은 순서)
    public IReadOnlyList<LeagueGameScore> Scores => _scores;

    //타석 로그 포함. 실시간 로그 관전·박스스코어에 사용 (기획서 8.5)
    public GameResult PlayerGameResult { get; }

    private readonly LeagueGameScore[] _scores;

    public LeagueDayResult(int dayIndex, LeagueGameScore[] scores, GameResult playerGameResult)
    {
        DayIndex = dayIndex;
        _scores = scores;
        PlayerGameResult = playerGameResult;
    }
}
