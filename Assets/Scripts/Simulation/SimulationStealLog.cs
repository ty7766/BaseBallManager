/// <summary>
/// 도루 시도 1건의 결과 로그 (기획서 8.3.1)
/// </summary>
/// <remarks>
/// 타석 로그(<see cref="SimulationBatterLog"/>)와 별개로 쌓는다.
/// 도루는 타석이 시작되기 전에 벌어지는 독립 이벤트이고, 도루 실패로 3아웃이 되면
/// 그 타석 자체가 없어져 붙일 타석 로그가 아예 존재하지 않기 때문이다.
/// </remarks>
public class SimulationStealLog
{
    public int Inning { get; }              //도루가 벌어진 이닝
    public bool IsTopInning { get; }        //true = 초(원정 공격)
    public int OutCountBefore { get; }      //시도 시점의 아웃 카운트

    public int RunnerInstanceId { get; }    //선수별 기록 집계 키
    public string RunnerName { get; }       //로그 출력용 주자 이름

    //출발 베이스 (1 = 1루에서 2루로 / 2 = 2루에서 3루로)
    public int FromBase { get; }

    public bool IsSuccess { get; }

    public SimulationStealLog(int inning, bool isTopInning, int outCountBefore,
        int runnerInstanceId, string runnerName, int fromBase, bool isSuccess)
    {
        Inning = inning;
        IsTopInning = isTopInning;
        OutCountBefore = outCountBefore;

        RunnerInstanceId = runnerInstanceId;
        RunnerName = runnerName;
        FromBase = fromBase;
        IsSuccess = isSuccess;
    }
}
