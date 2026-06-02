using UnityEngine;
using System.Collections.Generic;

public class Phase3B_BezierConnectors : MonoBehaviour
{
    [Header("Bezier Settings")]
    public GameObject waypointPrefab;

    [Tooltip("The MAX distance the curve is pulled straight. It will dynamically shrink for sharp/short turns.")]
    public float maxControlMagnetStrength = 15f;

    [Tooltip("Target distance between waypoints inside the intersection (match with Phase 3A).")]
    public float targetWaypointSpacing = 10f;

    [ContextMenu("Execute Phase 3B: Generate Bezier Curves")]
    public void GenerateConnectors()
    {
        IntersectionNode[] allNodes = FindObjectsByType<IntersectionNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        TrafficEdge[] allEdges = FindObjectsByType<TrafficEdge>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // Abort early if no edges or intersections found
        if (allEdges.Length == 0)
        {
            Debug.LogError("No edges found on the Graph. Please ensure you have executed Phase 1 and 2 scripts");
            return;

        }
        if (allNodes.Length == 0)
        {
            Debug.LogError("No intersection nodes found on the Graph. Please ensure you have executed Phase 1 and 2 scripts");
            return;
        }

        foreach (IntersectionNode node in allNodes)
        {
            if (node.isDeadEnd) continue;

            List<TrafficEdge> incomingEdges = new List<TrafficEdge>();
            foreach (TrafficEdge edge in allEdges)
            {
                if (edge.endNode == node) incomingEdges.Add(edge);
                if (edge.startNode == node) incomingEdges.Add(edge);
            }

            foreach (TrafficEdge incoming in incomingEdges)
            {
                bool isIncomingForward = (incoming.endNode == node);
                var incomingLanes = isIncomingForward ? incoming.forwardLanes : incoming.oncomingLanes;

                foreach (Route route in node.routingTable)
                {
                    TrafficEdge outgoing = route.edgeToTake;
                    if (incoming == outgoing) continue;

                    bool isOutgoingForward = (outgoing.startNode == node);
                    var outgoingLanes = isOutgoingForward ? outgoing.forwardLanes : outgoing.oncomingLanes;

                    GameObject turnFolder = new GameObject($"Turn_{incoming.name}_to_{outgoing.name}");
                    turnFolder.transform.SetParent(node.transform);

                    int lanesToConnect = Mathf.Min(incomingLanes.Count, outgoingLanes.Count);
                    for (int lane = 0; lane < lanesToConnect; lane++)
                    {
                        if (incomingLanes[lane].Count == 0 || outgoingLanes[lane].Count == 0) continue;

                        Transform startWP = incomingLanes[lane][incomingLanes[lane].Count - 1];
                        Transform endWP = outgoingLanes[lane][0];

                        DrawBezierCurve(startWP, endWP, turnFolder.transform, lane);
                    }
                }
            }
        }
        Debug.Log("Phase 3B Complete: Dynamic Bezier curves generated!");
    }

    private void DrawBezierCurve(Transform startWP, Transform endWP, Transform parentFolder, int laneIndex)
    {
        Vector3 p0 = startWP.position;
        Vector3 p3 = endWP.position;

        // --- NEW: DYNAMIC DISTANCE CALCULATION ---
        float distance = Vector3.Distance(p0, p3);

        // 1. Fix the Loop: The magnet can never exceed 40% of the physical distance!
        float dynamicMagnet = Mathf.Min(maxControlMagnetStrength, distance * 0.4f);

        Vector3 p1 = p0 + (startWP.forward * dynamicMagnet);
        Vector3 p2 = p3 - (endWP.forward * dynamicMagnet);

        // 2. Fix the Waypoint Clutter: Calculate how many points we actually need based on distance
        int calculatedWaypoints = Mathf.RoundToInt(distance / targetWaypointSpacing);

        // 3. The Acute Angle Rule: Even if it's a super short gap, drop at least 1 point in the middle so the AI has a target
        if (calculatedWaypoints < 1) calculatedWaypoints = 1;

        // --- NEW: EVEN SPACING MATH ---
        // We divide by (calculatedWaypoints + 1) so the points sit evenly inside the gap, 
        // without physically overlapping the exact StartWP or EndWP.
        for (int i = 1; i <= calculatedWaypoints; i++)
        {
            float t = i / (float)(calculatedWaypoints + 1);

            Vector3 curvePos = CalculateBezierPoint(t, p0, p1, p2, p3);

            float nextT = Mathf.Clamp01(t + 0.05f);
            Vector3 nextPos = CalculateBezierPoint(nextT, p0, p1, p2, p3);
            Vector3 direction = (nextPos - curvePos).normalized;

            if (direction == Vector3.zero) direction = startWP.forward;

            GameObject newWP = Instantiate(waypointPrefab, curvePos, Quaternion.LookRotation(direction));
            newWP.transform.SetParent(parentFolder);
            newWP.name = $"Curve_L{laneIndex}_{i}";
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 point = uuu * p0;
        point += 3 * uu * t * p1;
        point += 3 * u * tt * p2;
        point += ttt * p3;
        return point;
    }
}