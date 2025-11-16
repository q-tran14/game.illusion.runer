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
    [SerializeField] private bool allowBackward = true; // Cho phép spawn theo trục Z- (Back)
    [SerializeField] private bool allowVertical = true; // Cho phép spawn theo trục Y (Up/Down)
    private readonly Dictionary<(Vector3, PathSegment.FaceType), PathSegment> cubeMap = new();
    private readonly List<PathSegment> activeCubes = new();

    private Vector3 currentGrid = Vector3.zero;
    private PathSegment.TurnDir currentDir = PathSegment.TurnDir.Straight;

    // Spacing đo từ bounds của model (khởi tạo lazy lần đầu spawn)
    private bool spacingInitialized = false;
    private float spacingX;
    private float spacingY;
    private float spacingZ;
    private Vector3 placementOffset = Vector3.zero; // dịch pivot để tâm mesh nằm đúng grid
    [SerializeField] private float sideFacePad = 0.02f; // đẩy nhẹ cube mặt bên để tránh đè/z-fighting

    // Vertical chain tracking
    private bool isInVerticalChain = false;
    private int verticalChainLength = 0;
    private const int minVerticalChain = 2;
    private const int maxVerticalChain = 6;
    private PathSegment.FaceType currentFace = PathSegment.FaceType.Top;
    private PathSegment.TurnDir lastHorizontalDir = PathSegment.TurnDir.Straight; // hướng ngang trước khi Up/Down

    [Header("Environment Profiles")]
    [SerializeField] private EnvironmentProfile[] environments = new EnvironmentProfile[3];
    [SerializeField] private int currentEnvironmentIndex = 0;
    private EnvironmentProfile activeEnvironment;

    [Header("Boot" )]
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

    public async void SpawnMap()
    {
        isLoading = true;
        isMapReady = false;
        
        Debug.Log("[MapGenerator] Starting map generation...");
        
        for (int i = 0; i < initialCubes; i++) await SpawnNextCube();
        
        // Spawn player ở giữa trên cube đầu tiên
        if (activeCubes.Count > 0) await SpawnPlayer();
        
        isLoading = false;
        isMapReady = true;
        
        // Bảo đảm player có thể chạy ngay cả khi UI không gọi EnableMovement
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EnableMovement();
        }

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
            
            if (playerController != null) playerController.Initialize(spawnPos, firstCube);
            else player.position = spawnPos;
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
        currentFace = PathSegment.FaceType.Top;
        isInVerticalChain = false;
        verticalChainLength = 0;
        spacingInitialized = false; // đo lại sau khi clear

        // Chỉ dọn pool nếu bạn muốn thực sự hủy các instance nhàn rỗi
        ObjectPool.Instance.RemovePool();
    }

    private bool isSpawningInFlight = false;

    private void Update()
    {
        if (player == null || activeCubes.Count == 0) return;
        
        // Spawn thêm khi player gần cuối đường (dùng sqrDistance tránh sqrt)
        Vector3 lastPos = activeCubes[^1].transform.position;
        float distSqr = (player.position - lastPos).sqrMagnitude;
        float thresholdSqr = distPlayerAndLastCube * distPlayerAndLastCube;
        if (!isSpawningInFlight && distSqr < thresholdSqr)
        {
            _ = SpawnNextCubeGuarded();
        }

        // Trả những cube phía sau player về pool khi đã cách 1-2 ô
        TrimBehindCubes(keepBehind: 4);

        // Giới hạn số lượng cube trong scene
        if (activeCubes.Count > maxCubes)
        {
            var old = activeCubes[0];
            activeCubes.RemoveAt(0);
            cubeMap.Remove((old.gridPos, old.faceType));
            ObjectPool.Instance.Return(old.gameObject);
        }
    }

    private async System.Threading.Tasks.Task SpawnNextCubeGuarded()
    {
        if (isSpawningInFlight) return;
        isSpawningInFlight = true;
        try
        {
            await SpawnNextCube();
        }
        finally
        {
            isSpawningInFlight = false;
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
                cubeMap.Remove((seg.gridPos, seg.faceType));
                ObjectPool.Instance.Return(seg.gameObject);
            }
        }
    }

    private async Task SpawnNextCube()
    {
        var prefabObj = await ObjectPool.Instance.GetAsync();
        if (prefabObj == null)
        {
            Debug.LogWarning("[MapGenerator] Pool exhausted. Skipping spawn this frame.");
            return;
        }

        var seg = prefabObj.GetComponent<PathSegment>();
        if (seg == null) seg = prefabObj.AddComponent<PathSegment>();

        PathSegment.TurnDir nextDir;
        Vector3 nextGrid;
        PathSegment.FaceType nextFace;
        Quaternion nextRotation;

        bool isFirst = activeCubes.Count == 0;
        bool stillInitialStraight = activeCubes.Count > 0 && activeCubes.Count < initialStraightCount;

        if (isFirst)
        {
            // Cube đầu tiên
            nextDir = PathSegment.TurnDir.Straight;
            nextGrid = Vector3.zero;
            nextFace = PathSegment.FaceType.Top;
            nextRotation = Quaternion.identity;
        }
        else if (stillInitialStraight)
        {
            nextDir = PathSegment.TurnDir.Straight;
            nextGrid = GetNextGrid(currentGrid, nextDir);
            nextFace = PathSegment.FaceType.Top;
            nextRotation = Quaternion.identity;
        }
        else
        {
            // Logic chính: xử lý vertical chains
            nextDir = GetNextValidDirection();
            
            if (nextDir == PathSegment.TurnDir.Up)
            {
                // Bắt đầu hoặc tiếp tục Up chain
                if (!isInVerticalChain)
                {
                    // Bắt đầu Up chain mới
                    isInVerticalChain = true;
                    verticalChainLength = 1;
                    lastHorizontalDir = currentDir;
                    
                    // Xác định face dựa vào hướng horizontal trước đó
                    if (currentDir == PathSegment.TurnDir.Straight || currentDir == PathSegment.TurnDir.Backward)
                    {
                        nextFace = PathSegment.FaceType.Back;
                        nextRotation = Quaternion.Euler(-90f, 0f, 0f);
                    }
                    else // Left hoặc Right
                    {
                        nextFace = PathSegment.FaceType.Right;
                        nextRotation = Quaternion.Euler(-90f, -90f, 0f);
                    }
                    
                    // Up: gridPos MỚI (Y+1)
                    nextGrid = currentGrid + new Vector3(0, 1, 0);
                }
                else
                {
                    // Tiếp tục Up chain
                    verticalChainLength++;
                    nextFace = currentFace;
                    nextRotation = activeCubes[^1].transform.rotation;
                    nextGrid = currentGrid + new Vector3(0, 1, 0);
                }
            }
            else if (nextDir == PathSegment.TurnDir.Down)
            {
                if (!isInVerticalChain)
                {
                    // Bắt đầu Down chain mới
                    isInVerticalChain = true;
                    verticalChainLength = 1;
                    lastHorizontalDir = currentDir;
                    
                    // Xác định face dựa vào hướng horizontal (ngược với Up)
                    if (currentDir == PathSegment.TurnDir.Straight || currentDir == PathSegment.TurnDir.Backward)
                    {
                        nextFace = PathSegment.FaceType.Right;
                        nextRotation = Quaternion.Euler(-90f, -90f, 0f);
                    }
                    else // Left hoặc Right
                    {
                        nextFace = PathSegment.FaceType.Back;
                        nextRotation = Quaternion.Euler(-90f, 0f, 0f);
                    }
                    
                    // Down: KHÔNG spawn tại điểm giao; đặt cube đầu tiên tại Y-1
                    nextGrid = currentGrid + new Vector3(0, -1, 0);
                }
                else
                {
                    // Tiếp tục Down chain
                    verticalChainLength++;
                    nextFace = currentFace;
                    nextRotation = activeCubes[^1].transform.rotation;
                    nextGrid = currentGrid + new Vector3(0, -1, 0);
                }
            }
            else
            {
                // Horizontal direction
                if (isInVerticalChain)
                {
                    // Kết thúc vertical chain, quay về Top
                    bool wasUpChain = (currentDir == PathSegment.TurnDir.Up);
                    // Xác định vị trí Top tại điểm giao (không spawn tại đây)
                    Vector3 junctionTopGrid = wasUpChain
                        ? currentGrid                       // Up chain: Top tại cùng grid
                        : currentGrid + new Vector3(0, -1, 0); // Down chain: Top tại grid Y-1

                    // Không spawn tại junction; spawn ngay ô Top đầu tiên theo hướng horizontal chọn
                    nextFace = PathSegment.FaceType.Top;
                    nextRotation = Quaternion.identity;
                    nextGrid = GetNextGrid(junctionTopGrid, nextDir);

                    // Reset vertical chain
                    isInVerticalChain = false;
                    verticalChainLength = 0;
                }
                else
                {
                    // Di chuyển horizontal bình thường trên Top
                    nextGrid = GetNextGrid(currentGrid, nextDir);
                    nextFace = PathSegment.FaceType.Top;
                    nextRotation = Quaternion.identity;
                }
            }
        }

        // Kiểm tra trùng
        if (HasCubeAt(nextGrid, nextFace))
        {
            ObjectPool.Instance.Return(prefabObj);
            return;
        }

        // Assign direction to previous segment
        if (activeCubes.Count > 0)
        {
            var tail = activeCubes[^1];
            if (tail != null) tail.direction = nextDir;
        }

        // Init new segment
        seg.Init(nextGrid, PathSegment.TurnDir.Straight, nextFace);
        
        // Link list
        if (activeCubes.Count > 0)
        {
            var prev = activeCubes[^1];
            if (prev != null) prev.next = seg;
        }

        // Initialize spacing
        if (!spacingInitialized) InitializeSpacing(prefabObj);

        // Calculate world position (3D) with rotation-aware offset and face alignment
        Vector3 basePos = new Vector3(
            nextGrid.x * spacingX,
            nextGrid.y * spacingY,
            nextGrid.z * spacingZ
        );

        // Rotate placement offset so mesh center lands on grid center after rotation
        Vector3 rotatedPlacementOffset = nextRotation * placementOffset;

        // Push side-face cubes so their "top" sits flush on the side of Top cube at same grid
        // Compute face normal in world space (local up after rotation)
        Vector3 faceNormal = nextRotation * Vector3.up;

        // Determine dominant axis of the face normal and corresponding spacing
        float ax = Mathf.Abs(faceNormal.x);
        float ay = Mathf.Abs(faceNormal.y);
        float az = Mathf.Abs(faceNormal.z);
        float axisSize = spacingY; // default for Top
        if (ax > ay && ax > az)
        {
            axisSize = spacingX; // Right/Left faces attach along X
        }
        else if (az > ay)
        {
            axisSize = spacingZ; // Front/Back faces attach along Z
        }
        // If ay is largest, it's Top; axisSize stays spacingY

        // Shift amount so rotated cube's top plane touches the side plane of the Top cube
        float attachShift = (axisSize - spacingY) * 0.5f + sideFacePad;
        Vector3 attachOffset = faceNormal * attachShift;

        Vector3 worldPos = basePos - rotatedPlacementOffset + attachOffset;

        prefabObj.transform.position = worldPos;
        prefabObj.transform.rotation = nextRotation;

        cubeMap[(nextGrid, nextFace)] = seg;
        activeCubes.Add(seg);

        currentDir = nextDir;
        currentGrid = nextGrid;
        currentFace = nextFace;

        prefabObj.SetActive(true);
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
    spacingY = hasBounds ? bounds.size.y + gap : fallback;
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
        // Nếu đang trong vertical chain, chỉ cho phép Up/Down
        if (isInVerticalChain)
        {
            // Kiểm tra còn trong giới hạn chain không
            if (verticalChainLength < maxVerticalChain)
            {
                // Tiếp tục cùng hướng (Up hoặc Down)
                return currentDir;
            }
            else
            {
                // Đã đạt max, buộc phải kết thúc chain (quay về Top)
                // Direction sẽ được set khi spawn cube kết thúc
                return PathSegment.TurnDir.Straight; // placeholder
            }
        }

        // Đang ở Top face - có thể đi horizontal hoặc bắt đầu vertical
        var baseCandidates = new List<PathSegment.TurnDir>
        {
            PathSegment.TurnDir.Straight,
            PathSegment.TurnDir.Left,
            PathSegment.TurnDir.Right
        };
        if (allowBackward) baseCandidates.Add(PathSegment.TurnDir.Backward);
        if (allowVertical && activeCubes.Count >= initialStraightCount)
        {
            baseCandidates.Add(PathSegment.TurnDir.Up);
            baseCandidates.Add(PathSegment.TurnDir.Down);
        }

        var working = new List<PathSegment.TurnDir>(baseCandidates);
        var opposite = GetOpposite(currentDir);
        bool removedOpposite = working.Remove(opposite);

        List<PathSegment.TurnDir> FilterFeasible(List<PathSegment.TurnDir> list)
        {
            var res = new List<PathSegment.TurnDir>();
            for (int i = 0; i < list.Count; i++)
            {
                var dir = list[i];
                
                // Vertical directions được xử lý riêng
                if (dir == PathSegment.TurnDir.Up || dir == PathSegment.TurnDir.Down)
                {
                    // Luôn cho phép bắt đầu vertical nếu chưa trong chain
                    res.Add(dir);
                    continue;
                }
                
                // Horizontal directions
                Vector3 ng = GetNextGrid(currentGrid, dir);
                if (HasCubeAt(ng, currentFace)) continue;
                if (!HasSimplePathAdjacency(ng, currentGrid, currentFace)) continue;
                if (WouldExceedNeighborLimit(ng)) continue;
                res.Add(dir);
            }
            return res;
        }

        var feasible = FilterFeasible(working);

        if (feasible.Count == 0 && removedOpposite)
        {
            var withOpp = new List<PathSegment.TurnDir>(working) { opposite };
            feasible = FilterFeasible(withOpp);
        }

        if (feasible.Count == 0)
        {
            if (working.Contains(PathSegment.TurnDir.Straight)) return PathSegment.TurnDir.Straight;
            return baseCandidates[Random.Range(0, baseCandidates.Count)];
        }

        return feasible[Random.Range(0, feasible.Count)];
    }

    // Helper: kiểm tra xem có cube nào tại (grid, face) không
    private bool HasCubeAt(Vector3 grid, PathSegment.FaceType face)
    {
        return cubeMap.ContainsKey((grid, face));
    }

    // Helper: kiểm tra xem có cube nào tại grid (bất kỳ face nào) không
    private bool HasCubeAtAnyFace(Vector3 grid)
    {
        return HasCubeAt(grid, PathSegment.FaceType.Top) ||
               HasCubeAt(grid, PathSegment.FaceType.Back) ||
               HasCubeAt(grid, PathSegment.FaceType.Right);
    }

    // Đảm bảo ô mới chỉ kề đúng 1 ô là currentGrid (tránh khép vòng/nhánh)
    // Chỉ áp dụng cho horizontal movement trên cùng face
    private bool HasSimplePathAdjacency(Vector3 pos, Vector3 expectedNeighbor, PathSegment.FaceType face)
    {
        // Chỉ xét 4 hướng phẳng (±X, ±Z) trên cùng face
        Vector3[] dirs = new Vector3[]
        {
            new Vector3(1,0,0),
            new Vector3(-1,0,0),
            new Vector3(0,0,1),
            new Vector3(0,0,-1),
        };

        int count = 0;
        bool hasExpected = false;
        for (int i = 0; i < dirs.Length; i++)
        {
            Vector3 n = pos + dirs[i];
            if (HasCubeAt(n, face))
            {
                count++;
                if (n == expectedNeighbor) hasExpected = true;
            }
        }

        if (activeCubes.Count == 0)
        {
            return count == 0;
        }

        return count == 1 && hasExpected;
    }

    private PathSegment.TurnDir GetOpposite(PathSegment.TurnDir dir)
    {
        switch (dir)
        {
            case PathSegment.TurnDir.Straight: return PathSegment.TurnDir.Backward;
            case PathSegment.TurnDir.Backward: return PathSegment.TurnDir.Straight;
            case PathSegment.TurnDir.Left: return PathSegment.TurnDir.Right;
            case PathSegment.TurnDir.Right: return PathSegment.TurnDir.Left;
            case PathSegment.TurnDir.Up: return PathSegment.TurnDir.Down;
            case PathSegment.TurnDir.Down: return PathSegment.TurnDir.Up;
            default: return PathSegment.TurnDir.Straight;
        }
    }

    private Vector3 GetNextGrid(Vector3 pos, PathSegment.TurnDir dir)
    {
        switch (dir)
        {
            case PathSegment.TurnDir.Straight:   return pos + new Vector3(0, 0, 1);   // Z+
            case PathSegment.TurnDir.Backward:   return pos + new Vector3(0, 0, -1);  // Z-
            case PathSegment.TurnDir.Left:       return pos + new Vector3(-1, 0, 0);  // X-
            case PathSegment.TurnDir.Right:      return pos + new Vector3(1, 0, 0);   // X+
            case PathSegment.TurnDir.Up:         return pos + new Vector3(0, 1, 0);   // Y+
            case PathSegment.TurnDir.Down:       return pos + new Vector3(0, -1, 0);  // Y-
            default: return pos + new Vector3(0, 0, 1);
        }
    }

    // Đếm số cube neighbor kề cạnh (6 hướng chính: ±X, ±Y, ±Z) - xét tất cả faces
    private int CountNeighbors(Vector3 pos)
    {
        int count = 0;
        Vector3[] directions = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1)
        };

        foreach (var dir in directions)
        {
            if (HasCubeAtAnyFace(pos + dir))
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
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1)
        };

        foreach (var dir in directions)
        {
            Vector3 neighborPos = pos + dir;
            
            if (HasCubeAtAnyFace(neighborPos))
            {
                int neighborCount = CountNeighbors(neighborPos);
                
                if (neighborCount >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
