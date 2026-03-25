using UnityEngine;

public class SimpleWaypointFollower : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("LaneTrafficManager will automatically fill this with waypoints when car spawns")]
    public Transform[] waypoints;

    // --- A reference back to the manager so the car can report when finished ---
    [HideInInspector]
    public LeftLanesTrafficManager myManager;

    // --- NEW: We changed "Movement Settings" to "Speed Settings" and added our acceleration variables ---
    [Header("Speed Settings")]
    public float maxSpeed = 35f; // This replaces the old 'speed' variable. It's the top speed the car wants to reach.
    const float MPH_TO_MS = 0.44704f;
    public float acceleration = 15f; // How quickly the car gets up to maxSpeed from a resting state.
    public float deceleration = 15f; // How quickly the car hits the brakes (usually higher than acceleration).

    // TEMPORARILY removed because we don't brake anymore once car has reached final waypoint
    // public float brakingDistance = 15f; // How far away from the final waypoint the car should start hitting the brakes.

    // --- CHANGED: Sensor settings updated for Adaptive Cruise Control (ACC) ---
    [Header("Sensor Settings (Adaptive)")]
    [Tooltip("The size of the radar box (Width, Height, Depth). This replaces the single thin line.")]
    public Vector3 sensorBoxSize = new Vector3(2.5f, 2.5f, 0.2f); // Half-extents: makes a 1.8m wide, 1.6m tall box
    [Tooltip("The absolute minimum distance to maintain from the car ahead before coming to a complete stop.")]
    public float safeStoppingDistance = 5f; // 4 meters roughly equals one car length
    [Tooltip("Position offset to move the sensor to the front bumper (X, Y, Z).")]
    public Vector3 sensorOffset = new Vector3(0f, 0.8f, 2.5f);
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

    // --- How high above the road the center of the car should hover (0.5 is exactly half a default Unity cube). 
    // This fixes the issue where the cube's pivot point causes it to spawn halfway underground. ---
    public float heightOffset = 0.1f;

    // --- How fast the car "bounces" back to the correct height. 
    // Higher numbers = stiffer suspension. Lower numbers = bouncy/floaty suspension. ---
    public float suspensionSpeed = 15f;

    // --- We need a private variable to track the actual speed the wheels are turning at right now ---
    private float currentSpeed = 0f; // Starts at 0 (from rest)
    private int currentWaypointIndex = 0;

    // Tracks our dynamically changing sensor length so the Gizmo can draw it correctly
    private float currentSensorLength = 5f;

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

        // --- Calculate our Target Speed ---
        // Target speed is what the car WANTS to do right now. By default, it wants to go max speed.
        float targetSpeed = maxSpeed;

        // TEMPORARILY removed because we don't brake anymore once car has reached final waypoint
        //// Check if we are currently driving towards the very last waypoint in the array
        //if (currentWaypointIndex == waypoints.Length - 1)
        //{
        //    // If we are within the braking distance of the final stop, change our target speed to 0 so we begin to slow down.
        //    if (distanceToWaypoint <= brakingDistance)
        //    {
        //        targetSpeed = 0f;
        //    }
        //}

        // --- CHANGED: Adaptive Front Bumper Sensor Logic based on real-world stopping distances ---

        // A. Calculate dynamic sensor length based on the current speed (in mph).
        // Thinking distance approx: speed * 0.3
        // Braking distance approx: speed squared * 0.015
        float rawStoppingDistance = (currentSpeed * 0.3f) + (currentSpeed * currentSpeed * 0.015f);

        // B. Round up to the next multiple of 5 (e.g., 23m becomes 25m) to create a safe buffer zone
        currentSensorLength = Mathf.Ceil(rawStoppingDistance / 5f) * 5f;

        // Ensure the sensor never shrinks smaller than the safe stopping distance + a tiny buffer
        currentSensorLength = Mathf.Max(currentSensorLength, safeStoppingDistance + 2f);

        // C. Calculate the exact starting point of the sensor
        Vector3 sensorStartPos = transform.position + transform.TransformDirection(sensorOffset);

        // D. Shoot the adaptive raycast straight forward
        if (Physics.BoxCast(sensorStartPos, sensorBoxSize, transform.forward, out RaycastHit obstacleHit, transform.rotation, currentSensorLength, obstacleLayer))
        {
            // If the car ahead is critically close (inside our 4m safe zone), slam the brakes to 0.
            if (obstacleHit.distance <= safeStoppingDistance)
            {
                targetSpeed = 0f;
            }
            else
            {
                // ADAPTIVE CRUISE CONTROL MATH:
                // We figure out how much "safe room" we have left in our raycast line.
                float availableRoom = obstacleHit.distance - safeStoppingDistance;
                float totalRoom = currentSensorLength - safeStoppingDistance;

                // This gives us a percentage (from 0.0 to 1.0) of how much buffer we have left.
                float speedFactor = availableRoom / totalRoom;

                // Scale our target speed down proportionally. As the line gets shorter, the target speed drops.
                // This creates a self-correcting equilibrium where our car matches the speed of the car ahead!
                targetSpeed = maxSpeed * speedFactor;
            }
        }
        // ----------------------------------------

        // --- THE TELEPORT LOOP ---
        // This code block will teleport a car that has reached its last waypoint to...
        // ... the beginning of the road (the first waypoint of the lane)
        if (distanceToWaypoint < waypointThreshold)
        {
            if (currentWaypointIndex == waypoints.Length - 1)
            {
                // We've reached the absolute end of the road! 
                // Tell the manager to put us in the buffer queue.
                if (myManager != null)
                {
                    myManager.CarFinishedRoute(this.gameObject);
                }
                return; // Stop running any more code this frame since we are now "asleep"
            }
            else
            {
                currentWaypointIndex++;
                targetWaypoint = waypoints[currentWaypointIndex];
            }
        }

        // Switches to next waypoint in the array and stops when reached final waypoint
        //if (distanceToWaypoint < waypointThreshold)
        //{
        //    // Move to the next waypoint index, but only if we haven't reached the last one.
        //    if (currentWaypointIndex == waypoints.Length - 1)
        //    {
        //        // We've reached the last waypoint, so we stop the car and prevent further movement or rotation.
        //        currentSpeed = 0f; // --- CHANGED: Use currentSpeed instead of the old 'speed' variable to ensure it fully stops. ---
        //        rotationSpeed = 0f; // Stop the car from rotating when it reaches the last waypoint
        //        return; // Exit the Update method to prevent further movement or rotation
        //    }
        //    else
        //    {
        //        // We haven't reached the last waypoint, so target the next waypoint
        //        currentWaypointIndex++;
        //        targetWaypoint = waypoints[currentWaypointIndex];
        //    }
        //}


        // Shoot a raycast significantly higher above the car's center, straight down, to check for the ground.
        // Starting higher prevents the raycast from starting *inside* a steep hill and missing the road.
        Vector3 rayStart = transform.position + (Vector3.up * 2.0f);

        // 2. Ground Detection: Cast a ray downward to check if we're on the road
        // This section of code is to ensure that the cube or the car remains on the surface of the road on hills or dips.
        // It also allows the car to align with the slope of the road by using the normal of the surface it hits.
        Vector3 groundUpDirection = Vector3.up; // Default to world up if we don't hit anything
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, raycastLength, roadLayer))
        {
            groundUpDirection = hitInfo.normal; // Use the normal of the ground we hit

            // --- Fake Suspension (Smooth Y-Axis Snapping) ---
            // We calculate where the car SHOULD be (the target height).
            float targetY = hitInfo.point.y + heightOffset;
            Vector3 fixedPosition = transform.position;

            // Instead of instantly teleporting, we use Mathf.Lerp to smoothly glide the Y position 
            // from its current height to the target height. This mimics a real suspension spring!
            fixedPosition.y = Mathf.Lerp(transform.position.y, targetY, suspensionSpeed * Time.deltaTime);

            transform.position = fixedPosition;
            // ---------------------------------------------------------------------------------
        }

        // 3. Steer and Align (Calculated second using the road's angle)
        // First, we get the raw direction from the car to the waypoint
        Vector3 rawDirectionToTarget = targetWaypoint.position - transform.position;

        // Instead of forcing Y to 0, we project the direction vector onto the slope of the road
        // This stops nose-diving when going uphill and tail-lifting when going downhill, creating a more natural driving feel.
        Vector3 directionToTarget = Vector3.ProjectOnPlane(rawDirectionToTarget, groundUpDirection);

        // 4. Steer and Align (Pitch, Yaw, AND Roll): If we hit the ground, align the car's up direction with the ground normal
        if (directionToTarget != Vector3.zero)
        {
            // LookRotatoin takes a SECOND argument: "Which way is up?"
            // --- Added 'groundUpDirection' here so the car actually tilts to match the road's normal! ---
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, groundUpDirection);

            // Smoothly blend the rotation in all axes
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- Apply Smooth Acceleration & Deceleration ---
        // We figure out if we need to use the acceleration number (speeding up) or deceleration number (slowing down).
        // If our targetSpeed (e.g., 20) is higher than our currentSpeed (e.g., 5), we use acceleration. Otherwise, we use deceleration.
        float currentAccelRate = (targetSpeed > currentSpeed) ? acceleration : deceleration;

        // Mathf.MoveTowards shifts the first number (currentSpeed) towards the second number (targetSpeed)
        // by the amount specified in the third parameter. This ensures we smoothly speed up or slow down over time.
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentAccelRate * Time.deltaTime);
        // -----------------------------------------------------

        // 5. Move Forward 
        // First convert the speed to meters per second (since our speed is in mph) by multiplying by the conversion factor.
        // --- We now multiply by 'currentSpeed' instead of the fixed max speed so the car physically accelerates. ---
        float speedInMetersPerSecond = currentSpeed * MPH_TO_MS;
        transform.Translate(Vector3.forward * speedInMetersPerSecond * Time.deltaTime);
    }

    // --- NEW: Reset Method called by the Manager when releasing from the buffer ---
    public void ResetToStart()
    {
        currentWaypointIndex = 0;
        currentSpeed = 0f; // Reset speed to 0 so the car smoothly accelerates off the starting line again
    }

    // --- Gizmos: For Visual Debugging Purposes ---
    // This built-in Unity function draws shapes in the Scene view to help us see invisible math.
    // --- NEW: Gizmos: For Visual Debugging Purposes ---
    void OnDrawGizmos()
    {
        Vector3 sensorStartPos = transform.position + transform.TransformDirection(sensorOffset);
        Gizmos.color = Color.red;

        // Draw the center line
        Gizmos.DrawRay(sensorStartPos, transform.forward * currentSensorLength);

        // Draw the BoxCast volume at the end of the sensor
        // We use a matrix to ensure the drawn box rotates perfectly with the car
        Gizmos.matrix = Matrix4x4.TRS(sensorStartPos + transform.forward * currentSensorLength, transform.rotation, Vector3.one);

        // We multiply by 2 because BoxCast uses "Half-Extents" (radius from center to edge)
        Gizmos.DrawWireCube(Vector3.zero, sensorBoxSize * 2);
    }

    //void OnDrawGizmos()
    //{
    //    // Draw the front sensor as a red line using the dynamically calculated length
    //    Vector3 sensorStartPos = transform.position + transform.TransformDirection(sensorOffset);
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawRay(sensorStartPos, transform.forward * currentSensorLength);
    //}
}