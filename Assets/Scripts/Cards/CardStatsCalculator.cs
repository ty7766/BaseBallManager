/// <summary>
/// 카드의 최종 스탯 계산
/// </summary>
public static class CardStatsCalculator
{
    private const int EnhanceBonusPerLevel = 2;

    public static int CalculateFinalStat(int baseStat, int enhanceLevel, int trainDelta)
    {
        return baseStat + enhanceLevel * EnhanceBonusPerLevel + trainDelta;
    }
}
