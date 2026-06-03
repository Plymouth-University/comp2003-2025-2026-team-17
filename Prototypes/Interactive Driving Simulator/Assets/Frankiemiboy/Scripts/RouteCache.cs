using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DirectedEdge
{
    public TrafficEdge edge;
    public bool isForward; // True if driving StartNode -> EndNode. False if Oncoming.
}

[System.Serializable]
public class CachedRoute
{
    public List<DirectedEdge> path = new List<DirectedEdge>();
}

[System.Serializable]
public class RouteGroup
{
    public IntersectionNode startNode;
    public IntersectionNode targetNode;
    public List<CachedRoute> alternateRoutes = new List<CachedRoute>();
}

public class RouteCache : MonoBehaviour
{
    [Header("Master Route Dictionary")]
    public List<RouteGroup> allCachedRoutes = new List<RouteGroup>();

    // A helper method for the Dispatcher to quickly grab a route
    public RouteGroup GetRouteGroup(IntersectionNode start, IntersectionNode target)
    {
        foreach (RouteGroup group in allCachedRoutes)
        {
            if (group.startNode == start && group.targetNode == target) return group;
        }
        return null;
    }
}