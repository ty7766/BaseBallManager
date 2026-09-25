using System.Collections.Generic;

/// <summary>
/// 타석 종료 시점에 시뮬에 전달하는 교체 지시
/// </summary>
public class InterruptDecision
{
    public int PitcherSubstitutionSlot { get; }                             //해당 슬롯으로 교체
    public IReadOnlyList<HitterSubstitution> HitterSubstitutions { get; }   //대타 교체 목록

    public InterruptDecision(int pitcherSubstitutionSlot, IReadOnlyList<HitterSubstitution> hitterSubstitutions)
    {
        PitcherSubstitutionSlot = pitcherSubstitutionSlot;
        HitterSubstitutions = hitterSubstitutions;
    }
}
