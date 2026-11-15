using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이닝별 득점 + R·H·E 표 (명세 ③). 연장이면 칸 폭을 줄여 가로 스크롤 없이 담는다
/// </summary>
public class LineScoreView : UiWidgetBehaviour
{
    private const float LabelWidth = 170f;
    private const float SummaryCellWidth = 80f;
    private const float RegulationCellWidth = 66f;
    private const float RowHeight = 72f;
    private const float TextTop = 12f;
    private const float TextHeight = 48f;
    private const float CellFontSize = 38f;
    private const int SummaryColumnCount = 3;

    /// <summary>
    /// 표 전체 높이 (경기 후 화면 배치용)
    /// </summary>
    public static float TotalHeight => RowHeight * 3f + 16f;

    [SerializeField] private RectTransform _inningHeaderRow;
    [SerializeField] private RectTransform _awayRow;
    [SerializeField] private RectTransform _homeRow;
    [SerializeField] private TextMeshProUGUI _awayLabel;
    [SerializeField] private TextMeshProUGUI _homeLabel;
    private readonly List<TextMeshProUGUI> _inningHeaders = new List<TextMeshProUGUI>(11);
    private readonly List<TextMeshProUGUI> _awayCells = new List<TextMeshProUGUI>(11);
    private readonly List<TextMeshProUGUI> _homeCells = new List<TextMeshProUGUI>(11);
    [SerializeField] private TextMeshProUGUI[] _awaySummary = new TextMeshProUGUI[SummaryColumnCount];
    [SerializeField] private TextMeshProUGUI[] _homeSummary = new TextMeshProUGUI[SummaryColumnCount];

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static LineScoreView Bake(Transform parent)
    {
        LineScoreView view = CreateNode<LineScoreView>(parent, "LineScore");

        view.BuildWidget(parent);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(Transform parent)
    {
        Image background = BroadcastPanel.Create(Root, "Background");
        UiBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        _inningHeaderRow = UiBuilder.PlaceRow(UiBuilder.CreateRect(Root, "InningHeaderRow"), 8f, RowHeight);
        _awayRow = UiBuilder.PlaceRow(UiBuilder.CreateRect(Root, "AwayRow"), 8f + RowHeight, RowHeight);
        _homeRow = UiBuilder.PlaceRow(UiBuilder.CreateRect(Root, "HomeRow"), 8f + RowHeight * 2f, RowHeight);

        _awayLabel = UiBuilder.CreateText(_awayRow, "AwayLabel", CellFontSize, UiPalette.Text1);
        UiBuilder.Place(_awayLabel.rectTransform, 24f, TextTop, LabelWidth - 32f, TextHeight);
        _awayLabel.fontStyle = FontStyles.Bold;

        _homeLabel = UiBuilder.CreateText(_homeRow, "HomeLabel", CellFontSize, UiPalette.Text1);
        UiBuilder.Place(_homeLabel.rectTransform, 24f, TextTop, LabelWidth - 32f, TextHeight);
        _homeLabel.fontStyle = FontStyles.Bold;

        string[] summaryHeaders = { "R", "H", "E" };

        for (int i = 0; i < SummaryColumnCount; i++)
        {
            TextMeshProUGUI header = UiBuilder.CreateLabel(_inningHeaderRow, "SummaryHeader",
                summaryHeaders[i], 32f, i == 0 ? UiPalette.AccentGold : UiPalette.Text3,
                TextAlignmentOptions.Center);

            UiBuilder.PlaceRight(header.rectTransform,
                (SummaryColumnCount - 1 - i) * SummaryCellWidth + 16f, TextTop, SummaryCellWidth, TextHeight);

            _awaySummary[i] = CreateSummaryCell(_awayRow, i);
            _homeSummary[i] = CreateSummaryCell(_homeRow, i);
        }
    }

    /// <summary>
    /// 박스스코어를 표에 채운다
    /// </summary>
    public void SetBoxScore(BoxScore boxScore, string awayTeamName, string homeTeamName)
    {
        if (boxScore == null)
            return;

        _awayLabel.text = awayTeamName;
        _homeLabel.text = homeTeamName;

        int inningCount = Mathf.Max(GameState.RegulationInnings, boxScore.Innings.Count);
        float cellWidth = inningCount <= GameState.RegulationInnings
            ? RegulationCellWidth
            : RegulationCellWidth * GameState.RegulationInnings / inningCount;

        EnsureInningColumns(inningCount, cellWidth);

        for (int i = 0; i < _inningHeaders.Count; i++)
        {
            bool used = i < inningCount;

            _inningHeaders[i].gameObject.SetActive(used);
            _awayCells[i].gameObject.SetActive(used);
            _homeCells[i].gameObject.SetActive(used);

            if (!used)
                continue;

            _inningHeaders[i].text = (i + 1).ToString();

            if (i >= boxScore.Innings.Count)
            {
                _awayCells[i].text = string.Empty;
                _homeCells[i].text = string.Empty;
                continue;
            }

            InningScore inning = boxScore.Innings[i];

            _awayCells[i].text = inning.AwayRuns.ToString();
            SetHomeCell(_homeCells[i], inning);
        }

        _awaySummary[0].text = boxScore.AwayRuns.ToString();
        _awaySummary[1].text = boxScore.AwayHits.ToString();
        _awaySummary[2].text = boxScore.AwayErrors.ToString();

        _homeSummary[0].text = boxScore.HomeRuns.ToString();
        _homeSummary[1].text = boxScore.HomeHits.ToString();
        _homeSummary[2].text = boxScore.HomeErrors.ToString();
    }

    //홈이 말 공격을 하지 않은 이닝은 X로 구분한다 (0과 다르다)
    private static void SetHomeCell(TextMeshProUGUI cell, InningScore inning)
    {
        if (inning.HomePlayed)
        {
            cell.text = inning.HomeRuns.ToString();
            cell.color = UiPalette.Text1;
            return;
        }

        cell.text = "X";
        cell.color = UiPalette.TextOff;
    }

    //이닝 칸을 필요한 개수만큼 만들고 폭을 다시 잡는다
    private void EnsureInningColumns(int inningCount, float cellWidth)
    {
        while (_inningHeaders.Count < inningCount)
        {
            _inningHeaders.Add(UiBuilder.CreateText(_inningHeaderRow, "InningHeader", 30f,
                UiPalette.Text3, TextAlignmentOptions.Center));
            _awayCells.Add(BroadcastPanel.CreateNumber(_awayRow, "AwayCell", CellFontSize, UiPalette.Text1));
            _homeCells.Add(BroadcastPanel.CreateNumber(_homeRow, "HomeCell", CellFontSize, UiPalette.Text1));
        }

        for (int i = 0; i < inningCount; i++)
        {
            float x = LabelWidth + i * cellWidth;

            UiBuilder.Place(_inningHeaders[i].rectTransform, x, TextTop, cellWidth, TextHeight);
            UiBuilder.Place(_awayCells[i].rectTransform, x, TextTop, cellWidth, TextHeight);
            UiBuilder.Place(_homeCells[i].rectTransform, x, TextTop, cellWidth, TextHeight);
        }
    }

    private TextMeshProUGUI CreateSummaryCell(RectTransform row, int columnIndex)
    {
        TextMeshProUGUI cell = BroadcastPanel.CreateNumber(row, "Summary", CellFontSize,
            columnIndex == 0 ? UiPalette.AccentGold : UiPalette.Text1);

        UiBuilder.PlaceRight(cell.rectTransform,
            (SummaryColumnCount - 1 - columnIndex) * SummaryCellWidth + 16f, TextTop, SummaryCellWidth, TextHeight);

        return cell;
    }
}
