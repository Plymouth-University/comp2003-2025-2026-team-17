using UnityEngine;
using RoadArchitect;

public class BidirectionalWaypointGenerator : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The RoadArchitect SplineC component attached to your road.")]
    public SplineC roadSpline;
    [Tooltip("The prefab we will spawn as a waypoint. (Ensure its Collider is removed!)")]
    public GameObject waypointPrefab;

    [Header("Generation Settings")]
    [Tooltip("Distance between each waypoint along the road (in meters).")]
    public float distanceBetweenWaypoints = 10f;
    [Tooltip("How many lanes exist on EACH side of the center line?")]
    public int lanesPerSide = 2;
    [Tooltip("The width of a single lane (in meters).")]
    public float laneWidth = 3.5f;

    [Header("Intersection Detection")]
    [Tooltip("How far from the center of the intersection should waypoints stop? (In meters)")]
    public float intersectionClearance = 15f;

    [Header("Surface Snapping")]
    [Tooltip("The physics layer your road mesh is on, so we only snap to the road.")]
    public LayerMask roadLayerMask;
    [Tooltip("How high above the calculated point we start the raycast to shoot down.")]
    public float raycastStartHeight = 5f;

    // Settings for the LaneTrafficManager added to each lane
    [Header("Traffic Manager Settings (Auto-Assigned)")]
    [Tooltip("The AI Car prefab to assign to the generated LaneTrafficManagers.")]
    public GameObject defaultCarPrefab;
    [Tooltip("How many cars should each lane maintain?")]
    public int defaultTargetCarCount = 5;
    [Tooltip("Minimum time (in seconds) between releasing cars.")]
    public float defaultSpawnInterval = 3f;
    [Tooltip("Which layer holds your vehicles? (Used for spawn clearance checks).")]
    public LayerMask defaultObstacleLayer;

    [ContextMenu("Generate Bidirectional Waypoints")]
    public void GenerateWaypoints()
    {
        if (roadSpline == null || waypointPrefab == null)
        {
            Debug.LogError("Missing references! Please assign the SplineC and Prefab.");
            return;
        }

        // --- AUTO CLEANUP ---
        // If you run the script multiple times, it will create multiple sets of waypoints. This ensures a clean slate each time.
        string expectedFolderName = roadSpline.gameObject.name + "_Bidirectional_WPs";
        GameObject oldHierarchy = GameObject.Find(expectedFolderName);
        if (oldHierarchy != null)
        {
            Debug.LogWarning($"An old waypoint hierarchy named '{expectedFolderName}' was found and will be destroyed to prevent clutter.");
            DestroyImmediate(oldHierarchy);
        }

        // --- STEP 1: SET UP THE FOLDER HIERARCHY ---
        // Create a clean hierarchy to prevent hundreds of waypoints from cluttering your scene
        GameObject mainParent = new GameObject(roadSpline.gameObject.name + "_Bidirectional_WPs");

        GameObject leftSideParent = new GameObject("Left_Side_Forward");
        leftSideParent.transform.SetParent(mainParent.transform);

        GameObject rightSideParent = new GameObject("Right_Side_Oncoming");
        rightSideParent.transform.SetParent(mainParent.transform);

        // Arrays to hold the individual lane folders
        Transform[] leftLaneFolders = new Transform[lanesPerSide];
        Transform[] rightLaneFolders = new Transform[lanesPerSide];

        for (int i = 0; i < lanesPerSide; i++)
        {
            // The left lane folder
            // GameObject leftLaneObj = new GameObject("Lane_L" + i);
            leftLaneFolders[i] = new GameObject("Lane_L" + i).transform;
            leftLaneFolders[i].SetParent(leftSideParent.transform);

            //GameObject rightLaneObj = new GameObject("Lane_R" + i);
            rightLaneFolders[i] = new GameObject("Lane_R" + i).transform;
            rightLaneFolders[i].SetParent(rightSideParent.transform);
        }

        // Get the physical length of the road from RoadArchitect
        float totalLength = roadSpline.distance;

        // --- STEP 2: PASS 1 (LEFT SIDE / FORWARD TRAFFIC) ---
        // We walk from 0 to the end of the road.
        int leftIndex = 0;
        for (float currentDist = 5.0f; currentDist <= totalLength; currentDist += distanceBetweenWaypoints)
        {
            float t = currentDist / totalLength;

            // Get the center position and forward direction

            roadSpline.GetSplineValueBoth(t, out Vector3 centerPos, out Vector3 forwardDir);

            // Intersection zone detection
            if (IsInsideIntersectionZone(centerPos))
            {
                // If true, we are too close to intersection. Skip dropping waypoints here.
                Debug.Log($"Skipping waypoint at distance {currentDist} due to intersection proximity.");
                continue;
                //break;
            }

            forwardDir = forwardDir.normalized;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

            for (int lane = 0; lane < lanesPerSide; lane++)
            {
                // Calculate distance from center: inner lane is half a width, next is 1.5 widths, etc.
                float offsetDist = (laneWidth / 2f) + (lane * laneWidth);

                // Left side calculation (Subtracting RightDir pushes it Left)
                Vector3 lanePos = centerPos - (rightDir * offsetDist);
                string wpName = $"WP_L{lane}_{leftIndex}";

                // Place waypoint, facing the normal forward direction
                PlaceWaypoint(lanePos, forwardDir, leftLaneFolders[lane], wpName);
            }
            leftIndex++;
        }

        // --- STEP 3: PASS 2 (RIGHT SIDE / ONCOMING TRAFFIC) ---
        // We start at the totalLength (the end) and walk BACKWARDS down to 0.
        int rightIndex = 0;
        for (float currentDist = totalLength - 5.0f; currentDist >= 0; currentDist -= distanceBetweenWaypoints)
        {
            float t = currentDist / totalLength;

            // Get the center position and forward direction

            roadSpline.GetSplineValueBoth(t, out Vector3 centerPos, out Vector3 forwardDir);

            // Intersection zone detection
            if (IsInsideIntersectionZone(centerPos))
            {
                // If true, we are too close to intersection. Skip dropping waypoints here.
                Debug.Log($"Skipping waypoint at distance {currentDist} due to intersection proximity.");
                continue;
                //break;
            }

            forwardDir = forwardDir.normalized;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

            // CRITICAL DIFFERENCE: Because traffic is oncoming, the cars must face backwards!
            Vector3 oncomingForwardDir = -forwardDir;

            for (int lane = 0; lane < lanesPerSide; lane++)
            {
                float offsetDist = (laneWidth / 2f) + (lane * laneWidth);

                // Right side calculation (Adding RightDir pushes it to the physical right side of the road)
                Vector3 lanePos = centerPos + (rightDir * offsetDist);
                string wpName = $"WP_R{lane}_{rightIndex}";

                // Place waypoint, facing the ONCOMING (reversed) direction
                PlaceWaypoint(lanePos, oncomingForwardDir, rightLaneFolders[lane], wpName);
            }
            rightIndex++;
        }

        for (int i = 0; i < lanesPerSide; i++)
        {
            if (leftLaneFolders[i] != null)
            {
                AttachTrafficManager(leftLaneFolders[i].gameObject);
            }

            if (rightLaneFolders[i] != null)
            {
                AttachTrafficManager(rightLaneFolders[i].gameObject);
            }
        }

        Debug.Log($"Bidirectional generation complete! Left Forward WPs: {leftIndex} | Right Oncoming WPs: {rightIndex}");
    }

    // --- HELPER METHODS ---
    // Determine if a given position is within the intersection clearance zone
    // Scans all nodes in road. If intersection node found, check if current position is inside Intersection Clearance Zone
    private bool IsInsideIntersectionZone(Vector3 currentCenterPos)
    {
        foreach (SplineN node in roadSpline.nodes)
        {
            if (node.isIntersection)
            {
                float distanceToNode = Vector3.Distance(currentCenterPos, node.pos);

                if (distanceToNode <= intersectionClearance)
                {
                    return true; // Current position is too close to an intersection node
                }
            }
        }

        return false; // No intersection nodes are within the clearance distance
    }

    // Responsible for instantiating a waypoint prefab at the correct position and rotation
    // Requires a 'forwardDirection' to rotate the spawned waypoint correctly
    private void PlaceWaypoint(Vector3 rawPosition, Vector3 forwardDirection, Transform parent, string objectName)
    {
        Vector3 raycastStartPos = rawPosition + (Vector3.up * raycastStartHeight);

        if (Physics.Raycast(raycastStartPos, Vector3.down, out RaycastHit hitInfo, raycastStartHeight * 2f, roadLayerMask))
        {
            // Instantiate with LookRotation to make the waypoint "face" the correct travel direction
            Quaternion wpRotation = Quaternion.LookRotation(forwardDirection);
            GameObject newWaypoint = Instantiate(waypointPrefab, hitInfo.point, wpRotation);

            newWaypoint.transform.SetParent(parent);
            newWaypoint.name = objectName;
        }
        else
        {
            Debug.LogWarning($"Waypoint {objectName} missed the road mesh at position: {rawPosition}. Check your Raycast Start Height or Layer Mask.");
        }
    }


    // Method to add and configure a LaneTrafficManager on a given lane GameObject
    private void AttachTrafficManager(GameObject laneObject)
    {
        // AddComponent physically glues the script to the target GameObject
        LaneTrafficManager manager = laneObject.AddComponent<LaneTrafficManager>();

        // Pass the settings from the Generator down into the newly created Manager
        manager.carPrefab = defaultCarPrefab;
        manager.targetCarCount = defaultTargetCarCount;
        manager.spawnInterval = defaultSpawnInterval;
        manager.obstacleLayer = defaultObstacleLayer;
    }
}