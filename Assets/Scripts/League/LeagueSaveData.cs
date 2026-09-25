using System;

/// <summary>
/// 팀 1개 성적의 저장 형태
/// </summary>
[Serializable]
public class TeamRecordSaveData
{
    public string TeamName;
    public int Wins;
    public int Losses;
    public int Draws;
    public int RunsScored;
    public int RunsAllowed;

    /// <summary>
    /// 아래 3개 배열은 같은 길이이며 인덱스가 서로 대응한다
    /// </summary>
    public string[] OpponentNames;
    public int[] WinsAgainst;
    public int[] LossesAgainst;

    /// <summary>
    /// 치른 경기 수. 진행도와 어긋나면 순위표와 일정이 따로 논다
    /// </summary>
    public int GamePlayedCount => Wins + Losses + Draws;
}

/// <summary>
/// 리그 정규시즌 진행도의 저장 형태 (기획서 7.6 - 리그 진행도만 저장, 개별 경기는 저장하지 않음)
/// </summary>
[Serializable]
public class LeagueSaveData
{
    public int Tier;
    public string PlayerTeamName;

    /// <summary>
    /// 일정을 그대로 재생성하기 위한 팀 순서 (0번 = 플레이어)
    /// </summary>
    public string[] TeamNames;

    /// <summary>
    /// 일정 생성에 쓴 값을 함께 저장 - 티어 테이블을 도중에 수정해도 진행 중인 시즌이 깨지지 않게 함
    /// </summary>
    public int GameCount;
    public int SeriesLength;

    public int CurrentDayIndex;

    /// <summary>
    /// 종료 보상 수령 여부 (기획서 7.9 - 저장 후 재실행으로 중복 수령하는 것을 막음)
    /// </summary>
    public bool RewardsGranted;

    public TeamRecordSaveData[] Records;
}
