using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 한 장의 베이스. 루트 노드를 만들고 한 번만 조립한 뒤 보이기/숨기기로 전환한다
/// </summary>
public abstract class UiScreen
{
    /// <summary>
    /// 화면의 루트. Mount 전에는 null
    /// </summary>
    public RectTransform Root { get; private set; }

    public bool IsMounted => Root != null;

    /// <summary>
    /// 캔버스 아래에 화면을 조립한다. 두 번 불러도 한 번만 만든다
    /// </summary>
    public void Mount(RectTransform parent)
    {
        if (IsMounted)
            return;

        Root = UiBuilder.CreateRect(parent, ScreenName);
        UiBuilder.Stretch(Root, 0f, 0f, 0f, 0f);

        Image background = UiBuilder.CreateImage(Root, "Background", UiPalette.BgApp);
        UiBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        Build();
        SetVisible(false);
    }

    /// <summary>
    /// 화면 표시 여부. 보여질 때마다 OnShown으로 데이터를 다시 채운다
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (!IsMounted)
            return;

        Root.gameObject.SetActive(visible);

        if (visible)
            OnShown();
    }

    /// <summary>
    /// 하이어라키에 보일 이름
    /// </summary>
    protected abstract string ScreenName { get; }

    /// <summary>
    /// 화면 요소를 조립한다. Mount에서 한 번만 호출된다
    /// </summary>
    protected abstract void Build();

    /// <summary>
    /// 화면이 보여질 때마다 호출된다. 데이터 갱신 지점
    /// </summary>
    protected virtual void OnShown()
    {
    }
}
