using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Which slide should this show? (0 = first slide, 1 = second slide, etc.)")]
    public int slideIndexToTrigger;

    [Tooltip("The tag required to activate this trigger.")]
    public string playerTag = "Player";

    private TutorialManager tutorialManager;

    private void Start()
    {
        // Automatically find the Tutorial Manager in the scene
        tutorialManager = FindAnyObjectByType<TutorialManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing entering the trigger is the Player
        if (other.CompareTag(playerTag))
        {
            if (tutorialManager != null)
            {
                // Tell the manager to show the assigned slide
                tutorialManager.ShowSlide(slideIndexToTrigger);
            }

            // Disable this specific collider so it can never be triggered again
            GetComponent<Collider>().enabled = false;
        }
    }
}