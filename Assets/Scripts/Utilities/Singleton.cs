using UnityEngine;

/// <summary>
/// Generic Singleton base class.
/// Tự động tạo instance nếu chưa có.
/// Có thể tùy chọn không bị destroy khi load scene.
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isQuitting = false;

    public static T Instance
    {
        get
        {
            if (_isQuitting) return null;

            if (_instance == null)
            {
                // Tìm trong scene hiện tại
                _instance = FindObjectOfType<T>();

                // Nếu không có -> tạo mới GameObject chứa nó
                if (_instance == null)
                {
                    var obj = new GameObject(typeof(T).Name);
                    _instance = obj.AddComponent<T>();
                }

                // Gọi hàm khởi tạo mở rộng nếu có
                (_instance as Singleton<T>)?.OnSingletonInit();
            }

            return _instance;
        }
    }

    /// <summary>
    /// Gọi khi instance được tạo hoặc tìm thấy.
    /// Override nếu bạn cần custom init.
    /// </summary>
    protected virtual void OnSingletonInit() => DontDestroy();

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            OnSingletonInit();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    protected virtual void OnApplicationQuit() => _isQuitting = true;

    /// <summary>
    /// Gọi để giữ lại khi load scene mới.
    /// </summary>
    protected void DontDestroy()
    {
        if (Application.isPlaying) DontDestroyOnLoad(gameObject); 
    }
}
