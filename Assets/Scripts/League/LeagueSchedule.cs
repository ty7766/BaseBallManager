using System.Collections.Generic;

/// <summary>
/// 리그 1회분 전체 일정 (하루 = 5경기, 플레이어는 하루 1경기)
/// </summary>
public class LeagueSchedule
{
    public LeagueTier Tier { get; }
    public string PlayerTeamName { get; }

    //같은 상대와 연속으로 치르는 경기 수. 세이브에서 일정을 그대로 재생성할 때 필요
    public int SeriesLength { get; }

    public IReadOnlyList<LeagueGameDay> Days => _days;

    //플레이어는 하루에 정확히 1경기를 치르므로 일수 = 플레이어 경기 수
    public int PlayerGameCount => _days.Length;

    private readonly LeagueGameDay[] _days;

    public LeagueSchedule(LeagueTier tier, string playerTeamName, int seriesLength, LeagueGameDay[] days)
    {
        Tier = tier;
        PlayerTeamName = playerTeamName;
        SeriesLength = seriesLength;
        _days = days;
    }
}
