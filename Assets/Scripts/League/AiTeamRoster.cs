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
    public PitcherSnapshot Closer {  get; }               //CP 1명

    public AiTeamRoster(string teamName, HitterSnapshot[] lineup, PitcherSnapshot[] startingPitchers, PitcherSnapshot[] relievePitchers, PitcherSnapshot closer)
    {
        TeamName = teamName;
        Lineup = lineup;
        StartingPitchers = startingPitchers;
        RelievePitchers = relievePitchers;
        Closer = closer;
    }

    //로테이션에 맞는 투수진 7칸 조립
    public PitcherSnapshot[] GetPitcherStaff(int rotationIndex)
    {
        PitcherSnapshot[] allPitchers = new PitcherSnapshot[7];

        allPitchers[0] = StartingPitchers[rotationIndex % StartingPitchers.Length];
        Array.Copy(RelievePitchers, 0, allPitchers, 1, 5);
        allPitchers[6] = Closer;

        return allPitchers;
    }
}
