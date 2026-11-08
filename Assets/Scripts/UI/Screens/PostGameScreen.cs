using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ③ 경기 후 화면. FINAL · 라인스코어 · 타자/투수 기록 (명세 ③)
/// </summary>
public class PostGameScreen : UiScreenBehaviour
{
    private static readonly string[] HitterHeaders =
    {
        "타석", "타수", "안타", "2타", "3타", "홈런", "타점", "득점", "볼넷", "삼진", "도루", "도실", "타율"
    };

    private static readonly string[] PitcherHeaders =
    {
        "이닝", "투구", "상대", "피안타", "피홈런", "실점", "볼넷", "탈삼진"
    };

    private const float LineScoreTop = 350f;
    private const float TabTop = 556f;
    private const float TableTop = 636f;

    private Action _onReturn;

    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _detailText;
    [SerializeField] private LineScoreView _lineScore;
    [SerializeField] private StatTableView _hitterTable;
    [SerializeField] private StatTableView _pitcherTable;
    [SerializeField] private Button _hitterTabButton;
    [SerializeField] private Button _pitcherTabButton;
    [SerializeField] private Button _awayTabButton;
    [SerializeField] private Button _homeTabButton;

    private BoxScore _boxScore;
    private string _awayTeamName = string.Empty;
    private string _homeTeamName = string.Empty;
    private bool _showPitchers;
    private bool _showHome;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onReturn)
    {
        _onReturn = onReturn;
    }

    /// <summary>
    /// 끝난 경기의 박스스코어를 채운다
    /// </summary>
    public void SetResult(BoxScore boxScore, string awayTeamName, string homeTeamName, bool isPlayerHome)
    {
        if (boxScore == null)
        {
            Debug.LogError("[PostGameScreen]: 박스스코어가 없어 결과를 표시할 수 없습니다");
            return;
        }

        _boxScore = boxScore;
        _awayTeamName = awayTeamName;
        _homeTeamName = homeTeamName;
        _showHome = isPlayerHome;

        int playerRuns = isPlayerHome ? boxScore.HomeRuns : boxScore.AwayRuns;
        int opponentRuns = isPlayerHome ? boxScore.AwayRuns : boxScore.HomeRuns;

        _resultText.text = GetResultLabel(playerRuns, opponentRuns);
        _resultText.color = GetResultColor(playerRuns, opponentRuns);
        _scoreText.text = $"{boxScore.AwayRuns} - {boxScore.HomeRuns}";
        _detailText.text = $"{awayTeamName}  vs  {homeTeamName}";

        _lineScore.SetBoxScore(boxScore, awayTeamName, homeTeamName);

        RefreshTables();
        PlayResultEffect(playerRuns, opponentRuns);
    }

    protected override void BindEvents()
    {
        Bind(_hitterTabButton, ShowHitters);
        Bind(_pitcherTabButton, ShowPitchers);
        Bind(_awayTabButton, ShowAway);
        Bind(_homeTabButton, ShowHome);
        BindButton("ReturnButton", OnReturnClicked);
    }

    protected override void Build()
    {
        TextMeshProUGUI finalLabel = UiBuilder.CreateLabel(Root, "FinalLabel", "FINAL", 34f,
            UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(finalLabel.rectTransform, 340f, 40f, 400f, 40f);

        _resultText = UiBuilder.CreateLabel(Root, "Result", "WIN", 72f, UiPalette.Text1,
            TextAlignmentOptions.Center);
        UiBuilder.Place(_resultText.rectTransform, 340f, 84f, 400f, 84f);

        _scoreText = UiBuilder.CreateText(Root, "Score", 96f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Place(_scoreText.rectTransform, 290f, 172f, 500f, 110f);

        _detailText = UiBuilder.CreateText(Root, "Detail", 34f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Place(_detailText.rectTransform, 290f, 288f, 500f, 44f);

        _lineScore = LineScoreView.Bake(Root);
        UiBuilder.Place(_lineScore.Root, 24f, LineScoreTop, 1032f, LineScoreView.TotalHeight);

        _hitterTabButton = UiBuilder.CreateButton(Root, "HitterTab", "타자 기록", 32f, true, ShowHitters);
        UiBuilder.Place(_hitterTabButton.GetComponent<RectTransform>(), 24f, TabTop, 250f, 64f);

        _pitcherTabButton = UiBuilder.CreateButton(Root, "PitcherTab", "투수 기록", 32f, false, ShowPitchers);
        UiBuilder.Place(_pitcherTabButton.GetComponent<RectTransform>(), 284f, TabTop, 250f, 64f);

        _awayTabButton = UiBuilder.CreateButton(Root, "AwayTab", "AWAY", 30f, false, ShowAway);
        UiBuilder.Place(_awayTabButton.GetComponent<RectTransform>(), 596f, TabTop, 220f, 64f);

        _homeTabButton = UiBuilder.CreateButton(Root, "HomeTab", "HOME", 30f, false, ShowHome);
        UiBuilder.Place(_homeTabButton.GetComponent<RectTransform>(), 826f, TabTop, 220f, 64f);

        _hitterTable = StatTableView.Bake(Root, HitterHeaders, 230f, 78f);
        UiBuilder.Stretch(_hitterTable.Root, 24f, TableTop, 24f, 180f);

        _pitcherTable = StatTableView.Bake(Root, PitcherHeaders, 230f, 100f);
        UiBuilder.Stretch(_pitcherTable.Root, 24f, TableTop, 24f, 180f);

        Button returnButton = UiBuilder.CreateButton(Root, "ReturnButton", "리그로 돌아가기", 40f, true, OnReturnClicked);
        RectTransform returnRect = returnButton.GetComponent<RectTransform>();

        returnRect.anchorMin = new Vector2(0f, 0f);
        returnRect.anchorMax = new Vector2(1f, 0f);
        returnRect.pivot = new Vector2(0.5f, 0f);
        returnRect.offsetMin = new Vector2(24f, 32f);
        returnRect.offsetMax = new Vector2(-24f, 144f);
    }

    //탭 상태에 맞는 표만 보여준다
    private void RefreshTables()
    {
        if (_boxScore == null)
            return;

        _hitterTable.Root.gameObject.SetActive(!_showPitchers);
        _pitcherTable.Root.gameObject.SetActive(_showPitchers);

        UiTabStyle.Apply(_hitterTabButton, !_showPitchers);
        UiTabStyle.Apply(_pitcherTabButton, _showPitchers);
        UiTabStyle.Apply(_awayTabButton, !_showHome);
        UiTabStyle.Apply(_homeTabButton, _showHome);

        if (_showPitchers)
        {
            FillPitcherTable(_showHome ? _boxScore.HomePitchers : _boxScore.AwayPitchers);
            return;
        }

        FillHitterTable(_showHome ? _boxScore.HomeHitters : _boxScore.AwayHitters);
    }

    private void FillHitterTable(IReadOnlyList<HitterGameStats> hitters)
    {
        List<string> names = new List<string>(hitters.Count);
        List<string[]> cells = new List<string[]>(hitters.Count);

        foreach (HitterGameStats stats in hitters)
        {
            names.Add(stats.Name);
            cells.Add(new[]
            {
                stats.PlateAppearances.ToString(),
                stats.AtBats.ToString(),
                stats.Hits.ToString(),
                stats.Doubles.ToString(),
                stats.Triples.ToString(),
                stats.HomeRuns.ToString(),
                stats.RunsBattedIn.ToString(),
                stats.Runs.ToString(),
                stats.Walks.ToString(),
                stats.StrikeOuts.ToString(),
                stats.StolenBases.ToString(),
                stats.CaughtStealing.ToString(),
                FormatAverage(stats.Average)
            });
        }

        _hitterTable.SetRows(names, cells);
    }

    private void FillPitcherTable(IReadOnlyList<PitcherGameStats> pitchers)
    {
        List<string> names = new List<string>(pitchers.Count);
        List<string[]> cells = new List<string[]>(pitchers.Count);

        foreach (PitcherGameStats stats in pitchers)
        {
            names.Add(stats.Name);
            cells.Add(new[]
            {
                stats.GetInningsPitchedText(),
                stats.PitchCount.ToString(),
                stats.BattersFaced.ToString(),
                stats.HitsAllowed.ToString(),
                stats.HomeRunsAllowed.ToString(),
                stats.RunsAllowed.ToString(),
                stats.Walks.ToString(),
                stats.StrikeOuts.ToString()
            });
        }

        _pitcherTable.SetRows(names, cells);
    }

    private void ShowHitters()
    {
        _showPitchers = false;
        RefreshTables();
    }

    private void ShowPitchers()
    {
        _showPitchers = true;
        RefreshTables();
    }

    private void ShowAway()
    {
        _showHome = false;
        RefreshTables();
    }

    private void ShowHome()
    {
        _showHome = true;
        RefreshTables();
    }

    private void OnReturnClicked()
    {
        _onReturn?.Invoke();
    }

    //결과 글자가 튀어오르고, 이겼으면 파티클이 터진다
    private void PlayResultEffect(int playerRuns, int opponentRuns)
    {
        UiEffects.Pop(_resultText.transform, 0.5f, 0.42f);
        UiEffects.Pop(_scoreText.transform, 0.78f, 0.36f, 0.1f);

        if (playerRuns <= opponentRuns)
            return;

        UiEffects.Flash(_resultText, UiPalette.Text1, 0.8f);
        UiEffects.Burst(_resultText.rectTransform, UiEffectKind.HomeRun);
    }

    private static string GetResultLabel(int playerRuns, int opponentRuns)
    {
        if (playerRuns > opponentRuns)
            return "WIN";

        return playerRuns < opponentRuns ? "LOSE" : "DRAW";
    }

    private static Color GetResultColor(int playerRuns, int opponentRuns)
    {
        if (playerRuns > opponentRuns)
            return UiPalette.AccentGold;

        return playerRuns < opponentRuns ? UiPalette.Text3 : UiPalette.Text2;
    }

    //타율은 소수점 앞 0을 떼고 세 자리로 표기한다 (.270)
    private static string FormatAverage(float average)
    {
        string text = average.ToString("0.000");

        return average < 1f ? text.Substring(1) : text;
    }
}
