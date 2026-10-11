using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 리그 화면. 순위표 · 다음 경기 · 교체 모드를 보여주고 경기로 넘긴다 (명세 5장)
/// </summary>
public class LeagueScreen : UiScreen
{
    private static readonly string[] StandingHeaders = { "승", "패", "무", "승률", "게임차" };

    private readonly Action _onStartGame;
    private readonly Action _onToggleMode;
    private readonly Action _onSave;
    private readonly Action _onClaimRewards;
    private readonly Action _onBackToLobby;
    private readonly Action _onOpenPostSeason;

    private TextMeshProUGUI _tierText;
    private TextMeshProUGUI _progressText;
    private TextMeshProUGUI _nextGameText;
    private TextMeshProUGUI _modeText;
    private TextMeshProUGUI _noticeText;
    private StatTableView _standingsTable;
    private Button _startButton;
    private Button _claimButton;
    private Button _modeButton;
    private Button _postSeasonButton;

    public LeagueScreen(Action onStartGame, Action onToggleMode, Action onSave, Action onClaimRewards,
        Action onBackToLobby, Action onOpenPostSeason)
    {
        _onStartGame = onStartGame;
        _onToggleMode = onToggleMode;
        _onSave = onSave;
        _onClaimRewards = onClaimRewards;
        _onBackToLobby = onBackToLobby;
        _onOpenPostSeason = onOpenPostSeason;
    }

    protected override string ScreenName => "LeagueScreen";

    /// <summary>
    /// 순위표·다음 경기·모드를 현재 상태로 갱신한다
    /// </summary>
    public void Refresh(LeagueSeason season, LeagueStandingRow[] ranking, LeagueGame? nextGame,
        string playerTeamName, SubstitutionMode substitutionMode, bool hasPostSeason)
    {
        _postSeasonButton.gameObject.SetActive(hasPostSeason);

        if (season == null)
        {
            _noticeText.text = "진행 중인 리그가 없습니다";
            return;
        }

        _tierText.text = $"{LeagueTierNames.Get(season.Tier)} 리그";
        _progressText.text = $"{season.CurrentDayIndex} / {season.TotalDayCount} 경기";
        _modeText.text = substitutionMode == SubstitutionMode.Manual ? "수동 교체" : "자동 교체";

        bool finished = season.IsFinished;

        _startButton.gameObject.SetActive(!finished);
        _claimButton.gameObject.SetActive(finished);

        if (finished)
        {
            _nextGameText.text = "시즌 종료";
            _noticeText.text = season.RewardsGranted
                ? "보상을 받았습니다. 다음 리그로 넘어갈 수 있습니다"
                : "시즌 보상을 받으세요";
        }
        else if (nextGame.HasValue)
        {
            LeagueGame game = nextGame.Value;
            bool isPlayerHome = game.HomeTeamName == playerTeamName;
            string opponent = game.GetOpponent(playerTeamName);

            _nextGameText.text = $"다음 경기  vs {opponent}  ({(isPlayerHome ? "홈" : "원정")})";
            _noticeText.text = string.Empty;
        }

        FillStandings(ranking, playerTeamName);
    }

    protected override void Build()
    {
        _tierText = UiBuilder.CreateText(Root, "Tier", 48f, UiPalette.Text1);
        UiBuilder.Place(_tierText.rectTransform, 32f, 36f, 600f, 56f);

        _progressText = UiBuilder.CreateText(Root, "Progress", 34f, UiPalette.Text2, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_progressText.rectTransform, 32f, 44f, 360f, 44f);

        _nextGameText = UiBuilder.CreateText(Root, "NextGame", 40f, UiPalette.AccentGold);
        UiBuilder.Place(_nextGameText.rectTransform, 32f, 108f, 1016f, 52f);

        _noticeText = UiBuilder.CreateText(Root, "Notice", 30f, UiPalette.Text3);
        UiBuilder.Place(_noticeText.rectTransform, 32f, 164f, 1016f, 40f);

        TextMeshProUGUI standingsTitle = UiBuilder.CreateText(Root, "StandingsTitle", 34f, UiPalette.Text3);
        UiBuilder.Place(standingsTitle.rectTransform, 32f, 216f, 400f, 44f);
        standingsTitle.text = "순위표";

        _standingsTable = new StatTableView(Root, StandingHeaders, 280f, 120f, "팀");
        UiBuilder.Stretch(_standingsTable.Root, 24f, 268f, 24f, 320f);

        _modeButton = UiBuilder.CreateButton(Root, "ModeButton", "교체 모드 전환", 32f, false, OnToggleModeClicked);
        RectTransform modeRect = _modeButton.GetComponent<RectTransform>();

        modeRect.anchorMin = new Vector2(0f, 0f);
        modeRect.anchorMax = new Vector2(0.5f, 0f);
        modeRect.pivot = new Vector2(0f, 0f);
        modeRect.offsetMin = new Vector2(24f, 180f);
        modeRect.offsetMax = new Vector2(-12f, 264f);

        _modeText = UiBuilder.CreateText(Root, "Mode", 32f, UiPalette.Text1, TextAlignmentOptions.Center);
        RectTransform modeTextRect = _modeText.rectTransform;

        modeTextRect.anchorMin = new Vector2(0.5f, 0f);
        modeTextRect.anchorMax = new Vector2(1f, 0f);
        modeTextRect.pivot = new Vector2(0f, 0f);
        modeTextRect.offsetMin = new Vector2(12f, 180f);
        modeTextRect.offsetMax = new Vector2(-24f, 264f);

        Button saveButton = UiBuilder.CreateButton(Root, "SaveButton", "저장", 32f, false, OnSaveClicked);
        RectTransform saveRect = saveButton.GetComponent<RectTransform>();

        saveRect.anchorMin = new Vector2(0.5f, 0f);
        saveRect.anchorMax = new Vector2(1f, 0f);
        saveRect.pivot = new Vector2(0f, 0f);
        saveRect.offsetMin = new Vector2(12f, 92f);
        saveRect.offsetMax = new Vector2(-24f, 168f);

        _startButton = UiBuilder.CreateButton(Root, "StartButton", "경기 시작", 42f, true, OnStartClicked);
        RectTransform startRect = _startButton.GetComponent<RectTransform>();

        startRect.anchorMin = new Vector2(0f, 0f);
        startRect.anchorMax = new Vector2(0.5f, 0f);
        startRect.pivot = new Vector2(0f, 0f);
        startRect.offsetMin = new Vector2(24f, 92f);
        startRect.offsetMax = new Vector2(-12f, 168f);

        _postSeasonButton = UiBuilder.CreateButton(Root, "PostSeasonButton", "가을야구", 32f, false,
            OnPostSeasonClicked);
        RectTransform postSeasonRect = _postSeasonButton.GetComponent<RectTransform>();

        postSeasonRect.anchorMin = new Vector2(0f, 0f);
        postSeasonRect.anchorMax = new Vector2(0.5f, 0f);
        postSeasonRect.pivot = new Vector2(0f, 0f);
        postSeasonRect.offsetMin = new Vector2(24f, 276f);
        postSeasonRect.offsetMax = new Vector2(-12f, 352f);
        _postSeasonButton.gameObject.SetActive(false);

        Button lobbyButton = UiBuilder.CreateButton(Root, "LobbyButton", "로비로", 32f, false, OnBackClicked);
        RectTransform lobbyRect = lobbyButton.GetComponent<RectTransform>();

        lobbyRect.anchorMin = new Vector2(0.5f, 0f);
        lobbyRect.anchorMax = new Vector2(1f, 0f);
        lobbyRect.pivot = new Vector2(0f, 0f);
        lobbyRect.offsetMin = new Vector2(12f, 276f);
        lobbyRect.offsetMax = new Vector2(-24f, 352f);

        _claimButton = UiBuilder.CreateButton(Root, "ClaimButton", "시즌 보상 받기", 38f, true, OnClaimClicked);
        RectTransform claimRect = _claimButton.GetComponent<RectTransform>();

        claimRect.anchorMin = new Vector2(0f, 0f);
        claimRect.anchorMax = new Vector2(0.5f, 0f);
        claimRect.pivot = new Vector2(0f, 0f);
        claimRect.offsetMin = new Vector2(24f, 92f);
        claimRect.offsetMax = new Vector2(-12f, 168f);
        _claimButton.gameObject.SetActive(false);
    }

    //순위표 행을 "순위 팀명" + 숫자 5열로 채운다
    private void FillStandings(LeagueStandingRow[] ranking, string playerTeamName)
    {
        if (ranking == null)
            return;

        List<string> names = new List<string>(ranking.Length);
        List<string[]> cells = new List<string[]>(ranking.Length);

        foreach (LeagueStandingRow row in ranking)
        {
            TeamRecord record = row.Record;
            string marker = record.TeamName == playerTeamName ? " ◀" : string.Empty;

            names.Add($"{row.Rank}  {record.TeamName}{marker}");
            cells.Add(new[]
            {
                record.Wins.ToString(),
                record.Losses.ToString(),
                record.Draws.ToString(),
                record.WinRate.ToString("0.000"),
                row.GamesBehind.ToString("0.0")
            });
        }

        _standingsTable.SetRows(names, cells);
    }

    private void OnStartClicked()
    {
        _onStartGame?.Invoke();
    }

    private void OnToggleModeClicked()
    {
        _onToggleMode?.Invoke();
    }

    private void OnSaveClicked()
    {
        _onSave?.Invoke();
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
