/// <summary>
/// 선수 표기 공용 서식. 연도는 두 자리, 이름은 `이름 '연도` 꼴이다 (명세 1-1 · 6-4)
/// </summary>
public static class PlayerLabelFormat
{
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
}
