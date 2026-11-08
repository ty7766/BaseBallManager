using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이름 열은 고정하고 숫자 열만 가로 스크롤되는 기록 표 (명세 ③).
/// 타자·투수 기록이 컬럼 수만 다르므로 같은 표를 재사용한다
/// </summary>
public class StatTableView : UiWidgetBehaviour
{
    private const float RowHeight = 72f;
    private const float HeaderHeight = 56f;
    private const float NumberSize = 32f;

    [SerializeField] private RectTransform _nameColumn;
    [SerializeField] private RectTransform _numberContent;
    [SerializeField] private string[] _numericHeaders;
    [SerializeField] private float _nameWidth;
    [SerializeField] private float _cellWidth;
    private readonly List<TextMeshProUGUI> _nameCells = new List<TextMeshProUGUI>(16);
    private readonly List<TextMeshProUGUI[]> _numberCells = new List<TextMeshProUGUI[]>(16);
    private readonly List<Image> _rowBackgrounds = new List<Image>(16);

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static StatTableView Bake(Transform parent, string[] numericHeaders, float nameWidth, float cellWidth,
        string nameHeaderText = "선수")
    {
        StatTableView view = CreateNode<StatTableView>(parent, "StatTable");

        view.BuildWidget(parent, numericHeaders, nameWidth, cellWidth, nameHeaderText);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(Transform parent, string[] numericHeaders, float nameWidth, float cellWidth,
        string nameHeaderText = "선수")
    {
        _numericHeaders = numericHeaders;
        _nameWidth = nameWidth;
        _cellWidth = cellWidth;

        _nameColumn = UiBuilder.CreateRect(Root, "NameColumn");
        _nameColumn.anchorMin = new Vector2(0f, 0f);
        _nameColumn.anchorMax = new Vector2(0f, 1f);
        _nameColumn.pivot = new Vector2(0f, 1f);
        _nameColumn.offsetMin = new Vector2(0f, 0f);
        _nameColumn.offsetMax = new Vector2(nameWidth, 0f);

        TextMeshProUGUI nameHeader = UiBuilder.CreateText(_nameColumn, "NameHeader", 28f, UiPalette.Text3);
        UiBuilder.Place(nameHeader.rectTransform, 12f, 14f, nameWidth - 16f, 32f);
        nameHeader.text = nameHeaderText;

        ScrollRect scroll = UiBuilder.CreateTableScroll(Root, "NumberScroll", out _numberContent);
        RectTransform scrollRect = scroll.GetComponent<RectTransform>();

        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.pivot = new Vector2(0f, 1f);
        scrollRect.offsetMin = new Vector2(nameWidth, 0f);
        scrollRect.offsetMax = new Vector2(0f, 0f);
        scroll.vertical = false;

        RectTransform headerRow = UiBuilder.CreateRect(_numberContent, "HeaderRow");
        UiBuilder.Place(headerRow, 0f, 0f, numericHeaders.Length * cellWidth, HeaderHeight);

        for (int i = 0; i < numericHeaders.Length; i++)
        {
            TextMeshProUGUI cell = UiBuilder.CreateText(headerRow, "Header", 26f,
                UiPalette.Text3, TextAlignmentOptions.Center);

            UiBuilder.Place(cell.rectTransform, i * cellWidth, 14f, cellWidth, 32f);
            cell.text = numericHeaders[i];
        }

        _numberContent.sizeDelta = new Vector2(numericHeaders.Length * cellWidth, HeaderHeight);
    }

    /// <summary>
    /// 행 전체를 교체한다. cells의 각 배열 길이는 헤더 수와 같아야 한다
    /// </summary>
    public void SetRows(IReadOnlyList<string> names, IReadOnlyList<string[]> cells)
    {
        if (names == null || cells == null || names.Count != cells.Count)
        {
            Debug.LogError("[StatTableView]: 이름과 셀 목록의 길이가 맞지 않습니다");
            return;
        }

        EnsureRowCount(names.Count);

        for (int row = 0; row < _nameCells.Count; row++)
        {
            bool used = row < names.Count;

            _nameCells[row].gameObject.SetActive(used);
            _rowBackgrounds[row].gameObject.SetActive(used);

            TextMeshProUGUI[] numbers = _numberCells[row];

            for (int column = 0; column < numbers.Length; column++)
                numbers[column].gameObject.SetActive(used);

            if (!used)
                continue;

            _nameCells[row].text = names[row];

            string[] values = cells[row];

            for (int column = 0; column < numbers.Length; column++)
                numbers[column].text = column < values.Length ? values[column] : string.Empty;
        }

        float contentHeight = HeaderHeight + names.Count * RowHeight;

        _numberContent.sizeDelta = new Vector2(_numericHeaders.Length * _cellWidth, contentHeight);
    }

    //부족한 행을 만들어 둔다 (행 수는 경기마다 바뀐다)
    private void EnsureRowCount(int count)
    {
        while (_nameCells.Count < count)
        {
            int index = _nameCells.Count;
            float y = HeaderHeight + index * RowHeight;

            Image background = UiBuilder.CreateImage(_nameColumn, "RowBackground",
                index % 2 == 0 ? UiPalette.WithAlpha(UiPalette.BgRow, 0.5f) : UiPalette.WithAlpha(UiPalette.BgRow, 0.2f));
            UiBuilder.PlaceRow(background.rectTransform, y, RowHeight);
            _rowBackgrounds.Add(background);

            TextMeshProUGUI name = UiBuilder.CreateText(_nameColumn, "Name", 30f, UiPalette.Text1);
            UiBuilder.Place(name.rectTransform, 12f, y + 18f, _nameWidth - 16f, 36f);
            _nameCells.Add(name);

            TextMeshProUGUI[] numbers = new TextMeshProUGUI[_numericHeaders.Length];

            for (int column = 0; column < numbers.Length; column++)
            {
                numbers[column] = UiBuilder.CreateText(_numberContent, "Cell", NumberSize,
                    UiPalette.Text2, TextAlignmentOptions.Center);
                UiBuilder.Place(numbers[column].rectTransform, column * _cellWidth, y + 18f, _cellWidth, 36f);
            }

            _numberCells.Add(numbers);
        }
    }
}
