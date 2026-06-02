using UnityEngine;
using RoadArchitect;
using System.Collections.Generic;

public class TrafficEdge : MonoBehaviour
{
    public string edgeName;
    public SplineC parentSpline;

    // Where does this chunk of road start and end?
    public IntersectionNode startNode;
    public IntersectionNode endNode;

    // Which mathematical indices on the SplineC does this edge cover?
    public int startSplineIndex;
    public int endSplineIndex;

    // List of lanes where each lane is a list of waypoint Transforms
    public List<List<Transform>> forwardLanes = new List<List<Transform>>();
    public List<List<Transform>> oncomingLanes = new List<List<Transform>>();
}