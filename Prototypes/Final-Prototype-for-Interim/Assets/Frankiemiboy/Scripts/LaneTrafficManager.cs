using System.Collections.Generic; // Needed for the Queue system
using UnityEngine;

public class LaneTrafficManager : MonoBehaviour
{
    [Header("Traffic Settings")]
    [Tooltip("The AI Car prefab you want to spawn in this specific lane.")]
    public GameObject carPrefab;
    [Tooltip("How many cars should this lane maintain in total?")]
    public int targetCarCount = 5;
    [Tooltip("Minimum time (in seconds) between releasing cars onto the road.")]
    public float spawnInterval = 3f;

    [Header("Safety Checks")]
    [Tooltip("The radius of the invisible sphere used to check if the spawn point is clear.")]
    public float clearanceRadius = 4f;
    [Tooltip("Which layer holds your vehicles? (Used to check for traffic jams at the spawn).")]
    public LayerMask obstacleLayer;

    // Internal State Trackers
    private Transform[] laneWaypoints;
    private int currentCarCount = 0;
    private float timeSinceLastSpawn = 0f;

    // --- THE BUFFER ---
    // This queue holds cars waiting to restart their route
    private Queue<GameObject> carBuffer = new Queue<GameObject>();

    void Start()
    {
        // 1. As soon as the game starts, gather all the waypoints in this lane folder
        GatherWaypoints();

        // 2. // Start the timer at the maximum so it tries to spawn the very first car immediately
        timeSinceLastSpawn = spawnInterval;
    }

    void Update()
    {
        if (laneWaypoints == null || laneWaypoints.Length == 0 || carPrefab == null) return;

        // 1. Run the stopwatch
        timeSinceLastSpawn += Time.deltaTime;

        // 2. Has enough time passed to release a car?
        if (timeSinceLastSpawn >= spawnInterval)
        {
            // 3. Is the physical runway clear?
            if (IsSpawnPointClear())
            {
                // 4A. If we haven't hit our quota, spawn a brand new car.
                if (currentCarCount < targetCarCount)
                {
                    SpawnAndSetupNewCar();
                }
                // 4B. If quota is met, check if there are any cars waiting in the invisible buffer!
                else if (carBuffer.Count > 0)
                {
                    ReleaseCarFromBuffer();
                }
            }
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

    private bool IsSpawnPointClear()
    {
        // We raise the check point slightly above the ground so it hits the car's body, not the asphalt.
        Vector3 checkPosition = laneWaypoints[0].position + (Vector3.up * 1f);

        // CheckSphere returns TRUE if it hits an obstacle. We want to return TRUE if it is CLEAR.
        return !Physics.CheckSphere(checkPosition, clearanceRadius, obstacleLayer);
    }

    private void SpawnAndSetupNewCar()
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
            aiBrain.myManager = this;
        }
        else
        {
            Debug.LogError($"The car prefab spawned in {gameObject.name} is missing the SimpleWaypointFollower script!");
        }

        currentCarCount++;
        timeSinceLastSpawn = 0f; // Reset the timer
    }

    public void CarFinishedRoute(GameObject car)
    {
        // 1. Turn the car invisible and turn off its physics
        car.SetActive(false);

        // 2. Put it in the back of the waiting line
        carBuffer.Enqueue(car);
    }

    private void ReleaseCarFromBuffer()
    {
        // 1. Pull the first car out of the front of the line
        GameObject car = carBuffer.Dequeue();

        // 2. Teleport it to the starting line
        car.transform.position = laneWaypoints[0].position;
        car.transform.rotation = laneWaypoints[0].rotation;

        // 3. Reset its brain (so it targets waypoint 0 and starts at 0 speed)
        SimpleWaypointFollower aiBrain = car.GetComponent<SimpleWaypointFollower>();
        if (aiBrain != null)
        {
            aiBrain.ResetToStart();
        }

        // 4. Turn it back on so it can drive!
        car.SetActive(true);
        timeSinceLastSpawn = 0f; // Reset the timer
    }

    // Draw the invisible safety sphere in the Scene view so you can adjust its size!
    void OnDrawGizmos()
    {
        if (transform.childCount > 0)
        {
            Transform firstWP = transform.GetChild(0);
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Transparent green
            Gizmos.DrawSphere(firstWP.position + (Vector3.up * 1f), clearanceRadius);
        }
    }
}