using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 팀 1개의 리그 성적 (승·패·무 + 득실 + 상대전적)
/// </summary>
public class TeamRecord
{
    public string TeamName { get; }

    public int Wins { get; private set; }
    public int Losses { get; private set; }
    public int Draws { get; private set; }

    public int RunsScored { get; private set; }
    public int RunsAllowed { get; private set; }

    public int GamePlayedCount => Wins + Losses + Draws;
    public int RunDifference => RunsScored - RunsAllowed;

    //KBO 방식 - 무승부는 승률 계산에서 제외 (기획서 7.7)
    public float WinRate => Wins + Losses == 0 ? 0f : (float)Wins / (Wins + Losses);

    //타이브레이커 3순위(상대전적)용 - 상대 팀명별 승/패
    private readonly Dictionary<string, int> _winsAgainst = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _lossesAgainst = new Dictionary<string, int>();

    public TeamRecord(string teamName)
    {
        TeamName = teamName;
    }

    //세이브 복원 전용 생성자 - 경기를 다시 치르지 않고 누적값을 그대로 되살린다
    public TeamRecord(string teamName, int wins, int losses, int draws, int runsScored, int runsAllowed,
        string[] opponentNames, int[] winsAgainst, int[] lossesAgainst)
    {
        TeamName = teamName;
        Wins = wins;
        Losses = losses;
        Draws = draws;
        RunsScored = runsScored;
        RunsAllowed = runsAllowed;

        if (opponentNames == null || winsAgainst == null || lossesAgainst == null)
            return;

        //세 배열은 인덱스가 서로 대응하므로 길이가 어긋나면 상대전적을 신뢰할 수 없음
        if (opponentNames.Length != winsAgainst.Length || opponentNames.Length != lossesAgainst.Length)
        {
            Debug.LogError($"[TeamRecord]: {teamName}의 상대전적 배열 길이가 어긋나 복원하지 않았습니다");
            return;
        }

        for (int i = 0; i < opponentNames.Length; i++)
        {
            _winsAgainst[opponentNames[i]] = winsAgainst[i];
            _lossesAgainst[opponentNames[i]] = lossesAgainst[i];
        }
    }

    //경기 1건 반영 (득점 · 실점은 이 팀 기준)
    public void AddResult(string opponentTeamName, int runsScored, int runsAllowed)
    {
        RunsScored += runsScored;
        RunsAllowed += runsAllowed;

        if (runsScored > runsAllowed)
        {
            Wins++;
            AddCount(_winsAgainst, opponentTeamName);
        }
        else if (runsScored < runsAllowed)
        {
            Losses++;
            AddCount(_lossesAgainst, opponentTeamName);
        }
        else
        {
            Draws++;
        }
    }

    //해당 상대에게 거둔 승수
    public int GetWinsAgainst(string opponentTeamName)
    {
        return _winsAgainst.TryGetValue(opponentTeamName, out int count) ? count : 0;
    }

    //해당 상대에게 당한 패수
    public int GetLossesAgainst(string opponentTeamName)
    {
        return _lossesAgainst.TryGetValue(opponentTeamName, out int count) ? count : 0;
    }

    //팀명별 카운터 1 증가
    private static void AddCount(Dictionary<string, int> counter, string teamName)
    {
        counter.TryGetValue(teamName, out int count);
        counter[teamName] = count + 1;
    }
}
