using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 화면 요소를 코드로 조립하는 공용 팩토리.
/// 좌표는 전부 1080×1920 기준 왼쪽 위 원점이라 시안 값을 그대로 옮긴다
/// </summary>
public static class UiBuilder
{
    /// <summary>
    /// 설계 기준 해상도. CanvasScaler의 Reference Resolution과 같아야 한다
    /// </summary>
    public static readonly Vector2 DesignSize = new Vector2(1080f, 1920f);

    /// <summary>
    /// RectTransform만 가진 빈 노드
    /// </summary>
    public static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();

        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        return rect;
    }

    /// <summary>
    /// 왼쪽 위 원점 기준으로 위치·크기를 지정한다 (시안 좌표를 그대로 넣는다)
    /// </summary>
    public static RectTransform Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);

        return rect;
    }

    /// <summary>
    /// 부모를 가득 채우되 네 변에 여백을 둔다. 세로가 긴 기기에서 늘어나는 영역에 쓴다
    /// </summary>
    public static RectTransform Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);

        return rect;
    }

    /// <summary>
    /// 부모 폭에 맞춰 늘어나는 목록 행. y는 위에서부터의 거리다
    /// </summary>
    public static RectTransform PlaceRow(RectTransform rect, float y, float height, float sideMargin = 0f)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(sideMargin, -(y + height));
        rect.offsetMax = new Vector2(-sideMargin, -y);

        return rect;
    }

    /// <summary>
    /// 부모의 오른쪽 위를 기준으로 배치한다 (폭이 가변인 행 안에서 오른쪽 정렬)
    /// </summary>
    public static RectTransform PlaceRight(RectTransform rect, float rightOffset, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-rightOffset, -y);
        rect.sizeDelta = new Vector2(width, height);

        return rect;
    }

    /// <summary>
    /// 단색 또는 스프라이트 이미지. 스프라이트가 없으면 단색 사각형이 된다
    /// </summary>
    public static Image CreateImage(Transform parent, string name, Color color,
        string spritePath = null, Image.Type type = Image.Type.Simple)
    {
        RectTransform rect = CreateRect(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();

        image.color = color;
        image.raycastTarget = false;

        Sprite sprite = UiSprites.Get(spritePath);

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = type;
        }

        return image;
    }

    /// <summary>
    /// 글자 하나. 폰트는 UiFontProvider가 만든 한글 동적 폰트를 쓴다
    /// </summary>
    public static TextMeshProUGUI CreateText(Transform parent, string name, float fontSize,
        Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        RectTransform rect = CreateRect(parent, name);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();

        text.font = UiFontProvider.Korean;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = string.Empty;

        return text;
    }

    /// <summary>
    /// 영문 라벨(AWAY · HOME · FINAL 등). 자간을 벌려 6-1 지침을 맞춘다
    /// </summary>
    public static TextMeshProUGUI CreateLabel(Transform parent, string name, string label,
        float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        TextMeshProUGUI text = CreateText(parent, name, fontSize, color, alignment);

        text.text = label;
        text.characterSpacing = 3.5f;

        return text;
    }

    /// <summary>
    /// 주 버튼(흰 채움 + 남색 글자) 또는 보조 버튼(테두리만). 누르면 살짝 눌리는 반응이 붙는다
    /// </summary>
    public static Button CreateButton(Transform parent, string name, string label,
        float fontSize, bool primary, UnityAction onClick)
    {
        return CreateButton(parent, name, label, fontSize, primary, onClick, out TextMeshProUGUI _);
    }

    /// <summary>
    /// 라벨까지 함께 돌려주는 버튼 (글자를 상태에 따라 바꿔야 할 때)
    /// </summary>
    public static Button CreateButton(Transform parent, string name, string label,
        float fontSize, bool primary, UnityAction onClick, out TextMeshProUGUI labelText)
    {
        RectTransform rect = CreateRect(parent, name);
        Image background = rect.gameObject.AddComponent<Image>();

        background.color = primary ? UiPalette.ButtonPrimary : UiPalette.BgRow;
        background.raycastTarget = true;

        Sprite sprite = UiSprites.Get(primary ? UiSprites.ButtonPrimary : UiSprites.ButtonNormal);

        if (sprite != null)
        {
            background.sprite = sprite;
            background.type = Image.Type.Sliced;
        }

        Button button = rect.gameObject.AddComponent<Button>();

        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        rect.gameObject.AddComponent<UiPressFeedback>();

        if (onClick != null)
            button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(rect, name + "Label", fontSize,
            primary ? UiPalette.ButtonPrimaryText : UiPalette.Text1, TextAlignmentOptions.Center);

        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        text.text = label;
        labelText = text;

        return button;
    }

    /// <summary>
    /// 가로 구분선 1줄
    /// </summary>
    public static Image CreateDivider(Transform parent, float x, float y, float width)
    {
        Image divider = CreateImage(parent, "Divider", UiPalette.LineHair);

        Place(divider.rectTransform, x, y, width, 2f);

        return divider;
    }

    /// <summary>
    /// 세로 스크롤 영역. content의 pivot은 위쪽이라 아래로 쌓아 내려간다
    /// </summary>
    public static ScrollRect CreateVerticalScroll(Transform parent, string name, out RectTransform content)
    {
        RectTransform viewport = CreateRect(parent, name);
        Image mask = viewport.gameObject.AddComponent<Image>();

        mask.color = UiPalette.WithAlpha(UiPalette.BgPanel, 0f);
        mask.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        content = CreateRect(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();

        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        return scroll;
    }

    /// <summary>
    /// 세로 스크롤 영역 오른쪽 바깥(gap 만큼 띄워)에 스크롤바를 붙인다. 내용이 영역보다 짧으면 숨는다
    /// </summary>
    public static Scrollbar AddVerticalScrollbar(ScrollRect scroll, float gap, float width)
    {
        RectTransform viewport = (RectTransform)scroll.transform;
        RectTransform bar = CreateRect(viewport.parent, "Scrollbar");

        bar.anchorMin = new Vector2(1f, 0f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0f, 0.5f);
        bar.offsetMin = new Vector2(gap, viewport.offsetMin.y);
        bar.offsetMax = new Vector2(gap + width, viewport.offsetMax.y);

        Image track = bar.gameObject.AddComponent<Image>();

        track.sprite = UiSprites.Get(UiSprites.ScrollHandle);
        track.type = Image.Type.Sliced;
        track.color = UiPalette.WithAlpha(Color.white, 0.08f);

        RectTransform slidingArea = CreateRect(bar, "SlidingArea");
        Stretch(slidingArea, 0f, 0f, 0f, 0f);

        Image handle = CreateImage(slidingArea, "Handle", UiPalette.WithAlpha(Color.white, 0.5f),
            UiSprites.ScrollHandle, Image.Type.Sliced);
        Stretch(handle.rectTransform, 0f, 0f, 0f, 0f);

        Scrollbar scrollbar = bar.gameObject.AddComponent<Scrollbar>();

        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.transition = Selectable.Transition.None;

        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        return scrollbar;
    }

    /// <summary>
    /// 가로·세로 둘 다 스크롤되는 영역 (박스스코어 표)
    /// </summary>
    public static ScrollRect CreateTableScroll(Transform parent, string name, out RectTransform content)
    {
        ScrollRect scroll = CreateVerticalScroll(parent, name, out content);

        scroll.horizontal = true;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);

        return scroll;
    }
}
