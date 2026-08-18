using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 리그 경기 1건을 시뮬 입력(SimulationContext)으로 변환
/// </summary>
/// <remarks>정규시즌과 포스트시즌이 같은 규칙으로 컨텍스트를 만들도록 한 곳에 모음</remarks>
public class LeagueGameContextFactory
{
    private readonly IReadOnlyDictionary<string, AiTeamRoster> _rosters;
    private readonly string _playerTeamName;

    public LeagueGameContextFactory(IReadOnlyDictionary<string, AiTeamRoster> rosters, string playerTeamName)
    {
        _rosters = rosters;
        _playerTeamName = playerTeamName;
    }

    //경기가 플레이어 팀 경기인지
    public bool IsPlayerGame(LeagueGame game)
    {
        return game.Contains(_playerTeamName);
    }

    //경기 1건의 시뮬 입력 구성. 실패 시 null
    public SimulationContext Create(LeagueGame game, int homeRotationIndex, int awayRotationIndex)
    {
        if (IsPlayerGame(game))
        {
            bool isPlayerHome = game.HomeTeamName == _playerTeamName;
            AiTeamRoster opponent = GetRoster(game.GetOpponent(_playerTeamName));

            if (opponent == null)
                return null;

            int playerRotationIndex = isPlayerHome ? homeRotationIndex : awayRotationIndex;
            int opponentRotationIndex = isPlayerHome ? awayRotationIndex : homeRotationIndex;

            return SimulationContextBuilder.Build(opponent, isPlayerHome, playerRotationIndex, opponentRotationIndex);
        }

        AiTeamRoster homeRoster = GetRoster(game.HomeTeamName);
        AiTeamRoster awayRoster = GetRoster(game.AwayTeamName);

        if (homeRoster == null || awayRoster == null)
            return null;

        return SimulationContextBuilder.BuildAiVersusAi(homeRoster, awayRoster, homeRotationIndex, awayRotationIndex);
    }

    //팀명으로 로스터 조회
    private AiTeamRoster GetRoster(string teamName)
    {
        if (teamName != null && _rosters.TryGetValue(teamName, out AiTeamRoster roster))
            return roster;

        Debug.LogError($"[LeagueGameContextFactory]: '{teamName}' 팀의 로스터가 없습니다");
        return null;
    }
}
