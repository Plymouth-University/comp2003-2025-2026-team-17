using UnityEngine;
using System.Collections.Generic;

public class YieldManager : MonoBehaviour
{
    [Header("Settings")]
    public string stopLayerName = "StopLine";
    public string goLayerName = "Ignore Raycast";

    private int stopLayerId;
    private int goLayerId;
    private List<Collider> vehiclesInZone = new List<Collider>();

    void Start()
    {
        stopLayerId = LayerMask.NameToLayer(stopLayerName);
        goLayerId = LayerMask.NameToLayer(goLayerName);

        // Default to STOP
        gameObject.layer = stopLayerId;
    }

    // The Sensors call this when a car enters
    public void AddVehicle(Collider car)
    {
        if (!vehiclesInZone.Contains(car)) vehiclesInZone.Add(car);
    }

    // The Sensors call this when a car leaves
    public void RemoveVehicle(Collider car)
    {
        if (vehiclesInZone.Contains(car)) vehiclesInZone.Remove(car);
    }

    void Update()
    {
        // Clean up any destroyed cars
        vehiclesInZone.RemoveAll(item => item == null);

        // --- THE LOGIC YOU REQUESTED ---
        // If the master list has ANY cars from ANY sensor, it stays STOP.
        if (vehiclesInZone.Count > 0)
        {
            gameObject.layer = stopLayerId;
        }
        else
        {
            gameObject.layer = goLayerId;
        }
    }
}