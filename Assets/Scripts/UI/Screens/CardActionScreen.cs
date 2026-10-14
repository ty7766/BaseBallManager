using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 1장을 골라 무언가를 하는 화면들의 공통 틀 (강화 · 훈련 · 조합 · 분해 · 제작).
/// 제목 · 재화 줄 · 본문 영역 · 로비 버튼을 공유하고 본문만 각 화면이 채운다
/// </summary>
public abstract class CardActionScreen : UiScreenBehaviour
{
    private const float BodyTop = 212f;
    //카드 목록은 화면 중간보다 조금 아래부터. 위쪽은 기능 UI 가 쓴다
    private const float CardListTop = 1060f;

    /// <summary>
    /// 화면 제목. 베이크 시점에 쓰이므로 상수 문자열로 둔다
    /// </summary>
    protected abstract string Title { get; }

    /// <summary>
    /// 본문을 올릴 영역 (제목·재화 줄 아래, 하단 버튼 위)
    /// </summary>
    protected RectTransform Body => _body;

    /// <summary>
    /// 상단 재화 줄
    /// </summary>
    protected CurrencyBarView CurrencyBar => _currencyBar;

    /// <summary>
    /// 화면 아래쪽 카드 목록. 카드를 쓰지 않는 화면은 UsesCardList 를 false 로 둔다
    /// </summary>
    protected virtual bool UsesCardList => true;

    /// <summary>
    /// 한 번에 골라야 하는 장수. 조합처럼 여러 장이면 덮어쓴다
    /// </summary>
    protected virtual int RequiredSelectionCount => 1;

    /// <summary>
    /// 목록에 깔 카드를 거른다
    /// </summary>
    protected virtual bool CanSelectCard(CardDisplayInfo info)
    {
        return true;
    }

    /// <summary>
    /// 고를 수 없는 카드의 사유. 목록에서 빼지 않고 흐리게 덮어 이유를 보여준다
    /// </summary>
    protected virtual string GetBlockedReason(CardDisplayInfo info)
    {
        return null;
    }

    /// <summary>
    /// 고른 카드가 바뀌었다. 화면이 자기 상태를 갱신한다
    /// </summary>
    protected abstract void OnSelectionChanged(List<int> selectedIds);

    [SerializeField] private RectTransform _body;
    [SerializeField] private CurrencyBarView _currencyBar;
    [SerializeField] private CardGridView _cardList;

    private readonly List<int> _selectedIds = new List<int>(4);
    private readonly List<int> _candidateIds = new List<int>(64);

    private Action _onBack;
    private Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> _onPickCard;
    private Action<string, bool> _onNotify;
    private Action<int> _onShowCardDetail;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onBack,
        Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> onPickCard,
        Action<string, bool> onNotify, Action<int> onShowCardDetail)
    {
        _onBack = onBack;
        _onPickCard = onPickCard;
        _onNotify = onNotify;
        _onShowCardDetail = onShowCardDetail;
    }

    /// <summary>
    /// 화면에 들어올 때마다 상태를 다시 읽는다
    /// </summary>
    public virtual void Refresh()
    {
        _currencyBar.Refresh();
        RefreshCardList();
    }

    /// <summary>
    /// 고른 카드를 모두 비운다 (기능을 실행한 뒤)
    /// </summary>
    protected void ClearSelection()
    {
        _selectedIds.Clear();
        OnSelectionChanged(_selectedIds);
    }

    //보유 카드 중 고를 수 있는 것만 깐다
    private void RefreshCardList()
    {
        if (!UsesCardList || _cardList == null || InventoryManager.Instance == null)
            return;

        _candidateIds.Clear();

        foreach (CardInstance card in InventoryManager.Instance.GetAllCards())
        {
            if (!CardDisplayInfo.TryCreate(card.InstanceId, out CardDisplayInfo info))
                continue;

            if (!CanSelectCard(info))
                continue;

            _candidateIds.Add(card.InstanceId);
        }

        _cardList.SetCards(_candidateIds);
        _cardList.SetSelection(_selectedIds);
    }

    //2초 길게 누르면 고르지 않고 카드 상세를 띄운다
    private void OnCardListLongPressed(int instanceId)
    {
        _onShowCardDetail?.Invoke(instanceId);
    }

    //단일이면 바꿔 끼우고, 여러 장이면 필요한 수까지 토글한다
    private void OnCardListClicked(int instanceId)
    {
        if (RequiredSelectionCount <= 1)
        {
            _selectedIds.Clear();
            _selectedIds.Add(instanceId);
        }
        else if (_selectedIds.Contains(instanceId))
        {
            _selectedIds.Remove(instanceId);
        }
        else if (_selectedIds.Count < RequiredSelectionCount)
        {
            _selectedIds.Add(instanceId);
        }

        OnSelectionChanged(_selectedIds);
    }

    protected sealed override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, 32f, 32f, 700f, 60f);
        title.text = Title;

        _currencyBar = CurrencyBarView.Bake(Root);
        UiBuilder.Place(_currencyBar.Root, 24f, 104f, 1032f, CurrencyBarView.BarHeight);

        _body = UiBuilder.CreateRect(Root, "Body");

        if (UsesCardList)
        {
            UiBuilder.Stretch(_body, 32f, BodyTop, 32f, 1920f - CardListTop);

            _cardList = CardGridView.Bake(Root);
            UiBuilder.Stretch(_cardList.Root, 32f, CardListTop, 32f, 200f);
            _cardList.SetClickHandler(OnCardListClicked);
            _cardList.SetLongPressHandler(OnCardListLongPressed);
            _cardList.SetBlockedReason(GetBlockedReason);
            _cardList.SetEmptyMessage("고를 수 있는 카드가 없습니다");
        }
        else
        {
            UiBuilder.Stretch(_body, 32f, BodyTop, 32f, 200f);
        }

        Button backButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, true, OnBackClicked);
        RectTransform backRect = backButton.GetComponent<RectTransform>();

        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.offsetMin = new Vector2(32f, 40f);
        backRect.offsetMax = new Vector2(-32f, 160f);

        BuildBody();
    }

    protected override void BindEvents()
    {
        BindButton("BackButton", OnBackClicked);

        if (_cardList == null)
            return;

        _cardList.SetClickHandler(OnCardListClicked);
        _cardList.SetLongPressHandler(OnCardListLongPressed);
        _cardList.SetBlockedReason(GetBlockedReason);
    }

    protected override void OnShown()
    {
        //넣어만 두고 나갔다 오면 비운 상태로 시작한다
        _selectedIds.Clear();
        OnSelectionChanged(_selectedIds);
        Refresh();
    }

    /// <summary>
    /// 화면별 본문 구성
    /// </summary>
    protected abstract void BuildBody();

    /// <summary>
    /// 성공·실패 안내
    /// </summary>
    protected void Notify(string message, bool success)
    {
        _onNotify?.Invoke(message, success);
    }

    /// <summary>
    /// 카드 선택 모달을 띄운다
    /// </summary>
    protected void PickCard(string title, Func<CardDisplayInfo, bool> canSelect, Action<int> onPicked)
    {
        _onPickCard?.Invoke(title, 1, canSelect, ids => onPicked(ids[0]));
    }

    /// <summary>
    /// 여러 장을 고르는 선택 모달 (조합 재료)
    /// </summary>
    protected void PickCards(string title, int count, Func<CardDisplayInfo, bool> canSelect,
        Action<List<int>> onPicked)
    {
        _onPickCard?.Invoke(title, count, canSelect, onPicked);
    }

    /// <summary>
    /// 선택된 카드 요약 1줄 ("이서준 '24 · LF · OVR 73 · 강화 +2 · 훈련 Lv.5")
    /// </summary>
    protected static string BuildCardSummary(CardDisplayInfo info)
    {
        return $"{PlayerLabelFormat.NameWithYear(info.Name, info.Year)}  ·  {info.Position}  ·  " +
               $"OVR {info.FinalOvr}  ·  강화 +{info.EnhanceLevel}  ·  훈련 Lv.{info.TrainLevel}";
    }

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }
}
