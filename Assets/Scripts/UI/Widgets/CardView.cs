using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 1장 (명세 1-2 레이어 구조 · 6-6 좌표). 누르면 앞면/뒷면이 바뀐다.
/// 좌표는 1398×1957 기준으로 적고 생성 시 폭에 맞춰 일괄 축소한다
/// </summary>
public class CardView
{
    /// <summary>
    /// 카드 원본 규격 (명세 1-2)
    /// </summary>
    public const float BaseWidth = 1398f;
    public const float BaseHeight = 1957f;

    private const int MaxStars = 5;
    private const int StatCount = 4;
    private const float StarSize = 176f;
    private const float StatBarMaxStat = 150f;

    public RectTransform Root => _root;

    private readonly RectTransform _root;
    private readonly RectTransform _front;
    private readonly RectTransform _back;
    private readonly float _scale;

    private readonly Image _frontBase;
    private readonly Image _teamAura;
    private readonly Image _photo;
    private readonly Image _photoMask;
    private readonly Image _nameShadow;
    private readonly Image _teamLogo;
    private readonly Image _shine;
    private readonly Image _glow;
    private readonly TextMeshProUGUI _ovrText;
    private readonly TextMeshProUGUI _positionText;
    private readonly TextMeshProUGUI _nameText;
    private readonly Image[] _stars = new Image[MaxStars];

    private readonly Image _enhanceFill;
    private readonly Image _enhanceRim;
    private readonly Image _enhanceLine;
    private readonly TextMeshProUGUI _enhanceText;

    private readonly Image _backBase;
    private readonly TextMeshProUGUI _backNameText;
    private readonly TextMeshProUGUI _backDetailText;
    private readonly TextMeshProUGUI _backOvrText;
    private readonly TextMeshProUGUI[] _statLabels = new TextMeshProUGUI[StatCount];
    private readonly TextMeshProUGUI[] _statValues = new TextMeshProUGUI[StatCount];
    private readonly Image[] _statBaseBars = new Image[StatCount];
    private readonly Image[] _statEnhanceBars = new Image[StatCount];
    private readonly Image[] _statTrainBars = new Image[StatCount];
    private readonly TextMeshProUGUI _growthText;
    private readonly Image _breakthroughIcon;

    private readonly Button _button;
    private readonly TextMeshProUGUI _emptyText;

    private Action _onClick;
    private bool _showBack;

    public CardView(Transform parent, float width)
    {
        _scale = width / BaseWidth;

        _root = UiBuilder.CreateRect(parent, "Card");
        UiBuilder.Place(_root, 0f, 0f, width, BaseHeight * _scale);

        Image hitArea = UiBuilder.CreateImage(_root, "HitArea", UiPalette.WithAlpha(UiPalette.BgApp, 0f));
        UiBuilder.Stretch(hitArea.rectTransform, 0f, 0f, 0f, 0f);
        hitArea.raycastTarget = true;

        _button = _root.gameObject.AddComponent<Button>();
        _button.targetGraphic = hitArea;
        _button.transition = Selectable.Transition.None;
        _button.interactable = false;
        _button.onClick.AddListener(OnClicked);

        _front = UiBuilder.CreateRect(_root, "Front");
        UiBuilder.Stretch(_front, 0f, 0f, 0f, 0f);

        _back = UiBuilder.CreateRect(_root, "Back");
        UiBuilder.Stretch(_back, 0f, 0f, 0f, 0f);
        _back.gameObject.SetActive(false);

        // --- 앞면: 배경 -> 선수 -> 프레임 -> 텍스트 -> 효과 (명세 1-2) ---
        _teamAura = CreateLayer(_front, "TeamAura", "Card/card_team_aura", 139f, 300f, 1120f, 1120f);
        _photo = CreateLayer(_front, "Photo", null, 123f, 150f, 1152f, 1440f);
        _photo.preserveAspect = true;
        _photoMask = CreateLayer(_front, "PhotoMask", "Card/card_mask_photo", 0f, 150f, 1398f, 1700f);
        _frontBase = CreateLayer(_front, "Base", null, 0f, 0f, BaseWidth, BaseHeight);
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
        _glow = CreateLayer(_front, "Glow", "Card/card_fx_glow_signature", -180f, -180f, BaseWidth + 360f, BaseHeight + 360f);

        _emptyText = CreateCardText(_front, "Empty", 120f, 60f, 880f, 1278f, 200f, TextAlignmentOptions.Center);
        _emptyText.gameObject.SetActive(false);

        // --- 뒷면 ---
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
        _back.gameObject.SetActive(_showBack);

        Color typeColor = CardVisualLookup.GetTypeColor(info.CardType);
        Color teamColor = UiPalette.GetTeamColor(info.TeamName);

        ApplyBase(_frontBase, info.CardType, teamColor);
        ApplyBase(_backBase, info.CardType, teamColor);

        _teamAura.color = UiPalette.WithAlpha(teamColor, 0.55f);
        _photoMask.color = UiPalette.WithAlpha(UiPalette.BgApp, _photoMask.sprite == null ? 0f : 0.9f);

        Sprite photo = PlayerPhotoProvider.Get(info.PhotoKey);

        _photo.sprite = photo;
        _photo.gameObject.SetActive(photo != null);

        _nameShadow.color = UiPalette.WithAlpha(typeColor, 0.45f);

        _ovrText.text = info.FinalOvr.ToString();
        _ovrText.color = info.CardType == CardType.Normal ? UiPalette.Text1 : typeColor;

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

        FillBack(info, typeColor);
    }

    /// <summary>
    /// 빈 슬롯 (라인업 칸 · 조합 재료 칸)
    /// </summary>
    public void SetEmpty(string label)
    {
        _showBack = false;
        _front.gameObject.SetActive(true);
        _back.gameObject.SetActive(false);

        _frontBase.sprite = UiSprites.Get(UiSprites.CardSlotEmpty);
        _frontBase.color = _frontBase.sprite == null ? UiPalette.BgPanel : Color.white;

        _photo.gameObject.SetActive(false);
        _teamLogo.gameObject.SetActive(false);
        _teamAura.color = UiPalette.WithAlpha(UiPalette.BgApp, 0f);
        _photoMask.color = UiPalette.WithAlpha(UiPalette.BgApp, 0f);
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
    /// 앞면/뒷면 전환
    /// </summary>
    public void SetFlipped(bool showBack)
    {
        _showBack = showBack;
        _front.gameObject.SetActive(!showBack);
        _back.gameObject.SetActive(showBack);
    }

    /// <summary>
    /// 카드를 눌렀을 때의 동작. null이면 누를 수 없다
    /// </summary>
    public void SetClickHandler(Action onClick)
    {
        _onClick = onClick;
        _button.interactable = onClick != null;
    }

    public void SetVisible(bool visible)
    {
        _root.gameObject.SetActive(visible);
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
