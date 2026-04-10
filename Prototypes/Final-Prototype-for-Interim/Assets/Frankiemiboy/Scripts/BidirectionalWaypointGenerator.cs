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

    [Header("Surface Snapping")]
    [Tooltip("The physics layer your road mesh is on, so we only snap to the road.")]
    public LayerMask roadLayerMask;
    [Tooltip("How high above the calculated point we start the raycast to shoot down.")]
    public float raycastStartHeight = 5f;

    [ContextMenu("Generate Bidirectional Waypoints")]
    public void GenerateWaypoints()
    {
        if (roadSpline == null || waypointPrefab == null)
        {
            Debug.LogError("Missing references! Please assign the SplineC and Prefab.");
            return;
        }

        // --- STEP 1: SET UP THE FOLDER HIERARCHY ---
        // We create a clean hierarchy to prevent hundreds of waypoints from cluttering your scene
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
            leftLaneFolders[i] = new GameObject("Lane_L" + i).transform;
            leftLaneFolders[i].SetParent(leftSideParent.transform);

            rightLaneFolders[i] = new GameObject("Lane_R" + i).transform;
            rightLaneFolders[i].SetParent(rightSideParent.transform);
        }

        // Get the physical length of the road from RoadArchitect
        float totalLength = roadSpline.distance;

        // --- STEP 2: PASS 1 (LEFT SIDE / FORWARD TRAFFIC) ---
        // We walk from 0 to the end of the road.
        int leftIndex = 0;
        for (float currentDist = 0; currentDist <= totalLength; currentDist += distanceBetweenWaypoints)
        {
            float t = currentDist / totalLength;

            // Get the center position and forward direction
            roadSpline.GetSplineValueBoth(t, out Vector3 centerPos, out Vector3 forwardDir);
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
        for (float currentDist = totalLength; currentDist >= 0; currentDist -= distanceBetweenWaypoints)
        {
            float t = currentDist / totalLength;

            // Get the center position and forward direction
            roadSpline.GetSplineValueBoth(t, out Vector3 centerPos, out Vector3 forwardDir);
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

        Debug.Log($"Bidirectional generation complete! Left Forward WPs: {leftIndex} | Right Oncoming WPs: {rightIndex}");
    }

    // Helper function now requires a 'forwardDirection' to rotate the spawned waypoint correctly
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
}