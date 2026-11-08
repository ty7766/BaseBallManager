using UnityEngine;

/// <summary>
/// 보유 카드 1장을 화면에 그리는 데 필요한 값을 한 번에 모은다.
/// 마스터 데이터 + 인스턴스 + 최종 스탯 계산을 UI가 매번 조립하지 않도록 한 곳에 둔다
/// </summary>
public readonly struct CardDisplayInfo
{
    /// <summary>
    /// 타자 스탯 표기 순서
    /// </summary>
    public static readonly string[] HitterStatLabels = { "파워", "컨택", "주루", "수비" };

    /// <summary>
    /// 투수 스탯 표기 순서
    /// </summary>
    public static readonly string[] PitcherStatLabels = { "구속", "구위", "제어", "지구력" };

    public int InstanceId { get; }
    public int CardId { get; }
    public string Name { get; }
    public string TeamName { get; }
    public string Position { get; }
    public string PhotoKey { get; }
    public int Year { get; }
    public CardType CardType { get; }
    public CardGrade CardGrade { get; }
    public bool IsHitter { get; }

    public int EnhanceLevel { get; }
    public int TrainLevel { get; }
    public bool BreakthroughUsed { get; }
    public bool IsLocked { get; }

    public int BaseOvr { get; }
    public int FinalOvr { get; }

    private readonly int _base0;
    private readonly int _base1;
    private readonly int _base2;
    private readonly int _base3;
    private readonly int _delta0;
    private readonly int _delta1;
    private readonly int _delta2;
    private readonly int _delta3;

    private CardDisplayInfo(CardInstance card, CardMasterData master, bool isHitter,
        int base0, int base1, int base2, int base3)
    {
        InstanceId = card.InstanceId;
        CardId = master.CardId;
        Name = master.Name;
        TeamName = master.TeamName;
        Position = master.Position;
        PhotoKey = master.PhotoKey;
        Year = master.Year;
        CardType = master.CardType;
        CardGrade = master.CardGrade;
        IsHitter = isHitter;

        EnhanceLevel = card.EnhanceLevel;
        TrainLevel = card.TrainLevel;
        BreakthroughUsed = card.BreakthroughUsed;
        IsLocked = card.IsLocked;

        _base0 = base0;
        _base1 = base1;
        _base2 = base2;
        _base3 = base3;

        _delta0 = card.TrainDelta.Count > 0 ? card.TrainDelta[0] : 0;
        _delta1 = card.TrainDelta.Count > 1 ? card.TrainDelta[1] : 0;
        _delta2 = card.TrainDelta.Count > 2 ? card.TrainDelta[2] : 0;
        _delta3 = card.TrainDelta.Count > 3 ? card.TrainDelta[3] : 0;

        BaseOvr = master.OVR;
        FinalOvr = CardStatsCalculator.CalculateFinalOVR(master.OVR, card.EnhanceLevel, card.TrainDelta);
    }

    /// <summary>
    /// 스탯 표기 라벨 4개 (타자/투수에 따라 다르다)
    /// </summary>
    public string[] StatLabels => IsHitter ? HitterStatLabels : PitcherStatLabels;

    /// <summary>
    /// 보유 카드 1장의 표시 정보. 인벤토리나 마스터에 없으면 false
    /// </summary>
    public static bool TryCreate(int instanceId, out CardDisplayInfo info)
    {
        info = default;

        //아직 아무것도 고르지 않은 상태다. 조회 자체를 하지 않아 오류 로그를 남기지 않는다
        if (instanceId == InventoryManager.InvalidInstanceId)
            return false;

        if (InventoryManager.Instance == null || CardDataManager.Instance == null)
        {
            Debug.LogError("[CardDisplayInfo]: InventoryManager 또는 CardDataManager가 씬에 없습니다");
            return false;
        }

        CardInstance card = InventoryManager.Instance.GetCard(instanceId);

        if (card == null)
            return false;

        CardMasterData master = CardDataManager.Instance.GetCardMasterData(card.CardId);

        if (master == null)
            return false;

        if (master is HitterMasterData hitter)
        {
            info = new CardDisplayInfo(card, master, true,
                hitter.Power, hitter.Contact, hitter.Run, hitter.Defense);
            return true;
        }

        if (master is PitcherMasterData pitcher)
        {
            info = new CardDisplayInfo(card, master, false,
                pitcher.Velocity, pitcher.Stuff, pitcher.Control, pitcher.Stamina);
            return true;
        }

        Debug.LogError($"[CardDisplayInfo]: 타자도 투수도 아닌 카드입니다 (cardId {card.CardId})");

        return false;
    }

    /// <summary>
    /// 강화·훈련 전 기본 스탯 (index 0~3)
    /// </summary>
    public int GetBaseStat(int index)
    {
        return index switch
        {
            0 => _base0,
            1 => _base1,
            2 => _base2,
            3 => _base3,
            _ => 0
        };
    }

    /// <summary>
    /// 훈련으로 더해진 값 (index 0~3)
    /// </summary>
    public int GetTrainDelta(int index)
    {
        return index switch
        {
            0 => _delta0,
            1 => _delta1,
            2 => _delta2,
            3 => _delta3,
            _ => 0
        };
    }

    /// <summary>
    /// 강화·훈련이 반영된 최종 스탯 (index 0~3)
    /// </summary>
    public int GetFinalStat(int index)
    {
        return CardStatsCalculator.CalculateFinalStat(GetBaseStat(index), EnhanceLevel, GetTrainDelta(index));
    }

    /// <summary>
    /// 훈련 상한 (돌파 여부에 따라 다르다). TrainManager가 없으면 0
    /// </summary>
    public int GetMaxTrainLevel()
    {
        if (TrainManager.Instance == null)
            return 0;

        return BreakthroughUsed
            ? TrainManager.Instance.MaxTrainLevelAfterBreakthrough
            : TrainManager.Instance.MaxTrainLevel;
    }
}
