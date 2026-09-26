/// <summary>
/// 재화 1종의 소모량 (여러 재화를 동시에 쓰는 비용 표현용)
/// </summary>
public readonly struct CurrencyCost
{
    public CurrencyType Type { get; }
    public int Amount { get; }

    public CurrencyCost(CurrencyType type, int amount)
    {
        Type = type;
        Amount = amount;
    }
}
