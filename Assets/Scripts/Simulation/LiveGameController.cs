using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LiveGameController : MonoBehaviour, IGameInterruptHandler
{
    //예약 버퍼
    private readonly List<Hittersubstitution> _pendingHitterSubs = new();
    private int _pendingPitcherSlot = -1;

    //인터럽트 핸들러
    //매 타석 종료 후 인터럽트 큐에 인터럽트가 있는지 확인. 없으면 계속 진행. 있으면 교체 알고리즘 진행
    public InterruptDecision OnAtBatEnded(GameState state, SimulationContext context)
    {
        List<Hittersubstitution>_pendingHitterSubsCopy = new List<Hittersubstitution>(_pendingHitterSubs);
        int pitcherSlot = _pendingPitcherSlot;

        InterruptDecision interrupt = new InterruptDecision(pitcherSlot, _pendingHitterSubsCopy);
        _pendingHitterSubs.Clear();
        _pendingPitcherSlot = -1;
        return interrupt;
    }
        
    //UI 진입점 - 버튼 호출
    public bool ReserveHitterSubstitution(int battingOrderIndex, int benchIndex)
    {
        if (battingOrderIndex < 0 || battingOrderIndex > 8)
        {
            Debug.LogWarning("[LiveGameController]: 타순 범위가 유효하지 않습니다.");
            return false;
        }

        if (benchIndex < 0 || benchIndex > 4)
        {
            Debug.LogWarning("[LiveGameController]: 벤치 인덱스 범위가 유효하지 않습니다.");
            return false;
        }

        if (_pendingHitterSubs.Any(sub => sub.BenchIndex == benchIndex))
        {
            Debug.LogWarning("[LiveGameController]: 이미 교체 예약된 벤치 카드 입니다.");
            return false;
        }

        if (_pendingHitterSubs.Any(sub => sub.BattingOrderIndex == battingOrderIndex))
        {
            Debug.LogWarning("[LiveGameController]: 이미 교체 예약된 타순입니다.");
            return false;
        }

        _pendingHitterSubs.Add(new Hittersubstitution(battingOrderIndex, benchIndex));
        return true;
    }

    public bool ReservePitcherSubstitution(int pitcherSlot)
    {
        if (pitcherSlot < 0 || pitcherSlot > 6)
        {
            Debug.LogWarning("[LiveGameController]: 투수 범위가 유효하지 않습니다.");
            return false;
        }
        
        _pendingPitcherSlot = pitcherSlot;
        return true;
    }
}
