/*
 * SceneNavigation.cs
 * This script is like our "doorman" that opens the doors between scenes.
 * It should be attached to manager objects in MainMenu and GameScene.
 *
 * Comments are written by a student for other students! :)
 */

using UnityEngine;
using UnityEngine.SceneManagement; // Needed for scene management!

public class SceneNavigation : MonoBehaviour
{
    // --- PUBLIC METHODS FOR BUTTONS ---

    // This method is called by the "Start Game" button in the main menu.
    public void StartGame()
    {
        // Loads the main game scene.
        // IMPORTANT: Make sure your game scene is named "GameScene".
        // If it's called something else, just change the name here.
        SceneManager.LoadScene("GameScene");
    }

    // This method is called by the "Restart" button on the Game Over screen.
    public void RestartGame()
    {
        // This is the safest way to reload the CURRENT active scene.
        // It doesn't even need to know the scene's name.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // This method is called by the "Main Menu" button on the Game Over screen.
    public void LoadMainMenu()
    {
        // Takes us back to the main menu (scene with index 0 in Build Settings)
        SceneManager.LoadScene("MainMenu");
    }

    // Bonus method for a "Quit" button (if you want to add one)
    public void QuitGame()
    {
        // In the Unity editor, this log just shows the button works
        Debug.Log("Player pressed 'Quit'. In a built game, the app will close.");
        // This command closes the .exe file of the game (doesn't work in editor)
        Application.Quit();
    }
}