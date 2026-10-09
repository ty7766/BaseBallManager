using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 등록된 화면 중 하나만 보이게 한다. 화면 전환 외의 판단은 하지 않는다
/// </summary>
public class UiRouter
{
    private readonly RectTransform _parent;
    private readonly List<UiScreen> _screens = new List<UiScreen>(8);

    public UiRouter(RectTransform parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// 현재 보이는 화면. 아직 아무것도 띄우지 않았으면 null
    /// </summary>
    public UiScreen Current { get; private set; }

    /// <summary>
    /// 화면을 캔버스에 조립해 등록한다
    /// </summary>
    public void Register(UiScreen screen)
    {
        if (screen == null)
        {
            Debug.LogError("[UiRouter]: 등록할 화면이 null입니다");
            return;
        }

        screen.Mount(_parent);
        _screens.Add(screen);
    }

    /// <summary>
    /// 지정한 화면만 띄운다. 등록되지 않은 화면이면 아무것도 하지 않는다
    /// </summary>
    public void Show(UiScreen screen)
    {
        if (!_screens.Contains(screen))
        {
            Debug.LogError("[UiRouter]: 등록되지 않은 화면을 띄우려 했습니다");
            return;
        }

        foreach (UiScreen registered in _screens)
        {
            if (registered != screen)
                registered.SetVisible(false);
        }

        screen.SetVisible(true);
        Current = screen;
    }

    /// <summary>
    /// 다른 화면을 숨기지 않고 위에 겹쳐 띄운다 (일시정지 화면)
    /// </summary>
    public void ShowOverlay(UiScreen screen)
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
    public void HideOverlay(UiScreen screen)
    {
        if (screen != null)
            screen.SetVisible(false);
    }
}
