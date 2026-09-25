using UnityEngine;

/// <summary>
/// 도루 시도 여부와 성패를 주루 스탯으로 판정한다. 상태 변경은 GameSimulator의 몫이다
/// </summary>
public class StealCalculator
{
    //경기당 도루 1.2~1.6개(KBO)에 맞춰 500경기 실측으로 역산한 값
    private const float BaseAttemptChance = 0.070f;
    private const float AttemptRunWeight = 0.20f;
    private const float MinAttemptChance = 0.01f;
    private const float MaxAttemptChance = 0.35f;

    //3루 도루는 2루 도루보다 훨씬 드물다
    private const float ThirdBaseAttemptMultiplier = 0.35f;

    //평균 주루 주자의 성공률. KBO 도루 성공률 약 70%
    private const float BaseSuccessChance = 0.625f;
    private const float SuccessRunWeight = 0.18f;
    private const float MinSuccessChance = 0.40f;
    private const float MaxSuccessChance = 0.92f;

    private const int FromFirstBase = 1;
    private const int FromSecondBase = 2;

    /// <summary>
    /// 타석 전에 도루를 시도할 주자를 고른다. 한 타석에 한 명만 시도한다
    /// </summary>
    public StealAttempt DecideAttempt(GameState state, SimulationContext context, bool isTopInning)
    {
        if (state.SecondBase != GameState.NoRunner && state.ThirdBase == GameState.NoRunner)
        {
            HitterSnapshot runner = RunnerLookup.Find(state.SecondBase, context, isTopInning);

            if (Roll(GetAttemptChance(runner.Run) * ThirdBaseAttemptMultiplier))
                return new StealAttempt(state.SecondBase, runner.Name, FromSecondBase);
        }

        if (state.FirstBase != GameState.NoRunner && state.SecondBase == GameState.NoRunner)
        {
            HitterSnapshot runner = RunnerLookup.Find(state.FirstBase, context, isTopInning);

            if (Roll(GetAttemptChance(runner.Run)))
                return new StealAttempt(state.FirstBase, runner.Name, FromFirstBase);
        }

        return StealAttempt.None;
    }

    /// <summary>
    /// 해당 주자가 도루에 성공했는지 판정한다
    /// </summary>
    public bool IsSuccess(StealAttempt attempt, SimulationContext context, bool isTopInning)
    {
        HitterSnapshot runner = RunnerLookup.Find(attempt.RunnerInstanceId, context, isTopInning);

        return Roll(GetSuccessChance(runner.Run));
    }

    //주루 편차 기반 시도 확률
    private static float GetAttemptChance(int run)
    {
        float runEdge = StatBaseline.GetEdge(run, StatBaseline.HitterRun);

        return Mathf.Clamp(BaseAttemptChance + AttemptRunWeight * runEdge, MinAttemptChance, MaxAttemptChance);
    }

    //주루 편차 기반 성공 확률
    private static float GetSuccessChance(int run)
    {
        float runEdge = StatBaseline.GetEdge(run, StatBaseline.HitterRun);

        return Mathf.Clamp(BaseSuccessChance + SuccessRunWeight * runEdge, MinSuccessChance, MaxSuccessChance);
    }

    private static bool Roll(float probability)
    {
        return Random.value < probability;
    }
}
