using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보유 카드로 라인업을 자동 편성한다 (기획서 6장)
/// </summary>
/// <remarks>
/// 편성 자체는 사용자가 UI에서 직접 하는 것이 기본이다(기획서 6.4 - 타순 직접 지정).
/// 이 클래스는 "자동 편성" 버튼의 구현부이자, UI가 없는 현재 라인업을 채울 수 있는 유일한 경로다.
/// 기준은 강화·훈련이 반영된 역할별 편성 점수 내림차순이며, 사용자가 이후 손으로 고치는 것을 전제한 근사치다.
/// OVR(4스탯 단순 평균)은 선발 기준으로 쓰지 않는다 - ScoreHitter · ScorePitcher 주석 참고.
/// </remarks>
public static class LineUpAutoFill
{
    //수비 8자리. DH는 어느 포지션 카드든 올 수 있어 마지막에 따로 채운다 (세션 32 확정)
    private static readonly HitterPosition[] DefensePositions =
    {
        HitterPosition.C, HitterPosition.FB, HitterPosition.SB, HitterPosition.TB,
        HitterPosition.SS, HitterPosition.LF, HitterPosition.CF, HitterPosition.RF
    };

    private const int BenchSlotCount = 5;

    /// <summary>
    /// 라인업을 다시 편성한다. 20칸을 모두 채웠으면 true
    /// </summary>
    public static bool Fill()
    {
        if (InventoryManager.Instance == null || CardDataManager.Instance == null || LineUpManager.Instance == null)
        {
            Debug.LogError("[LineUpAutoFill]: 필요한 매니저가 씬에 없습니다 (Inventory / CardData / LineUp)");
            return false;
        }

        //기존 편성이 남아 있으면 중복 배치 검사에 걸려 새 편성이 실패한다
        LineUpManager.Instance.ClearAll();

        List<Candidate> hitters = new List<Candidate>();
        List<Candidate> pitchers = new List<Candidate>();

        CollectCandidates(hitters, pitchers);

        //편성 점수 내림차순 - 이후 모든 선택이 "남은 것 중 첫 번째"로 끝난다
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
                //포지션 표기가 CSV 규칙을 벗어난 카드는 어느 슬롯에도 못 넣으므로 제외
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
        //(슬롯, 후보) 쌍을 모아둔다. 타순은 9명이 다 정해진 뒤에야 알 수 있음
        List<(HitterPosition slot, Candidate candidate)> selected =
            new List<(HitterPosition slot, Candidate candidate)>(DefensePositions.Length + 1);

        //제약이 강한 수비 자리부터 채운다. DH를 먼저 채우면 최상위 선수가 빠져 포수 자리가 빌 수 있음
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

        //DH는 남은 타자 중 최고 점수 (포지션 무관)
        int dhIndex = FindFirstUnused(hitters, usedInstanceIds, -1);

        if (dhIndex != -1)
        {
            usedInstanceIds.Add(hitters[dhIndex].InstanceId);
            selected.Add((HitterPosition.DH, hitters[dhIndex]));
        }

        //타순은 타격 점수 내림차순 (단순 근사 - 사용자가 UI에서 조정하는 것을 전제)
        selected.Sort((left, right) => right.candidate.Score.CompareTo(left.candidate.Score));

        for (int i = 0; i < selected.Count; i++)
        {
            //실패 사유는 LineUpManager가 로그로 남김
            LineUpManager.Instance.AssignHitter(selected[i].slot, selected[i].candidate.InstanceId, i + 1);
        }
    }

    //역할별 투수 슬롯을 앞에서부터 채운다 (SP 배열 순서 = 로테이션 순서)
    private static void FillPitchers(List<Candidate> pitchers, HashSet<int> usedInstanceIds)
    {
        FillPitcherRole(pitchers, usedInstanceIds, PitcherPosition.SP, 5);
        FillPitcherRole(pitchers, usedInstanceIds, PitcherPosition.RP, 5);
        FillPitcherRole(pitchers, usedInstanceIds, PitcherPosition.CP, 1);
    }

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

    //남은 타자 중 상위 5명을 벤치에 (기획서 6.3 - 벤치는 선택이라 못 채워도 실패가 아님)
    private static void FillBench(List<Candidate> hitters, HashSet<int> usedInstanceIds)
    {
        for (int benchIndex = 0; benchIndex < BenchSlotCount; benchIndex++)
        {
            int index = FindFirstUnused(hitters, usedInstanceIds, -1);

            if (index == -1)
                return;

            usedInstanceIds.Add(hitters[index].InstanceId);
            LineUpManager.Instance.AssignBench(benchIndex, hitters[index].InstanceId);
        }
    }

    /// <summary>
    /// 타자 편성 점수 - 타격(파워·정확)에 2배 가중
    /// </summary>
    /// <remarks>
    /// OVR(4스탯 단순 평균)로 고르면 안 된다. 실측 결과 OVR과 정확의 상관은 +0.33에 불과해,
    /// 주루·수비가 좋고 타격이 나쁜 선수가 상위로 올라온다.
    /// (예: 정확 48인 카드가 주루 84·수비 80 덕에 OVR 전체 4위)
    /// 실제로 OVR 기준으로 9명을 뽑으면 정확 평균이 타격 기준 대비 12점 낮았다.
    /// 주루·수비도 진루·실책에 쓰이므로 0으로 두지 않고 절반만 반영한다.
    /// </remarks>
    private static int ScoreHitter(HitterMasterData data, CardInstance card)
    {
        int power = CardStatsCalculator.CalculateFinalStat(data.Power, card.EnhanceLevel, card.TrainDelta[0]);
        int contact = CardStatsCalculator.CalculateFinalStat(data.Contact, card.EnhanceLevel, card.TrainDelta[1]);
        int run = CardStatsCalculator.CalculateFinalStat(data.Run, card.EnhanceLevel, card.TrainDelta[2]);
        int defense = CardStatsCalculator.CalculateFinalStat(data.Defense, card.EnhanceLevel, card.TrainDelta[3]);

        return (power + contact) * 2 + run + defense;
    }

    /// <summary>
    /// 투수 편성 점수 - 구위·제구 중심. 지구력은 선발에만 가중
    /// </summary>
    /// <remarks>
    /// 투수 OVR은 지구력과의 상관이 +0.92라 사실상 지구력 순위가 된다.
    /// 불펜은 지구력이 35~45로 설계돼 있어(짧게 던지는 보직) OVR이 구조적으로 낮고,
    /// 구위가 좋아도 밀린다. 그래서 결과에 직접 쓰이는 구위·제구·구속으로 고르고,
    /// 긴 이닝을 책임지는 선발만 지구력을 함께 본다.
    /// </remarks>
    private static int ScorePitcher(PitcherMasterData data, CardInstance card, PitcherPosition position)
    {
        int velo = CardStatsCalculator.CalculateFinalStat(data.Velocity, card.EnhanceLevel, card.TrainDelta[0]);
        int stuff = CardStatsCalculator.CalculateFinalStat(data.Stuff, card.EnhanceLevel, card.TrainDelta[1]);
        int control = CardStatsCalculator.CalculateFinalStat(data.Control, card.EnhanceLevel, card.TrainDelta[2]);
        int stamina = CardStatsCalculator.CalculateFinalStat(data.Stamina, card.EnhanceLevel, card.TrainDelta[3]);

        int score = (stuff + control) * 2 + velo;

        //선발은 오래 던져야 하므로 지구력이 실제 성능에 직결된다
        if (position == PitcherPosition.SP)
            score += stamina;

        return score;
    }

    //아직 쓰지 않은 후보 중 첫 번째(= 최고 점수). position이 -1이면 포지션을 따지지 않는다
    private static int FindFirstUnused(List<Candidate> candidates, HashSet<int> usedInstanceIds, int position)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (usedInstanceIds.Contains(candidates[i].InstanceId))
                continue;

            if (position != -1 && candidates[i].Position != position)
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

    /// <summary>
    /// 편성 후보 1명 (포지션은 타자면 HitterPosition, 투수면 PitcherPosition을 int로 담는다)
    /// </summary>
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
