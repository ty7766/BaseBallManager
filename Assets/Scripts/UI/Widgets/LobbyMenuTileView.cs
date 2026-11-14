using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 로비 메뉴 칸 하나. 칸보다 큰 아이콘을 왼쪽 끝에 반쯤 걸쳐 흐리게 깔고, 이름은 왼쪽 아래에 둔다 (팀 선택 칸과 같은 짜임).
/// 주 칸(리그 입장)은 흰 채움, 나머지는 어두운 반투명 채움
/// </summary>
public class LobbyMenuTileView : UiWidgetBehaviour
{
    private const string IconResourcePrefix = "Lobby/Icons/icon_";
    //아이콘 크기 = 칸 높이 × 이 배수. 너비 중 칸 안으로 들어오는 비율
    private const float IconScale = 1.3f;
    private const float IconVisibleRatio = 0.6f;
    private const float FillAlpha = 0.8f;
    private const float DisabledAlpha = 0.45f;
    private const float LabelLeft = 28f;
    private const float LabelBottom = 18f;

    //선형 색 공간에서 흰색 저알파는 어두운 바탕 위로 생각보다 밝게 뜬다. 흰 채움 위 어두운 아이콘은 반대로 묻힌다
    private static readonly Color IconTint = new Color(1f, 1f, 1f, 0.035f);
    private static readonly Color PrimaryIconTint = new Color(0.04f, 0.07f, 0.11f, 0.3f);

    [SerializeField] private Button _button;
    [SerializeField] private CanvasGroup _group;

    /// <summary>
    /// 칸의 버튼 (프리팹 인스턴스에서 핸들러를 다시 건다)
    /// </summary>
    public Button Button => _button;

    /// <summary>
    /// 칸을 만든다. 아이콘 크기를 칸 높이로 정하므로 크기를 함께 받고, 위치는 호출부가 정한다
    /// </summary>
    public static LobbyMenuTileView Create(Transform parent, string name, string label, string iconName,
        Vector2 size, float fontSize, bool primary, UnityAction onClick)
    {
        LobbyMenuTileView view = CreateNode<LobbyMenuTileView>(parent, name);
        view.Root.sizeDelta = size;
        view._group = view.gameObject.AddComponent<CanvasGroup>();

        Image shape = CreateShapeMask(view.Root);
        Image fill = UiBuilder.CreateImage(shape.rectTransform, "Fill",
            primary ? UiPalette.ButtonPrimary : UiPalette.WithAlpha(UiPalette.BgApp, FillAlpha));
        UiBuilder.Stretch(fill.rectTransform, 0f, 0f, 0f, 0f);

        CreateIcon(shape.rectTransform, iconName, size.y, primary);

        if (!primary)
        {
            Image frame = UiBuilder.CreateImage(view.Root, "Frame", UiPalette.LineBorder, UiSprites.ButtonNormal, Image.Type.Sliced);
            UiBuilder.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
        }

        TextMeshProUGUI text = UiBuilder.CreateText(view.Root, "Label", fontSize,
            primary ? UiPalette.ButtonPrimaryText : UiPalette.Text1, TextAlignmentOptions.BottomLeft);
        UiBuilder.Stretch(text.rectTransform, LabelLeft, 0f, LabelLeft, LabelBottom);
        text.text = label;

        view._button = view.gameObject.AddComponent<Button>();
        view._button.targetGraphic = fill;
        view._button.transition = Selectable.Transition.None;
        view.gameObject.AddComponent<UiPressFeedback>();

        if (onClick != null)
            view._button.onClick.AddListener(onClick);

        return view;
    }

    /// <summary>
    /// 누를 수 있는지. 못 누르면 칸 전체를 흐리게 한다
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        _button.interactable = interactable;
        _group.alpha = interactable ? 1f : DisabledAlpha;
    }

    //공용 버튼과 같은 모양으로 잘라 채움 · 아이콘이 둥근 모서리 밖으로 새지 않게 한다
    private static Image CreateShapeMask(RectTransform root)
    {
        Image shape = UiBuilder.CreateImage(root, "Shape", Color.white, UiSprites.ButtonPrimary, Image.Type.Sliced);

        UiBuilder.Stretch(shape.rectTransform, 0f, 0f, 0f, 0f);
        shape.raycastTarget = true;
        shape.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        return shape;
    }

    //칸 높이보다 크게, 왼쪽 밖으로 밀어 너비의 IconVisibleRatio 만 보이게 한다
    private static void CreateIcon(RectTransform shape, string iconName, float tileHeight, bool primary)
    {
        Image icon = UiBuilder.CreateImage(shape, "Icon", primary ? PrimaryIconTint : IconTint, IconResourcePrefix + iconName);

        if (icon.sprite == null)
        {
            Debug.LogError($"[LobbyMenuTileView]: 아이콘 '{iconName}' 이 없습니다");
            icon.enabled = false;
            return;
        }

        float size = tileHeight * IconScale;

        icon.preserveAspect = true;
        UiBuilder.Place(icon.rectTransform, -(1f - IconVisibleRatio) * size, (tileHeight - size) * 0.5f, size, size);
    }
}
