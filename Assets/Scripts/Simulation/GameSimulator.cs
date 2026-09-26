using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경기 진행 규칙 한 벌. 타석 단위 처리와 경기 전체 루프를 담당한다
/// </summary>
public class GameSimulator
{
    private readonly BatterOutcomeCalculator _batterOutcomeCalc;
    private readonly BaseRunningCalculator _baseRunningCalc;
    private readonly PitchCountCalculator _pitchCountCalc;
    private readonly PitcherChangeEvaluator _pitcherChangeEval;
    private readonly StealCalculator _stealCalc;

    private readonly IGameInterruptHandler _interruptHandler;

    //타석마다 재사용하는 득점 주자 버퍼. 매 타석 새 List를 만들면 일괄 시뮬에서 GC 압박이 된다
    private readonly List<int> _scoredRunnerBuffer = new List<int>(4);

    public GameSimulator(int pullThreshold = 3, IGameInterruptHandler interruptHandler = null)
    {
        _batterOutcomeCalc = new BatterOutcomeCalculator();
        _baseRunningCalc = new BaseRunningCalculator();
        _pitchCountCalc = new PitchCountCalculator();
        _pitcherChangeEval = new PitcherChangeEvaluator(pullThreshold);
        _stealCalc = new StealCalculator();

        _interruptHandler = interruptHandler;
    }

    /// <summary>
    /// 경기가 끝날 때까지 돌리고 결과를 반환한다. 진행은 GameSession을 경유한다
    /// </summary>
    public GameResult SimulateGame(SimulationContext context)
    {
        GameSession session = new GameSession(context, this);

        while (session.StepAtBat())
        {
        }

        return session.BuildResult();
    }

    /// <summary>
    /// 타석 1회 처리 (도루 판정 -> 타석 결과 -> 진루 -> 투수 교체 -> 인터럽트)
    /// </summary>
    internal void SimulateAtBat(GameState gameState, SimulationContext context,
        List<SimulationBatterLog> logs, List<SimulationStealLog> stealLogs)
    {
        bool isTopInning = gameState.IsTopInning;

        if (!TryResolveSteal(gameState, context, isTopInning, stealLogs))
            return;

        HitterSnapshot hitter = isTopInning
            ? context.AwayLineup[gameState.AwayBattingIndex]
            : context.HomeLineup[gameState.HomeBattingIndex];

        PitcherState defPitcherState = isTopInning ? gameState.HomePitcherState : gameState.AwayPitcherState;
        PitcherSnapshot pitcher = defPitcherState.GetFatiguedSnapshot();

        HitterSnapshot[] defenseLineup = isTopInning ? context.HomeLineup : context.AwayLineup;
        float avgDefense = CalcAverageDefense(defenseLineup);

        BatterOutcome outcome = _batterOutcomeCalc.Calculate(gameState, hitter, pitcher, avgDefense);
        int pitchCount = _pitchCountCalc.Calculate(outcome, hitter, pitcher);
        defPitcherState.ConsumePitches(pitchCount);

        int scoreBefore = isTopInning ? gameState.AwayScore : gameState.HomeScore;
        int inningRunsBefore = defPitcherState.CurrentInningRuns;
        int inning = gameState.Inning;
        int outCountBefore = gameState.OutCount;

        _scoredRunnerBuffer.Clear();

        gameState.AdvanceBatter();
        _baseRunningCalc.Apply(outcome, hitter.InstanceId, gameState, context, _scoredRunnerBuffer);

        int runsScored = (isTopInning ? gameState.AwayScore : gameState.HomeScore) - scoreBefore;
        bool inningEnded = isTopInning != gameState.IsTopInning;

        if (!inningEnded)
        {
            for (int i = 0; i < runsScored; i++)
                defPitcherState.AddInningRun();
        }

        int effectiveInningRuns = inningRunsBefore + runsScored;

        logs.Add(new SimulationBatterLog(outcome, pitchCount, runsScored, hitter.Name,
            inning, isTopInning, outCountBefore,
            hitter.InstanceId, pitcher.InstanceId, pitcher.Name,
            _scoredRunnerBuffer.Count == 0 ? Array.Empty<int>() : _scoredRunnerBuffer.ToArray()));

        TryChangePitcher(gameState, context, defPitcherState, isTopInning, effectiveInningRuns);

        if (_interruptHandler != null)
        {
            InterruptDecision decision = _interruptHandler.OnAtBatEnded(gameState, context);
            ApplyInterruptDecision(decision, gameState, context, isTopInning);
        }
    }

    //자동 투수 교체. 강판된 슬롯을 기록해야 다음 탐색이 그 투수를 다시 올리지 않는다
    private void TryChangePitcher(GameState gameState, SimulationContext context,
        PitcherState defPitcherState, bool isTopInning, int effectiveInningRuns)
    {
        if (!_pitcherChangeEval.ShouldChange(defPitcherState, gameState, effectiveInningRuns))
            return;

        int nextSlot = _pitcherChangeEval.GetNextPitcherSlot(defPitcherState, gameState, isTopInning);

        if (nextSlot == PitcherChangeEvaluator.NoNextPitcher)
            return;

        gameState.MarkPitcherUsed(isTopInning, defPitcherState.PitcherSlotIndex);
        gameState.SubstitutePitcher(context, isTopInning, nextSlot);
    }

    //타석 시작 전 도루 판정. 도루 실패로 이닝이 끝나면 false (타순을 전진시키지 않는다)
    private bool TryResolveSteal(GameState gameState, SimulationContext context, bool isTopInning,
        List<SimulationStealLog> stealLogs)
    {
        StealAttempt attempt = _stealCalc.DecideAttempt(gameState, context, isTopInning);

        if (!attempt.Exists)
            return true;

        int inning = gameState.Inning;
        int outCountBefore = gameState.OutCount;
        PitcherSnapshot pitcher = (isTopInning ? gameState.HomePitcherState : gameState.AwayPitcherState).Snapshot;

        bool isSuccess = _stealCalc.IsSuccess(attempt, context, isTopInning);

        if (isSuccess)
        {
            if (attempt.FromBase == 1)
            {
                gameState.SetFirstBase(GameState.NoRunner);
                gameState.SetSecondBase(attempt.RunnerInstanceId);
            }
            else
            {
                gameState.SetSecondBase(GameState.NoRunner);
                gameState.SetThirdBase(attempt.RunnerInstanceId);
            }
        }
        else
        {
            if (attempt.FromBase == 1)
                gameState.SetFirstBase(GameState.NoRunner);
            else
                gameState.SetSecondBase(GameState.NoRunner);

            gameState.AddOut();
        }

        stealLogs.Add(new SimulationStealLog(inning, isTopInning, outCountBefore,
            attempt.RunnerInstanceId, attempt.RunnerName, pitcher.InstanceId, pitcher.Name,
            attempt.FromBase, isSuccess));

        return !gameState.IsGameOver && isTopInning == gameState.IsTopInning;
    }

    //수비팀 라인업의 수비 스탯 평균을 0~1로 정규화
    private static float CalcAverageDefense(HitterSnapshot[] lineup)
    {
        float sumDefense = 0f;

        foreach (HitterSnapshot hitter in lineup)
            sumDefense += hitter.Defense;

        return sumDefense / lineup.Length / 100f;
    }

    //인터럽트 적용. isTopInning은 방금 끝난 타석 시점의 값이어야 한다 (Apply 이후 값이 아님)
    private static void ApplyInterruptDecision(InterruptDecision decision, GameState state,
        SimulationContext context, bool isTopInning)
    {
        bool isHomeDefending = isTopInning;
        bool isHomeAttacking = !isTopInning;

        HitterSnapshot[] attackLineup = isHomeAttacking ? context.HomeLineup : context.AwayLineup;
        HitterSnapshot[] attackBench = isHomeAttacking ? context.HomeBench : context.AwayBench;

        foreach (HitterSubstitution sub in decision.HitterSubstitutions)
        {
            if (sub.BenchIndex < 0 || sub.BenchIndex >= attackBench.Length)
            {
                Debug.LogWarning($"[GameSimulator]: 벤치 {sub.BenchIndex}번이 없어 대타 교체를 건너뜁니다 (벤치 {attackBench.Length}칸)");
                continue;
            }

            if (sub.BattingOrderIndex < 0 || sub.BattingOrderIndex >= attackLineup.Length)
            {
                Debug.LogWarning($"[GameSimulator]: 타순 {sub.BattingOrderIndex}번이 라인업 범위를 벗어났습니다");
                continue;
            }

            HitterSnapshot benchHitter = attackBench[sub.BenchIndex];

            if (benchHitter.InstanceId == SimulationContext.NoHitter)
            {
                Debug.LogWarning($"[GameSimulator]: 벤치 {sub.BenchIndex}번이 비어 있어 대타 교체를 건너뜁니다");
                continue;
            }

            if (state.IsBenchUsed(isHomeAttacking, sub.BenchIndex))
            {
                Debug.LogWarning($"[GameSimulator]: 벤치 {sub.BenchIndex}번은 이미 투입됐습니다 (한 선수가 두 타순에 설 수 없음)");
                continue;
            }

            if (state.IsHitterUsed(isHomeAttacking, benchHitter.InstanceId))
            {
                Debug.LogWarning($"[GameSimulator]: {benchHitter.Name} 선수는 이미 교체로 빠졌습니다");
                continue;
            }

            int originHitterInstanceId = attackLineup[sub.BattingOrderIndex].InstanceId;
            state.MarkHitterUsed(isHomeAttacking, originHitterInstanceId);
            state.MarkBenchUsed(isHomeAttacking, sub.BenchIndex);
            attackLineup[sub.BattingOrderIndex] = benchHitter;
        }

        ApplyPitcherSubstitution(decision, state, context, isHomeDefending);
    }

    //투수 교체 인터럽트. 같은 슬롯 재지정은 PitcherState를 새로 만들어 체력을 되돌리므로 막는다
    private static void ApplyPitcherSubstitution(InterruptDecision decision, GameState state,
        SimulationContext context, bool isHomeDefending)
    {
        int newSlotIndex = decision.PitcherSubstitutionSlot;

        if (newSlotIndex == LiveGameController.NoPitcherSubstitution)
            return;

        int originPitcherSlotIndex = isHomeDefending
            ? state.HomePitcherState.PitcherSlotIndex
            : state.AwayPitcherState.PitcherSlotIndex;

        if (newSlotIndex == originPitcherSlotIndex)
        {
            Debug.LogWarning($"[GameSimulator]: {newSlotIndex}번 투수는 이미 등판 중입니다");
            return;
        }

        if (state.IsPitcherUsed(isHomeDefending, newSlotIndex))
        {
            Debug.LogWarning($"[GameSimulator]: {newSlotIndex}번 투수는 이미 강판됐습니다");
            return;
        }

        state.MarkPitcherUsed(isHomeDefending, originPitcherSlotIndex);
        state.SubstitutePitcher(context, isHomeDefending, newSlotIndex);
    }
}
