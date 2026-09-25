using System.Collections.Generic;

/// <summary>
/// [게임 시작] 1회분 결과 (같은 날 5경기 점수 + 플레이어 경기 상세)
/// </summary>
public class LeagueDayResult
{
    public int DayIndex { get; }

    /// <summary>
    /// 0번이 플레이어 경기 (LeagueGameDay와 같은 순서). 나머지 4개는 AI끼리의 경기라 점수만 있음
    /// </summary>
    public IReadOnlyList<LeagueGameScore> Scores => _scores;

    /// <summary>
    /// 타석 로그 포함. 실시간 로그 관전·박스스코어에 사용 (기획서 8.5)
    /// </summary>
    public GameResult PlayerGameResult { get; }

    private readonly LeagueGameScore[] _scores;

    public LeagueDayResult(int dayIndex, LeagueGameScore[] scores, GameResult playerGameResult)
    {
        DayIndex = dayIndex;
        _scores = scores;
        PlayerGameResult = playerGameResult;
    }
}
