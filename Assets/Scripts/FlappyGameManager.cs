using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the Flappy Bird game state: score, game over, restart.
/// Created programmatically by FlappyBirdSetup.
/// </summary>
public class FlappyGameManager : MonoBehaviour
{
    private int score = 0;
    private int highScore = 0;
    private bool isGameOver = false;
    private bool isGameStarted = false;

    // UI references (set by FlappyBirdSetup)
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverScoreText;
    public TextMeshProUGUI newRecordText;
    public GameObject startPanel;

    private BirdController bird;
    private PipeSpawner pipeSpawner;

    private void Start()
    {
        highScore = PlayerPrefs.GetInt("FlappyHighScore", 0);
        UpdateUI();
    }

    public void SetReferences(BirdController birdCtrl, PipeSpawner spawner)
    {
        bird = birdCtrl;
        pipeSpawner = spawner;

        bird.OnDied += OnBirdDied;
    }

    private void Update()
    {
        if (isGameOver) return;

        if (!isGameStarted)
        {
            // New Input System tap to start
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                StartGame();
            }
            return;
        }

        // Check if bird fell off screen
        if (bird != null && bird.transform.position.y < -6f)
        {
            bird.Die();
        }
    }

    private void StartGame()
    {
        isGameStarted = true;
        isGameOver = false;
        score = 0;
        UpdateUI();

        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Activate bird physics (gravity) when game starts
        if (bird != null) bird.EnablePhysics();

        if (pipeSpawner != null) pipeSpawner.StartSpawning();
    }

    public void AddScore()
    {
        if (isGameOver) return;
        score++;
        UpdateUI();
    }

    private void OnBirdDied()
    {
        isGameOver = true;

        if (pipeSpawner != null) pipeSpawner.StopSpawning();

        // Check high score
        bool isNewRecord = false;
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("FlappyHighScore", highScore);
            PlayerPrefs.Save();
            isNewRecord = true;
        }

        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverScoreText != null)
                gameOverScoreText.text = "Pontuação: " + score;
            if (newRecordText != null)
                newRecordText.gameObject.SetActive(isNewRecord && score > 0);
        }

        UpdateUI();
    }

    public void RestartGame()
    {
        // Clean up all pipes
        foreach (var pipe in FindObjectsByType<PipeMove>(FindObjectsSortMode.None))
        {
            Destroy(pipe.gameObject);
        }

        isGameStarted = false;
        isGameOver = false;
        score = 0;

        if (bird != null) bird.ResetBird(new Vector2(-2f, 0f));
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (startPanel != null) startPanel.SetActive(true);

        UpdateUI();
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = score.ToString();
        if (highScoreText != null) highScoreText.text = "Recorde: " + highScore;
    }
}
