using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리그 경기 진행 (하루 단위 시뮬 + 순위표 반영)
/// </summary>
public class LeagueRunner
{
    public LeagueSeason Season { get; }
    public LeagueDayResult LastDayResult { get; private set; }

    public LeagueGameContextFactory ContextFactory { get; }

    private readonly GameSimulator _simulator;

    public LeagueRunner(LeagueSeason season, IReadOnlyDictionary<string, AiTeamRoster> rosters,
        bool useAiRosterForPlayerTeam, int pullThreshold = 3, IGameInterruptHandler interruptHandler = null)
    {
        Season = season;
        ContextFactory = new LeagueGameContextFactory(rosters, season.PlayerTeamName, useAiRosterForPlayerTeam);
        _simulator = new GameSimulator(pullThreshold, interruptHandler);
    }

    //하루치(5경기) 진행. 시즌이 끝났거나 실패하면 null
    public LeagueDayResult SimulateNextDay()
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

    //여러 날 한꺼번에 진행 (기획서 7.4 일괄 시뮬). 반환값은 실제 진행한 일수
    public int SimulateDays(int dayCount)
    {
        if (dayCount <= 0)
        {
            Debug.LogWarning($"[LeagueRunner]: 진행할 경기 수가 {dayCount}입니다");
            return 0;
        }

        int simulatedCount = 0;

        //중간 날짜의 타석 로그는 보관하지 않음 - 일괄 시뮬은 박스스코어 위주 (기획서 8.5)
        for (int i = 0; i < dayCount; i++)
        {
            if (Season.IsFinished)
                break;

            if (SimulateNextDay() == null)
                break;

            simulatedCount++;
        }

        return simulatedCount;
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
