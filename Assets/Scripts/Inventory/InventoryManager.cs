using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    //싱글톤
    public static InventoryManager Instance { get; private set; }

    //프로퍼티
    public int Count => _cards.Count;
    public bool IsFull => _cards.Count >= _maxCapacity;

    //세이브 저장용 - 다음에 발급할 인스턴스 ID
    public int NextInstanceId => _nextInstanceId;

    //한도 확장 1회분 정보 (UI 표기용)
    public int ExpandUnit => _expandUnit;
    public int ExpandGoldCost => _expandGoldCost;

    //변수
    [SerializeField]
    private int _maxCapacity = 200;

    [Header("보유 한도 확장 (기획서 9.3)")]
    [SerializeField, Tooltip("1회 확장 시 늘어나는 칸 수")]
    private int _expandUnit = 10;
    [SerializeField, Tooltip("1회 확장에 드는 골드 (누진 곡선은 TBD)")]
    private int _expandGoldCost = 1000;

    private int _nextInstanceId = 1;
    private List<CardInstance> _cards;
    
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _cards = new List<CardInstance>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //maxCapacity 전달용
    public int GetMaxCapacity()
    {
        return _maxCapacity;
    }

    //인벤토리에 카드 추가. 발급된 instanceId 반환 (실패 시 -1)
    public int AddCard(int cardId)
    {
        if (IsFull)
        {
            Debug.LogWarning($"[InventoryManager] : 카드가 {_maxCapacity}를 초과했습니다!");
            return -1;
        }

        CardInstance cardInstance = new CardInstance(_nextInstanceId, cardId);
        _cards.Add(cardInstance);
        _nextInstanceId++;

        return cardInstance.InstanceId;
    }

    //인벤토리에서 카드 제거
    public bool RemoveCard(int instanceId)
    {
        CardInstance foundCard = _cards.Find(card => card.InstanceId == instanceId);
        //삭제하려는 카드가 있으면 삭제
        if (foundCard == null)
        {
            Debug.LogWarning("[InventoryManager] : 삭제하려는 카드가 없습니다!");
            return false;
        }
        _cards.Remove(foundCard);
        return true;
    }

    //카드 잠금
    public bool SetLocked (int instanceId, bool locked)
    {
        CardInstance foundCard = _cards.Find (card => card.InstanceId == instanceId);

        if (foundCard == null)
        {
            Debug.LogWarning("[InventoryManager] : 잠그려는 카드가 없습니다!");
            return false;
        }
        foundCard.SetLocked(locked);
        return true;
    }

    //인벤토리 확장 (무상 - 보상 지급 등)
    public void ExpandCapacity(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[InventoryManager] : 인벤토리 확장을 음수로 할 수 없습니다!");
            return;
        }
        _maxCapacity += amount;
    }

    //골드를 내고 한도 확장 (기획서 9.3). 골드가 모자라면 확장하지 않는다
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
    public void Restore(List<CardInstance> cards, int nextInstanceId, int maxCapacity)
    {
        if (cards == null)
        {
            Debug.LogError("[InventoryManager] : 복원할 카드 목록이 null입니다");
            return;
        }

        //발급 ID가 보유 카드보다 작으면 다음 뽑기가 기존 카드와 같은 ID를 받아 라인업 참조가 엉킨다
        foreach (CardInstance card in cards)
        {
            if (card.InstanceId >= nextInstanceId)
            {
                Debug.LogError($"[InventoryManager] : 세이브의 다음 발급 ID({nextInstanceId})가 보유 카드 ID({card.InstanceId})보다 작거나 같습니다");
                return;
            }
        }

        _cards = cards;
        _nextInstanceId = nextInstanceId;

        //세이브 당시 확장분을 되살린다. 기본값보다 작으면 확장 기록이 사라진 것이므로 기본값 유지
        if (maxCapacity > _maxCapacity)
            _maxCapacity = maxCapacity;
    }

    //인벤토리 필터링
    public List<CardInstance> GetFiltered(CardFilter filter)
    {
        List<CardInstance> result = new List<CardInstance>();

        foreach (CardInstance card in _cards)
        {
            CardMasterData cardMasterData = CardDataManager.Instance.GetCardMasterData(card.CardId);

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
        CardInstance card = _cards.Find(card => card.InstanceId == instanceId);
        if (card == null)
        {
            return null;
        }

        return card;
    }

    //인벤토리에서 전체 카드 반환
    public IReadOnlyList<CardInstance> GetAllCards()
    {
        return _cards;
    }
}
