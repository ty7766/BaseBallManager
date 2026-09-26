using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 티어에 맞는 팀 로스터를 세트 단위로 생성해 보관 (플레이어 팀 포함 10팀)
/// </summary>
public class AiRosterManager : SingletonBehaviour<AiRosterManager>
{
    public LeagueTier CurrentTier => _currentTier;
    public int TeamCount => _rosters.Count;
    public IReadOnlyDictionary<string, AiTeamRoster> Rosters => _rosters;

    [Header("티어 -> 로스터 세트 + 능력치 보정 테이블")]
    [SerializeField]
    private LeagueTierTable _tierTable;

    //팀명 -> 완성된 로스터. 리그 시작 시 1회 채우고 리그 내내 재사용
    private readonly Dictionary<string, AiTeamRoster> _rosters = new Dictionary<string, AiTeamRoster>(AiRosterSet.TeamCount);

    //로스터 세트의 배치 순서 보존용. 이 순서가 일정 대진을 결정하므로 딕셔너리 열거에 맡기면 안 된다
    private readonly List<string> _teamNamesInOrder = new List<string>(AiRosterSet.TeamCount);
    private LeagueTier _currentTier;

    /// <summary>
    /// 티어에 맞는 AI 로스터 전체 생성 (리그 시작 시 1회 호출)
    /// </summary>
    public bool BuildRosters(LeagueTier tier)
    {
        if (_tierTable == null)
        {
            Debug.LogError("[AiRosterManager]: 티어 테이블이 연결되어 있지 않습니다");
            return false;
        }

        AiRosterSet rosterSet = _tierTable.GetRosterSet(tier);

        if (rosterSet == null)
        {
            Debug.LogError($"[AiRosterManager]: {tier} 티어에 로스터 세트가 연결되어 있지 않습니다");
            return false;
        }

        int tierStatBonus = _tierTable.GetStatBonus(tier);
        IReadOnlyList<AiTeamRosterData> teams = rosterSet.Teams;

        _rosters.Clear();
        _teamNamesInOrder.Clear();

        for (int i = 0; i < teams.Count; i++)
        {
            AiTeamRosterData teamData = teams[i];

            if (teamData == null)
            {
                Debug.LogError($"[AiRosterManager]: {tier} 티어 로스터 세트의 {i + 1}번 칸이 비어 있습니다");
                ClearRosters();
                return false;
            }

            AiTeamRoster roster = AiRosterBuilder.BuildTeam(teamData, tierStatBonus);

            if (roster == null)
            {
                ClearRosters();
                return false;
            }

            if (_rosters.ContainsKey(roster.TeamName))
            {
                Debug.LogError($"[AiRosterManager]: {tier} 티어 로스터 세트에 '{roster.TeamName}' 팀이 두 번 들어 있습니다");
                ClearRosters();
                return false;
            }

            _rosters.Add(roster.TeamName, roster);
            _teamNamesInOrder.Add(roster.TeamName);
        }

        _currentTier = tier;

        Debug.Log($"[AiRosterManager]: {tier} 티어 로스터 {_rosters.Count}팀 생성 완료 (능력치 보정 +{tierStatBonus})");

        return true;
    }

    /// <summary>
    /// 플레이어 팀을 뺀 나머지 팀명 (나를 제외한 9팀)
    /// </summary>
    public List<string> GetOpponentTeamNames(string playerTeamName)
    {
        List<string> opponentNames = new List<string>(_teamNamesInOrder.Count);

        foreach (string teamName in _teamNamesInOrder)
        {
            if (teamName == playerTeamName)
                continue;

            opponentNames.Add(teamName);
        }

        return opponentNames;
    }

    /// <summary>
    /// 팀명으로 AI 로스터 조회
    /// </summary>
    public AiTeamRoster GetRoster(string teamName)
    {
        if (_rosters.TryGetValue(teamName, out AiTeamRoster roster))
            return roster;

        Debug.LogWarning($"[AiRosterManager]: '{teamName}' 팀의 AI 로스터를 찾을 수 없습니다");
        return null;
    }

    /// <summary>
    /// 생성된 팀명 전체 반환 (로스터 세트의 배치 순서 그대로)
    /// </summary>
    public IReadOnlyList<string> GetTeamNames()
    {
        return _teamNamesInOrder;
    }

    //실패 시 부분 생성 상태를 남기지 않는다
    private void ClearRosters()
    {
        _rosters.Clear();
        _teamNamesInOrder.Clear();
    }
}
