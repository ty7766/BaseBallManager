using System;

/// <summary>
/// 팀 1개 성적의 저장 형태
/// </summary>
/// <remarks>JsonUtility는 Dictionary를 직렬화하지 못하므로 상대전적을 3개의 나란한 배열로 편다</remarks>
[Serializable]
public class TeamRecordSaveData
{
    public string TeamName;
    public int Wins;
    public int Losses;
    public int Draws;
    public int RunsScored;
    public int RunsAllowed;

    //아래 3개 배열은 같은 길이이며 인덱스가 서로 대응한다
    public string[] OpponentNames;
    public int[] WinsAgainst;
    public int[] LossesAgainst;
}

/// <summary>
/// 리그 정규시즌 진행도의 저장 형태 (기획서 7.6 - 리그 진행도만 저장, 개별 경기는 저장하지 않음)
/// </summary>
[Serializable]
public class LeagueSaveData
{
    public int Tier;
    public string PlayerTeamName;

    //일정을 그대로 재생성하기 위한 팀 순서 (0번 = 플레이어)
    public string[] TeamNames;

    //일정 생성에 쓴 값을 함께 저장 - 티어 테이블을 도중에 수정해도 진행 중인 시즌이 깨지지 않게 함
    public int GameCount;
    public int SeriesLength;

    public int CurrentDayIndex;
    public TeamRecordSaveData[] Records;
}
