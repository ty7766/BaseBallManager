using UnityEngine;

/// <summary>
/// 카드 셰이더 머티리얼. Resources 의 원본을 스타일마다 한 번만 복제해 색을 넣어 둔다.
/// 같은 스타일 카드끼리 머티리얼을 나눠 써서 카드 수만큼 늘지 않는다
/// </summary>
public static class CardMaterials
{
    private const string MaterialRoot = "UI/Materials/";
    //사진 뒤 그림자 진하기 · 빛 번짐 진하기 (시안 값)
    private const float ShadowAlpha = 0.8f;
    private const float GlowAlpha = 0.32f;

    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int GradeColorId = Shader.PropertyToID("_GradeColor");
    private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int HoloAmountId = Shader.PropertyToID("_HoloAmount");
    private static readonly int PhotoRectId = Shader.PropertyToID("_PhotoRect");

    //사진 뒤 레이어는 카드 전체 칸이다. 그 안에서 사진이 차지하는 uv (아래에서부터)
    private static readonly Vector4 PhotoRectUv = new Vector4(0f,
        (CardView.BaseHeight - CardView.PhotoTop - CardView.PhotoHeight) / CardView.BaseHeight,
        1f, CardView.PhotoHeight / CardView.BaseHeight);

    private static readonly Material[] Photos = new Material[CardStyle.Count];
    private static readonly Material[] PhotoBacks = new Material[CardStyle.Count];
    private static readonly Material[] Holos = new Material[CardStyle.Count];
    private static Material _shine;

    /// <summary>
    /// 광택 띠. 화면 좌표로 계산하므로 모든 카드가 하나를 쓴다
    /// </summary>
    public static Material Shine
    {
        get
        {
            if (_shine == null)
                _shine = Clone("CardShine");

            return _shine;
        }
    }

    /// <summary>
    /// 선수 사진 (가장자리 빛 · 색 물들임)
    /// </summary>
    public static Material GetPhoto(CardStyle style)
    {
        Material material = Photos[style.Index];

        if (material != null)
            return material;

        material = Clone("CardPhoto");

        if (material != null)
        {
            material.SetColor(RimColorId, style.AccentColor);
            material.SetColor(GradeColorId, style.GradeColor);
        }

        Photos[style.Index] = material;

        return material;
    }

    /// <summary>
    /// 사진 뒤 그림자 · 빛 번짐
    /// </summary>
    public static Material GetPhotoBack(CardStyle style)
    {
        Material material = PhotoBacks[style.Index];

        if (material != null)
            return material;

        material = Clone("CardPhotoBack");

        if (material != null)
        {
            material.SetVector(PhotoRectId, PhotoRectUv);
            material.SetColor(ShadowColorId, UiPalette.WithAlpha(style.ShadowColor, ShadowAlpha));
            material.SetColor(GlowColorId, UiPalette.WithAlpha(style.AccentColor, GlowAlpha));
        }

        PhotoBacks[style.Index] = material;

        return material;
    }

    /// <summary>
    /// 배경 홀로 반사. 세기가 0인 스타일은 null (기본 UI 머티리얼로 그린다)
    /// </summary>
    public static Material GetHolo(CardStyle style)
    {
        if (style.HoloAmount <= 0f)
            return null;

        Material material = Holos[style.Index];

        if (material != null)
            return material;

        material = Clone("CardHolo");

        if (material != null)
            material.SetFloat(HoloAmountId, style.HoloAmount);

        Holos[style.Index] = material;

        return material;
    }

    //원본이 없으면 null 을 돌려 기본 UI 머티리얼로 떨어진다 (효과만 빠지고 카드는 그려진다)
    private static Material Clone(string name)
    {
        Material source = Resources.Load<Material>(MaterialRoot + name);

        if (source == null)
        {
            Debug.LogError($"[CardMaterials]: '{MaterialRoot}{name}' 머티리얼이 없습니다");
            return null;
        }

        return new Material(source) { name = name + " (Runtime)" };
    }
}
