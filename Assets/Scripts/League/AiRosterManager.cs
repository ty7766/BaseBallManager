using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 티어에 맞는 AI 팀 로스터 9개를 생성해 보관
/// </summary>
public class AiRosterManager : MonoBehaviour
{
    public static AiRosterManager Instance { get; private set; }

    public LeagueTier CurrentTier => _currentTier;
    public int TeamCount => _rosters.Count;

    [Header("티어 -> 로스터 세트 + 능력치 보정 테이블")]
    [SerializeField]
    private LeagueTierTable _tierTable;

    //팀명 -> 완성된 로스터. 리그 시작 시 1회 채우고 리그 내내 재사용
    private readonly Dictionary<string, AiTeamRoster> _rosters = new Dictionary<string, AiTeamRoster>(AiRosterSet.TeamCount);
    private LeagueTier _currentTier;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //티어에 맞는 AI 로스터 전체 생성 (리그 시작 시 1회 호출)
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

        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[AiRosterManager]: PlayerDataManager가 씬에 없습니다");
            return false;
        }

        string playerTeamName = PlayerDataManager.Instance.PlayerTeamName;

        if (string.IsNullOrEmpty(playerTeamName))
        {
            Debug.LogError("[AiRosterManager]: 플레이어 팀이 선택되지 않아 상대 팀을 가릴 수 없습니다");
            return false;
        }

        int tierStatBonus = _tierTable.GetStatBonus(tier);
        IReadOnlyList<AiTeamRosterData> teams = rosterSet.Teams;

        //티어를 바꿔 다시 부를 수 있으므로 이전 로스터를 먼저 버림
        _rosters.Clear();

        for (int i = 0; i < teams.Count; i++)
        {
            AiTeamRosterData teamData = teams[i];

            if (teamData == null)
            {
                Debug.LogError($"[AiRosterManager]: {tier} 티어 로스터 세트의 {i + 1}번 칸이 비어 있습니다");
                _rosters.Clear();
                return false;
            }

            //플레이어 팀은 AI가 쓰지 않음 (기획서 7.8 - 나를 제외한 9팀)
            if (teamData.TeamName == playerTeamName)
                continue;

            AiTeamRoster roster = AiRosterBuilder.BuildTeam(teamData, tierStatBonus);

            //편성 실패 - BuildTeam이 이미 원인을 로그로 남겼음
            if (roster == null)
            {
                _rosters.Clear();
                return false;
            }

            if (_rosters.ContainsKey(roster.TeamName))
            {
                Debug.LogError($"[AiRosterManager]: {tier} 티어 로스터 세트에 '{roster.TeamName}' 팀이 두 번 들어 있습니다");
                _rosters.Clear();
                return false;
            }

            _rosters.Add(roster.TeamName, roster);
        }

        _currentTier = tier;

        Debug.Log($"[AiRosterManager]: {tier} 티어 AI 로스터 {_rosters.Count}팀 생성 완료 (능력치 보정 +{tierStatBonus})");

        return true;
    }

    //팀명으로 AI 로스터 조회
    public AiTeamRoster GetRoster(string teamName)
    {
        if (_rosters.TryGetValue(teamName, out AiTeamRoster roster))
            return roster;

        Debug.LogWarning($"[AiRosterManager]: '{teamName}' 팀의 AI 로스터를 찾을 수 없습니다");
        return null;
    }

    //생성된 AI 팀명 전체 반환 (리그 일정 생성용)
    public IReadOnlyCollection<string> GetTeamNames()
    {
        return _rosters.Keys;
    }
}
