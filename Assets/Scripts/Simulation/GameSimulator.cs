using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 경기 전체 진행 상황을 시뮬레이션
/// </summary>
public class GameSimulator
{
    private readonly BatterOutcomeCalculator _batterOutcomeCalc;
    private readonly BaseRunningCalculator _baseRunningCalc;
    private readonly PitchCountCalculator _pitchCountCalc;
    private readonly PitcherChangeEvaluator _pitcherChangeEval;
    private readonly StealCalculator _stealCalc;

    private readonly IGameInterruptHandler _interruptHandler;

    //타석마다 재사용하는 득점 주자 버퍼. 매 타석 새 List를 만들면 일괄 시뮬에서 GC 압박이 됨
    private readonly List<int> _scoredRunnerBuffer = new List<int>(4);


    //알고리즘 계산기 초기화
    public GameSimulator(int pullThreshold = 3, IGameInterruptHandler interruptHandler = null)
    {
        _batterOutcomeCalc = new BatterOutcomeCalculator();
        _baseRunningCalc = new BaseRunningCalculator();
        _pitchCountCalc = new PitchCountCalculator();
        _pitcherChangeEval = new PitcherChangeEvaluator(pullThreshold);
        _stealCalc = new StealCalculator();

        _interruptHandler = interruptHandler;
    }

    //경기 루프 실행 후 GameResult 반환. 진행 규칙을 한 벌로 유지하기 위해 GameSession을 통해 돌린다
    public GameResult SimulateGame(SimulationContext context)
    {
        GameSession session = new GameSession(context, this);

        while (session.StepAtBat())
        {
        }

        return session.BuildResult();
    }

    //타석 1회 처리 (도루 판정, 확률 판정, 투구수 소모, 진루 처리, 투교 판단)
    //GameSession이 타석 단위로 호출한다. 프로젝트 밖에 내놓는 API가 아니므로 internal
    internal void SimulateAtBat(GameState gameState, SimulationContext context,
        List<SimulationBatterLog> logs, List<SimulationStealLog> stealLogs)
    {
        bool isTopInning = gameState.IsTopInning;

        //투구 전에 도루를 먼저 판정한다 (기획서 8.3.1).
        //실패로 3아웃이 되면 이 타석 자체가 없어지므로 여기서 돌아간다.
        //타순을 전진시키지 않으므로 같은 타자가 다음 이닝 선두 타자가 된다(실제 야구 규칙)
        if (!TryResolveSteal(gameState, context, isTopInning, stealLogs))
            return;

        //공격팀 타자 스냅샷
        HitterSnapshot hitter = isTopInning ? context.AwayLineup[gameState.AwayBattingIndex] :
            context.HomeLineup[gameState.HomeBattingIndex];

        //수비팀 투수 스냅샷
        PitcherState defPitcherState = isTopInning ? gameState.HomePitcherState : gameState.AwayPitcherState;
        PitcherSnapshot pitcher = defPitcherState.GetFatiguedSnapshot();

        //평균 수비력 계산
        HitterSnapshot[] defenseLineup = isTopInning ? context.HomeLineup : context.AwayLineup;
        float avgDefense = CalcAverageDefense(defenseLineup);

        //타석 결과 + 투구 수 소모
        BatterOutcome outcome = _batterOutcomeCalc.Calculate(gameState, hitter, pitcher, avgDefense);
        int pitchCount = _pitchCountCalc.Calculate(outcome, hitter, pitcher);
        defPitcherState.ConsumePitches(pitchCount);

        //득점 반영 + 주자 출루 + 타순 변경
        int scoreBefore = isTopInning ? gameState.AwayScore : gameState.HomeScore;
        int inningRunsBefore = defPitcherState.CurrentInningRuns;

        //Apply()가 이닝을 넘기면 Inning · OutCount가 바뀌므로 기록용 값은 미리 잡아둔다
        int inning = gameState.Inning;
        int outCountBefore = gameState.OutCount;

        _scoredRunnerBuffer.Clear();

        gameState.AdvanceBatter();
        _baseRunningCalc.Apply(outcome, hitter.InstanceId, gameState, context, _scoredRunnerBuffer);

        int runsScored = (isTopInning ? gameState.AwayScore : gameState.HomeScore) - scoreBefore;
        bool inningEnded = (isTopInning != gameState.IsTopInning);

        if (!inningEnded)
        {
            for (int i = 0; i < runsScored; i++)
                defPitcherState.AddInningRun();
        }

        int effectiveInningRuns = inningRunsBefore + runsScored;

        //로그 출력. 버퍼는 다음 타석에 재사용되므로 이 타석 몫만 복사해 넘긴다
        logs.Add(new SimulationBatterLog(outcome, pitchCount, runsScored, hitter.Name,
            inning, isTopInning, outCountBefore,
            hitter.InstanceId, pitcher.InstanceId, pitcher.Name,
            _scoredRunnerBuffer.Count == 0
                ? System.Array.Empty<int>()
                : _scoredRunnerBuffer.ToArray()));

        //투수 교체
        if (_pitcherChangeEval.ShouldChange(defPitcherState, gameState, effectiveInningRuns))
        {
            int nextSlot = _pitcherChangeEval.GetNextPitcherSlot(defPitcherState, gameState);
            if (nextSlot != -1)
                gameState.SubstitutePitcher(context, isHome: isTopInning, nextSlot);
        }

        //교체 인터럽트 호출.
        //isTopInning은 Apply() 전에 잡아둔 값을 넘긴다. 이 타석이 3아웃이면 Apply()가 공수를 뒤집어버리므로,
        //그대로 두면 사용자가 예약한 교체가 상대 팀 라인업에 적용된다 (AI는 벤치가 비어 있어 예외까지 남)
        if (_interruptHandler != null)
        {
            InterruptDecision decision = _interruptHandler.OnAtBatEnded(gameState, context);
            ApplyInterruptDecision(decision, gameState, context, isTopInning);
        }
    }

    /// <summary>
    /// 타석 시작 전 도루 판정 (기획서 8.3.1). 이어서 타석을 진행해도 되면 true
    /// </summary>
    /// <remarks>
    /// false를 반환하는 경우는 <b>도루 실패로 이닝이 끝났을 때</b>뿐이다.
    /// 이때 타순을 전진시키면 안 된다 - 실제 야구에서는 그 타자가 다음 이닝 선두 타자로 다시 나온다.
    /// </remarks>
    private bool TryResolveSteal(GameState gameState, SimulationContext context, bool isTopInning,
        List<SimulationStealLog> stealLogs)
    {
        StealAttempt attempt = _stealCalc.DecideAttempt(gameState, context, isTopInning);

        if (!attempt.Exists)
            return true;

        //AddOut()이 이닝을 넘기면 값이 바뀌므로 기록용 값은 미리 잡아둔다
        int inning = gameState.Inning;
        int outCountBefore = gameState.OutCount;

        bool isSuccess = _stealCalc.IsSuccess(attempt, gameState, context, isTopInning);

        if (isSuccess)
        {
            //출발 베이스를 비우고 다음 베이스로 옮긴다
            if (attempt.FromBase == 1)
            {
                gameState.SetFirstBase(-1);
                gameState.SetSecondBase(attempt.RunnerInstanceId);
            }
            else
            {
                gameState.SetSecondBase(-1);
                gameState.SetThirdBase(attempt.RunnerInstanceId);
            }
        }
        else
        {
            //주자 아웃. 베이스를 먼저 비워야 3아웃 잔루 처리와 겹치지 않는다
            if (attempt.FromBase == 1)
                gameState.SetFirstBase(-1);
            else
                gameState.SetSecondBase(-1);

            gameState.AddOut();
        }

        stealLogs.Add(new SimulationStealLog(inning, isTopInning, outCountBefore,
            attempt.RunnerInstanceId, attempt.RunnerName, attempt.FromBase, isSuccess));

        //이닝이 넘어갔거나(3아웃) 경기가 끝났으면 이 타석은 없던 일이 된다
        return !gameState.IsGameOver && isTopInning == gameState.IsTopInning;
    }

    //수비팀 라인업 수비 스탯 평균 및 정규화
    private float CalcAverageDefense(HitterSnapshot[] lineup)
    {
        float sumDefense = 0f;

        foreach(var hitter  in lineup)
        {
            sumDefense += hitter.Defense;
        }

        return sumDefense / 9f / 100f;
    }

    //인터럽트 적용. isTopInning은 방금 끝난 타석 시점의 값 (Apply() 이후 값이 아님)
    private void ApplyInterruptDecision(InterruptDecision decision, GameState state, SimulationContext context, bool isTopInning)
    {
        //초 = 원정 공격 / 홈 수비
        bool isHomeDefending = isTopInning;
        bool ishomeAttacking = !isTopInning;

        //1. 대타 교체 처리
        HitterSnapshot[] attackLineup = ishomeAttacking ? context.HomeLineup : context.AwayLineup;
        HitterSnapshot[] attackBench = ishomeAttacking ? context.HomeBench : context.AwayBench;

        foreach (var sub in decision.HitterSubstitutions)
        {
            //벤치가 없는 팀(AI - 기획서 7.8)에 대타 지시가 오면 조용히 넘긴다
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

            int originHitterInstanceId = attackLineup[sub.BattingOrderIndex].InstanceId;
            state.MarkHitterUsed(ishomeAttacking, originHitterInstanceId);
            attackLineup[sub.BattingOrderIndex] = attackBench[sub.BenchIndex];
        }

        //2. 투수 교체 처리
        if (decision.PitcherSubstitutionSlot != -1)
        {
            int originPitcherSlotIndex = isHomeDefending ? state.HomePitcherState.PitcherSlotIndex : state.AwayPitcherState.PitcherSlotIndex;
            state.MarkPitcherUsed(isHomeDefending, originPitcherSlotIndex);
            state.SubstitutePitcher(context, isHomeDefending, decision.PitcherSubstitutionSlot);
        }
    }
}
