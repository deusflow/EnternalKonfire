/*
 * GameManager.cs
 * FINAL VERSION (with full sound)
 *
 * This script manages the main game logic, score, UI, and sounds.
 * Comments are written by a student for other students!
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Using TextMeshPro for UI text
using System.Collections; // Needed for coroutines (IEnumerator)

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

    [Header("Audio Setup")]
    public AudioSource gameMusicSource;   
    public AudioSource buttonClickSource; 
    public AudioClip gameOverClip;
    
    [Header("Settings")]
    public float loadDelay = 0.5f; // Delay for button sounds

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

    public void EndGame(string reason)
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f; // Pause the game

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

    // --- Button functions (now use coroutines for sound timing) ---
    public void RestartGame()
    {
        // Button calls this, which starts a coroutine for sound
        StartCoroutine(RestartWithSound());
    }

    public void LoadMainMenu()
    {
        // Button calls this, which starts a coroutine for sound
        StartCoroutine(LoadMenuWithSound());
    }

    // --- Coroutines for button sound timing ---
    private IEnumerator RestartWithSound()
    {
        // 1. Play sound
        PlayButtonClickSound(); 
        // 2. Wait (ignores Time.timeScale = 0)
        yield return new WaitForSecondsRealtime(loadDelay); 
        // 3. Reload scene
        Time.timeScale = 1f; // Unpause
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator LoadMenuWithSound()
    {
        // 1. Play sound
        PlayButtonClickSound();
        // 2. Wait (ignores pause)
        yield return new WaitForSecondsRealtime(loadDelay);
        // 3. Load main menu
        Time.timeScale = 1f; // Unpause
        SceneManager.LoadScene("MainMenu");
    }
    
    // --- Play button click sound ---
    public void PlayButtonClickSound()
    {
        if (buttonClickSource != null && buttonClickSource.clip != null)
        {
            buttonClickSource.PlayOneShot(buttonClickSource.clip); 
        }
    }
}