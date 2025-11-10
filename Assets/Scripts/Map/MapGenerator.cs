using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using SampleCosmoRun;

public class MapGenerator : Singleton<MapGenerator>
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private float unitSize = 2f;
    [SerializeField] private int initialCubes = 15;
    [SerializeField] private int maxCubes = 20;

    [Header("Cube Prefab Settings")]
    [Tooltip("Prefab bounds center from mesh, typically (0,1,0) for bottom-pivot cube")]
    [SerializeField] private Vector3 prefabBoundsCenter = new(0f, 1f, 0f);
    [Tooltip("Prefab bounds size from mesh, typically (2,2,2) for a 2x2x2 cube")]
    [SerializeField] private Vector3 prefabBoundsSize = new(2f, 2f, 2f);
    [Tooltip("Gap (world units) between adjacent cubes")]
    [SerializeField] private float cubeGap = 0.2f;

    private int dirCounter = 0;
    private readonly Dictionary<Vector3, PathSegment> cubeMap = new();
    private readonly List<PathSegment> activeCubes = new();
    
    // Track cube positions using SampleCubeGroup
    [SerializeField] private SampleCosmoRun.SampleCubeGroup cubeGroup;

    private Vector3 currentGrid = Vector3.zero;
    private PathSegment.TurnDir currentDir = PathSegment.TurnDir.Straight;

    private void Start()
    {
        if (cubeGroup == null)
            cubeGroup = gameObject.AddComponent<SampleCosmoRun.SampleCubeGroup>();

        cubeGroup.PrefabBoundsCenter = prefabBoundsCenter;
        cubeGroup.PrefabBoundsSize = prefabBoundsSize;

        SpawnMap();
    }

    public void SpawnMap()
    {
        ClearMap();
        dirCounter = 0;
        for (int i = 0; i < initialCubes; i++) SpawnNextCube();
    }

    public void ClearMap()
    {
        ObjectPool.Instance.RemovePool();
        cubeMap.Clear();
        currentGrid = Vector3.zero;
        if (cubeGroup != null)
        {
            cubeGroup.Clear();
        }
    }

    private void Update()
    {
        if (activeCubes.Count == 0) return;

        // Spawn more when player is near the end
        float dist = Vector3.Distance(player.position, activeCubes[^1].transform.position);
        if (dist < 10f)
        {
            SpawnNextCube();
        }

        // Limit cube count in scene
        if (activeCubes.Count > maxCubes)
        {
            var old = activeCubes[0];
            activeCubes.RemoveAt(0);
            cubeMap.Remove(old.gridPos);
            ObjectPool.Instance.Return(old.gameObject);
            // Remove from cube group too
            if (cubeGroup != null) cubeGroup.RemoveTailFace();
        }
    }

    private bool SpawnNextCube()
    {
        // Create a new cube at next grid position
        Vector3 nextGrid = (activeCubes.Count == 0) ? Vector3.zero : GetNextGrid(currentGrid, GetNextValidDirection());

        // Skip if position already occupied
        if (cubeMap.ContainsKey(nextGrid)) return false;

        var prefabObj = ObjectPool.Instance.Get();
        var seg = prefabObj.GetComponent<PathSegment>();
        if (seg == null) seg = prefabObj.AddComponent<PathSegment>();


        // Initialize segment (using straight path for now, can be enhanced with SampleCubeFace.Direction)
        seg.Init(nextGrid, PathSegment.TurnDir.Straight, PathSegment.FaceType.Top);

        // Position using grid-based world position, add small gap between cubes but do NOT change prefab scale
        float spacing = prefabBoundsSize.y + cubeGap;
        prefabObj.transform.position = new Vector3(nextGrid.x * spacing, nextGrid.y * spacing, nextGrid.z * spacing);
        prefabObj.transform.rotation = Quaternion.identity;

        cubeMap[nextGrid] = seg;
        activeCubes.Add(seg);
        currentGrid = nextGrid;

        prefabObj.SetActive(true);
        return true;
    }

    private PathSegment.TurnDir GetNextValidDirection()
    {
        var all = new Dictionary<PathSegment.TurnDir, float>()
        {
            {PathSegment.TurnDir.Left, 20f},
            {PathSegment.TurnDir.Right, 20f},
            {PathSegment.TurnDir.UpLeft, 5f},
            {PathSegment.TurnDir.DownLeft, 5f},
            {PathSegment.TurnDir.UpRight, 5f},
            {PathSegment.TurnDir.DownRight, 5f},
            {PathSegment.TurnDir.Straight, 40f}
        };

        // Bỏ hướng ngược lại để tránh đảo chiều
        if (currentDir != PathSegment.TurnDir.Straight)
        {
            PathSegment.TurnDir opposite = GetOpposite(currentDir);
            all.Remove(opposite);
        }

        // Chọn random trong danh sách hợp lệ
        PathSegment.TurnDir dir = ChooseDir(all);

        return dir;
    }

    private PathSegment.TurnDir ChooseDir(Dictionary<PathSegment.TurnDir, float> validDirs)
    {
        // 7 trường hợp: Straight tỉ lệ cao nhất ~ giữ hướng di chuyển không đổi
        // 6 trường hợp còn lại tỉ lệ bằng nhau
        float totalWeight = 0f;
        foreach (var kv in validDirs)
            totalWeight += kv.Value;

        // Thay vì random, dùng chia lấy dư
        float value = dirCounter % totalWeight;
        dirCounter++;

        float cumulative = 0f;
        foreach (var kv in validDirs)
        {
            cumulative += kv.Value;
            if (value < cumulative)
                return kv.Key;
        }

        return validDirs.Keys.First();
    }

    private PathSegment.TurnDir GetOpposite(PathSegment.TurnDir dir)
    {
        switch (dir)
        {
            case PathSegment.TurnDir.Left: return PathSegment.TurnDir.Right;
            case PathSegment.TurnDir.Right: return PathSegment.TurnDir.Left;
            case PathSegment.TurnDir.UpLeft: return PathSegment.TurnDir.DownRight;
            case PathSegment.TurnDir.DownLeft: return PathSegment.TurnDir.UpRight;
            case PathSegment.TurnDir.UpRight: return PathSegment.TurnDir.DownLeft;
            case PathSegment.TurnDir.DownRight: return PathSegment.TurnDir.UpLeft;
            default: return PathSegment.TurnDir.Straight;
        }
    }

    private Vector3 GetNextGrid(Vector3 pos, PathSegment.TurnDir dir)
    {
        switch (dir)
        {
            case PathSegment.TurnDir.Left: return pos + new Vector3(-1, 0, 0);
            case PathSegment.TurnDir.Right: return pos + new Vector3(1, 0, 0);
            case PathSegment.TurnDir.UpLeft: return pos + new Vector3(-1, 1, 0);
            case PathSegment.TurnDir.DownLeft: return pos + new Vector3(-1, -1, 0);
            case PathSegment.TurnDir.UpRight: return pos + new Vector3(1, 1, 0);
            case PathSegment.TurnDir.DownRight: return pos + new Vector3(1, -1, 0);
            default: return pos + new Vector3(0, 0, 1);
        }
    }
}
