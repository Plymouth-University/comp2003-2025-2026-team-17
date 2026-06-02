using UnityEngine;
using RoadArchitect;

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
}