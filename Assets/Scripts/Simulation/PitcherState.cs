using System;

/// <summary>
/// 시뮬레이션 내 등판 중인 투수의 상태 추적
/// 1. 스냅샷 추적
/// 2. 체력, 투구 수, 이닝, 실점 변수 등 기록용
/// </summary>
public class PitcherState
{
    public PitcherSnapshot Snapshot { get; }

    /// <summary>
    /// 0 = SP
    /// 1~5 = RP
    /// 6 = CP
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

    //현재 체력 비율 반환
    public float GetFatigueRatio()
    {
        return (float)CurrentStamina / Snapshot.Stamina;
    }

    //투구 수 만큼 총 투구 수를 누적하고 체력을 소모
    public void ConsumePitches(int pitchCount)
    {
        TotalPitchCount += pitchCount;
        CurrentStamina = Math.Max(0, CurrentStamina - pitchCount);
    }

    //이번 이닝 실점 1 증가
    public void AddInningRun()
    {
        CurrentInningRuns++;
    }

    //이닝이 바뀔 때 이번 이닝 실점 초기화
    public void ResetInningStats()
    {
        CurrentInningRuns = 0;
    }
}
