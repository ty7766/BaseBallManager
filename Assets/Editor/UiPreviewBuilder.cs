using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 에디트 모드에서 화면을 코드로 조립해 Scene 뷰로 확인한다.
/// 프리팹을 거치지 않으므로 굽기 전의 좌표·스프라이트를 눈으로 보는 데 쓴다
/// </summary>
public static class UiPreviewBuilder
{
    private const string PreviewRootName = "[UI 미리보기]";
    private const string MenuRoot = "Tools/BaseBallManager/UI 미리보기/";

    [MenuItem(MenuRoot + "타이틀", false, 100)]
    private static void PreviewTitle()
    {
        Show<TitleScreen>("TitleScreen", screen => screen.SetSaveAvailable(true, "저장된 진행이 있습니다"));
    }

    [MenuItem(MenuRoot + "팀 선택", false, 101)]
    private static void PreviewTeamSelect()
    {
        Show<TeamSelectScreen>("TeamSelectScreen", screen => screen.SetTeams(new[]
        {
            "기아", "삼성", "LG", "한화", "두산", "SSG", "KT", "NC", "키움", "롯데"
        }));
    }

    [MenuItem(MenuRoot + "로비", false, 102)]
    private static void PreviewLobby()
    {
        Show<LobbyScreen>("LobbyScreen", null);
    }

    [MenuItem(MenuRoot + "리그", false, 103)]
    private static void PreviewLeague()
    {
        Show<LeagueScreen>("LeagueScreen", null);
    }

    [MenuItem(MenuRoot + "가을야구", false, 104)]
    private static void PreviewPostSeason()
    {
        Show<PostSeasonScreen>("PostSeasonScreen", null);
    }

    [MenuItem(MenuRoot + "경기 전", false, 120)]
    private static void PreviewPreGame()
    {
        Show<PreGameScreen>("PreGameScreen", null);
    }

    [MenuItem(MenuRoot + "경기 중", false, 121)]
    private static void PreviewLiveGame()
    {
        Show<LiveGameScreen>("LiveGameScreen", screen => screen.SetPauseState(true, false));
    }

    [MenuItem(MenuRoot + "일시정지 (교체)", false, 122)]
    private static void PreviewPause()
    {
        Show<PauseSubstitutionScreen>("PauseSubstitutionScreen", null);
    }

    [MenuItem(MenuRoot + "경기 후", false, 123)]
    private static void PreviewPostGame()
    {
        Show<PostGameScreen>("PostGameScreen", null);
    }

    [MenuItem(MenuRoot + "뽑기", false, 140)]
    private static void PreviewGacha()
    {
        Show<GachaScreen>("GachaScreen", null);
    }

    [MenuItem(MenuRoot + "인벤토리", false, 141)]
    private static void PreviewInventory()
    {
        Show<InventoryScreen>("InventoryScreen", null);
    }

    [MenuItem(MenuRoot + "라인업", false, 142)]
    private static void PreviewLineUp()
    {
        Show<LineUpScreen>("LineUpScreen", null);
    }

    [MenuItem(MenuRoot + "강화", false, 160)]
    private static void PreviewEnhance()
    {
        Show<EnhanceScreen>("EnhanceScreen", null);
    }

    [MenuItem(MenuRoot + "훈련 · 돌파", false, 161)]
    private static void PreviewTrain()
    {
        Show<TrainScreen>("TrainScreen", null);
    }

    [MenuItem(MenuRoot + "카드 조합", false, 162)]
    private static void PreviewCombine()
    {
        Show<CombineScreen>("CombineScreen", null);
    }

    [MenuItem(MenuRoot + "카드 분해", false, 163)]
    private static void PreviewDismantle()
    {
        Show<DismantleScreen>("DismantleScreen", null);
    }

    [MenuItem(MenuRoot + "골든글러브 제작", false, 164)]
    private static void PreviewGoldenGloveCraft()
    {
        Show<GoldenGloveCraftScreen>("GoldenGloveCraftScreen", null);
    }

    [MenuItem(MenuRoot + "카드 선택 모달", false, 180)]
    private static void PreviewCardPicker()
    {
        Show<CardPickerScreen>("CardPickerScreen", screen => screen.Open("카드 선택", 3, null, null));
    }

    [MenuItem(MenuRoot + "카드 상세", false, 181)]
    private static void PreviewCardDetail()
    {
        Show<CardDetailScreen>("CardDetailScreen", null);
    }

    [MenuItem(MenuRoot + "미리보기 지우기", false, 300)]
    private static void Clear()
    {
        GameObject existing = GameObject.Find(PreviewRootName);

        while (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
            existing = GameObject.Find(PreviewRootName);
        }

        Debug.Log("[UiPreviewBuilder]: 미리보기를 지웠습니다");
    }

    //캔버스를 새로 만들고 화면 하나를 코드로 조립해 띄운다
    private static void Show<T>(string screenName, Action<T> setup) where T : UiScreenBehaviour
    {
        Clear();

        RectTransform canvasRoot = CreatePreviewCanvas();
        T screen = UiScreenBehaviour.CreateFromCode<T>(canvasRoot, screenName);

        if (setup != null)
            setup(screen);

        screen.SetVisible(true);

        //FadeIn 이 알파 0에서 시작하므로 에디트 모드에서는 즉시 1로 돌려놓는다
        CanvasGroup group = screen.Root.GetComponent<CanvasGroup>();

        if (group != null)
            group.alpha = 1f;

        Selection.activeGameObject = screen.Root.gameObject;

        Debug.Log($"[UiPreviewBuilder]: '{screenName}' 미리보기를 띄웠습니다. " +
                  "Scene 뷰에서 확인하고 메뉴의 '미리보기 지우기'로 정리하세요");
    }

    //GameUiBootstrap 과 같은 설정. DontSave 라 씬에 저장되지 않는다
    private static RectTransform CreatePreviewCanvas()
    {
        GameObject canvasObject = new GameObject(PreviewRootName, typeof(RectTransform));

        canvasObject.hideFlags = HideFlags.DontSave;

        Canvas canvas = canvasObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UiBuilder.DesignSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        canvasObject.AddComponent<GraphicRaycaster>();

        return canvasObject.GetComponent<RectTransform>();
    }
}
