using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캔버스 아래의 화면 묶음을 들고, 등록된 화면 중 하나만 보이게 한다.
/// 화면 전환 외의 판단은 하지 않는다
/// </summary>
public class UiRouter
{
    private readonly RectTransform _parent;
    private readonly List<UiScreenBehaviour> _screens = new List<UiScreenBehaviour>(20);

    public UiRouter(RectTransform parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// 현재 보이는 화면. 아직 아무것도 띄우지 않았으면 null
    /// </summary>
    public UiScreenBehaviour Current { get; private set; }

    /// <summary>
    /// 화면 프리팹을 캔버스 아래에 깔고 숨긴 채 등록한다
    /// </summary>
    public T Create<T>(string screenName) where T : UiScreenBehaviour
    {
        T screen = UiScreenBehaviour.CreateRuntime<T>(_parent, screenName);

        if (screen == null)
        {
            Debug.LogError($"[UiRouter]: '{screenName}' 화면을 만들지 못했습니다");
            return null;
        }

        screen.SetVisible(false);
        _screens.Add(screen);

        return screen;
    }

    /// <summary>
    /// 지정한 화면만 띄운다. 등록되지 않은 화면이면 아무것도 하지 않는다
    /// </summary>
    public void Show(UiScreenBehaviour screen)
    {
        if (!_screens.Contains(screen))
        {
            Debug.LogError("[UiRouter]: 등록되지 않은 화면을 띄우려 했습니다");
            return;
        }

        UiEffects.Layer?.Clear();

        foreach (UiScreenBehaviour registered in _screens)
        {
            if (registered != screen)
                registered.SetVisible(false);
        }

        screen.SetVisible(true);
        Current = screen;
    }

    /// <summary>
    /// 다른 화면을 숨기지 않고 위에 겹쳐 띄운다 (일시정지 · 카드 선택 화면)
    /// </summary>
    public void ShowOverlay(UiScreenBehaviour screen)
    {
        if (!_screens.Contains(screen))
        {
            Debug.LogError("[UiRouter]: 등록되지 않은 화면을 띄우려 했습니다");
            return;
        }

        screen.Root.SetAsLastSibling();
        screen.SetVisible(true);
    }

    /// <summary>
    /// 겹쳐 띄운 화면을 닫는다
    /// </summary>
    public void HideOverlay(UiScreenBehaviour screen)
    {
        if (screen != null)
            screen.SetVisible(false);
    }
}
