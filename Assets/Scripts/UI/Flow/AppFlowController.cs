using UnityEngine;

/// <summary>
/// 타이틀 → 팀 선택 → 리그 → 경기 4화면의 전환을 담당한다.
/// 화면 조립은 GameUiBootstrap, 시뮬 진행은 GamePlaybackDriver가 맡는다
/// </summary>
public class AppFlowController : MonoBehaviour
{
    private UiRouter _router;
    private GamePlaybackDriver _driver;
    private LiveGameController _interruptController;

    private TitleScreen _titleScreen;
    private TeamSelectScreen _teamSelectScreen;
    private LeagueScreen _leagueScreen;
    private PreGameScreen _preGameScreen;
    private LiveGameScreen _liveGameScreen;
    private PauseSubstitutionScreen _pauseScreen;
    private PostGameScreen _postGameScreen;

    private GameSession _session;
    private LeagueGame _currentGame;
    private bool _hasCurrentGame;

    /// <summary>
    /// 캔버스와 진행 드라이버를 받아 화면을 조립하고 타이틀을 띄운다
    /// </summary>
    public void Initialize(RectTransform canvasRoot, GamePlaybackDriver driver, LiveGameController interruptController)
    {
        if (canvasRoot == null || driver == null || interruptController == null)
        {
            Debug.LogError("[AppFlowController]: 초기화 인자가 비어 있습니다");
            return;
        }

        _driver = driver;
        _interruptController = interruptController;
        _router = new UiRouter(canvasRoot);

        _titleScreen = new TitleScreen(OnNewGameRequested, OnContinueRequested);
        _teamSelectScreen = new TeamSelectScreen(OnTeamConfirmed);
        _leagueScreen = new LeagueScreen(OnStartGameRequested, OnToggleSubstitutionMode,
            OnSaveRequested, OnClaimSeasonRewards);
        _preGameScreen = new PreGameScreen(OnPreGameStart);
        _liveGameScreen = new LiveGameScreen(OnPauseRequested);
        _pauseScreen = new PauseSubstitutionScreen(OnSubstitutionApplied, OnSubstitutionSkipped);
        _postGameScreen = new PostGameScreen(OnReturnToLeague);

        _router.Register(_titleScreen);
        _router.Register(_teamSelectScreen);
        _router.Register(_leagueScreen);
        _router.Register(_preGameScreen);
        _router.Register(_liveGameScreen);
        _router.Register(_postGameScreen);
        _router.Register(_pauseScreen);

        _driver.BatterLogAdded += OnBatterLogAdded;
        _driver.StealLogAdded += OnStealLogAdded;
        _driver.Paused += OnPlaybackPaused;
        _driver.Finished += OnPlaybackFinished;

        ShowTitle();
    }

    private void OnDestroy()
    {
        if (_driver == null)
            return;

        _driver.BatterLogAdded -= OnBatterLogAdded;
        _driver.StealLogAdded -= OnStealLogAdded;
        _driver.Paused -= OnPlaybackPaused;
        _driver.Finished -= OnPlaybackFinished;
    }

    private void ShowTitle()
    {
        bool hasSave = GameFlowManager.Instance != null && GameFlowManager.Instance.HasSave();
        string notice = hasSave
            ? "저장된 진행이 있습니다"
            : "새 게임을 시작하면 팀을 고르고 시작 카드를 받습니다";

        _titleScreen.SetSaveAvailable(hasSave, notice);
        _router.Show(_titleScreen);
    }

    private void OnNewGameRequested()
    {
        if (GameFlowManager.Instance == null)
            return;

        _teamSelectScreen.SetTeams(GameFlowManager.Instance.SelectableTeamNames);
        _router.Show(_teamSelectScreen);
    }

    private void OnContinueRequested()
    {
        if (GameFlowManager.Instance == null || !GameFlowManager.Instance.ContinueGame())
            return;

        if (LeagueManager.Instance == null)
            return;

        if (LeagueManager.Instance.HasSavedLeague() && LeagueManager.Instance.LoadLeague())
        {
            LeagueManager.Instance.SetInterruptHandler(_interruptController);
            ShowLeague();
            return;
        }

        StartLeague();
    }

    private void OnTeamConfirmed(string teamName)
    {
        if (GameFlowManager.Instance == null || !GameFlowManager.Instance.StartNewGame(teamName))
            return;

        if (!RosterBootstrapper.Prepare())
            return;

        StartLeague();
    }

    //현재 해금된 가장 높은 티어로 리그를 시작한다
    private void StartLeague()
    {
        if (LeagueManager.Instance == null || PlayerDataManager.Instance == null)
            return;

        LeagueManager.Instance.SetInterruptHandler(_interruptController);

        if (!LeagueManager.Instance.StartLeague(PlayerDataManager.Instance.HighestUnlockedTier))
            return;

        GameFlowManager.Instance.SaveGame();
        LeagueManager.Instance.SaveLeague();

        ShowLeague();
    }

    private void ShowLeague()
    {
        LeagueManager league = LeagueManager.Instance;

        if (league == null || PlayerDataManager.Instance == null)
            return;

        _leagueScreen.Refresh(league.CurrentSeason, league.GetRanking(), league.NextPlayerGame,
            PlayerDataManager.Instance.PlayerTeamName, PlayerDataManager.Instance.CurrentSubstitutionMode);

        _router.Show(_leagueScreen);
    }

    private void OnStartGameRequested()
    {
        LeagueManager league = LeagueManager.Instance;

        if (league == null || !league.NextPlayerGame.HasValue)
            return;

        SimulationContext context = league.PeekNextPlayerGameContext();

        if (context == null)
            return;

        _currentGame = league.NextPlayerGame.Value;
        _hasCurrentGame = true;

        _preGameScreen.SetGame(league.CurrentSeason, _currentGame, context,
            PlayerDataManager.Instance.PlayerTeamName);

        _router.Show(_preGameScreen);
    }

    private void OnPreGameStart()
    {
        if (LeagueManager.Instance == null || !_hasCurrentGame)
            return;

        _session = LeagueManager.Instance.BeginPlayerGame();

        if (_session == null)
            return;

        _liveGameScreen.BeginGame(_session, _currentGame.AwayTeamName, _currentGame.HomeTeamName,
            _session.Context.IsPlayerHome);
        _liveGameScreen.SetPauseState(IsManualMode(), false);

        _router.Show(_liveGameScreen);

        _driver.Play(_session);
    }

    private void OnBatterLogAdded(SimulationBatterLog log)
    {
        _liveGameScreen.AppendBatterLog(log);
        _liveGameScreen.Refresh();
    }

    private void OnStealLogAdded(SimulationStealLog log)
    {
        _liveGameScreen.AppendStealLog(log);
    }

    private void OnPauseRequested()
    {
        if (_driver.RequestPause())
            _liveGameScreen.SetPauseState(IsManualMode(), true);
    }

    private void OnPlaybackPaused()
    {
        _pauseScreen.Open(_session, _interruptController, _session.Context.IsPlayerHome);
        _router.ShowOverlay(_pauseScreen);
    }

    private void OnSubstitutionApplied()
    {
        ClosePauseScreen();
    }

    private void OnSubstitutionSkipped()
    {
        ClosePauseScreen();
    }

    private void ClosePauseScreen()
    {
        _router.HideOverlay(_pauseScreen);
        _driver.Resume();
        _liveGameScreen.SetPauseState(IsManualMode(), false);
    }

    private void OnPlaybackFinished()
    {
        if (_session == null || LeagueManager.Instance == null)
            return;

        GameResult result = _session.BuildResult();

        if (result == null)
            return;

        LeagueManager.Instance.CommitPlayerGame(result);

        BoxScore boxScore = BoxScoreBuilder.Build(result.BatterLogs, result.StealLogs);

        _postGameScreen.SetResult(boxScore, _currentGame.AwayTeamName, _currentGame.HomeTeamName,
            _session.Context.IsPlayerHome);

        _router.Show(_postGameScreen);

        LeagueManager.Instance.SaveLeague();
        GameFlowManager.Instance.SaveGame();

        _session = null;
    }

    private void OnReturnToLeague()
    {
        ShowLeague();
    }

    private void OnToggleSubstitutionMode()
    {
        if (PlayerDataManager.Instance == null)
            return;

        SubstitutionMode next = PlayerDataManager.Instance.CurrentSubstitutionMode == SubstitutionMode.Manual
            ? SubstitutionMode.Auto
            : SubstitutionMode.Manual;

        PlayerDataManager.Instance.SetSubstitutionMode(next);
        ShowLeague();
    }

    private void OnSaveRequested()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.SaveGame();

        if (LeagueManager.Instance != null)
            LeagueManager.Instance.SaveLeague();
    }

    //시즌 보상을 받고 해금된 티어로 다음 리그를 연다
    private void OnClaimSeasonRewards()
    {
        if (LeagueManager.Instance == null)
            return;

        LeagueManager.Instance.ClaimSeasonRewards();
        LeagueManager.Instance.DeleteSavedLeague();

        StartLeague();
    }

    private static bool IsManualMode()
    {
        return PlayerDataManager.Instance != null
            && PlayerDataManager.Instance.CurrentSubstitutionMode == SubstitutionMode.Manual;
    }
}
