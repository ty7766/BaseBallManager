using UnityEngine;
/// <summary>
/// 카드 검색 기능
/// </summary>
public class CardIdAttribute : PropertyAttribute
{
    public PlayerTypeFilter Filter { get; }

    //같은 구조체 안에서 포지션을 담고 있는 필드 이름.
    //슬롯마다 포지션이 달라지는 경우에 쓴다 (타자 라인업)
    public string PositionFieldName { get; }

    //이 필드에 고정된 포지션 (CSV 표기: "SP", "RP", "CP" ...).
    //필드 자체가 역할을 결정하는 경우에 쓴다 (투수 배열)
    public string FixedPosition { get; }

    //둘 다 null이면 포지션 필터링을 하지 않는다
    public CardIdAttribute (PlayerTypeFilter filter,
                            string positionFieldName = null,
                            string fixedPosition = null)
    {
        Filter = filter;
        PositionFieldName = positionFieldName;
        FixedPosition = fixedPosition;
    }
}
