using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 뽑기 기록. 오래된 것부터 쌓고, 보관 한도를 넘으면 가장 오래된 기록부터 지운다
/// </summary>
public class GachaHistory
{
    private const int InitialCapacity = 64;

    /// <summary>
    /// 오래된 순 전체 기록
    /// </summary>
    public IReadOnlyList<GachaHistoryEntry> Entries => _entries;

    private readonly List<GachaHistoryEntry> _entries = new List<GachaHistoryEntry>(InitialCapacity);

    /// <summary>
    /// 1줄 추가. 한도를 넘으면 가장 오래된 기록을 지운다
    /// </summary>
    public void Add(GachaHistoryEntry entry, int limit)
    {
        _entries.Add(entry);
        TrimToLimit(limit);
    }

    /// <summary>
    /// 세이브 복원. 지금 기록을 비우고 오래된 순 목록으로 채운다
    /// </summary>
    public void Restore(IReadOnlyList<GachaHistoryEntry> entries, int limit)
    {
        _entries.Clear();
        _entries.AddRange(entries);
        TrimToLimit(limit);
    }

    //한도를 넘친 만큼 앞(오래된 쪽)에서 한 번에 지운다
    private void TrimToLimit(int limit)
    {
        int overflow = _entries.Count - Mathf.Max(0, limit);

        if (overflow > 0)
            _entries.RemoveRange(0, overflow);
    }
}
