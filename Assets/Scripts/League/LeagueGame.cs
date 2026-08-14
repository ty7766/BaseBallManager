/// <summary>
/// 리그 경기 1건 (홈팀 / 원정팀)
/// </summary>
public readonly struct LeagueGame
{
    public string HomeTeamName { get; }
    public string AwayTeamName { get; }

    public LeagueGame(string homeTeamName, string awayTeamName)
    {
        HomeTeamName = homeTeamName;
        AwayTeamName = awayTeamName;
    }

    //해당 팀이 이 경기에 나오는지
    public bool Contains(string teamName)
    {
        return HomeTeamName == teamName || AwayTeamName == teamName;
    }

    //해당 팀의 상대 팀명 (참가하지 않는 팀이면 null)
    public string GetOpponent(string teamName)
    {
        if (HomeTeamName == teamName)
            return AwayTeamName;

        if (AwayTeamName == teamName)
            return HomeTeamName;

        return null;
    }
}
