using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

public class MapGenerator : Singleton<MapGenerator>
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private float unitSize = 2f;
    [SerializeField] private int initialCubes = 15;
    [SerializeField] private int maxCubes = 20;
    private int dirCounter = 0;
    private readonly Dictionary<Vector3, PathSegment> cubeMap = new();
    private readonly List<PathSegment> activeCubes = new();

    private Vector3 currentGrid = Vector3.zero;
    private PathSegment.TurnDir currentDir = PathSegment.TurnDir.Straight;

    private void Start() => SpawnMap();

    public void SpawnMap()
    {
        dirCounter = 0;
        for (int i = 0; i < initialCubes; i++) SpawnNextCube();
    }

    public void ClearMap()
    {
        ObjectPool.Instance.RemovePool();
        cubeMap.Clear();
        currentGrid = Vector3.zero;
    }

    private void Update()
    {
        // Spawn thêm khi player gần cuối đường
        float dist = Vector3.Distance(player.position, activeCubes[^1].transform.position);
        if (dist < 10f) SpawnNextCube();

        // Giới hạn số lượng cube trong scene
        if (activeCubes.Count > maxCubes)
        {
            var old = activeCubes[0];
            activeCubes.RemoveAt(0);
            cubeMap.Remove(old.gridPos);
            ObjectPool.Instance.Return(old.gameObject);
        }
    }

    private void SpawnNextCube()
    {
        var prefabObj = ObjectPool.Instance.Get();

        var seg = prefabObj.GetComponent<PathSegment>();
        if (seg == null) seg = prefabObj.AddComponent<PathSegment>();

        // ✅ Với cube đầu tiên, luôn đặt tại (0,0,0)
        PathSegment.TurnDir nextDir = currentDir == PathSegment.TurnDir.Straight ? PathSegment.TurnDir.Straight : GetNextValidDirection();

        Vector3 nextGrid = (activeCubes.Count == 0) ? Vector3.zero : GetNextGrid(currentGrid, nextDir);

        // Nếu trùng cube → bỏ qua
        if (cubeMap.ContainsKey(nextGrid))
        {
            ObjectPool.Instance.Return(prefabObj);
            return;
        }

        seg.Init(nextGrid, nextDir, PathSegment.FaceType.Top);

        prefabObj.transform.position = seg.GetWorldPos(unitSize);
        prefabObj.transform.rotation = Quaternion.identity;

        cubeMap[nextGrid] = seg;
        activeCubes.Add(seg);

        currentDir = nextDir;
        currentGrid = nextGrid;

        prefabObj.SetActive(true);
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
