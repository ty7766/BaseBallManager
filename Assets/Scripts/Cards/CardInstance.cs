using System;
using System.Collections.Generic;
using UnityEngine;

public class CardInstance
{
    private const int TrainStatCount = 4;

    public int InstanceId { get; private set; }              //인스턴스ID
    public int CardId { get; private set; }                  //카드 ID - CSV와 연결
    public int EnhanceLevel { get; private set; }            //강화 레벨
    public int TrainLevel { get; private set; }              //훈련 레벨
    public bool IsLocked { get; private set; }               //잠금 상태
    public bool BreakthroughUsed { get; private set; }       //훈련 돌파 사용 여부
    public IReadOnlyList<int> TrainDelta => _trainDelta;     //훈련 스탯 분배값

    private readonly int[] _trainDelta;

    //신규 카드 획득 시 초기 값 생성자
    public CardInstance(int instanceId, int cardId)
    {
        InstanceId = instanceId;
        CardId = cardId;
        TrainLevel = 1;
        EnhanceLevel = 0;
        IsLocked = false;
        BreakthroughUsed = false;
        _trainDelta = new int[TrainStatCount];
    }

    //세이브 복원 전용 생성자 - 저장된 상태를 그대로 되살린다
    public CardInstance(int instanceId, int cardId, int enhanceLevel, int trainLevel,
        bool breakthroughUsed, int[] trainDelta, bool isLocked)
        :this(instanceId, cardId)
    {
        EnhanceLevel = enhanceLevel;
        TrainLevel = trainLevel;
        BreakthroughUsed = breakthroughUsed;
        IsLocked = isLocked;

        if (trainDelta == null || trainDelta.Length != TrainStatCount)
        {
            Debug.LogError($"[CardInstance]: 복원할 훈련 분배값이 올바르지 않습니다 (instanceId {instanceId})");
            return;
        }

        Array.Copy(trainDelta, _trainDelta, TrainStatCount);
    }

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
    public bool ApplyTrain(int[] increasedStat)
    {
        if (increasedStat == null || increasedStat.Length != TrainStatCount)
        {
            Debug.LogError($"[CardInstance]: 훈련 분배 값이 올바르지 않습니다. (instanceId) = {InstanceId}");
            return false;
        }

        for (int i = 0; i < TrainStatCount; i++)
            _trainDelta[i] += increasedStat[i];

        TrainLevel++;
        return true;
    }

    //돌파 완료 표시
    public void ApplyBreakthrough()
    {
        BreakthroughUsed = true;
    }
}
