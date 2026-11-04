using UnityEngine;

/// <summary>
/// 강화 단계별 육각 메달 (명세 6-6). +1~3 동 · +4~6 은 · +7~9 금 · +10 홀로.
/// 숫자는 금속면에 새긴 듯 금속마다 다른 진한 색으로 쓴다
/// </summary>
public static class EnhanceBadgeStyle
{
    /// <summary>
    /// 이 단계부터 은 · 금 · 홀로 메달
    /// </summary>
    public const int SilverLevel = 4;
    public const int GoldLevel = 7;
    public const int HoloLevel = 10;

    private static readonly Color BronzeInk = Hex("#3A1806");
    private static readonly Color SilverInk = Hex("#1C2430");
    private static readonly Color GoldInk = Hex("#3A2404");
    private static readonly Color HoloInk = Hex("#1A1033");

    /// <summary>
    /// 메달 스프라이트 경로 (Resources/UI 기준)
    /// </summary>
    public static string GetSpritePath(int enhanceLevel)
    {
        if (enhanceLevel >= HoloLevel)
            return "Card/badge_enhance_holo";

        if (enhanceLevel >= GoldLevel)
            return "Card/badge_enhance_gold";

        return enhanceLevel >= SilverLevel ? "Card/badge_enhance_silver" : "Card/badge_enhance_bronze";
    }

    /// <summary>
    /// 메달 위 숫자색
    /// </summary>
    public static Color GetInkColor(int enhanceLevel)
    {
        if (enhanceLevel >= HoloLevel)
            return HoloInk;

        if (enhanceLevel >= GoldLevel)
            return GoldInk;

        return enhanceLevel >= SilverLevel ? SilverInk : BronzeInk;
    }

    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color parsed) ? parsed : Color.white;
    }
}
