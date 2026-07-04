
public class HitterMasterData : CardMasterData
{
    public int Power { get; private set; }
    public int Contact { get; private set; }
    public int Run { get; private set; }
    public int Defense { get; private set; }

    public HitterMasterData(int cardId, string name, string teamName, int year, 
        CardType cardType, CardGrade cardGrade, string position, 
        int power, int contact, int run, int defense, int ovr) 
        : base(cardId, name, teamName, year, cardType, cardGrade, position, ovr)
    {
        Power = power;
        Contact = contact;
        Run = run;
        Defense = defense;
    }
}
