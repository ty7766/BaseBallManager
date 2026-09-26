/// <summary>
/// 카드 풀의 평균 스탯 기준값. 확률 공식들이 공유하는 단일 출처다
/// </summary>
public static class StatBaseline
{
    public const float HitterPower = 67f;
    public const float HitterContact = 61f;
    public const float HitterRun = 58f;
    public const float HitterDefense = 66f;

    public const float PitcherPower = 72f;      //(구위 + 구속) / 2
    public const float PitcherStuff = 68f;
    public const float PitcherControl = 66f;

    /// <summary>
    /// 편차 20점 = 1.0. 작을수록 스탯 차이가 결과에 크게 반영된다
    /// </summary>
    public const float StatScale = 20f;

    /// <summary>
    /// 자기 집단 평균 대비 편차. 평균이면 0, 20점 높으면 +1.0
    /// </summary>
    public static float GetEdge(float stat, float mean)
    {
        return (stat - mean) / StatScale;
    }

    /// <summary>
    /// 투수의 구위·구속 평균값
    /// </summary>
    public static float GetPitcherPower(PitcherSnapshot pitcher)
    {
        return (pitcher.Stuff + pitcher.Velo) * 0.5f;
    }
}
