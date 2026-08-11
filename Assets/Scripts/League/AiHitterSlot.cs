using System;
using UnityEngine;
/// <summary>
/// AI ¶óÀÎ¾÷
/// </summary>
[Serializable]
public struct AiHitterSlot
{
    public HitterPosition Position => _position;
    public int CardId => _cardId;

    [SerializeField]
    private HitterPosition _position;

    [CardId(PlayerTypeFilter.HitterOnly)]
    [SerializeField] 
    private int _cardId;
}
