using System;
using UnityEngine;
/// <summary>
/// AI 라인업
/// </summary>
[Serializable]
public struct AiHitterSlot
{
    public HitterPosition Position => _position;
    public int CardId => _cardId;

    [SerializeField]
    private HitterPosition _position;

    [CardId(PlayerTypeFilter.HitterOnly, nameof(_position))]
    [SerializeField]
    private int _cardId;
}
