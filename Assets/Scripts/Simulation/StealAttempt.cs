/// <summary>
/// 도루 시도 1건. 매 타석 전에 판정되므로 힙 할당이 없도록 struct로 둔다
/// </summary>
public readonly struct StealAttempt
{
    /// <summary>
    /// 시도 없음을 나타내는 값
    /// </summary>
    public static readonly StealAttempt None = new StealAttempt(GameState.NoRunner, null, 0);

    public int RunnerInstanceId { get; }
    public string RunnerName { get; }

    /// <summary>
    /// 출발 베이스 (1 = 1루에서 2루로 / 2 = 2루에서 3루로). 홈 스틸은 구현하지 않음
    /// </summary>
    public int FromBase { get; }

    public bool Exists => RunnerInstanceId != GameState.NoRunner;

    public StealAttempt(int runnerInstanceId, string runnerName, int fromBase)
    {
        RunnerInstanceId = runnerInstanceId;
        RunnerName = runnerName;
        FromBase = fromBase;
    }
}
