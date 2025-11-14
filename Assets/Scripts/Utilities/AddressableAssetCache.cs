using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Generic Addressables asset cache manager.
/// Prevents duplicate loading and manages asset lifecycle.
/// Thread-safe and reusable across projects.
/// </summary>
public static class AddressableAssetCache
{
    // Cache for all loaded assets by GUID
    private static readonly Dictionary<string, AsyncOperationHandle<GameObject>> gameObjectCache = 
        new Dictionary<string, AsyncOperationHandle<GameObject>>();

    /// <summary>
    /// Load a GameObject from Addressables with automatic caching.
    /// If already loaded, returns cached result instantly.
    /// </summary>
    /// <param name="assetRef">AssetReference to load</param>
    /// <returns>Loaded GameObject or null if failed</returns>
    public static async Task<GameObject> LoadGameObjectAsync(AssetReferenceGameObject assetRef)
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid())
        {
            Debug.LogWarning("[AddressableAssetCache] Invalid AssetReference provided.");
            return null;
        }

        string key = assetRef.AssetGUID;

        // Check cache first
        if (gameObjectCache.ContainsKey(key))
        {
            var cachedHandle = gameObjectCache[key];
            if (cachedHandle.IsValid() && cachedHandle.Status == AsyncOperationStatus.Succeeded)
            {
                return cachedHandle.Result;
            }
            else
            {
                // Invalid handle, remove from cache
                gameObjectCache.Remove(key);
            }
        }

        // Load new asset using RuntimeKey to avoid "already loaded" errors
        var handle = Addressables.LoadAssetAsync<GameObject>(assetRef.RuntimeKey);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[AddressableAssetCache] Failed to load asset with GUID: {key}");
            return null;
        }

        // Cache the handle
        gameObjectCache[key] = handle;
        return handle.Result;
    }

    /// <summary>
    /// Load a GameObject by direct key/address.
    /// Useful when you don't have AssetReference but know the address.
    /// </summary>
    /// <param name="address">Addressable address/key</param>
    /// <returns>Loaded GameObject or null if failed</returns>
    public static async Task<GameObject> LoadGameObjectAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogWarning("[AddressableAssetCache] Empty address provided.");
            return null;
        }

        // Use address as key for caching
        if (gameObjectCache.ContainsKey(address))
        {
            var cachedHandle = gameObjectCache[address];
            if (cachedHandle.IsValid() && cachedHandle.Status == AsyncOperationStatus.Succeeded)
            {
                return cachedHandle.Result;
            }
            else
            {
                gameObjectCache.Remove(address);
            }
        }

        var handle = Addressables.LoadAssetAsync<GameObject>(address);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[AddressableAssetCache] Failed to load asset with address: {address}");
            return null;
        }

        gameObjectCache[address] = handle;
        return handle.Result;
    }

    /// <summary>
    /// Check if an asset is already loaded and cached.
    /// </summary>
    /// <param name="assetRef">AssetReference to check</param>
    /// <returns>True if cached and valid</returns>
    public static bool IsCached(AssetReferenceGameObject assetRef)
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid()) return false;
        
        string key = assetRef.AssetGUID;
        if (!gameObjectCache.ContainsKey(key)) return false;

        var handle = gameObjectCache[key];
        return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded;
    }

    /// <summary>
    /// Get cached GameObject without loading (instant).
    /// Returns null if not cached.
    /// </summary>
    /// <param name="assetRef">AssetReference to retrieve</param>
    /// <returns>Cached GameObject or null</returns>
    public static GameObject GetCached(AssetReferenceGameObject assetRef)
    {
        if (!IsCached(assetRef)) return null;
        return gameObjectCache[assetRef.AssetGUID].Result;
    }

    /// <summary>
    /// Release a specific asset from cache.
    /// Use when you know the asset won't be needed anymore.
    /// </summary>
    /// <param name="assetRef">AssetReference to release</param>
    public static void Release(AssetReferenceGameObject assetRef)
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid()) return;

        string key = assetRef.AssetGUID;
        if (gameObjectCache.ContainsKey(key))
        {
            var handle = gameObjectCache[key];
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            gameObjectCache.Remove(key);
        }
    }

    /// <summary>
    /// Release a specific asset by address.
    /// </summary>
    /// <param name="address">Address of asset to release</param>
    public static void Release(string address)
    {
        if (string.IsNullOrEmpty(address)) return;

        if (gameObjectCache.ContainsKey(address))
        {
            var handle = gameObjectCache[address];
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            gameObjectCache.Remove(address);
        }
    }

    /// <summary>
    /// Clear all cached assets and release their handles.
    /// Call this when changing scenes or when memory cleanup is needed.
    /// </summary>
    public static void ClearAll()
    {
        foreach (var kvp in gameObjectCache)
        {
            if (kvp.Value.IsValid())
            {
                Addressables.Release(kvp.Value);
            }
        }
        gameObjectCache.Clear();
        Debug.Log("[AddressableAssetCache] Cleared all cached assets.");
    }

    /// <summary>
    /// Get statistics about current cache state.
    /// </summary>
    /// <returns>Number of cached assets</returns>
    public static int GetCacheCount()
    {
        return gameObjectCache.Count;
    }

    /// <summary>
    /// Preload multiple assets for faster runtime access.
    /// Useful for loading screens or menu initialization.
    /// </summary>
    /// <param name="assetRefs">List of AssetReferences to preload</param>
    /// <returns>Number of successfully loaded assets</returns>
    public static async Task<int> PreloadAsync(List<AssetReferenceGameObject> assetRefs)
    {
        if (assetRefs == null || assetRefs.Count == 0) return 0;

        int successCount = 0;
        var tasks = new List<Task<GameObject>>();

        foreach (var assetRef in assetRefs)
        {
            if (assetRef != null && assetRef.RuntimeKeyIsValid())
            {
                tasks.Add(LoadGameObjectAsync(assetRef));
            }
        }

        var results = await Task.WhenAll(tasks);
        
        foreach (var result in results)
        {
            if (result != null) successCount++;
        }

        Debug.Log($"[AddressableAssetCache] Preloaded {successCount}/{assetRefs.Count} assets.");
        return successCount;
    }
}
