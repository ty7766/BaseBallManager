using UnityEngine;

/// <summary>
/// 타석 1회의 총 투구 수를 산출한다 (기본 투구 수 + 파울)
/// </summary>
public class PitchCountCalculator
{
    private const int StrikeOutPitches = 3;
    private const int WalkPitches = 4;

    //인플레이 타구의 기본 투구 수 누적 분포 (1구 15% / 2구 25% / 3구 30% / 4구 20% / 나머지 5구)
    private const float OnePitchShare = 0.15f;
    private const float TwoPitchShare = 0.40f;
    private const float ThreePitchShare = 0.70f;
    private const float FourPitchShare = 0.90f;

    //리그 평균끼리 붙었을 때의 파울 발생 확률 (타석당 투구 수 ~3.9가 나오는 값)
    private const float BaseFoulChance = 0.48f;

    //스탯 편차 1.0당 파울 확률 변화폭
    private const float FoulChanceCoefficient = 0.10f;
    private const float MinFoulChance = 0.20f;
    private const float MaxFoulChance = 0.70f;

    //한 타석에서 허용하는 최대 파울 수 (무한 루프 방지)
    private const int MaxFouls = 18;

    /// <summary>
    /// 타석 결과와 타자·투수 스탯으로 이번 타석의 총 투구 수를 구한다
    /// </summary>
    public int Calculate(BatterOutcome outcome, HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        return CalcBasePitches(outcome) + CalcFouls(hitter, pitcher);
    }

    //파울을 뺀 기본 투구 수
    private static int CalcBasePitches(BatterOutcome outcome)
    {
        if (outcome == BatterOutcome.StrikeOut)
            return StrikeOutPitches;

        if (outcome == BatterOutcome.Walk)
            return WalkPitches;

        float roll = Random.value;

        if (roll < OnePitchShare)
            return 1;

        if (roll < TwoPitchShare)
            return 2;

        if (roll < ThreePitchShare)
            return 3;

        if (roll < FourPitchShare)
            return 4;

        return 5;
    }

    //정확한 타자일수록 커트가 늘어 투구 수가 올라간다
    private static int CalcFouls(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        float contactEdge = StatBaseline.GetEdge(hitter.Contact, StatBaseline.HitterContact);
        float stuffEdge = StatBaseline.GetEdge(pitcher.Stuff, StatBaseline.PitcherStuff);

        float foulChance = Mathf.Clamp(BaseFoulChance + FoulChanceCoefficient * (contactEdge - stuffEdge),
            MinFoulChance, MaxFoulChance);

        int fouls = 0;

        while (Random.value < foulChance && fouls < MaxFouls)
            fouls++;

        return fouls;
    }
}
