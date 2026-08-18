using System;
/// <summary>
/// 경기 1건의 시뮬 입력 데이터 (양 팀 타자·투수 스냅샷 + 홈/원정 여부)
/// </summary>
/// <remarks>
/// ⚠️ <b>이름과 달리 완전한 불변 객체가 아니다.</b>
/// 프로퍼티에 <c>set</c>이 없어 배열 <i>참조</i>만 고정될 뿐, <b>배열 요소는 밖에서 덮어쓸 수 있다.</b>
/// 실제로 <see cref="GameSimulator"/>의 대타 교체가 <see cref="HomeLineup"/> / <see cref="AwayLineup"/>의
/// 요소를 직접 갈아끼운다.
/// <para>
/// 따라서 <b>재사용되는 원본 배열(예: AI 고정 로스터)을 그대로 넘기면 안 된다.</b>
/// 경기 1회의 대타 교체가 로스터를 영구히 오염시켜 다음 경기부터 다른 선수가 출전하게 되고,
/// 이 오염은 예외도 로그도 없이 시즌 내내 누적된다.
/// 반드시 사본을 넘길 것 (<c>SimulationContextBuilder.CopyLineup</c>).
/// </para>
/// </remarks>
public class SimulationContext
{
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
