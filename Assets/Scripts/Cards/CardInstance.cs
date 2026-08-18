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

    //세이브 복원 전용 생성자 - 저장된 상태를 그대로 되살린다
    public CardInstance(int instanceId, int cardId, int enhanceLevel, int trainLevel,
        bool breakthroughUsed, int[] trainDelta, bool isLocked)
    {
        InstanceId = instanceId;
        CardId = cardId;
        EnhanceLevel = enhanceLevel;
        TrainLevel = trainLevel;
        BreakthroughUsed = breakthroughUsed;
        IsLocked = isLocked;

        _trainDelta = new int[4];

        //세이브가 깨졌더라도 스탯 계산이 터지지 않도록 4칸은 항상 확보한다
        if (trainDelta == null || trainDelta.Length != 4)
        {
            UnityEngine.Debug.LogError($"[CardInstance] : 복원할 훈련 분배값이 올바르지 않습니다 (instanceId {instanceId})");
            return;
        }

        //외부 배열을 그대로 들고 있으면 세이브 DTO 쪽 수정이 카드에 새어 들어온다
        System.Array.Copy(trainDelta, _trainDelta, 4);
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
