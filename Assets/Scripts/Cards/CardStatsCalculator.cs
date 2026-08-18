using System.Collections.Generic;

/// <summary>
/// 카드의 최종 스탯 계산
/// </summary>
public static class CardStatsCalculator
{
    private const int EnhanceBonusPerLevel = 2;
    private const int StatCount = 4;

    public static int CalculateFinalStat(int baseStat, int enhanceLevel, int trainDelta)
    {
        return baseStat + enhanceLevel * EnhanceBonusPerLevel + trainDelta;
    }

    /// <summary>
    /// 강화·훈련이 반영된 표시용 OVR (기획서 1.3)
    /// </summary>
    /// <remarks>
    /// OVR은 4스탯 평균이므로 강화는 레벨당 +2, 훈련은 분배값 총합 / 4 만큼 오른다.
    /// CSV의 OVR도 내림이라 정수 나눗셈을 그대로 쓴다.
    /// </remarks>
    public static int CalculateFinalOVR(int baseOvr, int enhanceLevel, IReadOnlyList<int> trainDelta)
    {
        int trainSum = 0;

        if (trainDelta != null)
        {
            for (int i = 0; i < trainDelta.Count; i++)
            {
                trainSum += trainDelta[i];
            }
        }

        return baseOvr + enhanceLevel * EnhanceBonusPerLevel + trainSum / StatCount;
    }
}
