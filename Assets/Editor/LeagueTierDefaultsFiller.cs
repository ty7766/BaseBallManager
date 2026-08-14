using UnityEditor;
using UnityEngine;

/// <summary>
/// 티어 테이블의 경기 수 · 연전 수를 기획서 기본값으로 채움
/// </summary>
public static class LeagueTierDefaultsFiller
{
    //SerializedProperty 조회용 필드명 - LeagueTierTable / LeagueTierEntry의 필드명을 바꾸면 여기도 고쳐야 함
    private const string FieldEntries = "_entries";
    private const string FieldGameCount = "_gameCount";
    private const string FieldSeriesLength = "_seriesLength";

    //기획서 7.2 - 티어별 정규시즌 경기 수 (배열 인덱스 = LeagueTier)
    private static readonly int[] GameCounts =
    {
        9,  36, 36,         //베이직 1·2·3
        54, 54, 54,         //루키 1·2·3
        81, 81, 81,         //아마추어 1·2·3
        108, 108, 108,      //프로 1·2·3
        144, 144, 144,      //마이너 1·2·3
        144, 144, 144,      //메이저 1·2·3
        144, 144, 144       //레전드 1·2·3
    };

    //기획서 7.3 - 프로 리그부터 같은 팀과 3연전
    private const int FirstSeriesTierIndex = (int)LeagueTier.Pro1;
    private const int SeriesLength = 3;

    //메뉴 진입점 - 프로젝트의 티어 테이블 전체를 기본값으로 채움
    [MenuItem("Tools/BaseBallManager/리그 티어 기본값 채우기")]
    public static void Fill()
    {
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(LeagueTierTable)}");

        if (guids.Length == 0)
        {
            Debug.LogError("[LeagueTierDefaultsFiller] 티어 테이블 에셋을 찾을 수 없습니다");
            return;
        }

        int filledCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            LeagueTierTable table = AssetDatabase.LoadAssetAtPath<LeagueTierTable>(assetPath);

            if (table == null)
                continue;

            if (!FillTable(table))
                return;

            filledCount++;
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"[LeagueTierDefaultsFiller] 티어 테이블 {filledCount}개에 경기 수 · 연전 수를 채웠습니다");
    }

    //티어 테이블 1개 채우기
    private static bool FillTable(LeagueTierTable table)
    {
        SerializedObject serializedTable = new SerializedObject(table);
        SerializedProperty entriesProperty = serializedTable.FindProperty(FieldEntries);

        if (entriesProperty == null)
        {
            Debug.LogError("[LeagueTierDefaultsFiller] LeagueTierTable의 필드명이 채우기 도구와 맞지 않습니다. 필드명 상수를 확인하세요", table);
            return false;
        }

        //인스펙터에서 배열 크기가 줄어 있어도 21칸으로 복구
        entriesProperty.arraySize = LeagueTierTable.TierCount;

        for (int i = 0; i < LeagueTierTable.TierCount; i++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);

            SerializedProperty gameCountProperty = entryProperty.FindPropertyRelative(FieldGameCount);
            SerializedProperty seriesLengthProperty = entryProperty.FindPropertyRelative(FieldSeriesLength);

            if (gameCountProperty == null || seriesLengthProperty == null)
            {
                Debug.LogError("[LeagueTierDefaultsFiller] LeagueTierEntry의 필드명이 채우기 도구와 맞지 않습니다. 필드명 상수를 확인하세요", table);
                return false;
            }

            gameCountProperty.intValue = GameCounts[i];
            seriesLengthProperty.intValue = i >= FirstSeriesTierIndex ? SeriesLength : 1;
        }

        serializedTable.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);

        return true;
    }
}
