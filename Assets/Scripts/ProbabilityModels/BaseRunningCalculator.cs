using UnityEngine;

/// <summary>
/// 진루 시스템
/// 타구 결과를 받아 주자 진루/득점 기록
/// </summary>
public class BaseRunningCalculator
{
    //베이스, 득점, 아웃 갱신
    public void Apply(BatterOutcome outcome, int batterInstanceId, GameState state, SimulationContext context)
    {
        switch(outcome)
        {
            case BatterOutcome.HomeRun:
                {
                    if (state.ThirdBase != -1)
                        Score(state);
                    if (state.SecondBase != -1)
                        Score(state);
                    if (state.FirstBase != -1)
                        Score(state);
                    Score(state);

                    state.SetThirdBase(-1);
                    state.SetSecondBase(-1);
                    state.SetFirstBase(-1);
                }
                break;
            case BatterOutcome.Triple:
                {
                    if (state.ThirdBase != -1)
                        Score(state);
                    if (state.SecondBase != -1)
                        Score(state);
                    if (state.FirstBase != -1)
                        Score(state);

                    state.SetSecondBase(-1);
                    state.SetFirstBase(-1);
                    state.SetThirdBase(batterInstanceId);
                }
                break;
            case BatterOutcome.Double:
                {
                    if (state.ThirdBase != -1)
                    {
                        Score(state);
                        state.SetThirdBase(-1);
                    }
                    if (state.SecondBase != -1)
                        Score(state);
                    if (state.FirstBase != -1)
                    {
                        HitterSnapshot runner = FindRunnerSnapshot(state.FirstBase, context, state.IsTopInning);
                        if (TryAdvance(runner))
                            Score(state);
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
                        Score(state);
                        state.SetThirdBase(-1);
                    }
                    if (state.SecondBase != -1)
                    {
                        HitterSnapshot runner = FindRunnerSnapshot(state.SecondBase, context, state.IsTopInning);
                        if (TryAdvance(runner))
                            Score(state);
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
                                Score(state);
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
                        Score(state);
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
                                Score(state);
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
        //1. 정규화
        float run = runner.Run / 100f;

        //2. 진루 확률 계산 (25% ~ 90% 범위 제한)
        float probRun = Mathf.Clamp(0.4f + 0.6f * (run - 0.5f), 0.25f, 0.9f);

        return Random.value < probRun;
    }

    //득점 처리
    private void Score(GameState state)
    {
        state.AddRun();
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
