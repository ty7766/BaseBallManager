using UnityEngine;

/// <summary>
/// 타자/투수 스탯을 받아 타석 결과(삼진, 볼넷, 안타, 실책, 아웃 등) 산출
/// </summary>
public class BatterOutcomeCalculator
{
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
            // 파워/주루 기반 장타 비중 보정
            float power = hitter.Power / 100f;
            float run = hitter.Run / 100f;
            float longHitBonus = 0.15f * (power * 0.6f + run * 0.4f);

            float tripleThreshold = 0.04f + longHitBonus * (0.04f / 0.22f);
            float doubleThreshold = tripleThreshold + 0.18f + longHitBonus * (0.18f / 0.22f);

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
        // 1. 스탯 정규화
        float stuff = pitcher.Stuff / 100f;
        float velo = pitcher.Velo / 100f;
        float contact = hitter.Contact / 100f;

        // 2. 투수 구위/구속 평균
        float pitcherPower = (stuff + velo) * 0.5f;

        // 3. 기본 삼진 확률 계산
        float probK = 0.22f + 0.30f * (pitcherPower - contact);

        // 4. (5% ~ 40% 범위 고정)
        return Mathf.Clamp(probK, 0.05f, 0.40f);
    }

    // 볼넷 확률 계산
    // 타자 정확↑, 투수 제구↓ 일수록 높아짐
    private float CalcWalkProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        // 1. 스탯 정규화
        float contact = hitter.Contact / 100f;
        float control = pitcher.Control / 100f;

        // 2. 기본 볼넷 확률 계산
        float probWalk = 0.085f + 0.20f * (contact * 0.4f - control);

        // 3. (2% ~ 20% 범위 고정)
        return Mathf.Clamp(probWalk, 0.02f, 0.20f);
    }

    // 홈런 확률 계산
    // 타자 파워↑, 투수 구위/구속↓ 일수록 높아짐
    private float CalcHomeRunProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        // 1. 스탯 정규화
        float power = hitter.Power / 100f;
        float stuff = pitcher.Stuff / 100f;
        float velo = pitcher.Velo / 100f;

        // 2. 기본 홈런 확률 계산
        float probHomerun = 0.05f + 0.25f * (power - ((stuff + velo) * 0.5f));

        // 3. (0.5% ~ 30% 범위 고정)
        return Mathf.Clamp(probHomerun, 0.005f, 0.30f);
    }

    // 안타 확률 계산 (BABIP 개념)
    // 타자 정확↑, 투수 구위↓ 일수록 높아짐
    private float CalcHitProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        // 1. 스탯 정규화
        float contact = hitter.Contact / 100f;
        float stuff = pitcher.Stuff / 100f;

        // 2. 기본 안타 확률 계산
        float probHit = 0.3f + 0.25f * (contact - stuff);

        // 3. (15% ~ 45% 범위 고정)
        return Mathf.Clamp(probHit, 0.15f, 0.45f);
    }

    // 실책 확률 계산
    // 수비팀 평균 수비↑ 일수록 낮아짐
    private float CalcErrorProb(float avgDefense)
    {
        // 1. 기본 실책 확률 계산
        float probError = 0.012f - 0.02f * (avgDefense - 0.5f);

        // 2. (0.3% ~ 3% 범위 고정)
        return Mathf.Clamp(probError, 0.003f, 0.03f);
    }

    // 타석의 발생 확률 판정
    private bool Roll(float probability)
    {
        float randomValue = Random.value;
        return randomValue < probability;
    }
}