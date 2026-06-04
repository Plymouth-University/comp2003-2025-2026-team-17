using UnityEngine;

public class YieldSensor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the StopSign_StopLine here")]
    public YieldManager masterManager;

    [Header("Settings")]
    public LayerMask vehicleLayer; // Set this to your AI car layer

    void OnTriggerEnter(Collider other)
    {
        // If the object entering is a vehicle...
        if (((1 << other.gameObject.layer) & vehicleLayer) != 0)
        {
            // Tell the master script to add it!
            if (masterManager != null) masterManager.AddVehicle(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // If the object leaving is a vehicle...
        if (((1 << other.gameObject.layer) & vehicleLayer) != 0)
        {
            // Tell the master script to remove it!
            if (masterManager != null) masterManager.RemoveVehicle(other);
        }
    }
}