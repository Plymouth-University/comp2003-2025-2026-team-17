using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    public void OnPlayClicked() // triggers when "Play" is pressed in the Start Menu
    {
        SceneManager.LoadScene("Level Selection Scene");
    }

    public void OnScenario1Clicked()
    {
        SceneManager.LoadScene("Tutorial Road Scene");
        unfreezeTime();
    }
    public void OnScenario2Clicked()
    {
        SceneManager.LoadScene("Road Network");
        unfreezeTime();
    }

    void unfreezeTime()
    {
        Time.timeScale = 1.0f;
    }

   
}
