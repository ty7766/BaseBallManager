using UnityEngine;

/// <summary>
/// 도루 시도·성공 판정 (기획서 8.3.1)
/// </summary>
/// <remarks>
/// 전자동 시뮬이라 플레이어가 도루를 지시하지 않는다. 주자의 주루 스탯으로 시도 여부와 성패를 모두 확률로 가른다.
/// 순수 C# 클래스 - 시뮬 상태를 바꾸지 않고 판정 결과만 돌려준다(상태 변경은 GameSimulator의 몫).
/// </remarks>
public class StealCalculator
{
    //경기당 도루 1.2~1.6개(KBO)에 맞춘 값.
    //기준값이 기획서 초안(0.12 / 0.70)보다 낮은 이유는, 실제로 베이스에 나가는 주자가
    //카드풀 평균보다 주루가 좋기 때문이다(실측 편차 약 +0.30). 500경기 실측으로 역산해 보정했다
    private const float BaseAttemptChance = 0.070f;
    private const float AttemptRunWeight = 0.20f;
    private const float MinAttemptChance = 0.01f;
    private const float MaxAttemptChance = 0.35f;

    //3루 도루는 실제 야구에서 2루 도루보다 훨씬 드물다
    private const float ThirdBaseAttemptMultiplier = 0.35f;

    //평균 주루 주자의 성공률. KBO 도루 성공률 약 70%
    private const float BaseSuccessChance = 0.625f;
    private const float SuccessRunWeight = 0.18f;
    private const float MinSuccessChance = 0.40f;
    private const float MaxSuccessChance = 0.92f;

    /// <summary>
    /// 이번 타석 전에 도루를 시도할 주자를 고른다. 시도하지 않으면 <c>StealAttempt.None</c>
    /// </summary>
    /// <remarks>
    /// 목표 베이스가 비어 있어야 하고, 한 타석에 한 명만 시도한다(더블 스틸 없음).
    /// 2루 주자를 1루 주자보다 먼저 보는 이유는, 1·2루 동시 주자일 때 1루 주자는 목표(2루)가 막혀 애초에 후보가 아니기 때문이다.
    /// </remarks>
    public StealAttempt DecideAttempt(GameState state, SimulationContext context, bool isTopInning)
    {
        //2루 주자 -> 3루 (3루가 비어 있을 때)
        if (state.SecondBase != -1 && state.ThirdBase == -1)
        {
            HitterSnapshot runner = FindRunner(state.SecondBase, context, isTopInning);

            if (Roll(GetAttemptChance(runner.Run) * ThirdBaseAttemptMultiplier))
                return new StealAttempt(state.SecondBase, runner.Name, 2);
        }

        //1루 주자 -> 2루 (2루가 비어 있을 때)
        if (state.FirstBase != -1 && state.SecondBase == -1)
        {
            HitterSnapshot runner = FindRunner(state.FirstBase, context, isTopInning);

            if (Roll(GetAttemptChance(runner.Run)))
                return new StealAttempt(state.FirstBase, runner.Name, 1);
        }

        return StealAttempt.None;
    }

    //해당 주자가 도루에 성공했는지
    public bool IsSuccess(StealAttempt attempt, GameState state, SimulationContext context, bool isTopInning)
    {
        HitterSnapshot runner = FindRunner(attempt.RunnerInstanceId, context, isTopInning);

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

    //공격팀 라인업에서 주자 스냅샷 탐색. 못 찾으면 default (BaseRunningCalculator와 같은 규약)
    private static HitterSnapshot FindRunner(int instanceId, SimulationContext context, bool isTopInning)
    {
        HitterSnapshot[] lineup = isTopInning ? context.AwayLineup : context.HomeLineup;

        foreach (HitterSnapshot hitter in lineup)
        {
            if (hitter.InstanceId == instanceId)
                return hitter;
        }

        return default;
    }

    private static bool Roll(float probability)
    {
        return Random.value < probability;
    }
}
