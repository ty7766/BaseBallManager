/// <summary>
/// 강화·훈련이 반영된 투수 최종 스탯. 경기 시작 전 한 번만 계산해 주입한다
/// </summary>
public readonly struct PitcherSnapshot
{
    public int InstanceId { get; }
    public string Name { get; }
    public int Year { get; }            //카드 년도. 화면 표기용
    public string Position { get; }     //CSV 표기 그대로 ("SP", "RP", "CP")

    public int Velo { get; }
    public int Stuff { get; }
    public int Control { get; }
    public int Stamina { get; }

    /// <summary>
    /// 표기용 능력치. 최종 4스탯의 평균(내림)이라 강화·훈련이 이미 반영돼 있다
    /// </summary>
    public int OVR => (Velo + Stuff + Control + Stamina) / StatCount;

    private const int StatCount = 4;

    public PitcherSnapshot(int instanceId, string name, int year, string position,
        int velo, int stuff, int control, int stamina)
    {
        InstanceId = instanceId;
        Name = name;
        Year = year;
        Position = position;
        Velo = velo;
        Stuff = stuff;
        Control = control;
        Stamina = stamina;
    }
}
