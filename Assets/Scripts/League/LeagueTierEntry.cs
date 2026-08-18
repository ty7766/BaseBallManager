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
    public int GoldReward => _goldReward;

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

    [Tooltip("리그 종료 시 지급 골드의 기준값. 실제 지급액은 여기에 순위 배수를 곱한다 (기획서 7.9)")]
    [SerializeField]
    private int _goldReward;
}
