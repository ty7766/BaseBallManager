using UnityEngine;

/// <summary>
/// 타자/투수 스탯을 받아 타석 결과(삼진, 볼넷, 안타, 실책, 아웃 등) 산출
/// </summary>
public class BatterOutcomeCalculator
{
    //리그 평균끼리 붙었을 때 나오는 기준 확률 (KBO 실제 지표에 맞춰 실측 조정한 값)
    private const float BaseStrikeOutProb = 0.190f;     //삼진율 ~17.5%
    private const float BaseWalkProb = 0.098f;          //볼넷율 ~9.3% (삼진 탈락 후 기준)
    private const float BaseHomeRunProb = 0.023f;       //홈런율 ~2.0% (삼진·볼넷 탈락 후 기준)
    private const float BaseErrorProb = 0.013f;
    private const float BaseHitProb = 0.303f;           //타율 ~.270이 나오는 인플레이 안타 확률

    //스탯 편차 1.0당 확률 변화폭
    private const float StrikeOutCoefficient = 0.10f;
    private const float WalkCoefficient = 0.06f;
    private const float HomeRunCoefficient = 0.025f;
    private const float HitCoefficient = 0.10f;
    private const float ErrorDefenseCoefficient = 0.006f;

    //안타 종류 분배 (홈런 제외) - KBO 기준 단타 79% / 2루타 19% / 3루타 1.5%
    private const float BaseTripleShare = 0.015f;
    private const float BaseDoubleShare = 0.190f;
    private const float LongHitBonusScale = 0.10f;

    // 확률 메서드를 조합하여 최종 타석 결과 반환
    public BatterOutcome Calculate(GameState state, HitterSnapshot hitter, PitcherSnapshot pitcher, float avgDefense)
    {
        // 1. 삼진
        if (Roll(CalcStrikeOutProb(hitter, pitcher)))
            return BatterOutcome.StrikeOut;
        // 2. 볼넷
        if (Roll(CalcWalkProb(hitter, pitcher)))
            return BatterOutcome.Walk;
        // 3. 홈런
        if (Roll(CalcHomeRunProb(hitter, pitcher)))
            return BatterOutcome.HomeRun;
        // 4. 실책
        if (Roll(CalcErrorProb(avgDefense)))
            return BatterOutcome.Error;
        // 5. 안타
        if (Roll(CalcHitProb(hitter, pitcher)))
        {
            // 파워/주루가 평균보다 높을수록 장타 비중 증가 (평균이면 보정 0)
            float longHitBonus = LongHitBonusScale
                * (StatBaseline.GetEdge(hitter.Power, StatBaseline.HitterPower) * 0.6f + StatBaseline.GetEdge(hitter.Run, StatBaseline.HitterRun) * 0.4f);

            float tripleThreshold = Mathf.Max(0f, BaseTripleShare + longHitBonus * 0.1f);
            float doubleThreshold = tripleThreshold + Mathf.Max(0f, BaseDoubleShare + longHitBonus * 0.9f);

            float r = Random.value;
            if (r < tripleThreshold)
                return BatterOutcome.Triple;
            else if (r < doubleThreshold)
                return BatterOutcome.Double;
            else
                return BatterOutcome.Single;
        }
        // 6. 아웃 종류
        else
        {
            bool canDoublePlay = state.FirstBase != -1 && state.OutCount < 2;
            float r = Random.value;

            if (canDoublePlay)
            {
                if (r < 0.45f)
                {
                    // 희생플라이: 뜬공 + 3루 주자 있음 + 2아웃 미만
                    if (state.ThirdBase != -1)
                        return BatterOutcome.SacrificeFly;
                    return BatterOutcome.FlyOut;
                }
                else if (r < 0.9f)
                    return BatterOutcome.GroundOut;
                else
                    return BatterOutcome.DoublePlay;
            }
            else
            {
                if (r < 0.5f)
                {
                    // 희생플라이: 뜬공 + 3루 주자 있음 + 2아웃 미만
                    if (state.ThirdBase != -1 && state.OutCount < 2)
                        return BatterOutcome.SacrificeFly;
                    return BatterOutcome.FlyOut;
                }
                return BatterOutcome.GroundOut;
            }
        }
    }

    // 삼진 확률 계산
    // 타자 정확↓, 투수 구위/구속↑ 일수록 높아짐
    private float CalcStrikeOutProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        float pitcherPowerEdge = StatBaseline.GetEdge(StatBaseline.GetPitcherPower(pitcher), StatBaseline.PitcherPower);
        float contactEdge = StatBaseline.GetEdge(hitter.Contact, StatBaseline.HitterContact);

        float probStrikeOut = BaseStrikeOutProb + StrikeOutCoefficient * (pitcherPowerEdge - contactEdge);

        return Mathf.Clamp(probStrikeOut, 0.05f, 0.40f);
    }

    // 볼넷 확률 계산
    // 타자 정확(선구안)↑, 투수 제구↓ 일수록 높아짐
    private float CalcWalkProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        float contactEdge = StatBaseline.GetEdge(hitter.Contact, StatBaseline.HitterContact);
        float controlEdge = StatBaseline.GetEdge(pitcher.Control, StatBaseline.PitcherControl);

        float probWalk = BaseWalkProb + WalkCoefficient * (contactEdge - controlEdge);

        return Mathf.Clamp(probWalk, 0.02f, 0.25f);
    }

    // 홈런 확률 계산
    // 타자 파워↑, 투수 구위/구속↓ 일수록 높아짐
    private float CalcHomeRunProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        float powerEdge = StatBaseline.GetEdge(hitter.Power, StatBaseline.HitterPower);
        float pitcherPowerEdge = StatBaseline.GetEdge(StatBaseline.GetPitcherPower(pitcher), StatBaseline.PitcherPower);

        float probHomeRun = BaseHomeRunProb + HomeRunCoefficient * (powerEdge - pitcherPowerEdge);

        return Mathf.Clamp(probHomeRun, 0.002f, 0.10f);
    }

    // 안타 확률 계산 (BABIP 개념)
    // 타자 정확↑, 투수 구위↓ 일수록 높아짐
    private float CalcHitProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        float contactEdge = StatBaseline.GetEdge(hitter.Contact, StatBaseline.HitterContact);
        float stuffEdge = StatBaseline.GetEdge(pitcher.Stuff, StatBaseline.PitcherStuff);

        float probHit = BaseHitProb + HitCoefficient * (contactEdge - stuffEdge);

        return Mathf.Clamp(probHit, 0.12f, 0.50f);
    }

    // 실책 확률 계산
    // 수비팀 평균 수비↑ 일수록 낮아짐
    private float CalcErrorProb(float avgDefense)
    {
        //avgDefense는 0~1로 정규화된 값이라 100을 곱해 원래 스탯 단위로 되돌림
        float defenseEdge = StatBaseline.GetEdge(avgDefense * 100f, StatBaseline.HitterDefense);

        float probError = BaseErrorProb - ErrorDefenseCoefficient * defenseEdge;

        return Mathf.Clamp(probError, 0.003f, 0.03f);
    }

    // 타석의 발생 확률 판정
    private bool Roll(float probability)
    {
        float randomValue = Random.value;
        return randomValue < probability;
    }
}
