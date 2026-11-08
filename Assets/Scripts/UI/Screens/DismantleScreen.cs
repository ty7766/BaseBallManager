using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 분해. 위 칸은 담은 카드 줄 + 반환 재화 미리보기, 아래는 보유 카드. 목록에서 누른 카드가 줄에 담긴다(최대 30장).
/// 분해하면 담은 카드가 한꺼번에 터지며 사라지고 받은 재화가 올라간다
/// </summary>
public class DismantleScreen : CardWorkScreen
{
    private const int MaxBatchCount = 30;
    private const int RewardCount = 3;
    //담은 카드 줄 — 실선 칸 안에서 가로로 스크롤
    private const float TrayTop = WorkTop;
    private const float TrayHeight = 236f;
    private const float TrayWidth = 1080f - SideMargin * 2f;
    private const float TrayCardWidth = 136f;
    private const float TrayCardHeight = TrayCardWidth * CardView.BaseHeight / CardView.BaseWidth;
    private const float TrayCardGap = 12f;
    private const float TrayPadding = (TrayHeight - TrayCardHeight) * 0.5f;
    //반환 재화 칩 3개 · 담은 수 · 훈련 카드 확률 안내
    private const float RewardTop = TrayTop + TrayHeight + 28f;
    private const float RewardChipWidth = 300f;
    private const float RewardChipHeight = 76f;
    private const float RewardChipGap = 18f;
    private const float RewardIconSize = 54f;
    private const float CountTop = RewardTop + RewardChipHeight + 20f;
    private const float NoteTop = CountTop + 56f;
    private const float ConsumeSeconds = 0.36f;
    private const string LockedReason = "잠금";
    private const string LineUpReason = "라인업";

    private static readonly string[] RewardIconPaths =
    {
        "Currency/currency_point",
        "Currency/currency_train_card",
        "Currency/currency_goldenglove_point"
    };

    private static readonly string[] RewardLabels = { "포인트", "훈련 카드", "골글 포인트" };

    /// <summary>
    /// 화면 제목 · 실행 버튼
    /// </summary>
    protected override string Title => "분해";
    protected override string ActionLabel => "분해";

    /// <summary>
    /// 1장이라도 담았는가
    /// </summary>
    protected override bool HasWork => _pickedIds.Count > 0;

    /// <summary>
    /// 담은 카드
    /// </summary>
    protected override IReadOnlyCollection<int> ListSelection => _pickedIds;

    [SerializeField] private ScrollRect _trayScroll;
    [SerializeField] private RectTransform _trayContent;
    [SerializeField] private TextMeshProUGUI _trayEmptyText;
    [SerializeField] private TextMeshProUGUI[] _rewardAmounts = new TextMeshProUGUI[RewardCount];
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private TextMeshProUGUI _noteText;

    private readonly List<int> _pickedIds = new List<int>(MaxBatchCount);
    private readonly List<CardView> _trayCards = new List<CardView>(MaxBatchCount);

    //담은 카드 줄(빈 칸 모양 + 가로 스크롤) · 반환 재화 칩 · 담은 수 · 안내
    protected override void BuildWork()
    {
        Image tray = CreateEmptySlot(WorkArea, "Tray", SideMargin, TrayTop, TrayWidth, TrayHeight);

        _trayScroll = UiBuilder.CreateTableScroll(tray.transform, "TrayScroll", out _trayContent);
        UiBuilder.Stretch(_trayScroll.GetComponent<RectTransform>(), 8f, 0f, 8f, 0f);
        _trayScroll.vertical = false;

        _trayEmptyText = UiBuilder.CreateText(tray.transform, "Empty", 36f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Stretch(_trayEmptyText.rectTransform, 0f, 0f, 0f, 0f);
        _trayEmptyText.text = "분해할 카드를 선택하세요";

        float rowWidth = RewardChipWidth * RewardCount + RewardChipGap * (RewardCount - 1);
        float rowLeft = SideMargin + (TrayWidth - rowWidth) * 0.5f;

        for (int i = 0; i < RewardCount; i++)
            BuildRewardChip(i, rowLeft + i * (RewardChipWidth + RewardChipGap));

        _countText = UiBuilder.CreateText(WorkArea, "Count", 40f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.CenterPivot(UiBuilder.Place(_countText.rectTransform, SideMargin, CountTop, TrayWidth, 52f));

        _noteText = UiBuilder.CreateText(WorkArea, "Note", 24f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(_noteText.rectTransform, SideMargin, NoteTop, TrayWidth, 34f);
    }

    //담은 카드 줄 · 반환 재화 미리보기 · 담은 수
    protected override void RefreshWork(bool animate)
    {
        RefreshTray();
        RefreshRewardPreview();

        _countText.text = $"{_pickedIds.Count} / {MaxBatchCount}";

        if (animate)
            UiEffects.Pop(_countText.transform, 0.85f, 0.2f);

        SetAction(_pickedIds.Count > 0, _pickedIds.Count > 0 ? $"분해  {_pickedIds.Count}장" : ActionLabel);
    }

    protected override void OnListCardClicked(int instanceId)
    {
        if (!_pickedIds.Remove(instanceId))
        {
            if (_pickedIds.Count >= MaxBatchCount)
            {
                Notify($"한 번에 {MaxBatchCount}장까지 분해할 수 있습니다", false);
                return;
            }

            _pickedIds.Add(instanceId);
        }

        OnPickedChanged(true);
    }

    protected override void ClearWork()
    {
        _pickedIds.Clear();
    }

    //잠그거나 라인업에 넣어 분해할 수 없게 된 카드는 뺀다
    protected override void ValidateWork()
    {
        for (int i = _pickedIds.Count - 1; i >= 0; i--)
        {
            if (!CardDisplayInfo.TryCreate(_pickedIds[i], out CardDisplayInfo info) || GetBlockedReason(info) != null)
                _pickedIds.RemoveAt(i);
        }
    }

    protected override string GetBlockedReason(CardDisplayInfo info)
    {
        if (info.IsLocked)
            return LockedReason;

        return LineUpManager.Instance != null && LineUpManager.Instance.IsCardAssigned(info.InstanceId) ? LineUpReason : null;
    }

    protected override void OnActionClicked()
    {
        if (DismantleManager.Instance == null || _pickedIds.Count == 0)
            return;

        int[] received = new int[RewardCount];
        int done = 0;

        foreach (int instanceId in _pickedIds)
        {
            DismantleResult result = DismantleManager.Instance.Dismantle(instanceId);

            if (result == null)
                continue;

            received[0] += result.Point;
            received[1] += result.TrainCard;
            received[2] += result.GoldenGlovePoint;
            done++;
        }

        if (done == 0)
        {
            Notify("분해하지 못했습니다", false);
            Refresh();
            return;
        }

        int shownCards = _pickedIds.Count;

        _pickedIds.Clear();
        PlaySequence(PlayDismantle(shownCards, done, received));
    }

    //아이콘 · 받을 양 · 재화 이름
    private void BuildRewardChip(int index, float left)
    {
        Image plate = UiBuilder.CreateImage(WorkArea, "RewardChip", UiPalette.BgRow, UiSprites.ButtonPrimary, Image.Type.Sliced);
        UiBuilder.Place(plate.rectTransform, left, RewardTop, RewardChipWidth, RewardChipHeight);

        Image icon = UiBuilder.CreateImage(plate.transform, "Icon", Color.white, RewardIconPaths[index]);
        UiBuilder.Place(icon.rectTransform, 14f, (RewardChipHeight - RewardIconSize) * 0.5f, RewardIconSize, RewardIconSize);
        icon.preserveAspect = true;

        _rewardAmounts[index] = UiBuilder.CreateText(plate.transform, "Amount", 32f, UiPalette.Text1);
        UiBuilder.CenterPivot(UiBuilder.Place(_rewardAmounts[index].rectTransform, 80f, 6f, RewardChipWidth - 92f, 38f));

        TextMeshProUGUI label = UiBuilder.CreateText(plate.transform, "Label", 22f, UiPalette.Text3);
        UiBuilder.Place(label.rectTransform, 80f, 44f, RewardChipWidth - 92f, 26f);
        label.text = RewardLabels[index];
    }

    //담은 카드를 가로 한 줄로 깐다. 칸은 필요한 만큼만 만들고 재사용한다
    private void RefreshTray()
    {
        for (int i = 0; i < _pickedIds.Count; i++)
        {
            CardView card = EnsureTrayCard(i);

            card.Root.localScale = Vector3.one;
            card.Root.gameObject.SetActive(true);

            if (CardDisplayInfo.TryCreate(_pickedIds[i], out CardDisplayInfo info))
                card.SetCard(info);
        }

        for (int i = _pickedIds.Count; i < _trayCards.Count; i++)
            _trayCards[i].Root.gameObject.SetActive(false);

        _trayEmptyText.gameObject.SetActive(_pickedIds.Count == 0);
        _trayContent.sizeDelta = new Vector2(TrayPadding * 2f + _pickedIds.Count * (TrayCardWidth + TrayCardGap) - TrayCardGap, 0f);
    }

    //줄의 카드는 런타임에 만든다. 누르면 줄에서 빠지고, 길게 누르면 상세
    private CardView EnsureTrayCard(int index)
    {
        while (_trayCards.Count <= index)
        {
            int slot = _trayCards.Count;
            CardView created = CardView.Bake(_trayContent, TrayCardWidth, false);

            UiBuilder.CenterPivot(UiBuilder.Place(created.Root, TrayPadding + slot * (TrayCardWidth + TrayCardGap), TrayPadding,
                TrayCardWidth, TrayCardHeight));
            created.SetClickHandler(() => RemovePickedAt(slot));
            created.SetLongPressHandler(() => ShowPickedDetail(slot));
            _trayCards.Add(created);
        }

        return _trayCards[index];
    }

    //담은 카드 전부의 확정 보상을 더한다. 훈련 카드는 장마다 확률이라 0 ~ 최대로 보인다
    private void RefreshRewardPreview()
    {
        int point = 0;
        int goldenGlovePoint = 0;

        if (DismantleManager.Instance != null)
        {
            foreach (int instanceId in _pickedIds)
            {
                DismantleResult preview = DismantleManager.Instance.GetPreview(instanceId);

                if (preview == null)
                    continue;

                point += preview.Point;
                goldenGlovePoint += preview.GoldenGlovePoint;
            }
        }

        int maxTrainCard = DismantleManager.Instance == null ? 0 : _pickedIds.Count * DismantleManager.Instance.TrainCardDropAmount;
        float dropChance = DismantleManager.Instance == null ? 0f : DismantleManager.Instance.TrainCardDropChance;

        _rewardAmounts[0].text = point.ToString("N0");
        _rewardAmounts[1].text = maxTrainCard > 0 ? $"0 ~ {maxTrainCard:N0}" : "0";
        _rewardAmounts[2].text = goldenGlovePoint.ToString("N0");
        _noteText.text = $"훈련 카드는 장마다 {dropChance * 100f:0}% 확률로 나옵니다";
    }

    private void OnPickedChanged(bool scrollToEnd)
    {
        RefreshChrome();
        RefreshListSelection();
        RefreshWork(true);

        if (scrollToEnd)
            _trayScroll.horizontalNormalizedPosition = 1f;
    }

    private void RemovePickedAt(int slot)
    {
        if (IsPlaying || slot >= _pickedIds.Count)
            return;

        _pickedIds.RemoveAt(slot);
        OnPickedChanged(false);
    }

    private void ShowPickedDetail(int slot)
    {
        if (slot < _pickedIds.Count)
            ShowCardDetail(_pickedIds[slot]);
    }

    //담은 카드가 한꺼번에 터지며 작아지고, 받은 재화가 0 에서 올라간다
    private IEnumerator PlayDismantle(int shownCards, int done, int[] received)
    {
        SetAction(false, ActionLabel);

        for (int i = 0; i < shownCards && i < _trayCards.Count; i++)
        {
            UiEffects.Burst(_trayCards[i].Root, UiEffectKind.Enhance);
            ShrinkAway(_trayCards[i].Root);
        }

        yield return new WaitForSecondsRealtime(ConsumeSeconds + 0.1f);

        for (int i = 0; i < RewardCount; i++)
        {
            UiEffects.CountUp(_rewardAmounts[i], 0, received[i], "{0:N0}", 0.5f);
            UiEffects.Pop(_rewardAmounts[i].transform, 0.7f, 0.3f);
        }

        Notify($"{done}장 분해 — 포인트 {received[0]:N0} · 훈련 카드 {received[1]} · 골글 포인트 {received[2]:N0}", true);

        yield return new WaitForSecondsRealtime(0.8f);

        RefreshChrome();
        RefreshList();
        RefreshWork(false);
    }

    private static void ShrinkAway(RectTransform card)
    {
        UiAnimator.Play(ConsumeSeconds, t =>
        {
            float scale = 1f - t;

            card.localScale = new Vector3(scale, scale, 1f);
        }, () => card.gameObject.SetActive(false), UiEase.InCubic);
    }
}
