using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 선택 모달. 강화 · 훈련 · 조합 · 분해 · 라인업이 같은 화면을 공유한다.
/// 필요 장수가 1장이면 누르는 즉시 확정된다
/// </summary>
public class CardPickerScreen : UiScreenBehaviour
{
    private Action _onCancel;

    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _selectionText;
    [SerializeField] private CardFilterBarView _filterBar;
    [SerializeField] private CardGridView _grid;
    [SerializeField] private Button _confirmButton;

    private List<int> _selectedIds = new List<int>(4);
    private List<int> _candidateIds = new List<int>(64);

    private Func<CardDisplayInfo, bool> _canSelect;
    private Action<List<int>> _onConfirm;
    private int _requiredCount = 1;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onCancel)
    {
        _onCancel = onCancel;
    }

    /// <summary>
    /// 선택 모달을 연다. canSelect가 false인 카드는 흐리게 그리고 고를 수 없다
    /// </summary>
    public void Open(string title, int requiredCount,
        Func<CardDisplayInfo, bool> canSelect, Action<List<int>> onConfirm)
    {
        _titleText.text = title;
        _requiredCount = Mathf.Max(1, requiredCount);
        _canSelect = canSelect;
        _onConfirm = onConfirm;

        _selectedIds.Clear();
        Refresh();
    }

    protected override void BindEvents()
    {
        Bind(_confirmButton, OnConfirmClicked);
        BindButton("CancelButton", OnCancelClicked);
    }

    protected override void Build()
    {
        Image dim = UiBuilder.CreateImage(Root, "Dim", UiPalette.WithAlpha(UiPalette.BgApp, 0.96f));
        UiBuilder.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
        dim.raycastTarget = true;

        _titleText = UiBuilder.CreateText(Root, "Title", 44f, UiPalette.Text1);
        UiBuilder.Place(_titleText.rectTransform, 32f, 32f, 700f, 56f);

        _selectionText = UiBuilder.CreateText(Root, "Selection", 32f, UiPalette.AccentGold, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_selectionText.rectTransform, 32f, 40f, 320f, 44f);

        _filterBar = CardFilterBarView.Bake(Root);
        UiBuilder.Place(_filterBar.Root, 32f, 100f, 1016f, CardFilterBarView.BarHeight);
        _filterBar.SetChangedHandler(Refresh);

        _grid = CardGridView.Bake(Root);
        UiBuilder.Stretch(_grid.Root, 32f, 100f + CardFilterBarView.BarHeight + 16f, 32f, 200f);
        _grid.SetClickHandler(OnCardClicked);
        _grid.SetEmptyMessage("고를 수 있는 카드가 없습니다");

        _confirmButton = UiBuilder.CreateButton(Root, "ConfirmButton", "확정", 36f, true, OnConfirmClicked);
        RectTransform confirmRect = _confirmButton.GetComponent<RectTransform>();

        confirmRect.anchorMin = new Vector2(0f, 0f);
        confirmRect.anchorMax = new Vector2(0.5f, 0f);
        confirmRect.pivot = new Vector2(0f, 0f);
        confirmRect.offsetMin = new Vector2(32f, 40f);
        confirmRect.offsetMax = new Vector2(-12f, 160f);

        Button cancelButton = UiBuilder.CreateButton(Root, "CancelButton", "취소", 36f, false, OnCancelClicked);
        RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();

        cancelRect.anchorMin = new Vector2(0.5f, 0f);
        cancelRect.anchorMax = new Vector2(1f, 0f);
        cancelRect.pivot = new Vector2(0f, 0f);
        cancelRect.offsetMin = new Vector2(12f, 40f);
        cancelRect.offsetMax = new Vector2(-32f, 160f);
    }

    //필터를 통과하고 선택 가능한 카드만 깐다
    private void Refresh()
    {
        if (InventoryManager.Instance == null)
            return;

        _candidateIds.Clear();

        List<CardInstance> filtered = InventoryManager.Instance.GetFiltered(_filterBar.CurrentFilter);

        foreach (CardInstance card in filtered)
        {
            if (!CardDisplayInfo.TryCreate(card.InstanceId, out CardDisplayInfo info))
                continue;

            if (_canSelect != null && !_canSelect(info))
                continue;

            _candidateIds.Add(card.InstanceId);
        }

        _grid.SetCards(_candidateIds);
        _grid.SetSelection(_selectedIds);

        _selectionText.text = _requiredCount == 1
            ? $"{_candidateIds.Count}장 중 1장 선택"
            : $"{_selectedIds.Count} / {_requiredCount} 선택";

        _confirmButton.gameObject.SetActive(_requiredCount > 1);
        _confirmButton.interactable = _selectedIds.Count == _requiredCount;
    }

    private void OnCardClicked(int instanceId)
    {
        if (_requiredCount == 1)
        {
            _selectedIds.Clear();
            _selectedIds.Add(instanceId);
            Confirm();
            return;
        }

        if (_selectedIds.Contains(instanceId))
            _selectedIds.Remove(instanceId);
        else if (_selectedIds.Count < _requiredCount)
            _selectedIds.Add(instanceId);

        _grid.SetSelection(_selectedIds);

        _selectionText.text = $"{_selectedIds.Count} / {_requiredCount} 선택";
        _confirmButton.interactable = _selectedIds.Count == _requiredCount;
    }

    private void OnConfirmClicked()
    {
        if (_selectedIds.Count != _requiredCount)
            return;

        Confirm();
    }

    private void Confirm()
    {
        Action<List<int>> callback = _onConfirm;

        _onConfirm = null;
        callback?.Invoke(new List<int>(_selectedIds));
    }

    private void OnCancelClicked()
    {
        _onConfirm = null;
        _onCancel?.Invoke();
    }
}
