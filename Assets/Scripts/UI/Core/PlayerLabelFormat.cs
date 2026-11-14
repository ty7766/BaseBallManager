/// <summary>
/// 선수 표기 공용 서식. 연도는 두 자리, 이름은 `이름 '연도` 꼴이다 (명세 1-1 · 6-4)
/// </summary>
public static class PlayerLabelFormat
{
    private const string SmallYearCloseTag = "</color></size>";

    //연도는 이름보다 작고 옅게, 굵기 없이
    private static readonly string SmallYearOpenTag = $"<size=76%><color=#{UnityEngine.ColorUtility.ToHtmlStringRGB(UiPalette.Text2)}> '";

    /// <summary>
    /// 두 자리 연도 (2024 → 24)
    /// </summary>
    public static string Year(int year)
    {
        return (year % 100).ToString("00");
    }

    /// <summary>
    /// `이름 '연도` 표기
    /// </summary>
    public static string NameWithYear(string name, int year)
    {
        return $"{name} '{Year(year)}";
    }

    /// <summary>
    /// 굵은 이름 + 작고 옅은 `'연도` (TMP 리치 텍스트). 표 · 줄처럼 이름이 주인공인 자리에 쓴다
    /// </summary>
    public static string NameWithSmallYear(string name, int year)
    {
        return "<b>" + name + "</b>" + SmallYearOpenTag + Year(year) + SmallYearCloseTag;
    }
}
