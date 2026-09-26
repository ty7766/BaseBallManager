using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 라인업 슬롯 관리 - 야수 9 + 벤치 5 + 투수 11(SP5·RP5·CP1)
/// </summary>
public class LineUpManager : SingletonBehaviour<LineUpManager>
{
    /// <summary>
    /// 빈 슬롯 센티넬
    /// </summary>
    public const int EmptySlot = -1;

    /// <summary>
    /// 선발 야수 슬롯 수 (= 타순 수)
    /// </summary>
    public const int HitterSlotCount = 9;

    /// <summary>
    /// 벤치 야수 슬롯 수
    /// </summary>
    public const int BenchSlotCount = 5;

    /// <summary>
    /// 선발 투수 슬롯 수 (배열 순서 = 로테이션 순서)
    /// </summary>
    public const int StartingPitcherCount = 5;

    /// <summary>
    /// 불펜 투수 슬롯 수
    /// </summary>
    public const int RelieverCount = 5;

    /// <summary>
    /// 마무리 투수 슬롯 수
    /// </summary>
    public const int CloserCount = 1;

    private readonly Dictionary<HitterPosition, (int instanceId, int battingOrder)> _hitterSlots =
        new Dictionary<HitterPosition, (int instanceId, int battingOrder)>(HitterSlotCount);

    private readonly int[] _benchSlots = new int[BenchSlotCount];

    private readonly Dictionary<PitcherPosition, int[]> _pitcherSlots = new Dictionary<PitcherPosition, int[]>(3)
    {
        { PitcherPosition.SP, new int[StartingPitcherCount] },
        { PitcherPosition.RP, new int[RelieverCount] },
        { PitcherPosition.CP, new int[CloserCount] }
    };

    protected override void OnSingletonAwake()
    {
        ClearAll();
    }

    /// <summary>
    /// 빈 야수 슬롯에 카드를 배치한다 (타순 1~9, 포지션 일치 필요. DH는 아무 야수나 가능).
    /// 이미 차 있는 슬롯은 RemoveHitter로 비운 뒤 배치한다
    /// </summary>
    public bool AssignHitter(HitterPosition slot, int instanceId, int battingOrder)
    {
        if (battingOrder <= 0 || battingOrder > HitterSlotCount)
        {
            Debug.LogWarning($"[LineUpManager]: 타순은 1~{HitterSlotCount}만 가능합니다 ({battingOrder})");
            return false;
        }

        if (_hitterSlots[slot].instanceId != EmptySlot)
        {
            Debug.LogWarning($"[LineUpManager]: {slot} 슬롯은 이미 차 있습니다");
            return false;
        }

        if (IsBattingOrderTaken(battingOrder, slot))
        {
            Debug.LogWarning($"[LineUpManager]: {battingOrder}번 타순은 이미 다른 선수가 사용 중입니다");
            return false;
        }

        if (IsCardAssigned(instanceId))
        {
            Debug.LogWarning($"[LineUpManager]: 이미 다른 슬롯에 배치된 카드입니다 (instanceId {instanceId})");
            return false;
        }

        HitterMasterData hitterData = GetHitterMasterData(instanceId);

        if (hitterData == null)
            return false;

        if (slot != HitterPosition.DH && !IsHitterPositionMatched(hitterData, slot, instanceId))
            return false;

        _hitterSlots[slot] = (instanceId, battingOrder);

        return true;
    }

    /// <summary>
    /// 야수 슬롯을 비운다
    /// </summary>
    public bool RemoveHitter(HitterPosition slot)
    {
        if (_hitterSlots[slot].instanceId == EmptySlot)
        {
            Debug.LogWarning($"[LineUpManager]: {slot} 슬롯이 이미 비어 있습니다");
            return false;
        }

        _hitterSlots[slot] = (EmptySlot, 0);

        return true;
    }

    /// <summary>
    /// 벤치 슬롯에 카드를 배치한다
    /// </summary>
    public bool AssignBench(int benchIndex, int instanceId)
    {
        if (benchIndex < 0 || benchIndex >= _benchSlots.Length)
        {
            Debug.LogWarning($"[LineUpManager]: 벤치 슬롯 번호가 범위를 벗어났습니다 ({benchIndex})");
            return false;
        }

        if (_benchSlots[benchIndex] != EmptySlot)
        {
            Debug.LogWarning($"[LineUpManager]: {benchIndex}번 벤치 슬롯은 이미 차 있습니다");
            return false;
        }

        if (IsCardAssigned(instanceId))
        {
            Debug.LogWarning($"[LineUpManager]: 이미 다른 슬롯에 배치된 카드입니다 (instanceId {instanceId})");
            return false;
        }

        if (GetHitterMasterData(instanceId) == null)
            return false;

        _benchSlots[benchIndex] = instanceId;

        return true;
    }

    /// <summary>
    /// 벤치 슬롯을 비운다
    /// </summary>
    public bool RemoveBench(int benchIndex)
    {
        if (benchIndex < 0 || benchIndex >= _benchSlots.Length)
        {
            Debug.LogWarning($"[LineUpManager]: 벤치 슬롯 번호가 범위를 벗어났습니다 ({benchIndex})");
            return false;
        }

        if (_benchSlots[benchIndex] == EmptySlot)
        {
            Debug.LogWarning($"[LineUpManager]: {benchIndex}번 벤치 슬롯이 이미 비어 있습니다");
            return false;
        }

        _benchSlots[benchIndex] = EmptySlot;

        return true;
    }

    /// <summary>
    /// 투수 슬롯에 카드를 배치한다 (카드의 보직과 슬롯 보직이 같아야 한다)
    /// </summary>
    public bool AssignPitcher(PitcherPosition position, int slotIndex, int instanceId)
    {
        int[] slots = _pitcherSlots[position];

        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            Debug.LogWarning($"[LineUpManager]: {position} 슬롯 번호가 범위를 벗어났습니다 ({slotIndex})");
            return false;
        }

        if (slots[slotIndex] != EmptySlot)
        {
            Debug.LogWarning($"[LineUpManager]: {position} {slotIndex}번 슬롯은 이미 차 있습니다");
            return false;
        }

        if (IsCardAssigned(instanceId))
        {
            Debug.LogWarning($"[LineUpManager]: 이미 다른 슬롯에 배치된 카드입니다 (instanceId {instanceId})");
            return false;
        }

        PitcherMasterData pitcherData = GetPitcherMasterData(instanceId);

        if (pitcherData == null)
            return false;

        if (!PitcherPositionParser.TryParse(pitcherData.Position, out PitcherPosition cardPosition))
        {
            Debug.LogError($"[LineUpManager]: 카드의 보직 표기를 해석할 수 없습니다 (instanceId {instanceId} / {pitcherData.Position})");
            return false;
        }

        if (cardPosition != position)
        {
            Debug.LogWarning($"[LineUpManager]: 보직이 맞지 않습니다 (슬롯 {position} / 카드 {cardPosition})");
            return false;
        }

        slots[slotIndex] = instanceId;

        return true;
    }

    /// <summary>
    /// 투수 슬롯을 비운다
    /// </summary>
    public bool RemovePitcher(PitcherPosition position, int slotIndex)
    {
        int[] slots = _pitcherSlots[position];

        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            Debug.LogWarning($"[LineUpManager]: {position} 슬롯 번호가 범위를 벗어났습니다 ({slotIndex})");
            return false;
        }

        if (slots[slotIndex] == EmptySlot)
        {
            Debug.LogWarning($"[LineUpManager]: {position} {slotIndex}번 슬롯이 이미 비어 있습니다");
            return false;
        }

        slots[slotIndex] = EmptySlot;

        return true;
    }

    /// <summary>
    /// 이미 배치된 야수의 타순을 바꾼다
    /// </summary>
    public bool SetBattingOrder(HitterPosition slot, int order)
    {
        if (order <= 0 || order > HitterSlotCount)
        {
            Debug.LogWarning($"[LineUpManager]: 타순은 1~{HitterSlotCount}만 가능합니다 ({order})");
            return false;
        }

        if (_hitterSlots[slot].instanceId == EmptySlot)
        {
            Debug.LogWarning($"[LineUpManager]: {slot} 슬롯이 비어 있습니다");
            return false;
        }

        if (IsBattingOrderTaken(order, slot))
        {
            Debug.LogWarning($"[LineUpManager]: {order}번 타순은 이미 다른 선수가 사용 중입니다");
            return false;
        }

        _hitterSlots[slot] = (_hitterSlots[slot].instanceId, order);

        return true;
    }

    /// <summary>
    /// 야수 9칸과 투수 11칸이 모두 찼는지 검사한다 (벤치는 선택이라 보지 않는다)
    /// </summary>
    public bool IsLineupComplete()
    {
        foreach ((int instanceId, int battingOrder) hitter in _hitterSlots.Values)
        {
            if (hitter.instanceId == EmptySlot)
                return false;
        }

        foreach (int[] slots in _pitcherSlots.Values)
        {
            foreach (int pitcherInstanceId in slots)
            {
                if (pitcherInstanceId == EmptySlot)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 어느 슬롯에든 배치된 카드인지 확인한다 (분해·조합·강화 차단 판정에도 쓴다)
    /// </summary>
    public bool IsCardAssigned(int instanceId)
    {
        if (instanceId == EmptySlot)
            return false;

        foreach ((int instanceId, int battingOrder) hitter in _hitterSlots.Values)
        {
            if (hitter.instanceId == instanceId)
                return true;
        }

        foreach (int benchInstanceId in _benchSlots)
        {
            if (benchInstanceId == instanceId)
                return true;
        }

        foreach (int[] slots in _pitcherSlots.Values)
        {
            foreach (int pitcherInstanceId in slots)
            {
                if (pitcherInstanceId == instanceId)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 타순대로 정렬된 야수 instanceId 배열. 라인업이 미완성이면 빈 배열
    /// </summary>
    public int[] GetHittersInBattingOrder()
    {
        if (!IsLineupComplete())
        {
            Debug.LogWarning("[LineUpManager]: 라인업이 완성되지 않았습니다");
            return Array.Empty<int>();
        }

        int[] result = new int[HitterSlotCount];

        foreach ((int instanceId, int battingOrder) slot in _hitterSlots.Values)
            result[slot.battingOrder - 1] = slot.instanceId;

        return result;
    }

    /// <summary>
    /// 벤치 야수 instanceId 배열 사본
    /// </summary>
    public int[] GetBenchInstanceIds()
    {
        int[] result = new int[_benchSlots.Length];
        Array.Copy(_benchSlots, result, _benchSlots.Length);

        return result;
    }

    /// <summary>
    /// 해당 보직의 투수 instanceId 배열 사본
    /// </summary>
    public int[] GetPitcherInstanceIds(PitcherPosition position)
    {
        int[] source = _pitcherSlots[position];
        int[] result = new int[source.Length];
        Array.Copy(source, result, source.Length);

        return result;
    }

    /// <summary>
    /// 야수 슬롯 1칸 조회 (빈 슬롯은 (-1, 0)). 미완성 라인업도 읽을 수 있어 세이브에 쓴다
    /// </summary>
    public (int instanceId, int battingOrder) GetHitterSlot(HitterPosition slot)
    {
        return _hitterSlots[slot];
    }

    /// <summary>
    /// 전 슬롯을 비운다 (세이브 복원 직전에 호출해 중복 배치 실패를 막는다)
    /// </summary>
    public void ClearAll()
    {
        foreach (HitterPosition position in Enum.GetValues(typeof(HitterPosition)))
            _hitterSlots[position] = (EmptySlot, 0);

        Array.Fill(_benchSlots, EmptySlot);

        foreach (int[] slots in _pitcherSlots.Values)
            Array.Fill(slots, EmptySlot);
    }

    //배치하려는 카드의 야수 마스터 데이터. 없거나 투수면 null
    private HitterMasterData GetHitterMasterData(int instanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);

        if (cardInstance == null)
            return null;

        CardMasterData cardData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);

        if (cardData is not HitterMasterData hitterData)
        {
            Debug.LogWarning($"[LineUpManager]: 야수 슬롯에는 야수 카드만 배치할 수 있습니다 (instanceId {instanceId})");
            return null;
        }

        return hitterData;
    }

    //배치하려는 카드의 투수 마스터 데이터. 없거나 야수면 null
    private PitcherMasterData GetPitcherMasterData(int instanceId)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);

        if (cardInstance == null)
            return null;

        CardMasterData cardData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);

        if (cardData is not PitcherMasterData pitcherData)
        {
            Debug.LogWarning($"[LineUpManager]: 투수 슬롯에는 투수 카드만 배치할 수 있습니다 (instanceId {instanceId})");
            return null;
        }

        return pitcherData;
    }

    //카드의 포지션 표기가 슬롯과 맞는지 검사. 표기를 해석할 수 없으면 실패로 본다
    private bool IsHitterPositionMatched(HitterMasterData hitterData, HitterPosition slot, int instanceId)
    {
        if (!HitterPositionParser.TryParse(hitterData.Position, out HitterPosition cardPosition))
        {
            Debug.LogError($"[LineUpManager]: 카드의 포지션 표기를 해석할 수 없습니다 (instanceId {instanceId} / {hitterData.Position})");
            return false;
        }

        if (cardPosition != slot)
        {
            Debug.LogWarning($"[LineUpManager]: 포지션이 맞지 않습니다 (슬롯 {slot} / 카드 {cardPosition})");
            return false;
        }

        return true;
    }

    //해당 타순을 다른 슬롯이 쓰고 있는지 확인
    private bool IsBattingOrderTaken(int order, HitterPosition excludeSlot)
    {
        foreach (KeyValuePair<HitterPosition, (int instanceId, int battingOrder)> pair in _hitterSlots)
        {
            if (pair.Key == excludeSlot)
                continue;

            if (pair.Value.instanceId == EmptySlot)
                continue;

            if (pair.Value.battingOrder == order)
                return true;
        }

        return false;
    }
}
