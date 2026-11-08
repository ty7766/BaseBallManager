using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 1장을 대상으로 삼는 작업 화면 (강화 · 훈련). 처음엔 위 왼쪽이 빈 자리 + 안내, 목록에서 고르면 그 카드가 놓인다.
/// 재료를 쓰는 화면은 대상을 고른 뒤 목록이 재료로 바뀐다 (MaterialListLabel)
/// </summary>
public abstract class TargetWorkScreen : CardWorkScreen
{
    /// <summary>
    /// 왼쪽 대상 카드 · 오른쪽 작업 내용(PreviewLeft ~ PreviewWidth) 규격
    /// </summary>
    protected const float TargetTop = WorkTop;
    protected const float TargetWidth = 340f;
    protected const float TargetHeight = WorkHeight;
    protected const float PreviewLeft = SideMargin + TargetWidth + 32f;
    protected const float PreviewWidth = 1080f - SideMargin - PreviewLeft;
    protected const float PreviewTop = TargetTop + 30f;

    private const float EmptyPlusSize = 84f;
    private const string EmptyPlusSprite = "Common/icon_plus";
    private const string CardMaskSprite = "Fx/card_mask";

    /// <summary>
    /// 빈 자리 옆 안내 ("강화할 카드를 선택하세요")
    /// </summary>
    protected abstract string EmptyMessage { get; }

    /// <summary>
    /// 대상을 고른 뒤 목록을 재료로 바꿀 때의 목록 이름. null 이면 목록은 보유 카드 그대로다
    /// </summary>
    protected virtual string MaterialListLabel => null;

    /// <summary>
    /// 재료 목록이 비었을 때 문구 · 재료 목록에서 체크로 보일 카드
    /// </summary>
    protected virtual string MaterialEmptyMessage => "카드가 없습니다";
    protected virtual IReadOnlyCollection<int> MaterialSelection => null;

    /// <summary>
    /// 위 칸에 놓인 카드. 없으면 InvalidInstanceId
    /// </summary>
    protected int TargetInstanceId => _targetInstanceId;
    protected bool HasTarget => _targetInstanceId != InventoryManager.InvalidInstanceId;

    /// <summary>
    /// 대상이 있을 때만 보이는 작업 내용 루트 · 대상 카드 · 카드 위 번쩍임
    /// </summary>
    protected RectTransform WorkPanel => _workPanel;
    protected CardView TargetCard => _targetCard;
    protected Image CardFlash => _cardFlash;

    protected override bool HasWork => HasTarget;
    protected override string ListLabel => ShowsMaterials() ? MaterialListLabel : base.ListLabel;
    protected override bool ShowsFilter => !ShowsMaterials();
    protected override string ListEmptyMessage => ShowsMaterials() ? MaterialEmptyMessage : null;
    protected override IReadOnlyCollection<int> ListSelection => ShowsMaterials() ? MaterialSelection : GetTargetSelection();

    [SerializeField] private Image _emptySlot;
    [SerializeField] private TextMeshProUGUI _emptyText;
    [SerializeField] private RectTransform _workPanel;
    [SerializeField] private CanvasGroup _workGroup;
    [SerializeField] private CardView _targetCard;
    [SerializeField] private Image _cardFlash;

    private readonly List<int> _targetSelection = new List<int>(1);
    private int _targetInstanceId = InventoryManager.InvalidInstanceId;

    //빈 자리 + 안내, 그 위에 대상이 있을 때 보이는 작업 내용(대상 카드 · 번쩍임 · 화면별 내용)
    protected sealed override void BuildWork()
    {
        _emptySlot = CreateEmptySlot(WorkArea, "EmptySlot", SideMargin, TargetTop, TargetWidth, TargetHeight);

        Image plus = UiBuilder.CreateImage(_emptySlot.transform, "Plus", GetEmptySlotLineColor(), EmptyPlusSprite);
        UiBuilder.Place(plus.rectTransform, (TargetWidth - EmptyPlusSize) * 0.5f, (TargetHeight - EmptyPlusSize) * 0.5f,
            EmptyPlusSize, EmptyPlusSize);

        _emptyText = UiBuilder.CreateText(WorkArea, "EmptyText", 36f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Place(_emptyText.rectTransform, PreviewLeft, TargetTop, PreviewWidth, TargetHeight);
        _emptyText.text = EmptyMessage;

        _workPanel = UiBuilder.CreateRect(WorkArea, "WorkPanel");
        UiBuilder.Stretch(_workPanel, 0f, 0f, 0f, 0f);
        _workGroup = _workPanel.gameObject.AddComponent<CanvasGroup>();

        _targetCard = CardView.Bake(_workPanel, TargetWidth, false);
        UiBuilder.CenterPivot(UiBuilder.Place(_targetCard.Root, SideMargin, TargetTop, TargetWidth, TargetHeight));

        _cardFlash = UiBuilder.CreateImage(_workPanel, "CardFlash", UiPalette.WithAlpha(Color.white, 0f), CardMaskSprite);
        UiBuilder.Place(_cardFlash.rectTransform, SideMargin, TargetTop, TargetWidth, TargetHeight);

        BuildTargetWork();
        _workPanel.gameObject.SetActive(false);
    }

    //처음엔 빈 자리 + 안내, 고르면 대상 카드와 작업 내용
    protected sealed override void RefreshWork(bool animate)
    {
        bool hasTarget = CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo info);

        _emptySlot.gameObject.SetActive(!hasTarget);
        _emptyText.gameObject.SetActive(!hasTarget);
        _workPanel.gameObject.SetActive(hasTarget);

        if (!hasTarget)
            return;

        _targetCard.SetCard(info);
        RefreshTargetWork(info, animate);
    }

    protected sealed override void OnListCardClicked(int instanceId)
    {
        if (ShowsMaterials())
            OnMaterialClicked(instanceId);
        else
            SelectTarget(instanceId);
    }

    protected sealed override void ClearWork()
    {
        _targetInstanceId = InventoryManager.InvalidInstanceId;
        OnTargetChanged();
    }

    //대상 카드가 사라졌으면(분해 등) 비운다
    protected override void ValidateWork()
    {
        if (HasTarget && !CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo _))
            ClearWork();
    }

    protected override void CollectListCards(List<int> instanceIds, List<RectTransform> leadingTiles)
    {
        if (ShowsMaterials())
            CollectMaterials(instanceIds, leadingTiles);
        else
            CollectOwnedCards(instanceIds);
    }

    protected override string GetBlockedReason(CardDisplayInfo info)
    {
        return ShowsMaterials() ? GetMaterialBlockedReason(info) : GetTargetBlockedReason(info);
    }

    protected override void WireWork()
    {
        _targetCard.SetLongPressHandler(OnTargetLongPressed);
    }

    /// <summary>
    /// 오른쪽 작업 내용(WorkPanel 안, PreviewLeft · PreviewTop 기준)을 조립한다. 베이크 시점에 한 번
    /// </summary>
    protected abstract void BuildTargetWork();

    /// <summary>
    /// 작업 내용을 대상 카드에 맞춰 갱신한다. 실행 버튼 상태도 여기서 SetAction 으로 정한다
    /// </summary>
    protected abstract void RefreshTargetWork(CardDisplayInfo target, bool animate);

    /// <summary>
    /// 대상이 바뀌거나 비워졌다. 화면이 들고 있던 재료 선택 등을 비운다
    /// </summary>
    protected virtual void OnTargetChanged()
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
    /// 보유 카드 목록 · 재료 목록에서 고를 수 없는 사유
    /// </summary>
    protected virtual string GetTargetBlockedReason(CardDisplayInfo info)
    {
        return null;
    }

    protected virtual string GetMaterialBlockedReason(CardDisplayInfo info)
    {
        return null;
    }

    /// <summary>
    /// 대상 카드 그림과 작업 내용만 다시 그린다
    /// </summary>
    protected void RefreshTarget(bool animate)
    {
        RefreshWork(animate);
    }

    private bool ShowsMaterials()
    {
        return HasTarget && MaterialListLabel != null;
    }

    private IReadOnlyCollection<int> GetTargetSelection()
    {
        _targetSelection.Clear();

        if (HasTarget)
            _targetSelection.Add(_targetInstanceId);

        return _targetSelection;
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
            Grid.ScrollToTop();

        if (wasEmpty)
            UiEffects.FadeIn(_workGroup, 0.2f);

        UiEffects.Pop(_targetCard.Root, 0.86f, 0.28f);
    }

    private void OnTargetLongPressed()
    {
        if (HasTarget)
            ShowCardDetail(_targetInstanceId);
    }
}
