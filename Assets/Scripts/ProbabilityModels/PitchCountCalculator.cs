using UnityEngine;

/// <summary>
/// 타석의 총 투구 수 반환
/// </summary>
public class PitchCountCalculator
{
    //리그 평균끼리 붙었을 때의 파울 발생 확률 (타석당 투구 수 ~3.9가 나오는 값)
    private const float BaseFoulChance = 0.48f;

    //스탯 편차 1.0당 파울 확률 변화폭
    private const float FoulChanceCoefficient = 0.10f;

    //한 타석에서 허용하는 최대 파울 수 (무한 루프 방지)
    private const int MaxFouls = 18;

    //타석 당 투구 수 반환
    public int Calculate(BatterOutcome outcome, HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        return CalcBasePitches(outcome) + CalcFouls(hitter, pitcher); 
    }

    //기본 투구 수 반환
    private int CalcBasePitches(BatterOutcome outcome)
    {
        //삼진
        if (outcome == BatterOutcome.StrikeOut)
        {
            return 3;
        }
        //볼넷
        if (outcome == BatterOutcome.Walk)
        {
            return 4;
        }

        //그 외 인플레이
        float r = Random.value;
        if (r < 0.15f)
            return 1;
        else if (r < 0.40f)
            return 2;
        else if (r < 0.70f)
            return 3;
        else if (r < 0.90f)
            return 4;
        else 
            return 5;
    }

    //파울 개수 반환
    private int CalcFouls(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        //1. 평균 대비 편차로 변환 (타자·투수의 스탯 평균이 다르므로 직접 빼면 한쪽으로 기욺)
        float contactEdge = StatBaseline.GetEdge(hitter.Contact, StatBaseline.HitterContact);
        float stuffEdge = StatBaseline.GetEdge(pitcher.Stuff, StatBaseline.PitcherStuff);

        //2. 파울 발생 확률 계산 - 정확한 타자일수록 커트가 많아 투구 수가 늘어남
        float foulChance = Mathf.Clamp(BaseFoulChance + FoulChanceCoefficient * (contactEdge - stuffEdge), 0.20f, 0.70f);

        int fouls = 0;
        while (Random.value < foulChance && fouls < MaxFouls)
        {
            fouls++;
        }

        return fouls;
    }
}
