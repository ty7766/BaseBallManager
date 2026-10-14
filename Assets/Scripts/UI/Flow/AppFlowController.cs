using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

        if (WasBackKeyPressed())
            GoBack();
    }

    //안드로이드 뒤로가기 키와 iOS 의 ESC 는 새 입력 시스템에서 escapeKey 로 들어온다
    private static bool WasBackKeyPressed()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    //덮어 띄운 화면을 먼저 닫고, 없으면 로비로. 로비·타이틀에서는 아무것도 하지 않는다
    private void GoBack()
    {
        if (_cardDetailScreen.gameObject.activeSelf)
        {
            CloseCardDetail();
            return;
        }

        if (_cardPickerScreen.gameObject.activeSelf)
        {
            CloseCardPicker();
            return;
        }

        if (_pauseScreen.gameObject.activeSelf)
            return;

        if (_router.Current == null
            || _router.Current == _titleScreen
            || _router.Current == _lobbyScreen
            || _router.Current == _liveGameScreen)
        {
            return;
        }

        ShowLobby();
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

    //화면 프리팹을 깔고 콜백을 꽂는다. 프리팹이 없으면 코드로 조립된다
    private void BuildScreens()
    {
        _titleScreen = _router.Create<TitleScreen>("TitleScreen");
        _teamSelectScreen = _router.Create<TeamSelectScreen>("TeamSelectScreen");
        _lobbyScreen = _router.Create<LobbyScreen>("LobbyScreen");
        _leagueScreen = _router.Create<LeagueScreen>("LeagueScreen");
        _preGameScreen = _router.Create<PreGameScreen>("PreGameScreen");
        _liveGameScreen = _router.Create<LiveGameScreen>("LiveGameScreen");
        _postGameScreen = _router.Create<PostGameScreen>("PostGameScreen");
        _postSeasonScreen = _router.Create<PostSeasonScreen>("PostSeasonScreen");
        _inventoryScreen = _router.Create<InventoryScreen>("InventoryScreen");
        _gachaScreen = _router.Create<GachaScreen>("GachaScreen");
        _lineUpScreen = _router.Create<LineUpScreen>("LineUpScreen");
        _enhanceScreen = _router.Create<EnhanceScreen>("EnhanceScreen");
        _trainScreen = _router.Create<TrainScreen>("TrainScreen");
        _combineScreen = _router.Create<CombineScreen>("CombineScreen");
        _dismantleScreen = _router.Create<DismantleScreen>("DismantleScreen");
        _goldenGloveCraftScreen = _router.Create<GoldenGloveCraftScreen>("GoldenGloveCraftScreen");
        _pauseScreen = _router.Create<PauseSubstitutionScreen>("PauseSubstitutionScreen");
        _cardPickerScreen = _router.Create<CardPickerScreen>("CardPickerScreen");
        _cardDetailScreen = _router.Create<CardDetailScreen>("CardDetailScreen");

        _titleScreen.Initialize(OnNewGameRequested, OnContinueRequested);
        _teamSelectScreen.Initialize(OnTeamConfirmed);

        _lobbyScreen.Initialize(new Action[]
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

        _leagueScreen.Initialize(OnStartGameRequested, OnToggleSubstitutionMode,
            SaveAll, OnClaimSeasonRewards, ShowLobby, () => ShowScreen(_postSeasonScreen));
        _preGameScreen.Initialize(OnPreGameStart);
        _liveGameScreen.Initialize(OnPauseRequested);
        _pauseScreen.Initialize(OnSubstitutionApplied, OnSubstitutionSkipped);
        _postGameScreen.Initialize(OnReturnToLeague);
        _postSeasonScreen.Initialize(ShowLobby, ShowToast);

        _inventoryScreen.Initialize(ShowLobby, OpenCardDetail, ShowToast);
        _gachaScreen.Initialize(ShowLobby, OpenCardDetail, ShowToast);
        _lineUpScreen.Initialize(ShowLobby, OpenCardPicker, ShowToast);
        _enhanceScreen.Initialize(ShowLobby, OpenCardPicker, ShowToast, OpenCardDetail);
        _trainScreen.Initialize(ShowLobby, OpenCardPicker, ShowToast, OpenCardDetail);
        _combineScreen.Initialize(ShowLobby, OpenCardPicker, ShowToast, OpenCardDetail);
        _dismantleScreen.Initialize(ShowLobby, OpenCardPicker, ShowToast, OpenCardDetail);
        _goldenGloveCraftScreen.Initialize(ShowLobby, OpenCardPicker, ShowToast, OpenCardDetail);

        _cardPickerScreen.Initialize(CloseCardPicker);
        _cardDetailScreen.Initialize(CloseCardDetail, ShowToast);
    }

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

    private void ShowScreen(UiScreenBehaviour screen)
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
