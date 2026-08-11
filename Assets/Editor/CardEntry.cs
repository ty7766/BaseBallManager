/// <summary>
/// 드롭다운에 표시할 카드 1장의 정보
/// </summary>

public readonly struct CardEntry
{
    public int CardId { get; }
    public string TeamName { get; }
    public string Label { get; }

    public CardEntry (int  cardId, string teamName, string label)
    {
        CardId = cardId;
        TeamName = teamName;
        Label = label;
    }
}
