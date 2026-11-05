using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뽑기 기록 창. 화면 가운데에 최신 순으로 한 쪽에 10줄씩, 세로줄 없는 표로 보여 준다.
/// 카드는 `이름 '연도` 를 희귀도 색(3성 파랑 · 4성 초록 · 5성 보라 · 골글 노랑 · 시그 분홍)으로 구별하고, 아래 화살표로 쪽을 넘긴다
/// </summary>
public class GachaHistoryPanel : UiWidgetBehaviour
{
    private const int RowsPerPage = 10;
    private const int ColumnCount = 4;
    private const float WindowWidth = 960f;
    private const float Padding = 48f;
    private const float TitleTop = 40f;
    private const float TitleHeight = 72f;
    private const float HeaderTop = 136f;
    private const float HeaderHeight = 60f;
    private const float RowTop = HeaderTop + HeaderHeight;
    private const float RowHeight = 80f;
    private const float FooterGap = 28f;
    private const float FooterTop = RowTop + RowsPerPage * RowHeight + FooterGap;
    private const float FooterHeight = 100f;
    private const float WindowHeight = FooterTop + FooterHeight + Padding;
    private const float ArrowButtonWidth = 120f;
    private const float CloseWidth = 140f;
    private const float CloseHeight = 72f;
    private const float RowFontSize = 32f;
    private const float TimeFontSize = 28f;
    private const string TimeFormat = "yy.MM.dd HH:mm";

    //열: 카드 · 등급 · 뽑기 · 시간 (내용 폭 864 를 나눈다)
    private static readonly float[] ColumnWidths = { 330f, 160f, 150f, 224f };
    private static readonly string[] ColumnTitles = { "카드", "등급", "뽑기", "시간" };

    private readonly TextMeshProUGUI[,] _cells = new TextMeshProUGUI[RowsPerPage, ColumnCount];
    private readonly GameObject[] _rows = new GameObject[RowsPerPage];

    private CanvasGroup _fade;
    private RectTransform _window;
    private TextMeshProUGUI _emptyText;
    private TextMeshProUGUI _pageText;
    private Button _previousButton;
    private Button _nextButton;
    private TextMeshProUGUI _previousLabel;
    private TextMeshProUGUI _nextLabel;
    private int _page;

    /// <summary>
    /// 화면 위에 덮을 기록 창을 만든다. 처음엔 닫혀 있다
    /// </summary>
    public static GachaHistoryPanel Create(Transform parent)
    {
        GachaHistoryPanel panel = CreateNode<GachaHistoryPanel>(parent, "GachaHistory");

        panel.BuildPanel();
        panel.SetVisible(false);

        return panel;
    }

    /// <summary>
    /// 가장 최근 쪽부터 연다
    /// </summary>
    public void Open()
    {
        _page = 0;
        Root.SetAsLastSibling();
        SetVisible(true);
        ShowPage();

        UiEffects.FadeIn(_fade, 0.16f);
        UiEffects.Pop(_window, 0.94f, 0.22f);
    }

    /// <summary>
    /// 창을 닫는다
    /// </summary>
    public void Close()
    {
        SetVisible(false);
    }

    //지금 쪽의 10줄을 채우고 화살표 · 쪽 번호를 맞춘다. 줄이 모자라면 빈 줄은 숨긴다
    private void ShowPage()
    {
        IReadOnlyList<GachaHistoryEntry> history = GetHistory();
        int pageCount = Mathf.Max(1, (history.Count + RowsPerPage - 1) / RowsPerPage);

        _page = Mathf.Clamp(_page, 0, pageCount - 1);

        for (int row = 0; row < RowsPerPage; row++)
        {
            int newestIndex = _page * RowsPerPage + row;
            bool used = newestIndex < history.Count;

            _rows[row].SetActive(used);

            if (used)
                FillRow(row, history[history.Count - 1 - newestIndex]);
        }

        _emptyText.gameObject.SetActive(history.Count == 0);
        _pageText.text = $"{_page + 1} / {pageCount}";
        SetArrow(_previousButton, _previousLabel, _page > 0);
        SetArrow(_nextButton, _nextLabel, _page < pageCount - 1);
    }

    //카드 이름 · 등급은 희귀도 색, 뽑기 종류 · 시간은 보조 글자색
    private void FillRow(int row, GachaHistoryEntry entry)
    {
        CardMasterData master = CardDataManager.Instance == null
            ? null
            : CardDataManager.Instance.GetCardMasterData(entry.CardId);

        if (master == null)
        {
            SetCell(row, 0, "알 수 없는 카드", UiPalette.Text3);
            SetCell(row, 1, "-", UiPalette.Text3);
        }
        else
        {
            Color rarityColor = CardStyle.GetRarityColor(master.CardType, master.CardGrade);

            SetCell(row, 0, PlayerLabelFormat.NameWithYear(master.Name, master.Year), rarityColor);
            SetCell(row, 1, GetRarityLabel(master.CardType, master.CardGrade), rarityColor);
        }

        SetCell(row, 2, entry.GachaType == GachaType.Signature ? "시그니쳐" : "일반", UiPalette.Text2);
        SetCell(row, 3, entry.RolledAtLocal.ToString(TimeFormat), UiPalette.Text2);
    }

    private void SetCell(int row, int column, string text, Color color)
    {
        TextMeshProUGUI cell = _cells[row, column];

        cell.text = text;
        cell.color = color;
    }

    private void TurnPage(int step)
    {
        _page += step;
        ShowPage();
    }

    //더 갈 수 없는 쪽이면 화살표를 회색으로 두고 누르지 못하게 한다
    private static void SetArrow(Button button, TextMeshProUGUI label, bool enabled)
    {
        button.interactable = enabled;
        label.color = enabled ? UiPalette.Text1 : UiPalette.TextOff;
        button.targetGraphic.color = enabled ? UiPalette.BgRow : UiPalette.WithAlpha(UiPalette.BgRow, 0.45f);
    }

    //Dim(누르면 닫힘) > Window > [제목 · 닫기, 머리줄, 10줄, 빈 기록 안내, < 쪽 >]
    private void BuildPanel()
    {
        UiBuilder.Stretch(Root, 0f, 0f, 0f, 0f);
        _fade = gameObject.AddComponent<CanvasGroup>();

        Image dim = UiBuilder.CreateImage(Root, "Dim", UiPalette.WithAlpha(UiPalette.BgApp, 0.86f));
        UiBuilder.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
        dim.raycastTarget = true;
        Button dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(Close);

        Image window = UiBuilder.CreateImage(Root, "Window", UiPalette.BgPanel, UiSprites.PanelLarge, Image.Type.Sliced);
        _window = window.rectTransform;
        _window.anchorMin = new Vector2(0.5f, 0.5f);
        _window.anchorMax = new Vector2(0.5f, 0.5f);
        _window.pivot = new Vector2(0.5f, 0.5f);
        _window.anchoredPosition = Vector2.zero;
        _window.sizeDelta = new Vector2(WindowWidth, WindowHeight);
        window.raycastTarget = true;

        TextMeshProUGUI title = UiBuilder.CreateText(_window, "Title", 44f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, Padding, TitleTop, 500f, TitleHeight);
        title.text = "뽑기 기록";

        Button closeButton = UiBuilder.CreateButton(_window, "CloseButton", "닫기", 30f, false, Close);
        UiBuilder.PlaceRight(closeButton.GetComponent<RectTransform>(), Padding, TitleTop, CloseWidth, CloseHeight);

        BuildHeader();
        BuildRows();

        _emptyText = UiBuilder.CreateText(_window, "Empty", RowFontSize, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(_emptyText.rectTransform, Padding, RowTop, WindowWidth - Padding * 2f, RowHeight * 3f);
        _emptyText.text = "아직 뽑기 기록이 없습니다";

        BuildFooter();
    }

    //열 제목 + 그 아래 굵은 구분선
    private void BuildHeader()
    {
        float x = Padding;

        for (int column = 0; column < ColumnCount; column++)
        {
            TextMeshProUGUI label = UiBuilder.CreateText(_window, "Header" + column, 28f, UiPalette.Text3,
                GetColumnAlignment(column));
            UiBuilder.Place(label.rectTransform, x, HeaderTop, ColumnWidths[column], HeaderHeight);
            label.text = ColumnTitles[column];
            x += ColumnWidths[column];
        }

        Image line = UiBuilder.CreateImage(_window, "HeaderLine", UiPalette.LineBorder);
        UiBuilder.Place(line.rectTransform, Padding, RowTop - 2f, WindowWidth - Padding * 2f, 2f);
    }

    //줄마다 칸 4개 + 아래 가는 구분선. 세로줄은 긋지 않는다
    private void BuildRows()
    {
        for (int row = 0; row < RowsPerPage; row++)
        {
            RectTransform rowRect = UiBuilder.CreateRect(_window, "Row" + row);
            UiBuilder.Place(rowRect, Padding, RowTop + row * RowHeight, WindowWidth - Padding * 2f, RowHeight);
            _rows[row] = rowRect.gameObject;

            float x = 0f;

            for (int column = 0; column < ColumnCount; column++)
            {
                TextMeshProUGUI cell = UiBuilder.CreateText(rowRect, "Cell" + column,
                    column == 3 ? TimeFontSize : RowFontSize, UiPalette.Text2, GetColumnAlignment(column));
                UiBuilder.Place(cell.rectTransform, x, 0f, ColumnWidths[column], RowHeight);
                cell.overflowMode = TextOverflowModes.Ellipsis;
                _cells[row, column] = cell;
                x += ColumnWidths[column];
            }

            Image line = UiBuilder.CreateImage(rowRect, "Line", UiPalette.LineHair);
            UiBuilder.Place(line.rectTransform, 0f, RowHeight - 2f, WindowWidth - Padding * 2f, 2f);
        }
    }

    //가운데 쪽 번호, 양옆 < > 화살표
    private void BuildFooter()
    {
        float contentWidth = WindowWidth - Padding * 2f;

        _previousButton = UiBuilder.CreateButton(_window, "PreviousPage", "<", 44f, false, () => TurnPage(-1),
            out _previousLabel);
        UiBuilder.Place(_previousButton.GetComponent<RectTransform>(), Padding, FooterTop, ArrowButtonWidth, FooterHeight);

        _nextButton = UiBuilder.CreateButton(_window, "NextPage", ">", 44f, false, () => TurnPage(1),
            out _nextLabel);
        UiBuilder.Place(_nextButton.GetComponent<RectTransform>(), Padding + contentWidth - ArrowButtonWidth,
            FooterTop, ArrowButtonWidth, FooterHeight);

        _pageText = UiBuilder.CreateText(_window, "Page", 34f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Place(_pageText.rectTransform, Padding + ArrowButtonWidth, FooterTop,
            contentWidth - ArrowButtonWidth * 2f, FooterHeight);
    }

    private static IReadOnlyList<GachaHistoryEntry> GetHistory()
    {
        if (GachaManager.Instance == null)
            return System.Array.Empty<GachaHistoryEntry>();

        return GachaManager.Instance.History;
    }

    //카드 · 등급 · 뽑기는 왼쪽, 시간은 오른쪽 정렬
    private static TextAlignmentOptions GetColumnAlignment(int column)
    {
        return column == 3 ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
    }

    private static string GetRarityLabel(CardType cardType, CardGrade cardGrade)
    {
        return cardType switch
        {
            CardType.Signature => "시그니쳐",
            CardType.GoldenGlove => "골든글러브",
            _ => cardGrade switch
            {
                CardGrade.Star5 => "5성",
                CardGrade.Star4 => "4성",
                _ => "3성"
            }
        };
    }
}
