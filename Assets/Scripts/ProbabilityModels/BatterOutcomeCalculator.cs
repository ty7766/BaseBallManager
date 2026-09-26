using UnityEngine;

/// <summary>
/// 타자·투수 스탯으로 타석 결과(삼진·볼넷·안타·실책·아웃)를 판정한다
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

    private const float MinStrikeOutProb = 0.05f;
    private const float MaxStrikeOutProb = 0.40f;
    private const float MinWalkProb = 0.02f;
    private const float MaxWalkProb = 0.25f;
    private const float MinHomeRunProb = 0.002f;
    private const float MaxHomeRunProb = 0.10f;
    private const float MinHitProb = 0.12f;
    private const float MaxHitProb = 0.50f;
    private const float MinErrorProb = 0.003f;
    private const float MaxErrorProb = 0.03f;

    //안타 종류 분배 (홈런 제외) - KBO 기준 단타 79% / 2루타 19% / 3루타 1.5%
    private const float BaseTripleShare = 0.015f;
    private const float BaseDoubleShare = 0.190f;
    private const float LongHitBonusScale = 0.10f;
    private const float TripleBonusShare = 0.1f;
    private const float DoubleBonusShare = 0.9f;
    private const float PowerWeightInLongHit = 0.6f;
    private const float RunWeightInLongHit = 0.4f;

    //아웃 종류 분배. 병살이 가능한 상황에서는 땅볼 비중이 올라간다
    private const float FlyOutShareWithDoublePlay = 0.45f;
    private const float GroundOutShareWithDoublePlay = 0.9f;
    private const float FlyOutShare = 0.5f;

    /// <summary>
    /// 삼진 -> 볼넷 -> 홈런 -> 실책 -> 안타 순으로 굴려 타석 결과를 정한다
    /// </summary>
    public BatterOutcome Calculate(GameState state, HitterSnapshot hitter, PitcherSnapshot pitcher, float avgDefense)
    {
        if (Roll(CalcStrikeOutProb(hitter, pitcher)))
            return BatterOutcome.StrikeOut;

        if (Roll(CalcWalkProb(hitter, pitcher)))
            return BatterOutcome.Walk;

        if (Roll(CalcHomeRunProb(hitter, pitcher)))
            return BatterOutcome.HomeRun;

        if (Roll(CalcErrorProb(avgDefense)))
            return BatterOutcome.Error;

        if (Roll(CalcHitProb(hitter, pitcher)))
            return ResolveHitType(hitter);

        return ResolveOutType(state);
    }

    //파워·주루가 평균보다 높을수록 장타 비중이 올라간다
    private static BatterOutcome ResolveHitType(HitterSnapshot hitter)
    {
        float longHitBonus = LongHitBonusScale
            * (StatBaseline.GetEdge(hitter.Power, StatBaseline.HitterPower) * PowerWeightInLongHit
                + StatBaseline.GetEdge(hitter.Run, StatBaseline.HitterRun) * RunWeightInLongHit);

        float tripleThreshold = Mathf.Max(0f, BaseTripleShare + longHitBonus * TripleBonusShare);
        float doubleThreshold = tripleThreshold + Mathf.Max(0f, BaseDoubleShare + longHitBonus * DoubleBonusShare);

        float roll = Random.value;

        if (roll < tripleThreshold)
            return BatterOutcome.Triple;

        if (roll < doubleThreshold)
            return BatterOutcome.Double;

        return BatterOutcome.Single;
    }

    //희생플라이는 뜬공 + 3루 주자 + 2아웃 미만일 때만 성립한다
    private static BatterOutcome ResolveOutType(GameState state)
    {
        bool canDoublePlay = state.FirstBase != GameState.NoRunner
            && state.OutCount < GameState.OutsPerInning - 1;

        float roll = Random.value;

        if (canDoublePlay)
        {
            if (roll < FlyOutShareWithDoublePlay)
                return ResolveFlyBall(state);

            if (roll < GroundOutShareWithDoublePlay)
                return BatterOutcome.GroundOut;

            return BatterOutcome.DoublePlay;
        }

        if (roll < FlyOutShare)
            return ResolveFlyBall(state);

        return BatterOutcome.GroundOut;
    }

    //뜬공이 희생플라이가 되는지 판정
    private static BatterOutcome ResolveFlyBall(GameState state)
    {
        if (state.ThirdBase != GameState.NoRunner && state.OutCount < GameState.OutsPerInning - 1)
            return BatterOutcome.SacrificeFly;

        return BatterOutcome.FlyOut;
    }

    //타자 정확이 낮고 투수 구위·구속이 높을수록 올라간다
    private static float CalcStrikeOutProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        float pitcherPowerEdge = StatBaseline.GetEdge(StatBaseline.GetPitcherPower(pitcher), StatBaseline.PitcherPower);
        float contactEdge = StatBaseline.GetEdge(hitter.Contact, StatBaseline.HitterContact);

        return Mathf.Clamp(BaseStrikeOutProb + StrikeOutCoefficient * (pitcherPowerEdge - contactEdge),
            MinStrikeOutProb, MaxStrikeOutProb);
    }

    //타자 선구안이 좋고 투수 제구가 나쁠수록 올라간다
    private static float CalcWalkProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        float contactEdge = StatBaseline.GetEdge(hitter.Contact, StatBaseline.HitterContact);
        float controlEdge = StatBaseline.GetEdge(pitcher.Control, StatBaseline.PitcherControl);

        return Mathf.Clamp(BaseWalkProb + WalkCoefficient * (contactEdge - controlEdge),
            MinWalkProb, MaxWalkProb);
    }

    //타자 파워가 높고 투수 구위·구속이 낮을수록 올라간다
    private static float CalcHomeRunProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        float powerEdge = StatBaseline.GetEdge(hitter.Power, StatBaseline.HitterPower);
        float pitcherPowerEdge = StatBaseline.GetEdge(StatBaseline.GetPitcherPower(pitcher), StatBaseline.PitcherPower);

        return Mathf.Clamp(BaseHomeRunProb + HomeRunCoefficient * (powerEdge - pitcherPowerEdge),
            MinHomeRunProb, MaxHomeRunProb);
    }

    //인플레이 타구가 안타가 될 확률 (BABIP 개념)
    private static float CalcHitProb(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        float contactEdge = StatBaseline.GetEdge(hitter.Contact, StatBaseline.HitterContact);
        float stuffEdge = StatBaseline.GetEdge(pitcher.Stuff, StatBaseline.PitcherStuff);

        return Mathf.Clamp(BaseHitProb + HitCoefficient * (contactEdge - stuffEdge),
            MinHitProb, MaxHitProb);
    }

    //수비팀 평균 수비가 높을수록 낮아진다. avgDefense는 0~1로 정규화된 값이다
    private static float CalcErrorProb(float avgDefense)
    {
        float defenseEdge = StatBaseline.GetEdge(avgDefense * 100f, StatBaseline.HitterDefense);

        return Mathf.Clamp(BaseErrorProb - ErrorDefenseCoefficient * defenseEdge,
            MinErrorProb, MaxErrorProb);
    }

    private static bool Roll(float probability)
    {
        return Random.value < probability;
    }
}
