using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// 인벤토리 관리 시스템
/// </summary>
public class InventoryManager : SingletonBehaviour<InventoryManager>
{
    /// <summary>
    /// 카드 추가에 실패했을 때 돌려주는 instanceId
    /// </summary>
    public const int InvalidInstanceId = -1;

    /// <summary>
    /// 프로퍼티
    /// </summary>
    public int Count => _cards.Count;
    public int NextInstanceId => _nextInstanceId;
    public int ExpandUnit => _expandUnit;
    public int ExpandGoldCost => _expandGoldCost;
    public int MaxCapacity => _maxCapacity;
    public bool IsFull => Count >= _maxCapacity;

    //변수
    [SerializeField, Tooltip("기본 카드 보유 한도. 확장분은 여기에 더해진다")]
    private int _maxCapacity = 200;

    [Header("보유 한도 확장")]
    [SerializeField, Tooltip("1회 확장 시 늘어나는 칸 수")]
    private int _expandUnit = 10;
    [SerializeField, Tooltip("1회 확장에 드는 골드 (누진 곡선은 TBD)")]
    private int _expandGoldCost = 1000;

    private int _nextInstanceId = 1;
    private readonly Dictionary<int, CardInstance> _cards = new Dictionary<int, CardInstance>();

    /// <summary>
    /// 인벤토리에 카드 추가. 발급된 instanceId 반환 (실패 시 -1)
    /// </summary>
    public int AddCard(int cardId)
    {
        if (IsFull)
        {
            Debug.LogWarning($"[InventoryManager] : 보유 한도 {MaxCapacity} 칸이 가득 찼습니다");
            return InvalidInstanceId;
        }

        CardInstance cardInstance = new CardInstance(_nextInstanceId, cardId);

        _cards.Add(cardInstance.InstanceId, cardInstance);
        _nextInstanceId++;

        return cardInstance.InstanceId;
    }

    /// <summary>
    /// 인벤토리에서 카드 제거
    /// </summary>
    public bool RemoveCard(int instanceId)
    {
        if (!_cards.Remove(instanceId))
        {
            Debug.LogWarning($"[InventoryManager] : 제거하려는 카드가 존재하지 않습니다 : {instanceId}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 여러 장을 한 번에 제거한다. 하나라도 없으면 아무것도 제거하지 않는다
    /// </summary>
    public bool RemoveCards(IReadOnlyList<int> instanceIds)
    {
        if (instanceIds == null || instanceIds.Count == 0)
        {
            Debug.LogWarning("[InventoryManager] : 제거할 카드 목록이 비어 있습니다");
            return false;
        }

        for (int i = 0; i < instanceIds.Count; i++)
        {
            if (!_cards.ContainsKey(instanceIds[i]))
            {
                Debug.LogError($"[InventoryManager] : 제거하려는 카드가 존재하지 않습니다 : {instanceIds[i]}");
                return false;
            }

            for (int j = i + 1; j < instanceIds.Count; j++)
            {
                if (instanceIds[i] == instanceIds[j])
                {
                    Debug.LogError($"[InventoryManager] : 제거 목록에 같은 카드가 두 번 들어 있습니다 : {instanceIds[i]}");
                    return false;
                }
            }
        }

        foreach (int instanceId in instanceIds)
            _cards.Remove(instanceId);

        return true;
    }

    /// <summary>
    /// 카드 잠금
    /// </summary>
    public bool SetLocked(int instanceId, bool locked)
    {
        CardInstance foundCard = GetCard(instanceId);
        if (foundCard == null)
            return false;

        foundCard.SetLocked(locked);
        return true;
    }

    /// <summary>
    /// 인벤토리 확장
    /// </summary>
    public void ExpandCapacity(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[InventoryManager] : 인벤토리 확장을 음수 또는 0으로 할 수 없습니다!");
            return;
        }
        _maxCapacity += amount;
    }

    /// <summary>
    /// 골드를 내고 한도 확장
    /// </summary>
    public bool TryExpandCapacityWithGold()
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[InventoryManager] : CurrencyManager가 씬에 없습니다");
            return false;
        }

        if (!CurrencyManager.Instance.Spend(CurrencyType.Gold, _expandGoldCost))
            return false;

        ExpandCapacity(_expandUnit);
        return true;
    }

    /// <summary>
    /// 세이브 복원용 일괄 주입
    /// </summary>
    public bool Restore(List<CardInstance> cards, int nextInstanceId, int maxCapacity)
    {
        if (cards == null)
        {
            Debug.LogError("[InventoryManager] : 복원할 카드 목록이 null입니다");
            return false;
        }

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
            if (!_cards.TryAdd(card.InstanceId, card))
            {
                Debug.LogError($"[InventoryManager] : 세이브에 중복된 카드 ID가 있습니다 : {card.InstanceId}");
                _cards.Clear();
                return false;
            }
        }

        _nextInstanceId = nextInstanceId;

        if (maxCapacity > _maxCapacity)
            _maxCapacity = maxCapacity;

        return true;
    }

    /// <summary>
    /// 인벤토리 필터링. 판정 규칙은 CardFilter가 가진다
    /// </summary>
    public List<CardInstance> GetFiltered(CardFilter filter)
    {
        if (filter == null)
        {
            Debug.LogError("[InventoryManager] : 필터가 null입니다");
            return new List<CardInstance>();
        }

        CardDataManager cardDataManager = CardDataManager.Instance;

        if (cardDataManager == null)
        {
            Debug.LogError("[InventoryManager] : CardDataManager가 씬에 없습니다");
            return new List<CardInstance>();
        }

        List<CardInstance> result = new List<CardInstance>();

        foreach (CardInstance card in _cards.Values)
        {
            CardMasterData cardMasterData = cardDataManager.GetCardMasterData(card.CardId);

            if (cardMasterData == null)
                continue;

            if (filter.Matches(cardMasterData))
                result.Add(card);
        }

        return result;
    }

    /// <summary>
    /// 인벤토리에서 카드 반환
    /// </summary>
    public CardInstance GetCard(int instanceId)
    {
        if (!_cards.TryGetValue(instanceId, out CardInstance card))
        {
            Debug.LogWarning($"[InventoryManager] : 찾으려는 카드가 존재하지 않습니다 : {instanceId}");
            return null;
        }

        return card;
    }

    /// <summary>
    /// 인벤토리에서 전체 카드 반환
    /// </summary>
    public Dictionary<int, CardInstance>.ValueCollection GetAllCards()
    {
        return _cards.Values;
    }
}
