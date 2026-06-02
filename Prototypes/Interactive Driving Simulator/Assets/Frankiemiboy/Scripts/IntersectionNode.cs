using UnityEngine;
using System.Collections.Generic;

// Used to display the routing pairs in the Inspector, since Dictionaries don't show up by default
[System.Serializable]
public class Route
{
    [Tooltip("The destination node for this route.")]
    public IntersectionNode destination;

    [Tooltip("The traffic edge to take to reach the destination node.")]
    public TrafficEdge edgeToTake;
}

public class IntersectionNode : MonoBehaviour
{
    [Header("Node Data")]
    public string nodeID;
    public bool isDeadEnd = false;

    // List of all neighbouring nodes and the edges that connect to them
    [Header("Routing Table")]
    public List<Route> routingTable = new List<Route>();

    public TrafficEdge GetRoute(IntersectionNode destination)
    {
        foreach (Route route in routingTable)
        {
            if (route.destination == destination)
            {
                return route.edgeToTake;
            }
        }

        Debug.LogWarning($"[Graph Error] No route found from Node {nodeID} to Node {destination.nodeID}.");
        return null;
    }
}