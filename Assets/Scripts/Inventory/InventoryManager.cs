using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// 인벤토리 관리 시스템
/// </summary>
public class InventoryManager : SingletonBehaviour<InventoryManager>
{
    //기본 카드 보유 한도량
    const int MAX_CAPACITY = 200;

    //프로퍼티
    public int Count => _cards.Count;
    public int NextInstanceId => _nextInstanceId;
    public int ExpandUnit => _expandUnit;
    public int ExpandGoldCost => _expandGoldCost;
    public int MaxCapacity => _maxCapacity;
    public bool IsFull => Count >= _maxCapacity;

    //변수
    [SerializeField]
    private int _maxCapacity = MAX_CAPACITY;

    [Header("보유 한도 확장")]
    [SerializeField, Tooltip("1회 확장 시 늘어나는 칸 수")]
    private int _expandUnit = 10;
    [SerializeField, Tooltip("1회 확장에 드는 골드 (누진 곡선은 TBD)")]
    private int _expandGoldCost = 1000;

    private int _nextInstanceId = 1;

    //키는 instanceId. 조회·제거가 O(1)이라야 강화 재료 루프와 시뮬 컨텍스트 생성이 선형 탐색을 반복하지 않는다
    private readonly Dictionary<int, CardInstance> _cards = new Dictionary<int, CardInstance>(MAX_CAPACITY);

    //인벤토리에 카드 추가. 발급된 instanceId 반환 (실패 시 -1)
    public int AddCard(int cardId)
    {
        if (IsFull)
        {
            Debug.LogWarning($"[InventoryManager] : 보유 한도 {MaxCapacity} 칸이 가득 찼습니다");
            return -1;
        }

        CardInstance cardInstance = new CardInstance(_nextInstanceId, cardId);

        //_nextInstanceId는 발급 후 증가하므로 키 중복이 생길 수 없다
        _cards.Add(cardInstance.InstanceId, cardInstance);
        _nextInstanceId++;

        return cardInstance.InstanceId;
    }

    //인벤토리에서 카드 제거
    public bool RemoveCard(int instanceId)
    {
        if (!_cards.Remove(instanceId))
        {
            Debug.LogWarning($"[InventoryManager] : 제거하려는 카드가 존재하지 않습니다 : {instanceId}");
            return false;
        }

        return true;
    }

    //카드 잠금
    public bool SetLocked (int instanceId, bool locked)
    {
        CardInstance foundCard = FindCard(instanceId);
        if (foundCard == null) 
            return false;

        foundCard.SetLocked(locked);
        return true;
    }

    //인벤토리 확장
    public void ExpandCapacity(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[InventoryManager] : 인벤토리 확장을 음수 또는 0으로 할 수 없습니다!");
            return;
        }
        _maxCapacity += amount;
    }

    //골드를 내고 한도 확장
    public bool TryExpandCapacityWithGold()
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[InventoryManager] : CurrencyManager가 씬에 없습니다");
            return false;
        }

        //부족 사유는 CurrencyManager가 로그로 남김
        if (!CurrencyManager.Instance.Spend(CurrencyType.Gold, _expandGoldCost))
            return false;

        ExpandCapacity(_expandUnit);
        return true;
    }

    //세이브 복원용 일괄 주입
    public bool Restore(List<CardInstance> cards, int nextInstanceId, int maxCapacity)
    {
        if (cards == null)
        {
            Debug.LogError("[InventoryManager] : 복원할 카드 목록이 null입니다");
            return false;
        }

        //발급 ID가 보유 카드보다 작으면 다음 뽑기가 기존 카드와 같은 ID를 받아 라인업 참조가 엉킨다
        foreach (CardInstance card in cards)
        {
            if (card.InstanceId >= nextInstanceId)
            {
                Debug.LogError($"[InventoryManager] : 세이브의 다음 발급 ID({nextInstanceId})가 보유 카드 ID({card.InstanceId})보다 작거나 같습니다");
                return false;
            }
        }

        _cards.Clear();

        foreach (CardInstance card in cards)
        {
            //Add는 중복 키에서 예외를 던져 복원이 중단된다. 손상된 세이브를 원인과 함께 남기고 실패로 돌린다
            if (!_cards.TryAdd(card.InstanceId, card))
            {
                Debug.LogError($"[InventoryManager] : 세이브에 중복된 카드 ID가 있습니다 : {card.InstanceId}");
                return false;
            }
        }

        _nextInstanceId = nextInstanceId;

        //세이브 당시 확장분을 되살린다. 기본값보다 작으면 확장 기록이 사라진 것이므로 기본값 유지
        if (maxCapacity > _maxCapacity)
            _maxCapacity = maxCapacity;

        return true;
    }

    //인벤토리 필터링
    public List<CardInstance> GetFiltered(CardFilter filter)
    {
        if (filter == null)
        {
            Debug.LogError("[InventoryManager] : 필터가 적용되지 않았습니다.");
            return new List<CardInstance>();
        }

        List<CardInstance> result = new List<CardInstance>();
        CardDataManager cardDataManager = CardDataManager.Instance;

        if (cardDataManager == null)
        {
            Debug.LogError("[InventoryManager] : CardDataManager가 씬에 없습니다.");
            return new List<CardInstance>();
        }

        foreach (CardInstance card in _cards.Values)
        {
            CardMasterData cardMasterData = cardDataManager.GetCardMasterData(card.CardId);
            if (cardMasterData == null)
                continue;

            //PlayerType 필터
            if (filter.PlayerType != PlayerTypeFilter.All)
            {
                bool isHitter = cardMasterData is HitterMasterData;
                if (filter.PlayerType == PlayerTypeFilter.HitterOnly && !isHitter)
                    continue;
                if (filter.PlayerType == PlayerTypeFilter.PitcherOnly && isHitter)
                    continue;
            }

            //Grade 필터
            if (filter.Grade != CardGrade.None && cardMasterData.CardGrade != filter.Grade)
                continue;
            
            //Type 필터
            if (filter.Type != CardType.None && cardMasterData.CardType != filter.Type)
                continue;

            //TeamName 필터
            if (!string.IsNullOrEmpty(filter.TeamName) && cardMasterData.TeamName != filter.TeamName)
                continue;

            result.Add(card);
        }

        return result;
    }
    
    //인벤토리에서 카드 반환
    public CardInstance GetCard(int instanceId)
    {
        return FindCard(instanceId);
    }

    //인벤토리에서 전체 카드 반환
    public Dictionary<int, CardInstance>.ValueCollection GetAllCards()
    {
        return _cards.Values;
    }

    //카드 찾기
    private CardInstance FindCard(int instanceId)
    {
        if (!_cards.TryGetValue(instanceId, out CardInstance card))
        {
            Debug.LogWarning($"[InventoryManager] : 찾으려는 카드가 존재하지 않습니다 : {instanceId}");
            return null;
        }

        return card;
    }
}
