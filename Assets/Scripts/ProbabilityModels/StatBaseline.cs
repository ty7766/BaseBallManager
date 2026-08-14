/// <summary>
/// 카드 풀의 평균 스탯 기준값 - 확률 공식들이 공유하는 단일 출처
/// </summary>
/// <remarks>
/// 타자와 투수는 스탯 평균이 서로 다르다(예: 타자 정확 61 vs 투수 구위·구속 72).
/// 두 스탯을 그대로 빼면 평균끼리의 대결에서도 한쪽으로 기울기 때문에,
/// 각자 자기 집단 평균 대비 편차로 바꾼 뒤 비교한다.
/// CSV를 대규모로 개편하면 이 값들을 다시 측정해야 한다.
/// </remarks>
public static class StatBaseline
{
    public const float HitterPower = 67f;
    public const float HitterContact = 61f;
    public const float HitterRun = 58f;
    public const float HitterDefense = 66f;

    public const float PitcherPower = 72f;      //(구위 + 구속) / 2
    public const float PitcherStuff = 68f;
    public const float PitcherControl = 66f;

    //편차 20점 = 1.0. 작을수록 스탯 차이가 결과에 크게 반영된다
    public const float StatScale = 20f;

    //평균 대비 편차 (평균이면 0, 20점 높으면 +1.0)
    public static float GetEdge(float stat, float mean)
    {
        return (stat - mean) / StatScale;
    }

    //투수의 구위·구속 평균값
    public static float GetPitcherPower(PitcherSnapshot pitcher)
    {
        return (pitcher.Stuff + pitcher.Velo) * 0.5f;
    }
}
