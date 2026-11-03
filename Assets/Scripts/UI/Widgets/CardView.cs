using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 카드 1장 (명세 1-2 레이어 구조 · 6-6 좌표). 누르면 앞면/뒷면이 바뀐다.
/// 좌표는 1398×1957 기준으로 적고 생성 시 폭에 맞춰 일괄 축소한다
/// </summary>
public class CardView : UiWidgetBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    /// <summary>
    /// 카드 원본 규격 (명세 1-2)
    /// </summary>
    public const float BaseWidth = 1398f;
    public const float BaseHeight = 1957f;

    private const int MaxStars = 5;
    private const int StatCount = 4;
    //5개를 가운데 정렬해도 오른쪽 위 강화 배지(x 1078)와 겹치지 않는 크기
    private const float StarSize = 144f;
    private const float StatBarMaxStat = 150f;
    //그림 알파가 255 까지 올라가 조금만 올려도 흰 띠가 된다
    private const float ShineAlpha = 0.10f;
    //노말 베이스가 어두워 살짝 들어 올린다. 흰색으로 덮으면 청사진 무늬가 날아가
    //회색으로 뿌얘지므로 카드 종류색(하늘색)으로 올린다
    private const float NormalLiftAlpha = 0.10f;
    //조합 결과 자리에 띄우는 물음표 크기 (카드 좌표계)
    private const float UnknownMarkSize = 520f;
    //이만큼 누르고 있으면 상세 보기로 넘긴다
    private const float LongPressSeconds = 2f;
    //화면 폭이 이 이하인 카드는 썸네일 사진(320×400)으로 충분하다
    private const float ThumbMaxDisplayWidth = 360f;

    [SerializeField] private RectTransform _front;
    [SerializeField] private RectTransform _back;
    [SerializeField] private float _scale;

    [SerializeField] private Image _frontBase;
    [SerializeField] private Image _baseLift;
    [SerializeField] private Image _teamAura;
    [SerializeField] private Image _photo;
    [SerializeField] private Image _nameShadow;
    [SerializeField] private Image _teamLogo;
    [SerializeField] private Image _shine;
    [SerializeField] private Image _glow;
    [SerializeField] private TextMeshProUGUI _ovrText;
    [SerializeField] private TextMeshProUGUI _positionText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image[] _stars = new Image[MaxStars];

    [SerializeField] private Image _enhanceFill;
    [SerializeField] private Image _enhanceRim;
    [SerializeField] private Image _enhanceLine;
    [SerializeField] private TextMeshProUGUI _enhanceText;

    [SerializeField] private Image _backBase;
    [SerializeField] private TextMeshProUGUI _backNameText;
    [SerializeField] private TextMeshProUGUI _backDetailText;
    [SerializeField] private TextMeshProUGUI _backOvrText;
    [SerializeField] private TextMeshProUGUI[] _statLabels = new TextMeshProUGUI[StatCount];
    [SerializeField] private TextMeshProUGUI[] _statValues = new TextMeshProUGUI[StatCount];
    [SerializeField] private Image[] _statBaseBars = new Image[StatCount];
    [SerializeField] private Image[] _statEnhanceBars = new Image[StatCount];
    [SerializeField] private Image[] _statTrainBars = new Image[StatCount];
    [SerializeField] private TextMeshProUGUI _growthText;
    [SerializeField] private Image _breakthroughIcon;

    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _emptyText;

    private Action _onClick;
    private Action _onLongPress;
    private float _pressSeconds;
    private bool _pressing;
    private bool _longPressFired;
    private bool _showBack;
    private float _displayWidth;
    private string _photoKey;
    [SerializeField] private bool _hasBack;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static CardView Bake(Transform parent, float width, bool withBack = true)
    {
        CardView view = CreateNode<CardView>(parent, "Card");

        view.BuildWidget(parent, width, withBack);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    protected override void BindEvents()
    {
        Bind(_button, OnClicked);
    }

    private void BuildWidget(Transform parent, float width, bool withBack)
    {
        _hasBack = withBack;
        _scale = width / BaseWidth;

        UiBuilder.Place(Root, 0f, 0f, width, BaseHeight * _scale);

        Image hitArea = UiBuilder.CreateImage(Root, "HitArea", UiPalette.WithAlpha(UiPalette.BgApp, 0f));
        UiBuilder.Stretch(hitArea.rectTransform, 0f, 0f, 0f, 0f);
        hitArea.raycastTarget = true;

        _button = Root.gameObject.AddComponent<Button>();
        _button.targetGraphic = hitArea;
        _button.transition = Selectable.Transition.None;
        _button.interactable = false;
        _button.onClick.AddListener(OnClicked);

        _front = UiBuilder.CreateRect(Root, "Front");
        UiBuilder.Stretch(_front, 0f, 0f, 0f, 0f);

        if (_hasBack)
        {
            _back = UiBuilder.CreateRect(Root, "Back");
            UiBuilder.Stretch(_back, 0f, 0f, 0f, 0f);
            _back.gameObject.SetActive(false);
        }

        // --- 앞면: 배경 -> 선수 -> 텍스트 -> 효과 (명세 1-2) ---
        //베이스 그림 3종은 완전 불투명이라 배경 레이어다. 선수 사진보다 반드시 아래에 깔아야 한다
        _frontBase = CreateLayer(_front, "Base", null, 0f, 0f, BaseWidth, BaseHeight);
        _baseLift = CreateLayer(_front, "BaseLift", null, 0f, 0f, BaseWidth, BaseHeight);
        _baseLift.raycastTarget = false;

        _teamAura = CreateLayer(_front, "TeamAura", "Card/card_team_aura", 139f, 300f, 1120f, 1120f);
        _photo = CreateLayer(_front, "Photo", null, 123f, 150f, 1152f, 1440f);
        _photo.preserveAspect = true;

        _nameShadow = CreateLayer(_front, "NameShadow", "Card/card_name_shadow", 0f, 1480f, 1398f, 417f);

        _ovrText = CreateCardText(_front, "Ovr", 270f, 90f, 60f, 300f, 320f, TextAlignmentOptions.Center);
        _positionText = CreateCardText(_front, "Position", 108f, 90f, 380f, 300f, 140f, TextAlignmentOptions.Center);
        _teamLogo = CreateLayer(_front, "TeamLogo", null, 150f, 530f, 180f, 180f);
        _teamLogo.preserveAspect = true;

        RectTransform starRow = UiBuilder.CreateRect(_front, "Stars");
        Place(starRow, (BaseWidth - MaxStars * StarSize) * 0.5f, 24f, MaxStars * StarSize, StarSize);

        for (int i = 0; i < MaxStars; i++)
        {
            _stars[i] = UiBuilder.CreateImage(starRow, "Star", UiPalette.CardNormal, UiSprites.IconStar);
            Place(_stars[i].rectTransform, i * StarSize, 0f, StarSize, StarSize);
            _stars[i].gameObject.SetActive(false);
        }

        _enhanceFill = CreateLayer(_front, "EnhanceFill", "Card/badge_enhance_fill", 1078f, 56f, 264f, 229f);
        _enhanceRim = CreateLayer(_front, "EnhanceRim", "Card/badge_enhance_rim", 1078f, 56f, 264f, 229f);
        _enhanceLine = CreateLayer(_front, "EnhanceLine", "Card/badge_enhance_line", 1078f, 56f, 264f, 229f);
        _enhanceText = CreateCardText(_front, "EnhanceText", 147f, 1078f, 86f, 264f, 170f, TextAlignmentOptions.Center);

        _nameText = CreateCardText(_front, "Name", 142f, 60f, 1560f, 1278f, 200f, TextAlignmentOptions.Center);

        _shine = CreateLayer(_front, "Shine", "Card/card_fx_shine_5", 0f, 0f, BaseWidth, BaseHeight);

        //그림 알파가 255까지 올라가 그대로 깔면 흰 띠가 된다. 광택으로 읽히도록 낮춘다
        _shine.color = UiPalette.WithAlpha(Color.white, ShineAlpha);
        _glow = CreateLayer(_front, "Glow", "Card/card_fx_glow_signature", -180f, -180f, BaseWidth + 360f, BaseHeight + 360f);

        _emptyText = CreateCardText(_front, "Empty", 120f, 60f, 880f, 1278f, 200f, TextAlignmentOptions.Center);
        _emptyText.gameObject.SetActive(false);

        // --- 뒷면 ---
        if (!_hasBack)
            return;

        _backBase = CreateLayer(_back, "Base", null, 0f, 0f, BaseWidth, BaseHeight);

        Image cover = CreateLayer(_back, "Cover", null, 0f, 0f, BaseWidth, BaseHeight);
        cover.color = UiPalette.WithAlpha(UiPalette.BgApp, 0.87f);

        _backNameText = CreateCardText(_back, "Name", 120f, 60f, 90f, 1278f, 170f, TextAlignmentOptions.Center);
        _backDetailText = CreateCardText(_back, "Detail", 88f, 60f, 270f, 1278f, 120f, TextAlignmentOptions.Center);
        _backOvrText = CreateCardText(_back, "Ovr", 180f, 60f, 400f, 1278f, 220f, TextAlignmentOptions.Center);

        for (int i = 0; i < StatCount; i++)
        {
            float rowY = 700f + i * 190f;

            _statLabels[i] = CreateCardText(_back, "StatLabel", 80f, 90f, rowY, 300f, 110f);
            _statValues[i] = CreateCardText(_back, "StatValue", 76f, 380f, rowY, 940f, 110f, TextAlignmentOptions.Right);

            float barY = rowY + 120f;

            _statBaseBars[i] = CreateLayer(_back, "StatBase", null, 90f, barY, 100f, 40f);
            _statBaseBars[i].color = UiPalette.Text3;

            _statEnhanceBars[i] = CreateLayer(_back, "StatEnhance", null, 90f, barY, 0f, 40f);
            _statEnhanceBars[i].color = UiPalette.StateReserved;

            _statTrainBars[i] = CreateLayer(_back, "StatTrain", null, 90f, barY, 0f, 40f);
            _statTrainBars[i].color = UiPalette.StaminaHigh;
        }

        _growthText = CreateCardText(_back, "Growth", 84f, 90f, 1600f, 1100f, 120f);
        _breakthroughIcon = CreateLayer(_back, "Breakthrough", "Card/icon_breakthrough", 1180f, 1590f, 140f, 140f);
    }

    /// <summary>
    /// 보유 카드를 그린다
    /// </summary>
    public void SetCard(CardDisplayInfo info)
    {
        _emptyText.gameObject.SetActive(false);
        _front.gameObject.SetActive(!_showBack);

        if (_hasBack)
            _back.gameObject.SetActive(_showBack);

        Color typeColor = CardVisualLookup.GetTypeColor(info.CardType);
        Color teamColor = UiPalette.GetTeamColor(info.TeamName);

        ApplyBase(_frontBase, info.CardType, teamColor);

        if (_hasBack)
            ApplyBase(_backBase, info.CardType, teamColor);

        //노말 베이스만 어두워 살짝 밝힌다
        _baseLift.color = UiPalette.WithAlpha(UiPalette.CardNormal,
            info.CardType == CardType.Normal ? NormalLiftAlpha : 0f);

        _teamAura.color = UiPalette.WithAlpha(teamColor, 0.55f);

        _photoKey = info.PhotoKey;
        ApplyPhoto();

        Color inkColor = CardVisualLookup.GetInkColor(info.CardType);

        //이름 뒤 타원 그림자는 종류별 진한 색이어야 흰 이름이 읽힌다 (명세 6-6).
        //시그·골글 베이스는 밝고 무늬가 많아 노말보다 진하게 깐다
        _nameShadow.color = UiPalette.WithAlpha(inkColor,
            info.CardType == CardType.Normal ? 0.72f : 0.92f);

        _ovrText.text = info.FinalOvr.ToString();
        //노말은 어두운 베이스라 흰 숫자, 시그·골글은 밝은 베이스라 진한 새김색
        _ovrText.color = info.CardType == CardType.Normal ? UiPalette.Text1 : inkColor;

        _positionText.text = info.Position;
        _positionText.color = _ovrText.color;

        Sprite logo = UiSprites.Get("Team/logo_" + info.TeamName);

        _teamLogo.sprite = logo;
        _teamLogo.gameObject.SetActive(logo != null);

        int starCount = (int)info.CardGrade;

        for (int i = 0; i < MaxStars; i++)
        {
            bool shown = i < starCount;

            _stars[i].gameObject.SetActive(shown);

            if (shown)
                _stars[i].color = typeColor;
        }

        CenterStars(starCount);
        ApplyEnhanceBadge(info.EnhanceLevel);

        _nameText.text = PlayerLabelFormat.NameWithYear(info.Name, info.Year);

        _shine.gameObject.SetActive(info.CardType == CardType.Normal && info.CardGrade == CardGrade.Star5);
        ApplyGlow(info.CardType);

        if (_hasBack)
            FillBack(info, typeColor);
    }

    /// <summary>
    /// 빈 슬롯 (라인업 칸 · 조합 재료 칸)
    /// </summary>
    public void SetEmpty(string label)
    {
        _showBack = false;
        _front.gameObject.SetActive(true);

        if (_hasBack)
            _back.gameObject.SetActive(false);

        _baseLift.color = UiPalette.WithAlpha(UiPalette.CardNormal, 0f);

        _frontBase.sprite = UiSprites.Get(UiSprites.CardSlotEmpty);
        _frontBase.color = _frontBase.sprite == null ? UiPalette.BgPanel : Color.white;

        _photoKey = null;
        _photo.gameObject.SetActive(false);
        _teamLogo.gameObject.SetActive(false);
        _teamAura.color = UiPalette.WithAlpha(UiPalette.BgApp, 0f);
        _nameShadow.color = UiPalette.WithAlpha(UiPalette.BgApp, 0f);

        _ovrText.text = string.Empty;
        _positionText.text = string.Empty;
        _nameText.text = string.Empty;
        _shine.gameObject.SetActive(false);
        _glow.gameObject.SetActive(false);

        for (int i = 0; i < MaxStars; i++)
            _stars[i].gameObject.SetActive(false);

        SetEnhanceBadgeVisible(false);

        _emptyText.gameObject.SetActive(true);
        _emptyText.text = label;
    }

    /// <summary>
    /// 길게 누르면 부를 콜백 (카드 상세 보기)
    /// </summary>
    public void SetLongPressHandler(Action onLongPress)
    {
        _onLongPress = onLongPress;
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        _pressing = true;
        _pressSeconds = 0f;
        _longPressFired = false;
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        _pressing = false;
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        _pressing = false;
    }

    //길게 누르는 동안만 센다. 한 번 터지면 손을 뗄 때까지 다시 터지지 않는다
    private void Update()
    {
        if (!_pressing || _longPressFired || _onLongPress == null)
            return;

        _pressSeconds += Time.unscaledDeltaTime;

        if (_pressSeconds < LongPressSeconds)
            return;

        _longPressFired = true;
        _onLongPress();
    }

    /// <summary>
    /// 아직 모르는 카드 (조합 결과 자리). 빈 슬롯 그림의 + 대신 큰 물음표를 보인다
    /// </summary>
    public void SetUnknown()
    {
        SetEmpty("?");

        _frontBase.sprite = UiSprites.Get(UiSprites.PanelLarge);
        _frontBase.type = Image.Type.Sliced;
        _frontBase.color = Color.white;

        _emptyText.fontSize = UnknownMarkSize * _scale;
        _emptyText.color = UiPalette.Text2;
        Place(_emptyText.rectTransform, 60f, 700f, 1278f, 560f);
    }

    /// <summary>
    /// 앞면/뒷면 전환
    /// </summary>
    public void SetFlipped(bool showBack)
    {
        if (!_hasBack)
            return;

        _showBack = showBack;
        _front.gameObject.SetActive(!showBack);
        _back.gameObject.SetActive(showBack);
    }

    /// <summary>
    /// 눌렸을 때 뒤집히는 연출. 가운데에서 앞뒤가 바뀐다
    /// </summary>
    public void PlayFlip(bool showBack)
    {
        if (!_hasBack)
            return;

        UiEffects.Flip(Root, () => SetFlipped(showBack));
    }

    /// <summary>
    /// 등장 연출 (상세 보기 · 뽑기 결과)
    /// </summary>
    public void PlayAppear(float delay = 0f)
    {
        UiEffects.Pop(Root, 0.82f, 0.3f, delay);
    }

    /// <summary>
    /// 카드를 눌렀을 때의 동작. null이면 누를 수 없다
    /// </summary>
    public void SetClickHandler(Action onClick)
    {
        _onClick = onClick;
        _button.interactable = onClick != null;
    }

    /// <summary>
    /// 조립 폭과 다르게 스케일로 키우거나 줄여 띄울 때 실제 화면 폭을 알린다. 사진 해상도를 고르는 데 쓴다
    /// </summary>
    public void SetDisplayWidth(float width)
    {
        if (Mathf.Approximately(_displayWidth, width))
            return;

        _displayWidth = width;

        if (_photoKey != null)
            ApplyPhoto();
    }

    //화면 폭이 작으면 썸네일을 쓴다. 그리드 수백 장에 원본을 올리면 메모리가 터진다
    private void ApplyPhoto()
    {
        float shownWidth = _displayWidth > 0f ? _displayWidth : BaseWidth * _scale;
        Sprite photo = PlayerPhotoProvider.Get(_photoKey, shownWidth <= ThumbMaxDisplayWidth);

        _photo.sprite = photo;
        _photo.gameObject.SetActive(photo != null);
    }

    //카드 좌표(1398 기준)를 실제 크기로 환산해 배치한다
    private void Place(RectTransform rect, float x, float y, float width, float height)
    {
        UiBuilder.Place(rect, x * _scale, y * _scale, width * _scale, height * _scale);
    }

    private Image CreateLayer(Transform parent, string name, string spritePath, float x, float y, float width, float height)
    {
        Image image = UiBuilder.CreateImage(parent, name, Color.white, spritePath);

        Place(image.rectTransform, x, y, width, height);

        if (image.sprite == null && spritePath != null)
            image.color = UiPalette.WithAlpha(Color.white, 0f);

        return image;
    }

    private TextMeshProUGUI CreateCardText(Transform parent, string name, float fontSize,
        float x, float y, float width, float height,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        TextMeshProUGUI text = UiBuilder.CreateText(parent, name, fontSize * _scale, UiPalette.Text1, alignment);

        Place(text.rectTransform, x, y, width, height);

        return text;
    }

    //베이스 스프라이트가 없으면 종류색 테두리 패널로 떨어진다
    private void ApplyBase(Image target, CardType cardType, Color teamColor)
    {
        Sprite baseSprite = UiSprites.Get(GetBaseSpritePath(cardType));

        target.sprite = baseSprite;
        target.color = baseSprite != null ? Color.white : UiPalette.WithAlpha(teamColor, 0.35f);

        if (baseSprite == null)
            target.type = Image.Type.Simple;
    }

    private void ApplyEnhanceBadge(int enhanceLevel)
    {
        bool shown = enhanceLevel > 0;

        SetEnhanceBadgeVisible(shown);

        if (!shown)
            return;

        _enhanceFill.sprite = UiSprites.Get(enhanceLevel >= EnhanceBadgeColors.HoloLevel
            ? "Card/badge_enhance_holo"
            : "Card/badge_enhance_fill");

        _enhanceFill.color = EnhanceBadgeColors.GetFillColor(enhanceLevel);
        _enhanceRim.color = EnhanceBadgeColors.GetRimColor(enhanceLevel);
        _enhanceLine.color = UiPalette.WithAlpha(Color.white, 0.2f);
        _enhanceText.text = "+" + enhanceLevel;
        _enhanceText.color = Color.white;
    }

    private void SetEnhanceBadgeVisible(bool visible)
    {
        _enhanceFill.gameObject.SetActive(visible);
        _enhanceRim.gameObject.SetActive(visible);
        _enhanceLine.gameObject.SetActive(visible);
        _enhanceText.gameObject.SetActive(visible);
    }

    private void ApplyGlow(CardType cardType)
    {
        if (cardType == CardType.Signature)
        {
            _glow.sprite = UiSprites.Get("Card/card_fx_glow_signature");
            _glow.color = UiPalette.WithAlpha(UiPalette.CardSignature, 0.8f);
            _glow.gameObject.SetActive(_glow.sprite != null);
            return;
        }

        if (cardType == CardType.GoldenGlove)
        {
            _glow.sprite = UiSprites.Get("Card/card_fx_glow_goldenglove");
            _glow.color = UiPalette.WithAlpha(UiPalette.CardGoldenGlove, 0.8f);
            _glow.gameObject.SetActive(_glow.sprite != null);
            return;
        }

        _glow.gameObject.SetActive(false);
    }

    //별은 등급 수만큼만 그리므로 가운데로 다시 모은다 (명세 6-4)
    private void CenterStars(int starCount)
    {
        float totalWidth = starCount * StarSize;
        float startX = (MaxStars * StarSize - totalWidth) * 0.5f;

        for (int i = 0; i < starCount; i++)
            Place(_stars[i].rectTransform, startX + i * StarSize, 0f, StarSize, StarSize);
    }

    private void FillBack(CardDisplayInfo info, Color typeColor)
    {
        _backNameText.text = PlayerLabelFormat.NameWithYear(info.Name, info.Year);
        _backDetailText.text = $"{info.Position} · {info.TeamName} · {GradeLabel(info.CardGrade)}";
        _backOvrText.text = "OVR " + info.FinalOvr;
        _backOvrText.color = typeColor;

        string[] labels = info.StatLabels;

        for (int i = 0; i < StatCount; i++)
        {
            int baseStat = info.GetBaseStat(i);
            int trainDelta = info.GetTrainDelta(i);
            int enhanceDelta = info.GetFinalStat(i) - baseStat - trainDelta;

            _statLabels[i].text = labels[i];
            _statValues[i].text = enhanceDelta > 0 || trainDelta > 0
                ? $"{baseStat} +{enhanceDelta + trainDelta} = {info.GetFinalStat(i)}"
                : baseStat.ToString();

            float barY = 700f + i * 190f + 120f;
            float baseWidth = BarWidth(baseStat);
            float enhanceWidth = BarWidth(enhanceDelta);
            float trainWidth = BarWidth(trainDelta);

            Place(_statBaseBars[i].rectTransform, 90f, barY, baseWidth, 40f);
            Place(_statEnhanceBars[i].rectTransform, 90f + baseWidth, barY, enhanceWidth, 40f);
            Place(_statTrainBars[i].rectTransform, 90f + baseWidth + enhanceWidth, barY, trainWidth, 40f);
        }

        _growthText.text = $"강화 +{info.EnhanceLevel}   ·   훈련 Lv.{info.TrainLevel} / {info.GetMaxTrainLevel()}";
        _breakthroughIcon.gameObject.SetActive(info.BreakthroughUsed && _breakthroughIcon.sprite != null);
    }

    private static float BarWidth(int statValue)
    {
        return Mathf.Clamp(statValue, 0, (int)StatBarMaxStat) / StatBarMaxStat * 1218f;
    }

    private static string GetBaseSpritePath(CardType cardType)
    {
        return cardType switch
        {
            CardType.Signature => "Card/card_base_signature",
            CardType.GoldenGlove => "Card/card_base_goldenglove",
            _ => "Card/card_base_normal"
        };
    }

    private static string GradeLabel(CardGrade grade)
    {
        return grade switch
        {
            CardGrade.Star3 => "3성",
            CardGrade.Star4 => "4성",
            CardGrade.Star5 => "5성",
            _ => "-"
        };
    }

    private void OnClicked()
    {
        _onClick?.Invoke();
    }
}
