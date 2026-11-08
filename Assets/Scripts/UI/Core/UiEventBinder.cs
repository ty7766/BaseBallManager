using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 프리팹 인스턴스의 버튼에 클릭 핸들러를 다시 거는 공용 도우미.
/// 런타임 AddListener는 프리팹에 직렬화되지 않으므로 Awake에서 매번 걸어야 한다
/// </summary>
public static class UiEventBinder
{
    /// <summary>
    /// 직렬화된 버튼 참조에 핸들러를 건다. 참조가 비어 있으면 아무것도 하지 않는다
    /// </summary>
    public static void Click(Button button, UnityAction handler)
    {
        if (button == null)
            return;

        button.onClick.AddListener(handler);
    }

    /// <summary>
    /// 배열에 담긴 버튼마다 인덱스를 넘기는 핸들러를 건다 (탭 · 그리드 버튼)
    /// </summary>
    public static void ClickEach(Button[] buttons, UnityAction<int> handler)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;

            Click(buttons[i], () => handler(index));
        }
    }

    /// <summary>
    /// 자식에서 이름으로 버튼을 찾아 핸들러를 건다. 없으면 경고를 남기고 null을 돌려준다
    /// </summary>
    public static Button ClickByName(Component owner, string buttonName, UnityAction handler)
    {
        foreach (Button button in owner.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name != buttonName)
                continue;

            button.onClick.AddListener(handler);
            return button;
        }

        Debug.LogWarning($"[{owner.GetType().Name}]: '{buttonName}' 버튼을 찾지 못해 핸들러를 걸지 못했습니다");
        return null;
    }
}
