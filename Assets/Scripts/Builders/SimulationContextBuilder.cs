using System;
using UnityEngine;

/// <summary>
/// 게임 데이터를 시뮬레이션 입력(Snapshot)으로 변환
/// </summary>
public static class SimulationContextBuilder
{
    //플레이어 팀 + AI 상대 팀 기준 SimulationContext 생성
    public static SimulationContext Build(AiTeamRoster opponent, bool isPlayerHome, int playerRotationIndex, int opponentRotationIndex)
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

        //라인업이 비면 스냅샷 배열 길이가 9·7과 어긋나 SimulationContext 생성자가 예외를 던진다.
        //여기서 null로 걸러 호출자(LeagueRunner)가 하루치를 통째로 중단하도록 한다
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

        //AI 팀은 벤치가 없음 (기획서 7.8 / 세션 26)
        HitterSnapshot[] opponentBench = Array.Empty<HitterSnapshot>();

        HitterSnapshot[] homeLineup = isPlayerHome ? lineup : opponentLineup;
        HitterSnapshot[] awayLineup = isPlayerHome ? opponentLineup : lineup;

        HitterSnapshot[] homeBench = isPlayerHome ? bench : opponentBench;
        HitterSnapshot[] awayBench = isPlayerHome ? opponentBench : bench;

        PitcherSnapshot[] homePitchers = isPlayerHome ? pitchers : opponentPitchers;
        PitcherSnapshot[] awayPitchers = isPlayerHome ? opponentPitchers : pitchers;

        return new SimulationContext(isPlayerHome, homeLineup, awayLineup, homeBench, awayBench, homePitchers, awayPitchers);
    }

    //AI 두 팀끼리의 SimulationContext 생성 (내가 뛰지 않는 리그 경기)
    public static SimulationContext BuildAiVersusAi(AiTeamRoster homeTeam, AiTeamRoster awayTeam, int homeRotationIndex, int awayRotationIndex)
    {
        if (homeTeam == null || awayTeam == null)
        {
            Debug.LogError("[SimulationContextBuilder]: AI 경기의 팀 로스터가 없습니다");
            return null;
        }

        //AI끼리의 경기라 IsPlayerHome은 의미가 없음. 시뮬 코어는 이 값을 읽지 않고 UI 표기용
        return new SimulationContext(
            false,
            CopyLineup(homeTeam.Lineup),
            CopyLineup(awayTeam.Lineup),
            Array.Empty<HitterSnapshot>(),
            Array.Empty<HitterSnapshot>(),
            homeTeam.GetPitcherStaff(homeRotationIndex),
            awayTeam.GetPitcherStaff(awayRotationIndex));
    }

    //최종 반영 타자 스탯
    public static HitterSnapshot BuildHitterSnapshot(int instanceId)
    {
        CardInstance card = InventoryManager.Instance.GetCard(instanceId);
        if (card == null)
        {
            Debug.LogError("[SimulationContextBuilder]: 타자 카드가 존재하지 않습니다");
            return default;
        }

        CardMasterData cardData = CardDataManager.Instance.GetCardMasterData(card.CardId);
        if (cardData is not HitterMasterData hitterData)
        {
            Debug.LogError("[SimulationContextBuilder]: 타자가 아니거나 마스터 데이터 조회에 실패했습니다");
            return default;
        }

        int finalPower = CardStatsCalculator.CalculateFinalStat(hitterData.Power, card.EnhanceLevel, card.TrainDelta[0]);
        int finalContact = CardStatsCalculator.CalculateFinalStat(hitterData.Contact, card.EnhanceLevel, card.TrainDelta[1]);
        int finalRun = CardStatsCalculator.CalculateFinalStat(hitterData.Run, card.EnhanceLevel, card.TrainDelta[2]);
        int finalDefense = CardStatsCalculator.CalculateFinalStat(hitterData.Defense, card.EnhanceLevel, card.TrainDelta[3]);

        return new HitterSnapshot(instanceId, cardData.Name, finalPower, finalContact, finalRun, finalDefense);
    }

    //최종 반영 투수 스탯
    public static PitcherSnapshot BuildPitcherSnapshot(int instanceId)
    {
        CardInstance card = InventoryManager.Instance.GetCard(instanceId);
        if (card == null)
        {
            Debug.LogError("[SimulationContextBuilder]: 투수 카드가 존재하지 않습니다");
            return default;
        }

        CardMasterData cardData = CardDataManager.Instance.GetCardMasterData(card.CardId);
        if (cardData is not PitcherMasterData pitcherData)
        {
            Debug.LogError("[SimulationContextBuilder]: 투수가 아니거나 마스터 데이터 조회에 실패했습니다");
            return default;
        }

        int finalVelo = CardStatsCalculator.CalculateFinalStat(pitcherData.Velocity, card.EnhanceLevel, card.TrainDelta[0]);
        int finalStuff = CardStatsCalculator.CalculateFinalStat(pitcherData.Stuff, card.EnhanceLevel, card.TrainDelta[1]);
        int finalControl = CardStatsCalculator.CalculateFinalStat(pitcherData.Control, card.EnhanceLevel, card.TrainDelta[2]);
        int finalStamina = CardStatsCalculator.CalculateFinalStat(pitcherData.Stamina, card.EnhanceLevel, card.TrainDelta[3]);

        return new PitcherSnapshot(instanceId, cardData.Name, finalVelo, finalStuff, finalControl, finalStamina);
    }

    //AI 라인업 사본 생성 - 시뮬 코어가 대타 교체 시 라인업 배열에 직접 덮어쓰므로(세션 26) 고정 로스터 원본을 보호
    private static HitterSnapshot[] CopyLineup(HitterSnapshot[] source)
    {
        HitterSnapshot[] copy = new HitterSnapshot[source.Length];
        Array.Copy(source, copy, source.Length);

        return copy;
    }

    //인스턴스 ID -> 타자 스냅샷 배열
    private static HitterSnapshot[] BuildHitterSnapshots(int[] instanceIds)
    {
        HitterSnapshot[] result = new HitterSnapshot[instanceIds.Length];

        for (int i = 0; i < instanceIds.Length; i++)
        {
            //채워져 있지 않은 벤치는 넘기기
            if (instanceIds[i] == -1)
                continue;
            result[i] = BuildHitterSnapshot(instanceIds[i]);
        }
        return result;
    }

    //인스턴스 ID -> 투수 스냅샷 배열
    private static PitcherSnapshot[] BuildPitcherSnapshots(int[] instanceIds)
    {
        PitcherSnapshot[] result = new PitcherSnapshot[instanceIds.Length];

        for (int i = 0; i < instanceIds.Length; i++)
        {
            result[i] = BuildPitcherSnapshot(instanceIds[i]);
        }
        return result;
    }

    //투수진 7칸 조립
    private static PitcherSnapshot[] BuildPitcherStaff(int rotationIndex)
    {
        //각 투수에 대해 배열 생성
        int[] spInstanceIds = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.SP);
        int[] rpInstanceIds = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.RP);
        int[] cpInstanceIds = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.CP);

        int[] staffIds = new int[7];

        //투수 로테이션 등록
        staffIds[0] = spInstanceIds[rotationIndex % spInstanceIds.Length];

        Array.Copy(rpInstanceIds, 0, staffIds, 1, 5);

        staffIds[6] = cpInstanceIds[0];

        return BuildPitcherSnapshots(staffIds);
    }
}
