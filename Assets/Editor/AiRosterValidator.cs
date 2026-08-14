using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AI 로스터 SO 검증 - 포지션 커버리지 · 중복 인물 · cardId 존재
/// </summary>
public static class AiRosterValidator
{
    //메뉴 진입점 - 프로젝트의 AI 로스터 에셋 전체를 검사
    [MenuItem("Tools/BaseBallManager/AI 로스터 검증")]
    public static void Validate()
    {
        if (CardCatalog.Hitters.Count == 0 || CardCatalog.Pitchers.Count == 0)
        {
            Debug.LogError("[AiRosterValidator] 카드 카탈로그가 비어 있습니다. 카드 CSV를 먼저 확인하세요");
            return;
        }

        HashSet<int> hitterCardIds = BuildCardIdSet(CardCatalog.Hitters);
        HashSet<int> pitcherCardIds = BuildCardIdSet(CardCatalog.Pitchers);

        int errorCount = 0;

        string[] teamGuids = AssetDatabase.FindAssets($"t:{nameof(AiTeamRosterData)}");

        foreach (string guid in teamGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AiTeamRosterData team = AssetDatabase.LoadAssetAtPath<AiTeamRosterData>(assetPath);

            if (team == null)
                continue;

            errorCount += ValidateTeam(team, hitterCardIds, pitcherCardIds);
        }

        string[] setGuids = AssetDatabase.FindAssets($"t:{nameof(AiRosterSet)}");

        foreach (string guid in setGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AiRosterSet rosterSet = AssetDatabase.LoadAssetAtPath<AiRosterSet>(assetPath);

            if (rosterSet == null)
                continue;

            errorCount += ValidateSet(rosterSet);
        }

        if (errorCount > 0)
        {
            Debug.LogError($"[AiRosterValidator] 검증 실패 - 오류 {errorCount}건 (팀 {teamGuids.Length}개 / 세트 {setGuids.Length}개 검사)");
            return;
        }

        Debug.Log($"[AiRosterValidator] 검증 통과 - 팀 {teamGuids.Length}개 / 세트 {setGuids.Length}개");
    }

    //팀 1개 검사. 반환값은 오류 건수
    private static int ValidateTeam(AiTeamRosterData team, HashSet<int> hitterCardIds, HashSet<int> pitcherCardIds)
    {
        int errorCount = 0;

        //팀명이 비어 있으면 로그에서 팀을 구분할 수 없으므로 에셋 파일명으로 대체
        string teamLabel = string.IsNullOrEmpty(team.TeamName) ? team.name : team.TeamName;

        if (string.IsNullOrEmpty(team.TeamName))
        {
            Debug.LogError($"[AiRosterValidator] {teamLabel}: 팀 이름이 비어 있습니다", team);
            errorCount++;
        }

        //팀 전체(타자 9 + 투수 11)를 한 통에 놓고 중복을 본다
        HashSet<int> usedCardIds = new HashSet<int>();
        HashSet<string> usedPeople = new HashSet<string>();

        errorCount += ValidateLineup(team, teamLabel, hitterCardIds, usedCardIds, usedPeople);

        errorCount += ValidatePitchers(team.StartingPitcherCardIds, AiTeamRosterData.StartingPitcherCount,
            "선발", PitcherPosition.SP, teamLabel, team, pitcherCardIds, usedCardIds, usedPeople);

        errorCount += ValidatePitchers(team.RelieverCardIds, AiTeamRosterData.RelieverCount,
            "불펜", PitcherPosition.RP, teamLabel, team, pitcherCardIds, usedCardIds, usedPeople);

        errorCount += ValidatePitchers(new[] { team.CloserCardId }, 1,
            "마무리", PitcherPosition.CP, teamLabel, team, pitcherCardIds, usedCardIds, usedPeople);

        return errorCount;
    }

    //타선 9칸 검사 - 슬롯별 카드 + 포지션 커버리지
    private static int ValidateLineup(AiTeamRosterData team, string teamLabel, HashSet<int> hitterCardIds,
        HashSet<int> usedCardIds, HashSet<string> usedPeople)
    {
        int errorCount = 0;
        IReadOnlyList<AiHitterSlot> slots = team.Lineup;

        if (slots.Count != AiTeamRosterData.LineupSize)
        {
            Debug.LogError($"[AiRosterValidator] {teamLabel}: 타선이 {slots.Count}칸입니다 (필요: {AiTeamRosterData.LineupSize})", team);
            errorCount++;
        }

        //포지션별 등장 횟수 - HitterPosition은 0부터 1씩 증가하므로 enum 값을 인덱스로 사용
        int[] positionCounts = new int[System.Enum.GetValues(typeof(HitterPosition)).Length];

        for (int i = 0; i < slots.Count; i++)
        {
            AiHitterSlot slot = slots[i];
            string slotLabel = $"{i + 1}번 타순({slot.Position})";

            positionCounts[(int)slot.Position]++;

            errorCount += ValidateCard(slot.CardId, hitterCardIds, "타자", teamLabel, slotLabel, team,
                usedCardIds, usedPeople, out CardEntry entry);

            //카드를 못 찾았으면 포지션 비교가 불가능
            if (entry.CardId == 0)
                continue;

            //DH는 어느 포지션 카드든 올 수 있음 (세션 32·33 확정)
            if (slot.Position == HitterPosition.DH)
                continue;

            if (!HitterPositionParser.TryParse(entry.Position, out HitterPosition cardPosition) || cardPosition != slot.Position)
            {
                Debug.LogError($"[AiRosterValidator] {teamLabel} {slotLabel}: {entry.Name} 선수의 포지션은 '{entry.Position}'입니다", team);
                errorCount++;
            }
        }

        errorCount += ValidatePositionCoverage(positionCounts, teamLabel, team);

        return errorCount;
    }

    //수비 8자리 + DH가 각각 정확히 1번씩 나오는지
    private static int ValidatePositionCoverage(int[] positionCounts, string teamLabel, Object context)
    {
        int errorCount = 0;

        for (int i = 0; i < positionCounts.Length; i++)
        {
            if (positionCounts[i] == 1)
                continue;

            HitterPosition position = (HitterPosition)i;

            if (positionCounts[i] == 0)
                Debug.LogError($"[AiRosterValidator] {teamLabel}: {position} 자리가 비어 있습니다", context);
            else
                Debug.LogError($"[AiRosterValidator] {teamLabel}: {position} 자리가 {positionCounts[i]}번 나옵니다", context);

            errorCount++;
        }

        return errorCount;
    }

    //투수 그룹 1개 검사 (선발 5 / 불펜 5 / 마무리 1)
    private static int ValidatePitchers(IReadOnlyList<int> cardIds, int requiredCount, string groupLabel,
        PitcherPosition role, string teamLabel, Object context,
        HashSet<int> pitcherCardIds, HashSet<int> usedCardIds, HashSet<string> usedPeople)
    {
        int errorCount = 0;

        if (cardIds.Count != requiredCount)
        {
            Debug.LogError($"[AiRosterValidator] {teamLabel}: {groupLabel}이(가) {cardIds.Count}명입니다 (필요: {requiredCount})", context);
            errorCount++;
        }

        for (int i = 0; i < cardIds.Count; i++)
        {
            string slotLabel = $"{groupLabel} {i + 1}번";

            errorCount += ValidateCard(cardIds[i], pitcherCardIds, "투수", teamLabel, slotLabel, context,
                usedCardIds, usedPeople, out CardEntry entry);

            if (entry.CardId == 0)
                continue;

            if (!PitcherPositionParser.TryParse(entry.Position, out PitcherPosition cardRole) || cardRole != role)
            {
                Debug.LogError($"[AiRosterValidator] {teamLabel} {slotLabel}: {entry.Name} 선수의 역할은 '{entry.Position}'입니다 (필요: {role})", context);
                errorCount++;
            }
        }

        return errorCount;
    }

    //카드 1장 공통 검사 - 빈 슬롯 · 존재 · 종류 · cardId 중복 · 동일 인물 중복 · 소속팀
    private static int ValidateCard(int cardId, HashSet<int> pool, string poolLabel, string teamLabel, string slotLabel,
        Object context, HashSet<int> usedCardIds, HashSet<string> usedPeople, out CardEntry entry)
    {
        entry = default;

        //빈 슬롯 센티넬은 0 (cardId는 1부터 시작)
        if (cardId == 0)
        {
            Debug.LogError($"[AiRosterValidator] {teamLabel} {slotLabel}: 슬롯이 비어 있습니다", context);
            return 1;
        }

        if (!CardCatalog.TryGet(cardId, out entry))
        {
            Debug.LogError($"[AiRosterValidator] {teamLabel} {slotLabel}: cardId {cardId} 카드가 CSV에 없습니다", context);
            entry = default;
            return 1;
        }

        if (!pool.Contains(cardId))
        {
            Debug.LogError($"[AiRosterValidator] {teamLabel} {slotLabel}: cardId {cardId}({entry.Name})는 {poolLabel} 카드가 아닙니다", context);
            entry = default;
            return 1;
        }

        int errorCount = 0;

        //cardId가 시뮬의 InstanceId 자리에 들어가므로(세션 30) 한 팀 안에서 겹치면 주자 추적이 깨짐
        if (!usedCardIds.Add(cardId))
        {
            Debug.LogError($"[AiRosterValidator] {teamLabel} {slotLabel}: cardId {cardId}({entry.Name})가 이 팀에 두 번 들어 있습니다", context);
            errorCount++;
        }

        //동일 인물 판정 키는 (이름, 팀) - 강화용 동일성 판정과 다른 규칙 (세션 33)
        if (!usedPeople.Add($"{entry.Name}/{entry.TeamName}"))
        {
            Debug.LogError($"[AiRosterValidator] {teamLabel} {slotLabel}: {entry.Name}({entry.TeamName}) 선수가 이 팀에 두 번 나옵니다", context);
            errorCount++;
        }

        //다른 팀 선수 편성은 의도일 수 있으므로 경고만 (기획서 7.8 미러전 취지)
        if (entry.TeamName != teamLabel)
        {
            Debug.LogWarning($"[AiRosterValidator] {teamLabel} {slotLabel}: {entry.Name} 선수의 소속은 {entry.TeamName}입니다", context);
        }

        return errorCount;
    }

    //로스터 세트 검사 - 팀 수 · 빈 칸 · 중복 팀
    private static int ValidateSet(AiRosterSet rosterSet)
    {
        int errorCount = 0;
        IReadOnlyList<AiTeamRosterData> teams = rosterSet.Teams;

        if (teams.Count != AiRosterSet.TeamCount)
        {
            Debug.LogError($"[AiRosterValidator] 세트 '{rosterSet.name}': 팀이 {teams.Count}개입니다 (필요: {AiRosterSet.TeamCount})", rosterSet);
            errorCount++;
        }

        HashSet<string> usedTeamNames = new HashSet<string>();

        for (int i = 0; i < teams.Count; i++)
        {
            AiTeamRosterData team = teams[i];

            if (team == null)
            {
                Debug.LogError($"[AiRosterValidator] 세트 '{rosterSet.name}': {i + 1}번 칸이 비어 있습니다", rosterSet);
                errorCount++;
                continue;
            }

            if (!usedTeamNames.Add(team.TeamName))
            {
                Debug.LogError($"[AiRosterValidator] 세트 '{rosterSet.name}': '{team.TeamName}' 팀이 두 번 들어 있습니다", rosterSet);
                errorCount++;
            }
        }

        return errorCount;
    }

    //카탈로그 목록 -> cardId 집합 (타자/투수 구분 판정용)
    private static HashSet<int> BuildCardIdSet(IReadOnlyList<CardEntry> entries)
    {
        HashSet<int> cardIds = new HashSet<int>(entries.Count);

        foreach (CardEntry entry in entries)
        {
            cardIds.Add(entry.CardId);
        }

        return cardIds;
    }
}
