using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 강화 재료 목록 맨 앞의 강화 전용 카드 칸. 누를 때마다 1묶음(+1)씩 넣고, 왼쪽 위 − 로 하나씩 뺀다.
/// 카드 타일(CardCellView)과 같은 규격으로 조립해 카드 목록 칸에 그대로 끼운다
/// </summary>
public class EnhanceCardTileView
{
    private const float IconSize = 250f;
    private const float IconTop = 30f;
    //넣은 묶음 수 글자는 가운데 체크 아래
    private const float SelectedTextTop = 330f;
    private const float MinusSize = 92f;
    private const float MinusMargin = 14f;
    private const float MinusIconSize = 52f;
    private const string MinusSprite = "Common/icon_minus";
    private const string DiscSprite = "Common/press_disc";
    private static readonly Color PlateColor = new Color(0.11f, 0.12f, 0.13f, 1f);

    /// <summary>
    /// 타일 루트. 카드 목록이 자기 칸으로 옮겨 놓는다
    /// </summary>
    public RectTransform Root => _root;

    private readonly RectTransform _root;
    private readonly Image _frame;
    private readonly Image _icon;
    private readonly TextMeshProUGUI _nameText;
    private readonly TextMeshProUGUI _ownedText;
    private readonly TextMeshProUGUI _ruleText;
    private readonly GameObject _selectedLayer;
    private readonly Image _checkMark;
    private readonly TextMeshProUGUI _selectedText;
    private readonly GameObject _minusButton;

    private Action _onAdd;
    private Action _onRemove;
    private int _sets;

    public EnhanceCardTileView(Transform parent)
    {
        _root = UiBuilder.CreateRect(parent, "EnhanceCardTile");
        UiBuilder.Place(_root, 0f, 0f, CardCellView.CellWidth, CardCellView.CellHeight);

        Image plate = UiBuilder.CreateImage(_root, "Plate", PlateColor, UiSprites.CardFill, Image.Type.Sliced);
        UiBuilder.Stretch(plate.rectTransform, 0f, 0f, 0f, 0f);
        plate.raycastTarget = true;

        Button addButton = plate.gameObject.AddComponent<Button>();
        addButton.transition = Selectable.Transition.None;
        addButton.onClick.AddListener(OnAddClicked);
        plate.gameObject.AddComponent<UiPressFeedback>();

        _frame = UiBuilder.CreateImage(plate.transform, "Frame", Color.white, UiSprites.CardFrame, Image.Type.Sliced);
        UiBuilder.Stretch(_frame.rectTransform, 0f, 0f, 0f, 0f);
        _frame.raycastTarget = false;

        _icon = UiBuilder.CreateImage(plate.transform, "Icon", Color.white);
        UiBuilder.Place(_icon.rectTransform, (CardCellView.CellWidth - IconSize) * 0.5f, IconTop, IconSize, IconSize);
        _icon.preserveAspect = true;
        _icon.raycastTarget = false;

        _nameText = CreateCenteredText(plate.transform, "Name", 32f, UiPalette.Text1, 284f, 48f);
        _nameText.enableAutoSizing = true;
        _nameText.fontSizeMin = 22f;
        _nameText.fontSizeMax = 32f;

        _ownedText = CreateCenteredText(plate.transform, "Owned", 28f, UiPalette.Text2, 336f, 40f);
        _ruleText = CreateCenteredText(plate.transform, "Rule", 24f, UiPalette.Text3, 384f, 36f);

        Image dim = CardCellView.CreateCheckOverlay(_root, out _checkMark);
        _selectedLayer = dim.gameObject;

        _selectedText = CreateCenteredText(dim.transform, "Count", 38f, UiPalette.Text1, SelectedTextTop, 56f);

        _minusButton = CreateMinusButton();
        _selectedLayer.SetActive(false);
        _minusButton.SetActive(false);
    }

    /// <summary>
    /// 칸을 눌러 넣기 · − 로 빼기 동작
    /// </summary>
    public void SetHandlers(Action onAdd, Action onRemove)
    {
        _onAdd = onAdd;
        _onRemove = onRemove;
    }

    /// <summary>
    /// 대상 카드에 맞는 전용 카드 종류와 보유 장수
    /// </summary>
    public void SetCard(CurrencyType enhanceCardType, int owned, int cardsPerLevel)
    {
        Color rarityColor = GetRarityColor(enhanceCardType);

        _icon.sprite = UiSprites.Get(GetIconPath(enhanceCardType));
        _frame.color = UiPalette.WithAlpha(rarityColor, 0.9f);
        _nameText.text = $"{GetGradeLabel(enhanceCardType)} 강화 카드";
        _ownedText.text = $"보유 {owned}장";
        _ruleText.text = $"{cardsPerLevel}장 = +1";
    }

    /// <summary>
    /// 넣은 묶음 수. 0 이면 선택 표시를 걷는다
    /// </summary>
    public void SetSelectedSets(int sets, int cardsPerLevel)
    {
        bool selected = sets > 0;

        _selectedLayer.SetActive(selected);
        _minusButton.SetActive(selected);
        _nameText.gameObject.SetActive(!selected);
        _ownedText.gameObject.SetActive(!selected);
        _ruleText.gameObject.SetActive(!selected);
        _selectedText.text = $"+{sets}  ·  {sets * cardsPerLevel}장";

        if (sets > _sets)
            UiEffects.Pop(_checkMark.transform, 0.5f, 0.22f);

        _sets = sets;
    }

    private TextMeshProUGUI CreateCenteredText(Transform parent, string name, float fontSize, Color color,
        float top, float height)
    {
        TextMeshProUGUI text = UiBuilder.CreateText(parent, name, fontSize, color, TextAlignmentOptions.Center);

        UiBuilder.Place(text.rectTransform, 8f, top, CardCellView.CellWidth - 16f, height);
        text.raycastTarget = false;

        return text;
    }

    //흰 원판 위 남색 − . 선택 표시 위에 얹는다
    private GameObject CreateMinusButton()
    {
        Image disc = UiBuilder.CreateImage(_root, "MinusButton", UiPalette.Text1, DiscSprite);
        UiBuilder.Place(disc.rectTransform, MinusMargin, MinusMargin, MinusSize, MinusSize);
        disc.raycastTarget = true;

        Button button = disc.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OnRemoveClicked);
        disc.gameObject.AddComponent<UiPressFeedback>();

        Image icon = UiBuilder.CreateImage(disc.transform, "Icon", UiPalette.ButtonPrimaryText, MinusSprite);
        UiBuilder.Place(icon.rectTransform, (MinusSize - MinusIconSize) * 0.5f, (MinusSize - MinusIconSize) * 0.5f,
            MinusIconSize, MinusIconSize);
        icon.raycastTarget = false;

        return disc.gameObject;
    }

    private void OnAddClicked()
    {
        _onAdd?.Invoke();
    }

    private void OnRemoveClicked()
    {
        _onRemove?.Invoke();
    }

    private static string GetIconPath(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.EnhanceCardStar3 => "Currency/currency_enhance_card_star3",
            CurrencyType.EnhanceCardStar4 => "Currency/currency_enhance_card_star4",
            CurrencyType.EnhanceCardStar5 => "Currency/currency_enhance_card_star5",
            CurrencyType.EnhanceCardSignature => "Currency/currency_enhance_card_signature",
            CurrencyType.EnhanceCardGoldenGlove => "Currency/currency_enhance_card_goldenglove",
            _ => null
        };
    }

    private static string GetGradeLabel(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.EnhanceCardStar3 => "3성",
            CurrencyType.EnhanceCardStar4 => "4성",
            CurrencyType.EnhanceCardStar5 => "5성",
            CurrencyType.EnhanceCardSignature => "시그니쳐",
            CurrencyType.EnhanceCardGoldenGlove => "골든글러브",
            _ => string.Empty
        };
    }

    private static Color GetRarityColor(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.EnhanceCardStar3 => UiPalette.CardStar3,
            CurrencyType.EnhanceCardStar4 => UiPalette.CardStar4,
            CurrencyType.EnhanceCardStar5 => UiPalette.CardStar5,
            CurrencyType.EnhanceCardSignature => UiPalette.CardSignature,
            CurrencyType.EnhanceCardGoldenGlove => UiPalette.CardGoldenGlove,
            _ => UiPalette.LineBorder
        };
    }
}
