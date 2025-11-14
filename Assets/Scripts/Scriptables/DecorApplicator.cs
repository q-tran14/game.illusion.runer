using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

[DisallowMultipleComponent]
public class DecorApplicator : MonoBehaviour
{
    [Tooltip("Nơi attach decor. Nếu rỗng, sẽ tự tạo child tên 'DecorRoot'.")]
    public Transform anchor;

    [Tooltip("Đặt lại local transform về (0,0,0),(0,0,0),(1,1,1) khi gắn decor.")]
    public bool resetLocalTransform = true;

    private GameObject currentDecorInstance;

    void Awake()
    {
        EnsureAnchor();
    }

    void OnDisable()
    {
        // Khi trả về pool, dọn decor (không release asset - vì đang cache)
        Clear();
    }

    public void Clear()
    {
        EnsureAnchor();
        
        // Destroy instances
        for (int i = anchor.childCount - 1; i >= 0; i--)
        {
            var child = anchor.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child.gameObject);
            else Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
        
        currentDecorInstance = null;
    }

    // Sync version (backward compatible)
    public void Apply(GameObject decorPrefab)
    {
        if (decorPrefab == null) return;
        EnsureAnchor();
        Clear();
        var inst = Instantiate(decorPrefab, anchor, false);
        if (resetLocalTransform)
        {
            var t = inst.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
        
        currentDecorInstance = inst;
    }

    // Async version with AssetReference
    public async Task ApplyAsync(AssetReferenceGameObject assetRef)
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid()) return;
        
        EnsureAnchor();
        Clear();

        // Use centralized cache
        var prefab = await AddressableAssetCache.LoadGameObjectAsync(assetRef);
        
        if (prefab == null)
        {
            Debug.LogWarning($"[DecorApplicator] Failed to load asset.");
            return;
        }

        InstantiateDecor(prefab);
    }

    private void InstantiateDecor(GameObject prefab)
    {
        var inst = Instantiate(prefab, anchor, false);
        
        if (resetLocalTransform)
        {
            var t = inst.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        currentDecorInstance = inst;
    }

    private void EnsureAnchor()
    {
        if (anchor == null)
        {
            var existing = transform.Find("DecorRoot");
            if (existing != null) anchor = existing;
            else
            {
                var go = new GameObject("DecorRoot");
                anchor = go.transform;
                anchor.SetParent(transform, false);
                anchor.localPosition = Vector3.zero;
                anchor.localRotation = Quaternion.identity;
                anchor.localScale = Vector3.one;
            }
        }
    }
}
