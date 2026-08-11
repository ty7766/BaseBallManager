/// <summary>
/// 시뮬레이션 전 현재 라인업에 등록되어있는 투수들의 정보를 기록
/// 이 스냅샷으로 시뮬레이션 전 한 번만 계산하여 시뮬레이션 진행
/// </summary>

public struct PitcherSnapshot
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
