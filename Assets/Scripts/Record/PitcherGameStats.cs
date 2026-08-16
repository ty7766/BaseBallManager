/// <summary>
/// 투수 1명의 경기 1건 기록 (기획서 8.5 박스스코어)
/// </summary>
public class PitcherGameStats
{
    public int InstanceId { get; }
    public string Name { get; }

    public int OutsRecorded { get; private set; }       //잡아낸 아웃 수 (이닝 표기의 원본)
    public int PitchCount { get; private set; }         //투구 수
    public int BattersFaced { get; private set; }       //상대한 타자 수
    public int HitsAllowed { get; private set; }        //피안타
    public int HomeRunsAllowed { get; private set; }    //피홈런
    public int RunsAllowed { get; private set; }        //실점
    public int Walks { get; private set; }              //볼넷 허용
    public int StrikeOuts { get; private set; }         //탈삼진

    public PitcherGameStats(int instanceId, string name)
    {
        InstanceId = instanceId;
        Name = name;
    }

    /// <summary>
    /// 이닝 표기 문자열 ("5 2/3" 형태)
    /// </summary>
    /// <remarks>야구는 이닝을 아웃 3개 단위로 세므로 소수점이 아니라 분수로 표기한다.</remarks>
    public string GetInningsPitchedText()
    {
        int fullInnings = OutsRecorded / 3;
        int remainder = OutsRecorded % 3;

        if (remainder == 0)
            return fullInnings.ToString();

        return $"{fullInnings} {remainder}/3";
    }

    //타석 1회 결과 반영
    public void AddBatterFaced(BatterOutcome outcome, int pitchCount, int runsAllowed)
    {
        BattersFaced++;
        PitchCount += pitchCount;
        RunsAllowed += runsAllowed;
        OutsRecorded += GetOutsFrom(outcome);

        switch (outcome)
        {
            case BatterOutcome.Single:
            case BatterOutcome.Double:
            case BatterOutcome.Triple:
                HitsAllowed++;
                break;
            case BatterOutcome.HomeRun:
                HitsAllowed++;
                HomeRunsAllowed++;
                break;
            case BatterOutcome.Walk:
                Walks++;
                break;
            case BatterOutcome.StrikeOut:
                StrikeOuts++;
                break;
        }
    }

    //결과별로 잡아낸 아웃 수. 실책 출루는 아웃이 아니다
    private static int GetOutsFrom(BatterOutcome outcome)
    {
        return outcome switch
        {
            BatterOutcome.DoublePlay => 2,
            BatterOutcome.StrikeOut => 1,
            BatterOutcome.GroundOut => 1,
            BatterOutcome.FlyOut => 1,
            BatterOutcome.SacrificeFly => 1,
            _ => 0
        };
    }
}
