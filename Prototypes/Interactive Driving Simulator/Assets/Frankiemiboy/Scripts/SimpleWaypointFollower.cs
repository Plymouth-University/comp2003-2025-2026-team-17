using UnityEngine;
using System.Collections.Generic;
using RoadArchitect; // --- NEW: Required to read the intersection rects ---

public class SimpleWaypointFollower : MonoBehaviour
{
    [Header("Path Settings")]
    private Queue<Transform[]> fullItinerary = new Queue<Transform[]>();
    private Transform[] currentEdgeWaypoints;
    [HideInInspector] public Phase4B_NodeDispatcher myDispatcher;

    [Header("Speed Settings")]
    public float maxSpeed = 30f;
    const float MPH_TO_MS = 0.44704f;
    public float acceleration = 15f;
    public float deceleration = 15f;

    [Header("Intersection Handling")]
    public float corneringSpeed = 10f;
    public float slowDownDistance = 20f;

    [Header("Sensor Settings (Adaptive)")]
    public Vector3 sensorBoxSize = new Vector3(2.5f, 2.5f, 0.2f);
    public float safeStoppingDistance = 5f;
    public Vector3 sensorOffset = new Vector3(0f, 0.8f, 2.5f);

    [Tooltip("Inside Intersection")]
    public bool isInsideIntersection = false;
    [Tooltip("The layer for physical vehicles.")]
    public LayerMask vehicleLayer;
    [Tooltip("The layer for traffic light stop lines.")]
    public LayerMask stopLineLayer;

    [Header("Movement Settings")]
    public float rotationSpeed = 10f;
    public float waypointThreshold = 1.0f;

    [Header("Ground Detection")]
    public LayerMask roadLayer;
    public float raycastLength = 5f;
    public float heightOffset = 0.1f;
    public float suspensionSpeed = 15f;

    private float currentSpeed = 0f;
    private int currentWaypointIndex = 0;
    private float currentSensorLength = 5f;

    // --- NEW: Storage for the physical intersection boxes ---
    private RoadIntersection[] allIntersections;

    void Start()
    {
        // Grab all intersections once when the car spawns to save CPU
        allIntersections = FindObjectsByType<RoadIntersection>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    void Update()
    {
        if (currentEdgeWaypoints == null || currentEdgeWaypoints.Length == 0) 
        { 
            return; 
        }

        Transform targetWaypoint = currentEdgeWaypoints[currentWaypointIndex];
        Debug.DrawLine(transform.position, targetWaypoint.position, Color.yellow);

        float targetSpeed = maxSpeed;

        float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint.position);

        // --- NEW: PHYSICAL INTERSECTION CHECK ---
        isInsideIntersection = IsPhysicallyInsideIntersection();

        Transform endOfLegWaypoint = currentEdgeWaypoints[currentEdgeWaypoints.Length - 1];
        float distanceToEndOfLeg = Vector3.Distance(transform.position, endOfLegWaypoint.position);

        // Slow down if approaching the end of a straight road, OR if physically inside the box
        if (distanceToEndOfLeg < slowDownDistance || isInsideIntersection)
        {
            targetSpeed = corneringSpeed;
        }

        // --- Adaptive Front Bumper Sensor Logic (ACC) ---
        float rawStoppingDistance = (currentSpeed * 0.3f) + (currentSpeed * currentSpeed * 0.015f);
        currentSensorLength = Mathf.Ceil(rawStoppingDistance / 5f) * 5f;
        currentSensorLength = Mathf.Max(currentSensorLength, safeStoppingDistance + 2f);

        Vector3 sensorStartPos = transform.position + transform.TransformDirection(sensorOffset);

        // --- DYNAMIC SENSOR MASKING ---
        LayerMask currentSensorMask = isInsideIntersection ? vehicleLayer : (vehicleLayer | stopLineLayer);

        if (Physics.BoxCast(sensorStartPos, sensorBoxSize, transform.forward, out RaycastHit obstacleHit, transform.rotation, currentSensorLength, currentSensorMask))
        {
            if (obstacleHit.distance <= safeStoppingDistance)
            {
                targetSpeed = 0f;
            }
            else
            {
                float availableRoom = obstacleHit.distance - safeStoppingDistance;
                float totalRoom = currentSensorLength - safeStoppingDistance;
                float speedFactor = availableRoom / totalRoom;
                targetSpeed = maxSpeed * speedFactor;
            }
        }

        // --- HANDOFF LOOP ---
        if (distanceToWaypoint < waypointThreshold)
        {
            if (currentWaypointIndex >= currentEdgeWaypoints.Length - 1)
            {
                if (fullItinerary.Count > 0)
                {
                    currentEdgeWaypoints = fullItinerary.Dequeue();
                    currentWaypointIndex = 0;
                    targetWaypoint = currentEdgeWaypoints[currentWaypointIndex];
                }
                else
                {
                    if (myDispatcher != null) myDispatcher.DespawnCar(this.gameObject);
                    return;
                }
            }
            else
            {
                currentWaypointIndex++;
                targetWaypoint = currentEdgeWaypoints[currentWaypointIndex];
            }
        }

        // --- Ground Detection & Fake Suspension ---
        Vector3 rayStart = transform.position + (Vector3.up * 2.0f);
        Vector3 groundUpDirection = Vector3.up;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, raycastLength, roadLayer))
        {
            groundUpDirection = hitInfo.normal;
            float targetY = hitInfo.point.y + heightOffset;
            Vector3 fixedPosition = transform.position;
            fixedPosition.y = Mathf.Lerp(transform.position.y, targetY, suspensionSpeed * Time.deltaTime);
            transform.position = fixedPosition;
        }

        // --- Steer and Align ---
        Vector3 rawDirectionToTarget = targetWaypoint.position - transform.position;
        Vector3 directionToTarget = Vector3.ProjectOnPlane(rawDirectionToTarget, groundUpDirection);

        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, groundUpDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- Apply Smooth Acceleration & Deceleration ---
        float currentAccelRate = (targetSpeed > currentSpeed) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentAccelRate * Time.deltaTime);

        // --- Move Forward ---
        float speedInMetersPerSecond = currentSpeed * MPH_TO_MS;
        transform.Translate(Vector3.forward * speedInMetersPerSecond * Time.deltaTime);
    }

    public void SetItinerary(Queue<Transform[]> newItinerary)
    {
        fullItinerary = newItinerary;
        currentWaypointIndex = 0;
        currentSpeed = 0f;

        if (fullItinerary.Count > 0) currentEdgeWaypoints = fullItinerary.Dequeue();
        else currentEdgeWaypoints = null;
    }

    // --- NEW: The Physical Geometric Check ---
    private bool IsPhysicallyInsideIntersection()
    {
        if (allIntersections == null || allIntersections.Length == 0) return false;

        Vector3 checkPos = transform.position;
        foreach (RoadIntersection intersection in allIntersections)
        {
            // Only do the expensive rectangle math if we are close to the center!
            if (Vector3.Distance(checkPos, intersection.transform.position) < 30f)
            {
                // RoadArchitect's internal check requires a 'ref' vector
                if (intersection.Contains(ref checkPos))
                {
                    return true;
                }
            }
        }
        return false;
    }

    void OnDrawGizmos()
    {
        Vector3 sensorStartPos = transform.position + transform.TransformDirection(sensorOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(sensorStartPos, transform.forward * currentSensorLength);
        Gizmos.matrix = Matrix4x4.TRS(sensorStartPos + transform.forward * currentSensorLength, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, sensorBoxSize * 2);
    }
}