using System;
using UnityEngine;
/// <summary>
/// 티어 1개의 리그 설정
/// </summary>

[Serializable]
public struct LeagueTierEntry
{
    public AiRosterSet RosterSet => _rosterSet;
    public int StatBonus => _statBonus;
    public int GameCount => _gameCount;
    public int SeriesLength => _seriesLength;

    [SerializeField]
    private AiRosterSet _rosterSet;

    [SerializeField]
    private int _statBonus;

    [Tooltip("정규시즌 경기 수 (기획서 7.2)")]
    [SerializeField]
    private int _gameCount;

    [Tooltip("같은 팀과 연속으로 치르는 경기 수. 프로 리그부터 3 (기획서 7.3)")]
    [SerializeField]
    private int _seriesLength;
}
