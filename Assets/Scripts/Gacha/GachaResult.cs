/// <summary>
/// 1회 뽑기 결과를 표현하는 데이터
/// 뽑기 결과에 나타낼 데이터 (ID, Grade, CardType)
/// </summary>
public class GachaResult
{
    /// <summary>
    /// 아직 인벤토리에 넣지 않은 결과
    /// </summary>
    public const int NoInstanceId = -1;

    public int          CardId      { get; }
    public CardGrade    CardGrade   { get; }
    public CardType     CardType    { get; }

    /// <summary>
    /// 인벤토리에 들어간 카드의 instanceId. 지급 전이면 NoInstanceId
    /// </summary>
    public int          InstanceId  { get; }

    public GachaResult(int cardId, CardGrade grade, CardType type)
        : this(cardId, grade, type, NoInstanceId)
    {
    }

    public GachaResult(int cardId, CardGrade grade, CardType type, int instanceId)
    {
        CardId = cardId;
        CardGrade = grade;
        CardType = type;
        InstanceId = instanceId;
    }
}
