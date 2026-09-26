/// <summary>
/// 라인스코어 한 칸 - 특정 이닝의 양 팀 득점
/// </summary>
public class InningScore
{
    public int Inning { get; }

    public int AwayRuns { get; private set; }
    public int HomeRuns { get; private set; }

    /// <summary>
    /// 홈팀이 이 이닝에 공격했는지. 라인스코어에 0과 X를 구분해 찍기 위해 필요하다
    /// </summary>
    public bool HomePlayed { get; private set; }

    public InningScore(int inning)
    {
        Inning = inning;
    }

    /// <summary>
    /// 원정팀(초) 득점 누적
    /// </summary>
    public void AddAwayRuns(int runs)
    {
        AwayRuns += runs;
    }

    /// <summary>
    /// 홈팀(말) 득점 누적. 0점이어도 공격했다는 사실은 남는다
    /// </summary>
    public void AddHomeRuns(int runs)
    {
        HomePlayed = true;
        HomeRuns += runs;
    }
}
