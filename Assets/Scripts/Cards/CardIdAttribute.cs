using UnityEngine;
/// <summary>
/// 카드 검색 기능
/// </summary>
public class CardIdAttribute : PropertyAttribute
{
    public PlayerTypeFilter Filter { get; }
    public CardIdAttribute (PlayerTypeFilter filter)
    {
        Filter = filter;
    }
}
