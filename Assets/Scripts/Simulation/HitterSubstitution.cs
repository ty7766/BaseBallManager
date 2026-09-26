/// <summary>
/// 대타 교체 1건 (타순 0~8 자리를 벤치 0~4번 카드로 바꾼다)
/// </summary>
public readonly struct HitterSubstitution
{
    public int BattingOrderIndex { get; }
    public int BenchIndex { get; }

    public HitterSubstitution(int battingOrderIndex, int benchIndex)
    {
        BattingOrderIndex = battingOrderIndex;
        BenchIndex = benchIndex;
    }
}
