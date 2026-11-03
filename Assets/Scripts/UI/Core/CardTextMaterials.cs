using TMPro;
using UnityEngine;

/// <summary>
/// 카드 앞면 글자용 TMP 머티리얼. 카드 폰트의 기본 머티리얼에 밑그림자(Underlay)만 켠 세 벌이다.
/// 글자마다 복제하지 않고 세 벌을 나눠 써서 같은 폰트 글자끼리 한 번에 그려진다
/// </summary>
public static class CardTextMaterials
{
    private const string UnderlayKeyword = "UNDERLAY_ON";

    private static readonly int UnderlayColorId = Shader.PropertyToID("_UnderlayColor");
    private static readonly int UnderlayOffsetXId = Shader.PropertyToID("_UnderlayOffsetX");
    private static readonly int UnderlayOffsetYId = Shader.PropertyToID("_UnderlayOffsetY");
    private static readonly int UnderlayDilateId = Shader.PropertyToID("_UnderlayDilate");
    private static readonly int UnderlaySoftnessId = Shader.PropertyToID("_UnderlaySoftness");

    private static Material _shadow;
    private static Material _lightShadow;
    private static Material _emboss;

    /// <summary>
    /// 어두운 배경 위 흰 글자 — 아래로 번지는 검은 그림자
    /// </summary>
    public static Material Shadow
    {
        get
        {
            if (_shadow == null)
                _shadow = Create("Shadow", new Color(0f, 0f, 0f, 0.6f), -0.45f, 0.2f, 0.55f);

            return _shadow;
        }
    }

    /// <summary>
    /// 밝은 배경(시그·골글) 위 진한 글자 — 아래쪽 흰 반사
    /// </summary>
    public static Material LightShadow
    {
        get
        {
            if (_lightShadow == null)
                _lightShadow = Create("LightShadow", new Color(1f, 1f, 1f, 0.45f), -0.4f, 0.05f, 0.15f);

            return _lightShadow;
        }
    }

    /// <summary>
    /// 강화 메달 숫자 — 금속에 새긴 듯 아래로 또렷한 흰 선
    /// </summary>
    public static Material Emboss
    {
        get
        {
            if (_emboss == null)
                _emboss = Create("Emboss", new Color(1f, 1f, 1f, 0.5f), -0.35f, 0f, 0f);

            return _emboss;
        }
    }

    //폰트가 바뀌면(카드 폰트가 없어 본문 폰트로 떨어진 경우 포함) 그 폰트의 아틀라스를 그대로 쓴다
    private static Material Create(string name, Color color, float offsetY, float dilate, float softness)
    {
        TMP_FontAsset font = UiFontProvider.Card;
        Material material = new Material(font.material) { name = $"{font.name} {name}" };

        material.EnableKeyword(UnderlayKeyword);
        material.SetColor(UnderlayColorId, color);
        material.SetFloat(UnderlayOffsetXId, 0f);
        material.SetFloat(UnderlayOffsetYId, offsetY);
        material.SetFloat(UnderlayDilateId, dilate);
        material.SetFloat(UnderlaySoftnessId, softness);

        return material;
    }
}
