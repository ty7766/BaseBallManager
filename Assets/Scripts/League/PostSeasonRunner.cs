using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포스트시즌 진행 (기획서 7.5 - 144경기 리그 한정, 상위 5팀 KBO 사다리)
/// </summary>
public class PostSeasonRunner
{
    //포스트시즌이 열리는 정규시즌 경기 수
    public const int PostSeasonLeagueGameCount = 144;

    //가을야구 진출 팀 수
    public const int QualifiedTeamCount = 5;

    public IReadOnlyList<PostSeasonSeries> Series => _series;

    //전부 끝났으면 null
    public PostSeasonSeries CurrentSeries => IsFinished ? null : _series[_currentSeriesIndex];

    public bool IsFinished => _currentSeriesIndex >= _series.Count;

    //세이브 기록용 (기획서 7.6)
    public int CurrentSeriesIndex => _currentSeriesIndex;
    public IReadOnlyDictionary<string, int> RotationIndices => _rotationIndices;

    //한국시리즈 우승 팀 (끝나기 전이면 null)
    public string ChampionTeamName => IsFinished ? _series[_series.Count - 1].WinnerTeamName : null;

    //상위 시드 홈 여부 (KBO 방식). 재경기로 길어지면 상위 시드 홈으로 처리
    private static readonly bool[] WildCardHomePattern = { true, true };
    private static readonly bool[] FiveGameHomePattern = { true, true, false, false, true };
    private static readonly bool[] SevenGameHomePattern = { true, true, false, false, false, true, true };

    private readonly List<PostSeasonSeries> _series;
    private readonly LeagueGameContextFactory _contextFactory;
    private readonly GameSimulator _simulator;

    //팀별 누적 등판 경기 수 - 선발 로테이션을 정규시즌에서 이어감 (기획서 6.2)
    private readonly Dictionary<string, int> _rotationIndices;

    private int _currentSeriesIndex;

    private PostSeasonRunner(List<PostSeasonSeries> series, LeagueGameContextFactory contextFactory,
        Dictionary<string, int> rotationIndices, int pullThreshold, int currentSeriesIndex = 0)
    {
        _series = series;
        _contextFactory = contextFactory;
        _rotationIndices = rotationIndices;
        _simulator = new GameSimulator(pullThreshold);
        _currentSeriesIndex = currentSeriesIndex;
    }

    //정규시즌 결과로 대진표 구성. 조건을 못 채우면 null
    public static PostSeasonRunner Create(LeagueSeason season, LeagueGameContextFactory contextFactory, int pullThreshold = 3)
    {
        if (!season.IsFinished)
        {
            Debug.LogError($"[PostSeasonRunner]: 정규시즌이 끝나지 않았습니다 ({season.CurrentDayIndex}/{season.TotalDayCount})");
            return null;
        }

        //기획서 7.5 - 144경기 미만 리그는 포스트시즌 없이 최종 순위로 마감
        if (season.TotalDayCount != PostSeasonLeagueGameCount)
        {
            Debug.LogWarning($"[PostSeasonRunner]: {season.Tier} 리그는 {season.TotalDayCount}경기라 포스트시즌이 없습니다");
            return null;
        }

        LeagueStandingRow[] ranking = season.Standings.GetRanking();

        if (ranking.Length < QualifiedTeamCount)
        {
            Debug.LogError($"[PostSeasonRunner]: 진출 팀이 {ranking.Length}팀뿐입니다 (필요: {QualifiedTeamCount})");
            return null;
        }

        List<PostSeasonSeries> series = new List<PostSeasonSeries>(4)
        {
            //4위는 1승을 안고 시작 - 1승만 하면 진출, 5위는 2연승 필요
            new PostSeasonSeries(PostSeasonRound.WildCard, ranking[3].Record.TeamName, ranking[4].Record.TeamName, 2, 1),
            new PostSeasonSeries(PostSeasonRound.SemiPlayOff, ranking[2].Record.TeamName, null, 3, 0),
            new PostSeasonSeries(PostSeasonRound.PlayOff, ranking[1].Record.TeamName, null, 3, 0),
            new PostSeasonSeries(PostSeasonRound.KoreanSeries, ranking[0].Record.TeamName, null, 4, 0)
        };

        Dictionary<string, int> rotationIndices = new Dictionary<string, int>(ranking.Length);

        foreach (LeagueStandingRow row in ranking)
        {
            rotationIndices[row.Record.TeamName] = row.Record.GamePlayedCount;
        }

        return new PostSeasonRunner(series, contextFactory, rotationIndices, pullThreshold);
    }

    /// <summary>
    /// 저장된 진행도로 복원 (기획서 7.6). 데이터가 깨졌으면 null
    /// </summary>
    /// <remarks>
    /// 대진표는 정규시즌 순위로 다시 뽑지 않고 <b>저장된 그대로</b> 되살린다.
    /// 순위표에서 다시 뽑으면 같은 결과가 나오긴 하지만, 이미 진행된 시리즈의 승자가 다음 시리즈에
    /// 채워져 있는 상태까지는 재현되지 않는다.
    /// </remarks>
    public static PostSeasonRunner Restore(PostSeasonSaveData saveData, LeagueGameContextFactory contextFactory, int pullThreshold = 3)
    {
        if (saveData == null || saveData.Series == null || saveData.Series.Length == 0)
        {
            Debug.LogError("[PostSeasonRunner]: 복원할 포스트시즌 데이터가 비어 있습니다");
            return null;
        }

        if (saveData.CurrentSeriesIndex < 0 || saveData.CurrentSeriesIndex > saveData.Series.Length)
        {
            Debug.LogError($"[PostSeasonRunner]: 진행 중인 시리즈 번호({saveData.CurrentSeriesIndex})가 범위를 벗어났습니다 (시리즈 {saveData.Series.Length}개)");
            return null;
        }

        List<PostSeasonSeries> series = new List<PostSeasonSeries>(saveData.Series.Length);

        foreach (PostSeasonSeriesSaveData seriesData in saveData.Series)
        {
            if (seriesData == null)
            {
                Debug.LogError("[PostSeasonRunner]: 시리즈 데이터 중 비어 있는 항목이 있습니다");
                return null;
            }

            series.Add(ToSeries(seriesData));
        }

        Dictionary<string, int> rotationIndices = ToRotationIndices(saveData);

        //로테이션이 비면 전 팀이 선발 1번부터 다시 던지게 되어 기획서 6.2가 깨진다
        if (rotationIndices == null)
            return null;

        return new PostSeasonRunner(series, contextFactory, rotationIndices, pullThreshold, saveData.CurrentSeriesIndex);
    }

    //경기 1건 진행. 더 진행할 경기가 없거나 실패하면 null
    public LeagueGameScore? SimulateNextGame()
    {
        PostSeasonSeries series = CurrentSeries;

        if (series == null)
        {
            Debug.LogWarning("[PostSeasonRunner]: 포스트시즌이 이미 끝났습니다");
            return null;
        }

        if (string.IsNullOrEmpty(series.LowerSeedTeamName))
        {
            Debug.LogError($"[PostSeasonRunner]: {series.Round} 상대 팀이 정해지지 않았습니다");
            return null;
        }

        //무승부는 승수를 올리지 않아 재경기가 된다 (기획서 7.5). 반복이 끝나지 않는 경우를 대비한 상한
        int maxGameCount = series.WinsToClinch * 2 + 5;

        if (series.Scores.Count >= maxGameCount)
        {
            Debug.LogError($"[PostSeasonRunner]: {series.Round}가 {maxGameCount}경기를 넘겼습니다 (무승부 반복)");
            return null;
        }

        bool isHigherSeedHome = IsHigherSeedHome(series);

        LeagueGame game = isHigherSeedHome
            ? new LeagueGame(series.HigherSeedTeamName, series.LowerSeedTeamName)
            : new LeagueGame(series.LowerSeedTeamName, series.HigherSeedTeamName);

        SimulationContext context = _contextFactory.Create(game,
            GetRotationIndex(game.HomeTeamName), GetRotationIndex(game.AwayTeamName));

        //원인 로그는 컨텍스트 빌더가 남김
        if (context == null)
            return null;

        GameResult result = _simulator.SimulateGame(context);
        LeagueGameScore score = new LeagueGameScore(game, result.HomeScore, result.AwayScore);

        series.AddScore(score);

        _rotationIndices[game.HomeTeamName] = GetRotationIndex(game.HomeTeamName) + 1;
        _rotationIndices[game.AwayTeamName] = GetRotationIndex(game.AwayTeamName) + 1;

        if (series.IsFinished)
            AdvanceSeries(series);

        return score;
    }

    //다음 시리즈로 승자 전달 후 인덱스 이동
    private void AdvanceSeries(PostSeasonSeries finishedSeries)
    {
        _currentSeriesIndex++;

        if (IsFinished)
            return;

        _series[_currentSeriesIndex].SetLowerSeedTeam(finishedSeries.WinnerTeamName);
    }

    //이번 경기의 홈이 상위 시드인지
    private static bool IsHigherSeedHome(PostSeasonSeries series)
    {
        bool[] pattern = GetHomePattern(series.Round);
        int gameIndex = series.Scores.Count;

        //재경기로 패턴을 넘어가면 상위 시드 홈으로 처리
        return gameIndex >= pattern.Length || pattern[gameIndex];
    }

    //단계별 홈/원정 배정표
    private static bool[] GetHomePattern(PostSeasonRound round) => round switch
    {
        PostSeasonRound.WildCard => WildCardHomePattern,
        PostSeasonRound.SemiPlayOff => FiveGameHomePattern,
        PostSeasonRound.PlayOff => FiveGameHomePattern,
        PostSeasonRound.KoreanSeries => SevenGameHomePattern,
        _ => FiveGameHomePattern
    };

    //팀의 누적 등판 경기 수
    private int GetRotationIndex(string teamName)
    {
        return _rotationIndices.TryGetValue(teamName, out int index) ? index : 0;
    }

    //저장 형태 -> 시리즈
    private static PostSeasonSeries ToSeries(PostSeasonSeriesSaveData seriesData)
    {
        PostSeasonGameSaveData[] games = seriesData.Games ?? System.Array.Empty<PostSeasonGameSaveData>();
        List<LeagueGameScore> scores = new List<LeagueGameScore>(games.Length);

        foreach (PostSeasonGameSaveData gameData in games)
        {
            if (gameData == null)
                continue;

            scores.Add(new LeagueGameScore(
                new LeagueGame(gameData.HomeTeamName, gameData.AwayTeamName),
                gameData.HomeScore, gameData.AwayScore));
        }

        return new PostSeasonSeries((PostSeasonRound)seriesData.Round,
            seriesData.HigherSeedTeamName, seriesData.LowerSeedTeamName,
            seriesData.WinsToClinch, seriesData.HigherSeedWins, seriesData.LowerSeedWins, scores);
    }

    //저장 형태 -> 팀별 누적 경기 수. 길이가 어긋나면 null
    private static Dictionary<string, int> ToRotationIndices(PostSeasonSaveData saveData)
    {
        string[] teamNames = saveData.RotationTeamNames;
        int[] gameCounts = saveData.RotationGameCounts;

        if (teamNames == null || gameCounts == null || teamNames.Length != gameCounts.Length)
        {
            Debug.LogError("[PostSeasonRunner]: 저장된 선발 로테이션 정보가 올바르지 않습니다 (팀명·경기 수 배열 길이 불일치)");
            return null;
        }

        Dictionary<string, int> rotationIndices = new Dictionary<string, int>(teamNames.Length);

        for (int i = 0; i < teamNames.Length; i++)
        {
            rotationIndices[teamNames[i]] = gameCounts[i];
        }

        return rotationIndices;
    }
}
