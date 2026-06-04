using UnityEngine;
using System.Collections.Generic;

public class YieldSignManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the StopSign_StopLine GameObject into this slot.")]
    public GameObject stopLineBarrier;

    [Header("Layer Swapping")]
    public string stopLayerName = "StopLine";
    public string goLayerName = "Ignore Raycast";

    [Header("Detection")]
    [Tooltip("The layer your cars are on. Used to ignore pedestrians or environment objects.")]
    public LayerMask vehicleLayer;

    private int stopLayerId;
    private int goLayerId;

    // This list tracks every vehicle currently inside this Danger Zone
    private List<Collider> carsInZone = new List<Collider>();

    void Start()
    {
        stopLayerId = LayerMask.NameToLayer(stopLayerName);
        goLayerId = LayerMask.NameToLayer(goLayerName);

        if (stopLayerId == -1 || goLayerId == -1)
        {
            Debug.LogError($"Layer names are invalid on {gameObject.name}.");
            return;
        }

        if (stopLineBarrier == null)
        {
            Debug.LogError($"You forgot to assign the Stop Line Barrier on {gameObject.name}!");
            return;
        }

        // Default to STOP just to be safe
        stopLineBarrier.layer = stopLayerId;
    }

    // A car enters the Danger Zone
    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering is actually on the vehicleLayer
        if (((1 << other.gameObject.layer) & vehicleLayer) != 0)
        {
            if (!carsInZone.Contains(other))
            {
                carsInZone.Add(other);
            }
        }
    }

    // A car leaves the Danger Zone 
    void OnTriggerExit(Collider other)
    {
        if (carsInZone.Contains(other))
        {
            carsInZone.Remove(other);
        }
    }

    void Update()
    {
        if (stopLineBarrier == null) return;

        // Remove any destroyed cars to prevent ghost triggers
        carsInZone.RemoveAll(item => item == null);

        // --- THE LOGIC ---
        if (carsInZone.Count > 0)
        {
            // There are cars in the danger zone. Keep the Stop Line active!
            stopLineBarrier.layer = stopLayerId;
        }
        else
        {
            // The list is empty. The road is clear! Lower the Stop Line.
            stopLineBarrier.layer = goLayerId;
        }
    }
}