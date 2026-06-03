using UnityEngine;
using RoadArchitect;
using System.Collections.Generic;

// --- NEW: A serializable wrapper so Unity doesn't delete our data when we hit Play! ---
[System.Serializable]
public class TrafficLane
{
    public List<Transform> waypoints = new List<Transform>();
}

public class TrafficEdge : MonoBehaviour
{
    public string edgeName;
    public SplineC parentSpline;

    public IntersectionNode startNode;
    public IntersectionNode endNode;

    public int startSplineIndex;
    public int endSplineIndex;

    // --- UPDATED: Using the wrapper class ---
    public List<TrafficLane> forwardLanes = new List<TrafficLane>();
    public List<TrafficLane> oncomingLanes = new List<TrafficLane>();
}