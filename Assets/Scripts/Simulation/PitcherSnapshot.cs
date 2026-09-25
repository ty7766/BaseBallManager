/// <summary>
/// 강화·훈련이 반영된 투수 최종 스탯. 경기 시작 전 한 번만 계산해 주입한다
/// </summary>
public readonly struct PitcherSnapshot
{
    public int InstanceId { get; }
    public string Name { get; }
    public int Velo { get; }
    public int Stuff { get; }
    public int Control { get; }
    public int Stamina { get; }

    public PitcherSnapshot(int instanceId, string name, int velo, int stuff, int control, int stamina)
    {
        InstanceId = instanceId;
        Name = name;
        Velo = velo;
        Stuff = stuff;
        Control = control;
        Stamina = stamina;      
    }
}
