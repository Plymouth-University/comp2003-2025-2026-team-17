using UnityEngine;
using System.Collections.Generic;

public class Phase4A_RouteBaker : MonoBehaviour
{
    [Header("A* Settings")]
    public int maxAlternateRoutes = 3;
    public float detourPenaltyMultiplier = 10f; // Forces A* to look for alternative streets

    [ContextMenu("Execute Phase 4A: Bake Route Cache")]
    public void BakeRoutes()
    {
        RouteCache cache = GetComponent<RouteCache>();
        if (cache == null) cache = gameObject.AddComponent<RouteCache>();
        cache.allCachedRoutes.Clear();

        IntersectionNode[] allNodes = FindObjectsByType<IntersectionNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // 1. Find all Dead Ends (our spawn/despawn points)
        List<IntersectionNode> deadEnds = new List<IntersectionNode>();
        foreach (IntersectionNode node in allNodes)
        {
            if (node.isDeadEnd) deadEnds.Add(node);
        }

        // 2. Calculate paths from every Dead End to every OTHER Dead End
        int routesBaked = 0;
        foreach (IntersectionNode startNode in deadEnds)
        {
            foreach (IntersectionNode targetNode in deadEnds)
            {
                if (startNode == targetNode) continue;

                RouteGroup newGroup = new RouteGroup { startNode = startNode, targetNode = targetNode };

                // 3. The Cost Penalty Loop (Finds Top 3 Routes)
                Dictionary<TrafficEdge, float> costPenalties = new Dictionary<TrafficEdge, float>();

                for (int i = 0; i < maxAlternateRoutes; i++)
                {
                    CachedRoute route = FindAStarPath(startNode, targetNode, costPenalties);
                    if (route != null && route.path.Count > 0)
                    {
                        newGroup.alternateRoutes.Add(route);

                        // Penalize the edges used so the next loop finds a different path!
                        foreach (DirectedEdge dEdge in route.path)
                        {
                            if (!costPenalties.ContainsKey(dEdge.edge)) costPenalties[dEdge.edge] = 1f;
                            costPenalties[dEdge.edge] *= detourPenaltyMultiplier;
                        }
                    }
                    else break; // No more possible routes found
                }

                if (newGroup.alternateRoutes.Count > 0)
                {
                    cache.allCachedRoutes.Add(newGroup);
                    routesBaked++;
                }
            }
        }
        Debug.Log($"Phase 4A Complete! Baked {routesBaked} Route Groups into the Cache.");
    }

    // --- Standard A* Pathfinding Implementation ---
    private CachedRoute FindAStarPath(IntersectionNode start, IntersectionNode target, Dictionary<TrafficEdge, float> penalties)
    {
        List<IntersectionNode> openSet = new List<IntersectionNode> { start };
        Dictionary<IntersectionNode, IntersectionNode> cameFrom = new Dictionary<IntersectionNode, IntersectionNode>();
        Dictionary<IntersectionNode, DirectedEdge> edgeTaken = new Dictionary<IntersectionNode, DirectedEdge>();

        Dictionary<IntersectionNode, float> gScore = new Dictionary<IntersectionNode, float>();
        gScore[start] = 0;

        Dictionary<IntersectionNode, float> fScore = new Dictionary<IntersectionNode, float>();
        fScore[start] = Vector3.Distance(start.transform.position, target.transform.position);

        while (openSet.Count > 0)
        {
            IntersectionNode current = GetLowestFScore(openSet, fScore);
            if (current == target) return ReconstructPath(cameFrom, edgeTaken, current);

            openSet.Remove(current);

            foreach (Route route in current.routingTable)
            {
                IntersectionNode neighbor = route.destination;
                TrafficEdge edge = route.edgeToTake;

                // Get physical length, apply penalty if this edge was used in a previous route iteration
                float edgeCost = edge.parentSpline.distance;
                if (penalties.ContainsKey(edge)) edgeCost *= penalties[edge];

                float tentativeGScore = (gScore.ContainsKey(current) ? gScore[current] : Mathf.Infinity) + edgeCost;

                if (tentativeGScore < (gScore.ContainsKey(neighbor) ? gScore[neighbor] : Mathf.Infinity))
                {
                    cameFrom[neighbor] = current;

                    // Determine if we are driving forward or oncoming on this edge
                    bool isForward = (edge.endNode == neighbor);
                    edgeTaken[neighbor] = new DirectedEdge { edge = edge, isForward = isForward };

                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Vector3.Distance(neighbor.transform.position, target.transform.position);

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }
        return null; // Path not found
    }

    private IntersectionNode GetLowestFScore(List<IntersectionNode> openSet, Dictionary<IntersectionNode, float> fScore)
    {
        IntersectionNode lowestNode = openSet[0];
        float lowestScore = fScore.ContainsKey(lowestNode) ? fScore[lowestNode] : Mathf.Infinity;

        for (int i = 1; i < openSet.Count; i++)
        {
            float score = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : Mathf.Infinity;
            if (score < lowestScore)
            {
                lowestScore = score;
                lowestNode = openSet[i];
            }
        }
        return lowestNode;
    }

    private CachedRoute ReconstructPath(Dictionary<IntersectionNode, IntersectionNode> cameFrom, Dictionary<IntersectionNode, DirectedEdge> edgeTaken, IntersectionNode current)
    {
        CachedRoute finalRoute = new CachedRoute();
        while (cameFrom.ContainsKey(current))
        {
            finalRoute.path.Insert(0, edgeTaken[current]); // Insert at beginning to reverse the path
            current = cameFrom[current];
        }
        return finalRoute;
    }
}