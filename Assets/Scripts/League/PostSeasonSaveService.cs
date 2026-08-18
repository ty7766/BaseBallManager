using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포스트시즌 진행도 저장·복원 (기획서 7.5 · 7.6 · 10장)
/// </summary>
/// <remarks>
/// 정규시즌 세이브(<see cref="LeagueSaveService"/>)와 키를 분리한다.
/// 수명이 다르기 때문이다 - 포스트시즌은 정규시즌이 끝난 뒤에야 생기고, 재도전 시 먼저 버려진다.
/// 개별 경기는 저장하지 않는다(기획서 7.6). 경기 도중 나가면 그 경기는 처음부터 다시 한다.
/// </remarks>
public class PostSeasonSaveService
{
    public const string SaveKey = "postseason";

    private readonly ISaveStorage _storage;

    public PostSeasonSaveService(ISaveStorage storage)
    {
        _storage = storage;
    }

    //저장된 포스트시즌이 있는지 (이어하기 버튼 노출 판단용)
    public bool HasSave()
    {
        return _storage.Exists(SaveKey);
    }

    //포스트시즌 진행도 저장
    public bool Save(PostSeasonRunner postSeason, LeagueTier tier, string playerTeamName)
    {
        if (postSeason == null)
        {
            Debug.LogError("[PostSeasonSaveService]: 저장할 포스트시즌이 없습니다");
            return false;
        }

        IReadOnlyList<PostSeasonSeries> series = postSeason.Series;

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

    //저장된 포스트시즌 데이터 읽기. 없거나 깨졌으면 null
    public PostSeasonSaveData Load()
    {
        string json = _storage.Load(SaveKey);

        //세이브가 없는 것은 정상 상태(포스트시즌에 아직 진출하지 않음)
        if (string.IsNullOrEmpty(json))
            return null;

        PostSeasonSaveData saveData = JsonUtility.FromJson<PostSeasonSaveData>(json);

        if (saveData == null || saveData.Series == null)
        {
            Debug.LogError("[PostSeasonSaveService]: 세이브 데이터를 읽지 못했습니다");
            return null;
        }

        return saveData;
    }

    //저장된 포스트시즌 삭제 (재도전으로 새 리그를 시작할 때)
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
