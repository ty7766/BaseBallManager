using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ① 경기 전 화면. 대진 · 상대전적 · 선발 투수 · 양 팀 타순을 보여주고 경기를 시작한다 (명세 ① · 중계 그래픽 B안).
/// 대진 판은 양 끝에 팀 색 면과 반쯤 걸친 엠블럼, 가운데 VS 양옆에 로고를 둔다. 팀 이름은 판 아래 귀퉁이에 선다
/// </summary>
public class PreGameScreen : UiScreenBehaviour
{
    private const float SideMargin = 24f;
    private const float PanelWidth = 1032f;
    private const float VersusTop = 120f;
    private const float VersusHeight = 250f;
    private const float LogoSize = 124f;
    private const float StarterTop = 404f;
    private const float StarterHeight = 196f;
    private const float StarterCardWidth = 124f;
    private const float StarterCardLift = 22f;
    private const float StarterCardTilt = 3f;
    private const float StarterTextInset = 184f;
    private const float LineupTop = 628f;
    private const float LineupGap = 16f;
    private const float CtaHeight = 160f;
    private const float CtaBottom = 36f;
    private const float CtaFontSize = 50f;
    private const string CtaIcon = "league";

    private Action _onStart;

    [SerializeField] private TextMeshProUGUI _tierText;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private Image _awayField;
    [SerializeField] private Image _homeField;
    [SerializeField] private Image _awayEdgeEmblem;
    [SerializeField] private Image _homeEdgeEmblem;
    [SerializeField] private TeamEmblemView _awayLogo;
    [SerializeField] private TeamEmblemView _homeLogo;
    [SerializeField] private TextMeshProUGUI _awayNameText;
    [SerializeField] private TextMeshProUGUI _homeNameText;
    [SerializeField] private TextMeshProUGUI _awayMyTeamTag;
    [SerializeField] private TextMeshProUGUI _homeMyTeamTag;
    [SerializeField] private TextMeshProUGUI _headToHeadText;
    [SerializeField] private CardView _awayStarterCard;
    [SerializeField] private CardView _homeStarterCard;
    [SerializeField] private TextMeshProUGUI _awayStarterName;
    [SerializeField] private TextMeshProUGUI _homeStarterName;
    [SerializeField] private TextMeshProUGUI _awayStarterRole;
    [SerializeField] private TextMeshProUGUI _homeStarterRole;
    [SerializeField] private TextMeshProUGUI _awayStarterOvr;
    [SerializeField] private TextMeshProUGUI _homeStarterOvr;
    [SerializeField] private MatchLineupColumnView _awayColumn;
    [SerializeField] private MatchLineupColumnView _homeColumn;
    [SerializeField] private LobbyMenuTileView _startTile;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onStart)
    {
        _onStart = onStart;
    }

    /// <summary>
    /// 오늘 경기 정보를 채운다. 경기 시작 직전에 호출한다
    /// </summary>
    public void SetGame(LeagueSeason season, LeagueGame game, SimulationContext context, string playerTeamName)
    {
        if (season == null || context == null)
        {
            Debug.LogError("[PreGameScreen]: 시즌 또는 시뮬 입력이 없어 화면을 채울 수 없습니다");
            return;
        }

        _tierText.text = $"{LeagueTierNames.Get(season.Tier)} 리그";
        _progressText.text = $"{season.CurrentDayIndex + 1} / {season.TotalDayCount}";

        bool isPlayerHome = game.HomeTeamName == playerTeamName;

        SetTeams(game.AwayTeamName, game.HomeTeamName, isPlayerHome);
        _headToHeadText.text = BuildHeadToHeadText(season, game, playerTeamName);

        SetStarter(_awayStarterCard, _awayStarterName, _awayStarterRole, _awayStarterOvr,
            context.AwayPitchers[SimulationContext.StartingPitcherSlot], !isPlayerHome);
        SetStarter(_homeStarterCard, _homeStarterName, _homeStarterRole, _homeStarterOvr,
            context.HomePitchers[SimulationContext.StartingPitcherSlot], isPlayerHome);

        _awayColumn.Fill(game.AwayTeamName, context.AwayLineup, !isPlayerHome);
        _homeColumn.Fill(game.HomeTeamName, context.HomeLineup, isPlayerHome);

        PlayEntranceEffect();
    }

    protected override void BindEvents()
    {
        Bind(_startTile.Button, OnStartClicked);
    }

    protected override void Build()
    {
        _tierText = UiBuilder.CreateText(Root, "Tier", 48f, UiPalette.Text1);
        UiBuilder.Place(_tierText.rectTransform, 32f, 36f, 600f, 56f);
        _tierText.fontStyle = FontStyles.Bold;

        _progressText = BroadcastPanel.CreateNumber(Root, "Progress", 34f, UiPalette.Text2, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_progressText.rectTransform, 32f, 44f, 300f, 44f);

        BuildVersusPanel();
        BuildStarterPanel();

        float columnWidth = (PanelWidth - LineupGap) * 0.5f;
        float lineupBottom = CtaBottom + CtaHeight + SideMargin;

        _awayColumn = MatchLineupColumnView.Bake(Root, "AwayLineup", "원정 타순", SideMargin, columnWidth, LineupTop, lineupBottom);
        _homeColumn = MatchLineupColumnView.Bake(Root, "HomeLineup", "홈 타순", SideMargin + columnWidth + LineupGap, columnWidth, LineupTop, lineupBottom);

        _startTile = LobbyMenuTileView.Create(Root, "StartButton", "경기 시작", CtaIcon,
            new Vector2(PanelWidth, CtaHeight), CtaFontSize, true, OnStartClicked, true);

        RectTransform startRect = _startTile.Root;

        startRect.anchorMin = startRect.anchorMax = new Vector2(0.5f, 0f);
        startRect.pivot = new Vector2(0.5f, 0f);
        startRect.anchoredPosition = new Vector2(0f, CtaBottom);
    }

    //대진 판: 팀 색 면 · 걸친 엠블럼 · 로고 VS 로고 · 상대전적 · 아래 귀퉁이 팀 이름
    private void BuildVersusPanel()
    {
        Image panel = BroadcastPanel.Create(Root, "VersusPanel", true);
        RectTransform rect = panel.rectTransform;

        UiBuilder.Place(rect, SideMargin, VersusTop, PanelWidth, VersusHeight);

        _awayField = BroadcastPanel.CreateTeamField(rect, "AwayField", true, 0.5f);
        _homeField = BroadcastPanel.CreateTeamField(rect, "HomeField", false, 0.5f);
        _awayEdgeEmblem = BroadcastPanel.CreateEdgeEmblem(rect, "AwayEdgeEmblem");
        _homeEdgeEmblem = BroadcastPanel.CreateEdgeEmblem(rect, "HomeEdgeEmblem");

        float versusWidth = 120f;
        float versusLeft = (PanelWidth - versusWidth) * 0.5f;

        TextMeshProUGUI versus = BroadcastPanel.CreateNumber(rect, "Versus", 64f, UiPalette.Text3);
        UiBuilder.Place(versus.rectTransform, versusLeft, 54f, versusWidth, 80f);
        versus.text = "VS";

        _awayLogo = TeamEmblemView.Bake(rect, LogoSize);
        UiBuilder.Place(_awayLogo.Root, versusLeft - 28f - LogoSize, 32f, LogoSize, LogoSize);

        _homeLogo = TeamEmblemView.Bake(rect, LogoSize);
        UiBuilder.Place(_homeLogo.Root, versusLeft + versusWidth + 28f, 32f, LogoSize, LogoSize);

        _headToHeadText = UiBuilder.CreateText(rect, "HeadToHead", 28f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Place(_headToHeadText.rectTransform, (PanelWidth - 400f) * 0.5f, 176f, 400f, 40f);

        TextMeshProUGUI awayTag = UiBuilder.CreateLabel(rect, "AwayTag", "AWAY", 24f, UiPalette.Text2);
        UiBuilder.Place(awayTag.rectTransform, 36f, 136f, 110f, 30f);

        _awayMyTeamTag = UiBuilder.CreateLabel(rect, "AwayMyTeam", "MY TEAM", 24f, UiPalette.AccentGold);
        UiBuilder.Place(_awayMyTeamTag.rectTransform, 140f, 136f, 170f, 30f);

        _awayNameText = UiBuilder.CreateText(rect, "AwayName", 52f, UiPalette.Text1);
        UiBuilder.Place(_awayNameText.rectTransform, 36f, 168f, 280f, 60f);
        _awayNameText.fontStyle = FontStyles.Bold;

        TextMeshProUGUI homeTag = UiBuilder.CreateLabel(rect, "HomeTag", "HOME", 24f, UiPalette.Text2, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(homeTag.rectTransform, 36f, 136f, 110f, 30f);

        _homeMyTeamTag = UiBuilder.CreateLabel(rect, "HomeMyTeam", "MY TEAM", 24f, UiPalette.AccentGold, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_homeMyTeamTag.rectTransform, 140f, 136f, 170f, 30f);

        _homeNameText = UiBuilder.CreateText(rect, "HomeName", 52f, UiPalette.Text1, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_homeNameText.rectTransform, 36f, 168f, 280f, 60f);
        _homeNameText.fontStyle = FontStyles.Bold;
    }

    //선발 판: 가운데 "선발 투수" 한 줄, 양쪽 끝에 판 위로 걸친 카드 + 이름 · 보직 · OVR
    private void BuildStarterPanel()
    {
        Image panel = BroadcastPanel.Create(Root, "StarterPanel");
        RectTransform rect = panel.rectTransform;

        UiBuilder.Place(rect, SideMargin, StarterTop, PanelWidth, StarterHeight);

        TextMeshProUGUI label = UiBuilder.CreateText(rect, "StarterLabel", 30f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Place(label.rectTransform, (PanelWidth - 300f) * 0.5f, 0f, 300f, StarterHeight);
        label.text = "선발 투수";
        label.characterSpacing = 4f;

        _awayStarterCard = CreateStarterCard("AwayStarterCard", SideMargin + 32f, -StarterCardTilt);
        _homeStarterCard = CreateStarterCard("HomeStarterCard", SideMargin + PanelWidth - 32f - StarterCardWidth, StarterCardTilt);

        _awayStarterName = UiBuilder.CreateText(rect, "AwayStarterName", 40f, UiPalette.Text1);
        UiBuilder.Place(_awayStarterName.rectTransform, StarterTextInset, 30f, 300f, 50f);

        _awayStarterRole = UiBuilder.CreateText(rect, "AwayStarterRole", 26f, UiPalette.Text2);
        UiBuilder.Place(_awayStarterRole.rectTransform, StarterTextInset, 82f, 160f, 32f);

        _awayStarterOvr = BroadcastPanel.CreateNumber(rect, "AwayStarterOvr", 66f, UiPalette.Text1, TextAlignmentOptions.MidlineLeft);
        UiBuilder.Place(_awayStarterOvr.rectTransform, StarterTextInset, 112f, 160f, 72f);

        _homeStarterName = UiBuilder.CreateText(rect, "HomeStarterName", 40f, UiPalette.Text1, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_homeStarterName.rectTransform, StarterTextInset, 30f, 300f, 50f);

        _homeStarterRole = UiBuilder.CreateText(rect, "HomeStarterRole", 26f, UiPalette.Text2, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_homeStarterRole.rectTransform, StarterTextInset, 82f, 160f, 32f);

        _homeStarterOvr = BroadcastPanel.CreateNumber(rect, "HomeStarterOvr", 66f, UiPalette.Text1, TextAlignmentOptions.MidlineRight);
        UiBuilder.PlaceRight(_homeStarterOvr.rectTransform, StarterTextInset, 112f, 160f, 72f);
    }

    //판 위쪽으로 살짝 걸치고 바깥쪽으로 기운 카드. 판 밖이라 판 모양에 잘리지 않는다
    private CardView CreateStarterCard(string name, float x, float tilt)
    {
        CardView card = CardView.Bake(Root, StarterCardWidth, false);
        RectTransform rect = card.Root;

        rect.name = name;
        UiBuilder.Place(rect, x, StarterTop - StarterCardLift, StarterCardWidth, rect.sizeDelta.y);
        UiBuilder.CenterPivot(rect);
        rect.localEulerAngles = new Vector3(0f, 0f, tilt);

        return card;
    }

    private void SetTeams(string awayTeamName, string homeTeamName, bool isPlayerHome)
    {
        _awayNameText.text = awayTeamName;
        _homeNameText.text = homeTeamName;
        _awayLogo.SetTeam(awayTeamName);
        _homeLogo.SetTeam(homeTeamName);
        _awayMyTeamTag.gameObject.SetActive(!isPlayerHome);
        _homeMyTeamTag.gameObject.SetActive(isPlayerHome);

        BroadcastPanel.PaintTeamField(_awayField, awayTeamName);
        BroadcastPanel.PaintTeamField(_homeField, homeTeamName);
        BroadcastPanel.PlaceEdgeEmblem(_awayEdgeEmblem, awayTeamName, true);
        BroadcastPanel.PlaceEdgeEmblem(_homeEdgeEmblem, homeTeamName, false);
    }

    private static void SetStarter(CardView card, TextMeshProUGUI nameText, TextMeshProUGUI roleText,
        TextMeshProUGUI ovrText, PitcherSnapshot pitcher, bool isPlayerCard)
    {
        nameText.text = $"<b>{pitcher.Name}</b><size=74%> '{PlayerLabelFormat.Year(pitcher.Year)}</size>";
        roleText.text = pitcher.Position;
        ovrText.text = pitcher.OVR.ToString();
        ovrText.color = UiPalette.Text1;

        if (!CardDisplayInfo.TryCreateForSnapshot(pitcher.InstanceId, isPlayerCard, out CardDisplayInfo info))
        {
            card.SetEmpty(string.Empty);
            return;
        }

        card.SetCard(info);

        if (isPlayerCard)
            ovrText.color = CardVisualLookup.GetTypeColor(info.CardType);
    }

    //양 팀 로고가 튀어오르고 선발 카드 · 타순이 위에서부터 차례로 뜬다
    private void PlayEntranceEffect()
    {
        UiEffects.Pop(_awayLogo.Root, 0.6f, 0.34f);
        UiEffects.Pop(_homeLogo.Root, 0.6f, 0.34f, 0.08f);
        UiEffects.Pop(_awayStarterCard.Root, 0.82f, 0.3f, 0.12f);
        UiEffects.Pop(_homeStarterCard.Root, 0.82f, 0.3f, 0.16f);
        _awayColumn.PlayEntrance(0.18f);
        _homeColumn.PlayEntrance(0.18f);
    }

    //이번 리그에서 이 상대에게 거둔 전적
    private static string BuildHeadToHeadText(LeagueSeason season, LeagueGame game, string playerTeamName)
    {
        TeamRecord record = season.Standings.GetRecord(playerTeamName);

        if (record == null)
            return string.Empty;

        string opponent = game.GetOpponent(playerTeamName);

        if (string.IsNullOrEmpty(opponent))
            return string.Empty;

        return $"상대전적 {record.GetWinsAgainst(opponent)}승 {record.GetLossesAgainst(opponent)}패";
    }

    private void OnStartClicked()
    {
        _onStart?.Invoke();
    }
}
