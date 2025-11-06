using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : Singleton<MapGenerator>
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private float unitSize = 2f;
    [SerializeField] private int initialCubes = 15;
    [SerializeField] private int maxCubes = 20;

    private readonly Dictionary<Vector3, PathSegment> cubeMap = new();
    private readonly List<PathSegment> activeCubes = new();

    private Vector3 currentGrid = Vector3.zero;
    private PathSegment.TurnDir currentDir = PathSegment.TurnDir.Straight;

    void Start()
    {
        // Spawn chuỗi cube ban đầu
        SpawnMap();
    }

    public void SpawnMap()
    {
        for (int i = 0; i < initialCubes; i++) SpawnNextCube();
    }

    public void ClearMap()
    {
        ObjectPool.Instance.RemovePool();
        cubeMap.Clear();
        currentGrid = Vector3.zero;
    }

    void Update()
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

    void SpawnNextCube()
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

    PathSegment.TurnDir GetNextValidDirection()
    {
        var all = new List<PathSegment.TurnDir>()
        {
            PathSegment.TurnDir.Left,
            PathSegment.TurnDir.Right,
            PathSegment.TurnDir.UpLeft,
            PathSegment.TurnDir.DownLeft,
            PathSegment.TurnDir.UpRight,
            PathSegment.TurnDir.DownRight,
            PathSegment.TurnDir.Straight
        };

        // Bỏ hướng ngược lại để tránh đảo chiều
        PathSegment.TurnDir opposite = GetOpposite(currentDir);
        all.Remove(opposite);

        // Chọn random trong danh sách hợp lệ
        int r = Random.Range(0, all.Count);
        return all[r];
    }

    PathSegment.TurnDir GetOpposite(PathSegment.TurnDir dir)
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

    Vector3 GetNextGrid(Vector3 pos, PathSegment.TurnDir dir)
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
