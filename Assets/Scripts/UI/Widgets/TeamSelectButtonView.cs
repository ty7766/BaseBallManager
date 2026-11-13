using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 팀 선택 칸 하나. 왼쪽에서 오른쪽으로 옅어지는 팀 색을 깔고, 엠블럼을 왼쪽 끝에 반쯤 걸쳐 흐리게 둔다
/// </summary>
public class TeamSelectButtonView : UiWidgetBehaviour
{
    private const string EmblemResourcePrefix = "Emblems/emblem_";
    //셰이더가 그라데이션을 픽셀마다 계산하고 디더링한다. 텍스처로 깔면 어두운 꼬리에 세로 띠가 진다
    private const string GradientMaterialPath = "UI/Materials/GradientDither";
    //엠블럼 너비 중 버튼 안으로 들어오는 비율. 반보다 살짝 더 들어온다
    private const float EmblemVisibleRatio = 0.56f;
    private const float EmblemHeightRatio = 0.94f;
    //가로로 긴 엠블럼은 이 가로세로비까지만 넓히고 높이를 줄인다
    private const float EmblemMaxAspect = 1.45f;
    private const float TintAlpha = 0.55f;
    private const float LabelRightPadding = 34f;

    //흰색 저알파는 Linear 블렌딩에서 진한 팀 색에 묻힌다. 어둡게 눌러 얹어야 어디서나 위로 떠 보인다
    private static readonly Color EmblemTint = new Color(0.35f, 0.35f, 0.35f, 0.35f);
    private static readonly Color EmblemSelectedTint = new Color(0.55f, 0.55f, 0.55f, 0.45f);

    [SerializeField] private float _height;
    [SerializeField] private Button _button;
    [SerializeField] private Image _tint;
    [SerializeField] private Image _emblem;
    [SerializeField] private Image _frame;
    [SerializeField] private TextMeshProUGUI _label;

    /// <summary>
    /// 칸을 만든다. 크기는 부모 기준 왼쪽 위 좌표로 Place 해서 쓴다
    /// </summary>
    public static TeamSelectButtonView Create(Transform parent, float height, float fontSize, UnityAction onClick)
    {
        TeamSelectButtonView view = CreateNode<TeamSelectButtonView>(parent, "TeamButton");
        Image shape = CreateButtonShapeMask(view.Root);

        view._height = height;
        view._tint = CreateGradientTint(shape.rectTransform);
        view._emblem = UiBuilder.CreateImage(shape.rectTransform, "TeamEmblem", EmblemTint);

        view._frame = UiBuilder.CreateImage(view.Root, "Frame", UiPalette.BgRow, UiSprites.ButtonNormal, Image.Type.Sliced);
        UiBuilder.Stretch(view._frame.rectTransform, 0f, 0f, 0f, 0f);

        view._label = UiBuilder.CreateText(view.Root, "TeamLabel", fontSize, UiPalette.Text1,
            TextAlignmentOptions.MidlineRight);
        UiBuilder.Stretch(view._label.rectTransform, 0f, 0f, LabelRightPadding, 0f);

        view._button = view.gameObject.AddComponent<Button>();
        view._button.targetGraphic = shape;
        view._button.transition = Selectable.Transition.None;
        view.gameObject.AddComponent<UiPressFeedback>();

        if (onClick != null)
            view._button.onClick.AddListener(onClick);

        return view;
    }

    /// <summary>
    /// 팀 이름 · 팀 색 · 엠블럼을 채운다. 엠블럼 그림이 없으면 색과 이름만 보인다
    /// </summary>
    public void SetTeam(string teamName)
    {
        _label.text = teamName;
        _tint.color = UiPalette.WithAlpha(UiPalette.GetTeamColorOnDark(teamName), TintAlpha);

        Sprite emblem = UiSprites.Get(EmblemResourcePrefix + teamName);

        _emblem.sprite = emblem;
        _emblem.enabled = emblem != null;

        if (emblem != null)
            FitEmblem(emblem);
    }

    /// <summary>
    /// 선택 표시. 흰 테두리를 두르고 엠블럼을 진하게 한다
    /// </summary>
    public void SetSelected(bool selected)
    {
        _frame.color = selected ? UiPalette.ButtonPrimary : UiPalette.BgRow;
        _emblem.color = selected ? EmblemSelectedTint : EmblemTint;
    }

    //버튼과 같은 모양의 채움으로 잘라 팀 색 · 엠블럼이 둥근 모서리 밖으로 새지 않게 한다
    private static Image CreateButtonShapeMask(RectTransform root)
    {
        Image shape = UiBuilder.CreateImage(root, "Shape", Color.white, UiSprites.ButtonPrimary, Image.Type.Sliced);

        UiBuilder.Stretch(shape.rectTransform, 0f, 0f, 0f, 0f);
        shape.raycastTarget = true;
        shape.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        return shape;
    }

    //왼쪽 끝 진하게 → 오른쪽 끝 투명. 색은 SetTeam 이 칠한다 (스프라이트 없이 칸 전체 uv 0~1)
    private static Image CreateGradientTint(RectTransform shape)
    {
        Image tint = UiBuilder.CreateImage(shape, "TeamTint", Color.white);

        tint.material = Resources.Load<Material>(GradientMaterialPath);
        UiBuilder.Stretch(tint.rectTransform, 0f, 0f, 0f, 0f);

        return tint;
    }

    //높이를 칸에 거의 맞추고 왼쪽 밖으로 밀어 너비의 EmblemVisibleRatio 만 보이게 한다
    private void FitEmblem(Sprite emblem)
    {
        float maxHeight = _height * EmblemHeightRatio;
        float aspect = emblem.rect.width / emblem.rect.height;
        float width = Mathf.Min(maxHeight * aspect, maxHeight * EmblemMaxAspect);
        float height = width / aspect;

        UiBuilder.Place(_emblem.rectTransform, -(1f - EmblemVisibleRatio) * width, (_height - height) * 0.5f,
            width, height);
    }
}
