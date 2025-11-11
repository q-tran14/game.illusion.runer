using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý UI trong game: Score, Game Over panel, Restart button
/// </summary>
public class UIManager : Singleton<UIManager>
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI finalScoreText;

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
