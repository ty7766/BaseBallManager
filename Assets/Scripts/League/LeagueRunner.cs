using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리그 경기 진행 (1경기 단위 시뮬 + 순위표 반영)
/// </summary>
/// <remarks>
/// 진행 단위는 항상 "내 경기 1건 + 같은 날 AI끼리 4경기"다 (기획서 7.4).
/// AI 경기를 함께 돌리는 이유는 순위표가 10팀 전체의 승·패·무를 요구하기 때문이다 (기획서 7.7).
/// </remarks>
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

    //경기 1개 진행 (내 경기 1건 + AI끼리 4경기). 시즌이 끝났거나 실패하면 null
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

        //① 5경기를 모두 시뮬한 뒤 ② 순위표에 한꺼번에 반영
        //중간에 실패했을 때 하루가 반만 반영되어 팀별 경기 수가 어긋나는 것을 막음
        for (int i = 0; i < day.Games.Count; i++)
        {
            LeagueGame game = day.Games[i];
            bool isPlayerGame = i == LeagueGameDay.PlayerGameIndex;

            SimulationContext context = BuildContext(game);

            //원인 로그는 컨텍스트 빌더가 남김
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
        //선발 로테이션은 그 팀이 지금까지 치른 경기 수를 따라감 (기획서 6.2 - 3연전에서도 연속)
        return ContextFactory.Create(game, GetPlayedGameCount(game.HomeTeamName), GetPlayedGameCount(game.AwayTeamName));
    }

    //해당 팀이 지금까지 치른 경기 수
    private int GetPlayedGameCount(string teamName)
    {
        TeamRecord record = Season.Standings.GetRecord(teamName);

        return record == null ? 0 : record.GamePlayedCount;
    }
}
