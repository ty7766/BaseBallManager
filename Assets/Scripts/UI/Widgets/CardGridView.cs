using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 타일을 여러 열로 깔고 스크롤한다. 인벤토리·뽑기 결과·카드 선택 모달이 공유한다.
/// 보이는 줄(+위아래 한 줄)만큼의 칸만 만들어 스크롤할 때 돌려 쓴다. 카드 수만큼 칸을 켜 두면 RectMask2D 가 매 프레임 전부를 훑는다
/// </summary>
public class CardGridView : UiWidgetBehaviour
{
    //보이는 줄 위아래로 미리 채워 둘 줄 수. 빠르게 넘겨도 빈 칸이 비치지 않게 한다
    private const int BufferRows = 1;
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
    //칸 위아래 바깥에 흰 실선을 그어 스크롤 영역을 가른다
    private const float EdgeLineGap = 14f;
    private const float EdgeLineThickness = 4f;
    private const float EdgeLineAlpha = 0.85f;

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
    private readonly List<RectTransform> _leadingTiles = new List<RectTransform>(2);
    private bool _checkSelection;
    private bool _edgeLinesBuilt;
    //깔 카드 전체. 칸은 이 중 보이는 몇 장에만 붙는다
    private readonly List<CardDisplayInfo> _items = new List<CardDisplayInfo>(64);
    //칸마다 지금 맡은 자리 번호(앞 타일 포함). -1 이면 비어 있다
    private readonly List<int> _cellSlots = new List<int>(32);
    private readonly List<int> _wantedSlots = new List<int>(32);
    private float _layoutCellWidth;
    private float _rowPitch;
    private float _offsetX;
    private float _offsetY;

    //화면 높이가 바뀌면(세로가 긴 기기) 보이는 줄 수도 바뀐다
    private void OnRectTransformDimensionsChange()
    {
        if (_items.Count > 0)
            RefreshVisibleCells();
    }

    protected override void BindEvents()
    {
        ScrollRect scroll = GetScroll();

        if (scroll != null)
            scroll.onValueChanged.AddListener(OnScrolled);
    }

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
        EnsureEdgeLines();

        _items.Clear();

        if (instanceIds != null)
        {
            for (int i = 0; i < instanceIds.Count; i++)
            {
                if (CardDisplayInfo.TryCreate(instanceIds[i], out CardDisplayInfo info))
                    _items.Add(info);
            }
        }

        int leading = _leadingTiles.Count;
        float cellWidth = _cellWidth > 0f ? _cellWidth : DefaultCellWidth;
        float cellHeight = cellWidth * CardView.BaseHeight / CardView.BaseWidth;
        float rowWidth = _columns * (cellWidth + ColumnGap) - ColumnGap;

        _layoutCellWidth = cellWidth;
        _rowPitch = cellHeight + RowGap;
        _offsetX = Mathf.Max(0f, (GridWidth - rowWidth) * 0.5f);
        _offsetY = 0f;

        if (_centerInHeight > 0f && _items.Count > 0)
        {
            int plannedRows = (_items.Count + leading + _columns - 1) / _columns;
            float blockHeight = plannedRows * _rowPitch - RowGap;

            //가운데에서 살짝 위로 올린다
            _offsetY = Mathf.Max(0f, (_centerInHeight - blockHeight) * 0.5f - CenterNudgeUp);
        }

        for (int i = 0; i < leading; i++)
        {
            CardCellView.PlaceInGrid(_leadingTiles[i], i % _columns, i / _columns, ColumnGap, RowGap,
                cellWidth, _offsetX, _offsetY);
        }

        Count = _items.Count;

        int rows = (leading + Count + _columns - 1) / _columns;

        _content.sizeDelta = new Vector2(0f, _offsetY + rows * _rowPitch);
        _emptyText.gameObject.SetActive(leading + Count == 0);

        for (int i = 0; i < _cellSlots.Count; i++)
            _cellSlots[i] = -1;

        RefreshVisibleCells();
    }

    /// <summary>
    /// 카드 앞 칸에 카드가 아닌 타일을 끼운다 (강화 전용 카드). 타일은 CardCellView 규격으로 조립해 둔다
    /// </summary>
    public void SetLeadingTiles(IReadOnlyList<RectTransform> tiles)
    {
        foreach (RectTransform tile in _leadingTiles)
        {
            if (tile != null)
                tile.gameObject.SetActive(false);
        }

        _leadingTiles.Clear();

        if (tiles == null)
            return;

        foreach (RectTransform tile in tiles)
        {
            if (tile == null)
                continue;

            tile.SetParent(_content, false);
            tile.SetAsFirstSibling();
            tile.gameObject.SetActive(true);
            _leadingTiles.Add(tile);
        }
    }

    /// <summary>
    /// 선택 표시를 테두리 대신 어둡게 덮고 가운데 체크로 한다
    /// </summary>
    public void SetCheckSelection(bool checkSelection)
    {
        _checkSelection = checkSelection;

        foreach (CardCellView cell in _cells)
            cell.SetCheckStyle(checkSelection);
    }

    /// <summary>
    /// 깔린 카드의 고를 수 없는 사유만 다시 계산한다 (선택이 바뀌어 한도가 찼을 때)
    /// </summary>
    public void RefreshBlocked()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            if (_cellSlots[i] < 0)
                continue;

            string reason = _blockedReason != null && CardDisplayInfo.TryCreate(_cells[i].InstanceId, out CardDisplayInfo info)
                ? _blockedReason(info)
                : null;

            _cells[i].SetBlocked(reason);
        }
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

        for (int i = 0; i < _cells.Count; i++)
        {
            if (_cellSlots[i] >= 0)
                _cells[i].SetSelected(_selected.Contains(_cells[i].InstanceId), true);
        }
    }

    /// <summary>
    /// 카드 1장만 다시 그린다 (목록은 그대로고 한 장의 단계 · OVR 만 바뀌었을 때)
    /// </summary>
    public void RefreshCard(int instanceId)
    {
        int itemIndex = IndexOfItem(instanceId);

        if (itemIndex < 0 || !CardDisplayInfo.TryCreate(instanceId, out CardDisplayInfo info))
            return;

        _items[itemIndex] = info;

        int slot = _leadingTiles.Count + itemIndex;
        int cellIndex = _cellSlots.IndexOf(slot);

        if (cellIndex >= 0)
            BindCell(cellIndex, slot);
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

    //모든 카드 목록에 공통. 구운 화면을 다시 굽지 않게 처음 깔 때 만든다
    private void EnsureEdgeLines()
    {
        if (_edgeLinesBuilt)
            return;

        _edgeLinesBuilt = true;
        CreateEdgeLine("GridTopLine", true);
        CreateEdgeLine("GridBottomLine", false);
    }

    //칸 바로 바깥에 붙는다. 칸이 늘거나 옮겨지면 같이 따라간다
    private void CreateEdgeLine(string lineName, bool top)
    {
        Image line = UiBuilder.CreateImage(Root, lineName, UiPalette.WithAlpha(UiPalette.Text1, EdgeLineAlpha));
        RectTransform rect = line.rectTransform;
        float edge = top ? 1f : 0f;

        rect.anchorMin = new Vector2(0f, edge);
        rect.anchorMax = new Vector2(1f, edge);
        rect.pivot = new Vector2(0.5f, top ? 0f : 1f);
        rect.anchoredPosition = new Vector2(0f, top ? EdgeLineGap : -EdgeLineGap);
        rect.sizeDelta = new Vector2(0f, EdgeLineThickness);
        line.raycastTarget = false;
    }

    //스크롤 영역은 베이크 때 만들어지고 참조는 직렬화하지 않았으므로 처음 쓸 때 찾아 둔다
    private ScrollRect GetScroll()
    {
        if (_scroll == null)
            _scroll = GetComponentInChildren<ScrollRect>(true);

        return _scroll;
    }

    //보이는 자리[first, end)만 칸에 붙인다. 자리 번호를 칸 수로 나눈 나머지로 칸을 정해 한 줄 넘길 때 그 줄 칸만 다시 채운다
    private void RefreshVisibleCells()
    {
        int leading = _leadingTiles.Count;
        int totalSlots = leading + _items.Count;

        GetVisibleSlotRange(totalSlots, out int firstSlot, out int endSlot);
        EnsureCellCount(endSlot - firstSlot);

        int pool = _cells.Count;

        _wantedSlots.Clear();

        for (int i = 0; i < pool; i++)
            _wantedSlots.Add(-1);

        for (int slot = Mathf.Max(firstSlot, leading); slot < endSlot; slot++)
            _wantedSlots[slot % pool] = slot;

        for (int i = 0; i < pool; i++)
        {
            int wanted = _wantedSlots[i];

            if (wanted < 0)
            {
                _cells[i].SetVisible(false);
                _cellSlots[i] = -1;
                continue;
            }

            if (_cellSlots[i] != wanted)
                BindCell(i, wanted);
        }
    }

    //뷰포트 위아래로 BufferRows 줄씩 더 잡는다. 크기를 아직 모르면 전부 잡는다
    private void GetVisibleSlotRange(int totalSlots, out int firstSlot, out int endSlot)
    {
        ScrollRect scroll = GetScroll();
        float viewHeight = scroll != null && scroll.viewport != null ? scroll.viewport.rect.height : 0f;

        if (viewHeight <= 0f || _rowPitch <= 0f)
        {
            firstSlot = 0;
            endSlot = totalSlots;
            return;
        }

        float top = Mathf.Max(0f, _content.anchoredPosition.y) - _offsetY;
        int firstRow = Mathf.Max(0, Mathf.FloorToInt(top / _rowPitch) - BufferRows);
        int lastRow = Mathf.FloorToInt((top + viewHeight) / _rowPitch) + BufferRows;

        firstSlot = Mathf.Min(totalSlots, firstRow * _columns);
        endSlot = Mathf.Min(totalSlots, (lastRow + 1) * _columns);
    }

    //칸 하나에 자리 하나의 카드를 붙인다
    private void BindCell(int cellIndex, int slot)
    {
        CardCellView cell = _cells[cellIndex];
        CardDisplayInfo info = _items[slot - _leadingTiles.Count];

        cell.SetVisible(true);
        cell.SetCard(info);
        cell.SetGridPosition(slot % _columns, slot / _columns, ColumnGap, RowGap,
            _layoutCellWidth, _offsetX, _offsetY);
        cell.SetSelected(_selected.Contains(info.InstanceId));
        cell.SetBlocked(_blockedReason == null ? null : _blockedReason(info));
        _cellSlots[cellIndex] = slot;
    }

    //칸은 보이는 자리 수만큼만 만든다. 칸 수가 늘면 나머지 짝이 바뀌므로 전부 다시 붙인다
    private void EnsureCellCount(int count)
    {
        if (_cells.Count >= count)
            return;

        while (_cells.Count < count)
        {
            CardCellView created = new CardCellView(_content);

            created.SetClickHandler(_onCardClicked == null ? null : OnCellClicked);
            created.SetLongPressHandler(_onCardLongPressed);
            created.SetCheckStyle(_checkSelection);
            created.SetVisible(false);
            _cells.Add(created);
            _cellSlots.Add(-1);
        }

        for (int i = 0; i < _cells.Count; i++)
            _cellSlots[i] = -1;
    }

    private int IndexOfItem(int instanceId)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].InstanceId == instanceId)
                return i;
        }

        return -1;
    }

    private void OnScrolled(Vector2 position)
    {
        RefreshVisibleCells();
    }

    private void OnCellClicked(int instanceId)
    {
        _onCardClicked?.Invoke(instanceId);
    }
}
