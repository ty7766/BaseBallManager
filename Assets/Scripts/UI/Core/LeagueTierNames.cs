/// <summary>
/// 티어 enum을 화면 표기로 바꾼다 (Basic1 → 베이직 1)
/// </summary>
public static class LeagueTierNames
{
    private static readonly string[] GroupNames =
    {
        "베이직", "루키", "아마추어", "프로", "마이너", "메이저", "레전드"
    };

    private const int TiersPerGroup = 3;

    /// <summary>
    /// "베이직 1" 꼴의 표기. 범위를 벗어나면 enum 이름을 그대로 돌려준다
    /// </summary>
    public static string Get(LeagueTier tier)
    {
        int index = (int)tier;
        int group = index / TiersPerGroup;

        if (group < 0 || group >= GroupNames.Length)
            return tier.ToString();

        return $"{GroupNames[group]} {index % TiersPerGroup + 1}";
    }
}
