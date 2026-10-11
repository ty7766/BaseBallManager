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
    private const int DefaultColumns = 3;
    private const float ColumnGap = 16f;
    private const float RowGap = 16f;

    /// <summary>
    /// 현재 깔려 있는 카드 수
    /// </summary>
    public int Count { get; private set; }

    [SerializeField] private RectTransform _content;
    [SerializeField] private TextMeshProUGUI _emptyText;
    private readonly List<CardCellView> _cells = new List<CardCellView>(32);
    private readonly HashSet<int> _selected = new HashSet<int>();
    [SerializeField] private int _columns;

    private Action<int> _onCardClicked;

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
        int used = 0;

        if (instanceIds != null)
        {
            for (int i = 0; i < instanceIds.Count; i++)
            {
                if (!CardDisplayInfo.TryCreate(instanceIds[i], out CardDisplayInfo info))
                    continue;

                CardCellView cell = EnsureCell(used);

                cell.SetVisible(true);
                cell.SetCard(info);
                cell.SetGridPosition(used % _columns, used / _columns, ColumnGap, RowGap);
                cell.SetSelected(_selected.Contains(info.InstanceId));

                used++;
            }
        }

        for (int i = used; i < _cells.Count; i++)
            _cells[i].SetVisible(false);

        Count = used;

        int rows = (used + _columns - 1) / _columns;

        _content.sizeDelta = new Vector2(0f, rows * (CardCellView.CellHeight + RowGap));
        _emptyText.gameObject.SetActive(used == 0);
    }

    /// <summary>
    /// 깔려 있는 카드를 앞에서부터 차례로 열어 보인다
    /// </summary>
    public void PlayRevealSequence(float stepSeconds = 0.08f)
    {
        for (int i = 0; i < Count; i++)
            _cells[i].PlayReveal(i * stepSeconds);
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
    /// 빈 목록일 때 보여줄 문구
    /// </summary>
    public void SetEmptyMessage(string message)
    {
        _emptyText.text = message;
    }

    //타일은 필요한 만큼만 만들고 다음 갱신에서 재사용한다
    private CardCellView EnsureCell(int index)
    {
        while (_cells.Count <= index)
        {
            CardCellView created = new CardCellView(_content);

            created.SetClickHandler(_onCardClicked == null ? null : OnCellClicked);
            _cells.Add(created);
        }

        return _cells[index];
    }

    private void OnCellClicked(int instanceId)
    {
        _onCardClicked?.Invoke(instanceId);
    }
}
