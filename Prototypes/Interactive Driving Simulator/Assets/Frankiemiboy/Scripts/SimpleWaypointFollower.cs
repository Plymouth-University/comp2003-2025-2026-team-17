using UnityEngine;
using System.Collections.Generic; // --- PHASE 4 UPGRADE: Needed for the Queue system ---

public class SimpleWaypointFollower : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("The Queue containing all the arrays of waypoints (Straights and Curves) to reach the destination.")]
    private Queue<Transform[]> fullItinerary = new Queue<Transform[]>();

    [Tooltip("The specific array of waypoints the car is currently driving on.")]
    private Transform[] currentEdgeWaypoints;

    // --- A reference back to the new Dispatcher so the car can report when finished ---
    [HideInInspector]
    public Phase4B_NodeDispatcher myDispatcher;

    [Header("Speed Settings")]
    public float maxSpeed = 30f;
    const float MPH_TO_MS = 0.44704f;
    public float acceleration = 15f;
    public float deceleration = 15f;

    [Header("Sensor Settings (Adaptive)")]
    public Vector3 sensorBoxSize = new Vector3(2.5f, 2.5f, 0.2f);
    public float safeStoppingDistance = 5f;
    public Vector3 sensorOffset = new Vector3(0f, 0.8f, 2.5f);
    public LayerMask obstacleLayer;

    [Header("Movement Settings")]
    public float rotationSpeed = 10f;
    public float waypointThreshold = 2.0f;

    [Header("Ground Detection")]
    public LayerMask roadLayer;
    public float raycastLength = 5f;
    public float heightOffset = 0.1f;
    public float suspensionSpeed = 15f;

    private float currentSpeed = 0f;
    private int currentWaypointIndex = 0;
    private float currentSensorLength = 5f;

    void Update()
    {
        // Safety check: Do nothing if we don't have a current edge to drive on
        if (currentEdgeWaypoints == null || currentEdgeWaypoints.Length == 0)
        {
            return;
        }

        Transform targetWaypoint = currentEdgeWaypoints[currentWaypointIndex];
        float targetSpeed = maxSpeed;

        // 1. Check Distance: Are we close enough to the current waypoint?
        float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint.position);
        

        // --- Adaptive Front Bumper Sensor Logic (ACC) ---
        float rawStoppingDistance = (currentSpeed * 0.3f) + (currentSpeed * currentSpeed * 0.015f);
        currentSensorLength = Mathf.Ceil(rawStoppingDistance / 5f) * 5f;
        currentSensorLength = Mathf.Max(currentSensorLength, safeStoppingDistance + 2f);

        Vector3 sensorStartPos = transform.position + transform.TransformDirection(sensorOffset);

        if (Physics.BoxCast(sensorStartPos, sensorBoxSize, transform.forward, out RaycastHit obstacleHit, transform.rotation, currentSensorLength, obstacleLayer))
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
        // Switches the car from one array of waypoints to the next!
        if (distanceToWaypoint < waypointThreshold)
        {
            // Last waypoint in current array?
            if (currentWaypointIndex >= currentEdgeWaypoints.Length - 1)
            {
                // Check if there are still any more arrays (edges) in the Queue to drive on
                if (fullItinerary.Count > 0)
                {
                    currentEdgeWaypoints = fullItinerary.Dequeue();
                    currentWaypointIndex = 0;
                    targetWaypoint = currentEdgeWaypoints[currentWaypointIndex];
                }
                else
                {
                    // Queue is completely empty. Final destination reached!
                    if (myDispatcher != null)
                    {
                        myDispatcher.DespawnCar(this.gameObject);
                    }
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

    // --- Route Setup Method ---
    // The Dispatcher calls this the exact moment the car spawns to inject the GPS data
    public void SetItinerary(Queue<Transform[]> newItinerary)
    {
        fullItinerary = newItinerary;
        currentWaypointIndex = 0;
        currentSpeed = 0f; // Reset speed so the car smoothly accelerates off the starting line

        // Immediately load the first edge of the journey so the car has something to target!
        if (fullItinerary.Count > 0)
        {
            currentEdgeWaypoints = fullItinerary.Dequeue();
        }
        else
        {
            currentEdgeWaypoints = null;
        }
    }

    // --- Gizmos: For Visual Debugging Purposes ---
    void OnDrawGizmos()
    {
        Vector3 sensorStartPos = transform.position + transform.TransformDirection(sensorOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(sensorStartPos, transform.forward * currentSensorLength);
        Gizmos.matrix = Matrix4x4.TRS(sensorStartPos + transform.forward * currentSensorLength, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, sensorBoxSize * 2);
    }
}