using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면을 코드로 한 번 조립해 Resources 프리팹으로 굽는다.
/// 런타임은 프리팹을 Instantiate만 하므로 기동 비용이 줄고 인스펙터로 레이아웃을 손볼 수 있다
/// </summary>
public static class UiPrefabBaker
{
    private const string PrefabFolder = "Assets/Resources/UI/Screens";
    private const string BakeRootName = "[UI 프리팹 굽기]";

    [MenuItem("Tools/BaseBallManager/UI 프리팹 모두 굽기", false, 11)]
    public static void BakeAll()
    {
        if (UiFontAssetBuilder.Ensure() == null)
        {
            Debug.LogError("[UiPrefabBaker]: 폰트 자산이 없어 굽기를 멈춥니다");
            return;
        }

        Directory.CreateDirectory(PrefabFolder);

        GameObject bakeRoot = new GameObject(BakeRootName, typeof(RectTransform));

        bakeRoot.hideFlags = HideFlags.DontSave;

        Canvas canvas = bakeRoot.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = bakeRoot.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UiBuilder.DesignSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        RectTransform parent = bakeRoot.GetComponent<RectTransform>();
        int baked = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            baked += Bake<TitleScreen>(parent, "TitleScreen");
            baked += Bake<TeamSelectScreen>(parent, "TeamSelectScreen");
            baked += Bake<LobbyScreen>(parent, "LobbyScreen");
            baked += Bake<LeagueScreen>(parent, "LeagueScreen");
            baked += Bake<PreGameScreen>(parent, "PreGameScreen");
            baked += Bake<LiveGameScreen>(parent, "LiveGameScreen");
            baked += Bake<PauseSubstitutionScreen>(parent, "PauseSubstitutionScreen");
            baked += Bake<PostGameScreen>(parent, "PostGameScreen");
            baked += Bake<PostSeasonScreen>(parent, "PostSeasonScreen");
            baked += Bake<InventoryScreen>(parent, "InventoryScreen");
            baked += Bake<GachaScreen>(parent, "GachaScreen");
            baked += Bake<LineUpScreen>(parent, "LineUpScreen");
            baked += Bake<EnhanceScreen>(parent, "EnhanceScreen");
            baked += Bake<TrainScreen>(parent, "TrainScreen");
            baked += Bake<CombineScreen>(parent, "CombineScreen");
            baked += Bake<DismantleScreen>(parent, "DismantleScreen");
            baked += Bake<GoldenGloveCraftScreen>(parent, "GoldenGloveCraftScreen");
            baked += Bake<CardPickerScreen>(parent, "CardPickerScreen");
            baked += Bake<CardDetailScreen>(parent, "CardDetailScreen");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();

            Object.DestroyImmediate(bakeRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[UiPrefabBaker]: 화면 {baked}장을 '{PrefabFolder}' 에 구웠습니다");
    }

    [MenuItem("Tools/BaseBallManager/UI 프리팹 모두 지우기", false, 12)]
    public static void DeleteAll()
    {
        if (!Directory.Exists(PrefabFolder))
            return;

        AssetDatabase.DeleteAsset(PrefabFolder);
        AssetDatabase.Refresh();

        Debug.Log("[UiPrefabBaker]: 구운 화면 프리팹을 지웠습니다");
    }

    //화면 하나를 조립해 프리팹으로 저장한다. 성공하면 1을 돌려준다
    private static int Bake<T>(RectTransform parent, string screenName) where T : UiScreenBehaviour
    {
        T screen = UiScreenBehaviour.CreateFromCode<T>(parent, screenName);

        if (screen == null)
        {
            Debug.LogError($"[UiPrefabBaker]: '{screenName}' 조립에 실패했습니다");
            return 0;
        }

        //프리팹에서 깨어날 때 FadeIn이 알파를 올리므로 저장 시점 값은 1로 둔다
        CanvasGroup group = screen.Root.GetComponent<CanvasGroup>();

        if (group != null)
            group.alpha = 1f;

        screen.gameObject.SetActive(true);

        string path = $"{PrefabFolder}/{screenName}.prefab";

        PrefabUtility.SaveAsPrefabAsset(screen.gameObject, path, out bool success);

        Object.DestroyImmediate(screen.gameObject);

        if (!success)
        {
            Debug.LogError($"[UiPrefabBaker]: '{path}' 저장에 실패했습니다");
            return 0;
        }

        return 1;
    }
}
