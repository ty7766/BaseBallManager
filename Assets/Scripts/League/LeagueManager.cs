using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리그 시작·진행 진입점 (로스터 · 일정 · 순위표 · 진행기를 엮음)
/// </summary>
public class LeagueManager : MonoBehaviour
{
    public static LeagueManager Instance { get; private set; }

    public LeagueSeason CurrentSeason => _runner == null ? null : _runner.Season;
    public bool IsSeasonRunning => _runner != null && !_runner.Season.IsFinished;
    public PostSeasonRunner PostSeason { get; private set; }

    [Header("티어 -> 경기 수 · 연전 수 테이블 (AiRosterManager와 같은 에셋)")]
    [SerializeField]
    private LeagueTierTable _tierTable;

    [Header("투수 교체 점수 기준 (기획서 8.4)")]
    [SerializeField]
    private int _pullThreshold = 3;

    [Header("리그 종료 보상 (기획서 7.1 · 7.9)")]
    [SerializeField, Tooltip("이 순위 이내면 다음 티어 해금")]
    private int _unlockRankThreshold = 2;
    [SerializeField, Tooltip("우승 시 지급하는 골카 전용 카드 수")]
    private int _goldenGloveEnhanceCardReward = 1;

    private LeagueRunner _runner;
    private LeagueSaveService _saveService;
    private PostSeasonSaveService _postSeasonSaveService;
    private LeagueRewardService _rewardService;

    //포스트시즌 복원 시 로스터를 다시 만들기 위해 보관 (정규시즌 없이 이어하기 가능하게)
    private LeagueTier _currentTier;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            //저장소 교체 지점 - 서버 저장으로 바꿀 땐 여기만 바뀐다 (기획서 10장)
            ISaveStorage storage = new LocalFileStorage();

            _saveService = new LeagueSaveService(storage);
            _postSeasonSaveService = new PostSeasonSaveService(storage);
            _rewardService = new LeagueRewardService(_tierTable, _unlockRankThreshold, _goldenGloveEnhanceCardReward);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //리그 1회분 시작 (일정 생성 + 순위표 초기화)
    public bool StartLeague(LeagueTier tier)
    {
        if (_tierTable == null)
        {
            Debug.LogError("[LeagueManager]: 티어 테이블이 연결되어 있지 않습니다");
            return false;
        }

        if (AiRosterManager.Instance == null || PlayerDataManager.Instance == null)
        {
            Debug.LogError("[LeagueManager]: AiRosterManager 또는 PlayerDataManager가 씬에 없습니다");
            return false;
        }

        string playerTeamName = PlayerDataManager.Instance.PlayerTeamName;

        if (string.IsNullOrEmpty(playerTeamName))
        {
            Debug.LogError("[LeagueManager]: 플레이어 팀이 선택되지 않았습니다");
            return false;
        }

        //해금 조건 (기획서 7.1 - 직전 리그 정규시즌 2등 이상)
        if (!PlayerDataManager.Instance.IsTierUnlocked(tier))
        {
            Debug.LogWarning($"[LeagueManager]: {tier} 리그가 아직 해금되지 않았습니다 (현재 해금 {PlayerDataManager.Instance.HighestUnlockedTier})");
            return false;
        }

        //라인업 20칸을 모두 채워야 입장 가능 (기획서 6.3)
        if (LineUpManager.Instance == null)
        {
            Debug.LogError("[LeagueManager]: LineUpManager가 씬에 없습니다");
            return false;
        }

        if (!LineUpManager.Instance.IsLineupComplete())
        {
            Debug.LogWarning("[LeagueManager]: 라인업이 완성되지 않아 리그에 입장할 수 없습니다 (야수 9 + 투수 11)");
            return false;
        }

        //로스터 실패 원인은 AiRosterManager가 로그로 남김
        if (!AiRosterManager.Instance.BuildRosters(tier))
            return false;

        List<string> opponentNames = AiRosterManager.Instance.GetOpponentTeamNames(playerTeamName);

        LeagueSchedule schedule = LeagueScheduleGenerator.Generate(
            tier, playerTeamName, opponentNames,
            _tierTable.GetGameCount(tier), _tierTable.GetSeriesLength(tier));

        if (schedule == null)
            return false;

        //순위표에 들어가는 팀 순서를 고정 (플레이어 먼저, 그다음 상대팀 순)
        List<string> allTeamNames = new List<string>(opponentNames.Count + 1) { playerTeamName };
        allTeamNames.AddRange(opponentNames);

        LeagueSeason season = new LeagueSeason(schedule, new LeagueStandings(allTeamNames));

        _runner = new LeagueRunner(season, AiRosterManager.Instance.Rosters, _pullThreshold);
        _currentTier = tier;
        PostSeason = null;

        //이전 시즌의 포스트시즌 세이브가 남아 있으면 새 리그와 뒤섞인다 (기획서 7.6 - 재도전 무제한)
        _postSeasonSaveService.Delete();

        Debug.Log($"[LeagueManager]: {tier} 리그 시작 - {schedule.PlayerGameCount}경기 / 참가 {allTeamNames.Count}팀");

        return true;
    }

    //정규시즌 종료 후 포스트시즌 대진표 구성 (기획서 7.5 - 144경기 리그만)
    public bool StartPostSeason()
    {
        if (_runner == null)
        {
            Debug.LogError("[LeagueManager]: 시작된 리그가 없습니다");
            return false;
        }

        //실패 원인은 PostSeasonRunner가 로그로 남김
        PostSeason = PostSeasonRunner.Create(_runner.Season, _runner.ContextFactory, _pullThreshold);

        return PostSeason != null;
    }

    //[게임 시작] 1회 = 포스트시즌 경기 1개 진행 (기획서 7.4). 더 진행할 경기가 없거나 실패하면 null
    public LeagueGameScore? SimulateNextPostSeasonGame()
    {
        if (PostSeason == null)
        {
            Debug.LogError("[LeagueManager]: 시작된 포스트시즌이 없습니다");
            return null;
        }

        return PostSeason.SimulateNextGame();
    }

    //저장된 포스트시즌이 있는지 (이어하기 버튼 노출 판단용)
    public bool HasSavedPostSeason()
    {
        return _postSeasonSaveService.HasSave();
    }

    //포스트시즌 진행도 저장 (기획서 7.6). 경기 1건이 끝날 때마다 호출하면 된다
    public bool SavePostSeason()
    {
        if (PostSeason == null)
        {
            Debug.LogError("[LeagueManager]: 시작된 포스트시즌이 없습니다");
            return false;
        }

        return _postSeasonSaveService.Save(PostSeason, _currentTier, PlayerDataManager.Instance == null
            ? null
            : PlayerDataManager.Instance.PlayerTeamName);
    }

    /// <summary>
    /// 저장된 포스트시즌 이어하기 (기획서 7.6)
    /// </summary>
    /// <remarks>
    /// 정규시즌을 먼저 불러올 필요가 없다. 포스트시즌 세이브가 티어·플레이어 팀을 함께 들고 있어
    /// 로스터를 스스로 다시 만들 수 있다.
    /// </remarks>
    public bool LoadPostSeason()
    {
        if (AiRosterManager.Instance == null || PlayerDataManager.Instance == null)
        {
            Debug.LogError("[LeagueManager]: AiRosterManager 또는 PlayerDataManager가 씬에 없습니다");
            return false;
        }

        //실패 원인은 세이브 서비스가 로그로 남김
        PostSeasonSaveData saveData = _postSeasonSaveService.Load();

        if (saveData == null)
            return false;

        LeagueTier tier = (LeagueTier)saveData.Tier;

        //로스터 실패 원인은 AiRosterManager가 로그로 남김
        if (!AiRosterManager.Instance.BuildRosters(tier))
            return false;

        PlayerDataManager.Instance.SetPlayerTeam(saveData.PlayerTeamName);

        LeagueGameContextFactory contextFactory =
            new LeagueGameContextFactory(AiRosterManager.Instance.Rosters, saveData.PlayerTeamName);

        //실패 원인은 PostSeasonRunner가 로그로 남김
        PostSeason = PostSeasonRunner.Restore(saveData, contextFactory, _pullThreshold);

        if (PostSeason == null)
            return false;

        _currentTier = tier;

        Debug.Log($"[LeagueManager]: {tier} 포스트시즌 이어하기 - {saveData.CurrentSeriesIndex + 1}번째 시리즈");

        return true;
    }

    //저장된 포스트시즌 삭제
    public bool DeleteSavedPostSeason()
    {
        return _postSeasonSaveService.Delete();
    }

    //[게임 시작] 1회 = 경기 1개 진행 (기획서 7.4).
    //내 경기 1건 + 같은 날 AI끼리 4경기가 함께 시뮬되어 순위표에 반영된다
    public LeagueDayResult SimulateNextGame()
    {
        if (_runner == null)
        {
            Debug.LogError("[LeagueManager]: 시작된 리그가 없습니다");
            return null;
        }

        return _runner.SimulateNextGame();
    }

    //저장된 리그가 있는지 (이어하기 버튼 노출 판단용)
    public bool HasSavedLeague()
    {
        return _saveService.HasSave();
    }

    //정규시즌 진행도 저장 (기획서 7.6)
    public bool SaveLeague()
    {
        if (_runner == null)
        {
            Debug.LogError("[LeagueManager]: 시작된 리그가 없습니다");
            return false;
        }

        return _saveService.Save(_runner.Season);
    }

    //저장된 리그 이어하기. 로스터는 저장하지 않고 티어 기준으로 다시 만든다
    public bool LoadLeague()
    {
        if (AiRosterManager.Instance == null || PlayerDataManager.Instance == null)
        {
            Debug.LogError("[LeagueManager]: AiRosterManager 또는 PlayerDataManager가 씬에 없습니다");
            return false;
        }

        //실패 원인은 세이브 서비스가 로그로 남김
        LeagueSeason season = _saveService.Load();

        if (season == null)
            return false;

        if (!AiRosterManager.Instance.BuildRosters(season.Tier))
            return false;

        PlayerDataManager.Instance.SetPlayerTeam(season.PlayerTeamName);

        _runner = new LeagueRunner(season, AiRosterManager.Instance.Rosters, _pullThreshold);
        _currentTier = season.Tier;
        PostSeason = null;

        Debug.Log($"[LeagueManager]: {season.Tier} 리그 이어하기 - {season.CurrentDayIndex}/{season.TotalDayCount}경기");

        return true;
    }

    //저장된 리그 삭제 (재도전으로 새로 시작할 때. 기획서 7.6 - 재도전 무제한).
    //포스트시즌은 정규시즌에 딸린 것이라 함께 지운다. 남겨두면 다음 시즌의 진출 여부와 무관하게 이어하기가 뜬다
    public bool DeleteSavedLeague()
    {
        bool postSeasonDeleted = _postSeasonSaveService.Delete();
        bool leagueDeleted = _saveService.Delete();

        return leagueDeleted && postSeasonDeleted;
    }

    //정규시즌 종료 보상 수령 + 다음 티어 해금 (기획서 7.1 · 7.9). 실패하거나 이미 받았으면 null
    public LeagueRewardResult ClaimSeasonRewards()
    {
        if (_runner == null)
        {
            Debug.LogError("[LeagueManager]: 시작된 리그가 없습니다");
            return null;
        }

        //실패 원인은 보상 서비스가 로그로 남김
        return _rewardService.Grant(_runner.Season);
    }

    //현재 순위표 (기획서 7.7)
    public LeagueStandingRow[] GetRanking()
    {
        if (_runner == null)
        {
            Debug.LogError("[LeagueManager]: 시작된 리그가 없습니다");
            return System.Array.Empty<LeagueStandingRow>();
        }

        return _runner.Season.Standings.GetRanking();
    }
}
