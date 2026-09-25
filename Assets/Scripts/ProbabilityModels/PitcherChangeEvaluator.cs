/// <summary>
/// 매 타석 종료 후 투수 교체 여부와 다음 투수를 판단한다
/// </summary>
public class PitcherChangeEvaluator
{
    private const float ExhaustedStaminaRatio = 0.30f;
    private const float TiredStaminaRatio = 0.50f;
    private const int SaveSituationMaxLead = 3;

    private readonly int _pullThreshold;

    public PitcherChangeEvaluator(int pullThreshold = 3)
    {
        _pullThreshold = pullThreshold;
    }

    /// <summary>
    /// 체력·실점·주자 상황을 점수화해 교체할지 판단한다
    /// </summary>
    public bool ShouldChange(PitcherState pitcherState, GameState gameState, int effectiveInningRuns)
    {
        float currentStaminaRatio = pitcherState.GetFatigueRatio();

        if (currentStaminaRatio <= 0f)
            return true;

        int pullScore = effectiveInningRuns;

        if (currentStaminaRatio < ExhaustedStaminaRatio)
            pullScore += 2;
        else if (currentStaminaRatio < TiredStaminaRatio)
            pullScore += 1;

        if (CountRunners(gameState) >= 2 && gameState.OutCount < GameState.OutsPerInning - 1)
            pullScore += 1;

        return pullScore >= _pullThreshold;
    }

    /// <summary>
    /// 다음에 올릴 투수 슬롯. 남은 투수가 없으면 -1
    /// </summary>
    public int GetNextPitcherSlot(PitcherState currentState, GameState gameState)
    {
        bool isHomePitching = gameState.IsTopInning;

        int scoreDiff = isHomePitching
            ? gameState.HomeScore - gameState.AwayScore
            : gameState.AwayScore - gameState.HomeScore;

        bool isSaveSituation = gameState.Inning >= GameState.RegulationInnings
            && scoreDiff >= 1 && scoreDiff <= SaveSituationMaxLead;

        if (isSaveSituation
            && currentState.PitcherSlotIndex != SimulationContext.CloserSlot
            && !gameState.IsPitcherUsed(isHomePitching, SimulationContext.CloserSlot))
        {
            return SimulationContext.CloserSlot;
        }

        for (int nextSlot = currentState.PitcherSlotIndex + 1; nextSlot <= SimulationContext.CloserSlot; nextSlot++)
        {
            if (!gameState.IsPitcherUsed(isHomePitching, nextSlot))
                return nextSlot;
        }

        return -1;
    }

    //베이스에 나가 있는 주자 수
    private static int CountRunners(GameState gameState)
    {
        int runnerCount = 0;

        if (gameState.FirstBase != GameState.NoRunner)
            runnerCount++;

        if (gameState.SecondBase != GameState.NoRunner)
            runnerCount++;

        if (gameState.ThirdBase != GameState.NoRunner)
            runnerCount++;

        return runnerCount;
    }
}
