using System;
using System.Collections.Generic;
using UnityEngine;

public class LineUpManager : MonoBehaviour
{
    public static LineUpManager Instance { get; private set; }

    //야수 슬롯 (9칸)
    private Dictionary<HitterPosition, (int instanceId, int battingOrder)> _hitterSlots;

    //야수 후보 슬롯 (5칸)
    private int[] _benchSlots;

    //투수 슬롯 (SP 5칸, RP 5칸, CP 1칸)
    private Dictionary<PitcherPosition, int[]> _pitcherSlots;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            _hitterSlots = new Dictionary<HitterPosition, (int instanceId, int battingOrder)>();
            foreach (HitterPosition pos in System.Enum.GetValues(typeof(HitterPosition)))
                _hitterSlots[pos] = (-1, 0);
            _benchSlots = new int[5];
            Array.Fill(_benchSlots, -1);
            _pitcherSlots = new Dictionary<PitcherPosition, int[]>();
            _pitcherSlots[PitcherPosition.SP] = new int[5];
            _pitcherSlots[PitcherPosition.RP] = new int[5];
            _pitcherSlots[PitcherPosition.CP] = new int[1];
            Array.Fill(_pitcherSlots[PitcherPosition.SP], -1);
            Array.Fill(_pitcherSlots[PitcherPosition.RP], -1);
            Array.Fill(_pitcherSlots[PitcherPosition.CP], -1);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //타자 슬롯에 카드 배치
    public bool AssignHitter(HitterPosition slot, int instanceId, int battingOrder)
    {
        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);
        if (cardInstance == null)
        {
            Debug.LogWarning("[LineUpManager]: 해당 카드가 존재하지 않습니다");
            return false;
        }

        CardMasterData cardData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);
        if (cardData == null || cardData is not HitterMasterData)
        {
            return false;
        }

        //타순은 1번부터 9번까지 허용
        if (battingOrder <= 0 || battingOrder >= 10)
        {
            return false;
        }

        //타순 중복 방지
        if (IsBattingOrderTaken(battingOrder, slot))
        {
            Debug.LogWarning($"[LineUpManager]: {battingOrder}번 타순은 이미 다른 선수가 사용 중입니다.");
            return false;
        }

        //이미 다른 슬롯에 배치된 카드인지 확인
        if (IsCardAssigned(instanceId))
        {
            Debug.LogWarning("[LineUpManager]: 이 카드는 이미 다른 슬롯에 배치되어있습니다");
            return false;
        }

        //DH 포지션에는 아무 카드 가능
        if (slot != HitterPosition.DH)
        {
            HitterPositionParser.TryParse(cardData.Position, out HitterPosition cardPosition);
            //해당 포지션과 일치하는 카드인지 확인
            if (cardPosition != slot)
            {
                return false;
            }
        }

        _hitterSlots[slot] = (instanceId, battingOrder);
        return true;
    }

    //타자 슬롯에 카드 제거
    public bool RemoveHitter(HitterPosition slot)
    {
        //해당 슬롯이 비어있는지 확인
        if (_hitterSlots[slot].instanceId == -1)
        {
            Debug.LogWarning("[LineUpManager]: 해당 슬롯이 이미 비어있습니다.");
            return false;
        }

        //슬롯 초기화
        _hitterSlots[slot] = (-1, 0);
        return true;
    }

    //벤치 슬롯에 카드 배치
    public bool AssignBench(int benchIndex, int instanceId)
    {
        if(benchIndex < 0 || benchIndex >= _benchSlots.Length)
            return false;

        if (_benchSlots[benchIndex] != -1)
        {
            Debug.LogWarning("[LineUpManager]: 해당 슬롯은 이미 할당되어있습니다.");
            return false;
        }

        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);
        if (cardInstance == null)
        {
            Debug.LogWarning("[LineUpManager]: 해당 카드가 존재하지 않습니다.");
            return false;
        }

        CardMasterData cardData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);
        if (cardData == null || cardData is not HitterMasterData)
        {
            return false;
        }

        if (IsCardAssigned(instanceId))
        {
            Debug.LogWarning("[LineUpManager]: 카드가 이미 배치되어있습니다.");
            return false;
        }

        _benchSlots[benchIndex] = instanceId;
        return true;
    }

    //벤치 슬롯에 카드 제거
    public bool RemoveBench(int benchIndex)
    {
        if (benchIndex < 0 || benchIndex >= _benchSlots.Length)
            return false;

        //해당 슬롯이 비어있는지 확인
        if (_benchSlots[benchIndex] == -1)
        {
            Debug.LogWarning("[LineUpManager]: 해당 슬롯이 이미 비어있습니다.");
            return false;
        }

        //슬롯 초기화
        _benchSlots[benchIndex] = -1;
        return true;
    }

    //투수 슬롯에 카드 추가
    public bool AssignPitcher(PitcherPosition position, int slotIndex, int instanceId)
    {
        if (slotIndex < 0 || slotIndex >= _pitcherSlots[position].Length)
            return false;

        if (_pitcherSlots[position][slotIndex] != -1)
        {
            Debug.LogWarning("[LineUpManager]: 해당 슬롯은 이미 할당되어있습니다.");
            return false;
        }

        CardInstance cardInstance = InventoryManager.Instance.GetCard(instanceId);
        if (cardInstance == null)
        {
            Debug.LogWarning("[LineUpManager]: 해당 카드가 존재하지 않습니다.");
            return false;
        }

        CardMasterData cardData = CardDataManager.Instance.GetCardMasterData(cardInstance.CardId);
        if (cardData == null || cardData is not PitcherMasterData)
        {
            return false;
        }

        PitcherPositionParser.TryParse(cardData.Position, out PitcherPosition cardPosition);
        //해당 포지션과 일치하는 카드인지 확인
        if (cardPosition != position)
        {
            return false;
        }

        if (IsCardAssigned(instanceId))
        {
            Debug.LogWarning("[LineUpManager]: 카드가 이미 배치되어있습니다.");
            return false;
        }

        _pitcherSlots[position][slotIndex] = instanceId;
        return true;
    }

    //투수 슬롯에 카드 제거
    public bool RemovePitcher(PitcherPosition position, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _pitcherSlots[position].Length)
            return false;

        //해당 슬롯이 비어있는지 확인
        if (_pitcherSlots[position][slotIndex] == -1)
        {
            Debug.LogWarning("[LineUpManager]: 해당 슬롯이 이미 비어있습니다.");
            return false;
        }

        //슬롯 초기화
        _pitcherSlots[position][slotIndex] = -1;
        return true;
    }

    //타자의 타순 설정 및 변경
    public bool SetBattingOrder(HitterPosition slot, int order)
    {
        if (_hitterSlots[slot].instanceId == -1)
        {
            Debug.LogWarning("[LineUpManager]: 해당 슬롯이 이미 비어있습니다.");
            return false;
        }

        if (IsBattingOrderTaken(order, slot))
        {
            Debug.LogWarning($"[LineUpManager]: {order}번 타순은 이미 다른 선수가 사용 중입니다.");
            return false;
        }

        if (order <= 0 || order >= 10)
            return false;

        _hitterSlots[slot] = (_hitterSlots[slot].instanceId, order);
        return true;
    }

    //라인업이 모두 채워졌는지 검사
    //1. 벤치 슬롯은 채워져있지 않아도 됨
    //2. 투/타 슬롯이 채워져있지 않으면 리그 입장 불가능
    public bool IsLineupComplete()
    {
        foreach(var hitter in _hitterSlots.Values)
        {
            if (hitter.instanceId == -1)
                return false;
        }

        foreach(int[] value in _pitcherSlots.Values)
        {
            foreach (int pitcherID in value)
            {
                if (pitcherID == -1)
                    return false;
            }
        }

        return true;
    }

    //이미 배치되어있는 카드인지 확인
    private bool IsCardAssigned(int instanceId)
    {
        //히터 슬롯에 이미 배치되어있는지 확인
        foreach(var hitterID in _hitterSlots.Values)
        {
            if (hitterID.instanceId == instanceId)
                return true;
        }
        
        //벤치 슬롯에 이미 배치되어있는지 확인
        foreach(int benchID in _benchSlots)
        {
            if (benchID == instanceId)
                return true;
        }

        //투수 슬롯에 이미 배치되어있는지 확인
        foreach (int[] value in _pitcherSlots.Values)
        {
            foreach(var pitcherID in value)
            {
                if (pitcherID == instanceId)
                    return true;
            }
        }

        return false;
    }

    //해당 타순이 이미 다른 슬롯에서 사용 중인지 확인
    private bool IsBattingOrderTaken(int order, HitterPosition excludeSlot)
    {
        foreach(var pair in _hitterSlots)
        {
            if (pair.Key == excludeSlot)
                continue;
            if (pair.Value.instanceId == -1)
                continue;
            if (pair.Value.battingOrder == order)
                return true;
        }
        return false;
    }

    //정렬된 타순의 instanceId 배열 반환
    public int[] GetHittersInBattingOrder()
    {
        if (!IsLineupComplete())
        {
            Debug.LogWarning("[LineUpManager]: 라인업이 완성되지 않았습니다.");
            return Array.Empty<int>();
        }

        int[] result = new int[9];
        foreach (var slot in _hitterSlots.Values)
        {
            result[slot.battingOrder - 1] = slot.instanceId;
        }

        return result;
    }

    //벤치 야수 instanceId 배열 반환
    public int[] GetBenchInstanceIds()
    {
        int[] result = new int[_benchSlots.Length];
        Array.Copy(_benchSlots, result, _benchSlots.Length);
        return result;
    }

    //투수 슬롯 instanceId 배열 반환
    public int[] GetPitcherInstanceIds(PitcherPosition position)
    {
        int[] source = _pitcherSlots[position];
        int[] result = new int[source.Length];

        Array.Copy(source, result, source.Length);
        return result;
    }
}
