using System;
/// <summary>
/// AI 팀 1개의 고정 로스터
/// </summary>
public class AiTeamRoster
{
    public string TeamName { get; }

    public HitterSnapshot[] Lineup { get; }                 //야수 9명
    public PitcherSnapshot[] StartingPitchers { get; }      //SP 5명
    public PitcherSnapshot[] RelievePitchers { get; }       //RP 5명
    public PitcherSnapshot Closer { get; }                  //CP 1명

    public AiTeamRoster(string teamName, HitterSnapshot[] lineup, PitcherSnapshot[] startingPitchers,
        PitcherSnapshot[] relievePitchers, PitcherSnapshot closer)
    {
        TeamName = teamName;
        Lineup = lineup;
        StartingPitchers = startingPitchers;
        RelievePitchers = relievePitchers;
        Closer = closer;
    }

    /// <summary>
    /// 로테이션 차례에 맞춰 투수진 7칸을 조립한다
    /// </summary>
    public PitcherSnapshot[] GetPitcherStaff(int rotationIndex)
    {
        PitcherSnapshot[] allPitchers = new PitcherSnapshot[SimulationContext.PitcherSlotCount];

        allPitchers[SimulationContext.StartingPitcherSlot] =
            StartingPitchers[rotationIndex % StartingPitchers.Length];

        Array.Copy(RelievePitchers, 0, allPitchers,
            SimulationContext.StartingPitcherSlot + 1, RelievePitchers.Length);

        allPitchers[SimulationContext.CloserSlot] = Closer;

        return allPitchers;
    }
}
