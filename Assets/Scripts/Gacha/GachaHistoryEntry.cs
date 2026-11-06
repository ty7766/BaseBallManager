using System;

/// <summary>
/// 뽑기 기록 1줄. 어떤 카드를 · 어느 뽑기로 · 언제(UTC) 뽑았는지
/// </summary>
public readonly struct GachaHistoryEntry
{
    public int CardId { get; }
    public GachaType GachaType { get; }
    public long RolledAtUtcTicks { get; }

    public GachaHistoryEntry(int cardId, GachaType gachaType, long rolledAtUtcTicks)
    {
        CardId = cardId;
        GachaType = gachaType;
        RolledAtUtcTicks = rolledAtUtcTicks;
    }

    /// <summary>
    /// 뽑은 시각 (기기 현지 시간)
    /// </summary>
    public DateTime RolledAtLocal => new DateTime(RolledAtUtcTicks, DateTimeKind.Utc).ToLocalTime();
}
