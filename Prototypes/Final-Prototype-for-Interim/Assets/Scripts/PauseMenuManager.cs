using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    // A slot to drag our Pause Menu panel into
    public GameObject pauseMenuObject;

    // A flag to keep track if we are paused
    private bool isPaused = false;

    // Update is called once per frame
    void Update()
    {
        // Check if the player presses Escape
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }

        }
    }

    void PauseGame()
    {
        Debug.Log("PauseGame function ran");

        // Show the cursor so we can click buttons
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        //Turn on the PauseMenu
        pauseMenuObject.SetActive(true);
        isPaused = true;

        //Freeze time
        Time.timeScale = 0f;
    }

    void ResumeGame()
    {
        Debug.Log("ResumeGame function ran");
        // Hide the cursor 
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        //Turn on the PauseMenu
        pauseMenuObject.SetActive(false);
        isPaused = false;

        //Freeze time
        Time.timeScale = 1f;
    }

    /// <summary>
    /// This part is for the buttons
    /// </summary>
    public void OnPauseMenuResumeClicked()
    {
        ResumeGame();
    }
    public void OnPauseMenuLevelSelectionClicked()
    {
        SceneManager.LoadScene("Level Selection Scene");
    }
    public void OnPauseMenuHomeClicked()
    {
        SceneManager.LoadScene("Title Screen");
    }
}
