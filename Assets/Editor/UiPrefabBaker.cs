using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// 화면을 코드로 한 번 조립해 Resources 프리팹으로 굽는다.
/// 런타임은 프리팹을 Instantiate만 하므로 기동 비용이 줄고 인스펙터로 레이아웃을 손볼 수 있다
/// </summary>
public static class UiPrefabBaker
{
    private const string PrefabFolder = "Assets/Resources/UI/Screens";
    private const string BakeRootName = "[UI 프리팹 굽기]";
    private const string SelectedMenu = "Tools/BaseBallManager/선택한 UI 프리팹만 굽기";
    private const string SelectedContextMenu = "Assets/BaseBallManager/이 화면 프리팹만 다시 굽기";

    //화면 이름(= 클래스 이름 = 프리팹 파일명) → 조립해 굽는 함수. 모두 굽기와 골라 굽기가 같은 목록을 쓴다
    private static readonly Dictionary<string, Func<RectTransform, int>> Bakers = CreateBakers();

    [MenuItem("Tools/BaseBallManager/UI 프리팹 모두 굽기", false, 11)]
    public static void BakeAll()
    {
        BakeScreens(Bakers.Keys);
    }

    /// <summary>
    /// Project 창에서 고른 화면 프리팹만 다시 굽는다. 메뉴 브랜치는 이것을 쓴다.
    /// 모두 굽기는 다른 화면 프리팹까지 바꿔 브랜치를 합칠 때 충돌이 난다
    /// </summary>
    [MenuItem(SelectedMenu, false, 11)]
    [MenuItem(SelectedContextMenu, false, 2000)]
    public static void BakeSelected()
    {
        List<string> screenNames = GetSelectedScreenNames();

        if (screenNames.Count == 0)
        {
            Debug.LogWarning($"[UiPrefabBaker]: Project 창에서 '{PrefabFolder}' 의 화면 프리팹을 고른 뒤 실행하세요");
            return;
        }

        BakeScreens(screenNames);
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

    //고른 것 중 화면 프리팹이 없으면 메뉴를 흐리게 둔다
    [MenuItem(SelectedMenu, true)]
    [MenuItem(SelectedContextMenu, true)]
    private static bool CanBakeSelected()
    {
        return GetSelectedScreenNames().Count > 0;
    }

    //화면을 하나 더 만들면 여기에 한 줄을 더한다
    private static Dictionary<string, Func<RectTransform, int>> CreateBakers()
    {
        Dictionary<string, Func<RectTransform, int>> bakers = new Dictionary<string, Func<RectTransform, int>>(20);

        Register<TitleScreen>(bakers);
        Register<TeamSelectScreen>(bakers);
        Register<LobbyScreen>(bakers);
        Register<LeagueScreen>(bakers);
        Register<PreGameScreen>(bakers);
        Register<LiveGameScreen>(bakers);
        Register<PauseSubstitutionScreen>(bakers);
        Register<PostGameScreen>(bakers);
        Register<PostSeasonScreen>(bakers);
        Register<InventoryScreen>(bakers);
        Register<GachaScreen>(bakers);
        Register<LineUpScreen>(bakers);
        Register<EnhanceScreen>(bakers);
        Register<TrainScreen>(bakers);
        Register<CombineScreen>(bakers);
        Register<DismantleScreen>(bakers);
        Register<GoldenGloveCraftScreen>(bakers);
        Register<CardPickerScreen>(bakers);
        Register<CardDetailScreen>(bakers);

        return bakers;
    }

    private static void Register<T>(Dictionary<string, Func<RectTransform, int>> bakers) where T : UiScreenBehaviour
    {
        string screenName = typeof(T).Name;

        bakers[screenName] = parent => Bake<T>(parent, screenName);
    }

    //Project 창에서 고른 것 중 목록에 있는 화면 프리팹만 추린다
    private static List<string> GetSelectedScreenNames()
    {
        List<string> screenNames = new List<string>();

        foreach (Object selected in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selected);

            if (!path.StartsWith(PrefabFolder + "/") || !path.EndsWith(".prefab"))
                continue;

            string screenName = Path.GetFileNameWithoutExtension(path);

            if (Bakers.ContainsKey(screenName) && !screenNames.Contains(screenName))
                screenNames.Add(screenName);
        }

        return screenNames;
    }

    //굽기용 캔버스 하나에 화면을 차례로 조립해 저장한다
    private static void BakeScreens(IEnumerable<string> screenNames)
    {
        if (UiFontAssetBuilder.Ensure() == null)
        {
            Debug.LogError("[UiPrefabBaker]: 폰트 자산이 없어 굽기를 멈춥니다");
            return;
        }

        Directory.CreateDirectory(PrefabFolder);

        GameObject bakeRoot = CreateBakeRoot();
        RectTransform parent = bakeRoot.GetComponent<RectTransform>();
        int baked = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string screenName in screenNames)
                baked += Bakers[screenName](parent);
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

    //GameUiBootstrap 과 같은 캔버스 설정. 씬에 저장되지 않는다
    private static GameObject CreateBakeRoot()
    {
        GameObject bakeRoot = new GameObject(BakeRootName, typeof(RectTransform));

        bakeRoot.hideFlags = HideFlags.DontSave;

        Canvas canvas = bakeRoot.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = bakeRoot.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UiBuilder.DesignSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        return bakeRoot;
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
