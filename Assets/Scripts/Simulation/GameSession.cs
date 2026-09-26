using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경기 1건의 진행 상태를 들고 타석 단위로 전진시킨다.
/// 일괄 시뮬과 실시간 관전 UI가 같은 진행 규칙을 공유하는 진입점이다
/// </summary>
public class GameSession
{
    public bool IsGameOver => _state.IsGameOver;
    public GameState State => _state;
    public IReadOnlyList<SimulationBatterLog> BatterLogs => _batterLogs;
    public IReadOnlyList<SimulationStealLog> StealLogs => _stealLogs;

    /// <summary>
    /// 이번 StepAtBat에서 새로 생긴 도루 로그. 화면에는 타석 로그보다 먼저 그린다
    /// </summary>
    public IReadOnlyList<SimulationStealLog> LastStepStealLogs => _lastStepStealLogs;
    public SimulationBatterLog LastStepBatterLog { get; private set; }

    private readonly GameSimulator _simulator;
    private readonly SimulationContext _context;
    private readonly GameState _state;
    private readonly List<SimulationBatterLog> _batterLogs;
    private readonly List<SimulationStealLog> _stealLogs;
    private readonly List<SimulationStealLog> _lastStepStealLogs;

    /// <summary>
    /// 시뮬레이터와 컨텍스트를 받아 경기 상태·로그 목록을 초기화한다
    /// </summary>
    public GameSession(SimulationContext context, GameSimulator simulator)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context), "경기 컨텍스트가 없으면 경기를 만들 수 없습니다");

        if (simulator == null)
            throw new ArgumentNullException(nameof(simulator), "시뮬레이터가 없으면 경기를 진행할 수 없습니다");

        _context = context;
        _simulator = simulator;
        _state = new GameState(context);

        _batterLogs = new List<SimulationBatterLog>(80);
        _stealLogs = new List<SimulationStealLog>(8);
        _lastStepStealLogs = new List<SimulationStealLog>();

        LastStepBatterLog = null;
    }

    /// <summary>
    /// 타석 1회 전진. 이미 끝난 경기면 아무것도 하지 않고 false를 반환한다.
    /// </summary>
    public bool StepAtBat()
    {
        if (_state.IsGameOver)
            return false;

        _lastStepStealLogs.Clear();
        LastStepBatterLog = null;

        int batterLogCountBefore = _batterLogs.Count;
        int stealLogCountBefore = _stealLogs.Count;

        _simulator.SimulateAtBat(_state, _context, _batterLogs, _stealLogs);

        for (int i = stealLogCountBefore; i < _stealLogs.Count; i++)
            _lastStepStealLogs.Add(_stealLogs[i]);

        int newBatterLogCount = _batterLogs.Count - batterLogCountBefore;

        if (newBatterLogCount > 0)
            LastStepBatterLog = _batterLogs[batterLogCountBefore];

        if (newBatterLogCount > 1)
        {
            Debug.LogError($"[GameSession]: 한 타석이 타석 로그를 {newBatterLogCount}개 남겼습니다. " +
                           $"LastStepBatterLog가 첫 번째만 노출하므로 목록 반환으로 바꿔야 합니다");
        }

        return true;
    }

    /// <summary>
    /// 종료된 경기의 <see cref="GameResult"/>를 조립한다. 진행 중이면 null.
    /// </summary>
    public GameResult BuildResult()
    {
        if (!_state.IsGameOver)
        {
            Debug.LogWarning("[GameSession]: 경기가 아직 끝나지 않아 결과를 만들 수 없습니다");
            return null;
        }

        return new GameResult(_state.HomeScore, _state.AwayScore, _batterLogs, _stealLogs);
    }
}
