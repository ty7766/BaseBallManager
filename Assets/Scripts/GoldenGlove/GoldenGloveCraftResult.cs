/// <summary>
/// 골든글러브 제작 1회 결과 (기획서 4장)
/// </summary>
public class GoldenGloveCraftResult
{
    public int CardId { get; }
    public int InstanceId { get; }      //인벤토리에 들어간 실제 카드
    public string Name { get; }
    public string TeamName { get; }

    public GoldenGloveCraftResult(int cardId, int instanceId, string name, string teamName)
    {
        CardId = cardId;
        InstanceId = instanceId;
        Name = name;
        TeamName = teamName;
    }
}
