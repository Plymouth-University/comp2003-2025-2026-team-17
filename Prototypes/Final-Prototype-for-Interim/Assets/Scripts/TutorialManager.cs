using UnityEngine;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Slides")]
    [Tooltip("Drag your Slide empty GameObjects here in order.")]
    public GameObject[] slides;

    [SerializeField] private GameObject readTheTutorialPrompt;

    [SerializeField] private float freezeTimeOnNewSlide = 7f;

    private void Start()
    {
        // When the scene loads, immediately show the first slide (index 0)
        ShowSlide(0);
    }

    public void ShowSlide(int slideIndex)
    {
        // Safety check to prevent errors if you type a wrong number
        if (slideIndex < 0 || slideIndex >= slides.Length)
        {
            Debug.LogWarning("Tutorial Manager: Slide index out of bounds!");
            return;
        }

        // Loop through the list of slides. 
        // Turn ON the requested one, turn OFF all the others.
        for (int i = 0; i < slides.Length; i++)
        {
            slides[i].SetActive(i == slideIndex);
        }

        // corutine to pause game for x seconds
        StartCoroutine(PauseGameRoutine(freezeTimeOnNewSlide));
    }

    // corutine
    private IEnumerator PauseGameRoutine(float duration)
    {
        Time.timeScale = 0.1f;
        readTheTutorialPrompt.SetActive(true);
        AudioListener.volume = 0.2f;
        // Wait for real-world seconds (ignores the frozen timeScale)
        yield return new WaitForSecondsRealtime(duration);

        readTheTutorialPrompt.SetActive(false);
        AudioListener.volume = 1f;
        Time.timeScale = 1f;
    }
}