/// <summary>
/// 리그 1회분의 진행 상태 (일정 + 순위표 + 진행도)
/// </summary>
public class LeagueSeason
{
    public LeagueSchedule Schedule { get; }
    public LeagueStandings Standings { get; }

    public LeagueTier Tier => Schedule.Tier;
    public string PlayerTeamName => Schedule.PlayerTeamName;

    public int CurrentDayIndex { get; private set; }
    public int TotalDayCount => Schedule.PlayerGameCount;
    public bool IsFinished => CurrentDayIndex >= TotalDayCount;

    //시즌이 끝났으면 null
    public LeagueGameDay CurrentDay => IsFinished ? null : Schedule.Days[CurrentDayIndex];

    public LeagueSeason(LeagueSchedule schedule, LeagueStandings standings)
        : this(schedule, standings, 0)
    {
    }

    //세이브 복원 전용 생성자 - 진행도까지 함께 되살린다
    public LeagueSeason(LeagueSchedule schedule, LeagueStandings standings, int currentDayIndex)
    {
        Schedule = schedule;
        Standings = standings;
        CurrentDayIndex = currentDayIndex;
    }

    //하루 진행 완료 처리
    public void AdvanceDay()
    {
        CurrentDayIndex++;
    }
}
