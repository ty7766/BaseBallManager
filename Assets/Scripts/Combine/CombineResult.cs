/// <summary>
/// 카드 조합 1회의 결과
/// </summary>
public class CombineResult
{
    public int InstanceId { get; }        //새로 생성된 카드의 인스턴스 ID
    public int CardId { get; }            //뽑힌 마스터 카드 ID
    public CardType CardType { get; }     //조합한 종류 (재료와 동일)
    public CardGrade BaseGrade { get; }   //승급 판정의 기준이 된 등급 (재료 3장 중 무작위 1장)
    public CardGrade Grade { get; }       //최종 결과 등급

    //기준 등급보다 올라갔는지. 결과 연출(승급 이펙트)용
    public bool IsUpgraded => Grade > BaseGrade;

    public CombineResult(int instanceId, int cardId, CardType cardType, CardGrade baseGrade, CardGrade grade)
    {
        InstanceId = instanceId;
        CardId = cardId;
        CardType = cardType;
        BaseGrade = baseGrade;
        Grade = grade;
    }
}
