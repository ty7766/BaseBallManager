using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 포스트시즌 사다리. 시리즈를 한 경기씩 진행하고 보상을 받는다
/// </summary>
public class PostSeasonScreen : UiScreen
{
    private const int MaxSeriesRows = 4;
    private const float RowHeight = 140f;
    private const float RowGap = 12f;

    private readonly Action _onBack;
    private readonly Action<string, bool> _onNotify;

    private TextMeshProUGUI _stateText;
    private TextMeshProUGUI _championText;
    private readonly TextMeshProUGUI[] _seriesTexts = new TextMeshProUGUI[MaxSeriesRows];
    private readonly Image[] _seriesBackgrounds = new Image[MaxSeriesRows];
    private Button _nextGameButton;
    private Button _claimButton;

    public PostSeasonScreen(Action onBack, Action<string, bool> onNotify)
    {
        _onBack = onBack;
        _onNotify = onNotify;
    }

    protected override string ScreenName => "PostSeasonScreen";

    /// <summary>
    /// 대진과 진행 상황을 다시 읽는다
    /// </summary>
    public void Refresh()
    {
        PostSeasonRunner postSeason = LeagueManager.Instance == null ? null : LeagueManager.Instance.PostSeason;

        if (postSeason == null)
        {
            _stateText.text = "진행 중인 포스트시즌이 없습니다";
            _championText.text = string.Empty;
            _nextGameButton.interactable = false;
            _claimButton.gameObject.SetActive(false);

            for (int i = 0; i < MaxSeriesRows; i++)
                _seriesBackgrounds[i].gameObject.SetActive(false);

            return;
        }

        string playerTeamName = PlayerDataManager.Instance == null
            ? string.Empty
            : PlayerDataManager.Instance.PlayerTeamName;

        _stateText.text = postSeason.IsFinished
            ? "가을야구 종료"
            : $"{GetRoundName(postSeason.CurrentSeries.Round)} 진행 중";

        _championText.text = postSeason.IsFinished && !string.IsNullOrEmpty(postSeason.ChampionTeamName)
            ? $"우승  {postSeason.ChampionTeamName}"
            : string.Empty;
        _championText.color = postSeason.ChampionTeamName == playerTeamName
            ? UiPalette.AccentGold
            : UiPalette.Text2;

        for (int i = 0; i < MaxSeriesRows; i++)
        {
            bool used = i < postSeason.Series.Count;

            _seriesBackgrounds[i].gameObject.SetActive(used);

            if (!used)
                continue;

            PostSeasonSeries series = postSeason.Series[i];
            bool isCurrent = !postSeason.IsFinished && i == postSeason.CurrentSeriesIndex;

            _seriesBackgrounds[i].color = isCurrent ? UiPalette.BgSelected : UiPalette.BgRow;
            _seriesTexts[i].text = BuildSeriesText(series, playerTeamName);
            _seriesTexts[i].color = IsPlayerSeries(series, playerTeamName) ? UiPalette.Text1 : UiPalette.Text3;
        }

        _nextGameButton.interactable = !postSeason.IsFinished;
        _claimButton.gameObject.SetActive(postSeason.IsFinished);
        _claimButton.interactable = postSeason.IsFinished && !postSeason.RewardsGranted;
    }

    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, 32f, 32f, 700f, 60f);
        title.text = "가을야구";

        _stateText = UiBuilder.CreateText(Root, "State", 34f, UiPalette.AccentGold);
        UiBuilder.Place(_stateText.rectTransform, 32f, 100f, 1016f, 48f);

        _championText = UiBuilder.CreateText(Root, "Champion", 44f, UiPalette.AccentGold);
        UiBuilder.Place(_championText.rectTransform, 32f, 152f, 1016f, 56f);

        for (int i = 0; i < MaxSeriesRows; i++)
        {
            _seriesBackgrounds[i] = UiBuilder.CreateImage(Root, "Series" + i, UiPalette.BgRow,
                UiSprites.RowNormal, Image.Type.Sliced);
            UiBuilder.Place(_seriesBackgrounds[i].rectTransform, 32f, 230f + i * (RowHeight + RowGap),
                1016f, RowHeight);

            _seriesTexts[i] = UiBuilder.CreateText(_seriesBackgrounds[i].transform, "Text", 34f, UiPalette.Text1);
            UiBuilder.Stretch(_seriesTexts[i].rectTransform, 24f, 0f, 24f, 0f);
            _seriesTexts[i].textWrappingMode = TextWrappingModes.Normal;
        }

        _nextGameButton = UiBuilder.CreateButton(Root, "NextGame", "다음 경기 진행", 36f, true, SimulateNextGame);
        RectTransform nextRect = _nextGameButton.GetComponent<RectTransform>();

        nextRect.anchorMin = new Vector2(0f, 0f);
        nextRect.anchorMax = new Vector2(0.5f, 0f);
        nextRect.pivot = new Vector2(0f, 0f);
        nextRect.offsetMin = new Vector2(32f, 180f);
        nextRect.offsetMax = new Vector2(-12f, 300f);

        _claimButton = UiBuilder.CreateButton(Root, "Claim", "가을야구 보상 받기", 34f, true, ClaimRewards);
        RectTransform claimRect = _claimButton.GetComponent<RectTransform>();

        claimRect.anchorMin = new Vector2(0.5f, 0f);
        claimRect.anchorMax = new Vector2(1f, 0f);
        claimRect.pivot = new Vector2(0f, 0f);
        claimRect.offsetMin = new Vector2(12f, 180f);
        claimRect.offsetMax = new Vector2(-32f, 300f);

        Button backButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, false, OnBackClicked);
        RectTransform backRect = backButton.GetComponent<RectTransform>();

        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.offsetMin = new Vector2(32f, 40f);
        backRect.offsetMax = new Vector2(-32f, 160f);
    }

    protected override void OnShown()
    {
        Refresh();
    }

    private void SimulateNextGame()
    {
        if (LeagueManager.Instance == null)
            return;

        LeagueGameScore? score = LeagueManager.Instance.SimulateNextPostSeasonGame();

        if (!score.HasValue)
        {
            _onNotify?.Invoke("더 진행할 경기가 없습니다", false);
            Refresh();
            return;
        }

        LeagueGameScore value = score.Value;

        _onNotify?.Invoke($"{value.Game.AwayTeamName} {value.AwayScore} - {value.HomeScore} {value.Game.HomeTeamName}", true);

        LeagueManager.Instance.SavePostSeason();
        Refresh();
    }

    private void ClaimRewards()
    {
        if (LeagueManager.Instance == null)
            return;

        PostSeasonRewardResult result = LeagueManager.Instance.ClaimPostSeasonRewards();

        if (result == null)
        {
            _onNotify?.Invoke("받을 보상이 없습니다", false);
            Refresh();
            return;
        }

        _onNotify?.Invoke($"보상 수령 — 골드 {result.Gold} · 골글 포인트 {result.GoldenGlovePoint} · 시그권 {result.SignatureTicket}", true);

        LeagueManager.Instance.SavePostSeason();
        Refresh();
    }

    private static string BuildSeriesText(PostSeasonSeries series, string playerTeamName)
    {
        string lower = string.IsNullOrEmpty(series.LowerSeedTeamName) ? "미정" : series.LowerSeedTeamName;
        string marker = IsPlayerSeries(series, playerTeamName) ? "  ◀ 우리 팀" : string.Empty;
        string winner = series.IsFinished ? $"   승 {series.WinnerTeamName}" : string.Empty;

        return $"{GetRoundName(series.Round)}   {series.HigherSeedTeamName} " +
               $"{series.HigherSeedWins} - {series.LowerSeedWins} {lower}" +
               $"   ({series.WinsToClinch}선승){winner}{marker}";
    }

    //PostSeasonSeries 에는 참가 여부 질의가 없어 이름으로 비교한다
    private static bool IsPlayerSeries(PostSeasonSeries series, string playerTeamName)
    {
        if (string.IsNullOrEmpty(playerTeamName))
            return false;

        return series.HigherSeedTeamName == playerTeamName || series.LowerSeedTeamName == playerTeamName;
    }

    private static string GetRoundName(PostSeasonRound round)
    {
        return round switch
        {
            PostSeasonRound.WildCard => "와일드카드",
            PostSeasonRound.SemiPlayOff => "준플레이오프",
            PostSeasonRound.PlayOff => "플레이오프",
            PostSeasonRound.KoreanSeries => "한국시리즈",
            _ => round.ToString()
        };
    }

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }
}
