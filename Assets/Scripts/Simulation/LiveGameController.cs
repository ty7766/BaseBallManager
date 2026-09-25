using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실시간 관전 중 사용자가 예약한 교체를 타석 사이에 시뮬로 전달한다
/// </summary>
public class LiveGameController : MonoBehaviour, IGameInterruptHandler
{
    /// <summary>
    /// 예약된 투수 교체가 없음을 나타내는 값
    /// </summary>
    public const int NoPitcherSubstitution = -1;

    //예약이 하나도 없는 타석에서 매번 새 객체를 만들지 않도록 재사용하는 빈 지시
    private static readonly InterruptDecision EmptyDecision =
        new InterruptDecision(NoPitcherSubstitution, System.Array.Empty<HitterSubstitution>());

    private readonly List<HitterSubstitution> _pendingHitterSubs = new List<HitterSubstitution>();
    private int _pendingPitcherSlot = NoPitcherSubstitution;

    /// <summary>
    /// 매 타석 종료 시 시뮬이 호출한다. 예약분을 넘기고 버퍼를 비운다
    /// </summary>
    public InterruptDecision OnAtBatEnded(GameState state, SimulationContext context)
    {
        if (_pendingHitterSubs.Count == 0 && _pendingPitcherSlot == NoPitcherSubstitution)
            return EmptyDecision;

        InterruptDecision decision =
            new InterruptDecision(_pendingPitcherSlot, _pendingHitterSubs.ToArray());

        _pendingHitterSubs.Clear();
        _pendingPitcherSlot = NoPitcherSubstitution;

        return decision;
    }

    /// <summary>
    /// 대타 교체를 예약한다. 같은 타순·같은 벤치 카드를 두 번 지정할 수 없다
    /// </summary>
    public bool ReserveHitterSubstitution(int battingOrderIndex, int benchIndex)
    {
        if (battingOrderIndex < 0 || battingOrderIndex >= SimulationContext.LineupSize)
        {
            Debug.LogWarning($"[LiveGameController]: 타순 범위가 유효하지 않습니다 ({battingOrderIndex})");
            return false;
        }

        if (benchIndex < 0 || benchIndex >= SimulationContext.MaxBenchSize)
        {
            Debug.LogWarning($"[LiveGameController]: 벤치 번호가 유효하지 않습니다 ({benchIndex})");
            return false;
        }

        foreach (HitterSubstitution sub in _pendingHitterSubs)
        {
            if (sub.BenchIndex == benchIndex)
            {
                Debug.LogWarning($"[LiveGameController]: 이미 교체 예약된 벤치 카드입니다 ({benchIndex})");
                return false;
            }

            if (sub.BattingOrderIndex == battingOrderIndex)
            {
                Debug.LogWarning($"[LiveGameController]: 이미 교체 예약된 타순입니다 ({battingOrderIndex})");
                return false;
            }
        }

        _pendingHitterSubs.Add(new HitterSubstitution(battingOrderIndex, benchIndex));

        return true;
    }

    /// <summary>
    /// 투수 교체를 예약한다. 뒤에 부르면 앞 예약을 덮어쓴다
    /// </summary>
    public bool ReservePitcherSubstitution(int pitcherSlot)
    {
        if (pitcherSlot < 0 || pitcherSlot > SimulationContext.CloserSlot)
        {
            Debug.LogWarning($"[LiveGameController]: 투수 슬롯 번호가 유효하지 않습니다 ({pitcherSlot})");
            return false;
        }

        _pendingPitcherSlot = pitcherSlot;

        return true;
    }
}
