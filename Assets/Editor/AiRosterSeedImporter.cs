using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 씨앗 CSV -> AI 로스터 SO 에셋 일괄 생성
/// </summary>
public static class AiRosterSeedImporter
{
    private const string HitterSeedPath = "Assets/Editor/RosterSeed/AiHitterRosters.csv";
    private const string PitcherSeedPath = "Assets/Editor/RosterSeed/AiPitcherRosters.csv";
    private const string OutputRoot = "Assets/Data/League";

    //읽어야 하는 열 (name/ovr은 사람이 알아보기 위한 열이라 읽지 않음)
    private static readonly string[] HitterSeedColumns = { "rosterSet", "team", "position", "battingOrder", "cardId" };
    private static readonly string[] PitcherSeedColumns = { "rosterSet", "team", "role", "order", "cardId" };

    //SerializedProperty 조회용 필드명 - 대상 클래스의 필드명을 바꾸면 여기도 고쳐야 함
    private const string FieldTeamName = "_teamName";
    private const string FieldLineup = "_lineup";
    private const string FieldStartingPitchers = "_startingPitcherCardIds";
    private const string FieldRelievers = "_relieverCardIds";
    private const string FieldCloser = "_closerCardId";
    private const string FieldSlotPosition = "_position";
    private const string FieldSlotCardId = "_cardId";
    private const string FieldTeams = "_teams";

    //메뉴 진입점 - 씨앗 CSV 전체를 SO 에셋으로 변환
    [MenuItem("Tools/BaseBallManager/AI 로스터 씨앗 임포트")]
    public static void Import()
    {
        if (!TryReadSeed(HitterSeedPath, HitterSeedColumns, out Dictionary<string, int> hitterHeaders, out List<string[]> hitterRows))
            return;

        if (!TryReadSeed(PitcherSeedPath, PitcherSeedColumns, out Dictionary<string, int> pitcherHeaders, out List<string[]> pitcherRows))
            return;

        //팀 초안 수집 - 딕셔너리는 조회용, 리스트는 순서 보존용
        Dictionary<string, TeamDraft> draftLookup = new Dictionary<string, TeamDraft>();
        List<TeamDraft> drafts = new List<TeamDraft>();

        int errorCount = FillHitters(hitterRows, hitterHeaders, draftLookup, drafts)
                       + FillPitchers(pitcherRows, pitcherHeaders, draftLookup, drafts);

        //에셋을 하나도 만들기 전에 중단 - 반쯤 채워진 에셋이 남는 것을 막음
        if (errorCount > 0)
        {
            Debug.LogError($"[AiRosterSeedImporter] 씨앗 CSV 오류 {errorCount}건. 에셋을 만들지 않고 중단했습니다");
            return;
        }

        EnsureFolder(OutputRoot);

        //세트별로 생성한 팀 에셋을 모아둠
        Dictionary<string, List<AiTeamRosterData>> teamsBySet = new Dictionary<string, List<AiTeamRosterData>>();
        List<string> setNames = new List<string>();

        foreach (TeamDraft draft in drafts)
        {
            string setFolder = $"{OutputRoot}/{draft.RosterSet}";
            EnsureFolder(setFolder);

            AiTeamRosterData teamAsset = WriteTeamAsset(draft, setFolder);

            //필드명 불일치 - WriteTeamAsset에서 이미 로그를 남겼음
            if (teamAsset == null)
                return;

            if (!teamsBySet.TryGetValue(draft.RosterSet, out List<AiTeamRosterData> teams))
            {
                teams = new List<AiTeamRosterData>(AiRosterSet.TeamCount);
                teamsBySet[draft.RosterSet] = teams;
                setNames.Add(draft.RosterSet);
            }

            teams.Add(teamAsset);
        }

        foreach (string setName in setNames)
        {
            if (!WriteRosterSetAsset(setName, teamsBySet[setName], $"{OutputRoot}/{setName}"))
                return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AiRosterSeedImporter] 임포트 완료 - 세트 {setNames.Count}개 / 팀 {drafts.Count}개");
    }

    //씨앗 CSV 1개를 읽어 헤더와 데이터 행으로 분해
    private static bool TryReadSeed(string assetPath, string[] requiredColumns,
        out Dictionary<string, int> headers, out List<string[]> rows)
    {
        headers = null;
        rows = null;

        //씨앗 CSV는 Resources 밖에 있으므로 AssetDatabase로 읽음
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

        if (asset == null)
        {
            Debug.LogError($"[AiRosterSeedImporter] 씨앗 CSV를 찾을 수 없습니다: {assetPath}");
            return false;
        }

        string[] lines = asset.text.Split('\n');

        if (lines.Length < 2)
        {
            Debug.LogError($"[AiRosterSeedImporter] 씨앗 CSV에 데이터 행이 없습니다: {assetPath}");
            return false;
        }

        headers = ParseHeaders(lines[0].Trim());

        //필요한 열이 하나라도 없으면 행 파싱을 시작하지 않음
        foreach (string column in requiredColumns)
        {
            if (!headers.ContainsKey(column))
            {
                Debug.LogError($"[AiRosterSeedImporter] '{column}' 열이 없습니다: {assetPath}");
                headers = null;
                return false;
            }
        }

        rows = new List<string[]>(lines.Length - 1);

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            rows.Add(line.Split(','));
        }

        return true;
    }

    //CSV 첫째 줄 -> 열 이름 : 인덱스 딕셔너리
    private static Dictionary<string, int> ParseHeaders(string headerLine)
    {
        string[] columns = headerLine.Split(',');
        Dictionary<string, int> headers = new Dictionary<string, int>(columns.Length);

        for (int i = 0; i < columns.Length; i++)
        {
            headers[columns[i].Trim()] = i;
        }

        return headers;
    }

    //타자 행 -> 팀 초안의 타선 채우기. 반환값은 오류 건수
    private static int FillHitters(List<string[]> rows, Dictionary<string, int> headers,
        Dictionary<string, TeamDraft> draftLookup, List<TeamDraft> drafts)
    {
        int errorCount = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            string[] cols = rows[i];

            //헤더 1줄 + 0부터 시작하는 인덱스 -> 실제 CSV 줄 번호
            int lineNumber = i + 2;

            if (cols.Length < headers.Count)
            {
                Debug.LogError($"[AiRosterSeedImporter] {HitterSeedPath} {lineNumber}번 줄: 열 개수가 부족합니다");
                errorCount++;
                continue;
            }

            string rosterSet = cols[headers["rosterSet"]].Trim();
            string teamName = cols[headers["team"]].Trim();

            if (!HitterPositionParser.TryParse(cols[headers["position"]].Trim(), out HitterPosition position))
            {
                Debug.LogError($"[AiRosterSeedImporter] {HitterSeedPath} {lineNumber}번 줄: 알 수 없는 포지션 '{cols[headers["position"]]}'");
                errorCount++;
                continue;
            }

            if (!int.TryParse(cols[headers["battingOrder"]].Trim(), out int battingOrder)
                || battingOrder < 1 || battingOrder > AiTeamRosterData.LineupSize)
            {
                Debug.LogError($"[AiRosterSeedImporter] {HitterSeedPath} {lineNumber}번 줄: 타순이 1~{AiTeamRosterData.LineupSize} 범위를 벗어났습니다");
                errorCount++;
                continue;
            }

            if (!int.TryParse(cols[headers["cardId"]].Trim(), out int cardId))
            {
                Debug.LogError($"[AiRosterSeedImporter] {HitterSeedPath} {lineNumber}번 줄: cardId가 숫자가 아닙니다");
                errorCount++;
                continue;
            }

            TeamDraft draft = GetOrCreateDraft(rosterSet, teamName, draftLookup, drafts);
            int slotIndex = battingOrder - 1;

            //빈 슬롯 센티넬은 0 (cardId는 1부터 시작)
            if (draft.HitterCardIds[slotIndex] != 0)
            {
                Debug.LogError($"[AiRosterSeedImporter] {HitterSeedPath} {lineNumber}번 줄: {teamName} {battingOrder}번 타순이 중복입니다");
                errorCount++;
                continue;
            }

            draft.Positions[slotIndex] = position;
            draft.HitterCardIds[slotIndex] = cardId;
        }

        return errorCount;
    }

    //투수 행 -> 팀 초안의 투수진 채우기. 반환값은 오류 건수
    private static int FillPitchers(List<string[]> rows, Dictionary<string, int> headers,
        Dictionary<string, TeamDraft> draftLookup, List<TeamDraft> drafts)
    {
        int errorCount = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            string[] cols = rows[i];
            int lineNumber = i + 2;

            if (cols.Length < headers.Count)
            {
                Debug.LogError($"[AiRosterSeedImporter] {PitcherSeedPath} {lineNumber}번 줄: 열 개수가 부족합니다");
                errorCount++;
                continue;
            }

            string rosterSet = cols[headers["rosterSet"]].Trim();
            string teamName = cols[headers["team"]].Trim();

            if (!PitcherPositionParser.TryParse(cols[headers["role"]].Trim(), out PitcherPosition role))
            {
                Debug.LogError($"[AiRosterSeedImporter] {PitcherSeedPath} {lineNumber}번 줄: 알 수 없는 역할 '{cols[headers["role"]]}'");
                errorCount++;
                continue;
            }

            if (!int.TryParse(cols[headers["order"]].Trim(), out int order))
            {
                Debug.LogError($"[AiRosterSeedImporter] {PitcherSeedPath} {lineNumber}번 줄: order가 숫자가 아닙니다");
                errorCount++;
                continue;
            }

            if (!int.TryParse(cols[headers["cardId"]].Trim(), out int cardId))
            {
                Debug.LogError($"[AiRosterSeedImporter] {PitcherSeedPath} {lineNumber}번 줄: cardId가 숫자가 아닙니다");
                errorCount++;
                continue;
            }

            TeamDraft draft = GetOrCreateDraft(rosterSet, teamName, draftLookup, drafts);

            switch (role)
            {
                case PitcherPosition.SP:
                    if (!TrySetPitcher(draft.StartingPitcherCardIds, order, cardId,
                        $"{teamName} 선발 {order}번", PitcherSeedPath, lineNumber))
                    {
                        errorCount++;
                    }
                    break;

                case PitcherPosition.RP:
                    if (!TrySetPitcher(draft.RelieverCardIds, order, cardId,
                        $"{teamName} 불펜 {order}번", PitcherSeedPath, lineNumber))
                    {
                        errorCount++;
                    }
                    break;

                case PitcherPosition.CP:
                    if (draft.CloserCardId != 0)
                    {
                        Debug.LogError($"[AiRosterSeedImporter] {PitcherSeedPath} {lineNumber}번 줄: {teamName} 마무리가 중복입니다");
                        errorCount++;
                        break;
                    }
                    draft.CloserCardId = cardId;
                    break;
            }
        }

        return errorCount;
    }

    //투수 배열의 order번 칸에 cardId 배치 (범위·중복 검사 포함)
    private static bool TrySetPitcher(int[] slots, int order, int cardId, string slotLabel, string seedPath, int lineNumber)
    {
        if (order < 1 || order > slots.Length)
        {
            Debug.LogError($"[AiRosterSeedImporter] {seedPath} {lineNumber}번 줄: order가 1~{slots.Length} 범위를 벗어났습니다");
            return false;
        }

        if (slots[order - 1] != 0)
        {
            Debug.LogError($"[AiRosterSeedImporter] {seedPath} {lineNumber}번 줄: {slotLabel}이(가) 중복입니다");
            return false;
        }

        slots[order - 1] = cardId;
        return true;
    }

    //세트+팀 조합의 초안을 가져오거나 새로 만듦
    private static TeamDraft GetOrCreateDraft(string rosterSet, string teamName,
        Dictionary<string, TeamDraft> draftLookup, List<TeamDraft> drafts)
    {
        string key = $"{rosterSet}/{teamName}";

        if (draftLookup.TryGetValue(key, out TeamDraft draft))
            return draft;

        draft = new TeamDraft(rosterSet, teamName);
        draftLookup[key] = draft;
        drafts.Add(draft);

        return draft;
    }

    //팀 초안 -> AiTeamRosterData 에셋 생성 또는 갱신
    private static AiTeamRosterData WriteTeamAsset(TeamDraft draft, string folderPath)
    {
        string assetPath = $"{folderPath}/AiTeamRoster_{draft.TeamName}.asset";
        AiTeamRosterData asset = AssetDatabase.LoadAssetAtPath<AiTeamRosterData>(assetPath);

        //없으면 새로 만들고, 있으면 기존 에셋을 덮어씀 (참조를 끊지 않기 위해 지우지 않음)
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<AiTeamRosterData>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        SerializedObject serializedAsset = new SerializedObject(asset);

        SerializedProperty teamNameProperty = serializedAsset.FindProperty(FieldTeamName);
        SerializedProperty lineupProperty = serializedAsset.FindProperty(FieldLineup);
        SerializedProperty startersProperty = serializedAsset.FindProperty(FieldStartingPitchers);
        SerializedProperty relieversProperty = serializedAsset.FindProperty(FieldRelievers);
        SerializedProperty closerProperty = serializedAsset.FindProperty(FieldCloser);

        if (teamNameProperty == null || lineupProperty == null || startersProperty == null
            || relieversProperty == null || closerProperty == null)
        {
            Debug.LogError("[AiRosterSeedImporter] AiTeamRosterData의 필드명이 임포터와 맞지 않습니다. 필드명 상수를 확인하세요");
            return null;
        }

        teamNameProperty.stringValue = draft.TeamName;

        lineupProperty.arraySize = AiTeamRosterData.LineupSize;

        for (int i = 0; i < AiTeamRosterData.LineupSize; i++)
        {
            SerializedProperty slotProperty = lineupProperty.GetArrayElementAtIndex(i);

            //HitterPosition은 0부터 1씩 증가하므로 enum 값을 그대로 인덱스로 쓸 수 있음
            slotProperty.FindPropertyRelative(FieldSlotPosition).enumValueIndex = (int)draft.Positions[i];
            slotProperty.FindPropertyRelative(FieldSlotCardId).intValue = draft.HitterCardIds[i];
        }

        WriteIntArray(startersProperty, draft.StartingPitcherCardIds);
        WriteIntArray(relieversProperty, draft.RelieverCardIds);
        closerProperty.intValue = draft.CloserCardId;

        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        return asset;
    }

    //팀 에셋 목록 -> AiRosterSet 에셋 생성 또는 갱신
    private static bool WriteRosterSetAsset(string rosterSetName, List<AiTeamRosterData> teams, string folderPath)
    {
        string assetPath = $"{folderPath}/AiRosterSet_{rosterSetName}.asset";
        AiRosterSet asset = AssetDatabase.LoadAssetAtPath<AiRosterSet>(assetPath);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<AiRosterSet>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        SerializedObject serializedAsset = new SerializedObject(asset);
        SerializedProperty teamsProperty = serializedAsset.FindProperty(FieldTeams);

        if (teamsProperty == null)
        {
            Debug.LogError("[AiRosterSeedImporter] AiRosterSet의 필드명이 임포터와 맞지 않습니다. 필드명 상수를 확인하세요");
            return false;
        }

        teamsProperty.arraySize = teams.Count;

        for (int i = 0; i < teams.Count; i++)
        {
            teamsProperty.GetArrayElementAtIndex(i).objectReferenceValue = teams[i];
        }

        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        //팀 수가 어긋나도 에셋은 만들되 눈에 띄게 알림
        if (teams.Count != AiRosterSet.TeamCount)
        {
            Debug.LogWarning($"[AiRosterSeedImporter] 세트 '{rosterSetName}'의 팀 수가 {teams.Count}개입니다 (기대: {AiRosterSet.TeamCount})");
        }

        return true;
    }

    //int 배열을 SerializedProperty 배열에 복사
    private static void WriteIntArray(SerializedProperty arrayProperty, int[] values)
    {
        arrayProperty.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            arrayProperty.GetArrayElementAtIndex(i).intValue = values[i];
        }
    }

    //에셋 폴더가 없으면 생성 (중첩 경로 지원)
    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    /// <summary>
    /// 팀 1개의 임포트 중간 결과 (CSV 행을 모아 담는 임시 그릇)
    /// </summary>
    private class TeamDraft
    {
        public string RosterSet { get; }
        public string TeamName { get; }

        public HitterPosition[] Positions { get; } = new HitterPosition[AiTeamRosterData.LineupSize];
        public int[] HitterCardIds { get; } = new int[AiTeamRosterData.LineupSize];
        public int[] StartingPitcherCardIds { get; } = new int[AiTeamRosterData.StartingPitcherCount];
        public int[] RelieverCardIds { get; } = new int[AiTeamRosterData.RelieverCount];
        public int CloserCardId { get; set; }

        public TeamDraft(string rosterSet, string teamName)
        {
            RosterSet = rosterSet;
            TeamName = teamName;
        }
    }
}
