using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뽑기 화면. 일반 · 시그니쳐를 1회 또는 10연차로 돌리고 결과를 그린다
/// </summary>
public class GachaScreen : UiScreenBehaviour
{
    private const float SingleCardWidth = 540f;
    private const int TenRollColumns = 5;
    private const float TenRollCardWidth = 185f;
    //결과 영역 높이 (1920 - 위 588 - 아래 200). 이 안에서 세로 가운데로 모은다
    private const float ResultAreaHeight = 1132f;

    private const int TenRollCount = 10;

    private Action _onBack;
    private Action<int> _onCardSelected;
    private Action<string, bool> _onNotify;

    [SerializeField] private CurrencyBarView _currencyBar;
    [SerializeField] private Button _normalTabButton;
    [SerializeField] private Button _signatureTabButton;
    [SerializeField] private Button _rollOnceButton;
    [SerializeField] private Button _rollTenButton;
    [SerializeField] private TextMeshProUGUI _rollOnceLabel;
    [SerializeField] private TextMeshProUGUI _rollTenLabel;
    [SerializeField] private TextMeshProUGUI _pityText;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private CardGridView _resultGrid;

    private List<int> _resultInstanceIds = new List<int>(TenRollCount);

    private GachaType _gachaType = GachaType.Normal;

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

        UiTabStyle.Apply(_normalTabButton, _gachaType == GachaType.Normal);
        UiTabStyle.Apply(_signatureTabButton, _gachaType == GachaType.Signature);

        if (GachaManager.Instance == null)
            return;

        int pity = _gachaType == GachaType.Normal
            ? GachaManager.Instance.NormalPityCount
            : GachaManager.Instance.SignaturePityCount;

        _pityText.text = $"천장  {pity} / {GachaManager.Instance.PityLimit}" +
                         $"   ·   보유 {GetTicketLabel()} {tickets}장";
    }

    protected override void BindEvents()
    {
        _resultGrid.SetClickHandler(OnResultClicked);
        Bind(_normalTabButton, () => SelectType(GachaType.Normal));
        Bind(_signatureTabButton, () => SelectType(GachaType.Signature));
        Bind(_rollOnceButton, RollOnce);
        Bind(_rollTenButton, RollTen);
        BindButton("BackButton", OnBackClicked);
    }

    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, 32f, 32f, 500f, 60f);
        title.text = "뽑기";

        _currencyBar = CurrencyBarView.Bake(Root);
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

        _resultGrid = CardGridView.Bake(Root);
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
        //단일은 가운데 큰 1장, 10연은 2행 5열
        if (_resultInstanceIds.Count <= 1)
            _resultGrid.SetLayout(1, SingleCardWidth, ResultAreaHeight);
        else
            _resultGrid.SetLayout(TenRollColumns, TenRollCardWidth, ResultAreaHeight);

        _resultGrid.SetCards(_resultInstanceIds);
        _resultGrid.PlayRevealSequence();
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

}
