using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 카드 1장 (명세 1-2 레이어 구조 · 6-6 좌표). 누르면 앞면/뒷면이 바뀌고, 꾹 누르면 원형 게이지가 차서 상세 보기로 넘긴다.
/// 왼쪽 아래 자물쇠는 잠금 토글(상세 보기) 또는 잠금 표시다. 좌표는 1398×1957 기준으로 적고 생성 시 폭에 맞춰 일괄 축소한다
/// </summary>
public class CardView : UiWidgetBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    /// <summary>
    /// 카드 원본 규격 (명세 1-2)
    /// </summary>
    public const float BaseWidth = 1398f;
    public const float BaseHeight = 1957f;

    /// <summary>
    /// 사진 칸 (카드 좌표, 위에서부터). 카드 폭에 꽉 채우고 머리가 별 아래에 오게 내린다.
    /// 좌우 잘림이 카드 테두리와 겹쳐 사진이 일부러 자른 것처럼 보인다
    /// </summary>
    public const float PhotoTop = 202f;
    public const float PhotoHeight = 1748f;

    private const int MaxStars = 5;
    private const int StatCount = 4;
    //별 그림 한 칸(그림자 자리 포함) · 보이는 별 · 별 사이 간격 · 칸 윗변
    private const float StarCell = 132f;
    private const float StarSize = 106f;
    private const float StarGap = 6f;
    private const float StarTop = 45f;
    private const float StatBarMaxStat = 150f;
    //강화 숫자 크기. 두 자리(+10)는 메달 안에 들도록 줄인다
    private const float EnhanceFontSize = 108f;
    private const float EnhanceWideFontSize = 92f;
    //이름 뒤 연도는 조금 작게 붙인다 (곽빈'26)
    private const string YearOpenTag = "<size=85%>'";
    private const string YearCloseTag = "</size>";
    //강화 + 기호는 숫자보다 작고 조금 위로
    private const string EnhancePlusTag = "<size=60%><voffset=0.12em>+</voffset></size>";
    //OVR 칸 (카드 좌표). 세 자리는 두 자리가 차지하는 가장 넓은 폭에 맞춰 가로로만 누른다
    private const float OvrLeft = 46f;
    //OVR · 포지션은 위 테두리에서 조금 띄운다
    private const float OvrTop = 70f;
    private const float PositionTop = 322f;
    private const float OvrWidth = 320f;
    //조합 결과 자리에 띄우는 물음표 크기 (카드 좌표계)
    private const float UnknownMarkSize = 520f;
    //이만큼 누르고 있으면 상세 보기로 넘긴다. 짧게 누를 때 게이지가 깜빡이지 않도록 조금 지나서 띄운다
    private const float LongPressSeconds = 0.8f;
    private const float PressGaugeDelay = 0.15f;
    //게이지 지름 (카드 폭 비율)
    private const float PressGaugeRatio = 0.42f;
    private const string PressRingSprite = "Common/press_ring";
    private const string PressDiscSprite = "Common/press_disc";
    //왼쪽 아래 자물쇠 (카드 좌표)
    private const float LockLeft = 36f;
    private const float LockTop = 1747f;
    private const float LockSize = 176f;
    private const string LockClosedSprite = "Card/lock_closed";
    private const string LockOpenSprite = "Card/lock_open";
    //화면 폭이 이 이하인 카드는 썸네일 사진(320×400)으로 충분하다
    private const float ThumbMaxDisplayWidth = 360f;

    [SerializeField] private RectTransform _front;
    [SerializeField] private RectTransform _back;
    [SerializeField] private float _scale;

    [SerializeField] private Image _frontBase;
    [SerializeField] private Image _overlay;
    [SerializeField] private Image _photoBack;
    [SerializeField] private Image _photo;
    [SerializeField] private Image _foreground;
    [SerializeField] private Image _teamLogo;
    [SerializeField] private Image _shine;
    [SerializeField] private Image _glow;
    [SerializeField] private TextMeshProUGUI _ovrText;
    [SerializeField] private TextMeshProUGUI _positionText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image[] _starBacks = new Image[MaxStars];
    [SerializeField] private Image[] _stars = new Image[MaxStars];

    [SerializeField] private Image _enhanceBadge;
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
    private bool _cardFontApplied;
    private float _ovrMaxWidth;
    [SerializeField] private bool _hasBack;
    //게이지 · 자물쇠는 처음 쓸 때 만든다. 이미 구운 화면 프리팹을 다시 굽지 않아도 된다
    private RectTransform _pressGauge;
    private Image _pressFill;
    private Button _lockButton;
    private Image _lockIcon;
    private Action _onLockToggle;
    private bool _locked;
    private bool _showsCard;

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

        //시그·골글 테두리 광은 카드 뒤에 깐다 (명세 6-6). 9-슬라이스 테두리도 카드 축소 비율을 따라야
        //고리가 카드 가장자리에 붙는다. 그대로 두면 작은 카드에서 고리가 카드 안쪽으로 파고든다
        _glow = CreateLayer(Root, "Glow", "Card/card_fx_glow_signature", -180f, -180f, BaseWidth + 360f, BaseHeight + 360f);
        _glow.type = Image.Type.Sliced;
        _glow.pixelsPerUnitMultiplier = 1f / _scale;

        _front = UiBuilder.CreateRect(Root, "Front");
        UiBuilder.Stretch(_front, 0f, 0f, 0f, 0f);

        if (_hasBack)
        {
            _back = UiBuilder.CreateRect(Root, "Back");
            UiBuilder.Stretch(_back, 0f, 0f, 0f, 0f);
            _back.gameObject.SetActive(false);
        }

        // --- 앞면: 배경 -> 사진 뒤 그림자 -> 선수 -> 하단 물감 띠 -> 글자 -> 광택 (명세 1-2 · 6-6) ---
        //배경 그림은 완전 불투명이라 맨 아래다. 골글 월계수는 그 위, 사진 뒤에 깐다
        _frontBase = CreateLayer(_front, "Base", null, 0f, 0f, BaseWidth, BaseHeight);
        _overlay = CreateHiddenLayer(_front, "Overlay", 0f, 0f, BaseWidth, BaseHeight);

        //그림자·빛 번짐은 사진 칸보다 넓게 퍼지므로 카드 전체 칸에 깔고 셰이더가 사진 위치를 안다
        _photoBack = CreateHiddenLayer(_front, "PhotoBack", 0f, 0f, BaseWidth, BaseHeight);
        _photo = CreateHiddenLayer(_front, "Photo", 0f, PhotoTop, BaseWidth, PhotoHeight);
        _foreground = CreateHiddenLayer(_front, "Foreground", 0f, 0f, BaseWidth, BaseHeight);

        _ovrText = CreateCardText(_front, "Ovr", 230f, OvrLeft, OvrTop, OvrWidth, 230f, TextAlignmentOptions.Center);
        CenterPivotX(_ovrText.rectTransform);
        _positionText = CreateCardText(_front, "Position", 104f, OvrLeft, PositionTop, OvrWidth, 104f, TextAlignmentOptions.Center);
        _teamLogo = CreateLayer(_front, "TeamLogo", null, 1046f, 60f, 300f, 300f);
        _teamLogo.preserveAspect = true;

        RectTransform starRow = UiBuilder.CreateRect(_front, "Stars");
        Place(starRow, 0f, StarTop, BaseWidth, StarCell);

        for (int i = 0; i < MaxStars; i++)
        {
            _starBacks[i] = CreateLayer(starRow, "StarBack", UiSprites.CardStarBack, 0f, 0f, StarCell, StarCell);
            _starBacks[i].gameObject.SetActive(false);

            _stars[i] = CreateLayer(starRow, "Star", UiSprites.CardStarFill, 0f, 0f, StarCell, StarCell);
            _stars[i].gameObject.SetActive(false);
        }

        _enhanceBadge = CreateHiddenLayer(_front, "EnhanceBadge", 1076f, 372f, 240f, 208f);
        _enhanceText = CreateCardText(_front, "EnhanceText", EnhanceFontSize, 1076f, 372f, 240f, 208f,
            TextAlignmentOptions.Center);

        _nameText = CreateCardText(_front, "Name", 138f, 40f, 1728f, 1318f, 170f, TextAlignmentOptions.Center);

        _shine = CreateHiddenLayer(_front, "Shine", 0f, 0f, BaseWidth, BaseHeight);

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
        ApplyCardFont();

        _emptyText.gameObject.SetActive(false);
        _front.gameObject.SetActive(!_showBack);

        if (_hasBack)
            _back.gameObject.SetActive(_showBack);

        CardStyle style = CardStyle.Get(info.CardType, info.CardGrade);
        Color typeColor = CardVisualLookup.GetTypeColor(info.CardType);

        ApplyBase(_frontBase, style);
        _frontBase.material = CardMaterials.GetHolo(style);

        if (_hasBack)
            ApplyBase(_backBase, style);

        ShowSprite(_overlay, style.OverlaySprite);
        ShowSprite(_foreground, style.ForegroundSprite);

        _photoBack.material = CardMaterials.GetPhotoBack(style);
        _photo.material = CardMaterials.GetPhoto(style);
        _photoKey = info.PhotoKey;
        ApplyPhoto();

        //노말은 어두운 배경이라 흰 숫자, 시그·골글은 밝은 배경이라 진한 새김색
        Material ovrMaterial = style.IsLightBase ? CardTextMaterials.LightShadow : CardTextMaterials.Shadow;

        _ovrText.text = info.FinalOvr.ToString();
        _ovrText.color = style.OvrColor;
        _ovrText.fontSharedMaterial = ovrMaterial;
        FitOvrWidth();

        _positionText.text = info.Position;
        _positionText.color = style.OvrColor;
        _positionText.fontSharedMaterial = ovrMaterial;

        Sprite logo = UiSprites.Get("Team/logo_" + info.TeamName);

        _teamLogo.sprite = logo;
        _teamLogo.gameObject.SetActive(logo != null);

        ApplyStars(info.CardGrade);
        ApplyEnhanceBadge(info.EnhanceLevel);

        _nameText.text = info.Name + YearOpenTag + PlayerLabelFormat.Year(info.Year) + YearCloseTag;

        _shine.material = CardMaterials.Shine;
        _shine.color = UiPalette.WithAlpha(Color.white, style.ShineStrength);
        _shine.gameObject.SetActive(true);
        ApplyGlow(info.CardType);

        if (_hasBack)
            FillBack(info, typeColor);

        _showsCard = true;
        SetLocked(info.IsLocked);
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

        _frontBase.sprite = UiSprites.Get(UiSprites.CardSlotEmpty);
        _frontBase.type = Image.Type.Simple;
        _frontBase.material = null;
        _frontBase.color = _frontBase.sprite == null ? UiPalette.BgPanel : Color.white;

        _photoKey = null;
        _photo.gameObject.SetActive(false);
        _photoBack.gameObject.SetActive(false);
        _overlay.gameObject.SetActive(false);
        _foreground.gameObject.SetActive(false);
        _teamLogo.gameObject.SetActive(false);

        _ovrText.text = string.Empty;
        _positionText.text = string.Empty;
        _nameText.text = string.Empty;
        _shine.gameObject.SetActive(false);
        _glow.gameObject.SetActive(false);

        ApplyStars(CardGrade.None);
        ApplyEnhanceBadge(0);

        _emptyText.gameObject.SetActive(true);
        _emptyText.text = label;

        _showsCard = false;
        RefreshLock();
    }

    /// <summary>
    /// 길게 누르면 부를 콜백 (카드 상세 보기)
    /// </summary>
    public void SetLongPressHandler(Action onLongPress)
    {
        _onLongPress = onLongPress;
    }

    /// <summary>
    /// 왼쪽 아래 자물쇠를 누를 때의 동작. 있으면 잠김 · 풀림을 오가는 버튼이 되고,
    /// null 이면 잠긴 카드에만 닫힌 자물쇠가 표시로 보인다
    /// </summary>
    public void SetLockToggleHandler(Action onToggle)
    {
        _onLockToggle = onToggle;
        RefreshLock();
    }

    /// <summary>
    /// 자물쇠 모양만 다시 그린다 (토글 직후)
    /// </summary>
    public void SetLocked(bool locked)
    {
        _locked = locked;
        RefreshLock();
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
        HidePressGauge();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        _pressing = false;
        HidePressGauge();
    }

    //길게 누르는 동안만 세며 게이지를 채운다. 한 번 터지면 손을 뗄 때까지 다시 터지지 않는다
    private void Update()
    {
        if (!_pressing || _longPressFired || _onLongPress == null)
            return;

        _pressSeconds += Time.unscaledDeltaTime;
        ShowPressGauge(Mathf.InverseLerp(PressGaugeDelay, LongPressSeconds, _pressSeconds));

        if (_pressSeconds < LongPressSeconds)
            return;

        _longPressFired = true;
        HidePressGauge();
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
        _photoBack.sprite = photo;
        _photoBack.gameObject.SetActive(photo != null);
    }

    //카드 앞면 글자를 카드 폰트로 바꾼다. 밑그림자 머티리얼은 런타임에 만들어 프리팹에 저장되지 않으므로 처음 그릴 때 함께 입힌다
    private void ApplyCardFont()
    {
        if (_cardFontApplied)
            return;

        _cardFontApplied = true;

        TMP_FontAsset cardFont = UiFontProvider.Card;

        _ovrText.font = cardFont;
        _positionText.font = cardFont;
        _nameText.font = cardFont;
        _nameText.fontSharedMaterial = CardTextMaterials.Shadow;
        _enhanceText.font = cardFont;
        _enhanceText.fontSharedMaterial = CardTextMaterials.Emboss;
        _ovrMaxWidth = MeasureWidestTwoDigits();
    }

    //세 자리 OVR 은 두 자리 최대 폭에 맞춰 가로로만 누른다. 키는 그대로라 숫자 크기가 같아 보인다
    private void FitOvrWidth()
    {
        float width = _ovrText.GetPreferredValues(_ovrText.text).x;
        float squeeze = _ovrMaxWidth > 0f && width > _ovrMaxWidth ? _ovrMaxWidth / width : 1f;

        _ovrText.rectTransform.localScale = new Vector3(squeeze, 1f, 1f);
    }

    //숫자마다 폭이 다른 글꼴이라 00 ~ 99 중 가장 넓은 것을 기준으로 삼는다
    private float MeasureWidestTwoDigits()
    {
        float widest = 0f;

        for (int digit = 0; digit <= 9; digit++)
            widest = Mathf.Max(widest, _ovrText.GetPreferredValues(digit.ToString() + digit).x);

        return widest;
    }

    //가로로 누를 때 가운데를 축으로 줄어들도록 중심을 칸 가운데로 옮긴다 (위치는 그대로)
    private static void CenterPivotX(RectTransform rect)
    {
        rect.pivot = new Vector2(0.5f, rect.pivot.y);
        rect.anchoredPosition += new Vector2(rect.sizeDelta.x * 0.5f, 0f);
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

    //효과 레이어는 스프라이트·머티리얼이 정해지기 전까지 흰 사각형이 보이지 않게 꺼 둔다
    private Image CreateHiddenLayer(Transform parent, string name, float x, float y, float width, float height)
    {
        Image image = CreateLayer(parent, name, null, x, y, width, height);

        image.gameObject.SetActive(false);

        return image;
    }

    //베이스 스프라이트가 없으면 어두운 패널로 떨어진다. 물음표 카드가 바꿔 둔 9-슬라이스도 되돌린다
    private void ApplyBase(Image target, CardStyle style)
    {
        Sprite baseSprite = UiSprites.Get(style.BaseSprite);

        target.sprite = baseSprite;
        target.type = Image.Type.Simple;
        target.color = baseSprite != null ? Color.white : UiPalette.BgPanel;
    }

    //경로가 없거나 그림이 없으면 레이어를 끈다
    private static void ShowSprite(Image target, string spritePath)
    {
        Sprite sprite = UiSprites.Get(spritePath);

        target.sprite = sprite;
        target.gameObject.SetActive(sprite != null);
    }

    //별은 등급 수만큼 가운데로 모아 그린다. 흰 테두리 별 위에 등급색 별을 얹는다 (명세 6-4 · 6-6)
    private void ApplyStars(CardGrade cardGrade)
    {
        int starCount = (int)cardGrade;
        Color starColor = CardStyle.GetStarColor(cardGrade);
        float totalWidth = starCount * StarSize + Mathf.Max(0, starCount - 1) * StarGap;
        float startX = (BaseWidth - totalWidth) * 0.5f - (StarCell - StarSize) * 0.5f;

        for (int i = 0; i < MaxStars; i++)
        {
            bool shown = i < starCount;

            _starBacks[i].gameObject.SetActive(shown);
            _stars[i].gameObject.SetActive(shown);

            if (!shown)
                continue;

            float x = startX + i * (StarSize + StarGap);

            Place(_starBacks[i].rectTransform, x, 0f, StarCell, StarCell);
            Place(_stars[i].rectTransform, x, 0f, StarCell, StarCell);
            _stars[i].color = starColor;
        }
    }

    //0이면 메달을 끈다. 금속(동·은·금·홀로)마다 숫자 새김색이 다르다
    private void ApplyEnhanceBadge(int enhanceLevel)
    {
        bool shown = enhanceLevel > 0;

        _enhanceBadge.gameObject.SetActive(shown);
        _enhanceText.gameObject.SetActive(shown);

        if (!shown)
            return;

        _enhanceBadge.sprite = UiSprites.Get(EnhanceBadgeStyle.GetSpritePath(enhanceLevel));
        _enhanceText.text = EnhancePlusTag + enhanceLevel;
        _enhanceText.color = EnhanceBadgeStyle.GetInkColor(enhanceLevel);
        _enhanceText.fontSize = (enhanceLevel >= EnhanceBadgeStyle.HoloLevel ? EnhanceWideFontSize : EnhanceFontSize) * _scale;
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

    //길게 눌러 상세 보기로 넘긴 손을 뗄 때 딸려 오는 클릭은 버린다
    private void OnClicked()
    {
        if (_longPressFired)
            return;

        _onClick?.Invoke();
    }

    //카드 가운데 원형 게이지. 어두운 원판 위 흐린 고리를 흰색으로 시계 방향 채운다
    private void ShowPressGauge(float fill)
    {
        if (fill <= 0f)
            return;

        if (_pressGauge == null)
            CreatePressGauge();

        float pop = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(fill * 5f));

        _pressGauge.gameObject.SetActive(true);
        _pressGauge.localScale = new Vector3(pop, pop, 1f);
        _pressFill.fillAmount = fill;
    }

    private void HidePressGauge()
    {
        if (_pressGauge != null)
            _pressGauge.gameObject.SetActive(false);
    }

    private void CreatePressGauge()
    {
        float size = Root.rect.width * PressGaugeRatio;

        _pressGauge = UiBuilder.CreateRect(Root, "PressGauge");
        _pressGauge.anchorMin = new Vector2(0.5f, 0.5f);
        _pressGauge.anchorMax = new Vector2(0.5f, 0.5f);
        _pressGauge.sizeDelta = new Vector2(size, size);

        Image disc = UiBuilder.CreateImage(_pressGauge, "Disc", UiPalette.WithAlpha(Color.black, 0.55f), PressDiscSprite);
        UiBuilder.Stretch(disc.rectTransform, 0f, 0f, 0f, 0f);

        Image track = UiBuilder.CreateImage(_pressGauge, "Track", UiPalette.WithAlpha(Color.white, 0.16f), PressRingSprite);
        UiBuilder.Stretch(track.rectTransform, 0f, 0f, 0f, 0f);

        _pressFill = UiBuilder.CreateImage(_pressGauge, "Fill", Color.white, PressRingSprite, Image.Type.Filled);
        UiBuilder.Stretch(_pressFill.rectTransform, 0f, 0f, 0f, 0f);
        _pressFill.fillMethod = Image.FillMethod.Radial360;
        _pressFill.fillOrigin = (int)Image.Origin360.Top;
        _pressFill.fillClockwise = true;
        _pressFill.fillAmount = 0f;
    }

    //토글이 있으면 늘 보이며 눌린다. 없으면 잠긴 카드에만 표시로 보이고 누름은 카드로 지나간다
    private void RefreshLock()
    {
        bool toggle = _onLockToggle != null;
        bool shown = _showsCard && (_locked || toggle);

        if (!shown)
        {
            if (_lockButton != null)
                _lockButton.gameObject.SetActive(false);

            return;
        }

        if (_lockButton == null)
            CreateLockButton();

        _lockButton.gameObject.SetActive(true);
        _lockButton.interactable = toggle;
        _lockIcon.raycastTarget = toggle;
        _lockIcon.sprite = UiSprites.Get(_locked ? LockClosedSprite : LockOpenSprite);
    }

    private void CreateLockButton()
    {
        _lockIcon = UiBuilder.CreateImage(Root, "Lock", Color.white, LockClosedSprite);
        Place(_lockIcon.rectTransform, LockLeft, LockTop, LockSize, LockSize);

        _lockButton = _lockIcon.gameObject.AddComponent<Button>();
        _lockButton.targetGraphic = _lockIcon;
        _lockButton.transition = Selectable.Transition.None;
        _lockButton.onClick.AddListener(OnLockClicked);
        _lockIcon.gameObject.AddComponent<UiPressFeedback>();
    }

    private void OnLockClicked()
    {
        _onLockToggle?.Invoke();
    }
}
