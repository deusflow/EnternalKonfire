using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Logic & Scoring")]
    public int score = 0;
    public int highScore = 0;
    public float survivalTimer = 0f;
    private bool isGameOver = false;

    [Header("Run Statistics")]
    public int treesChopped = 0;
    public int catsBanished = 0;
    public int logsBurned = 0;

    [Header("UI References")]
    public GameObject gameOverScreen;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI notificationText;

    [Header("Game Over Buttons")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Start Menu Overlay")]
    public GameObject startMenuScreen;
    public Button startGameButton;
    public TextMeshProUGUI startHighScoreText;
    public TextMeshProUGUI startRankText;
    public TextMeshProUGUI startLeaderboardText;
    private bool isGameStarted = false;

    [Header("Audio Setup")]
    public AudioSource gameMusicSource;
    public AudioSource buttonClickSource;
    public AudioClip gameOverClip;

    [Header("Settings")]
    public float loadDelay = 0.1f;

    private Coroutine notificationCoroutine;
    private const string PREF_HIGHSCORE = "Konfire_HighScore";

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
        if (gameOverScreen != null) { gameOverScreen.SetActive(false); }

        highScore = PlayerPrefs.GetInt(PREF_HIGHSCORE, 0);

        UpdateScoreText();
        UpdateTimerText();
        BindGameOverButtons();
        SetupStartMenu();
    }

    void Update()
    {
        if (!isGameStarted)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                StartGameFromMenu();
            }
            return;
        }

        if (!isGameOver)
        {
            survivalTimer += Time.deltaTime;
            UpdateTimerText();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                RestartGame();
            }
        }
    }

    public void SetupStartMenu()
    {
        if (startMenuScreen != null)
        {
            startMenuScreen.SetActive(true);
            startMenuScreen.transform.SetAsLastSibling();
            isGameStarted = false;
            Time.timeScale = 0f;

            if (startHighScoreText != null)
            {
                startHighScoreText.text = $"BEST RECORD: {highScore} PTS";
            }
            if (startRankText != null)
            {
                startRankText.text = $"TITLE: {GetRankTitle(highScore)}";
            }
            if (startLeaderboardText != null)
            {
                startLeaderboardText.text = 
                    "<color=#FFD700>[I] LORD OF CINDER</color>  (600+ PTS)\n" +
                    "<color=#FFA500>[II] FLAME GUARDIAN</color> (300+ PTS)\n" +
                    "<color=#00CED1>[III] EMBER TENDER</color>  (100+ PTS)\n" +
                    "<color=#AAAAAA>[IV] NOVICE KEEPER</color>   (0-99 PTS)\n\n" +
                    $"<color=#88FF88>CURRENT STANDING: {GetRankTitle(highScore)} ({highScore} PTS)</color>";
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(StartGameFromMenu);
            }
        }
        else
        {
            isGameStarted = true;
            Time.timeScale = 1f;
            if (gameMusicSource != null && !gameMusicSource.isPlaying)
            {
                gameMusicSource.loop = true;
                gameMusicSource.Play();
            }
        }
    }

    public void StartGameFromMenu()
    {
        PlayButtonClickSound();
        isGameStarted = true;
        Time.timeScale = 1f;

        if (startMenuScreen != null)
        {
            startMenuScreen.SetActive(false);
        }

        if (gameMusicSource != null && !gameMusicSource.isPlaying)
        {
            gameMusicSource.loop = true;
            gameMusicSource.Play();
        }
    }

    public string GetRankTitle(int scoreValue)
    {
        if (scoreValue >= 600) return "<color=#FFD700>LORD OF CINDER</color>";
        if (scoreValue >= 300) return "<color=#FFA500>FLAME GUARDIAN</color>";
        if (scoreValue >= 100) return "<color=#00CED1>EMBER TENDER</color>";
        return "<color=#CCCCCC>NOVICE KEEPER</color>";
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;
        score += amount;
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(PREF_HIGHSCORE, highScore);
        }
        UpdateScoreText();
    }

    public void RecordTreeChopped()
    {
        treesChopped++;
    }

    public void RecordCatBanished()
    {
        catsBanished++;
    }

    public void RecordLogBurned()
    {
        logsBurned++;
    }

    public void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
        if (highScoreText != null)
        {
            highScoreText.text = $"Best: {highScore}";
        }
    }

    void UpdateTimerText()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(survivalTimer / 60F);
            int seconds = Mathf.FloorToInt(survivalTimer % 60F);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(survivalTimer / 60F);
        int seconds = Mathf.FloorToInt(survivalTimer % 60F);
        return $"{minutes:00}:{seconds:00}";
    }

    public void ShowNotification(string message, float duration = 3f)
    {
        if (notificationText != null)
        {
            if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
            notificationCoroutine = StartCoroutine(DisplayNotification(message, duration));
        }
    }

    private IEnumerator DisplayNotification(string message, float duration)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(duration);
        notificationText.gameObject.SetActive(false);
    }

    public void EndGame(string reason)
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f;

        bool isNewRecord = false;
        if (score >= highScore && score > 0)
        {
            isNewRecord = true;
            highScore = score;
            PlayerPrefs.SetInt(PREF_HIGHSCORE, highScore);
            PlayerPrefs.Save();
        }

        if (gameMusicSource != null)
        {
            gameMusicSource.Stop();
        }
        if (gameMusicSource != null && gameOverClip != null)
        {
            gameMusicSource.PlayOneShot(gameOverClip);
        }

        if (gameOverScreen != null) 
        { 
            gameOverScreen.SetActive(true); 
            gameOverScreen.transform.SetAsLastSibling();
            BindGameOverButtons();
        }
        if (gameOverText != null)
        {
            string recordBadge = isNewRecord ? "\n<color=#FFD700>[NEW HIGH SCORE!]</color>" : "";
            gameOverText.text = $"{reason.ToUpper()}{recordBadge}\n\n" +
                                $"Time Survived:  {GetFormattedTime()}\n" +
                                $"Final Score:    {score}\n" +
                                $"High Score:     {highScore}\n\n" +
                                $"<size=20>Trees Felled: {treesChopped}  |  Cats Banished: {catsBanished}  |  Logs Burned: {logsBurned}</size>";
        }
    }

    public void BindGameOverButtons()
    {
        if (gameOverScreen != null)
        {
            if (restartButton == null)
            {
                var r = gameOverScreen.transform.Find("RestartButton");
                if (r != null) restartButton = r.GetComponent<Button>();
            }
            if (mainMenuButton == null)
            {
                var m = gameOverScreen.transform.Find("MainMenuButton");
                if (m != null) mainMenuButton = m.GetComponent<Button>();
            }
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
            restartButton.interactable = true;
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(LoadMainMenu);
            mainMenuButton.interactable = true;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        PlayButtonClickSound();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        PlayButtonClickSound();
        SceneManager.LoadScene("MainMenu");
    }

    public void PlayButtonClickSound()
    {
        if (buttonClickSource != null && buttonClickSource.clip != null)
        {
            buttonClickSource.PlayOneShot(buttonClickSource.clip);
        }
    }
}
