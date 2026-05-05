using UnityEngine;
using RoadArchitect;

public class RoadArchitectWaypointGenerator : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The RoadArchitect SplineC component attached to your road.")]
    public SplineC roadSpline;
    [Tooltip("The prefab we will spawn as a waypoint.")]
    public GameObject waypointPrefab;

    [Header("Generation Settings")]
    [Tooltip("Distance between each waypoint along the road (in meters).")]
    public float distanceBetweenWaypoints = 10f;

    [Tooltip("How many lanes exist on EACH side of the center line? (e.g., 2 = 4 lanes total)")]
    public int lanesPerSide = 2;
    [Tooltip("The width of a single lane (in meters).")]
    public float laneWidth = 3.5f;

    [Header("Surface Snapping")]
    [Tooltip("The physics layer your road mesh is on, so we only snap to the road.")]
    public LayerMask roadLayerMask;
    [Tooltip("How high above the calculated point we start the raycast to shoot down.")]
    public float raycastStartHeight = 5f;

    [ContextMenu("Generate Multi-Lane Waypoints")]
    public void GenerateWaypoints()
    {
        if (roadSpline == null || waypointPrefab == null)
        {
            Debug.LogError("Missing references! Please assign the SplineC and Prefab.");
            return;
        }

        // --- 1. SET UP THE FOLDER HIERARCHY ---
        GameObject mainParent = new GameObject(roadSpline.gameObject.name + "_Waypoints");

        GameObject leftSideParent = new GameObject("Left_Side");
        leftSideParent.transform.SetParent(mainParent.transform);

        GameObject rightSideParent = new GameObject("Right_Side");
        rightSideParent.transform.SetParent(mainParent.transform);

        // Arrays to hold the individual lane folders so we can easily assign parents later
        Transform[] leftLaneFolders = new Transform[lanesPerSide];
        Transform[] rightLaneFolders = new Transform[lanesPerSide];

        for (int i = 0; i < lanesPerSide; i++)
        {
            leftLaneFolders[i] = new GameObject("Lane_L" + i).transform;
            leftLaneFolders[i].SetParent(leftSideParent.transform);

            rightLaneFolders[i] = new GameObject("Lane_R" + i).transform;
            rightLaneFolders[i].SetParent(rightSideParent.transform);
        }

        // --- 2. WALK THE SPLINE ---
        float totalLength = roadSpline.distance;
        int waypointIndex = 0; // Our sequential index counter!

        for (float currentDistance = 0; currentDistance <= totalLength; currentDistance += distanceBetweenWaypoints)
        {
            float t = currentDistance / totalLength;

            Vector3 centerPos;
            Vector3 forwardDir;
            roadSpline.GetSplineValueBoth(t, out centerPos, out forwardDir);
            forwardDir = forwardDir.normalized;

            // Find the perfect Right direction
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

            // --- 3. GENERATE WAYPOINTS FOR EVERY LANE ---
            for (int lane = 0; lane < lanesPerSide; lane++)
            {
                // Calculate how far from the center this specific lane is
                // Lane 0 is half a lane width away. Lane 1 is 1.5 lane widths away, etc.
                float currentOffset = (laneWidth / 2f) + (lane * laneWidth);

                // Right Side calculation and placement
                Vector3 rightPos = centerPos + (rightDir * currentOffset);
                string rightName = $"WP_R{lane}_{waypointIndex}"; // e.g., WP_R0_45
                PlaceWaypoint(rightPos, rightLaneFolders[lane], rightName);

                // Left Side calculation and placement
                Vector3 leftPos = centerPos - (rightDir * currentOffset);
                string leftName = $"WP_L{lane}_{waypointIndex}"; // e.g., WP_L1_45
                PlaceWaypoint(leftPos, leftLaneFolders[lane], leftName);
            }

            // Increment the sequence number for the next step forward
            waypointIndex++;
        }

        Debug.Log($"Successfully generated {waypointIndex} waypoints per lane for {roadSpline.gameObject.name}");
    }

    private void PlaceWaypoint(Vector3 rawPosition, Transform parent, string objectName)
    {
        Vector3 raycastStartPos = rawPosition + (Vector3.up * raycastStartHeight);

        if (Physics.Raycast(raycastStartPos, Vector3.down, out RaycastHit hitInfo, raycastStartHeight * 2f, roadLayerMask))
        {
            GameObject newWaypoint = Instantiate(waypointPrefab, hitInfo.point, Quaternion.identity);
            newWaypoint.transform.SetParent(parent);
            newWaypoint.name = objectName; // Applies our dynamic WP_dy_x naming convention
        }
        else
        {
            Debug.LogWarning($"Waypoint {objectName} missed the road at position: {rawPosition}");
        }
    }
}