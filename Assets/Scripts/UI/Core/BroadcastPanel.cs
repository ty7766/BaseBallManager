using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 리그 화면 공용 중계 그래픽 판. 카드처럼 오른쪽 위 · 왼쪽 아래를 깎은 어두운 판에 팀 색 면 · 반쯤 걸친 구단 엠블럼을 얹는다.
/// 엠블럼 그림은 프리팹에 넣지 않고 런타임에 팀을 정할 때만 불러온다
/// </summary>
public static class BroadcastPanel
{
    private const string GradientMaterialPath = "UI/Materials/GradientDither";
    private const string EmblemResourcePrefix = "Emblems/emblem_";
    private const float TeamFieldAlpha = 0.55f;
    private const float EmblemVisibleRatio = 0.55f;
    private const float EmblemHeightRatio = 1.15f;
    private const float EmblemMaxAspect = 1.45f;
    private const float ButtonFillAlpha = 0.9f;

    //선형 색 공간에서 밝은 저알파는 생각보다 세게 뜬다. 팀 선택 칸과 같은 눌린 회색
    private static readonly Color EmblemTint = new Color(0.4f, 0.4f, 0.4f, 0.32f);

    /// <summary>
    /// 판 한 장. 안쪽 그림이 깎인 모서리 밖으로 새지 않게 하려면 clip 을 켠다
    /// </summary>
    public static Image Create(Transform parent, string name, bool clip = false)
    {
        Image panel = UiBuilder.CreateImage(parent, name, Color.white, UiSprites.CutPanel, Image.Type.Sliced);

        if (clip)
            panel.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        return panel;
    }

    /// <summary>
    /// 판 한쪽 끝에서 가운데로 옅어지는 팀 색 면. 색은 PaintTeamField 가 칠한다
    /// </summary>
    public static Image CreateTeamField(RectTransform panel, string name, bool fromLeft, float widthRatio)
    {
        Image field = UiBuilder.CreateImage(panel, name, Color.clear);

        field.material = Resources.Load<Material>(GradientMaterialPath);
        field.rectTransform.anchorMin = new Vector2(fromLeft ? 0f : 1f - widthRatio, 0f);
        field.rectTransform.anchorMax = new Vector2(fromLeft ? widthRatio : 1f, 1f);
        field.rectTransform.offsetMin = Vector2.zero;
        field.rectTransform.offsetMax = Vector2.zero;

        if (!fromLeft)
            field.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        return field;
    }

    /// <summary>
    /// 팀 색 면을 칠한다. 어두운 팀 색은 판 위에서 보이도록 밝기를 올린 색을 쓴다
    /// </summary>
    public static void PaintTeamField(Image field, string teamName)
    {
        field.color = UiPalette.WithAlpha(UiPalette.GetTeamColorOnDark(teamName), TeamFieldAlpha);
    }

    /// <summary>
    /// 판 가장자리에 반쯤 걸칠 엠블럼 자리. 그림은 PlaceEdgeEmblem 이 넣는다
    /// </summary>
    public static Image CreateEdgeEmblem(RectTransform panel, string name)
    {
        Image emblem = UiBuilder.CreateImage(panel, name, EmblemTint);

        emblem.preserveAspect = true;
        emblem.enabled = false;

        return emblem;
    }

    /// <summary>
    /// 구단 엠블럼을 판 높이보다 크게, 왼쪽(또는 오른쪽) 밖으로 밀어 일부만 보이게 둔다. 엠블럼이 없으면 숨긴다
    /// </summary>
    public static void PlaceEdgeEmblem(Image emblem, string teamName, bool leftEdge)
    {
        Sprite sprite = UiSprites.Get(EmblemResourcePrefix + teamName);

        emblem.sprite = sprite;
        emblem.enabled = sprite != null;

        if (sprite == null)
            return;

        RectTransform panel = (RectTransform)emblem.rectTransform.parent;
        float panelHeight = panel.rect.height > 0f ? panel.rect.height : panel.sizeDelta.y;
        float aspect = sprite.rect.width / sprite.rect.height;
        float height = panelHeight * EmblemHeightRatio;
        float width = Mathf.Min(height * aspect, height * EmblemMaxAspect);

        height = width / aspect;

        RectTransform rect = emblem.rectTransform;

        rect.anchorMin = rect.anchorMax = new Vector2(leftEdge ? 0f : 1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2((leftEdge ? 1f : -1f) * (EmblemVisibleRatio - 0.5f) * width, 0f);
    }

    /// <summary>
    /// 공용 버튼을 판과 같은 깎인 모양으로 바꾼다. 보조 버튼은 어두운 채움 + 옅은 테
    /// </summary>
    public static void ApplyButtonStyle(Button button, bool primary)
    {
        Image background = (Image)button.targetGraphic;

        background.sprite = UiSprites.Get(UiSprites.CutFill);
        background.type = Image.Type.Sliced;
        background.color = primary ? UiPalette.ButtonPrimary : UiPalette.WithAlpha(UiPalette.BgRow, ButtonFillAlpha);

        if (primary)
            return;

        Image frame = UiBuilder.CreateImage(button.transform, "Frame", UiPalette.LineBorder, UiSprites.CutOutline, Image.Type.Sliced);

        UiBuilder.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
        frame.transform.SetSiblingIndex(0);
    }

    /// <summary>
    /// 숫자 전용 글자 (점수 · OVR). 카드 숫자와 같은 이사만루 굵은체
    /// </summary>
    public static TextMeshProUGUI CreateNumber(Transform parent, string name, float fontSize, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        TextMeshProUGUI text = UiBuilder.CreateText(parent, name, fontSize, color, alignment);

        text.font = UiFontProvider.Card;

        return text;
    }
}
