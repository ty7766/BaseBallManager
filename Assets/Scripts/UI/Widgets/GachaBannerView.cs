using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 뽑기 로비의 큰 배너. 뽑기 종류 색으로 물든 빛살 무늬 위에 대표 카드 3장을 부채꼴로 세우고,
/// 아래에 배너 이름 · 설명 · 확정 띠를 얹는다. 좌우 화살표 또는 옆으로 밀어 다른 뽑기로 넘긴다
/// </summary>
public class GachaBannerView : UiWidgetBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>
    /// 대표 카드 수
    /// </summary>
    public const int FeaturedCount = 3;

    private const string ArtPath = "UI/Gacha/gacha_banner_art";
    private const string ShadePath = "UI/Gacha/gacha_banner_shade";
    private const string ArrowSprite = "Gacha/swipe_arrow";
    private const float ArtWidth = 1016f;
    private const float ArtHeight = 1320f;
    private const float ArtAlpha = 0.6f;
    private const float FrameAlpha = 0.75f;
    //대표 카드: 가운데 큰 카드 + 양옆 조금 작게 기울여 뒤에 둔다 (배너 위쪽 기준)
    private const float CenterCardWidth = 430f;
    private const float SideCardWidth = 300f;
    private const float SideCardOffsetX = 270f;
    private const float SideCardDrop = 70f;
    private const float SideCardTilt = 10f;
    private const float CardsTop = 120f;
    //아래 글자 받침
    private const float ShadeHeight = 560f;
    private const float TitleBottom = 156f;
    private const float SubtitleBottom = 108f;
    private const float BadgeBottom = 36f;
    private const float BadgeHeight = 58f;
    private const float BadgeWidth = 560f;
    private const float TextLeft = 40f;
    private const float ArrowWidth = 72f;
    private const float ArrowHeight = ArrowWidth * 164f / 330f;
    private const float ArrowInset = 18f;
    private const float HistoryWidth = 132f;
    private const float HistoryHeight = 64f;
    //이만큼 옆으로 밀어야 다음 배너로 넘긴다
    private const float SwipeThreshold = 90f;

    [SerializeField] private RawImage _art;
    [SerializeField] private Image _frame;
    [SerializeField] private Image _badge;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _subtitle;
    [SerializeField] private TextMeshProUGUI _badgeText;
    [SerializeField] private CardView[] _featured = new CardView[FeaturedCount];
    [SerializeField] private Button _previousButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _historyButton;

    private Action _onPrevious;
    private Action _onNext;
    private float _dragDistance;

    /// <summary>
    /// 이전 · 다음 배너 · 기록 버튼 (화면이 핸들러를 건다)
    /// </summary>
    public Button PreviousButton => _previousButton;
    public Button NextButton => _nextButton;
    public Button HistoryButton => _historyButton;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점). 크기는 부모가 Stretch 로 정한다
    /// </summary>
    public static GachaBannerView Bake(Transform parent)
    {
        GachaBannerView view = CreateNode<GachaBannerView>(parent, "GachaBanner");

        view.BuildWidget();

        return view;
    }

    /// <summary>
    /// 옆으로 밀었을 때 부를 동작
    /// </summary>
    public void SetSwipeHandlers(Action onPrevious, Action onNext)
    {
        _onPrevious = onPrevious;
        _onNext = onNext;
    }

    /// <summary>
    /// 배너 내용을 채운다. 대표 카드가 모자라면 남는 자리는 비운다
    /// </summary>
    public void SetBanner(string title, string subtitle, string badge, Color accent, IReadOnlyList<CardDisplayInfo> featured)
    {
        _title.text = title;
        _subtitle.text = subtitle;
        _badgeText.text = badge;
        _art.color = UiPalette.WithAlpha(accent, ArtAlpha);
        _frame.color = UiPalette.WithAlpha(accent, FrameAlpha);
        _badge.color = accent;

        for (int i = 0; i < _featured.Length; i++)
        {
            bool shown = i < featured.Count;

            _featured[i].Root.gameObject.SetActive(shown);

            if (shown)
                _featured[i].SetCard(featured[i]);
        }
    }

    /// <summary>
    /// 옆으로 밀기 시작
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragDistance = 0f;
    }

    /// <summary>
    /// 민 거리를 모은다
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        _dragDistance += eventData.delta.x;
    }

    /// <summary>
    /// 충분히 밀었으면 다른 배너를 연다 (왼쪽으로 밀면 다음)
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragDistance <= -SwipeThreshold)
            _onNext?.Invoke();
        else if (_dragDistance >= SwipeThreshold)
            _onPrevious?.Invoke();
    }

    //모양 마스크 안: 바탕 · 무늬 · 대표 카드 · 그늘 · 글자. 마스크 밖: 테두리 · 화살표 · 기록
    private void BuildWidget()
    {
        Image shape = UiBuilder.CreateImage(Root, "Shape", UiPalette.BgPanel, UiSprites.ButtonPrimary, Image.Type.Sliced);
        UiBuilder.Stretch(shape.rectTransform, 0f, 0f, 0f, 0f);
        shape.raycastTarget = true;
        shape.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        RectTransform content = shape.rectTransform;

        _art = CreateRawImage(content, "Art", ArtPath);
        _art.rectTransform.anchorMin = _art.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        _art.rectTransform.pivot = new Vector2(0.5f, 1f);
        _art.rectTransform.anchoredPosition = Vector2.zero;
        _art.rectTransform.sizeDelta = new Vector2(ArtWidth, ArtHeight);

        BuildFeaturedCards(content);

        RawImage shade = CreateRawImage(content, "Shade", ShadePath);
        shade.color = UiPalette.BgApp;
        RectTransform shadeRect = shade.rectTransform;
        shadeRect.anchorMin = new Vector2(0f, 0f);
        shadeRect.anchorMax = new Vector2(1f, 0f);
        shadeRect.pivot = new Vector2(0.5f, 0f);
        shadeRect.offsetMin = Vector2.zero;
        shadeRect.offsetMax = new Vector2(0f, ShadeHeight);

        _title = CreateBottomText(content, "Title", 62f, UiPalette.Text1, TitleBottom, 76f);
        _title.fontStyle = FontStyles.Bold;
        _subtitle = CreateBottomText(content, "Subtitle", 30f, UiPalette.Text2, SubtitleBottom, 42f);

        _badge = UiBuilder.CreateImage(content, "Badge", Color.white, UiSprites.ButtonPrimary, Image.Type.Sliced);
        PlaceBottomLeft(_badge.rectTransform, TextLeft, BadgeBottom, BadgeWidth, BadgeHeight);
        _badgeText = UiBuilder.CreateText(_badge.transform, "Text", 28f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Stretch(_badgeText.rectTransform, 0f, 0f, 0f, 0f);
        _badgeText.fontStyle = FontStyles.Bold;

        _frame = UiBuilder.CreateImage(Root, "Frame", Color.white, UiSprites.ButtonNormal, Image.Type.Sliced);
        UiBuilder.Stretch(_frame.rectTransform, 0f, 0f, 0f, 0f);

        _previousButton = CreateArrow("PreviousButton", true);
        _nextButton = CreateArrow("NextButton", false);

        _historyButton = UiBuilder.CreateButton(Root, "HistoryButton", "기록", 28f, false, null);
        UiBuilder.PlaceRight(_historyButton.GetComponent<RectTransform>(), 24f, 24f, HistoryWidth, HistoryHeight);
    }

    //양옆을 먼저 만들어 가운데 카드 뒤에 깔리게 한다
    private void BuildFeaturedCards(RectTransform content)
    {
        RectTransform group = UiBuilder.CreateRect(content, "Featured");
        group.anchorMin = group.anchorMax = new Vector2(0.5f, 1f);
        group.pivot = new Vector2(0.5f, 1f);
        group.anchoredPosition = new Vector2(0f, -CardsTop);
        group.sizeDelta = new Vector2(CenterCardWidth, CardHeight(CenterCardWidth));

        _featured[1] = CreateFeaturedCard(group, SideCardWidth, -SideCardOffsetX, SideCardDrop, SideCardTilt);
        _featured[2] = CreateFeaturedCard(group, SideCardWidth, SideCardOffsetX, SideCardDrop, -SideCardTilt);
        _featured[0] = CreateFeaturedCard(group, CenterCardWidth, 0f, 0f, 0f);
    }

    //묶음 위쪽 가운데 기준 (가로 offsetX · 아래로 drop) 에 기울여 세운다. 대표 카드는 누를 수 없다
    private static CardView CreateFeaturedCard(RectTransform group, float width, float offsetX, float drop, float tilt)
    {
        CardView card = CardView.Bake(group, width, false);
        RectTransform rect = card.Root;
        float height = CardHeight(width);

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(offsetX, -drop - height * 0.5f);
        rect.localEulerAngles = new Vector3(0f, 0f, tilt);
        rect.gameObject.SetActive(false);

        return card;
    }

    //배너 양옆 가운데쯤의 넘김 화살표. 왼쪽은 오른쪽 화살표를 뒤집어 쓴다
    private Button CreateArrow(string name, bool left)
    {
        Image arrow = UiBuilder.CreateImage(Root, name, UiPalette.Text1, ArrowSprite);
        RectTransform rect = arrow.rectTransform;

        arrow.preserveAspect = true;
        arrow.raycastTarget = true;
        rect.anchorMin = rect.anchorMax = new Vector2(left ? 0f : 1f, 0.55f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(left ? ArrowInset + ArrowWidth * 0.5f : -ArrowInset - ArrowWidth * 0.5f, 0f);
        rect.sizeDelta = new Vector2(ArrowWidth, ArrowHeight);
        rect.localEulerAngles = new Vector3(0f, 0f, left ? 180f : 0f);

        Button button = arrow.gameObject.AddComponent<Button>();
        button.targetGraphic = arrow;
        button.transition = Selectable.Transition.None;
        arrow.gameObject.AddComponent<UiPressFeedback>();

        return button;
    }

    //텍스처 한 장을 그대로 까는 층
    private static RawImage CreateRawImage(RectTransform parent, string name, string texturePath)
    {
        RectTransform rect = UiBuilder.CreateRect(parent, name);
        RawImage image = rect.gameObject.AddComponent<RawImage>();

        image.texture = Resources.Load<Texture2D>(texturePath);
        image.raycastTarget = false;

        return image;
    }

    //배너 아래쪽 기준 글자 한 줄
    private static TextMeshProUGUI CreateBottomText(RectTransform parent, string name, float fontSize, Color color,
        float bottom, float height)
    {
        TextMeshProUGUI text = UiBuilder.CreateText(parent, name, fontSize, color, TextAlignmentOptions.MidlineLeft);

        PlaceBottomLeft(text.rectTransform, TextLeft, bottom, 900f, height);

        return text;
    }

    //배너 왼쪽 아래 기준 배치
    private static void PlaceBottomLeft(RectTransform rect, float left, float bottom, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(left, bottom);
        rect.sizeDelta = new Vector2(width, height);
    }

    //카드 폭에 맞는 높이
    private static float CardHeight(float width)
    {
        return width * CardView.BaseHeight / CardView.BaseWidth;
    }
}
