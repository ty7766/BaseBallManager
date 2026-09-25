using System.Collections.Generic;

/// <summary>
/// 강화·훈련을 반영한 카드 최종 스탯 계산
/// </summary>
public static class CardStatsCalculator
{
    private const int EnhanceBonusPerLevel = 2;

    /// <summary>
    /// 세부 스탯 1개의 최종값 (기본값 + 강화 보정 + 훈련 분배값)
    /// </summary>
    public static int CalculateFinalStat(int baseStat, int enhanceLevel, int trainDelta)
    {
        return baseStat + enhanceLevel * EnhanceBonusPerLevel + trainDelta;
    }

    /// <summary>
    /// 표시용 OVR. 4스탯 평균이라 훈련 분배값은 총합을 스탯 수로 나눈다 (CSV와 같은 내림)
    /// </summary>
    public static int CalculateFinalOVR(int baseOvr, int enhanceLevel, IReadOnlyList<int> trainDelta)
    {
        int trainSum = 0;

        if (trainDelta != null)
        {
            for (int i = 0; i < trainDelta.Count; i++)
                trainSum += trainDelta[i];
        }

        return baseOvr + enhanceLevel * EnhanceBonusPerLevel + trainSum / CardInstance.TrainStatCount;
    }
}
