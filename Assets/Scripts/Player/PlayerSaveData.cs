using System;

/// <summary>
/// 보유 카드 1장의 저장 형태 (기획서 1.4 - B 인스턴스)
/// </summary>
[Serializable]
public class CardInstanceSaveData
{
    public int InstanceId;
    public int CardId;
    public int EnhanceLevel;
    public int TrainLevel;
    public bool BreakthroughUsed;

    /// <summary>
    /// 훈련 랜덤 분배값 4칸. 레벨만으로는 복원할 수 없다 (기획서 1.4 경고)
    /// </summary>
    public int[] TrainDelta;

    public bool IsLocked;
}

/// <summary>
/// 라인업 편성의 저장 형태 (기획서 6장)
/// </summary>
[Serializable]
public class LineUpSaveData
{
    /// <summary>
    /// 아래 3개 배열은 같은 길이이며 인덱스가 서로 대응한다
    /// </summary>
    public int[] HitterPositions;        //(int)HitterPosition
    public int[] HitterInstanceIds;      //빈 슬롯은 -1
    public int[] HitterBattingOrders;    //빈 슬롯은 0

    public int[] BenchInstanceIds;       //5칸, 빈 칸 -1
    public int[] StartingPitcherIds;     //SP 5칸
    public int[] RelieverPitcherIds;     //RP 5칸
    public int[] CloserPitcherIds;       //CP 1칸
}

/// <summary>
/// 플레이어 진행 데이터의 저장 형태 (기획서 10장)
/// </summary>
[Serializable]
public class PlayerSaveData
{
    public string PlayerTeamName;
    public int HighestUnlockedTier;
    public bool TutorialCompleted;

    /// <summary>
    /// 아래 2개 배열은 같은 길이이며 인덱스가 서로 대응한다 (Dictionary 직렬화 불가 회피)
    /// </summary>
    public int[] CurrencyTypes;          //(int)CurrencyType
    public int[] CurrencyAmounts;

    public CardInstanceSaveData[] Cards;
    public int NextInstanceId;
    public int MaxCapacity;

    /// <summary>
    /// 천장 카운터는 세션을 넘어가도 유지 (기획서 3장)
    /// </summary>
    public int NormalPityCount;
    public int SignaturePityCount;

    public LineUpSaveData LineUp;
}
