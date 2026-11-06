using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    //탭: 어두운 알약 안에서 고른 쪽에 흰 알약이 깔린다. 2배 그림이라 9-슬라이스 테두리를 반으로 줄여 쓴다
    private const string TabTrackSprite = "Gacha/tab_track";
    private const string TabPillSprite = "Gacha/tab_pill";
    private const float TabTop = 210f;
    private const float TabHeight = 96f;
    private const float TabInset = 8f;
    private const float SliceScale = 2f;
    //뽑기권 모양 버튼. 절취선 왼쪽은 카드 그림 칸이라 글자는 그 오른쪽 가운데에 둔다. 일반 뽑기는 파랑, 시그니쳐는 분홍
    private const string TicketOnceBlueSprite = "Gacha/ticket_once_blue";
    private const string TicketTenBlueSprite = "Gacha/ticket_ten_blue";
    private const string TicketOncePinkSprite = "Gacha/ticket_once_pink";
    private const string TicketTenPinkSprite = "Gacha/ticket_ten_pink";
    private const float TicketStubWidth = 150f;
    private const float DisabledAlpha = 0.4f;
    //뽑기 버튼 · 그 아래 줄 오른쪽 끝 네모난 기록 보기
    private const float RollTop = 326f;
    private const float RollHeight = 130f;
    private const float HistoryRowTop = 472f;
    private const float HistoryButtonSize = 104f;
    private const float ResultAreaTop = HistoryRowTop + HistoryButtonSize + 16f;
    private const float ResultAreaBottom = 184f;

    private Action _onBack;
    private Action<int> _onCardSelected;
    private Action<string, bool> _onNotify;

    [SerializeField] private CurrencyBarView _currencyBar;
    [SerializeField] private Button _normalTabButton;
    [SerializeField] private Button _signatureTabButton;
    [SerializeField] private TextMeshProUGUI _normalTabLabel;
    [SerializeField] private TextMeshProUGUI _signatureTabLabel;
    [SerializeField] private Button _rollOnceButton;
    [SerializeField] private Button _rollTenButton;
    [SerializeField] private TextMeshProUGUI _rollOnceLabel;
    [SerializeField] private TextMeshProUGUI _rollTenLabel;
    [SerializeField] private Button _historyButton;
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

        bool signature = _gachaType == GachaType.Signature;

        SetTicketSprite(_rollOnceButton, signature ? TicketOncePinkSprite : TicketOnceBlueSprite);
        SetTicketSprite(_rollTenButton, signature ? TicketTenPinkSprite : TicketTenBlueSprite);
        SetTicketEnabled(_rollOnceButton, _rollOnceLabel, tickets >= 1);
        SetTicketEnabled(_rollTenButton, _rollTenLabel, tickets >= TenRollCount);

        SetTabSelected(_normalTabButton, _normalTabLabel, _gachaType == GachaType.Normal);
        SetTabSelected(_signatureTabButton, _signatureTabLabel, _gachaType == GachaType.Signature);
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

        Image tabTrack = UiBuilder.CreateImage(Root, "TabTrack", Color.white, TabTrackSprite, Image.Type.Sliced);
        tabTrack.pixelsPerUnitMultiplier = SliceScale;
        UiBuilder.Place(tabTrack.rectTransform, 32f, TabTop, 1016f, TabHeight);

        _normalTabButton = CreateTab("NormalTab", "일반 뽑기", 32f, () => SelectType(GachaType.Normal),
            out _normalTabLabel);
        _signatureTabButton = CreateTab("SignatureTab", "시그니쳐 뽑기", 540f, () => SelectType(GachaType.Signature),
            out _signatureTabLabel);

        _rollOnceButton = CreateTicket("RollOnce", "1회", TicketOnceBlueSprite, 32f, RollOnce, out _rollOnceLabel);
        _rollTenButton = CreateTicket("RollTen", "10회", TicketTenBlueSprite, 548f, RollTen, out _rollTenLabel);

        _historyButton = UiBuilder.CreateButton(Root, "HistoryButton", "기록\n보기", 30f, false, OpenHistory);
        UiBuilder.Place(_historyButton.GetComponent<RectTransform>(), 1048f - HistoryButtonSize, HistoryRowTop,
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

    //알약 탭 하나. 바탕(흰 알약)은 고른 탭에서만 보인다
    private Button CreateTab(string name, string label, float x, UnityAction onClick,
        out TextMeshProUGUI labelText)
    {
        Button tab = UiBuilder.CreateButton(Root, name, label, 36f, false, onClick, out labelText);
        Image pill = (Image)tab.targetGraphic;

        pill.sprite = UiSprites.Get(TabPillSprite);
        pill.type = Image.Type.Sliced;
        pill.pixelsPerUnitMultiplier = SliceScale;
        UiBuilder.Place(tab.GetComponent<RectTransform>(), x + TabInset, TabTop + TabInset,
            508f - TabInset * 2f, TabHeight - TabInset * 2f);

        return tab;
    }

    //뽑기권 모양 버튼. 글자는 절취선 오른쪽 칸 가운데
    private Button CreateTicket(string name, string label, string spritePath, float x,
        UnityAction onClick, out TextMeshProUGUI labelText)
    {
        Button ticket = UiBuilder.CreateButton(Root, name, label, 56f, false, onClick, out labelText);
        Image background = (Image)ticket.targetGraphic;

        background.sprite = UiSprites.Get(spritePath);
        background.type = Image.Type.Simple;
        background.color = Color.white;
        labelText.color = UiPalette.Text1;
        labelText.rectTransform.offsetMin = new Vector2(TicketStubWidth, 0f);
        UiBuilder.Place(ticket.GetComponent<RectTransform>(), x, RollTop, 500f, RollHeight);

        return ticket;
    }

    private static void SetTabSelected(Button tab, TextMeshProUGUI label, bool selected)
    {
        tab.targetGraphic.color = selected ? Color.white : UiPalette.WithAlpha(Color.white, 0f);
        label.color = selected ? UiPalette.ButtonPrimaryText : UiPalette.Text2;
    }

    //고른 뽑기 종류의 색으로 바꾼다
    private static void SetTicketSprite(Button ticket, string spritePath)
    {
        ((Image)ticket.targetGraphic).sprite = UiSprites.Get(spritePath);
    }

    //뽑기권이 모자라면 누를 수 없고 흐리게 보인다
    private static void SetTicketEnabled(Button ticket, TextMeshProUGUI label, bool enabled)
    {
        float alpha = enabled ? 1f : DisabledAlpha;

        ticket.interactable = enabled;
        ticket.targetGraphic.color = UiPalette.WithAlpha(Color.white, alpha);
        label.alpha = alpha;
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
