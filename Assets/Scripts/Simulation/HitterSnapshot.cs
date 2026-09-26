/// <summary>
/// 강화·훈련이 반영된 타자 최종 스탯. 경기 시작 전 한 번만 계산해 주입한다
/// </summary>
public readonly struct HitterSnapshot
{
    public int InstanceId { get; }
    public string Name { get; }
    public int Power { get; }
    public int Contact { get; }
    public int Run { get; }
    public int Defense { get; }

    public HitterSnapshot(int instanceId, string name, int power, int contact, int run, int defense)
    {
        InstanceId = instanceId;
        Name = name;
        Power = power;
        Contact = contact;
        Run = run;
        Defense = defense;
    }
}
