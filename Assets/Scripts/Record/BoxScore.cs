using System.Collections.Generic;

/// <summary>
/// 경기 1건의 박스스코어 (기획서 8.5 - 이닝별 득점 + 선수별 기록 요약)
/// </summary>
/// <remarks>
/// 타석 로그를 집계해 만든 결과물이며, 시뮬레이션 도중에는 만들지 않는다.
/// 일괄 시뮬에서 경기마다 집계하면 낭비이므로 필요한 경기에서만 BoxScoreBuilder로 만든다.
/// </remarks>
public class BoxScore
{
    //이닝 순서대로. 연장전이면 9칸을 넘어간다
    public IReadOnlyList<InningScore> Innings { get; }

    public int AwayRuns { get; }        //R
    public int HomeRuns { get; }
    public int AwayHits { get; }        //H
    public int HomeHits { get; }
    public int AwayErrors { get; }      //E - 그 팀이 수비 중 저지른 실책
    public int HomeErrors { get; }

    //타순 등장 순서. 대타로 들어온 선수는 뒤에 붙는다
    public IReadOnlyList<HitterGameStats> AwayHitters { get; }
    public IReadOnlyList<HitterGameStats> HomeHitters { get; }

    //등판 순서
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
