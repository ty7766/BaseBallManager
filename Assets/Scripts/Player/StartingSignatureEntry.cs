using System;
using UnityEngine;

/// <summary>
/// 팀 1개의 시작 지급 시그니쳐 카드 (기획서 5장의 2단계)
/// </summary>
[Serializable]
public struct StartingSignatureEntry
{
    public string TeamName => _teamName;
    public int CardId => _cardId;

    [SerializeField, Tooltip("선택 가능한 10팀 중 하나 (기획서 5장)")]
    private string _teamName;

    //타자·투수 어느 쪽이든 지급할 수 있으므로 포지션 필터를 걸지 않는다.
    //빈 슬롯 센티넬은 0 (cardId는 1부터 시작 - 세션 33 규약)
    [CardId(PlayerTypeFilter.All)]
    [SerializeField, Tooltip("이 팀을 고른 플레이어에게 지급할 시그니쳐 카드")]
    private int _cardId;
}
