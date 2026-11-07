using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 깔때기 필터 버튼. 누르면 필터 창이 뜨고, 고른 조건 수를 오른쪽 위 배지로 보여 준다.
/// 인벤토리 · 카드 선택 모달이 같이 쓴다
/// </summary>
public class CardFilterButtonView : UiWidgetBehaviour
{
    /// <summary>
    /// 버튼 크기
    /// </summary>
    public const float ButtonWidth = 200f;
    public const float ButtonHeight = 80f;

    private const string IconSprite = "Common/icon_filter";
    private const float IconSize = 40f;
    private const float BadgeSize = 40f;

    /// <summary>
    /// 지금 적용된 필터. 인벤토리 조회에 그대로 넘긴다
    /// </summary>
    public CardFilter CurrentFilter { get; } = new CardFilter();

    [SerializeField] private Button _button;
    [SerializeField] private Image _badge;
    [SerializeField] private TextMeshProUGUI _badgeText;

    private Action _onChanged;
    private CardFilterPanel _panel;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static CardFilterButtonView Bake(Transform parent)
    {
        CardFilterButtonView view = CreateNode<CardFilterButtonView>(parent, "CardFilterButton");

        view.BuildWidget();

        return view;
    }

    /// <summary>
    /// 필터를 적용할 때 호출할 동작
    /// </summary>
    public void SetChangedHandler(Action onChanged)
    {
        _onChanged = onChanged;
    }

    /// <summary>
    /// 고른 조건을 모두 비우고 열린 창을 닫는다 (화면을 떠날 때)
    /// </summary>
    public void ResetFilter()
    {
        CurrentFilter.Clear();
        ClosePanel();
        RefreshBadge();
    }

    /// <summary>
    /// 열린 필터 창을 닫는다. 열려 있었으면 true (뒤로가기)
    /// </summary>
    public bool ClosePanel()
    {
        if (_panel == null || !_panel.gameObject.activeSelf)
            return false;

        _panel.Close();

        return true;
    }

    protected override void BindEvents()
    {
        Bind(_button, OpenPanel);
    }

    //창은 처음 열 때 화면 맨 위에 만든다
    private void OpenPanel()
    {
        if (_panel == null)
        {
            UiScreenBehaviour screen = GetComponentInParent<UiScreenBehaviour>();

            _panel = CardFilterPanel.Create(screen != null ? screen.Root : Root.parent);
        }

        _panel.Open(CurrentFilter, OnApplied);
    }

    private void OnApplied()
    {
        RefreshBadge();
        _onChanged?.Invoke();
    }

    private void RefreshBadge()
    {
        int count = CurrentFilter.SelectedCount;

        _badge.gameObject.SetActive(count > 0);
        _badgeText.text = count.ToString();
    }

    //깔때기 아이콘 + "필터" 글자, 오른쪽 위에 고른 수 배지
    private void BuildWidget()
    {
        UiBuilder.Place(Root, 0f, 0f, ButtonWidth, ButtonHeight);

        _button = UiBuilder.CreateButton(Root, "FilterButton", "필터", 32f, false, OpenPanel,
            out TextMeshProUGUI label);
        UiBuilder.Stretch(_button.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        label.rectTransform.offsetMin = new Vector2(IconSize + 24f, 0f);

        Image icon = UiBuilder.CreateImage(_button.transform, "Icon", UiPalette.Text1, IconSprite);
        UiBuilder.Place(icon.rectTransform, 28f, (ButtonHeight - IconSize) * 0.5f, IconSize, IconSize);

        _badge = UiBuilder.CreateImage(Root, "Badge", UiPalette.AccentGold, "Common/press_disc");
        _badge.rectTransform.anchorMin = new Vector2(1f, 1f);
        _badge.rectTransform.anchorMax = new Vector2(1f, 1f);
        _badge.rectTransform.anchoredPosition = new Vector2(-6f, -6f);
        _badge.rectTransform.sizeDelta = new Vector2(BadgeSize, BadgeSize);

        _badgeText = UiBuilder.CreateText(_badge.transform, "Count", 24f, UiPalette.ButtonPrimaryText,
            TextAlignmentOptions.Center);
        UiBuilder.Stretch(_badgeText.rectTransform, 0f, 0f, 0f, 0f);

        RefreshBadge();
    }
}
