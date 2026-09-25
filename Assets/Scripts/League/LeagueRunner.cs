using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리그 경기 진행 (1경기 단위 시뮬 + 순위표 반영)
/// </summary>
public class LeagueRunner
{
    public LeagueSeason Season { get; }
    public LeagueDayResult LastDayResult { get; private set; }

    public LeagueGameContextFactory ContextFactory { get; }

    private readonly GameSimulator _simulator;

    public LeagueRunner(LeagueSeason season, IReadOnlyDictionary<string, AiTeamRoster> rosters,
        int pullThreshold = 3, IGameInterruptHandler interruptHandler = null)
    {
        Season = season;
        ContextFactory = new LeagueGameContextFactory(rosters, season.PlayerTeamName);
        _simulator = new GameSimulator(pullThreshold, interruptHandler);
    }

    /// <summary>
    /// 경기 1개 진행 (내 경기 1건 + AI끼리 4경기). 시즌이 끝났거나 실패하면 null
    /// </summary>
    public LeagueDayResult SimulateNextGame()
    {
        if (Season.IsFinished)
        {
            Debug.LogWarning($"[LeagueRunner]: {Season.Tier} 시즌이 이미 끝났습니다 ({Season.TotalDayCount}경기)");
            return null;
        }

        LeagueGameDay day = Season.CurrentDay;
        LeagueGameScore[] scores = new LeagueGameScore[day.Games.Count];
        GameResult playerGameResult = null;

        for (int i = 0; i < day.Games.Count; i++)
        {
            LeagueGame game = day.Games[i];
            bool isPlayerGame = i == LeagueGameDay.PlayerGameIndex;

            SimulationContext context = BuildContext(game);

            if (context == null)
                return null;

            GameResult result = _simulator.SimulateGame(context);
            scores[i] = new LeagueGameScore(game, result.HomeScore, result.AwayScore);

            if (isPlayerGame)
                playerGameResult = result;
        }

        foreach (LeagueGameScore score in scores)
        {
            Season.Standings.ApplyGameResult(score.Game, score.HomeScore, score.AwayScore);
        }

        LastDayResult = new LeagueDayResult(Season.CurrentDayIndex, scores, playerGameResult);
        Season.AdvanceDay();

        return LastDayResult;
    }

    //경기 1건의 시뮬 입력 구성
    private SimulationContext BuildContext(LeagueGame game)
    {
        return ContextFactory.Create(game, GetPlayedGameCount(game.HomeTeamName), GetPlayedGameCount(game.AwayTeamName));
    }

    //해당 팀이 지금까지 치른 경기 수
    private int GetPlayedGameCount(string teamName)
    {
        TeamRecord record = Season.Standings.GetRecord(teamName);

        return record == null ? 0 : record.GamePlayedCount;
    }
}
