using UnityEngine;

/// <summary>
/// 강화 단계별 배지 색 (명세 6-6). 바탕은 단계색, 테두리는 3단(흰·은·금)
/// </summary>
public static class EnhanceBadgeColors
{
    /// <summary>
    /// 이 단계부터는 홀로 바탕을 쓴다
    /// </summary>
    public const int HoloLevel = 10;

    private static readonly Color[] FillColors =
    {
        Hex("#5F6A7A"), //+1
        Hex("#3C8B6E"), //+2
        Hex("#3D6FB0"), //+3
        Hex("#6B55B0"), //+4
        Hex("#9A4A8E"), //+5
        Hex("#B0703A"), //+6
        Hex("#B04A42"), //+7
        Hex("#8E2B3A"), //+8
        Hex("#4A1E2C")  //+9
    };

    private static readonly Color RimWhite = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color RimSilver = Hex("#C9D2DD");
    private static readonly Color RimGold = Hex("#D9B45A");

    /// <summary>
    /// 배지 바탕색. +10은 홀로 애셋을 쓰므로 흰색으로 돌려준다
    /// </summary>
    public static Color GetFillColor(int enhanceLevel)
    {
        if (enhanceLevel >= HoloLevel)
            return Color.white;

        int index = Mathf.Clamp(enhanceLevel, 1, FillColors.Length) - 1;

        return FillColors[index];
    }

    /// <summary>
    /// 배지 테두리색. +1~3 흰 · +4~6 은 · +7~9 금 · +10 흰
    /// </summary>
    public static Color GetRimColor(int enhanceLevel)
    {
        if (enhanceLevel >= HoloLevel)
            return Color.white;

        if (enhanceLevel >= 7)
            return RimGold;

        return enhanceLevel >= 4 ? RimSilver : RimWhite;
    }

    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color parsed) ? parsed : Color.white;
    }
}
