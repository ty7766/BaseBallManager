using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

public class InventoryManager : MonoBehaviour
{
    //싱글톤
    public static InventoryManager Instance { get; private set; }

    //프로퍼티
    public int Count => _cards.Count;
    public bool IsFull => _cards.Count >= _maxCapacity;

    //변수
    [SerializeField]
    private int _maxCapacity = 200;

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

    //인벤토리에 카드 추가
    public void AddCard(int cardId)
    {
        if (IsFull)
        {
            Debug.LogWarning($"[InventoryManager] : 카드가 {_maxCapacity}를 초과했습니다!");
            return;
        }

        CardInstance cardInstance = new CardInstance(_nextInstanceId, cardId);
        _cards.Add(cardInstance);
        _nextInstanceId++;
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

    //인벤토리 확장
    public void ExpandCapacity(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[InventoryManager] : 인벤토리 확장을 음수로 할 수 없습니다!");
            return;
        }
        _maxCapacity += amount;
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
