using UnityEngine;

public class LeftLanesTrafficManager : MonoBehaviour
{
    [Header("Traffic Settings")]
    [Tooltip("The AI Car prefab you want to spawn in this specific lane.")]
    public GameObject carPrefab;

    // We use a private array to hold the waypoints we gather from this folder
    private Transform[] laneWaypoints;

    void Start()
    {
        // 1. As soon as the game starts, gather all the waypoints in this lane folder
        GatherWaypoints();

        // 2. Safety check: Only spawn a car if we actually have waypoints and a prefab assigned
        if (laneWaypoints.Length > 0 && carPrefab != null)
        {
            SpawnAndSetupCar();
        }
        else
        {
            Debug.LogWarning($"Lane {gameObject.name} is missing its car prefab or has no waypoints!");
        }
    }

    private void GatherWaypoints()
    {
        // Find out exactly how many children (waypoints) are inside this folder
        int childCount = transform.childCount;

        // Initialize our array to be exactly that size
        laneWaypoints = new Transform[childCount];

        // Loop through every child in order (WP_0, WP_1, WP_2, etc.) and add it to the array
        for (int i = 0; i < childCount; i++)
        {
            laneWaypoints[i] = transform.GetChild(i);
        }
    }

    private void SpawnAndSetupCar()
    {
        // Instantiate (spawn) the car at the exact position and rotation of the very first waypoint
        GameObject spawnedCar = Instantiate(carPrefab, laneWaypoints[0].position, laneWaypoints[0].rotation);

        // Grab the AI brain (SimpleWaypointFollower script) from the newly spawned car
        SimpleWaypointFollower aiBrain = spawnedCar.GetComponent<SimpleWaypointFollower>();

        if (aiBrain != null)
        {
            // --- THE BATON PASS ---
            // We inject the array of waypoints we just gathered directly into the car's brain!
            aiBrain.waypoints = laneWaypoints;
        }
        else
        {
            Debug.LogError($"The car prefab spawned in {gameObject.name} is missing the SimpleWaypointFollower script!");
        }
    }
}