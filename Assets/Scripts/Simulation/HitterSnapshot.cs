/// <summary>
/// 시뮬레이션 전 현재 라인업에 등록되어있는 타자들의 정보를 기록
/// 이 스냅샷으로 시뮬레이션 전 한 번만 계산하여 시뮬레이션 진행
/// </summary>

public struct HitterSnapshot
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
