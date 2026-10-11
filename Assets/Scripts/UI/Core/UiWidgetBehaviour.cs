using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 프리팹에 직렬화되는 UI 위젯의 공통 부모.
/// 플레인 클래스로 두면 화면 프리팹이 위젯 참조를 저장하지 못해 MonoBehaviour로 둔다
/// </summary>
public abstract class UiWidgetBehaviour : MonoBehaviour
{
    /// <summary>
    /// 위젯의 RectTransform. 자기 자신이다
    /// </summary>
    public RectTransform Root => (RectTransform)transform;

    //프리팹에서 깨어날 때 버튼 이벤트를 다시 묶는다 (직렬화되지 않는다).
    //코드 조립 경로는 AddComponent 직후라 자식이 없다. 그때는 BuildWidget이 직접 리스너를 건다
    private void Awake()
    {
        if (transform.childCount == 0)
            return;

        BindEvents();
    }

    /// <summary>
    /// 프리팹 인스턴스의 버튼에 핸들러를 건다
    /// </summary>
    protected virtual void BindEvents()
    {
    }

    /// <summary>
    /// 자식에서 이름으로 버튼을 찾아 핸들러를 건다
    /// </summary>
    protected void BindButton(string buttonName, UnityAction handler)
    {
        UiEventBinder.ClickByName(this, buttonName, handler);
    }

    /// <summary>
    /// 직렬화된 버튼 참조에 핸들러를 건다
    /// </summary>
    protected static void Bind(Button button, UnityAction handler)
    {
        UiEventBinder.Click(button, handler);
    }

    /// <summary>
    /// 버튼 배열마다 인덱스를 넘기는 핸들러를 건다
    /// </summary>
    protected static void BindEach(Button[] buttons, UnityAction<int> handler)
    {
        UiEventBinder.ClickEach(buttons, handler);
    }

    /// <summary>
    /// 표시 여부
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// 위젯 노드를 만들어 컴포넌트를 붙인다. 베이크(에디터)와 런타임 생성이 함께 쓴다
    /// </summary>
    protected static T CreateNode<T>(Transform parent, string name) where T : UiWidgetBehaviour
    {
        RectTransform rect = UiBuilder.CreateRect(parent, name);

        return rect.gameObject.AddComponent<T>();
    }
}
