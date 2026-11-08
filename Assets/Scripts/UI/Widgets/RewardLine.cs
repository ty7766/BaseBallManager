/// <summary>
/// 보상 팝업 한 줄 — 재화 아이콘과 받은 양
/// </summary>
public readonly struct RewardLine
{
    public string IconPath { get; }
    public int Amount { get; }

    public RewardLine(string iconPath, int amount)
    {
        IconPath = iconPath;
        Amount = amount;
    }
}
