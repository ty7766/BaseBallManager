/// <summary>
/// 대타 교체 1건을 표현하는 값 객체
/// </summary>
public struct Hittersubstitution
{
    public int BattingOrderIndex { get; }       //0~8 (타순)
    public int BenchIndex { get; }              //0~4 (벤치)

    public Hittersubstitution(int battingOrderIndex, int benchIndex)
    {
        BattingOrderIndex = battingOrderIndex;
        BenchIndex = benchIndex;
    }   
}
