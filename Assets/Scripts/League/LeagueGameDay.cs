using System.Collections.Generic;

/// <summary>
/// 리그 하루치 경기 (10팀 = 5경기). 0번 칸이 항상 플레이어 경기
/// </summary>
public class LeagueGameDay
{
    public const int PlayerGameIndex = 0;

    public IReadOnlyList<LeagueGame> Games => _games;
    public LeagueGame PlayerGame => _games[PlayerGameIndex];

    private readonly LeagueGame[] _games;

    public LeagueGameDay(LeagueGame[] games)
    {
        _games = games;
    }
}
