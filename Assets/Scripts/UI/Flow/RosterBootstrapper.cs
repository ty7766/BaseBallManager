using UnityEngine;

/// <summary>
/// 새 게임 직후 리그에 입장할 수 있는 최소 로스터를 만든다.
/// 뽑기·인벤토리 UI가 아직 없으므로 튜토리얼 보상으로 일괄 뽑고 라인업을 자동 편성한다
/// </summary>
public static class RosterBootstrapper
{
    private const int RollsPerBatch = 10;
    private const int FirstBatchCount = 10;
    private const int RetryBatchCount = 5;

    /// <summary>
    /// 라인업이 완성되면 true. 보유 카드가 부족하면 false
    /// </summary>
    public static bool Prepare()
    {
        if (GameFlowManager.Instance == null || GachaManager.Instance == null
            || CurrencyManager.Instance == null || LineUpManager.Instance == null)
        {
            Debug.LogError("[RosterBootstrapper]: 필요한 매니저가 씬에 없습니다 (GameFlow / Gacha / Currency / LineUp)");
            return false;
        }

        if (LineUpManager.Instance.IsLineupComplete())
            return true;

        GameFlowManager.Instance.CompleteTutorial();

        RollBatches(FirstBatchCount);

        if (LineUpAutoFill.Fill())
            return true;

        for (int retry = 0; retry < 3; retry++)
        {
            if (RollBatches(RetryBatchCount) == 0)
                break;

            if (LineUpAutoFill.Fill())
                return true;
        }

        Debug.LogError("[RosterBootstrapper]: 뽑기권이 모자라 라인업을 채우지 못했습니다");

        return false;
    }

    //10연차를 batchCount번 돌린다. 실제로 돌린 횟수를 돌려준다
    private static int RollBatches(int batchCount)
    {
        int rolled = 0;

        for (int i = 0; i < batchCount; i++)
        {
            if (!CurrencyManager.Instance.CanAfford(CurrencyType.NormalTicket, RollsPerBatch))
                break;

            if (InventoryManager.Instance != null && InventoryManager.Instance.IsFull)
                break;

            if (GachaManager.Instance.Roll10(GachaType.Normal).Count == 0)
                break;

            rolled++;
        }

        return rolled;
    }
}
