/// <summary>
/// 순위표 한 줄 (순위 + 성적 + 게임차)
/// </summary>
public readonly struct LeagueStandingRow
{
    public int Rank { get; }
    public TeamRecord Record { get; }
    public float GamesBehind { get; }

    public LeagueStandingRow(int rank, TeamRecord record, float gamesBehind)
    {
        Rank = rank;
        Record = record;
        GamesBehind = gamesBehind;
    }
}
