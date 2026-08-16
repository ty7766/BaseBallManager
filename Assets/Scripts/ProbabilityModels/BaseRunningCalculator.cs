using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 진루 시스템
/// 타구 결과를 받아 주자 진루/득점 기록
/// </summary>
public class BaseRunningCalculator
{
    //평균 주루 스탯 주자의 추가 진루 성공 확률
    private const float BaseAdvanceChance = 0.62f;

    //주루 편차 1.0당 진루 확률 변화폭
    private const float AdvanceChanceCoefficient = 0.12f;

    //베이스, 득점, 아웃 갱신
    public void Apply(BatterOutcome outcome, int batterInstanceId, GameState state, SimulationContext context,
        List<int> scoredRunnerIds)
    {
        switch(outcome)
        {
            case BatterOutcome.HomeRun:
                {
                    if (state.ThirdBase != -1)
                        Score(state, state.ThirdBase, scoredRunnerIds);
                    if (state.SecondBase != -1)
                        Score(state, state.SecondBase, scoredRunnerIds);
                    if (state.FirstBase != -1)
                        Score(state, state.FirstBase, scoredRunnerIds);
                    Score(state, batterInstanceId, scoredRunnerIds);

                    state.SetThirdBase(-1);
                    state.SetSecondBase(-1);
                    state.SetFirstBase(-1);
                }
                break;
            case BatterOutcome.Triple:
                {
                    if (state.ThirdBase != -1)
                        Score(state, state.ThirdBase, scoredRunnerIds);
                    if (state.SecondBase != -1)
                        Score(state, state.SecondBase, scoredRunnerIds);
                    if (state.FirstBase != -1)
                        Score(state, state.FirstBase, scoredRunnerIds);

                    state.SetSecondBase(-1);
                    state.SetFirstBase(-1);
                    state.SetThirdBase(batterInstanceId);
                }
                break;
            case BatterOutcome.Double:
                {
                    if (state.ThirdBase != -1)
                    {
                        Score(state, state.ThirdBase, scoredRunnerIds);
                        state.SetThirdBase(-1);
                    }
                    if (state.SecondBase != -1)
                        Score(state, state.SecondBase, scoredRunnerIds);
                    if (state.FirstBase != -1)
                    {
                        HitterSnapshot runner = FindRunnerSnapshot(state.FirstBase, context, state.IsTopInning);
                        if (TryAdvance(runner))
                            Score(state, runner.InstanceId, scoredRunnerIds);
                        else
                            state.SetThirdBase(runner.InstanceId);
                    }
                    state.SetFirstBase(-1);
                    state.SetSecondBase(batterInstanceId);
                }
                break;
            case BatterOutcome.Single:
                {
                    if (state.ThirdBase != -1)
                    {
                        Score(state, state.ThirdBase, scoredRunnerIds);
                        state.SetThirdBase(-1);
                    }
                    if (state.SecondBase != -1)
                    {
                        HitterSnapshot runner = FindRunnerSnapshot(state.SecondBase, context, state.IsTopInning);
                        if (TryAdvance(runner))
                            Score(state, runner.InstanceId, scoredRunnerIds);
                        else
                            state.SetThirdBase(runner.InstanceId);
                        state.SetSecondBase(-1);
                    }
                    if (state.FirstBase != -1)
                    {
                        HitterSnapshot runner = FindRunnerSnapshot(state.FirstBase, context,
                    state.IsTopInning);
                        if (TryAdvance(runner))
                        {
                            if (state.ThirdBase == -1)
                                state.SetThirdBase(runner.InstanceId);
                            else
                                state.SetSecondBase(runner.InstanceId);
                        }
                        else
                            state.SetSecondBase(runner.InstanceId);
                    }
                    state.SetFirstBase(batterInstanceId);
                }
                break;
            case BatterOutcome.Walk:
                {
                    if (state.FirstBase != -1)
                    {
                        if (state.SecondBase != -1)
                        {
                            if (state.ThirdBase != -1)
                            {
                                Score(state, state.ThirdBase, scoredRunnerIds);
                            }
                            state.SetThirdBase(state.SecondBase);
                            state.SetSecondBase(state.FirstBase);
                            state.SetFirstBase(batterInstanceId);
                        }
                        else
                        {
                            state.SetSecondBase(state.FirstBase);
                            state.SetFirstBase(batterInstanceId);
                        }
                    }
                    else
                    {
                        state.SetFirstBase(batterInstanceId);
                    }
                }
                break;
            case BatterOutcome.StrikeOut:
                {
                    state.AddOut();
                }
                break;
            case BatterOutcome.GroundOut:
                {
                    state.AddOut();
                }
                break;
            case BatterOutcome.FlyOut:
                {
                    state.AddOut();
                }
                break;
            case BatterOutcome.SacrificeFly:
                {
                    state.AddOut();
                    HitterSnapshot runner = FindRunnerSnapshot(state.ThirdBase, context, state.IsTopInning);
                    if (TryAdvance(runner))
                    {
                        state.SetThirdBase(-1);
                        Score(state, runner.InstanceId, scoredRunnerIds);
                    }
                }
                break;
            case BatterOutcome.DoublePlay:
                 {
                    state.SetFirstBase(-1);
                    state.AddOut();
                    state.AddOut();
                }
                break;
            case BatterOutcome.Error:
                {
                    if (state.FirstBase != -1)
                    {
                        if (state.SecondBase != -1)
                        {
                            if (state.ThirdBase != -1)
                            {
                                Score(state, state.ThirdBase, scoredRunnerIds);
                            }
                            state.SetThirdBase(state.SecondBase);
                            state.SetSecondBase(state.FirstBase);
                            state.SetFirstBase(batterInstanceId);
                        }
                        else
                        {
                            state.SetSecondBase(state.FirstBase);
                            state.SetFirstBase(batterInstanceId);
                        }
                    }
                    else
                    {
                        state.SetFirstBase(batterInstanceId);
                    }
                }
                break;
        }
    }

    //추가 진루 확률 판정 (Run 스탯)
    private bool TryAdvance(HitterSnapshot runner)
    {
        //1. 평균 대비 편차로 변환 (평균 주자면 기준 확률 그대로)
        float runEdge = StatBaseline.GetEdge(runner.Run, StatBaseline.HitterRun);

        //2. 진루 확률 계산 (25% ~ 90% 범위 제한)
        float probRun = Mathf.Clamp(BaseAdvanceChance + AdvanceChanceCoefficient * runEdge, 0.25f, 0.90f);

        return Random.value < probRun;
    }

    //득점 처리. 홈을 밟은 주자를 함께 기록해 선수별 득점(R) 집계에 쓴다
    private void Score(GameState state, int runnerInstanceId, List<int> scoredRunnerIds)
    {
        state.AddRun();
        scoredRunnerIds.Add(runnerInstanceId);
    }

    //현재 베이스에 있는 주자의 스냅샷 탐색
    private HitterSnapshot FindRunnerSnapshot(int instanceId, SimulationContext context, bool isTopInning)
    {
        if (isTopInning)
        {
            foreach(var runner in context.AwayLineup)
            {
                if (runner.InstanceId == instanceId)
                    return runner;
            }
        }
        else
        {
            foreach(var runner in context.HomeLineup)
            {
                if (runner.InstanceId == instanceId)
                    return runner;
            }
        }
        return default;
    }
}
