using UnityEngine;
using System.Collections.Generic;

public class Phase4B_NodeDispatcher : MonoBehaviour
{
    [Header("Traffic Settings")]
    public GameObject carPrefab;
    public float spawnInterval = 4f;
    public float clearanceRadius = 5f;
    public LayerMask obstacleLayer;

    private IntersectionNode myNode;
    private RouteCache masterCache;
    private float timer = 0f;
    private Queue<GameObject> carPool = new Queue<GameObject>();

    void Start()
    {
        myNode = GetComponent<IntersectionNode>();
        masterCache = FindFirstObjectByType<RouteCache>();
        timer = spawnInterval; // Try to spawn immediately
    }

    void Update()
    {
        if (!myNode.isDeadEnd || masterCache == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval && IsSpawnPointClear())
        {
            SpawnCarWithRoute();
        }
    }

    private bool IsSpawnPointClear()
    {
        Vector3 checkPos = transform.position + (Vector3.up * 1f);
        return !Physics.CheckSphere(checkPos, clearanceRadius, obstacleLayer);
    }

    private void SpawnCarWithRoute()
    {
        // 1. Pick a random destination that isn't this node
        List<IntersectionNode> possibleTargets = new List<IntersectionNode>();
        foreach (RouteGroup rg in masterCache.allCachedRoutes)
        {
            if (rg.startNode == myNode) possibleTargets.Add(rg.targetNode);
        }

        if (possibleTargets.Count == 0) return;
        IntersectionNode chosenTarget = possibleTargets[Random.Range(0, possibleTargets.Count)];

        // 2. Ask the Cache for the pre-baked routes
        RouteGroup routeGroup = masterCache.GetRouteGroup(myNode, chosenTarget);
        if (routeGroup == null || routeGroup.alternateRoutes.Count == 0) return;

        // 3. The "Driver Preference" Dice Roll (60% Main, 30% Alt1, 10% Alt2)
        int randomRoll = Random.Range(0, 100);
        CachedRoute selectedLogicalRoute = routeGroup.alternateRoutes[0]; // Default to fastest

        if (routeGroup.alternateRoutes.Count > 1 && randomRoll >= 60 && randomRoll < 90)
            selectedLogicalRoute = routeGroup.alternateRoutes[1];
        else if (routeGroup.alternateRoutes.Count > 2 && randomRoll >= 90)
            selectedLogicalRoute = routeGroup.alternateRoutes[2];

        // 4. Compile the Logical Route into Physical Waypoints!
        Queue<Transform[]> compiledItinerary = CompilePhysicalItinerary(selectedLogicalRoute);

        // 5. Spawn or Pull Car from Pool
        GameObject car = carPool.Count > 0 ? carPool.Dequeue() : Instantiate(carPrefab, transform.position, transform.rotation);

        car.transform.position = transform.position;
        car.SetActive(true);

        // --- HANDOFF TO CAR BRAIN ---
        // You will need to update SimpleWaypointFollower to accept a Queue<Transform[]>!
        SimpleWaypointFollower ai = car.GetComponent<SimpleWaypointFollower>();
        if (ai != null)
        {
            ai.SetItinerary(compiledItinerary); // <-- This is what your Car Brain needs!
            // ai.myManager = null; // Unlink old manager
        }

        timer = 0f;
    }

    // This method digs into the Edges and Nodes to grab the exact Straight and Curve arrays
    private Queue<Transform[]> CompilePhysicalItinerary(CachedRoute logicalRoute)
    {
        Queue<Transform[]> itinerary = new Queue<Transform[]>();
        int defaultLane = 0; // Keeping it simple: cars stick to the outer lane for now

        for (int i = 0; i < logicalRoute.path.Count; i++)
        {
            DirectedEdge currentLeg = logicalRoute.path[i];

            // A. Grab the straight waypoints for this road
            List<Transform> straightLanes = currentLeg.isForward ? currentLeg.edge.forwardLanes[defaultLane].waypoints : currentLeg.edge.oncomingLanes[defaultLane].waypoints;
            itinerary.Enqueue(straightLanes.ToArray());

            // B. If there is a next road, we need the Bezier Curve to bridge the gap
            if (i < logicalRoute.path.Count - 1)
            {
                DirectedEdge nextLeg = logicalRoute.path[i + 1];
                IntersectionNode junction = currentLeg.isForward ? currentLeg.edge.endNode : currentLeg.edge.startNode;

                // Find the specific turn folder generated in Phase 3B
                string turnFolderName = $"Turn_{currentLeg.edge.name}_to_{nextLeg.edge.name}";
                Transform turnFolder = junction.transform.Find(turnFolderName);

                if (turnFolder != null)
                {
                    Transform[] curveWaypoints = new Transform[turnFolder.childCount];
                    for (int w = 0; w < turnFolder.childCount; w++) curveWaypoints[w] = turnFolder.GetChild(w);
                    itinerary.Enqueue(curveWaypoints);
                }
            }
        }
        return itinerary;
    }

    public void DespawnCar(GameObject car)
    {
        car.SetActive(false);
        carPool.Enqueue(car);
    }
}