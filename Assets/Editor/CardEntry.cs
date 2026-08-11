/// <summary>
/// 드롭다운에 표시할 카드 1장의 정보
/// </summary>

public readonly struct CardEntry
{
    public int CardId { get; }
    public string TeamName { get; }
    public string Position { get; }     //CSV 표기 그대로 ("LF", "1B", "SP" ...)
    public string Label { get; }

    public CardEntry (int  cardId, string teamName, string position, string label)
    {
        CardId = cardId;
        TeamName = teamName;
        Position = position;
        Label = label;
    }
}
