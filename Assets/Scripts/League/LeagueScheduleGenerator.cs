using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리그 일정 생성 (라운드 로빈 + 연전 + 홈/원정 교대)
/// </summary>
public static class LeagueScheduleGenerator
{
    //플레이어 1팀 + AI 9팀 일정 생성. 실패 시 null
    public static LeagueSchedule Generate(LeagueTier tier, string playerTeamName, IReadOnlyList<string> aiTeamNames, int gameCount, int seriesLength)
    {
        string[] teams = BuildTeamArray(playerTeamName, aiTeamNames);

        if (teams == null)
            return null;

        if (gameCount <= 0)
        {
            Debug.LogError($"[LeagueScheduleGenerator]: {tier} 티어의 경기 수가 {gameCount}입니다. 티어 테이블을 확인하세요");
            return null;
        }

        if (seriesLength <= 0)
        {
            Debug.LogError($"[LeagueScheduleGenerator]: {tier} 티어의 연전 수가 {seriesLength}입니다. 티어 테이블을 확인하세요");
            return null;
        }

        //연전이 중간에 끊기면 팀별 경기 수가 어긋나 순위표가 불공정해짐
        if (gameCount % seriesLength != 0)
        {
            Debug.LogError($"[LeagueScheduleGenerator]: {tier} 티어의 경기 수 {gameCount}가 연전 수 {seriesLength}로 나누어떨어지지 않습니다");
            return null;
        }

        LeagueGame[][] cycle = BuildRoundRobinCycle(teams);
        LeagueGameDay[] days = new LeagueGameDay[gameCount];

        int dayIndex = 0;

        //한 라운드를 seriesLength번 반복 = 같은 상대와 연전
        for (int unit = 0; unit < gameCount / seriesLength; unit++)
        {
            LeagueGame[] roundGames = cycle[unit % cycle.Length];

            //9라운드 사이클을 한 바퀴 돌 때마다 홈/원정을 통째로 뒤집어 팀별 홈경기 수를 맞춤
            bool flipHomeAway = (unit / cycle.Length) % 2 == 1;

            for (int series = 0; series < seriesLength; series++)
            {
                days[dayIndex] = BuildGameDay(roundGames, playerTeamName, dayIndex, flipHomeAway);
                dayIndex++;
            }
        }

        return new LeagueSchedule(tier, playerTeamName, seriesLength, days);
    }

    //플레이어 + AI 팀명을 한 배열로. 0번은 항상 플레이어 (회전 고정축)
    private static string[] BuildTeamArray(string playerTeamName, IReadOnlyList<string> aiTeamNames)
    {
        if (string.IsNullOrEmpty(playerTeamName))
        {
            Debug.LogError("[LeagueScheduleGenerator]: 플레이어 팀명이 비어 있습니다");
            return null;
        }

        if (aiTeamNames == null || aiTeamNames.Count != AiRosterSet.TeamCount - 1)
        {
            int count = aiTeamNames == null ? 0 : aiTeamNames.Count;
            Debug.LogError($"[LeagueScheduleGenerator]: AI 팀이 {count}개입니다 (필요: {AiRosterSet.TeamCount - 1})");
            return null;
        }

        string[] teams = new string[AiRosterSet.TeamCount];
        teams[0] = playerTeamName;

        HashSet<string> usedNames = new HashSet<string> { playerTeamName };

        for (int i = 0; i < aiTeamNames.Count; i++)
        {
            string teamName = aiTeamNames[i];

            if (string.IsNullOrEmpty(teamName))
            {
                Debug.LogError($"[LeagueScheduleGenerator]: {i + 1}번째 AI 팀명이 비어 있습니다");
                return null;
            }

            //같은 팀이 두 번 들어가면 하루에 자기 자신과 붙는 대진이 만들어짐
            if (!usedNames.Add(teamName))
            {
                Debug.LogError($"[LeagueScheduleGenerator]: '{teamName}' 팀이 두 번 들어 있습니다");
                return null;
            }

            teams[i + 1] = teamName;
        }

        return teams;
    }

    //원형 방식 라운드 로빈 - 9라운드 × 5경기, 모든 팀이 하루에 정확히 1경기
    private static LeagueGame[][] BuildRoundRobinCycle(string[] teams)
    {
        int roundCount = teams.Length - 1;
        int gamesPerDay = teams.Length / 2;

        LeagueGame[][] cycle = new LeagueGame[roundCount][];

        //0번(플레이어)을 고정하고 나머지를 회전시킴
        string[] rotation = new string[teams.Length];
        Array.Copy(teams, rotation, teams.Length);

        for (int round = 0; round < roundCount; round++)
        {
            LeagueGame[] games = new LeagueGame[gamesPerDay];

            for (int i = 0; i < gamesPerDay; i++)
            {
                string first = rotation[i];
                string second = rotation[teams.Length - 1 - i];

                //대진 자리(i)별로 홈/원정을 번갈아 배정.
                //회전에 따라 모든 팀이 모든 자리를 한 번씩 거치므로 홈 경기가 고르게 퍼짐
                games[i] = (i % 2 == 0)
                    ? new LeagueGame(first, second)
                    : new LeagueGame(second, first);
            }

            cycle[round] = games;
            Rotate(rotation);
        }

        return cycle;
    }

    //0번을 고정한 채 1번부터 끝까지 오른쪽으로 한 칸 회전
    private static void Rotate(string[] rotation)
    {
        string last = rotation[rotation.Length - 1];

        for (int i = rotation.Length - 1; i > 1; i--)
        {
            rotation[i] = rotation[i - 1];
        }

        rotation[1] = last;
    }

    //라운드 대진 -> 하루치 경기. 사이클 반전과 플레이어 홈/원정 교대를 적용
    private static LeagueGameDay BuildGameDay(LeagueGame[] roundGames, string playerTeamName, int playerGameIndex, bool flipHomeAway)
    {
        LeagueGame[] dayGames = new LeagueGame[roundGames.Length];

        for (int i = 0; i < roundGames.Length; i++)
        {
            LeagueGame game = roundGames[i];

            dayGames[i] = flipHomeAway
                ? new LeagueGame(game.AwayTeamName, game.HomeTeamName)
                : game;
        }

        //플레이어는 경기마다 홈/원정 교대 (기획서 7.3)
        bool isPlayerHome = playerGameIndex % 2 == 0;
        LeagueGame playerGame = dayGames[LeagueGameDay.PlayerGameIndex];

        if ((playerGame.HomeTeamName == playerTeamName) != isPlayerHome)
        {
            dayGames[LeagueGameDay.PlayerGameIndex] = new LeagueGame(playerGame.AwayTeamName, playerGame.HomeTeamName);
        }

        return new LeagueGameDay(dayGames);
    }
}
