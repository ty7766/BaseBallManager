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

    [SerializeField]
    private AiRosterSet _rosterSet;

    [SerializeField]
    private int _statBonus;
}
