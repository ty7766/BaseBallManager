using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 강화. 강화할 카드를 고르면 위 왼쪽에 놓이고 아래 목록이 재료(같은 카드 + 강화 전용 카드)로 바뀐다.
/// 재료를 고를수록 오른쪽에 +3 → +5 처럼 오를 단계를 미리 보여 주고, 강화를 누르면 한 번에 올린다
/// </summary>
public class EnhanceScreen : UiScreenBehaviour
{
    private const float SideMargin = 32f;
    //고르기 단계 — 제목 아래 안내 · 필터 줄, 그 아래 카드 목록
    private const float FilterRowTop = 112f;
    private const float PickGridTop = FilterRowTop + CardFilterButtonView.ButtonHeight + 36f;
    private const float GridBottom = 224f;
    //재료 단계 — 위 왼쪽 대상 카드 · 오른쪽 미리보기, 그 아래 재료 목록
    private const float TargetTop = 120f;
    private const float TargetWidth = 300f;
    private const float TargetHeight = TargetWidth * CardView.BaseHeight / CardView.BaseWidth;
    private const float MaterialGridTop = TargetTop + TargetHeight + 52f;
    private const float PreviewLeft = SideMargin + TargetWidth + 32f;
    private const float PreviewWidth = 1080f - SideMargin - PreviewLeft;
    private const float MedalWidth = 168f;
    private const float MedalHeight = MedalWidth * 208f / 240f;
    private const float MedalFontSize = 76f;
    private const float MedalWideFontSize = 64f;
    private const float ArrowWidth = 112f;
    private const float ArrowHeight = ArrowWidth * 164f / 330f;
    private const float MedalGap = 24f;
    //재료가 없으면 지금 메달 하나만 가운데, 고르면 왼쪽으로 비켜서며 화살표 · 오를 메달이 나온다
    private const float MedalTop = TargetTop + 72f;
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
    private const string CardMaskSprite = "Fx/card_mask";
    private const string EnhancePlusTag = "<size=60%><voffset=0.12em>+</voffset></size>";
    private const string PreviewColorTag = "<color=#F2C14E>";
    private const string MaxReason = "최대 강화";
    private static readonly Color EmptyMedalColor = new Color(0.36f, 0.38f, 0.42f, 1f);
    private static readonly Color EmptyMedalInk = new Color(0.1f, 0.11f, 0.13f, 1f);

    [SerializeField] private TextMeshProUGUI _hintText;
    [SerializeField] private CardFilterButtonView _filterButton;
    [SerializeField] private CardGridView _grid;
    [SerializeField] private RectTransform _targetPanel;
    [SerializeField] private CanvasGroup _targetGroup;
    [SerializeField] private CardView _targetCard;
    [SerializeField] private Image _cardFlash;
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
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _changeButton;
    [SerializeField] private Button _enhanceButton;
    [SerializeField] private TextMeshProUGUI _enhanceLabel;
    [SerializeField] private CanvasGroup _enhanceGroup;
    [SerializeField] private Image _inputBlocker;

    private readonly List<int> _listIds = new List<int>(64);
    private readonly List<CardInstance> _materials = new List<CardInstance>(16);
    private readonly List<int> _selectedMaterialIds = new List<int>(10);
    private readonly List<RectTransform> _leadingTiles = new List<RectTransform>(1);
    private readonly List<Image> _pips = new List<Image>(10);

    private Action _onBack;
    private Action<string, bool> _onNotify;
    private Action<int> _onShowCardDetail;
    private EnhanceCardTileView _enhanceCardTile;
    private Coroutine _playRoutine;
    private UiTween _medalSlide;
    private bool _medalPairShown = true;
    private int _targetInstanceId = InventoryManager.InvalidInstanceId;
    private int _selectedSets;
    private CurrencyType _enhanceCardType;
    private int _cardsPerLevel = 1;
    private int _ownedEnhanceCards;
    private int _shownGain;
    private bool _medalFontApplied;

    private bool HasTarget => _targetInstanceId != InventoryManager.InvalidInstanceId;
    private int Gain => _selectedMaterialIds.Count + _selectedSets;
    private int MaxLevel => EnhanceManager.Instance != null ? EnhanceManager.Instance.MaxEnhanceLevel : 0;

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
    /// 지금 단계를 다시 그린다. 고르던 재료 중 쓸 수 없게 된 것(잠금 등)은 빠진다
    /// </summary>
    public void Refresh()
    {
        if (InventoryManager.Instance == null || EnhanceManager.Instance == null)
            return;

        if (HasTarget && !CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo _))
            ClearTarget();

        if (HasTarget)
            ShowMaterialStep();
        else
            ShowPickStep();
    }

    /// <summary>
    /// 필터 창 → 재료 고르기 순으로 한 단계씩 물러난다. 연출 중에는 막는다
    /// </summary>
    public override bool HandleBack()
    {
        if (_playRoutine != null)
            return true;

        if (_filterButton.ClosePanel())
            return true;

        if (!HasTarget)
            return false;

        ReturnToPick();

        return true;
    }

    protected override void BindEvents()
    {
        WireWidgets();
        Bind(_backButton, OnBackClicked);
        Bind(_changeButton, ReturnToPick);
        Bind(_enhanceButton, OnEnhanceClicked);
    }

    //제목 · 안내 · 필터 · 목록 · 대상 칸(숨김) · 아래 버튼 · 연출 중 입력 막이
    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, SideMargin, 32f, 500f, 60f);
        title.text = "강화";

        _hintText = UiBuilder.CreateText(Root, "Hint", 30f, UiPalette.Text3);
        UiBuilder.Place(_hintText.rectTransform, SideMargin, FilterRowTop, 600f, CardFilterButtonView.ButtonHeight);
        _hintText.text = "강화할 카드를 고르세요";

        _filterButton = CardFilterButtonView.Bake(Root);
        UiBuilder.PlaceRight(_filterButton.Root, SideMargin, FilterRowTop, CardFilterButtonView.ButtonWidth,
            CardFilterButtonView.ButtonHeight);

        _grid = CardGridView.Bake(Root);
        UiBuilder.Stretch(_grid.Root, SideMargin, PickGridTop, SideMargin, GridBottom);

        BuildTargetPanel();

        _backButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, true, OnBackClicked);
        PlaceBottomButton(_backButton, 0f, 1f);

        _changeButton = UiBuilder.CreateButton(Root, "ChangeButton", "다른 카드", 34f, false, ReturnToPick);
        PlaceBottomButton(_changeButton, 0f, 0.5f);

        _enhanceButton = UiBuilder.CreateButton(Root, "EnhanceButton", "강화", 36f, true, OnEnhanceClicked,
            out _enhanceLabel);
        PlaceBottomButton(_enhanceButton, 0.5f, 1f);
        _enhanceGroup = _enhanceButton.gameObject.AddComponent<CanvasGroup>();

        _inputBlocker = UiBuilder.CreateImage(Root, "InputBlocker", UiPalette.WithAlpha(Color.black, 0f));
        UiBuilder.Stretch(_inputBlocker.rectTransform, 0f, 0f, 0f, 0f);
        _inputBlocker.raycastTarget = true;
        _inputBlocker.gameObject.SetActive(false);

        WireWidgets();
        SetStep(false);
    }

    protected override void OnShown()
    {
        ApplyMedalFont();
        Refresh();
    }

    //다시 들어오면 카드 고르기부터, 필터 없이 맨 위에서
    protected override void OnHidden()
    {
        _playRoutine = null;
        _inputBlocker.gameObject.SetActive(false);
        ClearTarget();
        _filterButton.ResetFilter();
        _grid.ScrollToTop();
    }

    //대상 카드 · 단계 메달 → 메달 · 단계 칸 · OVR · 세부 스탯 · 재료 요약
    private void BuildTargetPanel()
    {
        _targetPanel = UiBuilder.CreateRect(Root, "TargetPanel");
        UiBuilder.PlaceRow(_targetPanel, 0f, MaterialGridTop);
        _targetGroup = _targetPanel.gameObject.AddComponent<CanvasGroup>();

        _targetCard = CardView.Bake(_targetPanel, TargetWidth, false);
        UiBuilder.CenterPivot(UiBuilder.Place(_targetCard.Root, SideMargin, TargetTop, TargetWidth, TargetHeight));

        _cardFlash = UiBuilder.CreateImage(_targetPanel, "CardFlash", UiPalette.WithAlpha(Color.white, 0f), CardMaskSprite);
        UiBuilder.Place(_cardFlash.rectTransform, SideMargin, TargetTop, TargetWidth, TargetHeight);

        _targetName = CreatePreviewText("TargetName", 40f, UiPalette.Text1, TargetTop, 52f);

        _currentMedal = CreateMedal("CurrentMedal", MedalRowLeft, MedalTop, out _currentMedalText);

        _arrow = UiBuilder.CreateImage(_targetPanel, "Arrow", UiPalette.Text1, ArrowSprite);
        UiBuilder.CenterPivot(UiBuilder.Place(_arrow.rectTransform, MedalRowLeft + MedalWidth + MedalGap,
            MedalTop + (MedalHeight - ArrowHeight) * 0.5f, ArrowWidth, ArrowHeight));
        _arrow.preserveAspect = true;

        _nextMedal = CreateMedal("NextMedal", MedalRowLeft + MedalWidth + MedalGap * 2f + ArrowWidth, MedalTop,
            out _nextMedalText);

        float pipTop = MedalTop + MedalHeight + 26f;

        _pipRow = UiBuilder.CreateRect(_targetPanel, "LevelPips");
        UiBuilder.Place(_pipRow, PreviewLeft + (PreviewWidth - PipRowWidth) * 0.5f, pipTop, PipRowWidth, PipHeight);

        float ovrTop = pipTop + PipHeight + 18f;

        _ovrText = CreatePreviewText("Ovr", 44f, UiPalette.Text1, ovrTop, 56f);
        UiBuilder.CenterPivot(_ovrText.rectTransform);
        _statText = CreatePreviewText("Stat", 28f, UiPalette.Text2, ovrTop + 60f, 38f);
        _materialText = CreatePreviewText("Material", 26f, UiPalette.Text3, ovrTop + 102f, 38f);
    }

    private TextMeshProUGUI CreatePreviewText(string name, float fontSize, Color color, float top, float height)
    {
        TextMeshProUGUI text = UiBuilder.CreateText(_targetPanel, name, fontSize, color, TextAlignmentOptions.Center);

        UiBuilder.Place(text.rectTransform, PreviewLeft, top, PreviewWidth, height);

        return text;
    }

    //카드 앞면과 같은 육각 메달 + 새긴 숫자
    private Image CreateMedal(string name, float left, float top, out TextMeshProUGUI label)
    {
        Image medal = UiBuilder.CreateImage(_targetPanel, name, Color.white, EnhanceBadgeStyle.GetSpritePath(1));
        UiBuilder.CenterPivot(UiBuilder.Place(medal.rectTransform, left, top, MedalWidth, MedalHeight));

        label = UiBuilder.CreateText(medal.transform, "Level", MedalFontSize, Color.black, TextAlignmentOptions.Center);
        UiBuilder.Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

        return medal;
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

    //카드 앞면 글꼴은 런타임에 만들어지므로 처음 보일 때 입힌다
    private void ApplyMedalFont()
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

    private void SetStep(bool materialStep)
    {
        _hintText.gameObject.SetActive(!materialStep);
        _filterButton.gameObject.SetActive(!materialStep);
        _backButton.gameObject.SetActive(!materialStep);
        _targetPanel.gameObject.SetActive(materialStep);
        _changeButton.gameObject.SetActive(materialStep);
        _enhanceButton.gameObject.SetActive(materialStep);

        UiBuilder.Stretch(_grid.Root, SideMargin, materialStep ? MaterialGridTop : PickGridTop, SideMargin, GridBottom);
    }

    //보유 카드 전체. 더 못 올리는 카드는 흐리게 덮는다
    private void ShowPickStep()
    {
        SetStep(false);

        _listIds.Clear();

        foreach (CardInstance card in InventoryManager.Instance.GetFiltered(_filterButton.CurrentFilter))
            _listIds.Add(card.InstanceId);

        _grid.SetLeadingTiles(null);
        _grid.SetEmptyMessage(_filterButton.CurrentFilter.SelectedCount > 0 ? "조건에 맞는 카드가 없습니다" : "카드가 없습니다");
        _grid.SetSelection(null);
        _grid.SetCards(_listIds);
    }

    //위 왼쪽 대상 카드 + 아래 재료(전용 카드 칸이 맨 앞)
    private void ShowMaterialStep()
    {
        SetStep(true);

        if (CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo info))
        {
            _targetCard.SetCard(info);
            _targetName.text = PlayerLabelFormat.NameWithYear(info.Name, info.Year);
        }

        RebuildMaterialList();
        UpdatePreview(false);
    }

    private void RebuildMaterialList()
    {
        CollectMaterials();

        _listIds.Clear();

        foreach (CardInstance material in _materials)
            _listIds.Add(material.InstanceId);

        _leadingTiles.Clear();

        if (ShowsEnhanceCardTile())
        {
            EnsureEnhanceCardTile();
            _enhanceCardTile.SetCard(_enhanceCardType, _ownedEnhanceCards, _cardsPerLevel);
            _enhanceCardTile.SetSelectedSets(_selectedSets, _cardsPerLevel);
            _leadingTiles.Add(_enhanceCardTile.Root);
        }

        bool maxed = !EnhanceManager.Instance.CanEnhance(_targetInstanceId);

        _grid.SetLeadingTiles(_leadingTiles);
        _grid.SetEmptyMessage(maxed ? "더 강화할 수 없는 카드입니다" : "카드가 없습니다");
        _grid.SetSelection(_selectedMaterialIds);
        _grid.SetCards(_listIds);
    }

    //같은 카드(덜 키운 것부터) · 전용 카드 보유량. 쓸 수 없게 된 선택은 빼고, 최대 단계를 넘는 만큼 덜어 낸다
    private void CollectMaterials()
    {
        _materials.Clear();

        if (EnhanceManager.Instance.CanEnhance(_targetInstanceId))
            _materials.AddRange(EnhanceManager.Instance.GetIdenticalCards(_targetInstanceId));

        _materials.Sort(CompareMaterial);

        for (int i = _selectedMaterialIds.Count - 1; i >= 0; i--)
        {
            if (!ContainsMaterial(_selectedMaterialIds[i]))
                _selectedMaterialIds.RemoveAt(i);
        }

        (CurrencyType enhanceCardType, int count) required = EnhanceManager.Instance.GetRequiredEnhanceCard(_targetInstanceId);

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
        return _enhanceCardType != CurrencyType.None
            && _ownedEnhanceCards >= _cardsPerLevel
            && EnhanceManager.Instance.CanEnhance(_targetInstanceId);
    }

    //전용 카드 칸은 카드가 아니라 재화라 화면이 직접 만든다
    private void EnsureEnhanceCardTile()
    {
        if (_enhanceCardTile != null)
            return;

        _enhanceCardTile = new EnhanceCardTileView(_grid.Root);
        _enhanceCardTile.SetHandlers(OnAddEnhanceCard, OnRemoveEnhanceCard);
    }

    private int GetTargetLevel()
    {
        CardInstance target = HasTarget ? InventoryManager.Instance.GetCard(_targetInstanceId) : null;

        return target == null ? 0 : target.EnhanceLevel;
    }

    private int GetRemainingLevels()
    {
        return MaxLevel - GetTargetLevel() - Gain;
    }

    //고르기 단계는 이미 최대인 카드, 재료 단계는 단계가 다 찼을 때 남은 재료를 막는다
    private string GetBlockedReason(CardDisplayInfo info)
    {
        if (!HasTarget)
            return info.EnhanceLevel >= MaxLevel ? MaxReason : null;

        return GetRemainingLevels() <= 0 && !_selectedMaterialIds.Contains(info.InstanceId) ? MaxReason : null;
    }

    private void OnFilterChanged()
    {
        if (!HasTarget)
            ShowPickStep();
    }

    private void OnGridClicked(int instanceId)
    {
        if (_playRoutine != null)
            return;

        if (HasTarget)
            ToggleMaterial(instanceId);
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

    //고른 카드가 위 왼쪽에 튀어나오고 아래가 재료 목록으로 바뀐다
    private void SelectTarget(int instanceId)
    {
        _targetInstanceId = instanceId;
        _selectedMaterialIds.Clear();
        _selectedSets = 0;
        _shownGain = 0;

        ShowMaterialStep();
        _grid.ScrollToTop();

        UiEffects.FadeIn(_targetGroup, 0.2f);
        UiEffects.Pop(_targetCard.Root, 0.86f, 0.28f);
    }

    private void ReturnToPick()
    {
        if (_playRoutine != null)
            return;

        ClearTarget();
        ShowPickStep();
        _grid.ScrollToTop();
    }

    private void ClearTarget()
    {
        _targetInstanceId = InventoryManager.InvalidInstanceId;
        _selectedMaterialIds.Clear();
        _selectedSets = 0;
        _shownGain = 0;
    }

    private void ToggleMaterial(int instanceId)
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

    private void OnAddEnhanceCard()
    {
        if (_playRoutine != null)
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
        if (_playRoutine != null || _selectedSets <= 0)
            return;

        _selectedSets--;
        OnMaterialsChanged();
    }

    private void OnMaterialsChanged()
    {
        _grid.SetSelection(_selectedMaterialIds);
        _grid.RefreshBlocked();
        _enhanceCardTile?.SetSelectedSets(_selectedSets, _cardsPerLevel);
        UpdatePreview(true);
    }

    //지금 단계 → 오를 단계 메달, 단계 칸(채움 · 이번에 오를 칸 · 빈 칸), OVR 변화
    private void UpdatePreview(bool animate)
    {
        if (!CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo info))
            return;

        int maxLevel = MaxLevel;
        int current = info.EnhanceLevel;
        int gain = Gain;
        int next = Mathf.Min(maxLevel, current + gain);
        bool hasGain = gain > 0;

        ApplyMedal(_currentMedal, _currentMedalText, current);
        ApplyMedal(_nextMedal, _nextMedalText, next);
        SetMedalPair(hasGain, animate);
        UpdatePips(current, next, maxLevel, animate);

        int nextOvr = info.FinalOvr + GetOvrGain(info, gain);

        _ovrText.text = hasGain
            ? $"OVR  {info.FinalOvr}  →  {PreviewColorTag}{nextOvr}</color>"
            : $"OVR  {info.FinalOvr}";
        _statText.text = hasGain ? $"세부 스탯 각 +{CardStatsCalculator.CalculateFinalStat(0, gain, 0)}" : string.Empty;
        _materialText.text = BuildMaterialSummary(current >= maxLevel);

        _enhanceButton.interactable = hasGain;
        _enhanceGroup.alpha = hasGain ? 1f : 0.4f;
        _enhanceLabel.text = hasGain ? $"강화  +{gain}" : "강화";

        if (animate && gain != _shownGain)
        {
            UiEffects.Pop(_nextMedal.transform, 0.7f, 0.26f);
            UiEffects.Pop(_ovrText.transform, 0.88f, 0.22f);
        }

        _shownGain = gain;
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

    private void OnEnhanceClicked()
    {
        if (_playRoutine != null || !HasTarget || Gain <= 0 || EnhanceManager.Instance == null)
            return;

        if (!CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo before))
            return;

        if (!EnhanceManager.Instance.Enhance(_targetInstanceId, _selectedMaterialIds, _selectedSets))
        {
            Notify("강화하지 못했습니다", false);
            Refresh();
            return;
        }

        _selectedMaterialIds.Clear();
        _selectedSets = 0;
        _playRoutine = StartCoroutine(PlayEnhance(before));
    }

    //재료가 사라지고, 메달이 한 단계씩 오르며 카드가 번쩍인 뒤 마지막에 크게 터진다
    private IEnumerator PlayEnhance(CardDisplayInfo before)
    {
        _inputBlocker.gameObject.SetActive(true);

        RebuildMaterialList();
        _targetCard.SetCard(before);
        SetMedalPair(false, true);
        _statText.text = string.Empty;
        _materialText.text = string.Empty;
        _enhanceButton.interactable = false;
        _enhanceGroup.alpha = 0.4f;
        _enhanceLabel.text = "강화";

        CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo after);

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
            UiEffects.Flash(_cardFlash, UiPalette.WithAlpha(Color.white, 0.5f), 0.26f);
        }

        yield return new WaitForSecondsRealtime(StepSeconds);

        _targetCard.SetCard(after);
        UiEffects.Pop(_targetCard.Root, 0.9f, 0.3f);
        UiEffects.Flash(_cardFlash, UiPalette.WithAlpha(Color.white, 0.8f), 0.5f);
        UiEffects.Burst(_targetCard.Root, UiEffectKind.Enhance);
        Notify($"+{after.EnhanceLevel} 강화 완료", true);

        yield return new WaitForSecondsRealtime(SettleSeconds);

        _playRoutine = null;
        _inputBlocker.gameObject.SetActive(false);
        _shownGain = 0;
        UpdatePreview(false);
    }

    private void Notify(string message, bool success)
    {
        _onNotify?.Invoke(message, success);
    }

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }
}
