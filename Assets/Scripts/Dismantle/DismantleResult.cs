/// <summary>
/// 카드 분해 1건의 보상 (기획서 9.2)
/// </summary>
public class DismantleResult
{
    public int Point { get; }                //획득 포인트
    public int TrainCard { get; }            //획득 훈련 카드 (확률 드랍이라 0일 수 있음)
    public int GoldenGlovePoint { get; }     //획득 골든글러브 포인트 (골글 카드만)

    public DismantleResult(int point, int trainCard, int goldenGlovePoint)
    {
        Point = point;
        TrainCard = trainCard;
        GoldenGlovePoint = goldenGlovePoint;
    }
}
