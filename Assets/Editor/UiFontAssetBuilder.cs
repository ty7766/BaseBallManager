using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 본문(Pretendard) · 카드(이사만루) 폰트 파일에서 TMP 폰트 자산을 만들어 Resources에 저장한다.
/// 프리팹에 폰트를 직렬화하려면 메모리상의 런타임 자산이 아니라 디스크 자산이 필요하다
/// </summary>
public static class UiFontAssetBuilder
{
    private const string AssetFolder = "Assets/Resources/UI/Fonts";
    private const string AssetPath = AssetFolder + "/UiKoreanFont.asset";

    //본문 폰트(Pretendard · SIL OFL). 파일을 게임에 넣어 모바일에서도 같은 글꼴이 나온다. 라이선스 원문을 옆에 둔다
    private const string BodySourcePath = AssetFolder + "/Pretendard/Pretendard-Regular.ttf";
    private const int BodySamplingSize = 48;
    private const int BodyAtlasPadding = 9;
    private const int BodyAtlasSize = 1024;

    //카드 폰트(이사만루 · 공게임즈). 출처 표기 조건이라 README 크레딧에 남긴다
    private const string CardFolder = AssetFolder + "/Card";
    private const string CardSourcePath = CardFolder + "/esamanru Bold.ttf";
    private const string CardAssetPath = CardFolder + "/CardFont.asset";
    //큰 OVR 숫자도 뭉개지지 않는 샘플 크기 · 밑그림자가 번질 여백
    private const int CardSamplingSize = 64;
    private const int CardAtlasPadding = 8;
    private const int CardAtlasSize = 1024;

    [MenuItem("Tools/BaseBallManager/UI 폰트 자산 만들기", false, 10)]
    public static void Build()
    {
        if (Ensure() == null)
            Debug.LogError("[UiFontAssetBuilder]: 한글 폰트 자산을 만들지 못했습니다");
    }

    /// <summary>
    /// 본문 폰트 자산을 Pretendard 파일로 다시 만든다. 이미 있으면 자산 · 머티리얼 ID 를 그대로 두고 내용만 바꿔
    /// 프리팹 글자들이 가리키는 참조를 살린다
    /// </summary>
    [MenuItem("Tools/BaseBallManager/본문 폰트 자산 다시 만들기 (Pretendard)", false, 10)]
    public static void RebuildBodyFont()
    {
        TMP_FontAsset fresh = CreateBodyFont();

        if (fresh == null)
            return;

        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);

        if (existing == null)
            SaveNew(fresh, AssetPath);
        else
            ReplaceKeepingReferences(existing, fresh);

        Debug.Log($"[UiFontAssetBuilder]: '{AssetPath}' 를 Pretendard 로 만들었습니다");
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
        ClearGlyphs(AssetPath);
        ClearGlyphs(CardAssetPath);

        AssetDatabase.SaveAssets();

        Debug.Log("[UiFontAssetBuilder]: 본문 · 카드 폰트 글리프 캐시를 비웠습니다 (재생하면 다시 채워집니다)");
    }

    /// <summary>
    /// 폰트 자산을 보장한다. 이미 있으면 그것을 돌려준다
    /// </summary>
    public static TMP_FontAsset Ensure()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);

        if (existing != null)
            return existing;

        TMP_FontAsset created = CreateBodyFont();

        if (created == null)
            return null;

        SaveNew(created, AssetPath);

        Debug.Log($"[UiFontAssetBuilder]: '{AssetPath}' 를 만들었습니다");

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
    }

    //Pretendard 파일에서 동적 아틀라스 자산을 만든다. 글리프는 재생 중 필요한 만큼 채워진다
    private static TMP_FontAsset CreateBodyFont()
    {
        Font source = AssetDatabase.LoadAssetAtPath<Font>(BodySourcePath);

        if (source == null)
        {
            Debug.LogWarning($"[UiFontAssetBuilder]: '{BodySourcePath}' 에 Pretendard-Regular.ttf 를 넣고 다시 실행하세요");
            return null;
        }

        TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(source, BodySamplingSize, BodyAtlasPadding,
            GlyphRenderMode.SDFAA, BodyAtlasSize, BodyAtlasSize, AtlasPopulationMode.Dynamic, true);

        created.name = "UiKoreanFont";

        return created;
    }

    //새 자산을 디스크에 쓰고 아틀라스 · 머티리얼을 서브 에셋으로 붙인다
    private static void SaveNew(TMP_FontAsset fontAsset, string assetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
        AssetDatabase.CreateAsset(fontAsset, assetPath);
        AttachSubAssets(fontAsset);
        AssetDatabase.SaveAssets();
    }

    //프리팹이 자산(11400000)과 머티리얼을 fileID 로 가리킨다. 두 객체는 남기고 내용 · 아틀라스만 새것으로 바꾼다
    private static void ReplaceKeepingReferences(TMP_FontAsset existing, TMP_FontAsset fresh)
    {
        Material keptMaterial = existing.material;

        foreach (Object subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetPath))
        {
            if (subAsset is Texture2D)
                Object.DestroyImmediate(subAsset, true);
        }

        fresh.name = existing.name;
        EditorUtility.CopySerialized(fresh, existing);

        Texture2D[] atlases = existing.atlasTextures;

        for (int i = 0; i < atlases.Length; i++)
        {
            atlases[i].name = $"{existing.name} Atlas {i}";
            AssetDatabase.AddObjectToAsset(atlases[i], existing);
        }

        EditorUtility.CopySerialized(fresh.material, keptMaterial);
        keptMaterial.name = $"{existing.name} Material";
        keptMaterial.mainTexture = atlases[0];
        existing.material = keptMaterial;

        SerializedObject serialized = new SerializedObject(existing);
        serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(keptMaterial);
        EditorUtility.SetDirty(existing);
        AssetDatabase.SaveAssets();
    }

    //동적 아틀라스에 쌓인 글리프를 지운다. 자산이 없으면 경고만 남긴다
    private static void ClearGlyphs(string assetPath)
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

        if (fontAsset == null)
        {
            Debug.LogWarning($"[UiFontAssetBuilder]: '{assetPath}' 가 없습니다");
            return;
        }

        fontAsset.ClearFontAssetData(true);
        EditorUtility.SetDirty(fontAsset);
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
}
