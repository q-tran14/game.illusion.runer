using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

/// <summary>
/// Quản lý UI trong game: Score, Game Over panel, Restart button, Loading UI
/// </summary>
public class UIManager : Singleton<UIManager>
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private Image loadingProgressBar; // Optional: cho progress bar

    protected override void Awake()
    {
        base.Awake();
        
        // Setup button
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }

    void Start()
    {
        HideGameOver();
        UpdateScore(0);
        UpdateHighScore(GameManager.Instance.GetHighScore());
        
        // Check if map is loading and show loading UI
        CheckMapLoadingState();
    }

    async void CheckMapLoadingState()
    {
        var mapGen = MapGenerator.Instance;
        if (mapGen == null) return;

        // Nếu map đang load, hiện loading UI
        if (mapGen.IsLoading || !mapGen.IsMapReady)
        {
            ShowLoading();
            await WaitForMapReady();
            HideLoading();
        }
    }

    async Task WaitForMapReady()
    {
        var mapGen = MapGenerator.Instance;
        if (mapGen == null) return;

        while (!mapGen.IsMapReady)
        {
            // Update progress bar nếu có
            if (loadingProgressBar != null && mapGen.IsLoading)
            {
                // Progress tính theo số cube đã spawn (example)
                // Bạn có thể thêm LoadingProgress property vào MapGenerator để chính xác hơn
                loadingProgressBar.fillAmount = Mathf.Clamp01(0.5f); // Placeholder
            }
            
            await Task.Yield(); // Wait 1 frame
        }
    }

    public void ShowLoading()
    {
        if (loadingUI != null)
        {
            loadingUI.SetActive(true);
            Debug.Log("[UIManager] Loading UI shown.");
        }
    }

    public void HideLoading()
    {
        if (loadingUI != null)
        {
            loadingUI.SetActive(false);
            Debug.Log("[UIManager] Loading UI hidden.");
        }
        
        // Enable player movement khi tắt loading UI
        if (PlayerController.Instance != null) PlayerController.Instance.EnableMovement();
    }

    void Update()
    {
        // Update score liên tục
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentState() == GameManager.GameState.Playing)
        {
            UpdateScore(GameManager.Instance.GetScore());
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    public void UpdateHighScore(int highScore)
    {
        if (highScoreText != null)
        {
            highScoreText.text = $"Best: {highScore}";
        }
    }

    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {finalScore}";
        }

        UpdateHighScore(GameManager.Instance.GetHighScore());
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }
}
