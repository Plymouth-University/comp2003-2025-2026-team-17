using UnityEngine;
using RoadArchitect;
using System.Collections.Generic;

public class Phase1_GraphDiscovery : MonoBehaviour
{
    [Header("References")]
    public GameObject nodeMarkerPrefab; // Must have the IntersectionNode.cs script attached!
    public LayerMask roadLayermask; // Used to ensure our raycasts only hit the road when validating intersection positions
                                    // This will also prevent the cars from going beyond the road boundaries

    // A temporary map to help us link RoadArchitect's math nodes to our new AI nodes
    private Dictionary<SplineN, IntersectionNode> mathToAiNodeMap = new Dictionary<SplineN, IntersectionNode>();

    [ContextMenu("Execute Phase 1: Discover Graph")]
    public void DiscoverGraph()
    {
        mathToAiNodeMap.Clear();
        GameObject graphParent = new GameObject("AI_Traffic_Graph");

        // --- STEP 1: FIND ALL INTERSECTIONS ---
        RoadIntersection[] allIntersections = FindObjectsByType<RoadIntersection>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (RoadIntersection raIntersection in allIntersections)
        {
            // Check if the intersection's position is valid by raycasting down to the road layer
            if (!IsValidIntersectionPosition(raIntersection.transform.position))
            {
                Debug.LogWarning($"Skipping intersection '{raIntersection.name}' at {raIntersection.transform.position} because it doesn't appear to be above a valid road.");
                continue;
            }

            // Spawn our AI marker exactly at the blue gizmo's position
            GameObject newMarker = Instantiate(nodeMarkerPrefab, raIntersection.transform.position, Quaternion.identity);
            newMarker.transform.SetParent(graphParent.transform);

            IntersectionNode aiNode = newMarker.GetComponent<IntersectionNode>();
            aiNode.nodeID = "Intersection_" + raIntersection.name;
            aiNode.name = aiNode.nodeID;

            // Link the RoadArchitect math nodes to our new AI Node
            mathToAiNodeMap.Add(raIntersection.node1, aiNode); //
            mathToAiNodeMap.Add(raIntersection.node2, aiNode); //
        }

        // --- STEP 2: FIND ALL ROADS AND SLICE THEM INTO EDGES ---
        SplineC[] allRoads = FindObjectsByType<SplineC>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); //

        foreach (SplineC road in allRoads)
        {
            IntersectionNode lastFoundNode = null;
            int lastFoundIndex = 0;

            // Walk the spline mathematically
            for (int i = 0; i < road.nodes.Count; i++) //
            {
                SplineN currentNode = road.nodes[i]; //

                // Skip invalid nodes (This can happen if the road was edited in a way that broke the spline, but we still want to salvage the rest of the graph)
                if (!IsValidIntersectionPosition(currentNode.pos))
                {
                    Debug.LogWarning($"Skipping node index {i} on road '{road.name}' because it doesn't appear to be above a valid road position.");
                    continue;
                }

                // Is this node an intersection? Or is it the very end/start of the road (Dead End)?
                bool isIntersection = currentNode.isIntersection; //
                bool isDeadEnd = (i == 0 || i == road.nodes.Count - 1);

                if (isIntersection || isDeadEnd)
                {
                    IntersectionNode currentAiNode = null;

                    // If it's an intersection, grab the marker we spawned in Step 1
                    if (isIntersection && mathToAiNodeMap.ContainsKey(currentNode))
                    {
                        currentAiNode = mathToAiNodeMap[currentNode];
                    }
                    // If it's a dead end, we need to spawn a new marker for it
                    else if (isDeadEnd)
                    {
                        GameObject deadEndMarker = Instantiate(nodeMarkerPrefab, currentNode.pos, Quaternion.identity); //
                        deadEndMarker.transform.SetParent(graphParent.transform);
                        currentAiNode = deadEndMarker.GetComponent<IntersectionNode>();
                        currentAiNode.nodeID = "DeadEnd_" + road.name + "_Index_" + i;
                        currentAiNode.isDeadEnd = true;
                    }

                    // --- CREATE THE EDGE ---
                    // If we previously found a node on this road, create an Edge connecting them!
                    if (lastFoundNode != null && currentAiNode != null)
                    {
                        CreateTrafficEdge(lastFoundNode, currentAiNode, road, lastFoundIndex, i, graphParent.transform);
                    }

                    // Update our tracker to look for the next edge
                    lastFoundNode = currentAiNode;
                    lastFoundIndex = i;
                }
            }
        }
        Debug.Log("Phase 1 Complete: Nodes Discovered and Edges Defined.");
    }

    private void CreateTrafficEdge(IntersectionNode start, IntersectionNode end, SplineC spline, int startIndex, int endIndex, Transform parent)
    {
        GameObject edgeObj = new GameObject($"Edge_{start.nodeID}_to_{end.nodeID}");
        edgeObj.transform.SetParent(parent);

        TrafficEdge newEdge = edgeObj.AddComponent<TrafficEdge>();
        newEdge.startNode = start;
        newEdge.endNode = end;
        newEdge.parentSpline = spline;
        newEdge.startSplineIndex = startIndex;
        newEdge.endSplineIndex = endIndex;
    }

    // Check if a position is valid for an intersection by raycasting down to the road layer
    private bool IsValidIntersectionPosition(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 10f, Vector3.down);
        return Physics.Raycast(ray, out RaycastHit hit, 20f, roadLayermask);
    }
}