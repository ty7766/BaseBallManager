using UnityEngine;

/// <summary>
/// 앱 시작 때 목표 프레임을 정한다. 안드로이드는 따로 정하지 않으면 30프레임으로 돌아
/// 에디터보다 연출이 끊겨 보인다
/// </summary>
public static class AppFrameRate
{
    private const int TargetFrameRate = 60;

    //첫 씬이 뜨기 전에 한 번만 돈다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        Application.targetFrameRate = TargetFrameRate;
    }
}
