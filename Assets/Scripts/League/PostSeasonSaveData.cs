using System;

/// <summary>
/// 포스트시즌 경기 1건의 저장 형태
/// </summary>
/// <remarks>
/// LeagueGameScore는 struct라 JsonUtility가 중첩 struct를 다루기 까다롭다.
/// 저장 형태에서는 팀명·점수 4개 값으로 펴서 담는다.
/// </remarks>
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

    //경기 기록으로 다시 셀 수도 있으나 그대로 저장한다.
    //와일드카드는 4위가 1승을 안고 시작하므로(기획서 7.5) 승수가 경기 결과만으로 결정되지 않는다
    public int HigherSeedWins;
    public int LowerSeedWins;

    public PostSeasonGameSaveData[] Games;
}

/// <summary>
/// 포스트시즌 진행도의 저장 형태 (기획서 7.5 · 7.6)
/// </summary>
/// <remarks>
/// 7.4에서 진행 단위가 1경기로 확정되면서, 경기 사이에 앱이 종료될 수 있게 되어 저장이 필요해졌다.
/// 정규시즌 세이브와 키를 분리한다 - 정규시즌이 끝난 뒤에야 생기고, 재도전 시 정규시즌보다 먼저 버려진다.
/// </remarks>
[Serializable]
public class PostSeasonSaveData
{
    //로스터를 다시 만들기 위해 필요 (AI 능력치 보정이 티어마다 다름)
    public int Tier;
    public string PlayerTeamName;

    //진행 중인 시리즈 (Series.Length 이상이면 포스트시즌 종료)
    public int CurrentSeriesIndex;

    public PostSeasonSeriesSaveData[] Series;

    //선발 로테이션을 정규시즌에서 이어가기 위한 팀별 누적 경기 수 (기획서 6.2).
    //아래 2개 배열은 같은 길이이며 인덱스가 서로 대응한다 (JsonUtility가 Dictionary를 못 다룸)
    public string[] RotationTeamNames;
    public int[] RotationGameCounts;
}
