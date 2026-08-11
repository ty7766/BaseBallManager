using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// AI 팀 1개의 편성 데이터 (에디터 편집용)
/// </summary>

[CreateAssetMenu(fileName = "AiTeamRoster", menuName = "BaseBallManager/AI Team Roster")]
public class AiTeamRosterData : ScriptableObject
{
    public const int LineupSize = 9;
    public const int StartingPitcherCount = 5;
    public const int RelieverCount = 5;

    public string TeamName => _teamName;
    public IReadOnlyList<AiHitterSlot> Lineup => _lineup;
    public IReadOnlyList<int> StartingPitcherCardIds => _startingPitcherCardIds;
    public IReadOnlyList<int> RelieverCardIds => _relieverCardIds;
    public int CloserCardId => _closerCardId;

    [SerializeField]
    private string _teamName;

    [Header("타선 9명 - 배열 순서 = 타순")]
    [SerializeField]
    private AiHitterSlot[] _lineup = new AiHitterSlot[LineupSize];

    [Header("선발 5명 - 배열 순서 = 로테이션")]
    [SerializeField]
    private int[] _startingPitcherCardIds = new int[StartingPitcherCount];

    [Header("불펜 5명 - 배열 순서 = 등판 순서")]
    [SerializeField]
    private int[] _relieverCardIds = new int[RelieverCount];

    [Header("마무리")]
    [SerializeField]
    private int _closerCardId;
}
