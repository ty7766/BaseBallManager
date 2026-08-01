using UnityEngine;

/// <summary>
/// 타석의 총 투구 수 반환
/// </summary>
public class PitchCountCalculator
{
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
        //1. 정규화
        float contact = hitter.Contact / 100f;
        float stuff = pitcher.Stuff / 100f;

        //2. 파울 발생 확률 계산
        float foulChance = Mathf.Clamp(0.28f + 0.40f * (contact - stuff), 0.10f, 0.55f);

        int fouls = 0;
        while (Random.value < foulChance && fouls < 18)
        {
            fouls++;
        }

        return fouls;
    }
}
