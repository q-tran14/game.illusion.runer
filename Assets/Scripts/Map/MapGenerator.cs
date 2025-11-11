using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

public class MapGenerator : Singleton<MapGenerator>
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject playerPrefab; // Prefab của player
    [SerializeField] private Transform mapRoot;       // Parent cho các cube đang active
    [SerializeField] private float unitSize = 2f; // (legacy) không còn dùng để tính world pos
    [Header("Cube Spacing Settings")]
    [SerializeField] private float cubeSize = 4.5f; // kích thước cạnh mỗi cube (fallback)
    [SerializeField] private float gap = 0.2f;      // khoảng cách giữa hai cube kế tiếp (fallback)
    [SerializeField] private int initialCubes = 15;
    [SerializeField] private int maxCubes = 20;
    [SerializeField] private float distPlayerAndLastCube = 80f; // Khoảng cách kích hoạt spawn cube mới
    private readonly Dictionary<Vector3, PathSegment> cubeMap = new();
    private readonly List<PathSegment> activeCubes = new();

    private Vector3 currentGrid = Vector3.zero;
    private PathSegment.TurnDir currentDir = PathSegment.TurnDir.Straight;

    // Spacing đo từ bounds của model (khởi tạo lazy lần đầu spawn)
    private bool spacingInitialized = false;
    private float spacingX;
    private float spacingZ;
    private Vector3 placementOffset = Vector3.zero; // dịch pivot để tâm mesh nằm đúng grid

    [Header("Boot" )]
    [SerializeField] private bool autoSpawnOnStart = false; // Dành cho chạy độc lập không qua GameManager

    private void Start()
    {
        // Tạo mapRoot nếu chưa có
        if (mapRoot == null)
        {
            var go = new GameObject("MapRoot");
            mapRoot = go.transform;
        }

        if (autoSpawnOnStart)
            SpawnMap();
    }

    public void SpawnMap()
    {
        for (int i = 0; i < initialCubes; i++) SpawnNextCube();
        
        // Spawn player ở giữa trên cube đầu tiên
        if (activeCubes.Count > 0)
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        // Nếu player chưa tồn tại, tạo mới
        if (player == null && playerPrefab != null)
        {
            GameObject playerObj = Instantiate(playerPrefab);
            player = playerObj.transform;
        }

        if (player != null && activeCubes.Count > 0)
        {
            // Lấy cube đầu tiên
            PathSegment firstCube = activeCubes[0];
            
            // Tính top Y của cube đầu tiên
            var cubeCol = firstCube.GetComponent<Collider>();
            float cubeTopY = /*cubeCol != null ? cubeCol.bounds.max.y : */firstCube.transform.position.y /*+ cubeSize * 0.5f*/;

            // Chiều cao player
            var playerCol = player.GetComponent<Collider>();

            // Vị trí spawn: chính giữa mặt trên cube
            Vector3 spawnPos = new Vector3(
                firstCube.transform.position.x,
                Mathf.Abs(firstCube.transform.position.y),
                firstCube.transform.position.z
            );
            
            // Initialize player
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.Initialize(spawnPos, firstCube);
            }
            else
            {
                player.position = spawnPos;
            }
        }
    }

    public void ClearMap()
    {
        // Trả các cube đang active về pool
        for (int i = 0; i < activeCubes.Count; i++)
        {
            var seg = activeCubes[i];
            if (seg != null)
            {
                ObjectPool.Instance.Return(seg.gameObject);
            }
        }

        activeCubes.Clear();
        cubeMap.Clear();
        currentGrid = Vector3.zero;
        currentDir = PathSegment.TurnDir.Straight;
        spacingInitialized = false; // đo lại sau khi clear

        // Chỉ dọn pool nếu bạn muốn thực sự hủy các instance nhàn rỗi
        ObjectPool.Instance.RemovePool();
    }

    private void Update()
    {
        // Spawn thêm khi player gần cuối đường
        float dist = Vector3.Distance(player.position, activeCubes[^1].transform.position);
        if (dist < distPlayerAndLastCube) SpawnNextCube();

        // Trả những cube phía sau player về pool khi đã cách 1-2 ô
        TrimBehindCubes(keepBehind: 4);

        // Giới hạn số lượng cube trong scene
        if (activeCubes.Count > maxCubes)
        {
            var old = activeCubes[0];
            activeCubes.RemoveAt(0);
            cubeMap.Remove(old.gridPos);
            ObjectPool.Instance.Return(old.gameObject);
        }
    }

    // Giữ lại một số ô phía sau player, còn lại trả về pool
    private void TrimBehindCubes(int keepBehind)
    {
        if (player == null || activeCubes.Count == 0) return;

        // Tìm cube gần player nhất theo khoảng cách world
        int nearestIndex = -1;
        float nearestSqr = float.PositiveInfinity;
        Vector3 p = player.position;
        for (int i = 0; i < activeCubes.Count; i++)
        {
            var seg = activeCubes[i];
            if (seg == null) continue;
            float d = (seg.transform.position - p).sqrMagnitude;
            if (d < nearestSqr)
            {
                nearestSqr = d;
                nearestIndex = i;
            }
        }

        if (nearestIndex <= 0) return;

        int removeCount = Mathf.Max(0, nearestIndex - keepBehind);
        if (removeCount == 0) return;

        for (int i = 0; i < removeCount; i++)
        {
            var seg = activeCubes[0];
            activeCubes.RemoveAt(0);
            if (seg != null)
            {
                cubeMap.Remove(seg.gridPos);
                ObjectPool.Instance.Return(seg.gameObject);
            }
        }
    }

    private void SpawnNextCube()
    {
        var prefabObj = ObjectPool.Instance.Get();
        if (prefabObj == null)
        {
            // Nếu pool đã hết, tạm thời tăng thêm bằng cách yêu cầu pool tạo thêm (tùy chọn) hoặc bỏ qua spawn frame này
            Debug.LogWarning("[MapGenerator] Pool exhausted. Skipping spawn this frame.");
            return;
        }

        var seg = prefabObj.GetComponent<PathSegment>();
        if (seg == null) seg = prefabObj.AddComponent<PathSegment>();

        // ✅ Với cube đầu tiên, luôn đặt tại (0,0,0)
        PathSegment.TurnDir nextDir; //= currentDir == PathSegment.TurnDir.Straight ? PathSegment.TurnDir.Straight : GetNextValidDirection();
        if (activeCubes.Count <= Random.Range(3,5))
        {
            nextDir = PathSegment.TurnDir.Straight;
        } else nextDir = GetNextValidDirection();
        Vector3 nextGrid = (activeCubes.Count == 0) ? Vector3.zero : GetNextGrid(currentGrid, nextDir);

        // Nếu trùng cube → bỏ qua
        if (cubeMap.ContainsKey(nextGrid))
        {
            ObjectPool.Instance.Return(prefabObj);
            return;
        }

        seg.Init(nextGrid, nextDir, PathSegment.FaceType.Top);

        // Khởi tạo spacing nếu chưa có (đo từ Renderer/Collider bounds)
        if (!spacingInitialized)
        {
            InitializeSpacing(prefabObj);
        }

        // Tính center mong muốn của cube theo grid (luôn nằm trên mặt phẳng Y=0)
        Vector3 desiredCenter = new Vector3(
            seg.gridPos.x * spacingX,
            0f,
            seg.gridPos.z * spacingZ
        );

        // Đưa mesh (pivot lệch) vào đúng center bằng cách worldPos = desiredCenter - placementOffset
        Vector3 worldPos = desiredCenter - placementOffset;
        prefabObj.transform.position = worldPos;
        
        prefabObj.transform.rotation = Quaternion.identity;

        cubeMap[nextGrid] = seg;
        activeCubes.Add(seg);

        currentDir = nextDir;
        currentGrid = nextGrid;

        prefabObj.SetActive(true);
    }

    // Đo kích thước thực tế của model để tính spacing (center-to-center)
    private void InitializeSpacing(GameObject sample)
    {
        Bounds bounds;
        bool hasBounds = false;

        var renderers = sample.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            hasBounds = true;
        }
        else
        {
            var colliders = sample.GetComponentsInChildren<Collider>();
            if (colliders != null && colliders.Length > 0)
            {
                bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);
                hasBounds = true;
            }
            else
            {
                bounds = new Bounds(sample.transform.position, new Vector3(cubeSize, cubeSize, cubeSize));
            }
        }

    float fallback = cubeSize + gap;
    spacingX = hasBounds ? bounds.size.x + gap : fallback;
    spacingZ = hasBounds ? bounds.size.z + gap : fallback;

    // Tính offset để đưa tâm mesh đúng tại tọa độ grid (pivot có thể không ở giữa)
    Vector3 center = hasBounds ? bounds.center : sample.transform.position;
    Vector3 pivot = sample.transform.position;
    // Để khi đặt worldPos = -placementOffset, mesh center đặt tại (0,0,0)
    placementOffset = center - pivot;
        spacingInitialized = true;
        // Debug.Log($"[MapGenerator] spacingX={spacingX:F2}, spacingZ={spacingZ:F2}");
    }

    private PathSegment.TurnDir GetNextValidDirection()
    {
        // Danh sách tất cả hướng có thể
        var allDirections = new List<PathSegment.TurnDir>()
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
        if (currentDir != PathSegment.TurnDir.Straight)
        {
            PathSegment.TurnDir opposite = GetOpposite(currentDir);
            allDirections.Remove(opposite);
        }

        // Chọn random trong danh sách hợp lệ (tỉ lệ đều nhau)
        int randomIndex = Random.Range(0, allDirections.Count);
        return allDirections[randomIndex];
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
        // ✅ SỬA: Đồng bộ với hướng player (Z = forward, X = left/right)
        switch (dir)
        {
            case PathSegment.TurnDir.Straight:   return pos + new Vector3(0, 0, 1);   // Thẳng về phía trước (Z+)
            case PathSegment.TurnDir.Left:       return pos + new Vector3(-1, 0, 0);  // Rẽ trái (X-)
            case PathSegment.TurnDir.Right:      return pos + new Vector3(1, 0, 0);   // Rẽ phải (X+)
            case PathSegment.TurnDir.UpLeft:     return pos + new Vector3(-1, 0, 1);  // Trái + Thẳng (X- Z+)
            case PathSegment.TurnDir.DownLeft:   return pos + new Vector3(-1, 0, -1); // Trái + Lùi (X- Z-)
            case PathSegment.TurnDir.UpRight:    return pos + new Vector3(1, 0, 1);   // Phải + Thẳng (X+ Z+)
            case PathSegment.TurnDir.DownRight:  return pos + new Vector3(1, 0, -1);  // Phải + Lùi (X+ Z-)
            default: return pos + new Vector3(0, 0, 1);
        }
    }
}
