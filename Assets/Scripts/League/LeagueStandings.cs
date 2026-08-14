using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리그 순위표 (KBO 방식 - 승률 → 득실차 → 상대전적)
/// </summary>
public class LeagueStandings
{
    public IReadOnlyList<TeamRecord> Records => _records;

    //조회용 딕셔너리와 순서 보존용 리스트를 함께 둠 (딕셔너리 열거 순서는 보장되지 않음)
    private readonly Dictionary<string, TeamRecord> _recordLookup;
    private readonly List<TeamRecord> _records;

    public LeagueStandings(IReadOnlyList<string> teamNames)
    {
        _recordLookup = new Dictionary<string, TeamRecord>(teamNames.Count);
        _records = new List<TeamRecord>(teamNames.Count);

        foreach (string teamName in teamNames)
        {
            if (string.IsNullOrEmpty(teamName))
            {
                Debug.LogError("[LeagueStandings]: 팀명이 비어 있어 순위표에 넣지 않았습니다");
                continue;
            }

            if (_recordLookup.ContainsKey(teamName))
            {
                Debug.LogError($"[LeagueStandings]: '{teamName}' 팀이 두 번 들어와 한 번만 넣었습니다");
                continue;
            }

            TeamRecord record = new TeamRecord(teamName);
            _recordLookup.Add(teamName, record);
            _records.Add(record);
        }
    }

    //세이브 복원 전용 생성자 - 이미 값이 채워진 성적을 그대로 담는다
    public LeagueStandings(IReadOnlyList<TeamRecord> records)
    {
        _recordLookup = new Dictionary<string, TeamRecord>(records.Count);
        _records = new List<TeamRecord>(records.Count);

        foreach (TeamRecord record in records)
        {
            if (record == null || string.IsNullOrEmpty(record.TeamName))
            {
                Debug.LogError("[LeagueStandings]: 복원 데이터에 빈 성적이 있어 건너뛰었습니다");
                continue;
            }

            if (_recordLookup.ContainsKey(record.TeamName))
            {
                Debug.LogError($"[LeagueStandings]: 복원 데이터에 '{record.TeamName}' 팀이 두 번 들어 있습니다");
                continue;
            }

            _recordLookup.Add(record.TeamName, record);
            _records.Add(record);
        }
    }

    //경기 결과 1건을 양 팀 성적에 반영
    public bool ApplyGameResult(LeagueGame game, int homeScore, int awayScore)
    {
        TeamRecord homeRecord = GetRecord(game.HomeTeamName);
        TeamRecord awayRecord = GetRecord(game.AwayTeamName);

        //둘 중 하나라도 없으면 한쪽만 반영되어 순위표가 어긋나므로 아무것도 반영하지 않음
        if (homeRecord == null || awayRecord == null)
            return false;

        homeRecord.AddResult(game.AwayTeamName, homeScore, awayScore);
        awayRecord.AddResult(game.HomeTeamName, awayScore, homeScore);

        return true;
    }

    //팀명으로 성적 조회
    public TeamRecord GetRecord(string teamName)
    {
        if (_recordLookup.TryGetValue(teamName, out TeamRecord record))
            return record;

        Debug.LogWarning($"[LeagueStandings]: '{teamName}' 팀이 순위표에 없습니다");
        return null;
    }

    //순위 계산 (기획서 7.7 - 승률 → 득실차 → 상대전적)
    public LeagueStandingRow[] GetRanking()
    {
        List<TeamRecord> sorted = new List<TeamRecord>(_records);
        sorted.Sort(CompareByWinRateAndRunDifference);

        ResolveHeadToHead(sorted);

        LeagueStandingRow[] ranking = new LeagueStandingRow[sorted.Count];

        for (int i = 0; i < sorted.Count; i++)
        {
            ranking[i] = new LeagueStandingRow(i + 1, sorted[i], CalcGamesBehind(sorted[0], sorted[i]));
        }

        return ranking;
    }

    //1·2순위 비교 - 승률 내림차순, 같으면 득실차 내림차순. 팀명은 결과를 고정하기 위한 마지막 기준
    private static int CompareByWinRateAndRunDifference(TeamRecord left, TeamRecord right)
    {
        int byWinRate = right.WinRate.CompareTo(left.WinRate);

        if (byWinRate != 0)
            return byWinRate;

        int byRunDifference = right.RunDifference.CompareTo(left.RunDifference);

        if (byRunDifference != 0)
            return byRunDifference;

        return string.CompareOrdinal(left.TeamName, right.TeamName);
    }

    //승률·득실차까지 같은 구간을 찾아 상대전적으로 다시 정렬
    private static void ResolveHeadToHead(List<TeamRecord> sorted)
    {
        int start = 0;

        while (start < sorted.Count)
        {
            int end = start + 1;

            while (end < sorted.Count && IsTied(sorted[start], sorted[end]))
            {
                end++;
            }

            //동률이 2팀 이상일 때만 상대전적을 따짐
            if (end - start > 1)
            {
                List<TeamRecord> tiedGroup = sorted.GetRange(start, end - start);
                sorted.Sort(start, end - start, new HeadToHeadComparer(tiedGroup));
            }

            start = end;
        }
    }

    //승률과 득실차가 모두 같은지 (같은 계산식에서 나온 값이라 부동소수 비교가 안전)
    private static bool IsTied(TeamRecord left, TeamRecord right)
    {
        return left.WinRate == right.WinRate && left.RunDifference == right.RunDifference;
    }

    //1위 대비 게임차 = ((1위 승 - 팀 승) + (팀 패 - 1위 패)) / 2
    private static float CalcGamesBehind(TeamRecord leader, TeamRecord record)
    {
        return ((leader.Wins - record.Wins) + (record.Losses - leader.Losses)) / 2f;
    }

    /// <summary>
    /// 동률 구간 안에서만 쓰는 상대전적 비교기
    /// </summary>
    private class HeadToHeadComparer : IComparer<TeamRecord>
    {
        private readonly List<TeamRecord> _tiedGroup;

        public HeadToHeadComparer(List<TeamRecord> tiedGroup)
        {
            _tiedGroup = tiedGroup;
        }

        public int Compare(TeamRecord left, TeamRecord right)
        {
            int byHeadToHead = GetWinRateInGroup(right, _tiedGroup).CompareTo(GetWinRateInGroup(left, _tiedGroup));

            if (byHeadToHead != 0)
                return byHeadToHead;

            return string.CompareOrdinal(left.TeamName, right.TeamName);
        }

        //동률 구간에 속한 팀들만 상대로 계산한 승률 (무승부 제외)
        private static float GetWinRateInGroup(TeamRecord record, List<TeamRecord> tiedGroup)
        {
            int wins = 0;
            int losses = 0;

            foreach (TeamRecord other in tiedGroup)
            {
                if (ReferenceEquals(other, record))
                    continue;

                wins += record.GetWinsAgainst(other.TeamName);
                losses += record.GetLossesAgainst(other.TeamName);
            }

            return wins + losses == 0 ? 0f : (float)wins / (wins + losses);
        }
    }
}
