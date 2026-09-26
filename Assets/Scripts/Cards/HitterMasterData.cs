/// <summary>
/// 타자 도감 데이터
/// </summary>
public class HitterMasterData : CardMasterData
{
    public int Power { get; }
    public int Contact { get; }
    public int Run { get; }
    public int Defense { get; }

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
