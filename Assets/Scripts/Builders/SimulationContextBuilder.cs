using System;
using UnityEngine;

/// <summary>
/// 보유 카드·AI 로스터를 시뮬 입력(Snapshot)으로 변환한다
/// </summary>
public static class SimulationContextBuilder
{
    /// <summary>
    /// 플레이어 팀과 AI 상대 팀의 경기 컨텍스트. 라인업이 미완성이면 null
    /// </summary>
    public static SimulationContext Build(AiTeamRoster opponent, bool isPlayerHome,
        int playerRotationIndex, int opponentRotationIndex)
    {
        if (opponent == null)
        {
            Debug.LogError("[SimulationContextBuilder]: 상대 팀 로스터가 없습니다");
            return null;
        }

        if (LineUpManager.Instance == null)
        {
            Debug.LogError("[SimulationContextBuilder]: LineUpManager가 씬에 없습니다");
            return null;
        }

        if (!LineUpManager.Instance.IsLineupComplete())
        {
            Debug.LogError("[SimulationContextBuilder]: 플레이어 라인업이 완성되지 않았습니다 (야수 9 + 투수 11)");
            return null;
        }

        HitterSnapshot[] lineup = BuildHitterSnapshots(LineUpManager.Instance.GetHittersInBattingOrder());
        HitterSnapshot[] bench = BuildHitterSnapshots(LineUpManager.Instance.GetBenchInstanceIds());
        PitcherSnapshot[] pitchers = BuildPitcherStaff(playerRotationIndex);

        HitterSnapshot[] opponentLineup = CopyLineup(opponent.Lineup);
        PitcherSnapshot[] opponentPitchers = opponent.GetPitcherStaff(opponentRotationIndex);
        HitterSnapshot[] opponentBench = Array.Empty<HitterSnapshot>();

        return new SimulationContext(isPlayerHome,
            isPlayerHome ? lineup : opponentLineup,
            isPlayerHome ? opponentLineup : lineup,
            isPlayerHome ? bench : opponentBench,
            isPlayerHome ? opponentBench : bench,
            isPlayerHome ? pitchers : opponentPitchers,
            isPlayerHome ? opponentPitchers : pitchers);
    }

    /// <summary>
    /// AI 두 팀끼리의 경기 컨텍스트. IsPlayerHome은 UI 표기용이라 의미가 없다
    /// </summary>
    public static SimulationContext BuildAiVersusAi(AiTeamRoster homeTeam, AiTeamRoster awayTeam,
        int homeRotationIndex, int awayRotationIndex)
    {
        if (homeTeam == null || awayTeam == null)
        {
            Debug.LogError("[SimulationContextBuilder]: AI 경기의 팀 로스터가 없습니다");
            return null;
        }

        return new SimulationContext(
            false,
            CopyLineup(homeTeam.Lineup),
            CopyLineup(awayTeam.Lineup),
            Array.Empty<HitterSnapshot>(),
            Array.Empty<HitterSnapshot>(),
            homeTeam.GetPitcherStaff(homeRotationIndex),
            awayTeam.GetPitcherStaff(awayRotationIndex));
    }

    /// <summary>
    /// 강화·훈련이 반영된 타자 스냅샷. 조회 실패 시 기본값
    /// </summary>
    public static HitterSnapshot BuildHitterSnapshot(int instanceId)
    {
        CardInstance card = InventoryManager.Instance.GetCard(instanceId);

        if (card == null)
        {
            Debug.LogError($"[SimulationContextBuilder]: 타자 카드가 존재하지 않습니다 (instanceId {instanceId})");
            return default;
        }

        CardMasterData cardData = CardDataManager.Instance.GetCardMasterData(card.CardId);

        if (cardData is not HitterMasterData hitterData)
        {
            Debug.LogError($"[SimulationContextBuilder]: 타자가 아니거나 마스터 조회에 실패했습니다 (instanceId {instanceId})");
            return default;
        }

        return new HitterSnapshot(instanceId, cardData.Name,
            CardStatsCalculator.CalculateFinalStat(hitterData.Power, card.EnhanceLevel, card.TrainDelta[0]),
            CardStatsCalculator.CalculateFinalStat(hitterData.Contact, card.EnhanceLevel, card.TrainDelta[1]),
            CardStatsCalculator.CalculateFinalStat(hitterData.Run, card.EnhanceLevel, card.TrainDelta[2]),
            CardStatsCalculator.CalculateFinalStat(hitterData.Defense, card.EnhanceLevel, card.TrainDelta[3]));
    }

    /// <summary>
    /// 강화·훈련이 반영된 투수 스냅샷. 조회 실패 시 기본값
    /// </summary>
    public static PitcherSnapshot BuildPitcherSnapshot(int instanceId)
    {
        CardInstance card = InventoryManager.Instance.GetCard(instanceId);

        if (card == null)
        {
            Debug.LogError($"[SimulationContextBuilder]: 투수 카드가 존재하지 않습니다 (instanceId {instanceId})");
            return default;
        }

        CardMasterData cardData = CardDataManager.Instance.GetCardMasterData(card.CardId);

        if (cardData is not PitcherMasterData pitcherData)
        {
            Debug.LogError($"[SimulationContextBuilder]: 투수가 아니거나 마스터 조회에 실패했습니다 (instanceId {instanceId})");
            return default;
        }

        return new PitcherSnapshot(instanceId, cardData.Name,
            CardStatsCalculator.CalculateFinalStat(pitcherData.Velocity, card.EnhanceLevel, card.TrainDelta[0]),
            CardStatsCalculator.CalculateFinalStat(pitcherData.Stuff, card.EnhanceLevel, card.TrainDelta[1]),
            CardStatsCalculator.CalculateFinalStat(pitcherData.Control, card.EnhanceLevel, card.TrainDelta[2]),
            CardStatsCalculator.CalculateFinalStat(pitcherData.Stamina, card.EnhanceLevel, card.TrainDelta[3]));
    }

    //AI 고정 로스터 원본 보호. 시뮬이 대타 교체로 라인업 배열에 직접 덮어쓴다
    private static HitterSnapshot[] CopyLineup(HitterSnapshot[] source)
    {
        HitterSnapshot[] copy = new HitterSnapshot[source.Length];
        Array.Copy(source, copy, source.Length);

        return copy;
    }

    //빈 슬롯(벤치)은 기본값으로 남겨 둔다. 시뮬이 InstanceId 0으로 걸러낸다
    private static HitterSnapshot[] BuildHitterSnapshots(int[] instanceIds)
    {
        HitterSnapshot[] result = new HitterSnapshot[instanceIds.Length];

        for (int i = 0; i < instanceIds.Length; i++)
        {
            if (instanceIds[i] == LineUpManager.EmptySlot)
                continue;

            result[i] = BuildHitterSnapshot(instanceIds[i]);
        }

        return result;
    }

    //인스턴스 ID 배열 -> 투수 스냅샷 배열
    private static PitcherSnapshot[] BuildPitcherSnapshots(int[] instanceIds)
    {
        PitcherSnapshot[] result = new PitcherSnapshot[instanceIds.Length];

        for (int i = 0; i < instanceIds.Length; i++)
            result[i] = BuildPitcherSnapshot(instanceIds[i]);

        return result;
    }

    //선발 로테이션 1명 + 불펜 전원 + 마무리로 투수진 7칸을 조립한다
    private static PitcherSnapshot[] BuildPitcherStaff(int rotationIndex)
    {
        int[] spInstanceIds = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.SP);
        int[] rpInstanceIds = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.RP);
        int[] cpInstanceIds = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.CP);

        int[] staffIds = new int[SimulationContext.PitcherSlotCount];

        staffIds[SimulationContext.StartingPitcherSlot] = spInstanceIds[rotationIndex % spInstanceIds.Length];

        Array.Copy(rpInstanceIds, 0, staffIds, SimulationContext.StartingPitcherSlot + 1, rpInstanceIds.Length);

        staffIds[SimulationContext.CloserSlot] = cpInstanceIds[0];

        return BuildPitcherSnapshots(staffIds);
    }
}
