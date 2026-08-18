using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 진행 데이터 저장·복원 (기획서 10장)
/// </summary>
/// <remarks>
/// 리그 진행도는 LeagueSaveService가 따로 담당한다.
/// 두 세이브를 나눈 이유는 수명이 다르기 때문이다 - 리그는 재도전 시 버려지지만(기획서 7.6)
/// 카드·재화·해금은 리그를 넘어 계속 유지된다.
/// </remarks>
public class PlayerSaveService
{
    public const string SaveKey = "player";

    private readonly ISaveStorage _storage;

    public PlayerSaveService(ISaveStorage storage)
    {
        _storage = storage;
    }

    //저장된 플레이어 데이터가 있는지 (이어하기 / 새 게임 분기 판단용)
    public bool HasSave()
    {
        return _storage.Exists(SaveKey);
    }

    //현재 진행 상황 저장
    public bool Save()
    {
        if (!AreManagersReady())
            return false;

        IReadOnlyList<CardInstance> cards = InventoryManager.Instance.GetAllCards();

        PlayerSaveData saveData = new PlayerSaveData
        {
            PlayerTeamName = PlayerDataManager.Instance.PlayerTeamName,
            HighestUnlockedTier = (int)PlayerDataManager.Instance.HighestUnlockedTier,
            TutorialCompleted = PlayerDataManager.Instance.TutorialCompleted,

            NextInstanceId = InventoryManager.Instance.NextInstanceId,
            MaxCapacity = InventoryManager.Instance.GetMaxCapacity(),

            NormalPityCount = GachaManager.Instance.NormalPityCount,
            SignaturePityCount = GachaManager.Instance.SignaturePityCount,

            Cards = new CardInstanceSaveData[cards.Count],
            LineUp = BuildLineUpSaveData()
        };

        WriteCurrencies(saveData);

        for (int i = 0; i < cards.Count; i++)
        {
            saveData.Cards[i] = ToSaveData(cards[i]);
        }

        return _storage.Save(SaveKey, JsonUtility.ToJson(saveData, true));
    }

    //저장된 진행 상황을 각 매니저에 되살린다
    public bool Load()
    {
        if (!AreManagersReady())
            return false;

        string json = _storage.Load(SaveKey);

        //세이브가 없는 것은 정상 상태(첫 실행)
        if (string.IsNullOrEmpty(json))
            return false;

        PlayerSaveData saveData = JsonUtility.FromJson<PlayerSaveData>(json);

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

        //① 팀·해금 ② 재화 ③ 보유 카드 ④ 천장 ⑤ 라인업 순서.
        //라인업 복원은 카드가 인벤토리에 있어야 검증을 통과하므로 반드시 ③ 뒤에 와야 한다
        PlayerDataManager.Instance.Restore(saveData.PlayerTeamName,
            (LeagueTier)saveData.HighestUnlockedTier, saveData.TutorialCompleted);

        RestoreCurrencies(saveData);

        List<CardInstance> cards = new List<CardInstance>(saveData.Cards.Length);

        foreach (CardInstanceSaveData cardData in saveData.Cards)
        {
            cards.Add(ToCardInstance(cardData));
        }

        InventoryManager.Instance.Restore(cards, saveData.NextInstanceId, saveData.MaxCapacity);
        GachaManager.Instance.RestorePityCounts(saveData.NormalPityCount, saveData.SignaturePityCount);

        RestoreLineUp(saveData.LineUp);

        return true;
    }

    //세이브 삭제 (처음부터 다시 시작)
    public bool Delete()
    {
        return _storage.Delete(SaveKey);
    }

    //필요한 매니저가 전부 씬에 있는지
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
    private static void RestoreCurrencies(PlayerSaveData saveData)
    {
        if (saveData.CurrencyTypes == null || saveData.CurrencyAmounts == null)
        {
            Debug.LogError("[PlayerSaveService]: 세이브에 재화 정보가 없습니다");
            return;
        }

        CurrencyType[] types = new CurrencyType[saveData.CurrencyTypes.Length];

        for (int i = 0; i < types.Length; i++)
        {
            types[i] = (CurrencyType)saveData.CurrencyTypes[i];
        }

        //길이 불일치는 CurrencyManager가 걸러낸다
        CurrencyManager.Instance.Restore(types, saveData.CurrencyAmounts);
    }

    //카드 -> 저장 형태
    private static CardInstanceSaveData ToSaveData(CardInstance card)
    {
        int[] trainDelta = new int[card.TrainDelta.Count];

        for (int i = 0; i < trainDelta.Length; i++)
        {
            trainDelta[i] = card.TrainDelta[i];
        }

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
                if (lineUpData.HitterInstanceIds[i] == -1)
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
            if (benchInstanceIds[i] == -1)
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
            if (instanceIds[i] == -1)
                continue;

            LineUpManager.Instance.AssignPitcher(position, i, instanceIds[i]);
        }
    }
}
