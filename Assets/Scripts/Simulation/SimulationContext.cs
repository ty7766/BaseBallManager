using System;
/// <summary>
/// 시뮬레이션 시작 직전 불러와야 하는 데이터
/// 1. 플레이어 타자/투수
/// 2. AI 타자/투수
/// 3. 스냅샷 배열
/// 4. 플레이어 팀 홈/원정
/// </summary>
public class SimulationContext
{
    public bool IsPlayerHome { get; }

    public HitterSnapshot[] HomeLineup { get; }     //홈팀 타자 라인업
    public HitterSnapshot[] AwayLineup { get; }     //원정팀 타자 라인업
    
    public HitterSnapshot[] HomeBench { get; }      //홈팀 벤치 라인업
    public HitterSnapshot[] AwayBench { get; }      //원정팀 벤치 라인업

    public PitcherSnapshot[] HomePitchers { get; }  //홈팀 투수 라인업
    public PitcherSnapshot[] AwayPitchers { get; }  //원정팀 투수 라인업

    public SimulationContext(
        bool isPlayerHome, HitterSnapshot[] homeLineup, HitterSnapshot[] awayLineup,
        HitterSnapshot[] homeBench, HitterSnapshot[] awayBench,
        PitcherSnapshot[] homePitchers, PitcherSnapshot[] awayPitchers)
    {
        if (homeLineup == null || homeLineup.Length != 9)
            throw new ArgumentException("홈 타자 라인업 중 비어있는 슬롯이 있습니다!");
        if (awayLineup == null || awayLineup.Length != 9)
            throw new ArgumentException("원정 타자 라인업 중 비어있는 슬롯이 있습니다!");
        if (homePitchers == null || homePitchers.Length != 7)
            throw new ArgumentException("홈 투수 라인업 중 비어있는 슬롯이 있습니다!");
        if (awayPitchers == null || awayPitchers.Length != 7)
            throw new ArgumentException("원정 투수 라인업 중 비어있는 슬롯이 있습니다!");

        IsPlayerHome = isPlayerHome;
        HomeLineup = homeLineup;
        AwayLineup = awayLineup;
        HomeBench = homeBench;
        AwayBench = awayBench;
        HomePitchers = homePitchers;
        AwayPitchers = awayPitchers;
    }
}
