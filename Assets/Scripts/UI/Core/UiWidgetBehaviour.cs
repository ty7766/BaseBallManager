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

    //직렬화하지 않는다. 프리팹 인스턴스는 항상 false 로 깨어나 한 번만 묶인다
    private bool _eventsBound;

    //꺼진 채로 깔아 둔 화면 안의 위젯은 Awake 가 돌지 않으므로 처음 켜질 때 묶는다
    private void OnEnable()
    {
        if (_eventsBound)
            return;

        _eventsBound = true;
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

        //끈 채로 붙여야 자식이 없는 시점에 OnEnable 이 돌지 않는다
        rect.gameObject.SetActive(false);

        T widget = rect.gameObject.AddComponent<T>();

        //BuildWidget 이 리스너를 직접 건다. OnEnable 이 또 묶지 않도록 표시해 둔다
        widget._eventsBound = true;

        rect.gameObject.SetActive(true);

        return widget;
    }
}
