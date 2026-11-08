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

        return ApplyDayScores(scores, playerGameResult);
    }

    /// <summary>
    /// 관전용 경기 1건의 시뮬 입력. 시즌이 끝났으면 null
    /// </summary>
    public SimulationContext BuildNextPlayerGameContext()
    {
        if (Season.IsFinished)
        {
            Debug.LogWarning($"[LeagueRunner]: {Season.Tier} 시즌이 이미 끝났습니다 ({Season.TotalDayCount}경기)");
            return null;
        }

        return BuildContext(Season.CurrentDay.PlayerGame);
    }

    /// <summary>
    /// 관전용 경기 1건을 열어 타석 단위로 전진시킬 세션을 돌려준다. 실패하면 null
    /// </summary>
    public GameSession BeginPlayerGame()
    {
        SimulationContext context = BuildNextPlayerGameContext();

        if (context == null)
            return null;

        return new GameSession(context, _simulator);
    }

    /// <summary>
    /// 관전이 끝난 내 경기 결과를 확정한다. 같은 날 AI 경기까지 시뮬해 순위표에 반영한다
    /// </summary>
    public LeagueDayResult CommitPlayerGame(GameResult playerGameResult)
    {
        if (playerGameResult == null)
        {
            Debug.LogError("[LeagueRunner]: 확정할 내 경기 결과가 없습니다");
            return null;
        }

        if (Season.IsFinished)
        {
            Debug.LogWarning($"[LeagueRunner]: {Season.Tier} 시즌이 이미 끝났습니다 ({Season.TotalDayCount}경기)");
            return null;
        }

        LeagueGameDay day = Season.CurrentDay;
        LeagueGameScore[] scores = new LeagueGameScore[day.Games.Count];

        for (int i = 0; i < day.Games.Count; i++)
        {
            LeagueGame game = day.Games[i];

            if (i == LeagueGameDay.PlayerGameIndex)
            {
                scores[i] = new LeagueGameScore(game, playerGameResult.HomeScore, playerGameResult.AwayScore);
                continue;
            }

            SimulationContext context = BuildContext(game);

            if (context == null)
                return null;

            GameResult result = _simulator.SimulateGame(context);
            scores[i] = new LeagueGameScore(game, result.HomeScore, result.AwayScore);
        }

        return ApplyDayScores(scores, playerGameResult);
    }

    //하루치 점수를 순위표에 반영하고 결과를 확정한다
    private LeagueDayResult ApplyDayScores(LeagueGameScore[] scores, GameResult playerGameResult)
    {
        foreach (LeagueGameScore score in scores)
        {
            if (!Season.Standings.ApplyGameResult(score.Game, score.HomeScore, score.AwayScore))
            {
                Debug.LogError($"[LeagueRunner]: 순위표 반영에 실패했습니다 ({score.Game.HomeTeamName} vs {score.Game.AwayTeamName})");
                return null;
            }
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
