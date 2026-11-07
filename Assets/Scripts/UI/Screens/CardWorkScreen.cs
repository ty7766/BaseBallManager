using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 1장을 골라 조작하는 화면의 공통 틀 (강화 · 훈련 …). 위 절반은 작업 칸, 아래 절반은 카드 목록.
/// 처음엔 위 칸이 비어 있고, 목록에서 카드를 고르면 위 왼쪽에 놓인다. 뒤로가기는 대상 해제 → 로비
/// </summary>
public abstract class CardWorkScreen : UiScreenBehaviour
{
    /// <summary>
    /// 위 작업 칸 규격 — 왼쪽 대상 카드, 오른쪽 작업 내용(PreviewLeft ~ PreviewWidth)
    /// </summary>
    protected const float SideMargin = 32f;
    protected const float TargetTop = 120f;
    protected const float TargetWidth = 340f;
    protected const float TargetHeight = TargetWidth * CardView.BaseHeight / CardView.BaseWidth;
    protected const float PreviewLeft = SideMargin + TargetWidth + 32f;
    protected const float PreviewWidth = 1080f - SideMargin - PreviewLeft;
    protected const float PreviewTop = TargetTop + 30f;

    //아래 카드 목록 — 머리 줄(왼쪽 이름 · 오른쪽 필터) 아래로
    private const float ListRowTop = TargetTop + TargetHeight + 24f;
    private const float GridTop = ListRowTop + CardFilterButtonView.ButtonHeight + 36f;
    private const float GridBottom = 224f;
    private const string EmptySlotSprite = "Card/card_slot_empty";
    private const string CardMaskSprite = "Fx/card_mask";
    private const string OwnedListLabel = "보유 카드";

    /// <summary>
    /// 화면 제목 · 빈 칸 안내 · 실행 버튼 글자. 베이크 시점에 쓰이므로 상수 문자열로 둔다
    /// </summary>
    protected abstract string Title { get; }
    protected abstract string EmptyMessage { get; }
    protected abstract string ActionLabel { get; }

    /// <summary>
    /// 대상을 고른 뒤 목록을 재료로 바꿀 때의 목록 이름. null 이면 목록은 보유 카드 그대로다
    /// </summary>
    protected virtual string MaterialListLabel => null;

    /// <summary>
    /// 재료 목록이 비었을 때 문구
    /// </summary>
    protected virtual string MaterialEmptyMessage => "카드가 없습니다";

    /// <summary>
    /// 재료 목록에서 고른 카드 (체크 표시)
    /// </summary>
    protected virtual IReadOnlyCollection<int> MaterialSelection => null;

    /// <summary>
    /// 위 칸에 놓인 카드. 없으면 InvalidInstanceId
    /// </summary>
    protected int TargetInstanceId => _targetInstanceId;
    protected bool HasTarget => _targetInstanceId != InventoryManager.InvalidInstanceId;
    protected bool IsPlaying => _playRoutine != null;

    /// <summary>
    /// 위 작업 칸 루트 · 대상 카드 · 카드 위 번쩍임 · 카드 목록
    /// </summary>
    protected RectTransform WorkPanel => _workPanel;
    protected CardView TargetCard => _targetCard;
    protected Image CardFlash => _cardFlash;
    protected CardGridView Grid => _grid;

    [SerializeField] private Image _emptySlot;
    [SerializeField] private TextMeshProUGUI _emptyText;
    [SerializeField] private TextMeshProUGUI _listLabel;
    [SerializeField] private CardFilterButtonView _filterButton;
    [SerializeField] private CardGridView _grid;
    [SerializeField] private RectTransform _workPanel;
    [SerializeField] private CanvasGroup _workGroup;
    [SerializeField] private CardView _targetCard;
    [SerializeField] private Image _cardFlash;
    [SerializeField] private Button _lobbyButton;
    [SerializeField] private Button _stepBackButton;
    [SerializeField] private Button _actionButton;
    [SerializeField] private TextMeshProUGUI _actionLabel;
    [SerializeField] private CanvasGroup _actionGroup;
    [SerializeField] private Image _inputBlocker;

    private readonly List<int> _listIds = new List<int>(64);
    private readonly List<RectTransform> _leadingTiles = new List<RectTransform>(1);
    private readonly List<int> _targetSelection = new List<int>(1);

    private Action _onBack;
    private Action<string, bool> _onNotify;
    private Action<int> _onShowCardDetail;
    private Coroutine _playRoutine;
    private int _targetInstanceId = InventoryManager.InvalidInstanceId;

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

        if (HasTarget && !CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo _))
            ClearTarget();

        SetStep(HasTarget);
        RefreshList();

        if (HasTarget)
            RefreshTarget(false);
    }

    /// <summary>
    /// 필터 창 → 고른 카드 순으로 한 단계씩 물러난다. 연출 중에는 막는다
    /// </summary>
    public override bool HandleBack()
    {
        if (IsPlaying)
            return true;

        if (_filterButton.ClosePanel())
            return true;

        if (!HasTarget)
            return false;

        ReturnToEmpty();

        return true;
    }

    protected override void BindEvents()
    {
        WireWidgets();
        Bind(_lobbyButton, OnLobbyClicked);
        Bind(_stepBackButton, ReturnToEmpty);
        Bind(_actionButton, OnActionButtonClicked);
    }

    //제목 · 빈 자리 안내 · 목록 머리 줄 · 목록 · 작업 칸(숨김) · 아래 버튼 · 연출 중 입력 막이
    protected sealed override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, SideMargin, 32f, 500f, 60f);
        title.text = Title;

        _emptySlot = UiBuilder.CreateImage(Root, "EmptySlot", Color.white, EmptySlotSprite);
        UiBuilder.Place(_emptySlot.rectTransform, SideMargin, TargetTop, TargetWidth, TargetHeight);

        _emptyText = UiBuilder.CreateText(Root, "EmptyText", 36f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Place(_emptyText.rectTransform, PreviewLeft, TargetTop, PreviewWidth, TargetHeight);
        _emptyText.text = EmptyMessage;

        _listLabel = UiBuilder.CreateText(Root, "ListLabel", 32f, UiPalette.Text2);
        UiBuilder.Place(_listLabel.rectTransform, SideMargin, ListRowTop, 600f, CardFilterButtonView.ButtonHeight);

        _filterButton = CardFilterButtonView.Bake(Root);
        UiBuilder.PlaceRight(_filterButton.Root, SideMargin, ListRowTop, CardFilterButtonView.ButtonWidth,
            CardFilterButtonView.ButtonHeight);

        _grid = CardGridView.Bake(Root);
        UiBuilder.Stretch(_grid.Root, SideMargin, GridTop, SideMargin, GridBottom);

        _workPanel = UiBuilder.CreateRect(Root, "WorkPanel");
        UiBuilder.PlaceRow(_workPanel, 0f, ListRowTop);
        _workGroup = _workPanel.gameObject.AddComponent<CanvasGroup>();

        _targetCard = CardView.Bake(_workPanel, TargetWidth, false);
        UiBuilder.CenterPivot(UiBuilder.Place(_targetCard.Root, SideMargin, TargetTop, TargetWidth, TargetHeight));

        _cardFlash = UiBuilder.CreateImage(_workPanel, "CardFlash", UiPalette.WithAlpha(Color.white, 0f), CardMaskSprite);
        UiBuilder.Place(_cardFlash.rectTransform, SideMargin, TargetTop, TargetWidth, TargetHeight);

        BuildWork();

        _lobbyButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, true, OnLobbyClicked);
        PlaceBottomButton(_lobbyButton, 0f, 1f);

        _stepBackButton = UiBuilder.CreateButton(Root, "StepBackButton", "뒤로", 34f, false, ReturnToEmpty);
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
        SetStep(false);
    }

    protected override void OnShown()
    {
        OnWorkShown();
        Refresh();
    }

    //다시 들어오면 빈 칸부터, 필터 없이 맨 위에서
    protected override void OnHidden()
    {
        _playRoutine = null;
        _inputBlocker.gameObject.SetActive(false);
        ClearTarget();
        _filterButton.ResetFilter();
        _grid.ScrollToTop();
    }

    /// <summary>
    /// 위 칸 오른쪽(WorkPanel 안, PreviewLeft · PreviewTop 기준) 작업 내용을 조립한다. 베이크 시점에 한 번
    /// </summary>
    protected abstract void BuildWork();

    /// <summary>
    /// 작업 내용을 대상 카드에 맞춰 갱신한다. 실행 버튼 상태도 여기서 SetAction 으로 정한다
    /// </summary>
    protected abstract void RefreshWork(CardDisplayInfo target, bool animate);

    /// <summary>
    /// 실행 버튼(강화 · 훈련 …)을 눌렀다
    /// </summary>
    protected abstract void OnActionClicked();

    /// <summary>
    /// 대상이 바뀌거나 비워졌다. 화면이 들고 있던 재료 선택 등을 비운다
    /// </summary>
    protected virtual void OnTargetChanged()
    {
    }

    /// <summary>
    /// 화면이 보일 때마다 (런타임에만 입힐 수 있는 글꼴 등)
    /// </summary>
    protected virtual void OnWorkShown()
    {
    }

    /// <summary>
    /// 재료 목록을 채운다 (MaterialListLabel 이 있을 때). 카드가 아닌 칸은 leadingTiles 로 앞에 끼운다
    /// </summary>
    protected virtual void CollectMaterials(List<int> instanceIds, List<RectTransform> leadingTiles)
    {
    }

    /// <summary>
    /// 재료 목록의 카드를 눌렀다
    /// </summary>
    protected virtual void OnMaterialClicked(int instanceId)
    {
    }

    /// <summary>
    /// 보유 카드 목록에서 고를 수 없는 사유 (이미 최대 등)
    /// </summary>
    protected virtual string GetTargetBlockedReason(CardDisplayInfo info)
    {
        return null;
    }

    /// <summary>
    /// 재료 목록에서 고를 수 없는 사유
    /// </summary>
    protected virtual string GetMaterialBlockedReason(CardDisplayInfo info)
    {
        return null;
    }

    /// <summary>
    /// 실행 버튼 상태와 글자
    /// </summary>
    protected void SetAction(bool enabled, string label)
    {
        _actionButton.interactable = enabled;
        _actionGroup.alpha = enabled ? 1f : 0.4f;
        _actionLabel.text = label;
    }

    /// <summary>
    /// 대상 카드 그림과 작업 내용을 다시 그린다
    /// </summary>
    protected void RefreshTarget(bool animate)
    {
        if (!CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo info))
            return;

        _targetCard.SetCard(info);
        RefreshWork(info, animate);
    }

    /// <summary>
    /// 목록을 다시 깐다 (재료가 소모됐을 때)
    /// </summary>
    protected void RefreshList()
    {
        bool materials = ShowsMaterials();

        _listIds.Clear();
        _leadingTiles.Clear();

        if (materials)
        {
            CollectMaterials(_listIds, _leadingTiles);
        }
        else
        {
            foreach (CardInstance card in InventoryManager.Instance.GetFiltered(_filterButton.CurrentFilter))
                _listIds.Add(card.InstanceId);
        }

        _grid.SetLeadingTiles(_leadingTiles);
        _grid.SetEmptyMessage(materials ? MaterialEmptyMessage
            : _filterButton.CurrentFilter.SelectedCount > 0 ? "조건에 맞는 카드가 없습니다" : "카드가 없습니다");
        _grid.SetSelection(GetListSelection());
        _grid.SetCards(_listIds);
    }

    /// <summary>
    /// 재료 선택이 바뀌었다 — 체크 표시와 막힌 칸만 다시 칠한다
    /// </summary>
    protected void RefreshMaterialSelection()
    {
        _grid.SetSelection(GetListSelection());
        _grid.RefreshBlocked();
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

    //프리팹에 저장되지 않는 위젯 콜백. 베이크 경로와 코드 조립 경로가 함께 쓴다
    private void WireWidgets()
    {
        _filterButton.SetChangedHandler(OnFilterChanged);
        _grid.SetClickHandler(OnGridClicked);
        _grid.SetLongPressHandler(OnGridLongPressed);
        _grid.SetBlockedReason(GetBlockedReason);
        _grid.SetCheckSelection(true);
        _targetCard.SetLongPressHandler(OnTargetLongPressed);
    }

    //처음엔 위 칸이 비어 있고 아래는 보유 카드, 고르면 위 칸이 채워진다
    private void SetStep(bool hasTarget)
    {
        bool materials = hasTarget && MaterialListLabel != null;

        _emptySlot.gameObject.SetActive(!hasTarget);
        _emptyText.gameObject.SetActive(!hasTarget);
        _workPanel.gameObject.SetActive(hasTarget);
        _lobbyButton.gameObject.SetActive(!hasTarget);
        _stepBackButton.gameObject.SetActive(hasTarget);
        _actionButton.gameObject.SetActive(hasTarget);
        _filterButton.gameObject.SetActive(!materials);
        _listLabel.text = materials ? MaterialListLabel : OwnedListLabel;
    }

    private bool ShowsMaterials()
    {
        return HasTarget && MaterialListLabel != null;
    }

    private IReadOnlyCollection<int> GetListSelection()
    {
        if (ShowsMaterials())
            return MaterialSelection;

        _targetSelection.Clear();

        if (HasTarget)
            _targetSelection.Add(_targetInstanceId);

        return _targetSelection;
    }

    private string GetBlockedReason(CardDisplayInfo info)
    {
        return ShowsMaterials() ? GetMaterialBlockedReason(info) : GetTargetBlockedReason(info);
    }

    private void OnFilterChanged()
    {
        if (!ShowsMaterials())
            RefreshList();
    }

    private void OnGridClicked(int instanceId)
    {
        if (IsPlaying)
            return;

        if (ShowsMaterials())
            OnMaterialClicked(instanceId);
        else
            SelectTarget(instanceId);
    }

    private void OnGridLongPressed(int instanceId)
    {
        _onShowCardDetail?.Invoke(instanceId);
    }

    private void OnTargetLongPressed()
    {
        if (HasTarget)
            _onShowCardDetail?.Invoke(_targetInstanceId);
    }

    //고른 카드가 위 왼쪽에 튀어나온다. 재료를 쓰는 화면은 목록이 재료로 바뀐다
    private void SelectTarget(int instanceId)
    {
        if (instanceId == _targetInstanceId)
            return;

        bool wasEmpty = !HasTarget;

        _targetInstanceId = instanceId;
        OnTargetChanged();
        Refresh();

        if (ShowsMaterials())
            _grid.ScrollToTop();

        if (wasEmpty)
            UiEffects.FadeIn(_workGroup, 0.2f);

        UiEffects.Pop(_targetCard.Root, 0.86f, 0.28f);
    }

    private void ReturnToEmpty()
    {
        if (IsPlaying)
            return;

        bool materials = ShowsMaterials();

        ClearTarget();
        Refresh();

        if (materials)
            _grid.ScrollToTop();
    }

    private void ClearTarget()
    {
        _targetInstanceId = InventoryManager.InvalidInstanceId;
        OnTargetChanged();
    }

    private void OnActionButtonClicked()
    {
        if (IsPlaying || !HasTarget)
            return;

        OnActionClicked();
    }

    private void OnLobbyClicked()
    {
        _onBack?.Invoke();
    }
}
