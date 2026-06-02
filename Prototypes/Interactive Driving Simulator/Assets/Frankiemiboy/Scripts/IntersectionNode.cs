using UnityEngine;
using System.Collections.Generic;

public class IntersectionNode : MonoBehaviour
{
    public string nodeID;
    public bool isDeadEnd = false;

    // The Routing Table: "To get to [Destination Node], take [Traffic Edge]"
    // (Note: Unity's Inspector doesn't show Dictionaries by default, but the data is there!)
    public Dictionary<IntersectionNode, TrafficEdge> routingTable = new Dictionary<IntersectionNode, TrafficEdge>();

    public TrafficEdge GetRoute(IntersectionNode destination)
    {
        if (routingTable.ContainsKey(destination)) return routingTable[destination];
        return null;
    }
}