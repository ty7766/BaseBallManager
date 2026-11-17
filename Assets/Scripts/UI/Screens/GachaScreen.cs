using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 뽑기 로비. 위에서 가운데 아래까지 큰 배너(일반 · 시그니쳐)를 좌우로 넘기고, 아래 뽑기권 버튼으로 1회 · 10회를 돌린다.
/// 뽑으면 화면 전체를 덮는 연출 무대에서 카드를 뒤집고, 다 뒤집은 뒤 확인을 누르면 이 로비로 돌아온다
/// </summary>
public class GachaScreen : UiScreenBehaviour
{
    private const int TenRollCount = 10;
    //뽑기권 모양 버튼. 절취선 왼쪽은 카드 그림 칸이라 글자는 그 오른쪽 가운데에 둔다. 일반 뽑기는 파랑, 시그니쳐는 분홍
    private const string TicketOnceBlueSprite = "Gacha/ticket_once_blue";
    private const string TicketTenBlueSprite = "Gacha/ticket_ten_blue";
    private const string TicketOncePinkSprite = "Gacha/ticket_once_pink";
    private const string TicketTenPinkSprite = "Gacha/ticket_ten_pink";
    private const float TicketStubWidth = 150f;
    private const float TicketWidth = 500f;
    private const float DisabledAlpha = 0.4f;
    //화면 아래부터: 로비로 · 뽑기권 버튼 · 천장 안내 · 배너 넘김 점 · 배너
    private const float BackBottom = 40f;
    private const float BackHeight = 120f;
    private const float TicketBottom = 184f;
    private const float TicketHeight = 130f;
    private const float PityBottom = 330f;
    private const float PityHeight = 40f;
    private const float DotsBottom = 388f;
    private const float DotHeight = 12f;
    private const float DotWidth = 24f;
    private const float DotActiveWidth = 56f;
    private const float DotGap = 12f;
    private const float BannerTop = 204f;
    private const float BannerBottom = 420f;
    private const string GuaranteeBadge = "10회 뽑기 시 4성 이상 1장 확정";

    //배너 순서 (좌우로 넘긴다)
    private static readonly GachaType[] BannerOrder = { GachaType.Normal, GachaType.Signature };

    private Action _onBack;
    private Action<int> _onCardSelected;
    private Action<string, bool> _onNotify;

    [SerializeField] private CurrencyBarView _currencyBar;
    [SerializeField] private GachaBannerView _banner;
    [SerializeField] private Image[] _dots = new Image[BannerOrder.Length];
    [SerializeField] private TextMeshProUGUI _pityText;
    [SerializeField] private Button _rollOnceButton;
    [SerializeField] private Button _rollTenButton;
    [SerializeField] private TextMeshProUGUI _rollOnceLabel;
    [SerializeField] private TextMeshProUGUI _rollTenLabel;

    private readonly List<int> _resultInstanceIds = new List<int>(TenRollCount);
    private readonly List<CardDisplayInfo> _resultCards = new List<CardDisplayInfo>(TenRollCount);
    private readonly List<CardDisplayInfo> _featured = new List<CardDisplayInfo>(GachaBannerView.FeaturedCount);
    private GachaRevealStage _stage;
    private GachaHistoryPanel _historyPanel;

    private int _bannerIndex;

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
    /// 재화 · 배너 · 천장 · 뽑기권 버튼을 지금 상태로 다시 그린다
    /// </summary>
    public void Refresh()
    {
        _currencyBar.Refresh();

        GachaType gachaType = BannerOrder[_bannerIndex];
        bool signature = gachaType == GachaType.Signature;
        CurrencyType ticketType = signature ? CurrencyType.SignatureTicket : CurrencyType.NormalTicket;
        int tickets = CurrencyManager.Instance == null ? 0 : CurrencyManager.Instance.GetAmount(ticketType);

        RefreshBanner(gachaType);
        RefreshDots();
        _pityText.text = BuildPityText(gachaType);

        SetTicketSprite(_rollOnceButton, signature ? TicketOncePinkSprite : TicketOnceBlueSprite);
        SetTicketSprite(_rollTenButton, signature ? TicketTenPinkSprite : TicketTenBlueSprite);
        SetTicketEnabled(_rollOnceButton, _rollOnceLabel, tickets >= 1);
        SetTicketEnabled(_rollTenButton, _rollTenLabel, tickets >= TenRollCount);
    }

    /// <summary>
    /// 연출 중엔 뒤로 가기를 막고(다 뒤집은 뒤엔 확인과 같다), 아니면 기록 창부터 닫는다
    /// </summary>
    public override bool HandleBack()
    {
        if (_stage != null && _stage.IsOpen)
        {
            if (_stage.IsAllRevealed)
                CloseStage();

            return true;
        }

        if (_historyPanel == null || !_historyPanel.gameObject.activeSelf)
            return false;

        _historyPanel.Close();

        return true;
    }

    protected override void BindEvents()
    {
        Bind(_banner.PreviousButton, ShowPreviousBanner);
        Bind(_banner.NextButton, ShowNextBanner);
        Bind(_banner.HistoryButton, OpenHistory);
        _banner.SetSwipeHandlers(ShowPreviousBanner, ShowNextBanner);
        Bind(_rollOnceButton, RollOnce);
        Bind(_rollTenButton, RollTen);
        BindButton("BackButton", OnBackClicked);
    }

    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, 32f, 32f, 500f, 60f);
        title.text = "뽑기";

        _currencyBar = CurrencyBarView.Bake(Root, CurrencyBarView.AllTypes);
        UiBuilder.Place(_currencyBar.Root, 24f, 100f, 1032f, CurrencyBarView.BarHeight);

        _banner = GachaBannerView.Bake(Root);
        UiBuilder.Stretch(_banner.Root, 32f, BannerTop, 32f, BannerBottom);

        BuildDots();

        _pityText = UiBuilder.CreateText(Root, "Pity", 28f, UiPalette.Text2, TextAlignmentOptions.Center);
        PlaceFromBottom(_pityText.rectTransform, 32f, PityBottom, 1016f, PityHeight);

        _rollOnceButton = CreateTicket("RollOnce", "1회", TicketOnceBlueSprite, 32f, RollOnce, out _rollOnceLabel);
        _rollTenButton = CreateTicket("RollTen", "10회", TicketTenBlueSprite, 548f, RollTen, out _rollTenLabel);

        Button backButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, true, OnBackClicked);
        RectTransform backRect = backButton.GetComponent<RectTransform>();

        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.offsetMin = new Vector2(32f, BackBottom);
        backRect.offsetMax = new Vector2(-32f, BackBottom + BackHeight);
    }

    protected override void OnShown()
    {
        Refresh();
    }

    //다시 들어오면 일반 배너 · 무대 · 기록 창 닫힘 (뽑은 카드는 이미 인벤토리에 있다)
    protected override void OnHidden()
    {
        _bannerIndex = 0;
        _historyPanel?.Close();
        _stage?.Close();
    }

    //배너 수만큼 넘김 표시. 지금 배너만 길게 · 밝게
    private void BuildDots()
    {
        float total = DotActiveWidth + (_dots.Length - 1) * (DotWidth + DotGap);
        float left = (1080f - total) * 0.5f;

        for (int i = 0; i < _dots.Length; i++)
        {
            _dots[i] = UiBuilder.CreateImage(Root, "Dot", UiPalette.Text1, UiSprites.ScrollHandle, Image.Type.Sliced);
            PlaceFromBottom(_dots[i].rectTransform, left + i * (DotWidth + DotGap), DotsBottom, DotWidth, DotHeight);
        }
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
        PlaceFromBottom(ticket.GetComponent<RectTransform>(), x, TicketBottom, TicketWidth, TicketHeight);

        return ticket;
    }

    //화면 아래 기준 배치 (세로가 긴 기기에서도 버튼은 아래에 붙는다)
    private static void PlaceFromBottom(RectTransform rect, float x, float bottom, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, bottom);
        rect.sizeDelta = new Vector2(width, height);
    }

    //배너 이름 · 설명 · 확정 띠 · 대표 카드
    private void RefreshBanner(GachaType gachaType)
    {
        bool signature = gachaType == GachaType.Signature;

        CollectFeatured(gachaType);
        _banner.SetBanner(signature ? "시그니쳐 선수 뽑기" : "일반 선수 뽑기",
            signature ? "4성 · 5성 카드와 시그니쳐 카드" : "3성 · 4성 · 5성 일반 카드",
            GuaranteeBadge, signature ? UiPalette.CardSignature : UiPalette.CardStar3, _featured);
    }

    private void RefreshDots()
    {
        float total = DotActiveWidth + (_dots.Length - 1) * (DotWidth + DotGap);
        float x = (1080f - total) * 0.5f;

        for (int i = 0; i < _dots.Length; i++)
        {
            bool active = i == _bannerIndex;
            float width = active ? DotActiveWidth : DotWidth;
            RectTransform rect = _dots[i].rectTransform;

            rect.anchoredPosition = new Vector2(x, DotsBottom);
            rect.sizeDelta = new Vector2(width, DotHeight);
            _dots[i].color = active ? UiPalette.Text1 : UiPalette.WithAlpha(UiPalette.Text1, 0.3f);
            x += width + DotGap;
        }
    }

    //대표 카드는 모두 내 팀 카드다. 등급 높은 순 → OVR 높은 순이라 5성이 모자라면 4성 · 3성으로 채운다 (일반은 노말, 시그니쳐는 시그니쳐).
    //내 팀 카드가 3장도 안 될 때만 다른 팀에서 채운다
    private void CollectFeatured(GachaType gachaType)
    {
        _featured.Clear();

        if (CardDataManager.Instance == null)
            return;

        CardType wantedType = gachaType == GachaType.Signature ? CardType.Signature : CardType.Normal;
        string team = PlayerDataManager.Instance == null ? null : PlayerDataManager.Instance.PlayerTeamName;
        List<CardMasterData> candidates = new List<CardMasterData>(64);

        foreach (CardMasterData card in CardDataManager.Instance.GetAllHitters())
            AddCandidate(candidates, card, wantedType);

        foreach (CardMasterData card in CardDataManager.Instance.GetAllPitchers())
            AddCandidate(candidates, card, wantedType);

        candidates.Sort((a, b) =>
        {
            int byTeam = (b.TeamName == team).CompareTo(a.TeamName == team);

            if (byTeam != 0)
                return byTeam;

            int byGrade = ((int)b.CardGrade).CompareTo((int)a.CardGrade);

            return byGrade != 0 ? byGrade : b.OVR.CompareTo(a.OVR);
        });

        foreach (CardMasterData card in candidates)
        {
            if (_featured.Count >= GachaBannerView.FeaturedCount)
                break;

            if (CardDisplayInfo.TryCreateFromMaster(card.CardId, out CardDisplayInfo info))
                _featured.Add(info);
        }
    }

    //배너 종류와 같은 카드만 후보로 모은다
    private static void AddCandidate(List<CardMasterData> candidates, CardMasterData card, CardType wantedType)
    {
        if (card.CardType == wantedType)
            candidates.Add(card);
    }

    private static string BuildPityText(GachaType gachaType)
    {
        if (GachaManager.Instance == null || GachaManager.Instance.PityLimit <= 0)
            return string.Empty;

        int count = gachaType == GachaType.Signature
            ? GachaManager.Instance.SignaturePityCount
            : GachaManager.Instance.NormalPityCount;
        int remaining = Mathf.Max(1, GachaManager.Instance.PityLimit - count);

        return $"천장까지 {remaining}회  ·  내 팀 5성 확정";
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

    private void ShowPreviousBanner()
    {
        _bannerIndex = (_bannerIndex + BannerOrder.Length - 1) % BannerOrder.Length;
        Refresh();
        UiEffects.Pop(_banner.Root, 0.96f, 0.2f);
    }

    private void ShowNextBanner()
    {
        _bannerIndex = (_bannerIndex + 1) % BannerOrder.Length;
        Refresh();
        UiEffects.Pop(_banner.Root, 0.96f, 0.2f);
    }

    private void RollOnce()
    {
        if (GachaManager.Instance == null)
            return;

        GachaResult result = GachaManager.Instance.Roll1(BannerOrder[_bannerIndex]);

        if (result == null)
        {
            _onNotify?.Invoke("뽑기에 실패했습니다 (뽑기권 또는 보관함 공간 확인)", false);
            Refresh();
            return;
        }

        _resultInstanceIds.Clear();
        AddResult(result);
        OpenStage(1);
    }

    private void RollTen()
    {
        if (GachaManager.Instance == null)
            return;

        List<GachaResult> results = GachaManager.Instance.Roll10(BannerOrder[_bannerIndex]);

        if (results.Count == 0)
        {
            _onNotify?.Invoke("뽑기에 실패했습니다 (뽑기권 또는 보관함 공간 확인)", false);
            Refresh();
            return;
        }

        _resultInstanceIds.Clear();

        foreach (GachaResult result in results)
            AddResult(result);

        OpenStage(results.Count);
    }

    private void AddResult(GachaResult result)
    {
        if (result.InstanceId != GachaResult.NoInstanceId)
            _resultInstanceIds.Add(result.InstanceId);
    }

    //결과를 연출 무대에 넘긴다. 무대는 처음 뽑을 때 만든다
    private void OpenStage(int rolledCount)
    {
        _resultCards.Clear();

        foreach (int instanceId in _resultInstanceIds)
        {
            if (CardDisplayInfo.TryCreate(instanceId, out CardDisplayInfo info))
                _resultCards.Add(info);
        }

        if (_stage == null)
            _stage = new GachaRevealStage(Root);

        string kind = BannerOrder[_bannerIndex] == GachaType.Signature ? "시그니쳐 뽑기" : "일반 뽑기";

        _historyPanel?.Close();
        _stage.Open($"{kind}  ·  {rolledCount}회", _resultCards, GetLightColor(_resultCards), OnResultClicked, CloseStage);

        if (_resultInstanceIds.Count < rolledCount)
            _onNotify?.Invoke("보관함이 꽉 차 일부 카드를 받지 못했습니다", false);
    }

    private void CloseStage()
    {
        _stage?.Close();
        Refresh();
    }

    //가장 좋은 카드의 색 — 골든글러브 > 시그니쳐 > 등급 별 색
    private static Color GetLightColor(List<CardDisplayInfo> cards)
    {
        bool golden = false;
        bool signature = false;
        CardGrade best = CardGrade.Star3;

        foreach (CardDisplayInfo card in cards)
        {
            golden |= card.CardType == CardType.GoldenGlove;
            signature |= card.CardType == CardType.Signature;

            if (card.CardGrade > best)
                best = card.CardGrade;
        }

        if (golden)
            return UiPalette.CardGoldenGlove;

        if (signature)
            return UiPalette.CardSignature;

        return best switch
        {
            CardGrade.Star5 => UiPalette.CardStar5,
            CardGrade.Star4 => UiPalette.CardStar4,
            _ => UiPalette.CardStar3
        };
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

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }
}
