using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포스트시즌 진행도 저장·복원 (기획서 7.5 · 7.6 · 10장)
/// </summary>
public class PostSeasonSaveService
{
    public const string SaveKey = "postseason";

    private readonly ISaveStorage _storage;

    public PostSeasonSaveService(ISaveStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// 저장된 포스트시즌이 있는지 (이어하기 버튼 노출 판단용)
    /// </summary>
    public bool HasSave()
    {
        return _storage.Exists(SaveKey);
    }

    /// <summary>
    /// 포스트시즌 진행도 저장
    /// </summary>
    public bool Save(PostSeasonRunner postSeason, LeagueTier tier, string playerTeamName)
    {
        if (postSeason == null)
        {
            Debug.LogError("[PostSeasonSaveService]: 저장할 포스트시즌이 없습니다");
            return false;
        }

        IReadOnlyList<PostSeasonSeries> series = postSeason.Series;

        if (series.Count == 0)
        {
            Debug.LogError("[PostSeasonSaveService]: 시리즈가 없는 포스트시즌은 저장하지 않습니다 (복원할 수 없음)");
            return false;
        }

        if (!LeagueTierTable.IsValidTier(tier))
        {
            Debug.LogError($"[PostSeasonSaveService]: 저장할 티어가 올바르지 않습니다 ({(int)tier})");
            return false;
        }

        PostSeasonSaveData saveData = new PostSeasonSaveData
        {
            Tier = (int)tier,
            PlayerTeamName = playerTeamName,
            CurrentSeriesIndex = postSeason.CurrentSeriesIndex,
            Series = new PostSeasonSeriesSaveData[series.Count]
        };

        for (int i = 0; i < series.Count; i++)
        {
            saveData.Series[i] = ToSaveData(series[i]);
        }

        WriteRotation(saveData, postSeason.RotationIndices);

        return _storage.Save(SaveKey, JsonUtility.ToJson(saveData, true));
    }

    /// <summary>
    /// 저장된 포스트시즌 데이터 읽기. 없거나 깨졌으면 null
    /// </summary>
    public PostSeasonSaveData Load()
    {
        string json = _storage.Load(SaveKey);

        if (string.IsNullOrEmpty(json))
            return null;

        PostSeasonSaveData saveData = JsonUtility.FromJson<PostSeasonSaveData>(json);

        if (saveData == null || saveData.Series == null)
        {
            Debug.LogError("[PostSeasonSaveService]: 세이브 데이터를 읽지 못했습니다");
            return null;
        }

        if (!LeagueTierTable.IsValidTier((LeagueTier)saveData.Tier))
        {
            Debug.LogError($"[PostSeasonSaveService]: 세이브의 티어 번호가 올바르지 않습니다 ({saveData.Tier})");
            return null;
        }

        if (string.IsNullOrEmpty(saveData.PlayerTeamName))
        {
            Debug.LogError("[PostSeasonSaveService]: 세이브에 플레이어 팀이 없습니다");
            return null;
        }

        return saveData;
    }

    /// <summary>
    /// 저장된 포스트시즌 삭제 (재도전으로 새 리그를 시작할 때)
    /// </summary>
    public bool Delete()
    {
        return _storage.Delete(SaveKey);
    }

    //시리즈 -> 저장 형태
    private static PostSeasonSeriesSaveData ToSaveData(PostSeasonSeries series)
    {
        IReadOnlyList<LeagueGameScore> scores = series.Scores;
        PostSeasonGameSaveData[] games = new PostSeasonGameSaveData[scores.Count];

        for (int i = 0; i < scores.Count; i++)
        {
            LeagueGameScore score = scores[i];

            games[i] = new PostSeasonGameSaveData
            {
                HomeTeamName = score.Game.HomeTeamName,
                AwayTeamName = score.Game.AwayTeamName,
                HomeScore = score.HomeScore,
                AwayScore = score.AwayScore
            };
        }

        return new PostSeasonSeriesSaveData
        {
            Round = (int)series.Round,
            HigherSeedTeamName = series.HigherSeedTeamName,
            LowerSeedTeamName = series.LowerSeedTeamName,
            WinsToClinch = series.WinsToClinch,
            HigherSeedWins = series.HigherSeedWins,
            LowerSeedWins = series.LowerSeedWins,
            Games = games
        };
    }

    //팀별 누적 경기 수를 나란한 배열 2개로 편다 (JsonUtility가 Dictionary를 못 다룸)
    private static void WriteRotation(PostSeasonSaveData saveData, IReadOnlyDictionary<string, int> rotationIndices)
    {
        saveData.RotationTeamNames = new string[rotationIndices.Count];
        saveData.RotationGameCounts = new int[rotationIndices.Count];

        int index = 0;

        foreach (KeyValuePair<string, int> pair in rotationIndices)
        {
            saveData.RotationTeamNames[index] = pair.Key;
            saveData.RotationGameCounts[index] = pair.Value;
            index++;
        }
    }
}
