using System.Collections.Generic;

public class CardInstance
{
    public int InstanceId { get; private set; }             //인스턴스ID
    public int CardId { get; private set; }                  //카드 ID - CSV와 연결
    public int EnhanceLevel { get; private set; }           //강화 레벨
    public int TrainLevel { get; private set; }              //훈련 레벨
    public bool IsLocked { get; private set; }               //잠금 상태
    public bool BreakthroughUsed { get; private set; }       //훈련 돌파 사용 여부

    private int[] _trainDelta;
    public IReadOnlyList<int> TrainDelta => _trainDelta;     //훈련 스탯 분배값

    //카드를 처음 획득했을 때의 초기값 생성자
    public CardInstance(int instanceId, int cardId)
    {
        InstanceId = instanceId;
        CardId = cardId;
        EnhanceLevel = 0;
        TrainLevel = 1;
        _trainDelta = new int[4];
        IsLocked = false;
        BreakthroughUsed = false;
    }

    /// <summary>
    /// (외부 접근용) 카드의 인게임 속성을 관리 및 호출
    /// </summary>

    //카드 잠금 설정
    public void SetLocked(bool locked)
    {
        IsLocked = locked;
    }

    //강화 레벨 1 증가
    public void ApplyEnhance()
    {
        EnhanceLevel++;
    }

    //훈련 레벨 1 증가 + 스탯 분배 반영
    //delta : 이번 레벨 업에서 오른 각 스탯 증가량
    public void ApplyTrain(int[] delta)
    {
        for (int i = 0;  i < _trainDelta.Length; i++)
        {
            _trainDelta[i] += delta[i];
        }

        TrainLevel++;
    }

    //돌파 완료 표시
    public void ApplyBreakthrough()
    {
        BreakthroughUsed = true;
    }
}
