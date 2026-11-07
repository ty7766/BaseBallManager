using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 타일을 3열로 깔고 스크롤한다. 인벤토리·뽑기 결과·카드 선택 모달이 공유한다
/// </summary>
public class CardGridView : UiWidgetBehaviour
{
    private const int DefaultColumns = 4;
    //4열일 때의 칸 폭. 카드 그림은 CellWidth 기준으로 조립하고 스케일로 줄인다
    private const float DefaultCellWidth = 242f;
    private const float ColumnGap = 16f;
    private const float RowGap = 16f;
    //화면 좌우 여백 32 를 뺀 설계 폭. 가운데 정렬 기준이다
    private const float GridWidth = 1016f;
    private const float CenterNudgeUp = 60f;
    //스크롤바는 칸 오른쪽 바깥(화면 여백)에 붙인다
    private const float ScrollbarGap = 12f;
    private const float ScrollbarWidth = 8f;

    /// <summary>
    /// 현재 깔려 있는 카드 수
    /// </summary>
    public int Count { get; private set; }

    [SerializeField] private RectTransform _content;
    [SerializeField] private TextMeshProUGUI _emptyText;
    private readonly List<CardCellView> _cells = new List<CardCellView>(32);
    private readonly HashSet<int> _selected = new HashSet<int>();
    [SerializeField] private int _columns;
    [SerializeField] private float _cellWidth = DefaultCellWidth;
    //0보다 크면 이 높이 안에서 세로 가운데로 모은다 (뽑기 결과)
    [SerializeField] private float _centerInHeight;

    private Action<int> _onCardClicked;
    private Func<CardDisplayInfo, string> _blockedReason;
    private Action<int> _onCardLongPressed;
    private Scrollbar _scrollbar;
    private ScrollRect _scroll;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static CardGridView Bake(Transform parent, int columns = DefaultColumns)
    {
        CardGridView view = CreateNode<CardGridView>(parent, "CardGrid");

        view.BuildWidget(parent, columns);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(Transform parent, int columns = DefaultColumns)
    {
        _columns = Mathf.Max(1, columns);

        ScrollRect scroll = UiBuilder.CreateVerticalScroll(Root, "GridScroll", out _content);
        UiBuilder.Stretch(scroll.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        _emptyText = UiBuilder.CreateText(Root, "Empty", 34f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Stretch(_emptyText.rectTransform, 0f, 0f, 0f, 0f);
        _emptyText.text = "카드가 없습니다";
    }

    /// <summary>
    /// 열 수와 칸 폭을 바꾼다 (단일 뽑기 1장 크게 · 10연 5열 등)
    /// </summary>
    public void SetLayout(int columns, float cellWidth, float centerInHeight = 0f)
    {
        _columns = Mathf.Max(1, columns);
        _cellWidth = Mathf.Max(1f, cellWidth);
        _centerInHeight = centerInHeight;
    }

    /// <summary>
    /// 카드를 2초 길게 누르면 instanceId를 넘긴다 (상세 보기)
    /// </summary>
    public void SetLongPressHandler(Action<int> onCardLongPressed)
    {
        _onCardLongPressed = onCardLongPressed;

        foreach (CardCellView cell in _cells)
            cell.SetLongPressHandler(onCardLongPressed);
    }

    /// <summary>
    /// 고를 수 없는 카드의 사유를 돌려주는 함수. null 이나 빈 문자열이면 고를 수 있다
    /// </summary>
    public void SetBlockedReason(Func<CardDisplayInfo, string> blockedReason)
    {
        _blockedReason = blockedReason;
    }

    /// <summary>
    /// 카드를 눌렀을 때 instanceId를 넘긴다
    /// </summary>
    public void SetClickHandler(Action<int> onCardClicked)
    {
        _onCardClicked = onCardClicked;

        foreach (CardCellView cell in _cells)
            cell.SetClickHandler(onCardClicked == null ? null : OnCellClicked);
    }

    /// <summary>
    /// 목록을 교체한다. 조회에 실패한 instanceId는 건너뛴다
    /// </summary>
    public void SetCards(IReadOnlyList<int> instanceIds)
    {
        EnsureScrollbar();

        int used = 0;
        float cellWidth = _cellWidth > 0f ? _cellWidth : DefaultCellWidth;
        float cellHeight = cellWidth * CardView.BaseHeight / CardView.BaseWidth;
        float rowWidth = _columns * (cellWidth + ColumnGap) - ColumnGap;
        float offsetX = Mathf.Max(0f, (GridWidth - rowWidth) * 0.5f);
        float offsetY = 0f;

        if (_centerInHeight > 0f && instanceIds != null && instanceIds.Count > 0)
        {
            int plannedRows = (instanceIds.Count + _columns - 1) / _columns;
            float blockHeight = plannedRows * (cellHeight + RowGap) - RowGap;

            //가운데에서 살짝 위로 올린다
            offsetY = Mathf.Max(0f, (_centerInHeight - blockHeight) * 0.5f - CenterNudgeUp);
        }

        if (instanceIds != null)
        {
            for (int i = 0; i < instanceIds.Count; i++)
            {
                if (!CardDisplayInfo.TryCreate(instanceIds[i], out CardDisplayInfo info))
                    continue;

                CardCellView cell = EnsureCell(used);

                cell.SetVisible(true);
                cell.SetCard(info);
                cell.SetGridPosition(used % _columns, used / _columns, ColumnGap, RowGap,
                    cellWidth, offsetX, offsetY);
                cell.SetSelected(_selected.Contains(info.InstanceId));
                cell.SetBlocked(_blockedReason == null ? null : _blockedReason(info));

                used++;
            }
        }

        for (int i = used; i < _cells.Count; i++)
            _cells[i].SetVisible(false);

        Count = used;

        int rows = (used + _columns - 1) / _columns;

        _content.sizeDelta = new Vector2(0f, offsetY + rows * (cellHeight + RowGap));
        _emptyText.gameObject.SetActive(used == 0);
    }

    /// <summary>
    /// 선택 표시를 교체한다 (단일·다중 선택 공용)
    /// </summary>
    public void SetSelection(IReadOnlyCollection<int> selectedInstanceIds)
    {
        _selected.Clear();

        if (selectedInstanceIds != null)
        {
            foreach (int instanceId in selectedInstanceIds)
                _selected.Add(instanceId);
        }

        for (int i = 0; i < Count; i++)
            _cells[i].SetSelected(_selected.Contains(_cells[i].InstanceId));
    }

    /// <summary>
    /// 맨 위로 되돌린다 (화면을 떠날 때)
    /// </summary>
    public void ScrollToTop()
    {
        ScrollRect scroll = GetScroll();

        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// 빈 목록일 때 보여줄 문구
    /// </summary>
    public void SetEmptyMessage(string message)
    {
        _emptyText.text = message;
    }

    //오른쪽 맨 끝 열에서도 어디까지 내렸는지 보이게 한다. 이미 구운 화면 프리팹도 다시 굽지 않게 처음 깔 때 만든다
    private void EnsureScrollbar()
    {
        if (_scrollbar != null)
            return;

        ScrollRect scroll = GetScroll();

        if (scroll != null)
            _scrollbar = UiBuilder.AddVerticalScrollbar(scroll, ScrollbarGap, ScrollbarWidth);
    }

    //스크롤 영역은 베이크 때 만들어지고 참조는 직렬화하지 않았으므로 처음 쓸 때 찾아 둔다
    private ScrollRect GetScroll()
    {
        if (_scroll == null)
            _scroll = GetComponentInChildren<ScrollRect>(true);

        return _scroll;
    }

    //타일은 필요한 만큼만 만들고 다음 갱신에서 재사용한다
    private CardCellView EnsureCell(int index)
    {
        while (_cells.Count <= index)
        {
            CardCellView created = new CardCellView(_content);

            created.SetClickHandler(_onCardClicked == null ? null : OnCellClicked);
            created.SetLongPressHandler(_onCardLongPressed);
            _cells.Add(created);
        }

        return _cells[index];
    }

    private void OnCellClicked(int instanceId)
    {
        _onCardClicked?.Invoke(instanceId);
    }
}
