using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리그 정규시즌 진행도 저장·복원
/// </summary>
public class LeagueSaveService
{
    public const string SaveKey = "league";

    private readonly ISaveStorage _storage;

    public LeagueSaveService(ISaveStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// 저장된 리그가 있는지
    /// </summary>
    public bool HasSave()
    {
        return _storage.Exists(SaveKey);
    }

    /// <summary>
    /// 정규시즌 진행도 저장
    /// </summary>
    public bool Save(LeagueSeason season)
    {
        if (season == null)
        {
            Debug.LogError("[LeagueSaveService]: 저장할 시즌이 없습니다");
            return false;
        }

        IReadOnlyList<TeamRecord> records = season.Standings.Records;

        LeagueSaveData saveData = new LeagueSaveData
        {
            Tier = (int)season.Tier,
            PlayerTeamName = season.PlayerTeamName,
            TeamNames = BuildTeamNames(records),
            GameCount = season.TotalDayCount,
            SeriesLength = season.Schedule.SeriesLength,
            CurrentDayIndex = season.CurrentDayIndex,
            RewardsGranted = season.RewardsGranted,
            Records = new TeamRecordSaveData[records.Count]
        };

        for (int i = 0; i < records.Count; i++)
        {
            saveData.Records[i] = ToSaveData(records[i], saveData.TeamNames);
        }

        return _storage.Save(SaveKey, JsonUtility.ToJson(saveData, true));
    }

    /// <summary>
    /// 저장된 정규시즌 복원. 없거나 깨졌으면 null
    /// </summary>
    public LeagueSeason Load()
    {
        string json = _storage.Load(SaveKey);

        if (string.IsNullOrEmpty(json))
            return null;

        LeagueSaveData saveData = JsonUtility.FromJson<LeagueSaveData>(json);

        if (saveData == null || saveData.TeamNames == null || saveData.Records == null)
        {
            Debug.LogError("[LeagueSaveService]: 세이브 데이터를 읽지 못했습니다");
            return null;
        }

        if (saveData.TeamNames.Length < 2 || saveData.TeamNames[0] != saveData.PlayerTeamName)
        {
            Debug.LogError("[LeagueSaveService]: 세이브의 팀 목록이 올바르지 않습니다 (0번은 플레이어 팀이어야 함)");
            return null;
        }

        if (!LeagueTierTable.IsValidTier((LeagueTier)saveData.Tier))
        {
            Debug.LogError($"[LeagueSaveService]: 세이브의 티어 번호가 올바르지 않습니다 ({saveData.Tier})");
            return null;
        }

        //일정은 TeamNames로 재생성하고 순위표는 Records로 만든다. 둘이 어긋나면 매 경기 반영이 실패한다
        if (saveData.Records.Length != saveData.TeamNames.Length)
        {
            Debug.LogError($"[LeagueSaveService]: 팀 수({saveData.TeamNames.Length})와 성적 수({saveData.Records.Length})가 어긋납니다");
            return null;
        }

        List<string> opponentNames = new List<string>(saveData.TeamNames.Length - 1);

        for (int i = 1; i < saveData.TeamNames.Length; i++)
        {
            opponentNames.Add(saveData.TeamNames[i]);
        }

        LeagueSchedule schedule = LeagueScheduleGenerator.Generate(
            (LeagueTier)saveData.Tier, saveData.PlayerTeamName, opponentNames,
            saveData.GameCount, saveData.SeriesLength);

        if (schedule == null)
            return null;

        if (saveData.CurrentDayIndex < 0 || saveData.CurrentDayIndex > schedule.PlayerGameCount)
        {
            Debug.LogError($"[LeagueSaveService]: 진행도({saveData.CurrentDayIndex})가 일정 길이({schedule.PlayerGameCount})를 벗어났습니다");
            return null;
        }

        List<TeamRecord> records = new List<TeamRecord>(saveData.Records.Length);

        foreach (TeamRecordSaveData recordData in saveData.Records)
        {
            if (recordData == null)
            {
                Debug.LogError("[LeagueSaveService]: 성적 목록에 비어 있는 항목이 있습니다");
                return null;
            }

            if (recordData.GamePlayedCount != saveData.CurrentDayIndex)
            {
                Debug.LogError($"[LeagueSaveService]: '{recordData.TeamName}'의 경기 수({recordData.GamePlayedCount})가 진행도({saveData.CurrentDayIndex})와 어긋납니다");
                return null;
            }

            records.Add(ToTeamRecord(recordData));
        }

        return new LeagueSeason(schedule, new LeagueStandings(records), saveData.CurrentDayIndex, saveData.RewardsGranted);
    }

    /// <summary>
    /// 저장된 리그 삭제 (재도전 시작 시)
    /// </summary>
    public bool Delete()
    {
        return _storage.Delete(SaveKey);
    }

    //성적 목록 -> 팀 순서 배열
    private static string[] BuildTeamNames(IReadOnlyList<TeamRecord> records)
    {
        string[] teamNames = new string[records.Count];

        for (int i = 0; i < records.Count; i++)
        {
            teamNames[i] = records[i].TeamName;
        }

        return teamNames;
    }

    //성적 -> 저장 형태 (상대전적은 자기 자신을 뺀 나머지 팀 전부에 대해 기록)
    private static TeamRecordSaveData ToSaveData(TeamRecord record, string[] teamNames)
    {
        List<string> opponentNames = new List<string>(teamNames.Length - 1);
        List<int> winsAgainst = new List<int>(teamNames.Length - 1);
        List<int> lossesAgainst = new List<int>(teamNames.Length - 1);

        foreach (string teamName in teamNames)
        {
            if (teamName == record.TeamName)
                continue;

            opponentNames.Add(teamName);
            winsAgainst.Add(record.GetWinsAgainst(teamName));
            lossesAgainst.Add(record.GetLossesAgainst(teamName));
        }

        return new TeamRecordSaveData
        {
            TeamName = record.TeamName,
            Wins = record.Wins,
            Losses = record.Losses,
            Draws = record.Draws,
            RunsScored = record.RunsScored,
            RunsAllowed = record.RunsAllowed,
            OpponentNames = opponentNames.ToArray(),
            WinsAgainst = winsAgainst.ToArray(),
            LossesAgainst = lossesAgainst.ToArray()
        };
    }

    //저장 형태 -> 성적
    private static TeamRecord ToTeamRecord(TeamRecordSaveData recordData)
    {
        return new TeamRecord(recordData.TeamName, recordData.Wins, recordData.Losses, recordData.Draws,
            recordData.RunsScored, recordData.RunsAllowed,
            recordData.OpponentNames, recordData.WinsAgainst, recordData.LossesAgainst);
    }
}
