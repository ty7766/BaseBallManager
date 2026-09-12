/// <summary>
/// 1회 뽑기 결과를 표현하는 데이터
/// 뽑기 결과에 나타낼 데이터 (ID, Grade, CardType)
/// </summary>
public class GachaResult
{
    public int          CardId      { get; }
    public CardGrade    CardGrade   { get; }
    public CardType     CardType    { get; }

    public GachaResult(int cardId, CardGrade grade, CardType type)
    {
        CardId = cardId;
        CardGrade = grade;
        CardType = type;
    }
}
