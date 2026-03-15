using UnityEngine;

public class SimpleWaypointFollower : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("Drag your waypoint transforms here in order.")]
    public Transform[] waypoints;

    // --- NEW: We changed "Movement Settings" to "Speed Settings" and added our acceleration variables ---
    [Header("Speed Settings")]
    public float maxSpeed = 20f; // This replaces the old 'speed' variable. It's the top speed the car wants to reach.
    public float acceleration = 5f; // How quickly the car gets up to maxSpeed from a resting state.
    public float deceleration = 10f; // How quickly the car hits the brakes (usually higher than acceleration).
    public float brakingDistance = 15f; // How far away from the final waypoint the car should start hitting the brakes.

    // --- Sensor settings for detecting traffic ---
    [Header("Sensor Settings")]
    [Tooltip("How far ahead should the car look ahead for obstacles (other vehicles)?")]
    public float sensorLength = 10f;
    [Tooltip("Position offset to move the sensor to the front bumper (X, Y, Z).")]
    public Vector3 sensorOffset = new Vector3(0f, 0.5f, 2f);
    [Tooltip("Which layers count as obstacles? (e.g., Other vehicles)")]
    public LayerMask obstacleLayer;
    // ------------------------------------------------

    [Header("Movement Settings")]
    public float rotationSpeed = 10f;

    // How close the car needs to get to the waypoint before targeting the next one
    public float waypointThreshold = 2.0f; // If you set a higher speed for your car, you must increase this threshold to prevent the car from overshooting...
                                           // ...the waypoint and missing it entirely.
                                           // Adjust this value based on your car's speed and the distance between waypoints for optimal performance.

    // Add a layer mask so the downward raycast only hits the road, not other cars
    [Header("Ground Detection")]
    public LayerMask roadLayer;

    // --- CHANGED: Increased from 2f to 5f so the laser reaches the ground even if the road drops away quickly on steep hills. ---
    public float raycastLength = 5f;

    // --- NEW: How high above the road the center of the car should hover (0.5 is exactly half a default Unity cube). 
    // This fixes the issue where the cube's pivot point causes it to spawn halfway underground. ---
    public float heightOffset = 0.5f;

    // --- NEW: How fast the car "bounces" back to the correct height. 
    // Higher numbers = stiffer suspension. Lower numbers = bouncy/floaty suspension. ---
    public float suspensionSpeed = 15f;

    // --- NEW: We need a private variable to track the actual speed the wheels are turning at right now ---
    private float currentSpeed = 0f; // Starts at 0 (from rest)
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

        // --- NEW: Calculate our Target Speed ---
        // Target speed is what the car WANTS to do right now. By default, it wants to go max speed.
        float targetSpeed = maxSpeed;

        // Check if we are currently driving towards the very last waypoint in the array
        if (currentWaypointIndex == waypoints.Length - 1)
        {
            // If we are within the braking distance of the final stop, change our target speed to 0 so we begin to slow down.
            if (distanceToWaypoint <= brakingDistance)
            {
                targetSpeed = 0f;
            }
        }

        // --- Front Bumper Sesnor Logic ---
        // 1. Calculate the exact starting point of the sensor based on the car's current rotation
        Vector3 sensorStartPos = transform.position + transform.TransformDirection(sensorOffset);

        // 2. Shoot the raycast straight forward
        if (Physics.Raycast(sensorStartPos, transform.forward, out RaycastHit obstacleHit, sensorLength, obstacleLayer))
        {
            // If we hit something on the obstacle layer, override our target speed to 0 so we brake!
            targetSpeed = 0f;
        }
        // ----------------------------------------

        // The code commented out is to automatically switch to the next waypoint when we get close enough... BUT WILL CAUSE THE CAR TO CONITNUE TO SWITCH WAYPOINTS IN A LOOP -- The loop is endless until stopped manually.
        //if (distanceToWaypoint < waypointThreshold)
        //{
        //    // Move to the next waypoint index. 
        //    // The '%' operator loops it back to 0 when it reaches the end of the array.
        //    // currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        //    // targetWaypoint = waypoints[currentWaypointIndex];
        //}


        // This code will do the same thing as the above commented out code, but will stop at the last waypoint instead of looping back to the first one.
        if (distanceToWaypoint < waypointThreshold)
        {
            // Move to the next waypoint index, but only if we haven't reached the last one.
            if (currentWaypointIndex == waypoints.Length - 1)
            {
                // We've reached the last waypoint, so we stop the car and prevent further movement or rotation.
                currentSpeed = 0f; // --- CHANGED: Use currentSpeed instead of the old 'speed' variable to ensure it fully stops. ---
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
        directionToTarget.y = 0; // We only want to rotate on the X-axis (left/right), so we ignore any vertical difference between the car and the waypoint.

        // --- CHANGED: We raised the rayStart from 0.5f to 2.0f. ---
        // Shoot a raycast significantly higher above the car's center, straight down, to check for the ground.
        // Starting higher prevents the raycast from starting *inside* a steep hill and missing the road.
        Vector3 rayStart = transform.position + (Vector3.up * 2.0f);

        // 3. Ground Detection: Cast a ray downward to check if we're on the road
        // This section of code is to ensure that the cube or the car remains on the surface of the road on hills or dips.
        // It also allows the car to align with the slope of the road by using the normal of the surface it hits.
        Vector3 groundUpDirection = Vector3.up; // Default to world up if we don't hit anything
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, raycastLength, roadLayer))
        {
            groundUpDirection = hitInfo.normal; // Use the normal of the ground we hit

            // --- CHANGED: Fake Suspension (Smooth Y-Axis Snapping) ---
            // We calculate where the car SHOULD be (the target height).
            float targetY = hitInfo.point.y + heightOffset;
            Vector3 fixedPosition = transform.position;

            // Instead of instantly teleporting, we use Mathf.Lerp to smoothly glide the Y position 
            // from its current height to the target height. This mimics a real suspension spring!
            fixedPosition.y = Mathf.Lerp(transform.position.y, targetY, suspensionSpeed * Time.deltaTime);

            transform.position = fixedPosition;
            // ---------------------------------------------------------------------------------
        }

        // 4. Steer and Align (Pitch, Yaw, AND Roll): If we hit the ground, align the car's up direction with the ground normal
        if (directionToTarget != Vector3.zero)
        {
            // LookRotatoin takes a SECOND argument: "Which way is up?"
            // --- FIXED: Added 'groundUpDirection' here so the car actually tilts to match the road's normal! ---
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, groundUpDirection);

            // Smoothly blend the rotation in all axes
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- NEW: Apply Smooth Acceleration & Deceleration ---
        // We figure out if we need to use the acceleration number (speeding up) or deceleration number (slowing down).
        // If our targetSpeed (e.g., 20) is higher than our currentSpeed (e.g., 5), we use acceleration. Otherwise, we use deceleration.
        float currentAccelRate = (targetSpeed > currentSpeed) ? acceleration : deceleration;

        // Mathf.MoveTowards shifts the first number (currentSpeed) towards the second number (targetSpeed)
        // by the amount specified in the third parameter. This ensures we smoothly speed up or slow down over time.
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentAccelRate * Time.deltaTime);
        // -----------------------------------------------------

        // 5. Move Forward 
        // --- CHANGED: We now multiply by 'currentSpeed' instead of the fixed max speed so the car physically accelerates. ---
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    // --- NEW: Gizmos: For Visual Debugging Purposes ---
    // This built-in Unity function draws shapes in the Scene view to help us see invisible math.
    void OnDrawGizmos()
    {
        // Draw the front sensor as a red line
        Vector3 sensorStartPos = transform.position + transform.TransformDirection(sensorOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(sensorStartPos, transform.forward * sensorLength);
    }
}