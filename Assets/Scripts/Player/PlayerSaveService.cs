using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 진행 데이터 저장·복원 (기획서 10장)
/// </summary>
public class PlayerSaveService
{
    public const string SaveKey = "player";

    private readonly ISaveStorage _storage;

    public PlayerSaveService(ISaveStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// 저장된 플레이어 데이터가 있는지 (이어하기 / 새 게임 분기 판단용)
    /// </summary>
    public bool HasSave()
    {
        return _storage.Exists(SaveKey);
    }

    /// <summary>
    /// 현재 진행 상황 저장
    /// </summary>
    public bool Save()
    {
        if (!AreManagersReady())
            return false;

        Dictionary<int, CardInstance>.ValueCollection cards = InventoryManager.Instance.GetAllCards();

        PlayerSaveData saveData = new PlayerSaveData
        {
            PlayerTeamName = PlayerDataManager.Instance.PlayerTeamName,
            HighestUnlockedTier = (int)PlayerDataManager.Instance.HighestUnlockedTier,
            TutorialCompleted = PlayerDataManager.Instance.TutorialCompleted,

            NextInstanceId = InventoryManager.Instance.NextInstanceId,
            MaxCapacity = InventoryManager.Instance.MaxCapacity,

            NormalPityCount = GachaManager.Instance.NormalPityCount,
            SignaturePityCount = GachaManager.Instance.SignaturePityCount,

            Cards = new CardInstanceSaveData[cards.Count],
            LineUp = BuildLineUpSaveData()
        };

        WriteCurrencies(saveData);

        int cardIndex = 0;

        foreach (CardInstance card in cards)
        {
            saveData.Cards[cardIndex] = ToSaveData(card);
            cardIndex++;
        }

        return _storage.Save(SaveKey, JsonUtility.ToJson(saveData, true));
    }

    /// <summary>
    /// 저장된 진행 상황을 각 매니저에 되살린다
    /// </summary>
    public bool Load()
    {
        if (!AreManagersReady())
            return false;

        string json = _storage.Load(SaveKey);

        if (string.IsNullOrEmpty(json))
            return false;

        PlayerSaveData saveData = JsonUtility.FromJson<PlayerSaveData>(json);

        if (!Validate(saveData))
            return false;

        List<CardInstance> cards = new List<CardInstance>(saveData.Cards.Length);

        foreach (CardInstanceSaveData cardData in saveData.Cards)
            cards.Add(ToCardInstance(cardData));

        //검증을 통과했으므로 아래 복원은 실패하지 않는다. 실패하면 검증기와 복원기의 규칙이 어긋난 것이다
        if (!PlayerDataManager.Instance.Restore(saveData.PlayerTeamName,
                (LeagueTier)saveData.HighestUnlockedTier, saveData.TutorialCompleted)
            || !RestoreCurrencies(saveData)
            || !InventoryManager.Instance.Restore(cards, saveData.NextInstanceId, saveData.MaxCapacity))
        {
            Debug.LogError("[PlayerSaveService]: 검증을 통과한 세이브의 복원이 실패했습니다");
            return false;
        }

        GachaManager.Instance.RestorePityCounts(saveData.NormalPityCount, saveData.SignaturePityCount);

        RestoreLineUp(saveData.LineUp);

        return true;
    }

    /// <summary>
    /// 세이브 삭제 (처음부터 다시 시작)
    /// </summary>
    public bool Delete()
    {
        return _storage.Delete(SaveKey);
    }

    //어느 매니저도 건드리기 전에 전부 검사한다. 중간에 멈추면 되돌릴 수 없는 부분 적용 상태가 남는다
    private static bool Validate(PlayerSaveData saveData)
    {
        if (saveData == null || saveData.Cards == null)
        {
            Debug.LogError("[PlayerSaveService]: 세이브 데이터를 읽지 못했습니다");
            return false;
        }

        if (string.IsNullOrEmpty(saveData.PlayerTeamName))
        {
            Debug.LogError("[PlayerSaveService]: 세이브에 플레이어 팀이 없습니다");
            return false;
        }

        if (!LeagueTierTable.IsValidTier((LeagueTier)saveData.HighestUnlockedTier))
        {
            Debug.LogError($"[PlayerSaveService]: 세이브의 해금 티어가 올바르지 않습니다 ({saveData.HighestUnlockedTier})");
            return false;
        }

        if (saveData.NormalPityCount < 0 || saveData.SignaturePityCount < 0)
        {
            Debug.LogError($"[PlayerSaveService]: 세이브의 천장 카운터가 음수입니다 (일반 {saveData.NormalPityCount} / 시그 {saveData.SignaturePityCount})");
            return false;
        }

        return ValidateCurrencies(saveData) && ValidateCards(saveData);
    }

    //재화 - 두 배열의 길이 대응과 음수 보유량
    private static bool ValidateCurrencies(PlayerSaveData saveData)
    {
        if (saveData.CurrencyTypes == null || saveData.CurrencyAmounts == null
            || saveData.CurrencyTypes.Length != saveData.CurrencyAmounts.Length)
        {
            Debug.LogError("[PlayerSaveService]: 세이브의 재화 배열이 올바르지 않습니다");
            return false;
        }

        for (int i = 0; i < saveData.CurrencyAmounts.Length; i++)
        {
            if (saveData.CurrencyAmounts[i] >= 0)
                continue;

            Debug.LogError($"[PlayerSaveService]: 세이브의 재화 보유량이 음수입니다 ({(CurrencyType)saveData.CurrencyTypes[i]} {saveData.CurrencyAmounts[i]})");
            return false;
        }

        return true;
    }

    //보유 카드 - 빈 항목 · 발급 ID 정합 · ID 중복 · 훈련 분배 배열 길이
    private static bool ValidateCards(PlayerSaveData saveData)
    {
        HashSet<int> instanceIds = new HashSet<int>(saveData.Cards.Length);

        foreach (CardInstanceSaveData cardData in saveData.Cards)
        {
            if (cardData == null)
            {
                Debug.LogError("[PlayerSaveService]: 보유 카드 목록에 비어 있는 항목이 있습니다");
                return false;
            }

            if (cardData.InstanceId >= saveData.NextInstanceId)
            {
                Debug.LogError($"[PlayerSaveService]: 다음 발급 ID({saveData.NextInstanceId})가 보유 카드 ID({cardData.InstanceId})보다 작거나 같습니다");
                return false;
            }

            if (!instanceIds.Add(cardData.InstanceId))
            {
                Debug.LogError($"[PlayerSaveService]: 세이브에 중복된 카드 ID가 있습니다 : {cardData.InstanceId}");
                return false;
            }

            if (cardData.TrainDelta == null || cardData.TrainDelta.Length != CardInstance.TrainStatCount)
            {
                Debug.LogError($"[PlayerSaveService]: 훈련 분배값이 {CardInstance.TrainStatCount}칸이 아닙니다 (instanceId {cardData.InstanceId})");
                return false;
            }
        }

        return true;
    }

    //필요한 매니저가 전부 씬에 있는지 확인
    private static bool AreManagersReady()
    {
        if (PlayerDataManager.Instance == null || InventoryManager.Instance == null
            || CurrencyManager.Instance == null || GachaManager.Instance == null
            || LineUpManager.Instance == null)
        {
            Debug.LogError("[PlayerSaveService]: 필요한 매니저가 씬에 없습니다 (Player / Inventory / Currency / Gacha / LineUp)");
            return false;
        }

        return true;
    }

    //보유 재화를 나란한 배열 2개로 펴서 담는다
    private static void WriteCurrencies(PlayerSaveData saveData)
    {
        IReadOnlyDictionary<CurrencyType, int> amounts = CurrencyManager.Instance.Amounts;

        saveData.CurrencyTypes = new int[amounts.Count];
        saveData.CurrencyAmounts = new int[amounts.Count];

        int index = 0;

        foreach (KeyValuePair<CurrencyType, int> pair in amounts)
        {
            saveData.CurrencyTypes[index] = (int)pair.Key;
            saveData.CurrencyAmounts[index] = pair.Value;
            index++;
        }
    }

    //나란한 배열 2개 -> 재화
    private static bool RestoreCurrencies(PlayerSaveData saveData)
    {
        if (saveData.CurrencyTypes == null || saveData.CurrencyAmounts == null)
        {
            Debug.LogError("[PlayerSaveService]: 세이브에 재화 정보가 없습니다");
            return false;
        }

        CurrencyType[] types = new CurrencyType[saveData.CurrencyTypes.Length];

        for (int i = 0; i < types.Length; i++)
            types[i] = (CurrencyType)saveData.CurrencyTypes[i];

        return CurrencyManager.Instance.Restore(types, saveData.CurrencyAmounts);
    }

    //카드 -> 저장 형태
    private static CardInstanceSaveData ToSaveData(CardInstance card)
    {
        int[] trainDelta = new int[card.TrainDelta.Count];

        for (int i = 0; i < trainDelta.Length; i++)
            trainDelta[i] = card.TrainDelta[i];

        return new CardInstanceSaveData
        {
            InstanceId = card.InstanceId,
            CardId = card.CardId,
            EnhanceLevel = card.EnhanceLevel,
            TrainLevel = card.TrainLevel,
            BreakthroughUsed = card.BreakthroughUsed,
            TrainDelta = trainDelta,
            IsLocked = card.IsLocked
        };
    }

    //저장 형태 -> 카드
    private static CardInstance ToCardInstance(CardInstanceSaveData cardData)
    {
        return new CardInstance(cardData.InstanceId, cardData.CardId, cardData.EnhanceLevel,
            cardData.TrainLevel, cardData.BreakthroughUsed, cardData.TrainDelta, cardData.IsLocked);
    }

    //현재 라인업 -> 저장 형태
    private static LineUpSaveData BuildLineUpSaveData()
    {
        Array positions = Enum.GetValues(typeof(HitterPosition));

        LineUpSaveData lineUpData = new LineUpSaveData
        {
            HitterPositions = new int[positions.Length],
            HitterInstanceIds = new int[positions.Length],
            HitterBattingOrders = new int[positions.Length],

            BenchInstanceIds = LineUpManager.Instance.GetBenchInstanceIds(),
            StartingPitcherIds = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.SP),
            RelieverPitcherIds = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.RP),
            CloserPitcherIds = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.CP)
        };

        int index = 0;

        foreach (HitterPosition position in positions)
        {
            (int instanceId, int battingOrder) = LineUpManager.Instance.GetHitterSlot(position);

            lineUpData.HitterPositions[index] = (int)position;
            lineUpData.HitterInstanceIds[index] = instanceId;
            lineUpData.HitterBattingOrders[index] = battingOrder;
            index++;
        }

        return lineUpData;
    }

    //저장 형태 -> 라인업.
    //검증을 우회하는 대신 평소와 같은 Assign* 경로로 되살린다.
    //CSV에서 카드 포지션이 바뀌는 등으로 더 이상 유효하지 않은 편성은 여기서 걸러져 빈 슬롯이 된다
    private static void RestoreLineUp(LineUpSaveData lineUpData)
    {
        if (lineUpData == null)
        {
            Debug.LogWarning("[PlayerSaveService]: 세이브에 라인업 정보가 없습니다");
            return;
        }

        LineUpManager.Instance.ClearAll();

        if (lineUpData.HitterPositions != null && lineUpData.HitterInstanceIds != null
            && lineUpData.HitterBattingOrders != null
            && lineUpData.HitterPositions.Length == lineUpData.HitterInstanceIds.Length
            && lineUpData.HitterPositions.Length == lineUpData.HitterBattingOrders.Length)
        {
            for (int i = 0; i < lineUpData.HitterPositions.Length; i++)
            {
                if (lineUpData.HitterInstanceIds[i] == LineUpManager.EmptySlot)
                    continue;

                LineUpManager.Instance.AssignHitter((HitterPosition)lineUpData.HitterPositions[i],
                    lineUpData.HitterInstanceIds[i], lineUpData.HitterBattingOrders[i]);
            }
        }
        else
        {
            Debug.LogError("[PlayerSaveService]: 세이브의 야수 라인업 배열 길이가 어긋납니다");
        }

        RestoreBench(lineUpData.BenchInstanceIds);

        RestorePitchers(PitcherPosition.SP, lineUpData.StartingPitcherIds);
        RestorePitchers(PitcherPosition.RP, lineUpData.RelieverPitcherIds);
        RestorePitchers(PitcherPosition.CP, lineUpData.CloserPitcherIds);
    }

    //벤치 5칸 복원 (빈 칸 -1 유지 - 인덱스가 곧 UI 슬롯 번호이므로 압축하면 안 됨)
    private static void RestoreBench(int[] benchInstanceIds)
    {
        if (benchInstanceIds == null)
            return;

        for (int i = 0; i < benchInstanceIds.Length; i++)
        {
            if (benchInstanceIds[i] == LineUpManager.EmptySlot)
                continue;

            LineUpManager.Instance.AssignBench(i, benchInstanceIds[i]);
        }
    }

    //역할별 투수 슬롯 복원
    private static void RestorePitchers(PitcherPosition position, int[] instanceIds)
    {
        if (instanceIds == null)
            return;

        for (int i = 0; i < instanceIds.Length; i++)
        {
            if (instanceIds[i] == LineUpManager.EmptySlot)
                continue;

            LineUpManager.Instance.AssignPitcher(position, i, instanceIds[i]);
        }
    }
}
