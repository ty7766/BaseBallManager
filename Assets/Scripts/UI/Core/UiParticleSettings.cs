using UnityEngine;

/// <summary>
/// 파티클 1회 분사의 설정. 종류별 프리셋을 여기서 고른다.
/// 스프라이트는 Resources/UI/Fx 아래 PNG를 쓰고, 없으면 단색 사각형으로 떨어진다
/// </summary>
public readonly struct UiParticleSettings
{
    /// <summary>
    /// 외부 파티클 애셋을 떨어뜨리는 폴더 (Resources/UI/Fx)
    /// </summary>
    public const string SpriteRoot = "Fx/";

    public int Count { get; }
    public string SpritePath { get; }
    public float MinSpeed { get; }
    public float MaxSpeed { get; }
    public float Gravity { get; }
    public float Lifetime { get; }
    public float MinSize { get; }
    public float MaxSize { get; }
    public float SpreadDegrees { get; }
    public float UpwardBias { get; }
    public bool Spin { get; }

    private readonly Color _colorA;
    private readonly Color _colorB;

    public UiParticleSettings(int count, string spritePath, Color colorA, Color colorB,
        float minSpeed, float maxSpeed, float gravity, float lifetime,
        float minSize, float maxSize, float spreadDegrees, float upwardBias, bool spin)
    {
        Count = count;
        SpritePath = spritePath;
        _colorA = colorA;
        _colorB = colorB;
        MinSpeed = minSpeed;
        MaxSpeed = maxSpeed;
        Gravity = gravity;
        Lifetime = lifetime;
        MinSize = minSize;
        MaxSize = maxSize;
        SpreadDegrees = spreadDegrees;
        UpwardBias = upwardBias;
        Spin = spin;
    }

    /// <summary>
    /// 두 색 사이에서 하나를 고른다
    /// </summary>
    public Color PickColor(float amount)
    {
        return new Color(
            _colorA.r + (_colorB.r - _colorA.r) * amount,
            _colorA.g + (_colorB.g - _colorA.g) * amount,
            _colorA.b + (_colorB.b - _colorA.b) * amount,
            1f);
    }

    /// <summary>
    /// 연출 종류별 프리셋
    /// </summary>
    public static UiParticleSettings Get(UiEffectKind kind)
    {
        return kind switch
        {
            UiEffectKind.Score => new UiParticleSettings(14, SpriteRoot + "spark",
                UiPalette.AccentGold, UiPalette.Text1,
                260f, 520f, 1400f, 0.7f, 10f, 20f, 120f, 1f, false),

            UiEffectKind.HomeRun => new UiParticleSettings(40, SpriteRoot + "confetti",
                UiPalette.AccentGold, UiPalette.LogHomeRun,
                420f, 980f, 1700f, 1.25f, 14f, 30f, 180f, 1f, true),

            UiEffectKind.GradeUp => new UiParticleSettings(32, SpriteRoot + "star",
                UiPalette.CardSignature, UiPalette.AccentGold,
                340f, 760f, 900f, 1.1f, 18f, 36f, 360f, 0.2f, true),

            UiEffectKind.Enhance => new UiParticleSettings(24, SpriteRoot + "spark",
                UiPalette.StateReserved, UiPalette.Text1,
                300f, 640f, 700f, 0.85f, 12f, 24f, 360f, 0.3f, false),

            UiEffectKind.Craft => new UiParticleSettings(36, SpriteRoot + "star",
                UiPalette.CardGoldenGlove, UiPalette.AccentGold,
                360f, 820f, 820f, 1.2f, 16f, 34f, 360f, 0.2f, true),

            UiEffectKind.Reveal => new UiParticleSettings(12, SpriteRoot + "spark",
                UiPalette.Text1, UiPalette.CardNormal,
                200f, 420f, 600f, 0.55f, 8f, 18f, 360f, 0.1f, false),

            UiEffectKind.RevealRare => new UiParticleSettings(24, SpriteRoot + "star",
                UiPalette.CardStar5, UiPalette.Text1,
                320f, 700f, 700f, 0.9f, 14f, 28f, 360f, 0.3f, true),

            UiEffectKind.RevealSignature => new UiParticleSettings(40, SpriteRoot + "star",
                UiPalette.CardSignature, UiPalette.Text1,
                380f, 880f, 760f, 1.15f, 16f, 34f, 360f, 0.3f, true),

            UiEffectKind.RevealGoldenGlove => new UiParticleSettings(40, SpriteRoot + "star",
                UiPalette.CardGoldenGlove, UiPalette.AccentGold,
                380f, 880f, 760f, 1.15f, 16f, 34f, 360f, 0.3f, true),

            UiEffectKind.Champion => new UiParticleSettings(64, SpriteRoot + "confetti",
                UiPalette.AccentGold, UiPalette.CardSignature,
                480f, 1150f, 1500f, 1.8f, 16f, 34f, 200f, 1f, true),

            _ => new UiParticleSettings(10, SpriteRoot + "spark",
                UiPalette.StatePitching, UiPalette.Text1,
                220f, 440f, 900f, 0.6f, 9f, 16f, 140f, 1f, false)
        };
    }
}
