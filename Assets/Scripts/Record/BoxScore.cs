using System.Collections.Generic;

/// <summary>
/// 경기 1건의 박스스코어. 타석 로그를 사후 집계해 만든 결과물이다
/// </summary>
public class BoxScore
{
    /// <summary>
    /// 이닝 순서대로. 연장전이면 9칸을 넘어간다
    /// </summary>
    public IReadOnlyList<InningScore> Innings { get; }

    public int AwayRuns { get; }        //R
    public int HomeRuns { get; }
    public int AwayHits { get; }        //H
    public int HomeHits { get; }
    public int AwayErrors { get; }      //E - 그 팀이 수비 중 저지른 실책
    public int HomeErrors { get; }

    /// <summary>
    /// 타순 등장 순서. 대타로 들어온 선수는 뒤에 붙는다
    /// </summary>
    public IReadOnlyList<HitterGameStats> AwayHitters { get; }
    public IReadOnlyList<HitterGameStats> HomeHitters { get; }

    /// <summary>
    /// 등판 순서
    /// </summary>
    public IReadOnlyList<PitcherGameStats> AwayPitchers { get; }
    public IReadOnlyList<PitcherGameStats> HomePitchers { get; }

    public BoxScore(IReadOnlyList<InningScore> innings,
        int awayRuns, int homeRuns, int awayHits, int homeHits, int awayErrors, int homeErrors,
        IReadOnlyList<HitterGameStats> awayHitters, IReadOnlyList<HitterGameStats> homeHitters,
        IReadOnlyList<PitcherGameStats> awayPitchers, IReadOnlyList<PitcherGameStats> homePitchers)
    {
        Innings = innings;

        AwayRuns = awayRuns;
        HomeRuns = homeRuns;
        AwayHits = awayHits;
        HomeHits = homeHits;
        AwayErrors = awayErrors;
        HomeErrors = homeErrors;

        AwayHitters = awayHitters;
        HomeHitters = homeHitters;
        AwayPitchers = awayPitchers;
        HomePitchers = homePitchers;
    }
}
