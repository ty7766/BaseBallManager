using System;
using UnityEngine;

/// <summary>
/// 포스트시즌 단계 1개의 보상 수량 (기획서 7.5 - 보너스 콘텐츠)
/// </summary>
[Serializable]
public struct PostSeasonReward
{
    public int Gold => _gold;
    public int GoldenGlovePoint => _goldenGlovePoint;
    public int SignatureTicket => _signatureTicket;
    public int GoldenGloveEnhanceCard => _goldenGloveEnhanceCard;

    [SerializeField, Tooltip("골드")]
    private int _gold;

    [SerializeField, Tooltip("골든글러브 포인트")]
    private int _goldenGlovePoint;

    [SerializeField, Tooltip("시그니쳐 뽑기권")]
    private int _signatureTicket;

    [SerializeField, Tooltip("골카 전용 강화 카드")]
    private int _goldenGloveEnhanceCard;
}
