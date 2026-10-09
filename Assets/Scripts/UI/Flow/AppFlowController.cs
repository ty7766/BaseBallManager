using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 앱 전체 흐름. 타이틀 → 로비를 축으로 리그 · 경기 · 수급 · 육성 화면을 전환한다.
/// 화면 조립은 GameUiBootstrap, 시뮬 진행은 GamePlaybackDriver가 맡는다
/// </summary>
public class AppFlowController : MonoBehaviour
{
    private UiRouter _router;
    private GamePlaybackDriver _driver;
    private LiveGameController _interruptController;
    private ToastView _toast;
    private UiEffectLayer _effectLayer;

    private TitleScreen _titleScreen;
    private TeamSelectScreen _teamSelectScreen;
    private LobbyScreen _lobbyScreen;
    private LeagueScreen _leagueScreen;
    private PreGameScreen _preGameScreen;
    private LiveGameScreen _liveGameScreen;
    private PauseSubstitutionScreen _pauseScreen;
    private PostGameScreen _postGameScreen;
    private PostSeasonScreen _postSeasonScreen;
    private InventoryScreen _inventoryScreen;
    private GachaScreen _gachaScreen;
    private LineUpScreen _lineUpScreen;
    private EnhanceScreen _enhanceScreen;
    private TrainScreen _trainScreen;
    private CombineScreen _combineScreen;
    private DismantleScreen _dismantleScreen;
    private GoldenGloveCraftScreen _goldenGloveCraftScreen;
    private CardPickerScreen _cardPickerScreen;
    private CardDetailScreen _cardDetailScreen;

    private GameSession _session;
    private LeagueGame _currentGame;
    private bool _hasCurrentGame;

    /// <summary>
    /// 캔버스와 진행 드라이버를 받아 모든 화면을 조립하고 타이틀을 띄운다
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

        BuildScreens();
        RegisterScreens();

        _toast = new ToastView(canvasRoot);

        _effectLayer = new UiEffectLayer(canvasRoot);
        UiEffects.Layer = _effectLayer;

        _driver.BatterLogAdded += OnBatterLogAdded;
        _driver.StealLogAdded += OnStealLogAdded;
        _driver.Paused += OnPlaybackPaused;
        _driver.Finished += OnPlaybackFinished;

        ShowTitle();
    }

    private void Update()
    {
        float deltaSeconds = Time.unscaledDeltaTime;

        UiAnimator.Tick(deltaSeconds);
        _toast?.Tick(deltaSeconds);
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

    private void BuildScreens()
    {
        _titleScreen = new TitleScreen(OnNewGameRequested, OnContinueRequested);
        _teamSelectScreen = new TeamSelectScreen(OnTeamConfirmed);

        _lobbyScreen = new LobbyScreen(new Action[]
        {
            EnterLeague,
            () => ShowScreen(_lineUpScreen),
            () => ShowScreen(_gachaScreen),
            () => ShowScreen(_inventoryScreen),
            () => ShowScreen(_enhanceScreen),
            () => ShowScreen(_trainScreen),
            () => ShowScreen(_combineScreen),
            () => ShowScreen(_dismantleScreen),
            () => ShowScreen(_goldenGloveCraftScreen),
            SaveAll,
            QuickStart
        });

        _leagueScreen = new LeagueScreen(OnStartGameRequested, OnToggleSubstitutionMode,
            SaveAll, OnClaimSeasonRewards, ShowLobby, () => ShowScreen(_postSeasonScreen));
        _preGameScreen = new PreGameScreen(OnPreGameStart);
        _liveGameScreen = new LiveGameScreen(OnPauseRequested);
        _pauseScreen = new PauseSubstitutionScreen(OnSubstitutionApplied, OnSubstitutionSkipped);
        _postGameScreen = new PostGameScreen(OnReturnToLeague);
        _postSeasonScreen = new PostSeasonScreen(ShowLobby, ShowToast);

        _inventoryScreen = new InventoryScreen(ShowLobby, OpenCardDetail, ShowToast);
        _gachaScreen = new GachaScreen(ShowLobby, OpenCardDetail, ShowToast);
        _lineUpScreen = new LineUpScreen(ShowLobby, OpenCardPicker, ShowToast);
        _enhanceScreen = new EnhanceScreen(ShowLobby, OpenCardPicker, ShowToast);
        _trainScreen = new TrainScreen(ShowLobby, OpenCardPicker, ShowToast);
        _combineScreen = new CombineScreen(ShowLobby, OpenCardPicker, ShowToast);
        _dismantleScreen = new DismantleScreen(ShowLobby, OpenCardPicker, ShowToast);
        _goldenGloveCraftScreen = new GoldenGloveCraftScreen(ShowLobby, OpenCardPicker, ShowToast);

        _cardPickerScreen = new CardPickerScreen(CloseCardPicker);
        _cardDetailScreen = new CardDetailScreen(CloseCardDetail, ShowToast);
    }

    private void RegisterScreens()
    {
        _router.Register(_titleScreen);
        _router.Register(_teamSelectScreen);
        _router.Register(_lobbyScreen);
        _router.Register(_leagueScreen);
        _router.Register(_preGameScreen);
        _router.Register(_liveGameScreen);
        _router.Register(_postGameScreen);
        _router.Register(_postSeasonScreen);
        _router.Register(_inventoryScreen);
        _router.Register(_gachaScreen);
        _router.Register(_lineUpScreen);
        _router.Register(_enhanceScreen);
        _router.Register(_trainScreen);
        _router.Register(_combineScreen);
        _router.Register(_dismantleScreen);
        _router.Register(_goldenGloveCraftScreen);
        _router.Register(_pauseScreen);
        _router.Register(_cardPickerScreen);
        _router.Register(_cardDetailScreen);
    }

    // ---------------------------------------------------------------- 시작 흐름

    private void ShowTitle()
    {
        bool hasSave = GameFlowManager.Instance != null && GameFlowManager.Instance.HasSave();
        string notice = hasSave
            ? "저장된 진행이 있습니다"
            : "새 게임을 시작하면 팀을 고르고 뽑기권을 받습니다";

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
        {
            ShowToast("저장된 진행을 불러오지 못했습니다", false);
            return;
        }

        if (LeagueManager.Instance != null)
        {
            LeagueManager.Instance.SetInterruptHandler(_interruptController);

            if (LeagueManager.Instance.HasSavedLeague())
                LeagueManager.Instance.LoadLeague();

            if (LeagueManager.Instance.HasSavedPostSeason())
                LeagueManager.Instance.LoadPostSeason();
        }

        ShowToast("이어하기", true);
        ShowLobby();
    }

    private void OnTeamConfirmed(string teamName)
    {
        if (GameFlowManager.Instance == null || !GameFlowManager.Instance.StartNewGame(teamName))
        {
            ShowToast("새 게임을 시작하지 못했습니다", false);
            return;
        }

        GameFlowManager.Instance.CompleteTutorial();
        GameFlowManager.Instance.SaveGame();

        ShowToast("뽑기권을 받았습니다. 뽑기 → 라인업 순서로 준비하세요", true);
        ShowLobby();
    }

    // ---------------------------------------------------------------- 로비

    private void ShowLobby()
    {
        _router.Show(_lobbyScreen);
    }

    private void ShowScreen(UiScreen screen)
    {
        _router.Show(screen);
    }

    //뽑기와 라인업을 한 번에 처리해 바로 리그에 들어갈 수 있게 한다
    private void QuickStart()
    {
        if (!RosterBootstrapper.Prepare())
        {
            ShowToast("뽑기권이 모자라 라인업을 다 채우지 못했습니다", false);
            _lobbyScreen.Refresh();
            return;
        }

        SaveAll();
        ShowToast("자동 뽑기와 라인업 편성을 마쳤습니다", true);
        _lobbyScreen.Refresh();
    }

    private void SaveAll()
    {
        bool saved = GameFlowManager.Instance != null && GameFlowManager.Instance.SaveGame();

        if (LeagueManager.Instance != null && LeagueManager.Instance.IsSeasonRunning)
            LeagueManager.Instance.SaveLeague();

        ShowToast(saved ? "저장했습니다" : "저장에 실패했습니다", saved);
    }

    // ---------------------------------------------------------------- 리그

    private void EnterLeague()
    {
        if (LeagueManager.Instance == null || PlayerDataManager.Instance == null)
            return;

        if (LineUpManager.Instance == null || !LineUpManager.Instance.IsLineupComplete())
        {
            ShowToast("라인업을 먼저 채워야 리그에 입장할 수 있습니다", false);
            return;
        }

        LeagueManager.Instance.SetInterruptHandler(_interruptController);

        if (LeagueManager.Instance.IsSeasonRunning)
        {
            ShowLeague();
            return;
        }

        if (!LeagueManager.Instance.StartLeague(PlayerDataManager.Instance.HighestUnlockedTier))
        {
            ShowToast("리그를 시작하지 못했습니다", false);
            return;
        }

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
            PlayerDataManager.Instance.PlayerTeamName, PlayerDataManager.Instance.CurrentSubstitutionMode,
            league.PostSeason != null);

        _router.Show(_leagueScreen);
    }

    private void OnStartGameRequested()
    {
        LeagueManager league = LeagueManager.Instance;

        if (league == null || !league.NextPlayerGame.HasValue)
            return;

        SimulationContext context = league.PeekNextPlayerGameContext();

        if (context == null)
        {
            ShowToast("경기 정보를 만들지 못했습니다", false);
            return;
        }

        _currentGame = league.NextPlayerGame.Value;
        _hasCurrentGame = true;

        _preGameScreen.SetGame(league.CurrentSeason, _currentGame, context,
            PlayerDataManager.Instance.PlayerTeamName);

        _router.Show(_preGameScreen);
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

    //시즌 보상을 받고 다음 단계로 넘긴다 (144경기 리그는 가을야구로)
    private void OnClaimSeasonRewards()
    {
        if (LeagueManager.Instance == null)
            return;

        LeagueRewardResult result = LeagueManager.Instance.ClaimSeasonRewards();

        if (result != null)
        {
            string unlocked = result.TierUnlocked
                ? $" · {LeagueTierNames.Get(result.UnlockedTier)} 해금"
                : string.Empty;

            ShowToast($"{result.Rank}위 — 골드 {result.Gold} · 시그권 {result.SignatureTicket}{unlocked}", true);
        }

        if (LeagueManager.Instance.StartPostSeason())
        {
            LeagueManager.Instance.SavePostSeason();
            GameFlowManager.Instance.SaveGame();
            ShowScreen(_postSeasonScreen);
            return;
        }

        LeagueManager.Instance.DeleteSavedLeague();
        GameFlowManager.Instance.SaveGame();

        ShowToast("다음 리그를 시작할 수 있습니다", true);
        ShowLobby();
    }

    // ---------------------------------------------------------------- 경기

    private void OnPreGameStart()
    {
        if (LeagueManager.Instance == null || !_hasCurrentGame)
            return;

        _session = LeagueManager.Instance.BeginPlayerGame();

        if (_session == null)
        {
            ShowToast("경기를 시작하지 못했습니다", false);
            return;
        }

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
        ShowToast("교체를 예약했습니다", true);
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

    // ---------------------------------------------------------------- 공용 오버레이

    //카드 선택 모달. 선택이 끝나면 모달을 닫고 콜백을 넘긴다
    private void OpenCardPicker(string title, int count,
        Func<CardDisplayInfo, bool> canSelect, Action<List<int>> onPicked)
    {
        _cardPickerScreen.Open(title, count, canSelect, ids =>
        {
            _router.HideOverlay(_cardPickerScreen);
            onPicked?.Invoke(ids);
        });

        _router.ShowOverlay(_cardPickerScreen);
    }

    private void CloseCardPicker()
    {
        _router.HideOverlay(_cardPickerScreen);
    }

    private void OpenCardDetail(int instanceId)
    {
        _cardDetailScreen.Open(instanceId);
        _router.ShowOverlay(_cardDetailScreen);
    }

    private void CloseCardDetail()
    {
        _router.HideOverlay(_cardDetailScreen);
        PlayerPhotoProvider.UnloadAll();
    }

    private void ShowToast(string message, bool success)
    {
        _toast?.Show(message, success);
    }

    private static bool IsManualMode()
    {
        return PlayerDataManager.Instance != null
            && PlayerDataManager.Instance.CurrentSubstitutionMode == SubstitutionMode.Manual;
    }
}
