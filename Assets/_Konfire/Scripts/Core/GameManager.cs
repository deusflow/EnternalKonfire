using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Audio Setup")]
    public AudioSource gameMusicSource;
    public AudioSource buttonClickSource;
    public AudioClip gameOverClip;

    [Header("Settings")]
    public float loadDelay = 0.5f;

    private Coroutine notificationCoroutine;
    private const string PREF_HIGHSCORE = "Konfire_HighScore";

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
        if (gameOverScreen != null) { gameOverScreen.SetActive(false); }
        Time.timeScale = 1f;

        highScore = PlayerPrefs.GetInt(PREF_HIGHSCORE, 0);

        if (gameMusicSource != null)
        {
            gameMusicSource.loop = true;
            gameMusicSource.Play();
        }

        UpdateScoreText();
        UpdateTimerText();
    }

    void Update()
    {
        if (!isGameOver)
        {
            survivalTimer += Time.deltaTime;
            UpdateTimerText();
        }
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

        if (gameOverScreen != null) { gameOverScreen.SetActive(true); }
        if (gameOverText != null)
        {
            string recordBadge = isNewRecord ? "\n<color=#FFD700>★ NEW HIGH SCORE! ★</color>" : "";
            gameOverText.text = $"{reason.ToUpper()}{recordBadge}\n\n" +
                                $"Time Survived:  {GetFormattedTime()}\n" +
                                $"Final Score:    {score}\n" +
                                $"High Score:     {highScore}\n\n" +
                                $"<size=20>Trees Felled: {treesChopped}  |  Cats Banished: {catsBanished}  |  Logs Burned: {logsBurned}</size>";
        }
    }

    public void RestartGame()
    {
        StartCoroutine(RestartWithSound());
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadMenuWithSound());
    }

    private IEnumerator RestartWithSound()
    {
        PlayButtonClickSound();
        yield return new WaitForSecondsRealtime(loadDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator LoadMenuWithSound()
    {
        PlayButtonClickSound();
        yield return new WaitForSecondsRealtime(loadDelay);
        Time.timeScale = 1f;
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
