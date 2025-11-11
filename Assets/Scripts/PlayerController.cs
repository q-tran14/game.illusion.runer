using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("State")]
    private Vector3 moveDirection = Vector3.forward;
    private PathSegment currentSegment;
    private PathSegment nextSegment;
    private bool canTurn = false;
    private bool isAlive = true;
    private bool isOnPath = false;
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
        if (!isAlive) return;

        // Di chuyển player theo hướng hiện tại
        transform.position += moveDirection * speed * Time.deltaTime;

        // Xoay mượt player theo hướng di chuyển
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Click chuột trái để turn khi có thể
        if (Input.GetMouseButtonDown(0) && canTurn && nextSegment != null)
        {
            TurnToDirection(nextSegment.direction);
            currentSegment = nextSegment;
            nextSegment = null;
            canTurn = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Phát hiện cube tiếp theo - CHỜ CLICK ĐỂ TURN
        if (other.CompareTag("PathCube"))
        {
            var segment = other.GetComponent<PathSegment>();
            if (segment != null && segment != currentSegment)
            {
                // Lưu segment tiếp theo, chờ player click để turn
                nextSegment = segment;
                canTurn = true;
                isOnPath = true;
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Player vẫn ở trên path
        if (other.CompareTag("PathCube"))
        {
            isOnPath = true;
        }
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
        if (!isOnPath && isAlive)
        {
            GameOver();
        }
    }

    private void TurnToDirection(PathSegment.TurnDir turnDir)
    {
        // ✅ Chuyển đổi TurnDir thành hướng di chuyển 3D
        // Đồng bộ với MapGenerator: Z = forward, X = left/right
        Vector3 newDirection = Vector3.forward;
        
        switch (turnDir)
        {
            case PathSegment.TurnDir.Straight:
                // Tiếp tục đi thẳng về phía trước
                newDirection = Vector3.forward; // (0, 0, 1)
                break;
            case PathSegment.TurnDir.Left:
                // Rẽ trái
                newDirection = Vector3.left; // (-1, 0, 0)
                break;
            case PathSegment.TurnDir.Right:
                // Rẽ phải
                newDirection = Vector3.right; // (1, 0, 0)
                break;
            case PathSegment.TurnDir.UpLeft:
                // Rẽ trái + tiếp tục thẳng (chéo)
                newDirection = (Vector3.left + Vector3.forward).normalized; // (-1, 0, 1)
                break;
            case PathSegment.TurnDir.DownLeft:
                // Rẽ trái + lùi lại (chéo ngược)
                newDirection = (Vector3.left + Vector3.back).normalized; // (-1, 0, -1)
                break;
            case PathSegment.TurnDir.UpRight:
                // Rẽ phải + tiếp tục thẳng (chéo)
                newDirection = (Vector3.right + Vector3.forward).normalized; // (1, 0, 1)
                break;
            case PathSegment.TurnDir.DownRight:
                // Rẽ phải + lùi lại (chéo ngược)
                newDirection = (Vector3.right + Vector3.back).normalized; // (1, 0, -1)
                break;
        }

        moveDirection = newDirection;
        Debug.Log($"Player turned to {turnDir} - Direction: {newDirection}");
    }

    public void Initialize(Vector3 startPos, PathSegment startSegment)
    {
        transform.position = startPos;
        currentSegment = startSegment;
        nextSegment = null;
        moveDirection = Vector3.forward;
        isAlive = true;
        isOnPath = true;
        canTurn = false;
        transform.rotation = Quaternion.identity;
        
        Debug.Log($"Player initialized at {startPos}");
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

