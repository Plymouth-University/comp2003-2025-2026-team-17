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
        SceneManager.LoadScene("Scenario 1");
        unfreezeTime();
    }
    public void OnScenario2Clicked()
    {
        SceneManager.LoadScene("Scenario 2");
        unfreezeTime();
    }

    void unfreezeTime()
    {
        Time.timeScale = 1.0f;
    }

   
}
