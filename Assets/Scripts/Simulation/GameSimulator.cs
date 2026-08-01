using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// 경기 전체 진행 상황을 시뮬레이션
/// </summary>
public class GameSimulator
{
    private readonly BatterOutcomeCalculator _batterOutcomeCalc;
    private readonly BaseRunningCalculator _baseRunningCalc;
    private readonly PitchCountCalculator _pitchCountCalc;
    private readonly PitcherChangeEvaluator _pitcherChangeEval;

    //알고리즘 계산기 초기화
    public GameSimulator(int pullThreshold = 3)
    {
        _batterOutcomeCalc = new BatterOutcomeCalculator();
        _baseRunningCalc = new BaseRunningCalculator();
        _pitchCountCalc = new PitchCountCalculator();
        _pitcherChangeEval = new PitcherChangeEvaluator(pullThreshold);
    }

    //경기 루프 실행 후 GameResult 반환
    public GameResult SimulateGame(SimulationContext context)
    {
        GameState gameState = new GameState(context);
        List<SimulationBatterLog> logs = new List<SimulationBatterLog>();

        while (!gameState.IsGameOver)
        {
            SimulateAtBat(gameState, context, logs);
        }

        return new GameResult(gameState.HomeScore, gameState.AwayScore, logs);
    }

    //타석 1회 처리 (확률 판정, 투구수 소모, 진루 처리, 투교 판단)
    private void SimulateAtBat(GameState gameState, SimulationContext context, List<SimulationBatterLog> logs)
    {
        bool isTopInning = gameState.IsTopInning;

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
        gameState.AdvanceBatter();
        _baseRunningCalc.Apply(outcome, hitter.InstanceId, gameState, context);
        int runsScored = (isTopInning ? gameState.AwayScore : gameState.HomeScore) - scoreBefore;

        for (int i = 0; i < runsScored; i++)
            defPitcherState.AddInningRun();

        //로그 출력
        logs.Add(new SimulationBatterLog(outcome, pitchCount, runsScored, hitter.Name));

        //투수 교체
        if (_pitcherChangeEval.ShouldChange(defPitcherState, gameState))
        {
            int nextSlot = _pitcherChangeEval.GetNextPitcherSlot(defPitcherState, gameState);
            gameState.SubstitutePitcher(context, isHome: isTopInning, nextSlot);
        }
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
}
