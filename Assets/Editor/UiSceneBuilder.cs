using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// 구운 화면 프리팹을 씬에 프리팹 인스턴스로 깐다.
/// 재생하지 않아도 하이어라키·씬 뷰에서 UI를 보고 인스펙터로 고칠 수 있게 하는 것이 목적이다
/// </summary>
public static class UiSceneBuilder
{
    private const string MenuRoot = "Tools/BaseBallManager/";

    private static readonly string[] ScreenNames =
    {
        "TitleScreen", "TeamSelectScreen", "LobbyScreen", "LeagueScreen", "PreGameScreen",
        "LiveGameScreen", "PauseSubstitutionScreen", "PostGameScreen", "PostSeasonScreen",
        "InventoryScreen", "GachaScreen", "LineUpScreen", "EnhanceScreen", "TrainScreen",
        "CombineScreen", "DismantleScreen", "GoldenGloveCraftScreen", "CardPickerScreen",
        "CardDetailScreen"
    };

    [MenuItem(MenuRoot + "씬에 UI 배치", false, 13)]
    public static void Place()
    {
        RectTransform canvasRoot = EnsureCanvas();

        if (canvasRoot == null)
            return;

        EnsureEventSystem();

        int placed = 0;

        foreach (string screenName in ScreenNames)
        {
            if (PlaceScreen(canvasRoot, screenName))
                placed++;
        }

        BindBootstrap(canvasRoot);
        MarkSceneDirty();

        Debug.Log($"[UiSceneBuilder]: 화면 {placed}장을 '{GameUiBootstrap.CanvasName}' 아래에 깔았습니다. " +
                  "첫 화면만 켜 두었고 나머지는 꺼져 있습니다 (하이어라키에서 켜면 씬 뷰로 보입니다)");
    }

    [MenuItem(MenuRoot + "씬에서 UI 걷어내기", false, 14)]
    public static void Remove()
    {
        GameObject canvasObject = FindCanvasObject();

        if (canvasObject == null)
            return;

        Object.DestroyImmediate(canvasObject);
        MarkSceneDirty();

        Debug.Log("[UiSceneBuilder]: 씬에서 UI 캔버스를 걷어냈습니다 (런타임 조립으로 되돌아갑니다)");
    }

    //캔버스를 찾거나 새로 만든다
    private static RectTransform EnsureCanvas()
    {
        GameObject existing = FindCanvasObject();

        if (existing != null)
            return (RectTransform)existing.transform;

        RectTransform created = GameUiBootstrap.CreateCanvas(GameUiBootstrap.CanvasName);

        Undo.RegisterCreatedObjectUndo(created.gameObject, "UI 캔버스 생성");

        return created;
    }

    private static GameObject FindCanvasObject()
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == GameUiBootstrap.CanvasName && root.GetComponent<Canvas>() != null)
                return root;
        }

        return null;
    }

    //화면 프리팹 하나를 인스턴스로 붙인다. 이미 있으면 건드리지 않는다
    private static bool PlaceScreen(RectTransform canvasRoot, string screenName)
    {
        if (canvasRoot.Find(screenName) != null)
            return false;

        string path = $"Assets/Resources/UI/Screens/{screenName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"[UiSceneBuilder]: '{path}' 가 없습니다. 'UI 프리팹 모두 굽기' 를 먼저 실행하세요");
            return false;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasRoot);

        instance.name = screenName;
        instance.SetActive(screenName == ScreenNames[0]);

        Undo.RegisterCreatedObjectUndo(instance, "UI 화면 배치");

        return true;
    }

    //부트스트랩이 씬 캔버스를 바로 집도록 참조를 꽂아 둔다
    private static void BindBootstrap(RectTransform canvasRoot)
    {
        GameUiBootstrap bootstrap = Object.FindAnyObjectByType<GameUiBootstrap>(FindObjectsInactive.Include);

        if (bootstrap == null)
        {
            Debug.LogWarning("[UiSceneBuilder]: 씬에 GameUiBootstrap 이 없습니다");
            return;
        }

        SerializedObject serialized = new SerializedObject(bootstrap);

        serialized.FindProperty("_sceneCanvas").objectReferenceValue = canvasRoot.GetComponent<Canvas>();
        serialized.ApplyModifiedProperties();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");

        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();

        Undo.RegisterCreatedObjectUndo(eventSystemObject, "EventSystem 생성");
    }

    private static void MarkSceneDirty()
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
