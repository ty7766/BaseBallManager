/// <summary>
/// 라인스코어 한 칸 - 특정 이닝의 양 팀 득점 (기획서 8.5 이닝별 득점)
/// </summary>
public class InningScore
{
    public int Inning { get; }

    public int AwayRuns { get; private set; }
    public int HomeRuns { get; private set; }

    /// <summary>
    /// 홈팀이 이 이닝에 공격을 했는지
    /// </summary>
    /// <remarks>
    /// 홈팀이 이기고 있으면 마지막 회 말 공격을 하지 않는다.
    /// 이 경우 라인스코어에 0이 아니라 'X'를 찍어야 하므로 "0점"과 "공격 없음"을 구분한다.
    /// </remarks>
    public bool HomePlayed { get; private set; }

    public InningScore(int inning)
    {
        Inning = inning;
    }

    //원정팀(초) 득점 누적
    public void AddAwayRuns(int runs)
    {
        AwayRuns += runs;
    }

    //홈팀(말) 득점 누적. 득점이 0이어도 공격을 했다는 사실은 기록된다
    public void AddHomeRuns(int runs)
    {
        HomePlayed = true;
        HomeRuns += runs;
    }
}
