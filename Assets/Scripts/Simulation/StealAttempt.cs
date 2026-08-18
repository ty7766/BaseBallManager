/// <summary>
/// 도루 시도 1건 (기획서 8.3.1)
/// </summary>
/// <remarks>
/// 매 타석 전에 판정되므로 타석마다 생성된다. struct로 두어 힙 할당을 만들지 않는다.
/// </remarks>
public readonly struct StealAttempt
{
    //시도 없음을 나타내는 값. 베이스 주자 -1 규약과 같은 방식
    public static readonly StealAttempt None = new StealAttempt(-1, null, 0);

    public int RunnerInstanceId { get; }
    public string RunnerName { get; }

    //출발 베이스 (1 = 1루에서 2루로 / 2 = 2루에서 3루로). 홈 스틸은 구현하지 않음
    public int FromBase { get; }

    public bool Exists => RunnerInstanceId != -1;

    public StealAttempt(int runnerInstanceId, string runnerName, int fromBase)
    {
        RunnerInstanceId = runnerInstanceId;
        RunnerName = runnerName;
        FromBase = fromBase;
    }
}
