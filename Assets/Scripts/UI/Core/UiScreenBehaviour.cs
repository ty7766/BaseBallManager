using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 화면 한 장. 프리팹으로 구워 두고 런타임에는 Instantiate만 한다.
/// Build는 프리팹을 굽는 에디터 도구에서만 호출된다
/// </summary>
public abstract class UiScreenBehaviour : MonoBehaviour
{
    private const float FadeInSeconds = 0.16f;

    /// <summary>
    /// 프리팹이 놓이는 Resources 하위 폴더
    /// </summary>
    public const string PrefabResourceRoot = "UI/Screens/";

    [SerializeField] private CanvasGroup _fadeGroup;

    /// <summary>
    /// 화면의 루트. 자기 자신이다
    /// </summary>
    public RectTransform Root => (RectTransform)transform;

    //프리팹에서 깨어날 때 버튼 이벤트를 다시 묶는다.
    //런타임 AddListener 는 프리팹에 직렬화되지 않아 인스턴스마다 새로 걸어야 한다.
    //코드 조립 경로는 AddComponent 직후라 자식이 없다. 그때는 Build가 직접 리스너를 건다
    private void Awake()
    {
        if (transform.childCount == 0)
            return;

        BindEvents();
    }

    /// <summary>
    /// 프리팹 인스턴스의 버튼에 핸들러를 건다. 베이크로 만든 화면은 이것으로만 동작한다
    /// </summary>
    protected virtual void BindEvents()
    {
    }

    /// <summary>
    /// 자식에서 이름으로 버튼을 찾아 핸들러를 건다 (지역변수로만 잡던 버튼)
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
    /// 화면 표시 여부. 보여질 때마다 OnShown으로 데이터를 다시 채운다
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (!visible)
            return;

        OnShown();
        UiEffects.FadeIn(_fadeGroup, FadeInSeconds);
    }

    /// <summary>
    /// 화면 하나를 만든다. Resources에 구워 둔 프리팹이 있으면 그것을, 없으면 코드로 조립한다
    /// </summary>
    public static T CreateRuntime<T>(RectTransform parent, string screenName) where T : UiScreenBehaviour
    {
        GameObject prefab = Resources.Load<GameObject>(PrefabResourceRoot + screenName);

        if (prefab != null)
        {
            GameObject spawned = Instantiate(prefab, parent, false);

            spawned.name = screenName;

            return spawned.GetComponent<T>();
        }

        return CreateFromCode<T>(parent, screenName);
    }

    /// <summary>
    /// 프리팹을 거치지 않고 코드로 화면을 조립한다. 프리팹을 굽는 도구와 미리보기가 쓴다
    /// </summary>
    public static T CreateFromCode<T>(RectTransform parent, string screenName) where T : UiScreenBehaviour
    {
        RectTransform rect = UiBuilder.CreateRect(parent, screenName);
        T screen = rect.gameObject.AddComponent<T>();

        screen.BakeHierarchy();

        return screen;
    }

    /// <summary>
    /// 프리팹을 굽는 에디터 도구가 호출한다. 배경과 본문을 코드로 조립하고 참조를 채운다
    /// </summary>
    public void BakeHierarchy()
    {
        _fadeGroup = gameObject.GetComponent<CanvasGroup>();

        if (_fadeGroup == null)
            _fadeGroup = gameObject.AddComponent<CanvasGroup>();

        UiBuilder.Stretch(Root, 0f, 0f, 0f, 0f);

        Image background = UiBuilder.CreateImage(Root, "Background", UiPalette.BgApp);
        UiBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        Build();
    }

    /// <summary>
    /// 화면 요소를 조립한다. 베이크 시점에 한 번만 돈다
    /// </summary>
    protected abstract void Build();

    /// <summary>
    /// 화면이 보여질 때마다 호출된다. 데이터 갱신 지점
    /// </summary>
    protected virtual void OnShown()
    {
    }
}
