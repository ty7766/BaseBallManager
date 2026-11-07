using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보유 카드 목록. 필터(깔때기 → 체크박스 창) · 상세 보기 · 용량 확장을 담당한다
/// </summary>
public class InventoryScreen : UiScreenBehaviour
{
    //제목 아래 줄 오른쪽 필터 버튼
    private const float FilterRowTop = 112f;
    //카드 목록 칸(위아래 여백)과 칸 위아래를 가르는 흰 선
    private const float GridTop = FilterRowTop + CardFilterButtonView.ButtonHeight + 36f;
    private const float GridBottom = 224f;
    private const float EdgeLineGap = 14f;
    private const float EdgeLineThickness = 2f;
    private const float EdgeLineAlpha = 0.55f;

    private Action _onBack;
    private Action<int> _onCardSelected;
    private Action<string, bool> _onNotify;

    [SerializeField] private TextMeshProUGUI _capacityText;
    [SerializeField] private CardFilterButtonView _filterButton;
    [SerializeField] private CardGridView _grid;
    [SerializeField] private Button _expandButton;
    [SerializeField] private TextMeshProUGUI _expandLabel;

    private List<int> _visibleInstanceIds = new List<int>(64);

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onBack, Action<int> onCardSelected, Action<string, bool> onNotify)
    {
        _onBack = onBack;
        _onCardSelected = onCardSelected;
        _onNotify = onNotify;
    }

    /// <summary>
    /// 목록과 용량을 다시 읽는다
    /// </summary>
    public void Refresh()
    {
        if (InventoryManager.Instance == null)
            return;

        _visibleInstanceIds.Clear();

        List<CardInstance> filtered = InventoryManager.Instance.GetFiltered(_filterButton.CurrentFilter);

        foreach (CardInstance card in filtered)
            _visibleInstanceIds.Add(card.InstanceId);

        _grid.SetCards(_visibleInstanceIds);

        int count = InventoryManager.Instance.Count;
        int capacity = InventoryManager.Instance.MaxCapacity;

        _capacityText.text = $"{count} / {capacity}";
        _capacityText.color = count >= capacity ? UiPalette.StateOut : UiPalette.Text2;

        _expandLabel.text = $"칸 확장  +{InventoryManager.Instance.ExpandUnit}\n골드 {InventoryManager.Instance.ExpandGoldCost}";
        _expandButton.interactable = CurrencyManager.Instance != null
            && CurrencyManager.Instance.CanAfford(CurrencyType.Gold, InventoryManager.Instance.ExpandGoldCost);
    }

    protected override void BindEvents()
    {
        _filterButton.SetChangedHandler(Refresh);
        _grid.SetClickHandler(OnCardClicked);
        Bind(_expandButton, OnExpandClicked);
        BindButton("BackButton", OnBackClicked);
    }

    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, 32f, 32f, 500f, 60f);
        title.text = "인벤토리";

        _capacityText = UiBuilder.CreateText(Root, "Capacity", 36f, UiPalette.Text2, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_capacityText.rectTransform, 32f, 40f, 320f, 48f);

        _filterButton = CardFilterButtonView.Bake(Root);
        UiBuilder.PlaceRight(_filterButton.Root, 32f, FilterRowTop, CardFilterButtonView.ButtonWidth,
            CardFilterButtonView.ButtonHeight);
        _filterButton.SetChangedHandler(Refresh);

        _grid = CardGridView.Bake(Root);
        UiBuilder.Stretch(_grid.Root, 32f, GridTop, 32f, GridBottom);

        Image topLine = UiBuilder.CreateImage(Root, "GridTopLine", UiPalette.WithAlpha(UiPalette.Text1, EdgeLineAlpha));
        UiBuilder.PlaceRow(topLine.rectTransform, GridTop - EdgeLineGap - EdgeLineThickness, EdgeLineThickness, 32f);

        Image bottomLine = UiBuilder.CreateImage(Root, "GridBottomLine", UiPalette.WithAlpha(UiPalette.Text1, EdgeLineAlpha));
        RectTransform bottomRect = bottomLine.rectTransform;

        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.offsetMin = new Vector2(32f, GridBottom - EdgeLineGap - EdgeLineThickness);
        bottomRect.offsetMax = new Vector2(-32f, GridBottom - EdgeLineGap);
        _grid.SetClickHandler(OnCardClicked);
        _grid.SetEmptyMessage("조건에 맞는 카드가 없습니다");

        _expandButton = UiBuilder.CreateButton(Root, "ExpandButton", string.Empty, 28f, false,
            OnExpandClicked, out _expandLabel);
        RectTransform expandRect = _expandButton.GetComponent<RectTransform>();

        expandRect.anchorMin = new Vector2(0f, 0f);
        expandRect.anchorMax = new Vector2(0.5f, 0f);
        expandRect.pivot = new Vector2(0f, 0f);
        expandRect.offsetMin = new Vector2(32f, 40f);
        expandRect.offsetMax = new Vector2(-12f, 160f);

        Button backButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, true, OnBackClicked);
        RectTransform backRect = backButton.GetComponent<RectTransform>();

        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(0f, 0f);
        backRect.offsetMin = new Vector2(12f, 40f);
        backRect.offsetMax = new Vector2(-32f, 160f);
    }

    protected override void OnShown()
    {
        Refresh();
    }

    /// <summary>
    /// 필터 창이 열려 있으면 그것부터 닫는다
    /// </summary>
    public override bool HandleBack()
    {
        return _filterButton.ClosePanel();
    }

    //다시 들어오면 필터 없이 맨 위부터
    protected override void OnHidden()
    {
        _filterButton.ResetFilter();
        _grid.ScrollToTop();
    }

    private void OnCardClicked(int instanceId)
    {
        _onCardSelected?.Invoke(instanceId);
    }

    private void OnExpandClicked()
    {
        if (InventoryManager.Instance == null)
            return;

        if (!InventoryManager.Instance.TryExpandCapacityWithGold())
        {
            _onNotify?.Invoke("골드가 부족해 확장하지 못했습니다", false);
            return;
        }

        _onNotify?.Invoke($"보유 한도를 {InventoryManager.Instance.MaxCapacity}칸으로 늘렸습니다", true);
        Refresh();
    }

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }
}
