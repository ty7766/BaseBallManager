using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보유 카드로 라인업을 자동 편성한다. 기준은 역할별 편성 점수 내림차순이다
/// </summary>
public static class LineUpAutoFill
{
    //DH를 뺀 수비 8자리. DH는 포지션 제약이 없어 마지막에 따로 채운다
    //FindFirstUnused에서 포지션을 따지지 않는다는 표시
    private const int AnyPosition = -1;

    private static readonly HitterPosition[] DefensePositions =
    {
        HitterPosition.C, HitterPosition.FB, HitterPosition.SB, HitterPosition.TB,
        HitterPosition.SS, HitterPosition.LF, HitterPosition.CF, HitterPosition.RF
    };

    /// <summary>
    /// 라인업을 다시 편성한다. 야수 9칸과 투수 11칸을 모두 채웠으면 true
    /// </summary>
    public static bool Fill()
    {
        if (InventoryManager.Instance == null || CardDataManager.Instance == null || LineUpManager.Instance == null)
        {
            Debug.LogError("[LineUpAutoFill]: 필요한 매니저가 씬에 없습니다 (Inventory / CardData / LineUp)");
            return false;
        }

        LineUpManager.Instance.ClearAll();

        List<Candidate> hitters = new List<Candidate>();
        List<Candidate> pitchers = new List<Candidate>();

        CollectCandidates(hitters, pitchers);

        hitters.Sort(CompareByScoreDescending);
        pitchers.Sort(CompareByScoreDescending);

        HashSet<int> usedInstanceIds = new HashSet<int>();

        FillHitters(hitters, usedInstanceIds);
        FillPitchers(pitchers, usedInstanceIds);
        FillBench(hitters, usedInstanceIds);

        bool complete = LineUpManager.Instance.IsLineupComplete();

        if (!complete)
            Debug.LogWarning("[LineUpAutoFill]: 보유 카드가 부족해 라인업을 다 채우지 못했습니다");

        return complete;
    }

    //인벤토리를 훑어 타자·투수 후보를 만든다
    private static void CollectCandidates(List<Candidate> hitters, List<Candidate> pitchers)
    {
        foreach (CardInstance card in InventoryManager.Instance.GetAllCards())
        {
            CardMasterData masterData = CardDataManager.Instance.GetCardMasterData(card.CardId);

            if (masterData == null)
                continue;

            if (masterData is HitterMasterData hitterData)
            {
                if (HitterPositionParser.TryParse(masterData.Position, out HitterPosition hitterPosition))
                    hitters.Add(new Candidate(card.InstanceId, ScoreHitter(hitterData, card), (int)hitterPosition));
            }
            else if (masterData is PitcherMasterData pitcherData
                && PitcherPositionParser.TryParse(masterData.Position, out PitcherPosition pitcherPosition))
            {
                pitchers.Add(new Candidate(card.InstanceId, ScorePitcher(pitcherData, card, pitcherPosition), (int)pitcherPosition));
            }
        }
    }

    //수비 8자리 + DH를 고른 뒤 타격 점수 순으로 타순을 매긴다
    private static void FillHitters(List<Candidate> hitters, HashSet<int> usedInstanceIds)
    {
        List<(HitterPosition slot, Candidate candidate)> selected =
            new List<(HitterPosition slot, Candidate candidate)>(DefensePositions.Length + 1);

        foreach (HitterPosition position in DefensePositions)
        {
            int index = FindFirstUnused(hitters, usedInstanceIds, (int)position);

            if (index == -1)
            {
                Debug.LogWarning($"[LineUpAutoFill]: {position} 포지션 카드가 없습니다");
                continue;
            }

            usedInstanceIds.Add(hitters[index].InstanceId);
            selected.Add((position, hitters[index]));
        }

        int dhIndex = FindFirstUnused(hitters, usedInstanceIds, AnyPosition);

        if (dhIndex != -1)
        {
            usedInstanceIds.Add(hitters[dhIndex].InstanceId);
            selected.Add((HitterPosition.DH, hitters[dhIndex]));
        }

        selected.Sort((left, right) => right.candidate.Score.CompareTo(left.candidate.Score));

        for (int i = 0; i < selected.Count; i++)
            LineUpManager.Instance.AssignHitter(selected[i].slot, selected[i].candidate.InstanceId, i + 1);
    }

    //역할별 투수 슬롯을 앞에서부터 채운다 (SP 배열 순서 = 로테이션 순서)
    private static void FillPitchers(List<Candidate> pitchers, HashSet<int> usedInstanceIds)
    {
        FillPitcherRole(pitchers, usedInstanceIds, PitcherPosition.SP, LineUpManager.StartingPitcherCount);
        FillPitcherRole(pitchers, usedInstanceIds, PitcherPosition.RP, LineUpManager.RelieverCount);
        FillPitcherRole(pitchers, usedInstanceIds, PitcherPosition.CP, LineUpManager.CloserCount);
    }

    //한 보직의 슬롯을 점수 순으로 앞에서부터 채운다
    private static void FillPitcherRole(List<Candidate> pitchers, HashSet<int> usedInstanceIds,
        PitcherPosition position, int slotCount)
    {
        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            int index = FindFirstUnused(pitchers, usedInstanceIds, (int)position);

            if (index == -1)
            {
                Debug.LogWarning($"[LineUpAutoFill]: {position} 카드가 부족합니다 ({slotIndex}/{slotCount}칸 채움)");
                return;
            }

            usedInstanceIds.Add(pitchers[index].InstanceId);
            LineUpManager.Instance.AssignPitcher(position, slotIndex, pitchers[index].InstanceId);
        }
    }

    //남은 타자 중 상위 5명을 벤치에. 벤치는 선택이라 못 채워도 실패가 아니다
    private static void FillBench(List<Candidate> hitters, HashSet<int> usedInstanceIds)
    {
        for (int benchIndex = 0; benchIndex < LineUpManager.BenchSlotCount; benchIndex++)
        {
            int index = FindFirstUnused(hitters, usedInstanceIds, AnyPosition);

            if (index == -1)
                return;

            usedInstanceIds.Add(hitters[index].InstanceId);
            LineUpManager.Instance.AssignBench(benchIndex, hitters[index].InstanceId);
        }
    }

    //타자 편성 점수 - 타격(파워·정확)에 2배 가중. OVR로 고르면 타격이 나쁜 선수가 올라온다
    private static int ScoreHitter(HitterMasterData data, CardInstance card)
    {
        int power = CardStatsCalculator.CalculateFinalStat(data.Power, card.EnhanceLevel, card.TrainDelta[0]);
        int contact = CardStatsCalculator.CalculateFinalStat(data.Contact, card.EnhanceLevel, card.TrainDelta[1]);
        int run = CardStatsCalculator.CalculateFinalStat(data.Run, card.EnhanceLevel, card.TrainDelta[2]);
        int defense = CardStatsCalculator.CalculateFinalStat(data.Defense, card.EnhanceLevel, card.TrainDelta[3]);

        return (power + contact) * 2 + run + defense;
    }

    //투수 편성 점수 - 구위·제구 중심. 지구력은 선발에만 가중
    private static int ScorePitcher(PitcherMasterData data, CardInstance card, PitcherPosition position)
    {
        int velo = CardStatsCalculator.CalculateFinalStat(data.Velocity, card.EnhanceLevel, card.TrainDelta[0]);
        int stuff = CardStatsCalculator.CalculateFinalStat(data.Stuff, card.EnhanceLevel, card.TrainDelta[1]);
        int control = CardStatsCalculator.CalculateFinalStat(data.Control, card.EnhanceLevel, card.TrainDelta[2]);
        int stamina = CardStatsCalculator.CalculateFinalStat(data.Stamina, card.EnhanceLevel, card.TrainDelta[3]);

        int score = (stuff + control) * 2 + velo;

        if (position == PitcherPosition.SP)
            score += stamina;

        return score;
    }

    //아직 쓰지 않은 후보 중 첫 번째(= 최고 점수). AnyPosition이면 포지션을 따지지 않는다
    private static int FindFirstUnused(List<Candidate> candidates, HashSet<int> usedInstanceIds, int position)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (usedInstanceIds.Contains(candidates[i].InstanceId))
                continue;

            if (position != AnyPosition && candidates[i].Position != position)
                continue;

            return i;
        }

        return -1;
    }

    //편성 점수 내림차순
    private static int CompareByScoreDescending(Candidate left, Candidate right)
    {
        return right.Score.CompareTo(left.Score);
    }

    //편성 후보 1명. 포지션은 타자면 HitterPosition, 투수면 PitcherPosition을 int로 담는다
    private readonly struct Candidate
    {
        public int InstanceId { get; }
        public int Score { get; }
        public int Position { get; }

        public Candidate(int instanceId, int score, int position)
        {
            InstanceId = instanceId;
            Score = score;
            Position = position;
        }
    }
}
