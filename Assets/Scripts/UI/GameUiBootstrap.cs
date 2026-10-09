using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 씬에 하나만 두는 UI 조립 진입점. 캔버스·입력·진행 드라이버를 만들어 흐름 제어기에 넘긴다
/// </summary>
public class GameUiBootstrap : MonoBehaviour
{
    [SerializeField, Tooltip("타석 하나를 보여주고 다음으로 넘어가기까지의 시간(초)")]
    private float _secondsPerAtBat = 1.2f;

    private void Start()
    {
        RectTransform canvasRoot = CreateCanvas();

        EnsureEventSystem();

        GamePlaybackDriver driver = gameObject.AddComponent<GamePlaybackDriver>();

        driver.SetSecondsPerAtBat(_secondsPerAtBat);

        LiveGameController interruptController = gameObject.AddComponent<LiveGameController>();
        AppFlowController flow = gameObject.AddComponent<AppFlowController>();

        flow.Initialize(canvasRoot, driver, interruptController);
    }

    //1080×1920 기준 세로 고정 캔버스. 세로가 긴 기기는 아래로 더 보인다
    private RectTransform CreateCanvas()
    {
        GameObject canvasObject = new GameObject("GameCanvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UiBuilder.DesignSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        canvasObject.AddComponent<GraphicRaycaster>();

        return canvasObject.GetComponent<RectTransform>();
    }

    //버튼 입력을 받으려면 EventSystem이 필요하다. 이 프로젝트는 새 입력 시스템을 쓴다
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");

        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
