using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 모든 재화 보관 · 증감 (기획서 9.1)
/// </summary>
/// <remarks>
/// 재화를 "얼마나 주는가"는 각 시스템(시작 보상 · 리그 보상 · 분해)의 규칙이다.
/// 이 클래스는 금고 역할만 하며 스스로 재화를 만들어내지 않는다.
/// </remarks>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    //세이브 저장용 읽기 전용 뷰
    public IReadOnlyDictionary<CurrencyType, int> Amounts => _amounts;

    private readonly Dictionary<CurrencyType, int> _amounts = new Dictionary<CurrencyType, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //보유량 조회. 한 번도 획득한 적 없는 재화는 0 (정상 상태이므로 경고를 남기지 않음)
    public int GetAmount(CurrencyType type)
    {
        return _amounts.TryGetValue(type, out int amount) ? amount : 0;
    }

    //보유량이 요구량 이상인지 (UI 버튼 활성화 판정용)
    public bool CanAfford(CurrencyType type, int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[CurrencyManager]: 요구량이 {amount}입니다 ({type})");
            return false;
        }

        return GetAmount(type) >= amount;
    }

    //재화 획득
    public bool Add(CurrencyType type, int amount)
    {
        //호출부 버그 - 지급 대상 재화가 지정되지 않았다는 뜻
        if (type == CurrencyType.None)
        {
            Debug.LogError("[CurrencyManager]: 지급할 재화 종류가 None입니다");
            return false;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"[CurrencyManager]: 지급량이 {amount}입니다 ({type})");
            return false;
        }

        int current = GetAmount(type);

        //오버플로가 나면 보유량이 음수로 뒤집혀 조용히 재화가 증발한다
        if (current > int.MaxValue - amount)
        {
            Debug.LogWarning($"[CurrencyManager]: {type} 보유량이 상한에 도달해 int.MaxValue로 고정합니다");
            _amounts[type] = int.MaxValue;
            return true;
        }

        _amounts[type] = current + amount;
        return true;
    }

    //재화 소모. 부족하면 차감하지 않고 실패
    public bool Spend(CurrencyType type, int amount)
    {
        if (!CanAfford(type, amount))
        {
            Debug.LogWarning($"[CurrencyManager]: {type}이(가) 부족합니다 (보유 {GetAmount(type)} / 필요 {amount})");
            return false;
        }

        _amounts[type] = GetAmount(type) - amount;
        return true;
    }

    //여러 재화 동시 소모. 하나라도 부족하면 아무것도 차감하지 않는다
    public bool SpendAll(IReadOnlyList<CurrencyCost> costs)
    {
        if (costs == null || costs.Count == 0)
        {
            Debug.LogWarning("[CurrencyManager]: 소모할 비용 목록이 비어 있습니다");
            return false;
        }

        //1차 - 전부 지불 가능한지만 확인 (여기서는 차감하지 않는다)
        foreach (CurrencyCost cost in costs)
        {
            if (!CanAfford(cost.Type, cost.Amount))
            {
                Debug.LogWarning($"[CurrencyManager]: {cost.Type}이(가) 부족합니다 (보유 {GetAmount(cost.Type)} / 필요 {cost.Amount})");
                return false;
            }
        }

        //2차 - 여기서 비로소 차감. 강화 · 훈련은 되돌릴 수 없어 부분 차감이 곧 손실이다 (기획서 2장)
        foreach (CurrencyCost cost in costs)
        {
            _amounts[cost.Type] = GetAmount(cost.Type) - cost.Amount;
        }

        return true;
    }

    //세이브 복원용 일괄 주입 (두 배열은 인덱스가 서로 대응한다)
    public void Restore(CurrencyType[] types, int[] amounts)
    {
        if (types == null || amounts == null || types.Length != amounts.Length)
        {
            Debug.LogError("[CurrencyManager]: 복원할 재화 배열이 올바르지 않습니다");
            return;
        }

        _amounts.Clear();

        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == CurrencyType.None)
                continue;

            _amounts[types[i]] = amounts[i];
        }
    }
}
