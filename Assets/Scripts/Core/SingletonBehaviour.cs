using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 싱글톤 공통 부모
/// </summary>
public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    public static T Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = (T)this;
        DontDestroyOnLoad(gameObject);
        OnSingletonAwake();
    }

    //따로 초기화할 것이 있으면 이것을 override 하여 사용
    protected virtual void OnSingletonAwake() { }
}
