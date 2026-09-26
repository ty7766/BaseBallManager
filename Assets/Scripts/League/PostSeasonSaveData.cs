using System;

/// <summary>
/// 포스트시즌 경기 1건의 저장 형태
/// </summary>
[Serializable]
public class PostSeasonGameSaveData
{
    public string HomeTeamName;
    public string AwayTeamName;
    public int HomeScore;
    public int AwayScore;
}

/// <summary>
/// 포스트시즌 시리즈 1개의 저장 형태
/// </summary>
[Serializable]
public class PostSeasonSeriesSaveData
{
    public int Round;

    public string HigherSeedTeamName;
    public string LowerSeedTeamName;

    public int WinsToClinch;

    /// <summary>
    /// 경기 기록으로 다시 셀 수도 있으나 그대로 저장한다.
    /// 와일드카드는 4위가 1승을 안고 시작하므로 승수가 경기 결과만으로 결정되지 않는다
    /// </summary>
    public int HigherSeedWins;
    public int LowerSeedWins;

    public PostSeasonGameSaveData[] Games;
}

/// <summary>
/// 포스트시즌 진행도의 저장 형태
/// </summary>
[Serializable]
public class PostSeasonSaveData
{
    /// <summary>
    /// 로스터를 다시 만들기 위해 필요 (AI 능력치 보정이 티어마다 다름)
    /// </summary>
    public int Tier;
    public string PlayerTeamName;

    /// <summary>
    /// 진행 중인 시리즈 (Series.Length 이상이면 포스트시즌 종료)
    /// </summary>
    public int CurrentSeriesIndex;

    /// <summary>
    /// 종료 보상 수령 여부 (재실행으로 중복 수령하는 것을 막음)
    /// </summary>
    public bool RewardsGranted;

    public PostSeasonSeriesSaveData[] Series;

    /// <summary>
    /// 선발 로테이션을 정규시즌에서 이어가기 위한 팀별 누적 경기 수.
    /// 아래 2개 배열은 같은 길이이며 인덱스가 서로 대응한다 (JsonUtility가 Dictionary를 못 다룸)
    /// </summary>
    public string[] RotationTeamNames;
    public int[] RotationGameCounts;
}
