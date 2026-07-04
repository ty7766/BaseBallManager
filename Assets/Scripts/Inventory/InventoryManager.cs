using UnityEngine;
using System.Collections.Generic;

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
}
