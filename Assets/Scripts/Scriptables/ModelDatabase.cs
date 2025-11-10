using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModelDatabase", menuName = "Game/Model Database")]
public class ModelDatabase : ScriptableObject
{
    [System.Serializable]
    public class ModelEntry
    {
        public string modelName;
        public GameObject prefab; // prefab gốc có child "Model"
    }

    [Header("List of available models", order = 0)]
    [Header("List of characters:",order = 1)]
    public List<ModelEntry> characters = new List<ModelEntry>();
    [Header("List of evironments:", order = 0)]
    public List<ModelEntry> environments = new List<ModelEntry>();
    [Header("List of collectibles:",order = 0)]
    public List<ModelEntry> collectibles = new List<ModelEntry>();

    private Dictionary<string, GameObject> modelLookup;

    private void OnEnable()
    {
        modelLookup = new Dictionary<string, GameObject>();
        foreach (var entry in characters)
        {
            if (entry != null && entry.prefab != null && !modelLookup.ContainsKey(entry.modelName)) modelLookup.Add(entry.modelName, entry.prefab);
        }
        foreach (var entry in environments)
        {
            if (entry != null && entry.prefab != null && !modelLookup.ContainsKey(entry.modelName)) modelLookup.Add(entry.modelName, entry.prefab);
        }
        foreach (var entry in collectibles)
        {
            if (entry != null && entry.prefab != null && !modelLookup.ContainsKey(entry.modelName)) modelLookup.Add(entry.modelName, entry.prefab);
        }
    }

    /// <summary>
    /// Lấy prefab gốc
    /// </summary>
    public GameObject GetModelRoot(string name)
    {
        if (modelLookup == null || modelLookup.Count == 0)
            OnEnable();

        if (modelLookup.TryGetValue(name, out GameObject prefab))
            return prefab;

        Debug.LogWarning($"[ModelDatabase] Không tìm thấy model '{name}'");
        return null;
    }

    /// <summary>
    /// Lấy transform của child "Model" trong prefab instance
    /// </summary>
    public Transform GetInnerModel(GameObject instance)
    {
        if (instance == null) return null;

        var child = instance.transform.Find("Model");
        if (child != null)
            return child;

        Debug.LogWarning($"[ModelDatabase] Prefab '{instance.name}' không có child 'Model'");
        return instance.transform; // fallback: trả root nếu không có child
    }
}
