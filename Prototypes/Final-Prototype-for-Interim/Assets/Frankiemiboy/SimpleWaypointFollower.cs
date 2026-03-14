using UnityEngine;

public class SimpleWaypointFollower : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("Drag your waypoint transforms here in order.")]
    public Transform[] waypoints;

    [Header("Movement Settings")]
    public float speed = 5f;
    public float rotationSpeed = 5f;

    // How close the car needs to get to the waypoint before targeting the next one
    public float waypointThreshold = 0.5f;

    private int currentWaypointIndex = 0;

    void Update()
    {
        // Safety check: Do nothing if no waypoints are assigned
        if (waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // 1. Check Distance: Are we close enough to the current waypoint?
        float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint.position);

        // The code commented out is to automatically switch to the next waypoint when we get close enough... BUT WILL CAUSE THE CAR TO CONITNUE TO SWITCH WAYPOINTS IN A LOOP -- The loop is endless until stopped manually.
        //if (distanceToWaypoint < waypointThreshold)
        //{
        //    // Move to the next waypoint index. 
        //    // The '%' operator loops it back to 0 when it reaches the end of the array.
        //    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        //    targetWaypoint = waypoints[currentWaypointIndex];
        //}


        // This code will do the same thing as the above commented out code, but will stop at the last waypoint instead of looping back to the first one.
        if (distanceToWaypoint < waypointThreshold)
        {
            // Move to the next waypoint index, but only if we haven't reached the last one.
            if (currentWaypointIndex == waypoints.Length - 1)
            {
                // We've reached the last waypoint, so we stop the car and prevent further movement or rotation.
                speed = 0f; // Stop the car when it reaches the last waypoint
                rotationSpeed = 0f; // Stop the car from rotating when it reaches the last waypoint
                return; // Exit the Update method to prevent further movement or rotation
            }
            else 
            {
                // We haven't reached the last waypoint, so target the next waypoint
                currentWaypointIndex++;
                targetWaypoint = waypoints[currentWaypointIndex];
            }
        }

        // 2. Rotate towards the target waypoint smoothly
        Vector3 directionToTarget = targetWaypoint.position - transform.position;

        // Ensure we don't calculate a rotation if we are exactly on the spot
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 3. Move forward continually
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}