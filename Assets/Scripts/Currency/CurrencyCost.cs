/// <summary>
/// 재화 1종의 소모량 (여러 재화를 동시에 쓰는 비용 표현용)
/// </summary>
/// <remarks>
/// 훈련은 포인트 + 훈련 카드를 함께 소모하고(기획서 2.2), 골글 제작도 재료가 여러 종이다(기획서 4장).
/// 이런 비용을 하나의 목록으로 넘겨 부분 차감을 원천 차단하기 위한 값 타입이다.
/// </remarks>
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
