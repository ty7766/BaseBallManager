using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 훈련과 훈련돌파. 카드를 고르면 위 칸에 훈련 단계 · 4개 스탯 · 비용이 나오고, 훈련을 누를 때마다 1단계씩 오른다.
/// 상한에 닿으면 같은 버튼이 돌파로 바뀌어 상한을 연다. 아래 목록은 보유 카드 그대로라 다른 카드를 누르면 바로 바뀐다
/// </summary>
public class TrainScreen : CardWorkScreen
{
    //지금 카드로 할 수 있는 것
    private enum TrainStep
    {
        Train,
        Breakthrough,
        Done
    }

    private const int StatCount = CardInstance.TrainStatCount;
    private const int CostChipCount = 2;
    //이름 · 단계 글자 · 단계 막대 · 스탯 2×2 · 비용 칩 · 안내 순으로 위에서 아래. 카드 높이 안에서 가운데쯤
    private const float NameTop = TargetTop + 56f;
    private const float LevelTop = NameTop + 62f;
    private const float LevelHeight = 48f;
    private const float BarWidth = 600f;
    private const float BarHeight = 16f;
    private const float BarLeft = PreviewLeft + (PreviewWidth - BarWidth) * 0.5f;
    private const float BarTop = LevelTop + LevelHeight + 10f;
    private const int StatColumns = 2;
    private const float StatTop = BarTop + BarHeight + 24f;
    private const float StatRowHeight = 52f;
    private const float StatCellWidth = PreviewWidth / StatColumns;
    private const float CostTop = StatTop + StatRowHeight * (StatCount / StatColumns) + 20f;
    private const float CostChipWidth = 290f;
    private const float CostChipHeight = 64f;
    private const float CostChipGap = 24f;
    private const float CostIconSize = 52f;
    private const float BarSlideSeconds = 0.3f;
    private const float GainFadeDelay = 0.5f;
    private const float GainFadeSeconds = 0.6f;
    private const float FlashPeak = 0.4f;
    private const float FlashSeconds = 0.28f;
    private const string PointIcon = "Currency/currency_point";
    private const string TrainCardIcon = "Currency/currency_train_card";
    private const string BreakthroughIcon = "Currency/currency_breakthrough_card";
    private const string MaxLevelColorTag = "<color=#6E7C92>";
    private const string MaxReason = "최대 훈련";

    /// <summary>
    /// 화면 제목 · 빈 칸 안내 · 실행 버튼
    /// </summary>
    protected override string Title => "훈련";
    protected override string EmptyMessage => "훈련할 카드를 선택하세요";
    protected override string ActionLabel => "훈련";

    [SerializeField] private TextMeshProUGUI _targetName;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Image _levelBarFill;
    [SerializeField] private TextMeshProUGUI[] _statLabels = new TextMeshProUGUI[StatCount];
    [SerializeField] private TextMeshProUGUI[] _statValues = new TextMeshProUGUI[StatCount];
    [SerializeField] private TextMeshProUGUI[] _statDeltas = new TextMeshProUGUI[StatCount];
    [SerializeField] private TextMeshProUGUI[] _statGains = new TextMeshProUGUI[StatCount];
    [SerializeField] private RectTransform[] _costChips = new RectTransform[CostChipCount];
    [SerializeField] private Image[] _costIcons = new Image[CostChipCount];
    [SerializeField] private TextMeshProUGUI[] _costAmounts = new TextMeshProUGUI[CostChipCount];
    [SerializeField] private TextMeshProUGUI[] _costOwned = new TextMeshProUGUI[CostChipCount];
    [SerializeField] private TextMeshProUGUI _noteText;

    private readonly UiTween[] _gainTweens = new UiTween[StatCount];
    private UiTween _barTween;
    private UiTween _flashTween;

    //이름 · 단계 · 막대 · 스탯 2×2(이름 · 값 · 훈련 누적 · 이번 상승) · 비용 칩 2개 · 안내
    protected override void BuildWork()
    {
        _targetName = CreatePreviewText("TargetName", 40f, UiPalette.Text1, NameTop, 52f);
        _levelText = CreatePreviewText("Level", 38f, UiPalette.Text1, LevelTop, LevelHeight);
        UiBuilder.CenterPivot(_levelText.rectTransform);

        Image track = UiBuilder.CreateImage(WorkPanel, "LevelBar", UiPalette.WithAlpha(UiPalette.Text1, 0.12f),
            UiSprites.ScrollHandle, Image.Type.Sliced);
        UiBuilder.Place(track.rectTransform, BarLeft, BarTop, BarWidth, BarHeight);

        _levelBarFill = UiBuilder.CreateImage(track.transform, "Fill", UiPalette.WithAlpha(UiPalette.Text1, 0.95f),
            UiSprites.ScrollHandle, Image.Type.Sliced);
        UiBuilder.Place(_levelBarFill.rectTransform, 0f, 0f, 0f, BarHeight);

        for (int i = 0; i < StatCount; i++)
            BuildStatCell(i, PreviewLeft + i % StatColumns * StatCellWidth, StatTop + i / StatColumns * StatRowHeight);

        for (int i = 0; i < CostChipCount; i++)
            BuildCostChip(i);

        _noteText = CreatePreviewText("Note", 24f, UiPalette.Text3, CostTop + CostChipHeight + 8f, 34f);
    }

    protected override void OnTargetChanged()
    {
        for (int i = 0; i < StatCount; i++)
        {
            CancelTween(ref _gainTweens[i]);
            _statGains[i].text = string.Empty;
        }
    }

    //더 훈련도 돌파도 못 하는 카드는 막는다
    protected override string GetTargetBlockedReason(CardDisplayInfo info)
    {
        return GetStep(info.InstanceId) == TrainStep.Done ? MaxReason : null;
    }

    //단계 · 스탯은 늘 보이고, 비용 칩과 버튼은 훈련 / 돌파 / 끝 에 따라 바뀐다
    protected override void RefreshWork(CardDisplayInfo target, bool animate)
    {
        int maxLevel = target.GetMaxTrainLevel();
        string[] labels = target.StatLabels;

        _targetName.text = PlayerLabelFormat.NameWithYear(target.Name, target.Year);
        _levelText.text = $"훈련 Lv.{target.TrainLevel}  {MaxLevelColorTag}/ {maxLevel}</color>";
        _levelBarFill.color = target.TrainLevel >= maxLevel ? UiPalette.AccentGold : UiPalette.WithAlpha(UiPalette.Text1, 0.95f);
        SetBar(maxLevel > 0 ? (float)target.TrainLevel / maxLevel : 0f, animate);

        for (int i = 0; i < StatCount; i++)
        {
            int trainDelta = target.GetTrainDelta(i);

            _statLabels[i].text = labels[i];
            _statValues[i].text = target.GetFinalStat(i).ToString();
            _statDeltas[i].text = trainDelta > 0 ? $"+{trainDelta}" : string.Empty;
        }

        switch (GetStep(target.InstanceId))
        {
            case TrainStep.Train:
                ShowTrainCost(target);
                break;
            case TrainStep.Breakthrough:
                ShowBreakthroughCost(target);
                break;
            default:
                SetCostChipCount(0);
                _noteText.text = "훈련을 모두 마친 카드입니다";
                SetAction(false, "최대 훈련");
                break;
        }
    }

    protected override void OnActionClicked()
    {
        switch (GetStep(TargetInstanceId))
        {
            case TrainStep.Train:
                Train();
                break;
            case TrainStep.Breakthrough:
                Breakthrough();
                break;
        }
    }

    private TextMeshProUGUI CreatePreviewText(string name, float fontSize, Color color, float top, float height)
    {
        TextMeshProUGUI text = UiBuilder.CreateText(WorkPanel, name, fontSize, color, TextAlignmentOptions.Center);

        UiBuilder.Place(text.rectTransform, PreviewLeft, top, PreviewWidth, height);

        return text;
    }

    //한 칸 = 스탯 이름 · 최종 값 · 훈련 누적(+5). 이번에 오른 만큼(+1)은 누적 위에 잠깐 떴다 사라진다
    private void BuildStatCell(int index, float left, float top)
    {
        _statLabels[index] = CreateStatText("StatLabel", 30f, UiPalette.Text2, TextAlignmentOptions.MidlineLeft,
            left + 20f, top, 120f, StatRowHeight);
        _statValues[index] = CreateStatText("StatValue", 38f, UiPalette.Text1, TextAlignmentOptions.MidlineRight,
            left + 140f, top, 90f, StatRowHeight);
        UiBuilder.CenterPivot(_statValues[index].rectTransform);
        _statDeltas[index] = CreateStatText("StatDelta", 24f, UiPalette.Text3, TextAlignmentOptions.MidlineLeft,
            left + 240f, top, 70f, StatRowHeight);
        _statGains[index] = CreateStatText("StatGain", 26f, UiPalette.AccentGold, TextAlignmentOptions.MidlineLeft,
            left + 240f, top - 16f, 70f, 30f);
        UiBuilder.CenterPivot(_statGains[index].rectTransform);
    }

    private TextMeshProUGUI CreateStatText(string name, float fontSize, Color color, TextAlignmentOptions alignment,
        float left, float top, float width, float height)
    {
        TextMeshProUGUI text = UiBuilder.CreateText(WorkPanel, name, fontSize, color, alignment);

        UiBuilder.Place(text.rectTransform, left, top, width, height);

        return text;
    }

    //아이콘 · 드는 양 · 보유량
    private void BuildCostChip(int index)
    {
        Image plate = UiBuilder.CreateImage(WorkPanel, "CostChip", UiPalette.BgRow, UiSprites.ButtonPrimary, Image.Type.Sliced);
        _costChips[index] = plate.rectTransform;
        UiBuilder.Place(_costChips[index], 0f, CostTop, CostChipWidth, CostChipHeight);

        _costIcons[index] = UiBuilder.CreateImage(plate.transform, "Icon", Color.white);
        UiBuilder.Place(_costIcons[index].rectTransform, 12f, (CostChipHeight - CostIconSize) * 0.5f, CostIconSize, CostIconSize);
        _costIcons[index].preserveAspect = true;

        _costAmounts[index] = UiBuilder.CreateText(plate.transform, "Amount", 30f, UiPalette.Text1);
        UiBuilder.Place(_costAmounts[index].rectTransform, 76f, 4f, CostChipWidth - 88f, 34f);

        _costOwned[index] = UiBuilder.CreateText(plate.transform, "Owned", 22f, UiPalette.Text3);
        UiBuilder.Place(_costOwned[index].rectTransform, 76f, 36f, CostChipWidth - 88f, 24f);
    }

    private static TrainStep GetStep(int instanceId)
    {
        if (TrainManager.Instance != null && TrainManager.Instance.CanTrain(instanceId))
            return TrainStep.Train;

        if (BreakthroughManager.Instance != null && BreakthroughManager.Instance.CanBreakthrough(instanceId))
            return TrainStep.Breakthrough;

        return TrainStep.Done;
    }

    //포인트 · 훈련 카드. 모자란 쪽은 빨간 글자
    private void ShowTrainCost(CardDisplayInfo target)
    {
        (int pointCost, int trainCardCost) = TrainManager.Instance.GetTrainCost(target.TrainLevel);

        SetCostChipCount(2);
        bool pointEnough = SetCostChip(0, PointIcon, pointCost, CurrencyType.Point);
        bool cardEnough = SetCostChip(1, TrainCardIcon, trainCardCost, CurrencyType.TrainCard);

        _noteText.text = string.Empty;
        SetAction(pointEnough && cardEnough, ActionLabel);
    }

    //돌파 카드 한 칸. 돌파하면 상한이 얼마로 열리는지 알린다
    private void ShowBreakthroughCost(CardDisplayInfo target)
    {
        int cost = BreakthroughManager.Instance.GetBreakthroughCost(target.InstanceId);
        int openedLevel = TrainManager.Instance == null ? 0 : TrainManager.Instance.MaxTrainLevelAfterBreakthrough;

        SetCostChipCount(1);
        bool enough = SetCostChip(0, BreakthroughIcon, cost, CurrencyType.BreakthroughCard) && cost > 0;

        _noteText.text = $"돌파하면 훈련 상한 {target.TrainLevel} → {openedLevel}";
        SetAction(enough, "돌파", true);
    }

    private bool SetCostChip(int index, string iconPath, int cost, CurrencyType type)
    {
        int owned = CurrencyManager.Instance == null ? 0 : CurrencyManager.Instance.GetAmount(type);
        bool enough = owned >= cost;

        _costIcons[index].sprite = UiSprites.Get(iconPath);
        _costAmounts[index].text = cost.ToString("N0");
        _costAmounts[index].color = enough ? UiPalette.Text1 : UiPalette.StateOut;
        _costOwned[index].text = $"보유 {owned:N0}";

        return enough;
    }

    //칩 수에 맞춰 가운데로 모은다
    private void SetCostChipCount(int count)
    {
        float rowWidth = count * CostChipWidth + Mathf.Max(0, count - 1) * CostChipGap;
        float left = PreviewLeft + (PreviewWidth - rowWidth) * 0.5f;

        for (int i = 0; i < CostChipCount; i++)
        {
            bool shown = i < count;

            _costChips[i].gameObject.SetActive(shown);

            if (shown)
                _costChips[i].anchoredPosition = new Vector2(left + i * (CostChipWidth + CostChipGap), -CostTop);
        }
    }

    private void SetBar(float ratio, bool animate)
    {
        RectTransform fill = _levelBarFill.rectTransform;
        float targetWidth = BarWidth * Mathf.Clamp01(ratio);

        CancelTween(ref _barTween);

        if (!animate)
        {
            fill.sizeDelta = new Vector2(targetWidth, BarHeight);
            return;
        }

        float fromWidth = fill.sizeDelta.x;

        _barTween = UiAnimator.Play(BarSlideSeconds,
            t => fill.sizeDelta = new Vector2(Mathf.LerpUnclamped(fromWidth, targetWidth, t), BarHeight),
            () => fill.sizeDelta = new Vector2(targetWidth, BarHeight), UiEase.OutCubic);
    }

    private void Train()
    {
        if (TrainManager.Instance == null || !CardDisplayInfo.TryCreate(TargetInstanceId, out CardDisplayInfo before))
            return;

        if (!TrainManager.Instance.Train(TargetInstanceId))
        {
            Notify("포인트나 훈련 카드가 부족합니다", false);
            RefreshTarget(false);
            return;
        }

        RefreshTarget(true);

        if (!CardDisplayInfo.TryCreate(TargetInstanceId, out CardDisplayInfo after))
            return;

        for (int i = 0; i < StatCount; i++)
        {
            int gain = after.GetFinalStat(i) - before.GetFinalStat(i);

            if (gain > 0)
                ShowGain(i, gain);
        }

        UiEffects.Pop(_levelText.transform, 0.85f, 0.22f);
        FlashCard(FlashPeak, FlashSeconds);
        Grid.RefreshCard(TargetInstanceId);
    }

    private void Breakthrough()
    {
        if (BreakthroughManager.Instance == null)
            return;

        if (!BreakthroughManager.Instance.Breakthrough(TargetInstanceId))
        {
            Notify("돌파 카드가 부족합니다", false);
            RefreshTarget(false);
            return;
        }

        RefreshTarget(true);
        Grid.RefreshCard(TargetInstanceId);

        UiEffects.Pop(TargetCard.Root, 0.9f, 0.3f);
        UiEffects.Pop(_levelText.transform, 0.7f, 0.3f);
        UiEffects.Burst(TargetCard.Root, UiEffectKind.GradeUp);
        FlashCard(0.8f, 0.5f);

        int openedLevel = TrainManager.Instance == null ? 0 : TrainManager.Instance.MaxTrainLevelAfterBreakthrough;

        Notify($"훈련 상한이 {openedLevel}까지 열렸습니다", true);
    }

    //오른 스탯 옆에 금색 +N 이 튀어나왔다 사라진다. 연달아 눌러도 앞의 것을 끊고 새로 띄운다
    private void ShowGain(int index, int gain)
    {
        TextMeshProUGUI text = _statGains[index];

        CancelTween(ref _gainTweens[index]);
        text.text = $"+{gain}";
        text.color = UiPalette.AccentGold;
        UiEffects.Pop(text.transform, 0.5f, 0.24f);
        UiEffects.Pop(_statValues[index].transform, 0.8f, 0.24f);

        _gainTweens[index] = UiAnimator.Play(GainFadeSeconds,
            t => text.color = UiPalette.WithAlpha(UiPalette.AccentGold, 1f - t),
            () => text.text = string.Empty, UiEase.Linear, GainFadeDelay);
    }

    //연달아 눌러도 번쩍임이 쌓이지 않게 앞의 것을 끊는다
    private void FlashCard(float peak, float seconds)
    {
        Image flash = CardFlash;

        CancelTween(ref _flashTween);
        _flashTween = UiAnimator.Play(seconds,
            t => flash.color = UiPalette.WithAlpha(Color.white, peak * UiEase.Pulse(t)),
            () => flash.color = UiPalette.WithAlpha(Color.white, 0f), UiEase.Linear);
    }

    private static void CancelTween(ref UiTween tween)
    {
        if (tween == null)
            return;

        UiAnimator.Cancel(tween);
        tween = null;
    }
}
