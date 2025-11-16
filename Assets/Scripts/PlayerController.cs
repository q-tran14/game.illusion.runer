using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private bool autoTurn = true; // Tự động rẽ theo hướng đã được định trước (segment.direction)
    [SerializeField] private float centeringSpeed = 5f; // Tốc độ kéo về giữa cube
    
    [Header("State")]
    private Vector3 moveDirection = Vector3.forward;
    private PathSegment currentSegment;
    // Không dùng cơ chế 'nextSegment' nữa; hướng được lấy từ segment hiện tại
    private bool isAlive = true;
    private bool isOnPath = false;
    private bool canMove = false; // Chỉ cho phép di chuyển khi map ready
    private Rigidbody rb;

    protected override void Awake()
    {
        base.Awake();
        
        // Setup Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = true;
        
        // Setup BoxCollider
        if (GetComponent<BoxCollider>() == null)
        {
            var boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = false;
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one;
        }
        
        // Tag
        gameObject.tag = "Player";
    }

    void Update()
    {
        if (!isAlive || !canMove) return;

        // Di chuyển player theo hướng hiện tại
        transform.position += moveDirection * speed * Time.deltaTime;

        // Kéo player về giữa cube hiện tại (chống trôi ra mép)
        if (currentSegment != null)
        {
            Vector3 cubeCenter = currentSegment.transform.position;
            
            // Tính mặt trên của cube
            Collider cubeCollider = currentSegment.GetComponent<Collider>();
            float cubeTopY = cubeCollider != null ? cubeCollider.bounds.max.y : cubeCenter.y;
            
            Vector3 currentPos = transform.position;
            Vector3 targetPos = currentPos;
            
            // Luôn kéo player về Y của mặt trên cube
            targetPos.y = Mathf.Lerp(currentPos.y, cubeTopY, centeringSpeed * Time.deltaTime);
            
            // Xác định trục cần điều chỉnh dựa vào hướng di chuyển
            if (Mathf.Abs(moveDirection.x) > 0.5f) // Đi ngang (Left/Right)
            {
                // Kéo về center theo Z (giữa hàng)
                targetPos.z = Mathf.Lerp(currentPos.z, cubeCenter.z, centeringSpeed * Time.deltaTime);
            }
            else if (Mathf.Abs(moveDirection.z) > 0.5f) // Đi thẳng (Forward/Back)
            {
                // Kéo về center theo X (giữa cột)
                targetPos.x = Mathf.Lerp(currentPos.x, cubeCenter.x, centeringSpeed * Time.deltaTime);
            }
            else if (Mathf.Abs(moveDirection.y) > 0.5f) // Đi dọc (Up/Down)
            {
                // Kéo về center theo cả X và Z
                targetPos.x = Mathf.Lerp(currentPos.x, cubeCenter.x, centeringSpeed * Time.deltaTime);
                targetPos.z = Mathf.Lerp(currentPos.z, cubeCenter.z, centeringSpeed * Time.deltaTime);
            }
            else // Diagonal
            {
                // Kéo về các trục không chủ yếu
                if (Mathf.Abs(moveDirection.x) < 0.3f)
                    targetPos.x = Mathf.Lerp(currentPos.x, cubeCenter.x, centeringSpeed * Time.deltaTime);
                if (Mathf.Abs(moveDirection.z) < 0.3f)
                    targetPos.z = Mathf.Lerp(currentPos.z, cubeCenter.z, centeringSpeed * Time.deltaTime);
            }
            
            transform.position = targetPos;
        }

        // Xoay mượt player theo hướng di chuyển
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Hướng di chuyển luôn theo direction của currentSegment
        if (autoTurn && currentSegment != null)
        {
            ApplySegmentDirection(currentSegment.direction);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isAlive) return;
        if (!other.CompareTag("PathCube")) return;

        var segment = other.GetComponent<PathSegment>();
        if (segment == null) return;

        currentSegment = segment;
        isOnPath = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("PathCube")) isOnPath = true;
    }

    void OnTriggerExit(Collider other)
    {
        // Rời khỏi cube
        if (other.CompareTag("PathCube"))
        {
            var segment = other.GetComponent<PathSegment>();
            if (segment == currentSegment)
            {
                isOnPath = false;
                
                // Nếu không có next segment → đã rời khỏi path → check game over
                // Delay 0.1s để tránh false positive khi chuyển cube
                Invoke(nameof(CheckGameOver), 0.1f);
            }
        }
    }

    private void CheckGameOver()
    {
        // Nếu player không còn trên path và chưa vào cube mới → Game Over
        if (!isOnPath && isAlive) GameOver();
    }

    private void ApplySegmentDirection(TurnDir turnDir)
    {
        Vector3 dir = moveDirection;
        switch (turnDir)
        {
            case TurnDir.Straight: dir = Vector3.forward; break;
            case TurnDir.Backward: dir = Vector3.back; break;
            case TurnDir.Left: dir = Vector3.left; break;
            case TurnDir.Right: dir = Vector3.right; break;
            case TurnDir.UpLeft: dir = (Vector3.left + Vector3.up).normalized; break;
            case TurnDir.DownLeft: dir = (Vector3.left + Vector3.down).normalized; break;
            case TurnDir.UpRight: dir = (Vector3.right + Vector3.up).normalized; break;
            case TurnDir.DownRight: dir = (Vector3.right + Vector3.down).normalized; break;
        }
        if (dir != moveDirection) moveDirection = dir;
    }

    public void Initialize(Vector3 startPos, PathSegment startSegment)
    {
        transform.position = startPos;
        currentSegment = startSegment;
        moveDirection = Vector3.forward;
        isAlive = true;
        isOnPath = true;
        canMove = false; // Mặc định không cho di chuyển cho đến khi gọi EnableMovement()
        transform.rotation = Quaternion.identity;
        
        Debug.Log($"Player initialized at {startPos}");
    }

    /// <summary>
    /// Cho phép player di chuyển (gọi sau khi loading xong)
    /// </summary>
    public void EnableMovement()
    {
        canMove = true;
        Debug.Log("[PlayerController] Movement enabled.");
    }

    /// <summary>
    /// Tắt di chuyển (dùng khi pause, loading, v.v.)
    /// </summary>
    public void DisableMovement()
    {
        canMove = false;
        Debug.Log("[PlayerController] Movement disabled.");
    }

    private void GameOver()
    {
        isAlive = false;
        moveDirection = Vector3.zero; // Dừng di chuyển
        Debug.Log("🔴 GAME OVER - Player rời khỏi đường!");
        
        // Gọi GameManager để xử lý game over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver();
        }
    }

    public bool IsAlive() => isAlive;
}

