using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 강화. 카드를 고르면 아래 목록이 재료(같은 카드 + 강화 전용 카드)로 바뀐다.
/// 재료를 고를수록 오른쪽에 +3 → +5 처럼 오를 단계를 미리 보여 주고, 강화를 누르면 한 번에 올린다
/// </summary>
public class EnhanceScreen : CardWorkScreen
{
    private const float MedalWidth = 168f;
    private const float MedalHeight = MedalWidth * 208f / 240f;
    private const float MedalFontSize = 76f;
    private const float MedalWideFontSize = 64f;
    private const float ArrowWidth = 112f;
    private const float ArrowHeight = ArrowWidth * 164f / 330f;
    private const float MedalGap = 24f;
    //재료가 없으면 지금 메달 하나만 가운데, 고르면 왼쪽으로 비켜서며 화살표 · 오를 메달이 나온다
    private const float MedalTop = PreviewTop + 72f;
    private const float MedalRowWidth = MedalWidth * 2f + ArrowWidth + MedalGap * 2f;
    private const float MedalRowLeft = PreviewLeft + (PreviewWidth - MedalRowWidth) * 0.5f;
    private const float MedalSingleLeft = PreviewLeft + (PreviewWidth - MedalWidth) * 0.5f;
    private const float MedalSlideSeconds = 0.22f;
    private const float PipRowWidth = 560f;
    private const float PipHeight = 14f;
    private const float PipGap = 8f;
    //강화 연출 — 한 단계씩 올라가는 간격
    private const float StepSeconds = 0.3f;
    private const float SettleSeconds = 0.4f;
    private const string ArrowSprite = "Gacha/swipe_arrow";
    private const string EnhancePlusTag = "<size=60%><voffset=0.12em>+</voffset></size>";
    private const string PreviewColorTag = "<color=#F2C14E>";
    private const string MaxReason = "최대 강화";
    private static readonly Color EmptyMedalColor = new Color(0.36f, 0.38f, 0.42f, 1f);
    private static readonly Color EmptyMedalInk = new Color(0.1f, 0.11f, 0.13f, 1f);

    /// <summary>
    /// 화면 제목 · 빈 칸 안내 · 실행 버튼
    /// </summary>
    protected override string Title => "강화";
    protected override string EmptyMessage => "강화할 카드를 선택하세요";
    protected override string ActionLabel => "강화";

    /// <summary>
    /// 카드를 고르면 목록이 재료로 바뀐다
    /// </summary>
    protected override string MaterialListLabel => "재료 카드";

    /// <summary>
    /// 재료가 없을 때 문구. 이미 최대면 따로 알린다
    /// </summary>
    protected override string MaterialEmptyMessage =>
        IsTargetMaxed() ? "더 강화할 수 없는 카드입니다" : "카드가 없습니다";

    /// <summary>
    /// 고른 같은 카드
    /// </summary>
    protected override IReadOnlyCollection<int> MaterialSelection => _selectedMaterialIds;

    [SerializeField] private TextMeshProUGUI _targetName;
    [SerializeField] private Image _currentMedal;
    [SerializeField] private TextMeshProUGUI _currentMedalText;
    [SerializeField] private Image _arrow;
    [SerializeField] private Image _nextMedal;
    [SerializeField] private TextMeshProUGUI _nextMedalText;
    [SerializeField] private RectTransform _pipRow;
    [SerializeField] private TextMeshProUGUI _ovrText;
    [SerializeField] private TextMeshProUGUI _statText;
    [SerializeField] private TextMeshProUGUI _materialText;

    private readonly List<CardInstance> _materials = new List<CardInstance>(16);
    private readonly List<int> _selectedMaterialIds = new List<int>(10);
    private readonly List<Image> _pips = new List<Image>(10);

    private EnhanceCardTileView _enhanceCardTile;
    private UiTween _medalSlide;
    private bool _medalPairShown = true;
    private int _selectedSets;
    private CurrencyType _enhanceCardType;
    private int _cardsPerLevel = 1;
    private int _ownedEnhanceCards;
    private int _shownGain;
    private bool _medalFontApplied;

    private int Gain => _selectedMaterialIds.Count + _selectedSets;
    private int MaxLevel => EnhanceManager.Instance != null ? EnhanceManager.Instance.MaxEnhanceLevel : 0;

    //이름 · 지금 메달 → 오를 메달 · 단계 칸 · OVR · 세부 스탯 · 재료 요약
    protected override void BuildWork()
    {
        _targetName = CreatePreviewText("TargetName", 40f, UiPalette.Text1, PreviewTop, 52f);

        _currentMedal = CreateMedal("CurrentMedal", MedalRowLeft, MedalTop, out _currentMedalText);

        _arrow = UiBuilder.CreateImage(WorkPanel, "Arrow", UiPalette.Text1, ArrowSprite);
        UiBuilder.CenterPivot(UiBuilder.Place(_arrow.rectTransform, MedalRowLeft + MedalWidth + MedalGap,
            MedalTop + (MedalHeight - ArrowHeight) * 0.5f, ArrowWidth, ArrowHeight));
        _arrow.preserveAspect = true;

        _nextMedal = CreateMedal("NextMedal", MedalRowLeft + MedalWidth + MedalGap * 2f + ArrowWidth, MedalTop,
            out _nextMedalText);

        float pipTop = MedalTop + MedalHeight + 26f;

        _pipRow = UiBuilder.CreateRect(WorkPanel, "LevelPips");
        UiBuilder.Place(_pipRow, PreviewLeft + (PreviewWidth - PipRowWidth) * 0.5f, pipTop, PipRowWidth, PipHeight);

        float ovrTop = pipTop + PipHeight + 18f;

        _ovrText = CreatePreviewText("Ovr", 44f, UiPalette.Text1, ovrTop, 56f);
        UiBuilder.CenterPivot(_ovrText.rectTransform);
        _statText = CreatePreviewText("Stat", 28f, UiPalette.Text2, ovrTop + 60f, 38f);
        _materialText = CreatePreviewText("Material", 26f, UiPalette.Text3, ovrTop + 102f, 38f);
    }

    //카드 앞면 글꼴은 런타임에 만들어지므로 처음 보일 때 입힌다
    protected override void OnWorkShown()
    {
        if (_medalFontApplied)
            return;

        _medalFontApplied = true;

        foreach (TextMeshProUGUI label in new[] { _currentMedalText, _nextMedalText })
        {
            label.font = UiFontProvider.Card;
            label.fontSharedMaterial = CardTextMaterials.Emboss;
        }
    }

    protected override void OnTargetChanged()
    {
        _selectedMaterialIds.Clear();
        _selectedSets = 0;
        _shownGain = 0;
    }

    //같은 카드(덜 키운 것부터) · 맨 앞 전용 카드 칸. 쓸 수 없게 된 선택은 빼고, 최대 단계를 넘는 만큼 덜어 낸다
    protected override void CollectMaterials(List<int> instanceIds, List<RectTransform> leadingTiles)
    {
        if (EnhanceManager.Instance == null)
            return;

        _materials.Clear();

        if (!IsTargetMaxed())
            _materials.AddRange(EnhanceManager.Instance.GetIdenticalCards(TargetInstanceId));

        _materials.Sort(CompareMaterial);

        for (int i = _selectedMaterialIds.Count - 1; i >= 0; i--)
        {
            if (!ContainsMaterial(_selectedMaterialIds[i]))
                _selectedMaterialIds.RemoveAt(i);
        }

        (CurrencyType enhanceCardType, int count) required = EnhanceManager.Instance.GetRequiredEnhanceCard(TargetInstanceId);

        _enhanceCardType = required.enhanceCardType;
        _cardsPerLevel = Mathf.Max(1, required.count);
        _ownedEnhanceCards = CurrencyManager.Instance == null || _enhanceCardType == CurrencyType.None
            ? 0
            : CurrencyManager.Instance.GetAmount(_enhanceCardType);
        _selectedSets = Mathf.Clamp(_selectedSets, 0, _ownedEnhanceCards / _cardsPerLevel);

        while (GetRemainingLevels() < 0 && Gain > 0)
        {
            if (_selectedSets > 0)
                _selectedSets--;
            else
                _selectedMaterialIds.RemoveAt(_selectedMaterialIds.Count - 1);
        }

        foreach (CardInstance material in _materials)
            instanceIds.Add(material.InstanceId);

        if (!ShowsEnhanceCardTile())
            return;

        EnsureEnhanceCardTile();
        _enhanceCardTile.SetCard(_enhanceCardType, _ownedEnhanceCards, _cardsPerLevel);
        _enhanceCardTile.SetSelectedSets(_selectedSets, _cardsPerLevel);
        leadingTiles.Add(_enhanceCardTile.Root);
    }

    protected override void OnMaterialClicked(int instanceId)
    {
        if (!_selectedMaterialIds.Remove(instanceId))
        {
            if (GetRemainingLevels() <= 0)
            {
                Notify($"+{MaxLevel} 까지만 올릴 수 있습니다", false);
                return;
            }

            _selectedMaterialIds.Add(instanceId);
        }

        OnMaterialsChanged();
    }

    //이미 최대인 카드는 고를 수 없다
    protected override string GetTargetBlockedReason(CardDisplayInfo info)
    {
        return info.EnhanceLevel >= MaxLevel ? MaxReason : null;
    }

    //단계가 다 차면 남은 재료를 막는다
    protected override string GetMaterialBlockedReason(CardDisplayInfo info)
    {
        return GetRemainingLevels() <= 0 && !_selectedMaterialIds.Contains(info.InstanceId) ? MaxReason : null;
    }

    //지금 단계 → 오를 단계 메달, 단계 칸(채움 · 이번에 오를 칸 · 빈 칸), OVR 변화
    protected override void RefreshWork(CardDisplayInfo target, bool animate)
    {
        int maxLevel = MaxLevel;
        int current = target.EnhanceLevel;
        int gain = Gain;
        int next = Mathf.Min(maxLevel, current + gain);
        bool hasGain = gain > 0;

        _targetName.text = PlayerLabelFormat.NameWithYear(target.Name, target.Year);

        ApplyMedal(_currentMedal, _currentMedalText, current);
        ApplyMedal(_nextMedal, _nextMedalText, next);
        SetMedalPair(hasGain, animate);
        UpdatePips(current, next, maxLevel, animate);

        int nextOvr = target.FinalOvr + GetOvrGain(target, gain);

        _ovrText.text = hasGain
            ? $"OVR  {target.FinalOvr}  →  {PreviewColorTag}{nextOvr}</color>"
            : $"OVR  {target.FinalOvr}";
        _statText.text = hasGain ? $"세부 스탯 각 +{CardStatsCalculator.CalculateFinalStat(0, gain, 0)}" : string.Empty;
        _materialText.text = BuildMaterialSummary(current >= maxLevel);

        SetAction(hasGain, hasGain ? $"강화  +{gain}" : ActionLabel);

        if (animate && gain != _shownGain)
        {
            UiEffects.Pop(_nextMedal.transform, 0.7f, 0.26f);
            UiEffects.Pop(_ovrText.transform, 0.88f, 0.22f);
        }

        _shownGain = gain;
    }

    protected override void OnActionClicked()
    {
        if (Gain <= 0 || EnhanceManager.Instance == null)
            return;

        if (!CardDisplayInfo.TryCreate(TargetInstanceId, out CardDisplayInfo before))
            return;

        if (!EnhanceManager.Instance.Enhance(TargetInstanceId, _selectedMaterialIds, _selectedSets))
        {
            Notify("강화하지 못했습니다", false);
            Refresh();
            return;
        }

        _selectedMaterialIds.Clear();
        _selectedSets = 0;
        PlaySequence(PlayEnhance(before));
    }

    private TextMeshProUGUI CreatePreviewText(string name, float fontSize, Color color, float top, float height)
    {
        TextMeshProUGUI text = UiBuilder.CreateText(WorkPanel, name, fontSize, color, TextAlignmentOptions.Center);

        UiBuilder.Place(text.rectTransform, PreviewLeft, top, PreviewWidth, height);

        return text;
    }

    //카드 앞면과 같은 육각 메달 + 새긴 숫자
    private Image CreateMedal(string name, float left, float top, out TextMeshProUGUI label)
    {
        Image medal = UiBuilder.CreateImage(WorkPanel, name, Color.white, EnhanceBadgeStyle.GetSpritePath(1));
        UiBuilder.CenterPivot(UiBuilder.Place(medal.rectTransform, left, top, MedalWidth, MedalHeight));

        label = UiBuilder.CreateText(medal.transform, "Level", MedalFontSize, Color.black, TextAlignmentOptions.Center);
        UiBuilder.Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

        return medal;
    }

    private bool IsTargetMaxed()
    {
        return EnhanceManager.Instance == null || !EnhanceManager.Instance.CanEnhance(TargetInstanceId);
    }

    private bool ContainsMaterial(int instanceId)
    {
        foreach (CardInstance material in _materials)
        {
            if (material.InstanceId == instanceId)
                return true;
        }

        return false;
    }

    private static int CompareMaterial(CardInstance a, CardInstance b)
    {
        int byEnhance = a.EnhanceLevel.CompareTo(b.EnhanceLevel);

        if (byEnhance != 0)
            return byEnhance;

        int byTrain = a.TrainLevel.CompareTo(b.TrainLevel);

        return byTrain != 0 ? byTrain : a.InstanceId.CompareTo(b.InstanceId);
    }

    private bool ShowsEnhanceCardTile()
    {
        return _enhanceCardType != CurrencyType.None && _ownedEnhanceCards >= _cardsPerLevel && !IsTargetMaxed();
    }

    //전용 카드 칸은 카드가 아니라 재화라 화면이 직접 만든다
    private void EnsureEnhanceCardTile()
    {
        if (_enhanceCardTile != null)
            return;

        _enhanceCardTile = new EnhanceCardTileView(Grid.Root);
        _enhanceCardTile.SetHandlers(OnAddEnhanceCard, OnRemoveEnhanceCard);
    }

    private int GetTargetLevel()
    {
        CardInstance target = HasTarget ? InventoryManager.Instance.GetCard(TargetInstanceId) : null;

        return target == null ? 0 : target.EnhanceLevel;
    }

    private int GetRemainingLevels()
    {
        return MaxLevel - GetTargetLevel() - Gain;
    }

    private void OnAddEnhanceCard()
    {
        if (IsPlaying)
            return;

        if (GetRemainingLevels() <= 0)
        {
            Notify($"+{MaxLevel} 까지만 올릴 수 있습니다", false);
            return;
        }

        if ((_selectedSets + 1) * _cardsPerLevel > _ownedEnhanceCards)
        {
            Notify("강화 카드가 부족합니다", false);
            return;
        }

        _selectedSets++;
        OnMaterialsChanged();
    }

    private void OnRemoveEnhanceCard()
    {
        if (IsPlaying || _selectedSets <= 0)
            return;

        _selectedSets--;
        OnMaterialsChanged();
    }

    private void OnMaterialsChanged()
    {
        RefreshMaterialSelection();
        _enhanceCardTile?.SetSelectedSets(_selectedSets, _cardsPerLevel);

        if (CardDisplayInfo.TryCreate(TargetInstanceId, out CardDisplayInfo target))
            RefreshWork(target, true);
    }

    //+0 은 메달이 없으므로 회색 빈 메달
    private void ApplyMedal(Image medal, TextMeshProUGUI label, int level)
    {
        medal.sprite = UiSprites.Get(EnhanceBadgeStyle.GetSpritePath(Mathf.Max(1, level)));
        medal.color = level > 0 ? Color.white : EmptyMedalColor;
        label.text = EnhancePlusTag + level;
        label.color = level > 0 ? EnhanceBadgeStyle.GetInkColor(level) : EmptyMedalInk;
        label.fontSize = level >= EnhanceBadgeStyle.HoloLevel ? MedalWideFontSize : MedalFontSize;
    }

    private void SetMedalPair(bool pair, bool animate)
    {
        RectTransform current = _currentMedal.rectTransform;
        Vector2 target = GetMedalCenter(pair ? MedalRowLeft : MedalSingleLeft);
        bool changed = pair != _medalPairShown;

        _arrow.gameObject.SetActive(pair);
        _nextMedal.gameObject.SetActive(pair);
        _medalPairShown = pair;

        if (_medalSlide != null)
        {
            UiAnimator.Cancel(_medalSlide);
            _medalSlide = null;
        }

        if (!animate || !changed)
        {
            current.anchoredPosition = target;
            return;
        }

        Vector2 from = current.anchoredPosition;

        _medalSlide = UiAnimator.Play(MedalSlideSeconds, t => current.anchoredPosition = Vector2.LerpUnclamped(from, target, t),
            () => current.anchoredPosition = target, UiEase.OutCubic);

        if (pair)
            UiEffects.Pop(_arrow.transform, 0.4f, 0.24f, MedalSlideSeconds * 0.5f);
    }

    private static Vector2 GetMedalCenter(float left)
    {
        return new Vector2(left + MedalWidth * 0.5f, -(MedalTop + MedalHeight * 0.5f));
    }

    //이미 오른 칸은 흰색, 이번에 오를 칸은 금색, 남은 칸은 흐리게
    private void UpdatePips(int current, int next, int maxLevel, bool animate)
    {
        EnsurePips(maxLevel);

        for (int i = 0; i < _pips.Count; i++)
        {
            bool reached = i < current;
            bool preview = !reached && i < next;
            bool newlyLit = animate && preview && i >= current + _shownGain;

            _pips[i].color = reached
                ? UiPalette.WithAlpha(UiPalette.Text1, 0.95f)
                : preview ? UiPalette.AccentGold : UiPalette.WithAlpha(UiPalette.Text1, 0.12f);

            if (newlyLit)
                UiEffects.Pop(_pips[i].transform, 0.3f, 0.24f);
        }
    }

    //최대 단계는 인스펙터 값이라 칸은 런타임에 만든다
    private void EnsurePips(int maxLevel)
    {
        if (_pips.Count == maxLevel || maxLevel <= 0)
            return;

        foreach (Image pip in _pips)
            Destroy(pip.gameObject);

        _pips.Clear();

        float pipWidth = (PipRowWidth - PipGap * (maxLevel - 1)) / maxLevel;

        for (int i = 0; i < maxLevel; i++)
        {
            Image pip = UiBuilder.CreateImage(_pipRow, "Pip", Color.white, UiSprites.ScrollHandle, Image.Type.Sliced);
            UiBuilder.CenterPivot(UiBuilder.Place(pip.rectTransform, i * (pipWidth + PipGap), 0f, pipWidth, PipHeight));
            _pips.Add(pip);
        }
    }

    private static int GetOvrGain(CardDisplayInfo info, int gain)
    {
        return CardStatsCalculator.CalculateFinalOVR(info.BaseOvr, info.EnhanceLevel + gain, null)
            - CardStatsCalculator.CalculateFinalOVR(info.BaseOvr, info.EnhanceLevel, null);
    }

    private string BuildMaterialSummary(bool maxed)
    {
        if (maxed)
            return "최대 강화 카드입니다";

        if (Gain == 0)
            return "아래에서 재료를 고르세요";

        string identical = _selectedMaterialIds.Count > 0 ? $"같은 카드 {_selectedMaterialIds.Count}장" : null;
        string enhanceCards = _selectedSets > 0 ? $"강화 카드 {_selectedSets * _cardsPerLevel}장" : null;

        if (identical != null && enhanceCards != null)
            return $"{identical}  ·  {enhanceCards}";

        return identical ?? enhanceCards;
    }

    //재료가 사라지고, 메달이 한 단계씩 오르며 카드가 번쩍인 뒤 마지막에 크게 터진다
    private IEnumerator PlayEnhance(CardDisplayInfo before)
    {
        RefreshList();
        TargetCard.SetCard(before);
        SetMedalPair(false, true);
        _statText.text = string.Empty;
        _materialText.text = string.Empty;
        SetAction(false, ActionLabel);

        CardDisplayInfo.TryCreate(TargetInstanceId, out CardDisplayInfo after);

        int maxLevel = MaxLevel;
        int steps = after.EnhanceLevel - before.EnhanceLevel;

        UiEffects.CountUp(_ovrText, before.FinalOvr, after.FinalOvr, "OVR  {0}", StepSeconds * steps + 0.1f);

        for (int level = before.EnhanceLevel + 1; level <= after.EnhanceLevel; level++)
        {
            yield return new WaitForSecondsRealtime(StepSeconds);

            ApplyMedal(_currentMedal, _currentMedalText, level);
            UiEffects.Pop(_currentMedal.transform, 0.7f, 0.24f);
            UpdatePips(level, level, maxLevel, false);
            UiEffects.Pop(_pips[level - 1].transform, 0.3f, 0.24f);
            UiEffects.Flash(CardFlash, UiPalette.WithAlpha(Color.white, 0.5f), 0.26f);
        }

        yield return new WaitForSecondsRealtime(StepSeconds);

        TargetCard.SetCard(after);
        UiEffects.Pop(TargetCard.Root, 0.9f, 0.3f);
        UiEffects.Flash(CardFlash, UiPalette.WithAlpha(Color.white, 0.8f), 0.5f);
        UiEffects.Burst(TargetCard.Root, UiEffectKind.Enhance);
        Notify($"+{after.EnhanceLevel} 강화 완료", true);

        yield return new WaitForSecondsRealtime(SettleSeconds);

        _shownGain = 0;
        RefreshWork(after, false);
    }
}
