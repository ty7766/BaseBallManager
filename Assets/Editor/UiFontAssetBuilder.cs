using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// OS 한글 폰트에서 TMP 폰트 자산을 만들어 Resources에 저장한다.
/// 프리팹에 폰트를 직렬화하려면 메모리상의 런타임 자산이 아니라 디스크 자산이 필요하다
/// </summary>
public static class UiFontAssetBuilder
{
    private const string AssetFolder = "Assets/Resources/UI/Fonts";
    private const string AssetPath = AssetFolder + "/UiKoreanFont.asset";

    //카드 폰트(이사만루)는 라이선스 확인 전이라 이 폴더째 git 에서 뺀다
    private const string CardFolder = AssetFolder + "/Card";
    private const string CardSourcePath = CardFolder + "/esamanru Bold.ttf";
    private const string CardAssetPath = CardFolder + "/CardFont.asset";
    //큰 OVR 숫자도 뭉개지지 않는 샘플 크기 · 밑그림자가 번질 여백
    private const int CardSamplingSize = 64;
    private const int CardAtlasPadding = 8;
    private const int CardAtlasSize = 1024;

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
    /// 카드 폰트 자산을 이사만루 Bold 에서 새로 만든다. 글리프는 재생 중 필요한 만큼 채워진다
    /// </summary>
    [MenuItem("Tools/BaseBallManager/카드 폰트 자산 만들기", false, 10)]
    public static void BuildCardFont()
    {
        Font source = AssetDatabase.LoadAssetAtPath<Font>(CardSourcePath);

        if (source == null)
        {
            Debug.LogWarning($"[UiFontAssetBuilder]: '{CardSourcePath}' 에 이사만루 Bold 를 넣고 다시 실행하세요");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CardAssetPath) != null)
            AssetDatabase.DeleteAsset(CardAssetPath);

        TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(source, CardSamplingSize, CardAtlasPadding,
            GlyphRenderMode.SDFAA, CardAtlasSize, CardAtlasSize, AtlasPopulationMode.Dynamic, true);

        created.name = "CardFont";

        AssetDatabase.CreateAsset(created, CardAssetPath);
        AttachSubAssets(created);
        AssetDatabase.SaveAssets();

        Debug.Log($"[UiFontAssetBuilder]: '{CardAssetPath}' 를 만들었습니다");
    }

    /// <summary>
    /// 재생 중 구워진 글리프를 지워 자산을 원래 크기로 되돌린다.
    /// 동적 아틀라스는 쓸수록 커져 커밋 때마다 수 MB가 바뀌므로 커밋 전에 비운다
    /// </summary>
    [MenuItem("Tools/BaseBallManager/UI 폰트 글리프 캐시 비우기", false, 10)]
    public static void ClearGlyphCache()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);

        if (fontAsset == null)
        {
            Debug.LogWarning($"[UiFontAssetBuilder]: '{AssetPath}' 가 없습니다");
            return;
        }

        fontAsset.ClearFontAssetData(true);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();

        Debug.Log("[UiFontAssetBuilder]: 폰트 글리프 캐시를 비웠습니다 (재생하면 다시 채워집니다)");
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
        AttachSubAssets(created);
        AssetDatabase.SaveAssets();

        Debug.Log($"[UiFontAssetBuilder]: '{AssetPath}' 를 만들었습니다");

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
    }

    //아틀라스 텍스처와 머티리얼은 메모리상 객체다. 서브 에셋으로 함께 저장해야 참조가 살아남는다
    private static void AttachSubAssets(TMP_FontAsset fontAsset)
    {
        Texture2D[] atlases = fontAsset.atlasTextures;

        if (atlases != null)
        {
            for (int i = 0; i < atlases.Length; i++)
            {
                if (atlases[i] == null)
                    continue;

                atlases[i].name = $"{fontAsset.name} Atlas {i}";

                AssetDatabase.AddObjectToAsset(atlases[i], fontAsset);
            }
        }

        if (fontAsset.material == null)
            return;

        fontAsset.material.name = $"{fontAsset.name} Material";

        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
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
