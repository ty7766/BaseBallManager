
public class PitcherMasterData : CardMasterData
{
    public int Velocity { get; private set; }
    public int Stuff { get; private set; }
    public int Control { get; private set; }
    public int Stamina { get; private set; }

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
