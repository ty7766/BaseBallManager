
public abstract class CardMasterData
{
    public int          CardId      { get; private set; }
    public string       Name        { get; private set; }
    public string       TeamName    { get; private set; }
    public int          Year        { get; private set; }
    public CardType     CardType    { get; private set; }
    public CardGrade    CardGrade   { get; private set; }
    public string       Position    { get; private set; }



    public CardMasterData(int cardId, string name, string teamName, int year, CardType cardType, CardGrade cardGrade, string position)
    {
        CardId = cardId;
        Name = name;
        TeamName = teamName;
        Year = year;
        CardType = cardType;
        CardGrade = cardGrade;
        Position = position;
    }




    //카드 스탯을 합산하는 메소드
    protected abstract int GetStatSum();

    //카드 스탯의 평균 OVR을 계산하는 메소드
    public int CalculateOVR() => GetStatSum() / 4;
}
