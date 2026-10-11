using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// OS 한글 폰트에서 TMP 폰트 자산을 만들어 Resources에 저장한다.
/// 프리팹에 폰트를 직렬화하려면 메모리상의 런타임 자산이 아니라 디스크 자산이 필요하다
/// </summary>
public static class UiFontAssetBuilder
{
    private const string AssetFolder = "Assets/Resources/UI/Fonts";
    private const string AssetPath = AssetFolder + "/UiKoreanFont.asset";

    private static readonly string[] CandidateFamilies =
    {
        "Pretendard", "Malgun Gothic", "맑은 고딕", "Gulim", "Batang", "Dotum"
    };

    [MenuItem("Tools/BaseBallManager/UI 폰트 자산 만들기", false, 10)]
    public static void Build()
    {
        if (Ensure() == null)
            Debug.LogError("[UiFontAssetBuilder]: 한글 폰트 자산을 만들지 못했습니다");
    }

    /// <summary>
    /// 폰트 자산을 보장한다. 이미 있으면 그것을 돌려준다
    /// </summary>
    public static TMP_FontAsset Ensure()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);

        if (existing != null)
            return existing;

        TMP_FontAsset created = CreateFromOsFont();

        if (created == null)
            return null;

        Directory.CreateDirectory(AssetFolder);

        AssetDatabase.CreateAsset(created, AssetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[UiFontAssetBuilder]: '{AssetPath}' 를 만들었습니다");

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
    }

    //후보 패밀리를 차례로 시도한다. 동적 OS 아틀라스라 글리프는 런타임에 채워진다
    private static TMP_FontAsset CreateFromOsFont()
    {
        foreach (string family in CandidateFamilies)
        {
            TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(family, "Regular", 48);

            if (created == null)
                continue;

            created.name = "UiKoreanFont";

            return created;
        }

        return null;
    }
}
