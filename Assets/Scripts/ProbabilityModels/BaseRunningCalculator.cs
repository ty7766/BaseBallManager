using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타석 결과를 받아 주자 진루·득점·아웃을 반영한다
/// </summary>
public class BaseRunningCalculator
{
    //평균 주루 스탯 주자의 추가 진루 성공 확률
    private const float BaseAdvanceChance = 0.62f;

    //주루 편차 1.0당 진루 확률 변화폭
    private const float AdvanceChanceCoefficient = 0.12f;
    private const float MinAdvanceChance = 0.25f;
    private const float MaxAdvanceChance = 0.90f;

    /// <summary>
    /// 베이스·득점·아웃을 갱신한다. 홈을 밟은 주자는 scoredRunnerIds에 쌓인다
    /// </summary>
    public void Apply(BatterOutcome outcome, int batterInstanceId, GameState state, SimulationContext context,
        List<int> scoredRunnerIds)
    {
        switch (outcome)
        {
            case BatterOutcome.HomeRun:
                ScoreAllRunners(state, scoredRunnerIds);
                Score(state, batterInstanceId, scoredRunnerIds);
                ClearBases(state);
                break;

            case BatterOutcome.Triple:
                ScoreAllRunners(state, scoredRunnerIds);
                ClearBases(state);
                state.SetThirdBase(batterInstanceId);
                break;

            case BatterOutcome.Double:
                ApplyDouble(batterInstanceId, state, context, scoredRunnerIds);
                break;

            case BatterOutcome.Single:
                ApplySingle(batterInstanceId, state, context, scoredRunnerIds);
                break;

            case BatterOutcome.Walk:
                PushRunnersForced(batterInstanceId, state, scoredRunnerIds);
                break;

            case BatterOutcome.Error:
                PushAllRunners(batterInstanceId, state, scoredRunnerIds);
                break;

            case BatterOutcome.StrikeOut:
            case BatterOutcome.GroundOut:
            case BatterOutcome.FlyOut:
                state.AddOut();
                break;

            case BatterOutcome.SacrificeFly:
                ApplySacrificeFly(state, context, scoredRunnerIds);
                break;

            case BatterOutcome.DoublePlay:
                ApplyDoublePlay(state);
                break;
        }
    }

    //2루타 - 3루·2루 주자는 홈, 1루 주자는 주루 판정으로 홈 또는 3루
    private void ApplyDouble(int batterInstanceId, GameState state, SimulationContext context,
        List<int> scoredRunnerIds)
    {
        if (state.ThirdBase != GameState.NoRunner)
        {
            Score(state, state.ThirdBase, scoredRunnerIds);
            state.SetThirdBase(GameState.NoRunner);
        }

        if (state.SecondBase != GameState.NoRunner)
            Score(state, state.SecondBase, scoredRunnerIds);

        if (state.FirstBase != GameState.NoRunner)
        {
            HitterSnapshot runner = RunnerLookup.Find(state.FirstBase, context, state.IsTopInning);

            if (TryAdvance(runner))
                Score(state, runner.InstanceId, scoredRunnerIds);
            else
                state.SetThirdBase(runner.InstanceId);
        }

        state.SetFirstBase(GameState.NoRunner);
        state.SetSecondBase(batterInstanceId);
    }

    //단타 - 3루 주자는 홈, 2루·1루 주자는 주루 판정으로 한 베이스 더 갈 수 있다
    private void ApplySingle(int batterInstanceId, GameState state, SimulationContext context,
        List<int> scoredRunnerIds)
    {
        if (state.ThirdBase != GameState.NoRunner)
        {
            Score(state, state.ThirdBase, scoredRunnerIds);
            state.SetThirdBase(GameState.NoRunner);
        }

        if (state.SecondBase != GameState.NoRunner)
        {
            HitterSnapshot runner = RunnerLookup.Find(state.SecondBase, context, state.IsTopInning);

            if (TryAdvance(runner))
                Score(state, runner.InstanceId, scoredRunnerIds);
            else
                state.SetThirdBase(runner.InstanceId);

            state.SetSecondBase(GameState.NoRunner);
        }

        if (state.FirstBase != GameState.NoRunner)
        {
            HitterSnapshot runner = RunnerLookup.Find(state.FirstBase, context, state.IsTopInning);

            if (TryAdvance(runner) && state.ThirdBase == GameState.NoRunner)
                state.SetThirdBase(runner.InstanceId);
            else
                state.SetSecondBase(runner.InstanceId);
        }

        state.SetFirstBase(batterInstanceId);
    }

    //실책 - 추가 진루 판정 없이 전원 한 베이스씩 (기획서 8.3). 밀어내기와 달리 강제되지 않은 주자도 움직인다
    private static void PushAllRunners(int batterInstanceId, GameState state, List<int> scoredRunnerIds)
    {
        if (state.ThirdBase != GameState.NoRunner)
            Score(state, state.ThirdBase, scoredRunnerIds);

        state.SetThirdBase(state.SecondBase);
        state.SetSecondBase(state.FirstBase);
        state.SetFirstBase(batterInstanceId);
    }

    //병살 - 타자와 1루 주자가 아웃. 첫 아웃이 이닝을 끝내면 두 번째 아웃은 다음 이닝 몫이 되므로 멈춘다
    private static void ApplyDoublePlay(GameState state)
    {
        bool wasTopInning = state.IsTopInning;

        state.SetFirstBase(GameState.NoRunner);
        state.AddOut();

        if (state.IsGameOver || state.IsTopInning != wasTopInning)
            return;

        state.AddOut();
    }

    //볼넷 - 밀어내기. 1루가 비면 그 앞 주자는 움직이지 않는다
    private void PushRunnersForced(int batterInstanceId, GameState state, List<int> scoredRunnerIds)
    {
        if (state.FirstBase == GameState.NoRunner)
        {
            state.SetFirstBase(batterInstanceId);
            return;
        }

        if (state.SecondBase == GameState.NoRunner)
        {
            state.SetSecondBase(state.FirstBase);
            state.SetFirstBase(batterInstanceId);
            return;
        }

        if (state.ThirdBase != GameState.NoRunner)
            Score(state, state.ThirdBase, scoredRunnerIds);

        state.SetThirdBase(state.SecondBase);
        state.SetSecondBase(state.FirstBase);
        state.SetFirstBase(batterInstanceId);
    }

    //희생플라이 - 3루 주자를 AddOut 전에 잡아둔다. 아웃이 이닝을 넘기면 주자가 지워지기 때문
    private void ApplySacrificeFly(GameState state, SimulationContext context, List<int> scoredRunnerIds)
    {
        int thirdBaseRunnerId = state.ThirdBase;
        bool wasTopInning = state.IsTopInning;

        state.AddOut();

        //3아웃이면 득점이 무효다. 막지 않으면 AddRun이 공수가 바뀐 뒤의 팀에 점수를 준다
        if (state.IsGameOver || state.IsTopInning != wasTopInning)
            return;

        if (thirdBaseRunnerId == GameState.NoRunner)
            return;

        HitterSnapshot runner = RunnerLookup.Find(thirdBaseRunnerId, context, state.IsTopInning);

        if (!TryAdvance(runner))
            return;

        state.SetThirdBase(GameState.NoRunner);
        Score(state, runner.InstanceId, scoredRunnerIds);
    }

    //모든 베이스 주자 득점 처리
    private void ScoreAllRunners(GameState state, List<int> scoredRunnerIds)
    {
        if (state.ThirdBase != GameState.NoRunner)
            Score(state, state.ThirdBase, scoredRunnerIds);

        if (state.SecondBase != GameState.NoRunner)
            Score(state, state.SecondBase, scoredRunnerIds);

        if (state.FirstBase != GameState.NoRunner)
            Score(state, state.FirstBase, scoredRunnerIds);
    }

    //베이스 비우기
    private static void ClearBases(GameState state)
    {
        state.SetThirdBase(GameState.NoRunner);
        state.SetSecondBase(GameState.NoRunner);
        state.SetFirstBase(GameState.NoRunner);
    }

    //주루 편차 기반 추가 진루 판정
    private static bool TryAdvance(HitterSnapshot runner)
    {
        float runEdge = StatBaseline.GetEdge(runner.Run, StatBaseline.HitterRun);
        float advanceChance = Mathf.Clamp(BaseAdvanceChance + AdvanceChanceCoefficient * runEdge,
            MinAdvanceChance, MaxAdvanceChance);

        return Random.value < advanceChance;
    }

    //득점 처리. 홈을 밟은 주자를 함께 기록해 선수별 득점 집계에 쓴다
    private static void Score(GameState state, int runnerInstanceId, List<int> scoredRunnerIds)
    {
        state.AddRun();
        scoredRunnerIds.Add(runnerInstanceId);
    }
}
