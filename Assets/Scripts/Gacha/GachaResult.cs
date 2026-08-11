/// <summary>
/// 1회 뽑기 결과를 표현하는 데이터
/// 뽑기 결과에 나타낼 데이터 (ID, Grade, CardType) 반환
/// </summary>
public class GachaResult
{
    public int          CardId  { get; private set; }
    public CardGrade    Grade   { get; private set; }
    public CardType     Type    { get; private set; }

    public GachaResult (int cardId, CardGrade grade, CardType type)
    {
        CardId = cardId;
        Grade = grade;
        Type = type;
    }
}
