using System.Collections.Generic;

/// <summary>
/// 이번 타석 후 시뮬레이션 호출 (교체 인터럽트)
/// </summary>

public class InterruptDecision
{
    public int PitcherSubstitutionSlot { get; }    //해당 슬롯으로 교체
    public IReadOnlyList<Hittersubstitution> HitterSubstitutions    { get; }    //대타 교체 목록

    public InterruptDecision(int pitcherSubstitutionSlot, IReadOnlyList<Hittersubstitution> hitterSubstitutions)
    {
        PitcherSubstitutionSlot = pitcherSubstitutionSlot;
        HitterSubstitutions = hitterSubstitutions;
    }
}
