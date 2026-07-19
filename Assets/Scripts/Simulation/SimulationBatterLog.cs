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

    public SimulationBatterLog(BatterOutcome outcome, int pitchCount, int runsScored, string batterName)
    {
        Outcome = outcome;
        PitchCount = pitchCount;
        RunsScored = runsScored;
        BatterName = batterName;
    }
}
