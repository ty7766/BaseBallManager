using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뽑기 화면. 일반 · 시그니쳐를 1회 또는 10연차로 돌리고 결과를 그린다
/// </summary>
public class GachaScreen : UiScreen
{
    private const int TenRollCount = 10;

    private readonly Action _onBack;
    private readonly Action<int> _onCardSelected;
    private readonly Action<string, bool> _onNotify;

    private CurrencyBarView _currencyBar;
    private Button _normalTabButton;
    private Button _signatureTabButton;
    private Button _rollOnceButton;
    private Button _rollTenButton;
    private TextMeshProUGUI _rollOnceLabel;
    private TextMeshProUGUI _rollTenLabel;
    private TextMeshProUGUI _pityText;
    private TextMeshProUGUI _resultText;
    private CardGridView _resultGrid;

    private readonly List<int> _resultInstanceIds = new List<int>(TenRollCount);

    private GachaType _gachaType = GachaType.Normal;

    public GachaScreen(Action onBack, Action<int> onCardSelected, Action<string, bool> onNotify)
    {
        _onBack = onBack;
        _onCardSelected = onCardSelected;
        _onNotify = onNotify;
    }

    protected override string ScreenName => "GachaScreen";

    /// <summary>
    /// 보유 뽑기권 · 천장 카운터를 다시 읽는다
    /// </summary>
    public void Refresh()
    {
        _currencyBar.Refresh();

        CurrencyType ticketType = GetTicketType();
        int tickets = CurrencyManager.Instance == null ? 0 : CurrencyManager.Instance.GetAmount(ticketType);

        _rollOnceLabel.text = $"1회\n{GetTicketLabel()} 1";
        _rollTenLabel.text = $"10연차\n{GetTicketLabel()} {TenRollCount}";

        _rollOnceButton.interactable = tickets >= 1;
        _rollTenButton.interactable = tickets >= TenRollCount;

        SetTabSelected(_normalTabButton, _gachaType == GachaType.Normal);
        SetTabSelected(_signatureTabButton, _gachaType == GachaType.Signature);

        if (GachaManager.Instance == null)
            return;

        int pity = _gachaType == GachaType.Normal
            ? GachaManager.Instance.NormalPityCount
            : GachaManager.Instance.SignaturePityCount;

        _pityText.text = $"천장  {pity} / {GachaManager.Instance.PityLimit}" +
                         $"   ·   보유 {GetTicketLabel()} {tickets}장";
    }

    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, 32f, 32f, 500f, 60f);
        title.text = "뽑기";

        _currencyBar = new CurrencyBarView(Root);
        UiBuilder.Place(_currencyBar.Root, 24f, 100f, 1032f, CurrencyBarView.BarHeight);

        _normalTabButton = UiBuilder.CreateButton(Root, "NormalTab", "일반 뽑기", 36f, true,
            () => SelectType(GachaType.Normal));
        UiBuilder.Place(_normalTabButton.GetComponent<RectTransform>(), 32f, 210f, 500f, 96f);

        _signatureTabButton = UiBuilder.CreateButton(Root, "SignatureTab", "시그니쳐 뽑기", 36f, false,
            () => SelectType(GachaType.Signature));
        UiBuilder.Place(_signatureTabButton.GetComponent<RectTransform>(), 548f, 210f, 500f, 96f);

        _pityText = UiBuilder.CreateText(Root, "Pity", 32f, UiPalette.Text2);
        UiBuilder.Place(_pityText.rectTransform, 32f, 320f, 1016f, 44f);

        _rollOnceButton = UiBuilder.CreateButton(Root, "RollOnce", string.Empty, 34f, false,
            RollOnce, out _rollOnceLabel);
        UiBuilder.Place(_rollOnceButton.GetComponent<RectTransform>(), 32f, 376f, 500f, 140f);

        _rollTenButton = UiBuilder.CreateButton(Root, "RollTen", string.Empty, 34f, true,
            RollTen, out _rollTenLabel);
        UiBuilder.Place(_rollTenButton.GetComponent<RectTransform>(), 548f, 376f, 500f, 140f);

        _resultText = UiBuilder.CreateText(Root, "ResultTitle", 34f, UiPalette.Text3);
        UiBuilder.Place(_resultText.rectTransform, 32f, 536f, 1016f, 44f);
        _resultText.text = "결과";

        _resultGrid = new CardGridView(Root);
        UiBuilder.Stretch(_resultGrid.Root, 32f, 588f, 32f, 200f);
        _resultGrid.SetClickHandler(OnResultClicked);
        _resultGrid.SetEmptyMessage("아직 뽑지 않았습니다");

        Button backButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, true, OnBackClicked);
        RectTransform backRect = backButton.GetComponent<RectTransform>();

        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.offsetMin = new Vector2(32f, 40f);
        backRect.offsetMax = new Vector2(-32f, 160f);
    }

    protected override void OnShown()
    {
        Refresh();
    }

    private void SelectType(GachaType gachaType)
    {
        _gachaType = gachaType;
        Refresh();
    }

    private void RollOnce()
    {
        if (GachaManager.Instance == null)
            return;

        GachaResult result = GachaManager.Instance.Roll1(_gachaType);

        if (result == null)
        {
            _onNotify?.Invoke("뽑기에 실패했습니다 (뽑기권 또는 카드 풀 확인)", false);
            Refresh();
            return;
        }

        _resultInstanceIds.Clear();
        AddResult(result);
        ShowResults(1);
    }

    private void RollTen()
    {
        if (GachaManager.Instance == null)
            return;

        List<GachaResult> results = GachaManager.Instance.Roll10(_gachaType);

        if (results.Count == 0)
        {
            _onNotify?.Invoke("뽑기에 실패했습니다 (뽑기권 또는 카드 풀 확인)", false);
            Refresh();
            return;
        }

        _resultInstanceIds.Clear();

        foreach (GachaResult result in results)
            AddResult(result);

        ShowResults(results.Count);
    }

    private void AddResult(GachaResult result)
    {
        if (result.InstanceId != GachaResult.NoInstanceId)
            _resultInstanceIds.Add(result.InstanceId);
    }

    private void ShowResults(int rolledCount)
    {
        _resultGrid.SetCards(_resultInstanceIds);
        _resultText.text = $"결과  {_resultInstanceIds.Count}장" +
                           (_resultInstanceIds.Count < rolledCount ? "  (인벤토리가 꽉 차 일부를 받지 못했습니다)" : string.Empty);

        if (_resultInstanceIds.Count < rolledCount)
            _onNotify?.Invoke("인벤토리가 꽉 차 일부 카드를 받지 못했습니다", false);

        Refresh();
    }

    private void OnResultClicked(int instanceId)
    {
        _onCardSelected?.Invoke(instanceId);
    }

    private CurrencyType GetTicketType()
    {
        return _gachaType == GachaType.Signature ? CurrencyType.SignatureTicket : CurrencyType.NormalTicket;
    }

    private string GetTicketLabel()
    {
        return _gachaType == GachaType.Signature ? "시그권" : "일반권";
    }

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }

    private static void SetTabSelected(Button button, bool selected)
    {
        Image background = button.GetComponent<Image>();

        background.color = selected ? UiPalette.ButtonPrimary : UiPalette.BgRow;
        background.sprite = UiSprites.Get(selected ? UiSprites.TabOn : UiSprites.ButtonNormal);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.color = selected ? UiPalette.ButtonPrimaryText : UiPalette.Text2;
    }
}
