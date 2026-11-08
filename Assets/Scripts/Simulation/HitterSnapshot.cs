/// <summary>
/// 강화·훈련이 반영된 타자 최종 스탯. 경기 시작 전 한 번만 계산해 주입한다
/// </summary>
public readonly struct HitterSnapshot
{
    public int InstanceId { get; }
    public string Name { get; }
    public int Year { get; }            //카드 년도. 화면 표기용
    public string Position { get; }     //CSV 표기 그대로 ("LF", "1B" ...)

    public int Power { get; }
    public int Contact { get; }
    public int Run { get; }
    public int Defense { get; }

    /// <summary>
    /// 표기용 능력치. 최종 4스탯의 평균(내림)이라 강화·훈련이 이미 반영돼 있다
    /// </summary>
    public int OVR => (Power + Contact + Run + Defense) / StatCount;

    private const int StatCount = 4;

    public HitterSnapshot(int instanceId, string name, int year, string position,
        int power, int contact, int run, int defense)
    {
        InstanceId = instanceId;
        Name = name;
        Year = year;
        Position = position;
        Power = power;
        Contact = contact;
        Run = run;
        Defense = defense;
    }
}
