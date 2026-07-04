public class CardInstance
{
    public int InstanceId { get; private set; }             //인스턴스ID
    public int CardId { get; private set; }                  //카드 ID - CSV와 연결
    public int EnhanceLevel { get; private set; }           //강화 레벨
    public int TrainLevel { get; private set; }              //훈련 레벨
    public int[] TrainDelta { get; private set; }            //훈련 스탯 분배값
    public bool IsLocked { get; private set; }               //잠금 상태
    public bool BreakthroughUsed { get; private set; }       //훈련 돌파 사용 여부

    //카드를 처음 획득했을 때의 초기값 생성자
    public CardInstance(int instanceId, int cardId)
    {
        InstanceId = instanceId;
        CardId = cardId;
        EnhanceLevel = 0;
        TrainLevel = 1;
        TrainDelta = new int[4];
        IsLocked = false;
        BreakthroughUsed = false;
    }
}
