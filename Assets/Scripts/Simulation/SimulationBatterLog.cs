using System.Collections.Generic;

/// <summary>
/// 시뮬레이션 경기 중 타자의 상태 로그 출력용
/// </summary>

public enum BatterOutcome
{
    StrikeOut,      //삼진
    Walk,           //볼넷
    HomeRun,        //홈런
    Single,         //안타
    Double,         //2루타
    Triple,         //3루타
    Error,          //실책
    FlyOut,         //뜬공 아웃
    GroundOut,      //땅볼 아웃
    DoublePlay,     //병살
    SacrificeFly    //희생플라이
}
public class SimulationBatterLog
{
    public BatterOutcome Outcome {  get; }
    public int PitchCount { get; }              // 이번 타석 투구 수
    public int RunsScored {  get; }             //이번 타석에서 난 득점
    public string BatterName { get; }           //로그 출력용 타자 이름

    public int Inning { get; }                  //타석이 벌어진 이닝
    public bool IsTopInning { get; }            //true = 초(원정 공격)
    public int OutCountBefore { get; }          //타석 시작 시점의 아웃 카운트

    public int BatterInstanceId { get; }        //선수별 기록 집계 키
    public int PitcherInstanceId { get; }       //상대한 투수 (기록 집계 키)
    public string PitcherName { get; }          //로그 출력용 투수 이름

    //이번 타석에서 홈을 밟은 주자들. 선수별 득점(R) 집계에 필요하다
    public IReadOnlyList<int> ScoredRunnerIds { get; }

    public SimulationBatterLog(BatterOutcome outcome, int pitchCount, int runsScored, string batterName,
        int inning, bool isTopInning, int outCountBefore,
        int batterInstanceId, int pitcherInstanceId, string pitcherName, IReadOnlyList<int> scoredRunnerIds)
    {
        Outcome = outcome;
        PitchCount = pitchCount;
        RunsScored = runsScored;
        BatterName = batterName;

        Inning = inning;
        IsTopInning = isTopInning;
        OutCountBefore = outCountBefore;

        BatterInstanceId = batterInstanceId;
        PitcherInstanceId = pitcherInstanceId;
        PitcherName = pitcherName;
        ScoredRunnerIds = scoredRunnerIds;
    }
}
