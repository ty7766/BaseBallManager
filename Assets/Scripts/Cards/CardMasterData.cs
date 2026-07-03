
public abstract class CardMasterData
{
    public int          CardId      { get; private set; }
    public string       Name        { get; private set; }
    public string       TeamName    { get; private set; }
    public int          Year        { get; private set; }
    public CardType     CardType    { get; private set; }
    public CardGrade    CardGrade   { get; private set; }
    public string       Position    { get; private set; }
    public int          OVR         {  get; private set; }



    public CardMasterData(int cardId, string name, string teamName, int year, CardType cardType, CardGrade cardGrade, string position, int ovr)
    {
        CardId = cardId;
        Name = name;
        TeamName = teamName;
        Year = year;
        CardType = cardType;
        CardGrade = cardGrade;
        Position = position;
        OVR = ovr;
    }
}
