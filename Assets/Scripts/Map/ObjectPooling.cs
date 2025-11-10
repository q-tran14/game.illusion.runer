using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : Singleton<ObjectPool>
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private int minPoolSize = 12;
    [SerializeField] private int maxPoolSize = 15;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private int totalCreated = 0;

    protected override void OnSingletonInit()
    {
        base.OnSingletonInit();
        InitPool();
    }

    void InitPool()
    {
        // Khởi tạo tối thiểu 12 object
        for (int i = 0; i < minPoolSize; i++)
        {
            var obj = CreateNewInstance();
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject Get()
    {
        // Nếu có sẵn trong pool → dùng lại
        if (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        // Nếu chưa tới giới hạn → tạo mới
        if (totalCreated < maxPoolSize)
        {
            var newObj = CreateNewInstance();
            newObj.SetActive(true);
            return newObj;
        }

        // Nếu đã tới giới hạn → cảnh báo và lấy tạm 1 object đang dùng
        Debug.Log($"[ObjectPool] Max pool size ({maxPoolSize}) reached. Reusing an object.");
        var fallbackObj = pool.Count > 0 ? pool.Dequeue() : null;
        if (fallbackObj == null)
        {
            // Nếu chẳng còn object nào trong pool (cực hiếm)
            fallbackObj = CreateNewInstance();
        }

        fallbackObj.SetActive(true);
        return fallbackObj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;

        // Nếu pool còn chỗ → tái sử dụng
        if (pool.Count < maxPoolSize)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        else
        {
            // Nếu pool đầy và obj ko được dùng đến→ hủy object
            Destroy(obj);
            totalCreated--;
        }
    }

    private GameObject CreateNewInstance()
    {
        var newObj = Instantiate(prefab, transform);
        newObj.transform.localScale = newObj.transform.localScale * 4.5f;
        newObj.name = prefab.name + "_Pooled";
        totalCreated++;
        return newObj;
    }

    public void RemovePool()
    {
        int count = pool.Count;

        // Xoá object trong pool
        while (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            if (obj != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(obj);
                else Destroy(obj);
#else
            Destroy(obj);
#endif
            }
        }

#if UNITY_EDITOR
        // Xoá tất cả child nếu đang ở edit mode
        if (!Application.isPlaying)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);
        }
#endif

        Debug.Log($"[ObjectPool] Cleared {count} pooled objects. Remaining: {pool.Count}");
    }

}
