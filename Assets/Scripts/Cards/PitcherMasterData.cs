/// <summary>
/// 투수 도감 데이터
/// </summary>
public class PitcherMasterData : CardMasterData
{
    public int Velocity { get; }
    public int Stuff { get; }
    public int Control { get; }
    public int Stamina { get; }

    public PitcherMasterData(int cardId, string name, string teamName, int year,
        CardType cardType, CardGrade cardGrade, string position,
        int velocity, int stuff, int control, int stamina, int ovr)
        : base(cardId, name, teamName, year, cardType, cardGrade, position, ovr)
    {
        Velocity = velocity;
        Stuff = stuff;
        Control = control;
        Stamina = stamina;
    }
}
