/// <summary>
/// 도루 시도 1건의 결과 로그. 타석과 별개 이벤트라 타석 로그와 따로 쌓는다
/// </summary>
public class SimulationStealLog
{
    public int Inning { get; }              //도루가 벌어진 이닝
    public bool IsTopInning { get; }        //true = 초(원정 공격)
    public int OutCountBefore { get; }      //시도 시점의 아웃 카운트

    public int RunnerInstanceId { get; }    //선수별 기록 집계 키
    public string RunnerName { get; }       //로그 출력용 주자 이름

    /// <summary>
    /// 출발 베이스 (1 = 1루에서 2루로 / 2 = 2루에서 3루로)
    /// </summary>
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
