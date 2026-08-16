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

    //훈련 랜덤 분배값 4칸. 레벨만으로는 복원할 수 없다 (기획서 1.4 경고)
    public int[] TrainDelta;

    public bool IsLocked;
}

/// <summary>
/// 라인업 편성의 저장 형태 (기획서 6장)
/// </summary>
/// <remarks>JsonUtility는 Dictionary를 직렬화하지 못하므로 야수 슬롯을 3개의 나란한 배열로 편다</remarks>
[Serializable]
public class LineUpSaveData
{
    //아래 3개 배열은 같은 길이이며 인덱스가 서로 대응한다
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
/// <remarks>
/// 리그 진행도는 별도 세이브(LeagueSaveData)가 담당한다.
/// 여기에는 리그를 넘어 유지되는 것(팀 · 해금 · 재화 · 보유 카드 · 라인업 · 천장)만 담는다.
/// </remarks>
[Serializable]
public class PlayerSaveData
{
    public string PlayerTeamName;
    public int HighestUnlockedTier;
    public bool TutorialCompleted;

    //아래 2개 배열은 같은 길이이며 인덱스가 서로 대응한다 (Dictionary 직렬화 불가 회피)
    public int[] CurrencyTypes;          //(int)CurrencyType
    public int[] CurrencyAmounts;

    public CardInstanceSaveData[] Cards;
    public int NextInstanceId;
    public int MaxCapacity;

    //천장 카운터는 세션을 넘어가도 유지 (기획서 3장)
    public int NormalPityCount;
    public int SignaturePityCount;

    public LineUpSaveData LineUp;
}
