using UnityEngine;

/// <summary>
/// 베이스에 나가 있는 주자의 스냅샷을 공격팀 라인업에서 찾는다
/// </summary>
public static class RunnerLookup
{
    /// <summary>
    /// 주자 스냅샷. 못 찾으면 경고를 남기고 기본값을 돌려준다
    /// </summary>
    public static HitterSnapshot Find(int instanceId, SimulationContext context, bool isTopInning)
    {
        HitterSnapshot[] lineup = isTopInning ? context.AwayLineup : context.HomeLineup;

        foreach (HitterSnapshot hitter in lineup)
        {
            if (hitter.InstanceId == instanceId)
                return hitter;
        }

        Debug.LogWarning($"[RunnerLookup]: 베이스 주자를 라인업에서 찾지 못했습니다 (instanceId {instanceId}). 주루 스탯 0으로 처리합니다");

        return default;
    }
}
