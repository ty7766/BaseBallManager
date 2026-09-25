using System;
using UnityEngine;

/// <summary>
/// 등판 중인 투수 1명의 경기 중 상태 (체력·투구 수·이닝 실점)
/// </summary>
public class PitcherState
{
    private const float FatigueStartRatio = 0.5f;
    private const float MaxFatiguePenalty = 0.80f;

    public PitcherSnapshot Snapshot { get; }

    /// <summary>
    /// 투수진 배열에서의 슬롯 번호. 0 = SP / 1~5 = RP / 6 = CP
    /// </summary>
    public int PitcherSlotIndex { get; }

    public int CurrentStamina { get; private set; }     //남은 체력
    public int TotalPitchCount { get; private set; }    //경기 전체 투구 수
    public int CurrentInningRuns { get; private set; }  //이번 이닝 실점

    public PitcherState(PitcherSnapshot snapshot, int pitcherSlotIndex)
    {
        Snapshot = snapshot;
        PitcherSlotIndex = pitcherSlotIndex;
        CurrentStamina = Snapshot.Stamina;
    }

    /// <summary>
    /// 남은 체력 비율 (1 = 온전, 0 = 소진)
    /// </summary>
    public float GetFatigueRatio()
    {
        return (float)CurrentStamina / Snapshot.Stamina;
    }

    /// <summary>
    /// 투구 수를 누적하고 그만큼 체력을 깎는다
    /// </summary>
    public void ConsumePitches(int pitchCount)
    {
        TotalPitchCount += pitchCount;
        CurrentStamina = Math.Max(0, CurrentStamina - pitchCount);
    }

    /// <summary>
    /// 이번 이닝 실점 1 증가
    /// </summary>
    public void AddInningRun()
    {
        CurrentInningRuns++;
    }

    /// <summary>
    /// 이닝이 바뀔 때 이번 이닝 실점을 초기화한다
    /// </summary>
    public void ResetInningStats()
    {
        CurrentInningRuns = 0;
    }

    /// <summary>
    /// 체력 소모를 반영한 스냅샷. 체력 절반부터 구위·제구·구속이 최대 20% 깎인다
    /// </summary>
    public PitcherSnapshot GetFatiguedSnapshot()
    {
        float staminaRatio = GetFatigueRatio();
        float fatigue = 1.0f;

        if (staminaRatio <= FatigueStartRatio)
        {
            float t = (FatigueStartRatio - staminaRatio) * 2.0f;
            fatigue = Mathf.Lerp(1.0f, MaxFatiguePenalty, t);
        }

        return new PitcherSnapshot(Snapshot.InstanceId, Snapshot.Name,
            (int)(Snapshot.Velo * fatigue),
            (int)(Snapshot.Stuff * fatigue),
            (int)(Snapshot.Control * fatigue),
            Snapshot.Stamina);
    }
}
