using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 마스터 풀에서 AI 팀 로스터 편성
/// </summary>
public static class AiRosterBuilder
{
    //SO 로스터 + 티어 보정 -> 시뮬용 AI 팀 로스터 조립
    public static AiTeamRoster BuildTeam(AiTeamRosterData rosterData, int tierStatBonus)
    {
        if (rosterData == null)
        {
            Debug.LogError("[AiRosterBuilder]: 로스터 데이터가 비어 있습니다");
            return null;
        }

        HitterSnapshot[] lineup = BuildLineup(rosterData, tierStatBonus);

        if (lineup.Length != AiTeamRosterData.LineupSize)
            return null;

        PitcherSnapshot[] startingPitchers = BuildPitchers(rosterData.StartingPitcherCardIds, rosterData.TeamName, "선발", tierStatBonus);

        if (startingPitchers.Length != AiTeamRosterData.StartingPitcherCount)
            return null;

        PitcherSnapshot[] relievePitchers = BuildPitchers(rosterData.RelieverCardIds, rosterData.TeamName, "불펜", tierStatBonus);

        if (relievePitchers.Length != AiTeamRosterData.RelieverCount)
            return null;

        //빈 슬롯 센티넬은 0 (cardId는 1부터 시작)
        if (rosterData.CloserCardId == 0)
        {
            Debug.LogError($"[AiRosterBuilder]: {rosterData.TeamName} 마무리 슬롯이 비어 있습니다");
            return null;
        }

        PitcherMasterData closerData = CardDataManager.Instance.GetPitcher(rosterData.CloserCardId);

        if (closerData == null)
        {
            Debug.LogError($"[AiRosterBuilder]: {rosterData.TeamName} 마무리 카드(cardId {rosterData.CloserCardId})를 찾을 수 없습니다");
            return null;
        }

        PitcherSnapshot closer = ToPitcherSnapshot(closerData, tierStatBonus);

        return new AiTeamRoster(rosterData.TeamName, lineup, startingPitchers, relievePitchers, closer);
    }

    //타선 9칸 -> 타자 스냅샷 배열 (배열 순서 = 타순)
    private static HitterSnapshot[] BuildLineup(AiTeamRosterData rosterData, int tierStatBonus)
    {
        IReadOnlyList<AiHitterSlot> slots = rosterData.Lineup;

        if (slots.Count != AiTeamRosterData.LineupSize)
        {
            Debug.LogError($"[AiRosterBuilder]: {rosterData.TeamName} 타선이 {slots.Count}칸입니다 (필요: {AiTeamRosterData.LineupSize})");
            return Array.Empty<HitterSnapshot>();
        }

        HitterSnapshot[] lineup = new HitterSnapshot[AiTeamRosterData.LineupSize];

        for (int i = 0; i < slots.Count; i++)
        {
            AiHitterSlot slot = slots[i];

            if (slot.CardId == 0)
            {
                Debug.LogError($"[AiRosterBuilder]: {rosterData.TeamName} {i + 1}번 타순({slot.Position}) 슬롯이 비어 있습니다");
                return Array.Empty<HitterSnapshot>();
            }

            HitterMasterData hitterData = CardDataManager.Instance.GetHitter(slot.CardId);

            if (hitterData == null)
            {
                Debug.LogError($"[AiRosterBuilder]: {rosterData.TeamName} {i + 1}번 타순({slot.Position}) 카드(cardId {slot.CardId})를 찾을 수 없습니다");
                return Array.Empty<HitterSnapshot>();
            }

            lineup[i] = ToHitterSnapshot(hitterData, tierStatBonus);
        }

        return lineup;
    }

    //cardId 배열 -> 투수 스냅샷 배열 (선발·불펜 공용)
    private static PitcherSnapshot[] BuildPitchers(IReadOnlyList<int> cardIds, string teamName, string roleLabel, int tierStatBonus)
    {
        PitcherSnapshot[] pitchers = new PitcherSnapshot[cardIds.Count];

        for (int i = 0; i < cardIds.Count; i++)
        {
            int cardId = cardIds[i];

            if (cardId == 0)
            {
                Debug.LogError($"[AiRosterBuilder]: {teamName} {roleLabel} {i + 1}번 슬롯이 비어 있습니다");
                return Array.Empty<PitcherSnapshot>();
            }

            PitcherMasterData pitcherData = CardDataManager.Instance.GetPitcher(cardId);

            if (pitcherData == null)
            {
                Debug.LogError($"[AiRosterBuilder]: {teamName} {roleLabel} {i + 1}번 카드(cardId {cardId})를 찾을 수 없습니다");
                return Array.Empty<PitcherSnapshot>();
            }

            pitchers[i] = ToPitcherSnapshot(pitcherData, tierStatBonus);
        }

        return pitchers;
    }

    //마스터 데이터 + 티어 보정 = AI 타자 스냅샷
    private static HitterSnapshot ToHitterSnapshot(HitterMasterData hitterData, int tierStatBonus)
    {
        return new HitterSnapshot(hitterData.CardId, hitterData.Name, hitterData.Power + tierStatBonus, hitterData.Contact + tierStatBonus, hitterData.Run + tierStatBonus, hitterData.Defense + tierStatBonus);
    }

    //마스터 데이터 + 티어 보정 = AI 투수 스냅샷
    private static PitcherSnapshot ToPitcherSnapshot(PitcherMasterData pitcherData, int tierStatBonus)
    {
        return new PitcherSnapshot(pitcherData.CardId, pitcherData.Name, pitcherData.Velocity + tierStatBonus, pitcherData.Stuff + tierStatBonus, pitcherData.Control + tierStatBonus, pitcherData.Stamina + tierStatBonus);
    }
}
