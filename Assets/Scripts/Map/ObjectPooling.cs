using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
public class ObjectPool : Singleton<ObjectPool>
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject defaultEmptyPrefab;
    [SerializeField] private int minPoolSize = 12;
    [SerializeField] private int maxPoolSize = 15;

    [Header("Box Collider Setup (Optional)")]
    [Tooltip("If enabled, applies custom BoxCollider settings to pooled cubes.")]
    [SerializeField] private bool configureCollider = false;
    [Tooltip("Override collider.center when configureCollider is enabled.")]
    [SerializeField] private bool overrideColliderCenter = true;
    [SerializeField] private Vector3 colliderCenter = new Vector3(0f, 1f, 0f);
    [Tooltip("Override collider.size when configureCollider is enabled.")]
    [SerializeField] private bool overrideColliderSize = true;
    [SerializeField] private Vector3 colliderSize = new Vector3(2.2f, 2f, 2.2f);

    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private int totalCreated = 0;
    public int TotalCreated => totalCreated;
    public int AvailableCount => pool.Count;

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

    public async Task<GameObject> GetAsync()
    {
        GameObject obj = null;
        
        // Nếu có sẵn trong pool → dùng lại
        if (pool.Count > 0) obj = pool.Dequeue();
        // Nếu chưa tới giới hạn → tạo mới
        else if (totalCreated < maxPoolSize) obj = CreateNewInstance();
        else
        {
            // Đạt giới hạn → không tạo mới, trả null
            Debug.LogWarning($"[ObjectPool] Max pool size ({maxPoolSize}) reached. Consider increasing maxPoolSize.");
            return null;
        }
        
        // Apply decor async ngay trước khi trả về
        await ApplyDecorToObjectAsync(obj);
        obj.SetActive(true);
        return obj;
    }
    
    private async Task ApplyDecorToObjectAsync(GameObject obj)
    {
        if (obj == null) return;
        
        var applicator = obj.GetComponent<DecorApplicator>();
        if (applicator != null)
        {
            // Lấy decor từ MapGenerator environment
            var mapGen = MapGenerator.Instance;
            if (mapGen != null)
            {
                var decorAssetRef = mapGen.GetPathDecorAssetRef();
                if (decorAssetRef != null && decorAssetRef.RuntimeKeyIsValid()) await applicator.ApplyAsync(decorAssetRef);
            }
        }
    }


    public void Return(GameObject obj)
    {
        if (obj == null) return;

        // Nếu pool còn chỗ → tái sử dụng
        if (pool.Count < maxPoolSize)
        {
            obj.SetActive(false);
            // Đưa object về làm con của pool để gọn Hierarchy
            obj.transform.SetParent(transform, false);
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
        var newObj = Instantiate(defaultEmptyPrefab, transform);
        newObj.transform.localScale = new Vector3(4.5f, 4.5f, 4.5f);
        newObj.name = defaultEmptyPrefab.name + "_Pooled";
        
        // Tắt renderer của base prefab để tránh đè lên decor
        var renderers = newObj.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers) renderer.enabled = false;
        
        // Thêm PathSegment nếu chưa có
        if (newObj.GetComponent<PathSegment>() == null) newObj.AddComponent<PathSegment>();
        
        // Thêm DecorApplicator nếu chưa có (để apply decor sau này)
        if (newObj.GetComponent<DecorApplicator>() == null) newObj.AddComponent<DecorApplicator>();
        
        // Thêm BoxCollider nếu chưa có (cho .obj file)
        var boxCollider = newObj.GetComponent<BoxCollider>();
        if (boxCollider == null) boxCollider = newObj.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true; // Dùng trigger để detect player

        // Tùy chọn khôi phục cấu hình collider theo yêu cầu người dùng
        if (configureCollider)
        {
            if (overrideColliderCenter) boxCollider.center = colliderCenter;
            if (overrideColliderSize) boxCollider.size = colliderSize;
        }
        
        // Tag để dễ identify
        newObj.tag = "PathCube";
        
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

        totalCreated = 0; // reset đếm sau khi hủy toàn bộ
        Debug.Log($"[ObjectPool] Cleared {count} pooled objects. Remaining: {pool.Count}");
    }

}
