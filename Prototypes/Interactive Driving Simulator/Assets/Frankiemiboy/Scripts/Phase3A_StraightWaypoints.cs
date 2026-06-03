using UnityEngine;
using RoadArchitect;
using System.Collections.Generic;

public class Phase3A_StraightWaypoints : MonoBehaviour
{
    [Header("Generation Settings")]
    public GameObject waypointPrefab;
    public float distanceBetweenWaypoints = 10f;
    public int lanesPerSide = 1;
    public float laneWidth = 5.0f;
    public LayerMask roadLayerMask;
    public float raycastStartHeight = 5f;

    [ContextMenu("Execute Phase 3A: Generate Straights")]
    public void GenerateStraights()
    {
        TrafficEdge[] allEdges = FindObjectsByType<TrafficEdge>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        RoadIntersection[] allIntersections = FindObjectsByType<RoadIntersection>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); //

        // Abort early if no edges or intersections found
        if ( allEdges.Length == 0)
        {
            Debug.LogError("No edges found on the Graph. Please ensure you have executed Phase 1 and 2 scripts");
            return;

        }
        if (allIntersections.Length == 0)
        {
            Debug.LogError("No intersections found on the Graph. Please ensure you have executed Phase 1 and 2 scripts");
            return;
        }

        foreach (TrafficEdge edge in allEdges)
        {
            // Prepare the storage arrays in the Edge
            edge.forwardLanes.Clear();
            edge.oncomingLanes.Clear();
            for (int i = 0; i < lanesPerSide; i++)
            {
                edge.forwardLanes.Add(new TrafficLane());
                edge.oncomingLanes.Add(new TrafficLane());
            }

            // Debugging purposes:
            //Debug.LogWarning($"Number of forward lanes in Edge '{edge.name}': {edge.forwardLanes.Count}");
            //Debug.LogWarning($"Number of oncoming lanes in Edge '{edge.name}': {edge.oncomingLanes.Count}");

            // Get the physical start and end distances for this specific chunk of road
            float startDist = edge.parentSpline.nodes[edge.startSplineIndex].time * edge.parentSpline.distance;
            float endDist = edge.parentSpline.nodes[edge.endSplineIndex].time * edge.parentSpline.distance;

            // --- PASS 1: FORWARD LANES ---
            int leftIndex = 0;
            for (float currentDist = startDist; currentDist <= endDist; currentDist += distanceBetweenWaypoints)
            {
                float t = currentDist / edge.parentSpline.distance;
                edge.parentSpline.GetSplineValueBoth(t, out Vector3 centerPos, out Vector3 forwardDir);

                forwardDir = forwardDir.normalized;
                Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

                for (int lane = 0; lane < lanesPerSide; lane++)
                {
                    float offsetDist = (laneWidth / 2f) + (lane * laneWidth);
                    Vector3 lanePos = centerPos - (rightDir * offsetDist);

                    Transform wp = PlaceWaypoint(lanePos, forwardDir, edge.transform, $"WP_{edge.name}_Fwd_L{lane}_{leftIndex}", allIntersections);
                    if (wp != null) edge.forwardLanes[lane].waypoints.Add(wp);
                }
                leftIndex++;
            }

            // --- PASS 2: ONCOMING LANES (Backwards) ---
            int rightIndex = 0;
            for (float currentDist = endDist; currentDist >= startDist; currentDist -= distanceBetweenWaypoints)
            {
                float t = currentDist / edge.parentSpline.distance;
                edge.parentSpline.GetSplineValueBoth(t, out Vector3 centerPos, out Vector3 forwardDir);

                forwardDir = forwardDir.normalized;
                Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
                Vector3 oncomingDir = -forwardDir; // Reverse direction for oncoming traffic

                for (int lane = 0; lane < lanesPerSide; lane++)
                {
                    float offsetDist = (laneWidth / 2f) + (lane * laneWidth);
                    Vector3 lanePos = centerPos + (rightDir * offsetDist);

                    Transform wp = PlaceWaypoint(lanePos, oncomingDir, edge.transform, $"WP_{edge.name}_Onc_L{lane}_{rightIndex}", allIntersections);
                    if (wp != null) edge.oncomingLanes[lane].waypoints.Add(wp);
                }
                rightIndex++;
            }
        }
        Debug.Log("Phase 3A Complete: Straight waypoints generated and perfectly clipped via Raycast!");
    }

    private bool IsInsideAnyIntersection(Vector3 exactPos, RoadIntersection[] intersections)
    {
        // We make a local copy because Contains() method requires 'ref'
        Vector3 checkPos = exactPos;

        foreach (RoadIntersection raIntersection in intersections)
        {
            if (raIntersection.Contains(ref checkPos)) return true; //
        }
        return false;
    }

    private Transform PlaceWaypoint(Vector3 rawPos, Vector3 fwdDir, Transform parent, string wpName, RoadIntersection[] allIntersections)
    {
        Vector3 raycastStart = rawPos + (Vector3.up * raycastStartHeight);

        if (Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hit, raycastStartHeight * 2f, roadLayerMask))
        {
            // First check if the EXACT point on the asphalt is inside the intersection box
            if (IsInsideAnyIntersection(hit.point, allIntersections))
            {
                return null; // Abort instantiation!
            }

            GameObject wp = Instantiate(waypointPrefab, hit.point, Quaternion.LookRotation(fwdDir));
            wp.transform.SetParent(parent);
            wp.name = wpName;
            return wp.transform;
        }

        return null;
    }
}