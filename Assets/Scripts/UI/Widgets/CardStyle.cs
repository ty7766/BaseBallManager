using UnityEngine;

/// <summary>
/// 카드 종류·등급별 겉모습 (명세 6-6). 배경·전경 그림과 사진 빛·광택 세기를 한곳에 둔다.
/// 색은 Design/ui_assets/render_card_art.py 의 팔레트와 같은 값이다
/// </summary>
public sealed class CardStyle
{
    /// <summary>
    /// 스타일 개수. 머티리얼 캐시 크기로 쓴다
    /// </summary>
    public const int Count = 5;

    /// <summary>
    /// 0 ~ Count-1 고유 번호 (머티리얼 캐시 칸)
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 사진 뒤 배경 (불투명) · 사진 앞 하단 물감 띠 · 배경 위 장식(골글 월계수, 없으면 null)
    /// </summary>
    public string BaseSprite { get; }
    public string ForegroundSprite { get; }
    public string OverlaySprite { get; }

    /// <summary>
    /// 사진 가장자리 빛 · 빛 번짐 색 (배경의 밝은 강조색)
    /// </summary>
    public Color AccentColor { get; }

    /// <summary>
    /// 사진 뒤 그림자 색 (배경의 가장 진한 색)
    /// </summary>
    public Color ShadowColor { get; }

    /// <summary>
    /// 사진을 배경 쪽으로 물들이는 색
    /// </summary>
    public Color GradeColor { get; }

    /// <summary>
    /// OVR · 포지션 글자색. 밝은 배경(시그·골글)은 진한 색으로 새긴다
    /// </summary>
    public Color OvrColor { get; }
    public bool IsLightBase { get; }

    /// <summary>
    /// 배경 홀로 반사 세기 (0이면 끔) · 광택 띠 세기. 광택은 리니어 색공간에서 더해져 시안 값의 0.4배로 둔다
    /// </summary>
    public float HoloAmount { get; }
    public float ShineStrength { get; }

    private static readonly CardStyle Normal3 = new CardStyle(0, "Card/card_base_normal_3", "Card/card_fg_normal_3", null,
        Hex("#46D9FF"), Hex("#030921"), Hex("#2A6FE0"), Color.white, false, 0f, 0.12f);

    private static readonly CardStyle Normal4 = new CardStyle(1, "Card/card_base_normal_4", "Card/card_fg_normal_4", null,
        Hex("#B6EC5C"), Hex("#011410"), Hex("#16A07A"), Color.white, false, 0.22f, 0.14f);

    private static readonly CardStyle Normal5 = new CardStyle(2, "Card/card_base_normal_5", "Card/card_fg_normal_5", null,
        Hex("#38E8E0"), Hex("#0A0322"), Hex("#6A3CE6"), Color.white, false, 0.38f, 0.17f);

    private static readonly CardStyle Signature = new CardStyle(3, "Card/card_base_signature", "Card/card_fg_signature", null,
        Hex("#FFD3E3"), Hex("#2E0418"), Hex("#E05A8C"), Hex("#4A0826"), true, 0.2f, 0.16f);

    private static readonly CardStyle GoldenGlove = new CardStyle(4, "Card/card_base_goldenglove", "Card/card_fg_goldenglove",
        "Card/card_ov_goldenglove", Hex("#FFE9A8"), Hex("#221503"), Hex("#C8962E"), Hex("#3A2606"), true, 0.2f, 0.2f);

    private CardStyle(int index, string baseSprite, string foregroundSprite, string overlaySprite,
        Color accentColor, Color shadowColor, Color gradeColor, Color ovrColor, bool isLightBase,
        float holoAmount, float shineStrength)
    {
        Index = index;
        BaseSprite = baseSprite;
        ForegroundSprite = foregroundSprite;
        OverlaySprite = overlaySprite;
        AccentColor = accentColor;
        ShadowColor = shadowColor;
        GradeColor = gradeColor;
        OvrColor = ovrColor;
        IsLightBase = isLightBase;
        HoloAmount = holoAmount;
        ShineStrength = shineStrength;
    }

    /// <summary>
    /// 카드 한 장의 스타일. 시그·골글은 등급과 상관없이 하나, 노말은 등급마다 다르다
    /// </summary>
    public static CardStyle Get(CardType cardType, CardGrade cardGrade)
    {
        return cardType switch
        {
            CardType.Signature => Signature,
            CardType.GoldenGlove => GoldenGlove,
            _ => cardGrade switch
            {
                CardGrade.Star5 => Normal5,
                CardGrade.Star4 => Normal4,
                _ => Normal3
            }
        };
    }

    /// <summary>
    /// 별 색 (3성 파랑 · 4성 초록 · 5성 보라)
    /// </summary>
    public static Color GetStarColor(CardGrade cardGrade)
    {
        return cardGrade switch
        {
            CardGrade.Star5 => UiPalette.CardStar5,
            CardGrade.Star4 => UiPalette.CardStar4,
            _ => UiPalette.CardStar3
        };
    }

    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color parsed) ? parsed : Color.white;
    }
}
