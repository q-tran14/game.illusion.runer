using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Menu;

    [Header("Score")]
    [SerializeField] private int score = 0;
    [SerializeField] private int highScore = 0;
    [SerializeField] private float scorePerSecond = 10f; // Tăng 10 điểm/giây

    private float scoreTimer = 0f;

    protected override void Awake()
    {
        // Application.targetFrameRate = 60;
        base.Awake();
        LoadHighScore();
    }

    void Start()
    {
        // Auto start game
        // StartGame();
    }

    void Update()
    {
        // Tự động tăng score khi đang chơi
        if (currentState == GameState.Playing)
        {
            scoreTimer += Time.deltaTime;
            if (scoreTimer >= 1f / scorePerSecond)
            {
                AddScore(1);
                scoreTimer = 0f;
            }
        }
    }

    public void StartGame()
    {
        currentState = GameState.Playing;
        score = 0;
        Time.timeScale = 1f;

        // Hide play button when game starts
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HidePlayButton();
        }

        Debug.Log("Game Started!");
    }

    public void OnGameOver()
    {
        if (currentState == GameState.GameOver) return;

        currentState = GameState.GameOver;
        Time.timeScale = 0f;

        // Update high score
        if (score > highScore)
        {
            highScore = score;
            SaveHighScore();
        }

        // Notify UI - show game over with restart button
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver(score);
            UIManager.Instance.ShowRestartButton();
        }

        Debug.Log($"🔴 Game Over! Score: {score} | High Score: {highScore}");
    }

    public async void RestartGame()
    {
        Time.timeScale = 1f;
        currentState = GameState.Menu;
        score = 0;
        
        // Show loading UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideGameOver();
            UIManager.Instance.ShowLoading();
        }
        
        // Clear and regenerate map
        if (MapGenerator.Instance != null)
        {
            MapGenerator.Instance.ClearMap();
            MapGenerator.Instance.SpawnMap();
            
            // Wait for map to be ready
            while (!MapGenerator.Instance.IsMapReady)
            {
                await System.Threading.Tasks.Task.Yield();
            }
        }
        
        // Hide loading and show play button
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideLoading();
            UIManager.Instance.HideRestartButton();
            UIManager.Instance.ShowPlayButton();
        }
        
        Debug.Log("[GameManager] Game restarted - waiting for Play button");
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
        }
    }

    public void AddScore(int points)
    {
        score += points;
    }

    private void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    // Getters
    public GameState GetCurrentState() => currentState;
    public int GetScore() => score;
    public int GetHighScore() => highScore;
}

