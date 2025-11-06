/*
 * MainMenuManager.cs
 * FINAL VERSION (with coroutine for sound)
 *
 * This script manages the main menu, music, and button sounds.
 * Comments are written by a student for other students!
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Needed for coroutines (IEnumerator)

public class MainMenuManager : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource menuMusicSource; 
    public AudioSource buttonClickSource; 
    
    [Header("Settings")]
    public float loadDelay = 0.5f; // Delay in seconds (0.5s) so the sound can play

    void Start()
    {
        Time.timeScale = 1f;
        if (menuMusicSource != null)
        {
            menuMusicSource.loop = true;
            menuMusicSource.Play();
        }
    }

    // --- NEW BUTTON METHOD ---
    // This function is linked to the "Start Game" button
    public void OnStartGamePressed()
    {
        // We don't load the scene right away. We START a coroutine (timer).
        StartCoroutine(LoadSceneWithSound());
    }

    // --- NEW COROUTINE (TIMER) ---
    private IEnumerator LoadSceneWithSound()
    {
        // 1. Play the button sound
        if (buttonClickSource != null && buttonClickSource.clip != null)
        {
            buttonClickSource.PlayOneShot(buttonClickSource.clip); 
        }
        // 2. Wait for the sound to finish (doesn't freeze the whole game)
        yield return new WaitForSeconds(loadDelay);
        // 3. Load the game scene
        Debug.Log("SceneManager: Loading GameScene...");
        SceneManager.LoadScene("GameScene");
    }
}