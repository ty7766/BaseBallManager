using System;

/// <summary>
/// 경기 1건의 시뮬 입력 데이터. 라인업 배열 요소는 교체로 바뀌므로 반드시 사본을 넘긴다
/// </summary>
public class SimulationContext
{
    /// <summary>
    /// 타순 수 (= 선발 야수 수)
    /// </summary>
    public const int LineupSize = 9;

    /// <summary>
    /// 투수진 배열 길이. [0]=SP / [1~5]=RP / [6]=CP
    /// </summary>
    public const int PitcherSlotCount = 7;

    /// <summary>
    /// 선발 투수 슬롯 번호
    /// </summary>
    public const int StartingPitcherSlot = 0;

    /// <summary>
    /// 마무리 투수 슬롯 번호
    /// </summary>
    public const int CloserSlot = 6;

    /// <summary>
    /// 벤치 최대 인원. AI 팀은 벤치가 없어 0칸이다
    /// </summary>
    public const int MaxBenchSize = 5;

    /// <summary>
    /// 선수가 없는 칸. 스냅샷 기본값의 InstanceId가 0이라 빈 벤치 칸이 곧 이 값이다
    /// </summary>
    public const int NoHitter = 0;

    public bool IsPlayerHome { get; }

    public HitterSnapshot[] HomeLineup { get; }     //홈팀 타자 라인업 (타순 순서. 요소는 교체로 바뀔 수 있음)
    public HitterSnapshot[] AwayLineup { get; }     //원정팀 타자 라인업 (타순 순서. 요소는 교체로 바뀔 수 있음)

    public HitterSnapshot[] HomeBench { get; }      //홈팀 벤치 (0~5명. AI 팀은 빈 배열)
    public HitterSnapshot[] AwayBench { get; }      //원정팀 벤치 (0~5명. AI 팀은 빈 배열)

    public PitcherSnapshot[] HomePitchers { get; }  //홈팀 투수진 7칸 [0]=SP / [1~5]=RP / [6]=CP
    public PitcherSnapshot[] AwayPitchers { get; }  //원정팀 투수진 7칸 [0]=SP / [1~5]=RP / [6]=CP

    public SimulationContext(
        bool isPlayerHome, HitterSnapshot[] homeLineup, HitterSnapshot[] awayLineup,
        HitterSnapshot[] homeBench, HitterSnapshot[] awayBench,
        PitcherSnapshot[] homePitchers, PitcherSnapshot[] awayPitchers)
    {
        if (homeLineup == null || homeLineup.Length != LineupSize)
            throw new ArgumentException($"홈 타자 라인업이 {LineupSize}칸이 아닙니다");

        if (awayLineup == null || awayLineup.Length != LineupSize)
            throw new ArgumentException($"원정 타자 라인업이 {LineupSize}칸이 아닙니다");

        if (homePitchers == null || homePitchers.Length != PitcherSlotCount)
            throw new ArgumentException($"홈 투수진이 {PitcherSlotCount}칸이 아닙니다");

        if (awayPitchers == null || awayPitchers.Length != PitcherSlotCount)
            throw new ArgumentException($"원정 투수진이 {PitcherSlotCount}칸이 아닙니다");

        IsPlayerHome = isPlayerHome;
        HomeLineup = homeLineup;
        AwayLineup = awayLineup;
        HomeBench = homeBench ?? Array.Empty<HitterSnapshot>();
        AwayBench = awayBench ?? Array.Empty<HitterSnapshot>();
        HomePitchers = homePitchers;
        AwayPitchers = awayPitchers;
    }
}
