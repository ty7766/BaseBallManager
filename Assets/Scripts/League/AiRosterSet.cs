using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// AI 팀 10개를 묶은 로스터 세트
/// </summary>

[CreateAssetMenu(fileName = "AiRosterSet", menuName = "BaseBallManager/AI Roster Set")]
public class AiRosterSet : ScriptableObject
{
    public const int TeamCount = 10;

    public IReadOnlyList<AiTeamRosterData> Teams => _teams;

    [Header("팀 10개 - 플레이어 팀 포함")]
    [SerializeField]
    private AiTeamRosterData[] _teams = new AiTeamRosterData[TeamCount];
}
