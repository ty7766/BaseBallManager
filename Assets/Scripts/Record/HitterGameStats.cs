/// <summary>
/// 타자 1명의 경기 1건 기록 (기획서 8.5 박스스코어)
/// </summary>
/// <remarks>집계 중에만 값이 변하고, 완성된 뒤에는 읽기 전용으로 쓴다.</remarks>
public class HitterGameStats
{
    public int InstanceId { get; }
    public string Name { get; }

    public int PlateAppearances { get; private set; }   //타석
    public int AtBats { get; private set; }             //타수 (타석 - 볼넷 - 희생플라이)
    public int Hits { get; private set; }               //안타 (2·3루타, 홈런 포함)
    public int Doubles { get; private set; }            //2루타
    public int Triples { get; private set; }            //3루타
    public int HomeRuns { get; private set; }           //홈런
    public int RunsBattedIn { get; private set; }       //타점
    public int Runs { get; private set; }               //득점
    public int Walks { get; private set; }              //볼넷
    public int StrikeOuts { get; private set; }         //삼진
    public int StolenBases { get; private set; }        //도루 성공 (기획서 8.3.1)
    public int CaughtStealing { get; private set; }     //도루 실패

    //경기 타율. 타수가 0이면 0 (표기는 UI에서 .000 형태로)
    public float Average => AtBats == 0 ? 0f : (float)Hits / AtBats;

    public HitterGameStats(int instanceId, string name)
    {
        InstanceId = instanceId;
        Name = name;
    }

    //타석 1회 결과 반영
    public void AddPlateAppearance(BatterOutcome outcome, int runsBattedIn)
    {
        PlateAppearances++;

        //볼넷과 희생플라이는 타수에서 빠진다 (타율이 부당하게 깎이지 않도록 하는 야구 기록 규칙)
        if (outcome != BatterOutcome.Walk && outcome != BatterOutcome.SacrificeFly)
            AtBats++;

        RunsBattedIn += runsBattedIn;

        switch (outcome)
        {
            case BatterOutcome.Single:
                Hits++;
                break;
            case BatterOutcome.Double:
                Hits++;
                Doubles++;
                break;
            case BatterOutcome.Triple:
                Hits++;
                Triples++;
                break;
            case BatterOutcome.HomeRun:
                Hits++;
                HomeRuns++;
                break;
            case BatterOutcome.Walk:
                Walks++;
                break;
            case BatterOutcome.StrikeOut:
                StrikeOuts++;
                break;
        }
    }

    //홈을 밟았을 때 (타점과 달리 주자 본인에게 붙는 기록)
    public void AddRun()
    {
        Runs++;
    }

    //도루 시도 1건 반영 (기획서 8.3.1). 타석·타수와 무관한 별개 기록
    public void AddStealAttempt(bool isSuccess)
    {
        if (isSuccess)
            StolenBases++;
        else
            CaughtStealing++;
    }
}
