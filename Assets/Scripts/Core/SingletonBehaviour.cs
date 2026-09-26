using UnityEngine;
/// <summary>
/// 싱글톤 공통 부모
/// </summary>
public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    public static T Instance { get; private set; }

    private void Awake()
    {
        //중복은 자기 자신만 지운다. 부모(그룹 오브젝트)를 지우면 형제 매니저까지 사라진다
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = (T)this;

        //DontDestroyOnLoad는 루트 오브젝트에만 걸린다. 자식으로 묶여 있으면 부모째로 걸어야 살아남는다
        DontDestroyOnLoad(transform.root.gameObject);

        OnSingletonAwake();
    }

    /// <summary>
    /// 따로 초기화할 것이 있으면 이것을 override 하여 사용
    /// </summary>
    protected virtual void OnSingletonAwake() { }
}
