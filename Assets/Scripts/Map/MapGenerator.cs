using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using System.Linq;

public class MapGenerator : Singleton<MapGenerator>
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject playerPrefab; // Prefab của player
    [Header("Cube Spacing Settings")]
    [SerializeField] private float cubeSize = 4.5f; // kích thước cạnh mỗi cube (fallback)
    [SerializeField] private float gap = 0.2f;      // khoảng cách giữa hai cube kế tiếp (fallback)
    [SerializeField] private int initialCubes = 15;
    [SerializeField] private int maxCubes = 20;
    [SerializeField] private float distPlayerAndLastCube = 80f; // Khoảng cách kích hoạt spawn cube mới
    [SerializeField] private int initialStraightCount = 4; // số cube đầu (sau cube gốc) giữ thẳng
    private readonly Dictionary<Vector3, PathSegment> cubeMap = new();
    private readonly List<PathSegment> activeCubes = new();
    private List<Vector3> dirToTurn = new List<Vector3>();
    private Vector3 currentGrid = Vector3.zero;
    private TurnDir currentDir = TurnDir.Straight;
    private TurnDir initialDir = TurnDir.Straight;

    // Spacing đo từ bounds của model (khởi tạo lazy lần đầu spawn)
    private bool spacingInitialized = false;
    private float spacingX;
    private float spacingZ;
    private Vector3 placementOffset = Vector3.zero; // dịch pivot để tâm mesh nằm đúng grid

    [Header("Environment Profiles")]
    [SerializeField] private EnvironmentProfile[] environments = new EnvironmentProfile[3];
    [SerializeField] private int currentEnvironmentIndex = 0;
    private EnvironmentProfile activeEnvironment;

    [Header("Boot")]
    [SerializeField] private bool autoSpawnOnStart = false; // Dành cho chạy độc lập không qua GameManager

    // Loading state
    private bool isLoading = false;
    private bool isMapReady = false;

    public bool IsLoading => isLoading;
    public bool IsMapReady => isMapReady;

    protected override void OnSingletonInit()
    {
        base.OnSingletonInit();
        LoadEnvironment(currentEnvironmentIndex);
    }

    private void Start()
    {
        if (autoSpawnOnStart) SpawnMap();
    }

    #region For Decor Environment
    private void LoadEnvironment(int index)
    {
        if (environments == null || environments.Length == 0)
        {
            Debug.LogWarning("[MapGenerator] No environments configured.");
            return;
        }

        index = Mathf.Clamp(index, 0, environments.Length - 1);
        currentEnvironmentIndex = index;
        activeEnvironment = environments[index];

        if (activeEnvironment == null)
        {
            Debug.LogWarning($"[MapGenerator] Environment at index {index} is null.");
            return;
        }

        Debug.Log($"[MapGenerator] Loaded environment: {activeEnvironment.environmentName} (Total path cubes: {activeEnvironment.GetTotalPathCubeCount()})");
    }

    public void SwitchEnvironment(int index)
    {
        LoadEnvironment(index);
    }

    public void NextEnvironment()
    {
        int next = (currentEnvironmentIndex + 1) % environments.Length;
        LoadEnvironment(next);
    }

    public void PreviousEnvironment()
    {
        int prev = currentEnvironmentIndex - 1;
        if (prev < 0) prev = environments.Length - 1;
        LoadEnvironment(prev);
    }

    public EnvironmentProfile GetActiveEnvironment()
    {
        return activeEnvironment;
    }

    public int GetCurrentEnvironmentIndex()
    {
        return currentEnvironmentIndex;
    }

    // Get path decor AssetReference (sync - just returns reference)
    public AssetReferenceGameObject GetPathDecorAssetRef()
    {
        if (activeEnvironment == null
            || activeEnvironment.pathCubeLibraries == null
            || activeEnvironment.pathCubeLibraries.Length == 0) return null;

        return PickFromEnvironment();
    }

    private AssetReferenceGameObject PickFromEnvironment()
    {
        var libs = activeEnvironment.pathCubeLibraries;

        switch (activeEnvironment.selectionMode)
        {
            case EnvironmentProfile.PathSelectionMode.Single:
                // Chỉ dùng 1 library cố định
                int idx = Mathf.Clamp(activeEnvironment.selectedLibraryIndex, 0, libs.Length - 1);
                if (libs[idx] != null && libs[idx].Count > 0)
                {
                    int randomIdx = Random.Range(0, libs[idx].Count);
                    return libs[idx].items[randomIdx];
                }
                break;

            case EnvironmentProfile.PathSelectionMode.Random:
                // Random chọn library, rồi random cube trong library đó
                var validLibs = new List<DecorLibrary>();
                foreach (var lib in libs)
                {
                    if (lib != null && lib.Count > 0) validLibs.Add(lib);
                }
                if (validLibs.Count > 0)
                {
                    var chosenLib = validLibs[Random.Range(0, validLibs.Count)];
                    int randomIdx = Random.Range(0, chosenLib.Count);
                    return chosenLib.items[randomIdx];
                }
                break;

            case EnvironmentProfile.PathSelectionMode.Mix:
                // Gộp tất cả thành 1 pool rồi random
                var allAssetRefs = new List<AssetReferenceGameObject>();
                foreach (var lib in libs)
                {
                    if (lib != null && lib.Count > 0)
                    {
                        allAssetRefs.AddRange(lib.items);
                    }
                }
                if (allAssetRefs.Count > 0)
                    return allAssetRefs[Random.Range(0, allAssetRefs.Count)];
                break;
        }

        return null;
    }
    #endregion
    public async void SpawnMap()
    {
        isLoading = true;
        isMapReady = false;

        Debug.Log("[MapGenerator] Starting map generation...");
        // ✅ Random hướng spawn ban đầu (Forward, Backward, Left, Right)
        TurnDir[] startDirections = new TurnDir[]
        {
            TurnDir.Straight,  // Forward
            TurnDir.Backward,
            TurnDir.Left,
            TurnDir.Right
        };

        // Random hướng đầu tiên - sẽ được dùng cho cube thứ 2
        currentDir = startDirections[Random.Range(0, startDirections.Length)];
        initialDir = currentDir;
        Debug.Log($"[MapGenerator] Starting map generation with initial direction: {currentDir}");
        for (int i = 0; i < initialCubes; i++) await SpawnNextCube();

        // Spawn player ở giữa trên cube đầu tiên
        if (activeCubes.Count > 0) await SpawnPlayer();

        isLoading = false;
        isMapReady = true;

        Debug.Log("[MapGenerator] Map generation complete! Ready to play.");
    }

    private async Task SpawnPlayer()
    {
        // Nếu player chưa tồn tại, tạo mới
        if (player == null && playerPrefab != null)
        {
            GameObject playerObj = Instantiate(playerPrefab);
            player = playerObj.transform;

            // Apply player decor async
            var applicator = playerObj.GetComponent<DecorApplicator>();
            if (applicator == null) applicator = playerObj.AddComponent<DecorApplicator>();

            var playerDecorRef = DecorManager.Instance?.GetPlayerDecorAssetRef();
            if (playerDecorRef != null && playerDecorRef.RuntimeKeyIsValid())
            {
                await applicator.ApplyAsync(playerDecorRef);
                Debug.Log($"[MapGenerator] Applied player decor: {playerDecorRef.AssetGUID}");
            }
            else
            {
                Debug.LogWarning("[MapGenerator] No player decor found! Check DecorManager settings.");
            }
        }

        if (player != null && activeCubes.Count > 0)
        {
            // Lấy cube đầu tiên
            PathSegment firstCube = activeCubes[0];

            // Tính top Y của cube đầu tiên
            float cubeTopY = Mathf.Abs(firstCube.transform.position.y);

            // Vị trí spawn: chính giữa mặt trên cube
            Vector3 spawnPos = new Vector3(
                firstCube.transform.position.x,
                cubeTopY,
                firstCube.transform.position.z
            );

            // Initialize player
            PlayerController playerController = player.GetComponent<PlayerController>();

            if (playerController != null)
            {
                // ✅ Pass initial direction to player
                playerController.Initialize(spawnPos, firstCube, initialDir);
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
        dirToTurn.Clear(); 
        currentGrid = Vector3.zero;
        currentDir = TurnDir.Straight;
        spacingInitialized = false; // đo lại sau khi clear

        // Chỉ dọn pool nếu bạn muốn thực sự hủy các instance nhàn rỗi
        ObjectPool.Instance.RemovePool();
        
        Destroy(playerPrefab);
    }

    private async void Update()
    {
        if (player == null || activeCubes.Count == 0) return;

        // Spawn thêm khi player gần cuối đường
        float dist = Vector3.Distance(player.position, activeCubes[^1].transform.position);
        if (dist < distPlayerAndLastCube) await SpawnNextCube();

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

    private async Task SpawnNextCube()
    {
        var prefabObj = await ObjectPool.Instance.GetAsync();
        if (prefabObj == null)
        {
            // Nếu pool đã hết, tạm thời tăng thêm bằng cách yêu cầu pool tạo thêm (tùy chọn) hoặc bỏ qua spawn frame này
            Debug.LogWarning("[MapGenerator] Pool exhausted. Skipping spawn this frame.");
            return;
        }

        var seg = prefabObj.GetComponent<PathSegment>();
        if (seg == null) seg = prefabObj.AddComponent<PathSegment>();

        // ✅ Với cube đầu tiên, luôn đặt tại (0,0,0)
        TurnDir nextDir;
        bool isFirst = activeCubes.Count == 0;
        bool stillInitialStraight = activeCubes.Count > 0 && activeCubes.Count < initialStraightCount;

        if (isFirst || stillInitialStraight) nextDir = currentDir;
        else nextDir = GetNextValidDirection();

        Vector3 nextGrid = isFirst ? Vector3.zero : GetNextGrid(currentGrid, nextDir);

        // Nếu trùng cube → bỏ qua
        if (cubeMap.ContainsKey(nextGrid))
        {
            ObjectPool.Instance.Return(prefabObj);
            return;
        }

        // ✅ Kiểm tra: Nếu bất kỳ neighbor nào của vị trí mới đã có 2 neighbors → không spawn
        if (!isFirst && WouldExceedNeighborLimit(nextGrid))
        {
            ObjectPool.Instance.Return(prefabObj);
            return;
        }

        // IMPORTANT: direction belongs to the PREVIOUS segment (outgoing).
        // Assign chosen direction to tail, and keep new segment temporarily Straight until next spawn.
        if (activeCubes.Count > 0)
        {
            var tail = activeCubes[^1]; // last segment
            if (tail != null) tail.direction = nextDir;     //!
            if (tail != null && tail.previous != null && tail.previous.direction != tail.direction) 
                dirToTurn.Add(tail.GetDirectionVector(tail.direction));
        }

        // New segment starts with Straight; it will receive its real direction on the next spawn cycle.
        seg.Init(nextGrid, TurnDir.Straight, FaceType.Top);
        // Link list
        if (activeCubes.Count > 0)
        {
            var prev = activeCubes[^1];
            if (prev != null) prev.next = seg;
            seg.previous = prev;
        }

        // Khởi tạo spacing nếu chưa có (đo từ Renderer/Collider bounds)
        if (!spacingInitialized) InitializeSpacing(prefabObj);

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

        currentDir = nextDir; // current chosen outgoing dir (applied to previous segment)
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

    private TurnDir GetNextValidDirection()
    {
        // Chỉ cho phép 3 hướng: Straight, Left, Right (không có diagonal)
        var allDirections = new List<TurnDir>()
        {
            TurnDir.Straight,
            TurnDir.Left,
            TurnDir.Right,
            TurnDir.Backward
        };

        // Bỏ hướng ngược lại để tránh đảo chiều
        if (currentDir != TurnDir.Straight)
        {
            TurnDir opposite = GetOpposite(currentDir);
            allDirections.Remove(opposite);
        }

        // Chọn random trong danh sách hợp lệ (tỉ lệ đều nhau)
        int randomIndex = Random.Range(0, allDirections.Count);
        return allDirections[randomIndex];
    }

    private TurnDir GetOpposite(TurnDir dir)
    {
        switch (dir)
        {
            case TurnDir.Left: return TurnDir.Right;
            case TurnDir.Right: return TurnDir.Left;
            case TurnDir.Backward: return TurnDir.Straight;
            case TurnDir.Straight: return TurnDir.Backward;
            case TurnDir.Up: return TurnDir.Down;
            case TurnDir.Down: return TurnDir.Up;
            default: return TurnDir.Straight;
        }
    }

    private Vector3 GetNextGrid(Vector3 pos, TurnDir dir)
    {
        // ✅ SỬA: Đồng bộ với hướng player (Z = forward/ backward, X = left/right)
        switch (dir)
        {
            case TurnDir.Straight: return pos + new Vector3(0, 0, 1);   //* Tiến thẳng (Z+)
            case TurnDir.Backward: return pos + new Vector3(0, 0, -1);  //* Lùi (Z-)
            case TurnDir.Left: return pos + new Vector3(-1, 0, 0);  //* Rẽ trái (X-)
            case TurnDir.Right: return pos + new Vector3(1, 0, 0);   //* Rẽ phải (X+)
            case TurnDir.Up: return pos + new Vector3(0, 1, 0);      // Lên
            case TurnDir.Down: return pos + new Vector3(0, -1, 0);    // Xuống
            default: return pos + new Vector3(0, 0, 1);
        }
    }

    // Đếm số cube neighbor kề cạnh (6 hướng chính: ±X, ±Y, ±Z)
    private int CountNeighbors(Vector3 pos)
    {
        int count = 0;
        Vector3[] directions = new Vector3[]
        {
            new Vector3(1, 0, 0),   // Right
            new Vector3(-1, 0, 0),  // Left
            new Vector3(0, 1, 0),   // Up
            new Vector3(0, -1, 0),  // Down
            new Vector3(0, 0, 1),   // Forward
            new Vector3(0, 0, -1)   // Back
        };

        foreach (var dir in directions)
        {
            if (cubeMap.ContainsKey(pos + dir))
            {
                count++;
            }
        }

        return count;
    }

    // Kiểm tra xem việc spawn cube tại vị trí này có làm bất kỳ neighbor nào vượt quá 2 neighbors không
    private bool WouldExceedNeighborLimit(Vector3 pos)
    {
        Vector3[] directions = new Vector3[]
        {
            new Vector3(1, 0, 0),   // Right
            new Vector3(-1, 0, 0),  // Left
            new Vector3(0, 1, 0),   // Up
            new Vector3(0, -1, 0),  // Down
            new Vector3(0, 0, 1),   // Forward
            new Vector3(0, 0, -1)   // Back
        };

        // Kiểm tra từng neighbor hiện tại của vị trí mới
        foreach (var dir in directions)
        {
            Vector3 neighborPos = pos + dir;

            // Nếu có cube tại neighbor này
            if (cubeMap.ContainsKey(neighborPos))
            {
                // Đếm xem cube neighbor này hiện có bao nhiêu neighbors
                int neighborCount = CountNeighbors(neighborPos);

                // Nếu nó đã có 2 neighbors, thì việc thêm cube mới sẽ làm nó có 3
                if (neighborCount >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public Vector3 GetNextTurnDir(){
        if (dirToTurn.Count > 0){
            Vector3 dir = dirToTurn[0];
            dirToTurn.RemoveAt(0);
            return dir;
        }
        return Vector3.zero;
    }
}
