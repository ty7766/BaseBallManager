using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타석·도루 결과가 한 줄씩 흐르는 중계 영역 (명세 1-5 · 6-4).
/// 이닝이 바뀌면 구분 헤더를 스스로 끼워 넣는다
/// </summary>
public class PlayLogView
{
    private const float RowHeight = 68f;
    private const float HeaderHeight = 56f;
    private const float RowGap = 4f;
    private const float SentenceSize = 38f;

    public RectTransform Root => _root;

    private readonly RectTransform _root;
    private readonly ScrollRect _scroll;
    private readonly RectTransform _content;
    private readonly Button _jumpToLatestButton;
    private readonly List<LogRow> _rows = new List<LogRow>(128);

    private string _awayTeamName = string.Empty;
    private string _homeTeamName = string.Empty;
    private int _usedRowCount;
    private float _contentHeight;
    private int _lastInning;
    private bool _lastIsTopInning;
    private bool _hasLastInning;
    private bool _autoScroll = true;

    public PlayLogView(Transform parent)
    {
        _root = UiBuilder.CreateRect(parent, "PlayLog");

        Image background = UiBuilder.CreateImage(_root, "Background", UiPalette.BgPanel,
            UiSprites.PanelLarge, Image.Type.Sliced);
        UiBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        TextMeshProUGUI header = UiBuilder.CreateText(_root, "Header", 32f, UiPalette.Text3);
        UiBuilder.Place(header.rectTransform, 24f, 12f, 200f, 40f);
        header.text = "중계";

        _scroll = UiBuilder.CreateVerticalScroll(_root, "LogScroll", out _content);
        UiBuilder.Stretch(_scroll.GetComponent<RectTransform>(), 12f, 58f, 12f, 12f);
        _scroll.onValueChanged.AddListener(OnScrolled);

        _jumpToLatestButton = UiBuilder.CreateButton(_root, "JumpToLatest", "최신으로", 28f, false, JumpToLatest);

        RectTransform jumpRect = _jumpToLatestButton.GetComponent<RectTransform>();

        jumpRect.anchorMin = new Vector2(1f, 0f);
        jumpRect.anchorMax = new Vector2(1f, 0f);
        jumpRect.pivot = new Vector2(1f, 0f);
        jumpRect.anchoredPosition = new Vector2(-24f, 24f);
        jumpRect.sizeDelta = new Vector2(200f, 64f);
        _jumpToLatestButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 이닝 헤더에 쓸 팀 이름. 경기 시작 시 한 번 넣는다
    /// </summary>
    public void SetTeams(string awayTeamName, string homeTeamName)
    {
        _awayTeamName = awayTeamName;
        _homeTeamName = homeTeamName;
    }

    /// <summary>
    /// 모든 행을 비운다 (다음 경기 재사용)
    /// </summary>
    public void Clear()
    {
        foreach (LogRow row in _rows)
            row.Root.gameObject.SetActive(false);

        _usedRowCount = 0;
        _contentHeight = 0f;
        _hasLastInning = false;
        _autoScroll = true;

        _jumpToLatestButton.gameObject.SetActive(false);
        _content.sizeDelta = new Vector2(0f, 0f);
    }

    /// <summary>
    /// 타석 로그 1건을 추가한다. 이닝이 바뀌면 구분 헤더가 먼저 들어간다
    /// </summary>
    public void AppendBatterLog(SimulationBatterLog log)
    {
        if (log == null)
            return;

        EnsureInningHeader(log.Inning, log.IsTopInning);

        LogRow row = TakeRow(RowHeight);

        row.SetBatterLog(log, GetAttackTeamColor(log.IsTopInning));
        ScrollToLatest();
    }

    /// <summary>
    /// 도루 로그 1건을 추가한다 (타석 로그보다 먼저 들어온다)
    /// </summary>
    public void AppendStealLog(SimulationStealLog log)
    {
        if (log == null)
            return;

        EnsureInningHeader(log.Inning, log.IsTopInning);

        LogRow row = TakeRow(RowHeight);

        row.SetStealLog(log, GetAttackTeamColor(log.IsTopInning));
        ScrollToLatest();
    }

    //이닝·공수가 바뀌는 지점에 구분 헤더를 넣는다
    private void EnsureInningHeader(int inning, bool isTopInning)
    {
        if (_hasLastInning && _lastInning == inning && _lastIsTopInning == isTopInning)
            return;

        _lastInning = inning;
        _lastIsTopInning = isTopInning;
        _hasLastInning = true;

        LogRow row = TakeRow(HeaderHeight);
        string attackTeam = isTopInning ? _awayTeamName : _homeTeamName;

        row.SetInningHeader(PlayLogTextProvider.BuildInningHeader(inning, isTopInning, attackTeam));
    }

    //풀에서 행을 꺼내거나 새로 만든다
    private LogRow TakeRow(float height)
    {
        if (_usedRowCount == _rows.Count)
            _rows.Add(new LogRow(_content));

        LogRow row = _rows[_usedRowCount];

        _usedRowCount++;

        row.Root.gameObject.SetActive(true);
        UiBuilder.PlaceRow(row.Root, _contentHeight, height);

        _contentHeight += height + RowGap;
        _content.sizeDelta = new Vector2(0f, _contentHeight);

        return row;
    }

    private void ScrollToLatest()
    {
        if (_autoScroll)
            _scroll.verticalNormalizedPosition = 0f;
    }

    //사용자가 위로 올리면 자동 스크롤을 멈춘다
    private void OnScrolled(Vector2 position)
    {
        if (_usedRowCount == 0)
            return;

        bool scrolledUp = position.y > 0.03f;

        if (scrolledUp == !_autoScroll)
            return;

        _autoScroll = !scrolledUp;
        _jumpToLatestButton.gameObject.SetActive(scrolledUp);
    }

    private void JumpToLatest()
    {
        _autoScroll = true;
        _jumpToLatestButton.gameObject.SetActive(false);
        _scroll.verticalNormalizedPosition = 0f;
    }

    private Color GetAttackTeamColor(bool isTopInning)
    {
        string attackTeam = isTopInning ? _awayTeamName : _homeTeamName;
        Color teamColor = UiPalette.GetTeamColor(attackTeam);

        return teamColor.r + teamColor.g + teamColor.b < 0.45f ? UiPalette.Text2 : teamColor;
    }

    //중계 행 하나. 헤더 · 타석 · 도루 세 모양을 부분 토글로 공유한다
    private class LogRow
    {
        private readonly CanvasGroup _group;
        private readonly Image _background;
        private readonly Image _inningChip;
        private readonly TextMeshProUGUI _inningText;
        private readonly TextMeshProUGUI _sentenceText;
        private readonly Image _scoreBadge;
        private readonly TextMeshProUGUI _scoreText;

        public LogRow(RectTransform parent)
        {
            Root = UiBuilder.CreateRect(parent, "LogRow");
            _group = Root.gameObject.AddComponent<CanvasGroup>();

            _background = UiBuilder.CreateImage(Root, "Background", UiPalette.BgRow,
                UiSprites.RowNormal, Image.Type.Sliced);
            UiBuilder.Stretch(_background.rectTransform, 0f, 0f, 0f, 0f);

            _inningChip = UiBuilder.CreateImage(Root, "InningChip", UiPalette.LineBorder,
                UiSprites.LogInningChip, Image.Type.Sliced);
            UiBuilder.Place(_inningChip.rectTransform, 10f, 14f, 88f, 40f);

            _inningText = UiBuilder.CreateText(_inningChip.transform, "InningText", 26f,
                UiPalette.Text1, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_inningText.rectTransform, 0f, 0f, 0f, 0f);

            _sentenceText = UiBuilder.CreateText(Root, "Sentence", SentenceSize, UiPalette.LogNormal);
            UiBuilder.PlaceRow(_sentenceText.rectTransform, 12f, 44f, 0f);
            _sentenceText.rectTransform.offsetMin = new Vector2(112f, _sentenceText.rectTransform.offsetMin.y);
            _sentenceText.rectTransform.offsetMax = new Vector2(-160f, _sentenceText.rectTransform.offsetMax.y);

            _scoreBadge = UiBuilder.CreateImage(Root, "ScoreBadge", UiPalette.AccentGold,
                UiSprites.BadgeScore, Image.Type.Sliced);
            UiBuilder.PlaceRight(_scoreBadge.rectTransform, 12f, 14f, 136f, 40f);

            _scoreText = UiBuilder.CreateText(_scoreBadge.transform, "ScoreText", 28f,
                UiPalette.BgApp, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_scoreText.rectTransform, 0f, 0f, 0f, 0f);
        }

        public RectTransform Root { get; }

        /// <summary>
        /// 이닝 구분 헤더 모양
        /// </summary>
        public void SetInningHeader(string text)
        {
            _inningChip.gameObject.SetActive(false);
            _scoreBadge.gameObject.SetActive(false);

            _background.sprite = UiSprites.Get(UiSprites.LogInningHeader);
            _background.color = UiPalette.WithAlpha(UiPalette.LineHair, 0.9f);

            _sentenceText.text = text;
            _sentenceText.fontSize = 30f;
            _sentenceText.color = UiPalette.Text2;
            _sentenceText.fontStyle = FontStyles.Normal;

            UiEffects.SlideUp(Root, _group, 20f, 0.18f);
        }

        /// <summary>
        /// 타석 결과 모양. 득점이 있으면 금색 배지가 붙는다
        /// </summary>
        public void SetBatterLog(SimulationBatterLog log, Color inningChipColor)
        {
            SetCommon(log.Inning, log.IsTopInning, inningChipColor);

            _sentenceText.text = PlayLogTextProvider.BuildBatterSentence(log);
            _sentenceText.color = PlayLogTextProvider.GetOutcomeColor(log.Outcome);
            _sentenceText.fontStyle = PlayLogTextProvider.IsBoldOutcome(log.Outcome)
                ? FontStyles.Bold
                : FontStyles.Normal;

            bool isHomeRun = log.Outcome == BatterOutcome.HomeRun;

            _background.sprite = UiSprites.Get(GetBackgroundSprite(log.RunsScored, isHomeRun));
            _background.color = GetBackgroundColor(log.RunsScored, isHomeRun);

            _scoreBadge.gameObject.SetActive(log.RunsScored > 0);
            _scoreText.text = $"+{log.RunsScored}점";

            UiEffects.SlideUp(Root, _group, 26f, 0.22f);

            if (isHomeRun)
            {
                UiEffects.Flash(_background, UiPalette.AccentGold, 0.6f);
                UiEffects.Burst(Root, UiEffectKind.HomeRun);
                return;
            }

            if (log.RunsScored <= 0)
                return;

            UiEffects.Pop(_scoreBadge.transform, 0.5f, 0.32f);
            UiEffects.Burst(_scoreBadge.rectTransform, UiEffectKind.Score);
        }

        /// <summary>
        /// 도루 결과 모양
        /// </summary>
        public void SetStealLog(SimulationStealLog log, Color inningChipColor)
        {
            SetCommon(log.Inning, log.IsTopInning, inningChipColor);

            _sentenceText.text = PlayLogTextProvider.BuildStealSentence(log);
            _sentenceText.color = PlayLogTextProvider.GetStealColor(log);
            _sentenceText.fontStyle = FontStyles.Normal;

            _background.sprite = UiSprites.Get(UiSprites.RowNormal);
            _background.color = UiPalette.WithAlpha(UiPalette.BgRow, 0.6f);

            _scoreBadge.gameObject.SetActive(false);

            UiEffects.SlideUp(Root, _group, 22f, 0.2f);

            if (log.IsSuccess)
                UiEffects.Burst(Root, UiEffectKind.Steal);
        }

        private void SetCommon(int inning, bool isTopInning, Color inningChipColor)
        {
            _inningChip.gameObject.SetActive(true);
            _inningChip.color = inningChipColor;
            _inningText.text = PlayLogTextProvider.GetInningChipText(inning, isTopInning);
            _sentenceText.fontSize = SentenceSize;
        }

        private static string GetBackgroundSprite(int runsScored, bool isHomeRun)
        {
            if (isHomeRun)
                return UiSprites.LogRowHomeRun;

            return runsScored > 0 ? UiSprites.LogRowScore : UiSprites.LogRowOffense;
        }

        private static Color GetBackgroundColor(int runsScored, bool isHomeRun)
        {
            if (isHomeRun)
                return UiPalette.WithAlpha(UiPalette.AccentGold, 0.22f);

            return runsScored > 0
                ? UiPalette.WithAlpha(UiPalette.AccentGold, 0.12f)
                : UiPalette.WithAlpha(UiPalette.BgRow, 0.75f);
        }
    }
}
