using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경기 1건의 진행 상태를 들고 타석 단위로 전진시킨다.
/// </summary>
/// <remarks>
/// 일괄 시뮬(<see cref="GameSimulator.SimulateGame"/>)과 실시간 관전 UI가
/// <b>같은 진행 규칙 한 벌</b>을 공유하기 위한 진입점이다.
/// 상태(<see cref="GameState"/>·로그)를 들고 있는 쪽이 진행도 책임진다.
/// </remarks>
public class GameSession
{
    public bool IsGameOver => _state.IsGameOver;
    public GameState State => _state;
    public IReadOnlyList<SimulationBatterLog> BatterLogs => _batterLogs;
    public IReadOnlyList<SimulationStealLog> StealLogs => _stealLogs;

    //이번 StepAtBat()에서 새로 생긴 몫. UI가 화면에 흘릴 대상
    //실제 발생 순서는 도루가 먼저이므로 StealLogs -> BatterLog 순으로 그린다
    public IReadOnlyList<SimulationStealLog> LastStepStealLogs => _lastStepStealLogs;
    public SimulationBatterLog LastStepBatterLog { get; private set; }

    private readonly GameSimulator _simulator;
    private readonly SimulationContext _context;
    private readonly GameState _state;
    private readonly List<SimulationBatterLog> _batterLogs;
    private readonly List<SimulationStealLog> _stealLogs;
    private readonly List<SimulationStealLog> _lastStepStealLogs;

    //시뮬레이터 · 컨텍스트를 받아 경기 상태와 로그 목록을 초기화
    public GameSession(SimulationContext context, GameSimulator simulator)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context), "경기 컨텍스트가 없으면 경기를 만들 수 없습니다");

        if (simulator == null)
            throw new ArgumentNullException(nameof(simulator), "시뮬레이터가 없으면 경기를 진행할 수 없습니다");

        _context = context;
        _simulator = simulator;
        _state = new GameState(context);

        //9이닝 경기의 타석은 대략 70~80개. 미리 잡아두면 경기마다 발생하는 List 재할당이 사라진다
        _batterLogs = new List<SimulationBatterLog>(80);
        _stealLogs = new List<SimulationStealLog>(8);

        //한 타석에 도루는 최대 1건이라 용량 지정이 불필요
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

        //이번 스텝 몫 버퍼 초기화. 스텝이 로그를 하나도 안 남길 수 있으므로(도루 실패 3아웃) 먼저 비운다
        _lastStepStealLogs.Clear();
        LastStepBatterLog = null;

        //SimulateAtBat은 로그를 append만 하고 반환하지 않는다.
        //호출 전 개수를 잡아두고 그 뒤에 늘어난 몫만 꺼낸다 (마지막 원소를 읽으면 개수가 바뀌었을 때 조용히 틀린다)
        int batterLogCountBefore = _batterLogs.Count;
        int stealLogCountBefore = _stealLogs.Count;

        _simulator.SimulateAtBat(_state, _context, _batterLogs, _stealLogs);

        for (int i = stealLogCountBefore; i < _stealLogs.Count; i++)
        {
            _lastStepStealLogs.Add(_stealLogs[i]);
        }

        int newBatterLogCount = _batterLogs.Count - batterLogCountBefore;

        if (newBatterLogCount > 0)
        {
            LastStepBatterLog = _batterLogs[batterLogCountBefore];
        }

        //한 타석 = 타석 로그 0개(도루 실패 3아웃) 또는 1개가 전제.
        //2개 이상이면 이 프로퍼티가 뒤쪽 로그를 조용히 버리게 되므로 즉시 드러낸다
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
