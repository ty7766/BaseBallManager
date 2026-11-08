using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드를 골라 조작하는 화면의 공통 틀 (강화 · 훈련 · 조합 …). 위 절반은 작업 칸(화면마다 다름), 아래 절반은 카드 목록.
/// 아래 버튼 · 뒤로가기(필터 창 → 고른 것 비우기 → 로비) · 떠나면 초기화 · 연출 중 입력 막기를 함께 쓴다
/// </summary>
public abstract class CardWorkScreen : UiScreenBehaviour
{
    /// <summary>
    /// 위 작업 칸 규격 (화면 왼쪽 위 기준). 높이는 대상 카드(폭 340) 한 장 높이
    /// </summary>
    protected const float SideMargin = 32f;
    protected const float WorkTop = 120f;
    protected const float WorkHeight = 340f * CardView.BaseHeight / CardView.BaseWidth;

    //아래 카드 목록 — 머리 줄(왼쪽 이름 · 오른쪽 필터) 아래로
    private const float ListRowTop = WorkTop + WorkHeight + 24f;
    private const float GridTop = ListRowTop + CardFilterButtonView.ButtonHeight + 36f;
    private const float GridBottom = 224f;
    private const string OwnedListLabel = "보유 카드";
    //빈 칸 — 버튼과 같은 모서리(왼쪽 위 · 오른쪽 아래 직각)의 실선 칸 + 가운데 표시
    private const float EmptySlotCornerScale = 0.6f;
    private const float EmptySlotLineAlpha = 0.4f;

    /// <summary>
    /// 화면 제목 · 실행 버튼 글자. 베이크 시점에 쓰이므로 상수 문자열로 둔다
    /// </summary>
    protected abstract string Title { get; }
    protected abstract string ActionLabel { get; }

    /// <summary>
    /// 고른 것이 있는가. 아래 버튼(로비로 / 뒤로 + 실행)과 뒤로가기가 이것을 본다
    /// </summary>
    protected abstract bool HasWork { get; }

    /// <summary>
    /// 목록 머리 줄 이름 · 필터를 보일지 · 빈 목록 문구(null 이면 기본)
    /// </summary>
    protected virtual string ListLabel => OwnedListLabel;
    protected virtual bool ShowsFilter => true;
    protected virtual string ListEmptyMessage => null;

    /// <summary>
    /// 목록에서 체크로 보일 카드
    /// </summary>
    protected abstract IReadOnlyCollection<int> ListSelection { get; }

    /// <summary>
    /// 연출이 도는 중 (입력이 막혀 있다)
    /// </summary>
    protected bool IsPlaying => _playRoutine != null;

    /// <summary>
    /// 위 작업 칸 루트 (화면 폭 · 목록 머리 줄 위까지) · 아래 카드 목록
    /// </summary>
    protected RectTransform WorkArea => _workArea;
    protected CardGridView Grid => _grid;

    [SerializeField] private RectTransform _workArea;
    [SerializeField] private TextMeshProUGUI _listLabel;
    [SerializeField] private CardFilterButtonView _filterButton;
    [SerializeField] private CardGridView _grid;
    [SerializeField] private Button _lobbyButton;
    [SerializeField] private Button _stepBackButton;
    [SerializeField] private Button _actionButton;
    [SerializeField] private TextMeshProUGUI _actionLabel;
    [SerializeField] private CanvasGroup _actionGroup;
    [SerializeField] private Image _inputBlocker;

    private readonly List<int> _listIds = new List<int>(64);
    private readonly List<RectTransform> _leadingTiles = new List<RectTransform>(1);

    private Action _onBack;
    private Action<string, bool> _onNotify;
    private Action<int> _onShowCardDetail;
    private Coroutine _playRoutine;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onBack, Action<string, bool> onNotify, Action<int> onShowCardDetail)
    {
        _onBack = onBack;
        _onNotify = onNotify;
        _onShowCardDetail = onShowCardDetail;
    }

    /// <summary>
    /// 위 칸과 목록을 다시 그린다 (재화 · 잠금이 바뀌었을 때)
    /// </summary>
    public void Refresh()
    {
        if (InventoryManager.Instance == null)
            return;

        ValidateWork();
        RefreshChrome();
        RefreshList();
        RefreshWork(false);
    }

    /// <summary>
    /// 필터 창 → 고른 것 순으로 한 단계씩 물러난다. 연출 중에는 막는다
    /// </summary>
    public override bool HandleBack()
    {
        if (IsPlaying)
            return true;

        if (_filterButton.ClosePanel())
            return true;

        if (!HasWork)
            return false;

        ReturnToStart();

        return true;
    }

    protected override void BindEvents()
    {
        WireWidgets();
        Bind(_lobbyButton, OnLobbyClicked);
        Bind(_stepBackButton, ReturnToStart);
        Bind(_actionButton, OnActionButtonClicked);
    }

    //제목 · 목록 머리 줄 · 목록 · 작업 칸 · 아래 버튼 · 연출 중 입력 막이
    protected sealed override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, SideMargin, 32f, 500f, 60f);
        title.text = Title;

        _listLabel = UiBuilder.CreateText(Root, "ListLabel", 32f, UiPalette.Text2);
        UiBuilder.Place(_listLabel.rectTransform, SideMargin, ListRowTop, 600f, CardFilterButtonView.ButtonHeight);

        _filterButton = CardFilterButtonView.Bake(Root);
        UiBuilder.PlaceRight(_filterButton.Root, SideMargin, ListRowTop, CardFilterButtonView.ButtonWidth,
            CardFilterButtonView.ButtonHeight);

        _grid = CardGridView.Bake(Root);
        UiBuilder.Stretch(_grid.Root, SideMargin, GridTop, SideMargin, GridBottom);

        _workArea = UiBuilder.CreateRect(Root, "WorkArea");
        UiBuilder.PlaceRow(_workArea, 0f, ListRowTop);
        BuildWork();

        _lobbyButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, true, OnLobbyClicked);
        PlaceBottomButton(_lobbyButton, 0f, 1f);

        _stepBackButton = UiBuilder.CreateButton(Root, "StepBackButton", "뒤로", 34f, false, ReturnToStart);
        PlaceBottomButton(_stepBackButton, 0f, 0.5f);

        _actionButton = UiBuilder.CreateButton(Root, "ActionButton", ActionLabel, 36f, true, OnActionButtonClicked,
            out _actionLabel);
        PlaceBottomButton(_actionButton, 0.5f, 1f);
        _actionGroup = _actionButton.gameObject.AddComponent<CanvasGroup>();

        _inputBlocker = UiBuilder.CreateImage(Root, "InputBlocker", UiPalette.WithAlpha(Color.black, 0f));
        UiBuilder.Stretch(_inputBlocker.rectTransform, 0f, 0f, 0f, 0f);
        _inputBlocker.raycastTarget = true;
        _inputBlocker.gameObject.SetActive(false);

        WireWidgets();
        SetBottomButtons(false);
    }

    protected override void OnShown()
    {
        OnWorkShown();
        Refresh();
    }

    //다시 들어오면 고른 것 없이, 필터 없이 맨 위에서
    protected override void OnHidden()
    {
        _playRoutine = null;
        _inputBlocker.gameObject.SetActive(false);
        ClearWork();
        _filterButton.ResetFilter();
        _grid.ScrollToTop();
    }

    /// <summary>
    /// 위 작업 칸(WorkArea 안, 화면 왼쪽 위 기준 좌표)을 조립한다. 베이크 시점에 한 번
    /// </summary>
    protected abstract void BuildWork();

    /// <summary>
    /// 위 작업 칸을 지금 고른 것에 맞춰 갱신한다. 실행 버튼 상태도 여기서 SetAction 으로 정한다
    /// </summary>
    protected abstract void RefreshWork(bool animate);

    /// <summary>
    /// 목록의 카드를 눌렀다
    /// </summary>
    protected abstract void OnListCardClicked(int instanceId);

    /// <summary>
    /// 고른 것을 모두 비운다 (뒤로 · 화면을 떠날 때)
    /// </summary>
    protected abstract void ClearWork();

    /// <summary>
    /// 실행 버튼(강화 · 훈련 · 조합 …)을 눌렀다
    /// </summary>
    protected abstract void OnActionClicked();

    /// <summary>
    /// 다시 그리기 전에 고른 것 중 사라진 카드 등을 정리한다
    /// </summary>
    protected virtual void ValidateWork()
    {
    }

    /// <summary>
    /// 화면이 보일 때마다 (런타임에만 입힐 수 있는 글꼴 등)
    /// </summary>
    protected virtual void OnWorkShown()
    {
    }

    /// <summary>
    /// 목록을 채운다. 기본은 필터를 거친 보유 카드. 카드가 아닌 칸은 leadingTiles 로 앞에 끼운다
    /// </summary>
    protected virtual void CollectListCards(List<int> instanceIds, List<RectTransform> leadingTiles)
    {
        CollectOwnedCards(instanceIds);
    }

    /// <summary>
    /// 목록에서 고를 수 없는 사유 (흐리게 덮고 글자로 보인다)
    /// </summary>
    protected virtual string GetBlockedReason(CardDisplayInfo info)
    {
        return null;
    }

    /// <summary>
    /// 위 칸 위젯의 콜백 (프리팹에 저장되지 않는 것). 베이크 경로와 코드 조립 경로가 함께 부른다
    /// </summary>
    protected virtual void WireWork()
    {
    }

    /// <summary>
    /// 필터를 거친 보유 카드
    /// </summary>
    protected void CollectOwnedCards(List<int> instanceIds)
    {
        foreach (CardInstance card in InventoryManager.Instance.GetFiltered(_filterButton.CurrentFilter))
            instanceIds.Add(card.InstanceId);
    }

    /// <summary>
    /// 실행 버튼 상태와 글자. highlight 면 금색 버튼 (돌파처럼 한 단계 넘어서는 실행)
    /// </summary>
    protected void SetAction(bool enabled, string label, bool highlight = false)
    {
        _actionButton.interactable = enabled;
        _actionGroup.alpha = enabled ? 1f : 0.4f;
        _actionLabel.text = label;
        _actionButton.targetGraphic.color = highlight ? UiPalette.AccentGold : UiPalette.ButtonPrimary;
    }

    /// <summary>
    /// 아래 버튼 · 목록 머리 줄을 고른 것에 맞춘다
    /// </summary>
    protected void RefreshChrome()
    {
        SetBottomButtons(HasWork);
        _listLabel.text = ListLabel;
        _filterButton.gameObject.SetActive(ShowsFilter);
    }

    /// <summary>
    /// 목록을 다시 깐다 (내용이 바뀌거나 재료가 소모됐을 때)
    /// </summary>
    protected void RefreshList()
    {
        _listIds.Clear();
        _leadingTiles.Clear();
        CollectListCards(_listIds, _leadingTiles);

        _grid.SetLeadingTiles(_leadingTiles);
        _grid.SetEmptyMessage(ListEmptyMessage
            ?? (_filterButton.CurrentFilter.SelectedCount > 0 ? "조건에 맞는 카드가 없습니다" : "카드가 없습니다"));
        _grid.SetSelection(ListSelection);
        _grid.SetCards(_listIds);
    }

    /// <summary>
    /// 고른 것이 바뀌었다 — 체크 표시와 막힌 칸만 다시 칠한다
    /// </summary>
    protected void RefreshListSelection()
    {
        _grid.SetSelection(ListSelection);
        _grid.RefreshBlocked();
    }

    /// <summary>
    /// 고른 것을 비우고 처음 상태로 (아래 "뒤로" 버튼 · 뒤로가기)
    /// </summary>
    protected void ReturnToStart()
    {
        if (IsPlaying)
            return;

        string listLabel = ListLabel;

        ClearWork();
        Refresh();

        if (listLabel != ListLabel)
            _grid.ScrollToTop();
    }

    /// <summary>
    /// 연출을 돌리는 동안 입력을 막는다. 끝나면 풀린다
    /// </summary>
    protected void PlaySequence(IEnumerator sequence)
    {
        _playRoutine = StartCoroutine(RunSequence(sequence));
    }

    /// <summary>
    /// 성공 · 실패 안내
    /// </summary>
    protected void Notify(string message, bool success)
    {
        _onNotify?.Invoke(message, success);
    }

    /// <summary>
    /// 카드 상세를 덮어 띄운다 (길게 누르기)
    /// </summary>
    protected void ShowCardDetail(int instanceId)
    {
        _onShowCardDetail?.Invoke(instanceId);
    }

    /// <summary>
    /// 빈 칸 하나 — 채운 바탕 + 버튼 모서리 실선 테두리 + 가운데 표시(+ 아이콘 또는 글자). 큰 칸이라 모서리를 버튼보다 크게 잡는다
    /// </summary>
    protected static Image CreateEmptySlot(Transform parent, string name, float left, float top, float width, float height)
    {
        Color lineColor = GetEmptySlotLineColor();
        Image slot = UiBuilder.CreateImage(parent, name, UiPalette.BgPanel, UiSprites.ButtonPrimary, Image.Type.Sliced);

        UiBuilder.Place(slot.rectTransform, left, top, width, height);
        slot.pixelsPerUnitMultiplier = EmptySlotCornerScale;

        Image frame = UiBuilder.CreateImage(slot.transform, "Frame", lineColor, UiSprites.ButtonNormal, Image.Type.Sliced);
        UiBuilder.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
        frame.pixelsPerUnitMultiplier = EmptySlotCornerScale;

        return slot;
    }

    /// <summary>
    /// 빈 칸 테두리 · 가운데 표시 색
    /// </summary>
    protected static Color GetEmptySlotLineColor()
    {
        return UiPalette.WithAlpha(UiPalette.Text1, EmptySlotLineAlpha);
    }

    private IEnumerator RunSequence(IEnumerator sequence)
    {
        _inputBlocker.gameObject.SetActive(true);

        yield return sequence;

        _playRoutine = null;
        _inputBlocker.gameObject.SetActive(false);
    }

    private void PlaceBottomButton(Button button, float anchorLeft, float anchorRight)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        float leftInset = anchorLeft > 0f ? 12f : SideMargin;
        float rightInset = anchorRight < 1f ? 12f : SideMargin;

        rect.anchorMin = new Vector2(anchorLeft, 0f);
        rect.anchorMax = new Vector2(anchorRight, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(leftInset, 40f);
        rect.offsetMax = new Vector2(-rightInset, 160f);
    }

    //고른 것이 없으면 로비로 하나, 있으면 뒤로 + 실행
    private void SetBottomButtons(bool hasWork)
    {
        _lobbyButton.gameObject.SetActive(!hasWork);
        _stepBackButton.gameObject.SetActive(hasWork);
        _actionButton.gameObject.SetActive(hasWork);
    }

    //프리팹에 저장되지 않는 위젯 콜백. 베이크 경로와 코드 조립 경로가 함께 쓴다
    private void WireWidgets()
    {
        _filterButton.SetChangedHandler(OnFilterChanged);
        _grid.SetClickHandler(OnGridClicked);
        _grid.SetLongPressHandler(ShowCardDetail);
        _grid.SetBlockedReason(GetBlockedReason);
        _grid.SetCheckSelection(true);
        WireWork();
    }

    private void OnFilterChanged()
    {
        if (ShowsFilter)
            RefreshList();
    }

    private void OnGridClicked(int instanceId)
    {
        if (!IsPlaying)
            OnListCardClicked(instanceId);
    }

    private void OnActionButtonClicked()
    {
        if (!IsPlaying && HasWork)
            OnActionClicked();
    }

    private void OnLobbyClicked()
    {
        _onBack?.Invoke();
    }
}
