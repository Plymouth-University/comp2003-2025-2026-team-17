using UnityEngine;
using System.Collections;

public class TrafficLightSync : MonoBehaviour
{
    [Header("Traffic Light References")]
    [Tooltip("The actual Light component that turns ON when the light is Green.")]
    public Light greenLightComponent;

    [Header("Layer Swapping")]
    [Tooltip("The name of the layer that acts as an invisible wall (e.g., 'StopLine').")]
    public string stopLayerName = "StopLine";
    [Tooltip("The name of the layer the cars will ignore (e.g., 'Ignore Raycast').")]
    public string goLayerName = "Ignore Raycast";

    // Internal trackers
    private int stopLayerId;
    private int goLayerId;
    private WaitForSeconds checkInterval;

    void Start()
    {
        // Convert the string names to Unity's internal integer Layer IDs for fast swapping
        stopLayerId = LayerMask.NameToLayer(stopLayerName);
        Debug.Log($"Stop Layer '{stopLayerName}' has ID: {stopLayerId}");
        goLayerId = LayerMask.NameToLayer(goLayerName);
        Debug.Log($"Go Layer '{goLayerName}' has ID: {goLayerId}");

        if (stopLayerId == -1 || goLayerId == -1)
        {
            Debug.LogError($"Layer names are invalid on {gameObject.name}. Please ensure you created them in the Unity tags/layers menu.");
            return;
        }

        if (greenLightComponent == null)
        {
            Debug.LogError($"Missing references on {gameObject.name}. Please assign the Green Light GameObject.");
            return;
        }

        // We check the light state 5 times a second (0.2s). 
        checkInterval = new WaitForSeconds(0.2f);
        StartCoroutine(SyncWithTrafficLight());
    }

    private IEnumerator SyncWithTrafficLight()
    {
        while (true)
        {
            Debug.Log($"Checking traffic light state for {gameObject.name}... Green Light Active: {greenLightComponent.enabled}");
            // We simply check if the green light GameObject is currently turned on!
            if (greenLightComponent.enabled && greenLightComponent.intensity > 0.01f)
            {
                // Light is Green: Remove the invisible wall
                gameObject.layer = goLayerId;
            }
            else
            {
                // Light is Red or Amber: Raise the invisible wall
                gameObject.layer = stopLayerId;
            }

            // Wait 0.2 seconds before checking again
            yield return checkInterval;
        }
    }
}