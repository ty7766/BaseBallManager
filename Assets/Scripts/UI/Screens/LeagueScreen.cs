using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 리그 화면. 다음 경기 · 순위표 · 교체 모드를 보여주고 경기로 넘긴다 (명세 5장 · 중계 그래픽 B안).
/// 버튼은 로비와 같은 아이콘 칸으로 아래에 모으고, 경기 시작(시즌 보상)은 맨 아래 한 줄을 통째로 쓴다
/// </summary>
public class LeagueScreen : UiScreenBehaviour
{
    private const float SideMargin = 24f;
    private const float PanelWidth = 1032f;
    private const float Gap = 16f;
    private const float BottomMargin = 40f;
    private const float StartHeight = 168f;
    private const float StartFontSize = 46f;
    private const float MenuHeight = 150f;
    private const float MenuFontSize = 36f;
    private const float ModeHeight = 120f;
    private const float NextGameTop = 120f;
    private const float NextGameHeight = 196f;
    private const float NextGameLogoSize = 112f;
    private const float NextGameTextLeft = 166f;
    private const float StandingsTop = 340f;
    private const float OpponentFieldRatio = 0.55f;
    private const float SwitchWidth = 400f;
    private const float SwitchHeight = 84f;

    private Action _onStartGame;
    private Action _onToggleMode;
    private Action _onClaimRewards;
    private Action _onBackToLobby;
    private Action _onOpenPostSeason;

    [SerializeField] private TextMeshProUGUI _tierText;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private Image _opponentField;
    [SerializeField] private Image _opponentEdgeEmblem;
    [SerializeField] private TeamEmblemView _opponentLogo;
    [SerializeField] private TextMeshProUGUI _nextLabelText;
    [SerializeField] private TextMeshProUGUI _nextGameText;
    [SerializeField] private TextMeshProUGUI _noticeText;
    [SerializeField] private StandingsTableView _standings;
    [SerializeField] private ModeSwitchView _modeSwitch;
    [SerializeField] private TextMeshProUGUI _modeHintText;
    [SerializeField] private LobbyMenuTileView _startTile;
    [SerializeField] private LobbyMenuTileView _claimTile;
    [SerializeField] private LobbyMenuTileView _lobbyTile;
    [SerializeField] private LobbyMenuTileView _postSeasonTile;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onStartGame, Action onToggleMode, Action onClaimRewards,
        Action onBackToLobby, Action onOpenPostSeason)
    {
        _onStartGame = onStartGame;
        _onToggleMode = onToggleMode;
        _onClaimRewards = onClaimRewards;
        _onBackToLobby = onBackToLobby;
        _onOpenPostSeason = onOpenPostSeason;
    }

    /// <summary>
    /// 다음 경기 · 순위표 · 모드를 현재 상태로 갱신한다
    /// </summary>
    public void Refresh(LeagueSeason season, LeagueStandingRow[] ranking, LeagueGame? nextGame,
        string playerTeamName, SubstitutionMode substitutionMode, bool hasPostSeason)
    {
        _postSeasonTile.SetInteractable(hasPostSeason);
        RefreshMode(substitutionMode);

        if (season == null)
        {
            ShowNextGameMessage("리그", "진행 중인 리그가 없습니다", string.Empty);
            _startTile.SetVisible(true);
            _claimTile.SetVisible(false);
            _startTile.SetInteractable(false);
            return;
        }

        _tierText.text = $"{LeagueTierNames.Get(season.Tier)} 리그";
        _progressText.text = $"{season.CurrentDayIndex} / {season.TotalDayCount} 경기";

        bool finished = season.IsFinished;

        _startTile.SetVisible(!finished);
        _startTile.SetInteractable(true);
        _claimTile.SetVisible(finished);

        if (finished)
        {
            string notice = season.RewardsGranted
                ? "보상을 받았습니다. 다음 리그로 넘어갈 수 있습니다"
                : "시즌 보상을 받으세요";

            ShowNextGameMessage("이번 시즌", "시즌 종료", notice);
        }
        else if (nextGame.HasValue)
        {
            ShowNextGame(nextGame.Value, playerTeamName);
        }

        _standings.Fill(ranking, playerTeamName);
    }

    protected override void BindEvents()
    {
        Bind(_startTile.Button, OnStartClicked);
        Bind(_claimTile.Button, OnClaimClicked);
        Bind(_lobbyTile.Button, OnBackClicked);
        Bind(_postSeasonTile.Button, OnPostSeasonClicked);
        _modeSwitch.SetToggleHandler(OnToggleModeClicked);
    }

    protected override void Build()
    {
        _tierText = UiBuilder.CreateText(Root, "Tier", 48f, UiPalette.Text1);
        UiBuilder.Place(_tierText.rectTransform, 32f, 36f, 600f, 56f);
        _tierText.fontStyle = FontStyles.Bold;

        _progressText = BroadcastPanel.CreateNumber(Root, "Progress", 34f, UiPalette.Text2, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_progressText.rectTransform, 32f, 44f, 360f, 44f);

        BuildNextGamePanel();

        float startBottom = BottomMargin;
        float menuBottom = startBottom + StartHeight + Gap;
        float modeBottom = menuBottom + MenuHeight + Gap;
        float standingsBottom = modeBottom + ModeHeight + Gap;

        _standings = StandingsTableView.Bake(Root, PanelWidth);
        UiBuilder.Stretch(_standings.Root, SideMargin, StandingsTop, SideMargin, standingsBottom);

        BuildModePanel(modeBottom);

        float menuWidth = (PanelWidth - Gap) / 2f;

        _lobbyTile = CreateTile("LobbyButton", "로비로", "lobby", 0, menuWidth, menuBottom, OnBackClicked);
        _postSeasonTile = CreateTile("PostSeasonButton", "포스트시즌", "postseason", 1, menuWidth, menuBottom, OnPostSeasonClicked);

        _startTile = LobbyMenuTileView.Create(Root, "StartButton", "경기 시작", "league",
            new Vector2(PanelWidth, StartHeight), StartFontSize, true, OnStartClicked, true);
        PlaceBottom(_startTile.Root, SideMargin, startBottom, PanelWidth, StartHeight);

        _claimTile = LobbyMenuTileView.Create(Root, "ClaimButton", "시즌 보상 받기", "goldenglove",
            new Vector2(PanelWidth, StartHeight), StartFontSize, true, OnClaimClicked, true);
        PlaceBottom(_claimTile.Root, SideMargin, startBottom, PanelWidth, StartHeight);
        _claimTile.SetVisible(false);
    }

    //다음 경기 판: 상대 팀 색 면 · 걸친 엠블럼 · 로고 + "다음 경기" · 상대 · 안내
    private void BuildNextGamePanel()
    {
        Image panel = BroadcastPanel.Create(Root, "NextGamePanel", true);
        RectTransform rect = panel.rectTransform;

        UiBuilder.Place(rect, SideMargin, NextGameTop, PanelWidth, NextGameHeight);

        _opponentField = BroadcastPanel.CreateTeamField(rect, "OpponentField", false, OpponentFieldRatio);
        _opponentEdgeEmblem = BroadcastPanel.CreateEdgeEmblem(rect, "OpponentEdgeEmblem");

        _opponentLogo = TeamEmblemView.Bake(rect, NextGameLogoSize);
        UiBuilder.PlaceMiddle(_opponentLogo.Root, 30f, 0f, NextGameLogoSize, NextGameLogoSize);

        _nextLabelText = UiBuilder.CreateText(rect, "NextLabel", 26f, UiPalette.Text2);
        UiBuilder.Place(_nextLabelText.rectTransform, NextGameTextLeft, 30f, 400f, 34f);
        _nextLabelText.characterSpacing = 2f;

        _nextGameText = UiBuilder.CreateText(rect, "NextGame", 48f, UiPalette.Text1);
        UiBuilder.Place(_nextGameText.rectTransform, NextGameTextLeft, 66f, 600f, 60f);
        _nextGameText.fontStyle = FontStyles.Bold;

        _noticeText = UiBuilder.CreateText(rect, "Notice", 28f, UiPalette.Text2);
        UiBuilder.Place(_noticeText.rectTransform, NextGameTextLeft, 132f, 700f, 40f);
    }

    //교체 모드 판: 왼쪽 "교체 모드" + 설명, 오른쪽 수동 · 자동 슬라이드 스위치
    private void BuildModePanel(float bottom)
    {
        Image panel = BroadcastPanel.Create(Root, "ModePanel");
        PlaceBottom(panel.rectTransform, SideMargin, bottom, PanelWidth, ModeHeight);

        TextMeshProUGUI title = UiBuilder.CreateText(panel.transform, "ModeTitle", 36f, UiPalette.Text1);
        UiBuilder.PlaceMiddle(title.rectTransform, 32f, 18f, 400f, 46f);
        title.text = "교체 모드";
        title.fontStyle = FontStyles.Bold;

        _modeHintText = UiBuilder.CreateText(panel.transform, "ModeHint", 24f, UiPalette.Text3);
        UiBuilder.PlaceMiddle(_modeHintText.rectTransform, 32f, -24f, 560f, 32f);

        _modeSwitch = ModeSwitchView.Bake(panel.transform, SwitchWidth, SwitchHeight, 32f);
        UiBuilder.PlaceMiddleRight(_modeSwitch.Root, 18f, 0f, SwitchWidth, SwitchHeight);
    }

    private LobbyMenuTileView CreateTile(string name, string label, string icon, int column, float width,
        float bottom, UnityEngine.Events.UnityAction onClick)
    {
        LobbyMenuTileView tile = LobbyMenuTileView.Create(Root, name, label, icon,
            new Vector2(width, MenuHeight), MenuFontSize, false, onClick, true);

        PlaceBottom(tile.Root, SideMargin + column * (width + Gap), bottom, width, MenuHeight);

        return tile;
    }

    //화면 아래 기준 배치 (세로가 긴 기기에서도 버튼은 아래에 붙는다)
    private static void PlaceBottom(RectTransform rect, float x, float bottom, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(x, bottom);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void ShowNextGame(LeagueGame game, string playerTeamName)
    {
        bool isPlayerHome = game.HomeTeamName == playerTeamName;
        string opponent = game.GetOpponent(playerTeamName);

        _nextLabelText.text = "다음 경기";
        _nextGameText.text = $"vs {opponent}";
        _noticeText.text = isPlayerHome ? "홈 경기" : "원정 경기";

        _opponentLogo.SetVisible(true);
        _opponentLogo.SetTeam(opponent);
        BroadcastPanel.PaintTeamField(_opponentField, opponent);
        BroadcastPanel.PlaceEdgeEmblem(_opponentEdgeEmblem, opponent, false);
    }

    //상대가 없는 상태(시즌 종료 · 리그 없음): 로고 · 팀 색 없이 글자만
    private void ShowNextGameMessage(string label, string title, string notice)
    {
        _nextLabelText.text = label;
        _nextGameText.text = title;
        _noticeText.text = notice;

        _opponentLogo.SetVisible(false);
        _opponentField.color = Color.clear;
        _opponentEdgeEmblem.enabled = false;
    }

    private void RefreshMode(SubstitutionMode substitutionMode)
    {
        bool isManual = substitutionMode == SubstitutionMode.Manual;

        _modeSwitch.SetMode(isManual);
        _modeHintText.text = isManual
            ? "자동 교체에 더해 경기 중 일시정지로 직접 교체"
            : "자동 교체만 · 경기 중에는 멈출 수 없음";
    }

    private void OnStartClicked()
    {
        _onStartGame?.Invoke();
    }

    private void OnToggleModeClicked()
    {
        _onToggleMode?.Invoke();
    }

    private void OnClaimClicked()
    {
        _onClaimRewards?.Invoke();
    }

    private void OnBackClicked()
    {
        _onBackToLobby?.Invoke();
    }

    private void OnPostSeasonClicked()
    {
        _onOpenPostSeason?.Invoke();
    }
}
