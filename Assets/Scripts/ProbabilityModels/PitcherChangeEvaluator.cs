using System.Runtime.ExceptionServices;

/// <summary>
/// 매 타석 종료 후 투수 교체 판단
/// </summary>
public class PitcherChangeEvaluator
{
    private int _pullThreshold;     //교체 판단 점수 임계값

    public PitcherChangeEvaluator(int pullThreshold = 3)
    {
        _pullThreshold = pullThreshold;
    }

    //교체 여부 판단
    public bool ShouldChange(PitcherState pitcherState, GameState gameState, int effectiveInningRuns)
    {
        bool forcePull = false;
        int pullScore = 0;
        float currentStaminaRatio = pitcherState.GetFatigueRatio();

        //체력 계산
        if (currentStaminaRatio < 0.30f)
            pullScore += 2;
        else if (currentStaminaRatio < 0.50f)
            pullScore += 1;
        if (currentStaminaRatio <= 0)
            forcePull = true;
            

        //위기 상황 계산
        pullScore += effectiveInningRuns;
        int runnerCount = 0;
        if (gameState.FirstBase != -1)
            runnerCount++;
        if (gameState.SecondBase != -1)
            runnerCount++;
        if (gameState.ThirdBase != -1)
            runnerCount++;

        if (runnerCount >= 2 && gameState.OutCount < 2)
            pullScore += 1;

        //투구 교체 결정
        if (forcePull || pullScore >= _pullThreshold)
            return true;
        return false;
    }

    //다음 투수 슬롯 인덱스 반환
    public int GetNextPitcherSlot(PitcherState currentState, GameState gameState)
    {

        int scoreDiff = 0;

        //초(IsTopInning) = 원정 공격 = 홈 수비. GameSimulator가 수비 투수를 고르는 기준과 같아야 한다
        bool isHomePitching = gameState.IsTopInning;

        //현재 투구팀이 홈인지 원정인지 판별후 현재 점수차 계산
        if (isHomePitching)
        {
            scoreDiff = gameState.HomeScore - gameState.AwayScore;
        }
        else
        {
            scoreDiff = gameState.AwayScore - gameState.HomeScore;
        }

        bool isSaveSituation = gameState.Inning >= 9 && scoreDiff >= 1 && scoreDiff <= 3;

        //세이브 상황이면 마무리 등판
        if (isSaveSituation && currentState.PitcherSlotIndex != 6 && !gameState.IsPitcherUsed(isHomePitching, 6))
        {
            return 6;
        }

        //중계 투수 등판
        for (int nextSlot = currentState.PitcherSlotIndex + 1; nextSlot <= 6; nextSlot++)
        {
            //아직 등판 안한 투수 탐색하여 교체
            if (!gameState.IsPitcherUsed(isHomePitching, nextSlot))
                return nextSlot;
        }
        return -1;
    }
}
