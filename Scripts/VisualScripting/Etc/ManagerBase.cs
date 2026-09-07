using UnityEngine;

public abstract class ManagerBase<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _isApplicationQuitting = false;

    public static T instance
    {
        get
        {
            // 앱 종료 중에는 인스턴스를 새로 생성하지 않음 (유령 객체 방지)
            if (_isApplicationQuitting)
            {
                Debug.LogWarning($"[Singleton] {typeof(T).Name} 인스턴스가 종료 중 호출되었습니다. Null을 반환합니다.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)GameObject.FindAnyObjectByType(typeof(T));

                    if (_instance == null)
                    {
                        var singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = typeof(T).Name + " (Singleton)";
                        DontDestroyOnLoad(singleton);
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            // 중복 생성된 객체 파괴
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
    }
}