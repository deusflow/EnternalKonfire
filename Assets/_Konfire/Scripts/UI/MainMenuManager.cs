/*
 * MainMenuManager.cs
 * Enhanced dark-fantasy main menu manager with rating/ranking system,
 * score persistence, audio feedback, and clean scene transitions.
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource menuMusicSource; 
    public AudioSource buttonClickSource; 
    
    [Header("Settings")]
    public float loadDelay = 0.35f;

    [Header("Rating & Highscore UI")]
    public TMP_Text highScoreText;
    public TMP_Text rankTitleText;
    public TMP_Text ratingDetailsText;

    private const string PREF_HIGHSCORE = "Konfire_HighScore";

    void Start()
    {
        Time.timeScale = 1f;
        if (menuMusicSource != null && !menuMusicSource.isPlaying)
        {
            menuMusicSource.loop = true;
            menuMusicSource.Play();
        }

        UpdateRatingDisplay();
    }

    public void UpdateRatingDisplay()
    {
        int bestScore = PlayerPrefs.GetInt(PREF_HIGHSCORE, 0);
        string rank = GetRankTitle(bestScore);

        if (highScoreText != null)
        {
            highScoreText.text = $"BEST RECORD: <color=#FFD700>{bestScore} PTS</color>";
        }

        if (rankTitleText != null)
        {
            rankTitleText.text = $"RANK: <color=#FFA500>{rank.ToUpper()}</color>";
        }

        if (ratingDetailsText != null)
        {
            ratingDetailsText.text = bestScore == 0 
                ? "No trials completed yet. Kindle the bonfire to earn your rank!" 
                : $"Honored Keeper of the Hearth | Record: {bestScore} pts";
        }
    }

    public static string GetRankTitle(int score)
    {
        if (score >= 500) return "Lord of Cinder [III]";
        if (score >= 250) return "Pyre Guardian [II]";
        if (score >= 100) return "Flame Keeper [I]";
        if (score >= 30)  return "Ember Tender";
        return "Lost Wanderer";
    }

    public void OnStartGamePressed()
    {
        StartCoroutine(LoadSceneWithSound());
    }

    public void OnQuitGamePressed()
    {
        PlayButtonClickSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadSceneWithSound()
    {
        PlayButtonClickSound();
        yield return new WaitForSecondsRealtime(loadDelay);
        SceneManager.LoadScene("GameScene");
    }

    public void PlayButtonClickSound()
    {
        if (buttonClickSource != null && buttonClickSource.clip != null)
        {
            buttonClickSource.PlayOneShot(buttonClickSource.clip); 
        }
    }
}