using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Threading.Tasks;

[CreateAssetMenu(fileName = "DecorLibrary", menuName = "Runner/Decor Library", order = 10)]
public class DecorLibrary : ScriptableObject
{
    [Tooltip("Danh sách AssetReference cho prefab .obj/.fbx (Addressables).")]
    public List<AssetReferenceGameObject> items = new List<AssetReferenceGameObject>();

    public int Count => items != null ? items.Count : 0;

    // Async load specific index
    public async Task<GameObject> GetAsync(int index)
    {
        if (items == null || items.Count == 0) return null;
        if (index < 0 || index >= items.Count) return null;
        
        var assetRef = items[index];
        if (assetRef == null || !assetRef.RuntimeKeyIsValid()) return null;

        var handle = assetRef.LoadAssetAsync<GameObject>();
        await handle.Task;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
            return handle.Result;
        
        Debug.LogWarning($"[DecorLibrary] Failed to load asset at index {index}");
        return null;
    }

    // Async load random
    public async Task<GameObject> GetRandomAsync()
    {
        if (items == null || items.Count == 0) return null;
        int idx = Random.Range(0, items.Count);
        return await GetAsync(idx);
    }

    // Release asset when done (call when returning to pool)
    public void Release(int index)
    {
        if (items == null || index < 0 || index >= items.Count) return;
        var assetRef = items[index];
        if (assetRef != null && assetRef.IsValid())
        {
            assetRef.ReleaseAsset();
        }
    }
}
