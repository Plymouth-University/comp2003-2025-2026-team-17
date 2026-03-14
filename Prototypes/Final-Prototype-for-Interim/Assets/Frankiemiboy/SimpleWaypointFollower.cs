using UnityEngine;

public class SimpleWaypointFollower : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("Drag your waypoint transforms here in order.")]
    public Transform[] waypoints;

    [Header("Movement Settings")]
    public float speed = 20f;
    public float rotationSpeed = 10f;

    // How close the car needs to get to the waypoint before targeting the next one
    public float waypointThreshold = 2.0f; // If you set a higher speed for your car, you must increase this threshold to prevent the car from overshooting...
                                           // ...the waypoint and missing it entirely.
                                           // Adjust this value based on your car's speed and the distance between waypoints for optimal performance.

    // Add a layer mask so the downward raycast only hits the road, not other cars
    [Header("Ground Detection")]
    public LayerMask roadLayer;
    public float raycastLength = 2f;

    private int currentWaypointIndex = 0;

    void Update()
    {
        // Safety check: Do nothing if no waypoints are assigned
        if (waypoints.Length == 0) 
        {
            return;
        }

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

        // 2. Calcuate the direction to the target waypoint and rotate towards it smoothly
        Vector3 directionToTarget = targetWaypoint.position - transform.position;

        // 3. Ground Detection: Cast a ray downward to check if we're on the road
        Vector3 groundUpDirection = Vector3.up; // Default to world up if we don't hit anything

        // Shoot a raycast slightly above the car's center, straight down, to check for the ground
        Vector3 rayStart = transform.position + (Vector3.up * 0.5f);

        // 4. Steer and Align (Pitch, Yaw, AND Roll): If we hit the ground, align the car's up direction with the ground normal
        if (directionToTarget != Vector3.zero)
        {
            // LookRotatoin takes a SECOND argument: "Which way is up?"
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Smoothly blend the rotation in all axes
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 5. Move Forward 
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}