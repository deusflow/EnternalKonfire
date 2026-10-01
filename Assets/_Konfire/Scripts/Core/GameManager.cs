using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Logic")]
    public int score = 0;
    private bool isGameOver = false;

    [Header("UI References")]
    public GameObject gameOverScreen;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI notificationText;

    [Header("Audio Setup")]
    public AudioSource gameMusicSource;
    public AudioSource buttonClickSource;
    public AudioClip gameOverClip;

    [Header("Settings")]
    public float loadDelay = 0.5f;

    private Coroutine notificationCoroutine;

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
        if (gameOverScreen != null) { gameOverScreen.SetActive(false); }
        Time.timeScale = 1f;

        if (gameMusicSource != null)
        {
            gameMusicSource.loop = true;
            gameMusicSource.Play();
        }

        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;
        score += amount;
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
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
            gameOverText.text = reason.ToUpper() + "\nFinal Score: " + score;
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
