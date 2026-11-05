using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뽑기 화면. 일반 · 시그니쳐를 1회 또는 10회 돌리고, 결과는 뒷면으로 나눠 깔아 눌러 · 밀어 뒤집게 한다.
/// 10회 버튼 아래 줄 오른쪽 기록 보기로 지금까지 뽑은 카드를 최신 순으로 본다
/// </summary>
public class GachaScreen : UiScreenBehaviour
{
    private const int TenRollCount = 10;
    //구장 조명 배경. 화면 폭 1080 기준으로 위에 붙이고, 세로가 짧은 기기는 아래가 잘린다
    private const string StageSprite = "Gacha/gacha_bg";
    private const float StageWidth = 1080f;
    private const float StageHeight = 2340f;
    //뽑기 버튼 · 그 아래 결과 줄(왼쪽 결과 글 · 오른쪽 네모난 기록 보기)
    private const float RollTop = 326f;
    private const float RollHeight = 130f;
    private const float ResultRowTop = 472f;
    private const float HistoryButtonSize = 104f;
    private const float ResultAreaTop = ResultRowTop + HistoryButtonSize + 16f;
    private const float ResultAreaBottom = 184f;

    private Action _onBack;
    private Action<int> _onCardSelected;
    private Action<string, bool> _onNotify;

    [SerializeField] private CurrencyBarView _currencyBar;
    [SerializeField] private Button _normalTabButton;
    [SerializeField] private Button _signatureTabButton;
    [SerializeField] private Button _rollOnceButton;
    [SerializeField] private Button _rollTenButton;
    [SerializeField] private Button _historyButton;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private RectTransform _resultArea;
    [SerializeField] private TextMeshProUGUI _emptyText;

    private readonly List<int> _resultInstanceIds = new List<int>(TenRollCount);
    private readonly List<CardDisplayInfo> _resultCards = new List<CardDisplayInfo>(TenRollCount);
    private GachaRevealView _reveal;
    private GachaHistoryPanel _historyPanel;

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
    /// 보유 뽑기권을 다시 읽어 버튼을 켜고 끈다
    /// </summary>
    public void Refresh()
    {
        _currencyBar.Refresh();

        CurrencyType ticketType = GetTicketType();
        int tickets = CurrencyManager.Instance == null ? 0 : CurrencyManager.Instance.GetAmount(ticketType);

        _rollOnceButton.interactable = tickets >= 1;
        _rollTenButton.interactable = tickets >= TenRollCount;

        UiTabStyle.Apply(_normalTabButton, _gachaType == GachaType.Normal);
        UiTabStyle.Apply(_signatureTabButton, _gachaType == GachaType.Signature);
    }

    protected override void BindEvents()
    {
        Bind(_normalTabButton, () => SelectType(GachaType.Normal));
        Bind(_signatureTabButton, () => SelectType(GachaType.Signature));
        Bind(_rollOnceButton, RollOnce);
        Bind(_rollTenButton, RollTen);
        Bind(_historyButton, OpenHistory);
        BindButton("BackButton", OnBackClicked);
    }

    protected override void Build()
    {
        Image stage = UiBuilder.CreateImage(Root, "StageLights", Color.white, StageSprite);
        RectTransform stageRect = stage.rectTransform;

        stageRect.anchorMin = new Vector2(0.5f, 1f);
        stageRect.anchorMax = new Vector2(0.5f, 1f);
        stageRect.pivot = new Vector2(0.5f, 1f);
        stageRect.anchoredPosition = Vector2.zero;
        stageRect.sizeDelta = new Vector2(StageWidth, StageHeight);
        stage.gameObject.SetActive(stage.sprite != null);

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

        _rollOnceButton = UiBuilder.CreateButton(Root, "RollOnce", "1회", 52f, false, RollOnce);
        UiBuilder.Place(_rollOnceButton.GetComponent<RectTransform>(), 32f, RollTop, 500f, RollHeight);

        _rollTenButton = UiBuilder.CreateButton(Root, "RollTen", "10회", 52f, true, RollTen);
        UiBuilder.Place(_rollTenButton.GetComponent<RectTransform>(), 548f, RollTop, 500f, RollHeight);

        _resultText = UiBuilder.CreateText(Root, "ResultTitle", 34f, UiPalette.Text3);
        UiBuilder.Place(_resultText.rectTransform, 32f, ResultRowTop, 1016f - HistoryButtonSize - 24f, HistoryButtonSize);
        _resultText.text = "결과";

        _historyButton = UiBuilder.CreateButton(Root, "HistoryButton", "기록\n보기", 30f, false, OpenHistory);
        UiBuilder.Place(_historyButton.GetComponent<RectTransform>(), 1048f - HistoryButtonSize, ResultRowTop,
            HistoryButtonSize, HistoryButtonSize);

        _resultArea = UiBuilder.CreateRect(Root, "Result");
        UiBuilder.Stretch(_resultArea, 32f, ResultAreaTop, 32f, ResultAreaBottom);

        _emptyText = UiBuilder.CreateText(_resultArea, "Empty", 34f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Stretch(_emptyText.rectTransform, 0f, 0f, 0f, 0f);
        _emptyText.text = "아직 뽑지 않았습니다";

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
        _reveal?.ResumeHint();
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

    //결과를 뒷면으로 나눠 깐다. 1장은 가운데 크게, 10장은 4 · 4 · 2 줄
    private void ShowResults(int rolledCount)
    {
        _resultCards.Clear();

        foreach (int instanceId in _resultInstanceIds)
        {
            if (CardDisplayInfo.TryCreate(instanceId, out CardDisplayInfo info))
                _resultCards.Add(info);
        }

        if (_reveal == null)
            _reveal = new GachaRevealView(_resultArea);

        _emptyText.gameObject.SetActive(_resultCards.Count == 0);
        _reveal.Show(_resultCards, OnResultClicked);
        _resultText.text = $"결과  {_resultInstanceIds.Count}장" +
                           (_resultInstanceIds.Count < rolledCount ? "  (인벤토리가 꽉 차 일부를 받지 못했습니다)" : string.Empty);

        if (_resultInstanceIds.Count < rolledCount)
            _onNotify?.Invoke("인벤토리가 꽉 차 일부 카드를 받지 못했습니다", false);

        Refresh();
    }

    //처음 열 때 만들어 화면 맨 위에 덮는다
    private void OpenHistory()
    {
        if (_historyPanel == null)
            _historyPanel = GachaHistoryPanel.Create(Root);

        _historyPanel.Open();
    }

    private void OnResultClicked(int instanceId)
    {
        _onCardSelected?.Invoke(instanceId);
    }

    private CurrencyType GetTicketType()
    {
        return _gachaType == GachaType.Signature ? CurrencyType.SignatureTicket : CurrencyType.NormalTicket;
    }

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }
}
