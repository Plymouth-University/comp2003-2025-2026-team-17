using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

namespace Ezereal
{
    public class EzerealCarController : MonoBehaviour // This is the main system resposible for car control.
    {
        [Header("Ezereal References")]

        [SerializeField] EzerealLightController ezerealLightController;
        [SerializeField] EzerealSoundController ezerealSoundController;
        [SerializeField] EzerealWheelFrictionController ezerealWheelFrictionController;
        [SerializeField] RealAIRadio realAIRadio; // UPDATE: Added reference to the AI Radio script so that we can call it from here when the player presses the button to turn on the radio.

        [Header("References")]

        public Rigidbody vehicleRB;
        public WheelCollider frontLeftWheelCollider;
        public WheelCollider frontRightWheelCollider;
        public WheelCollider rearLeftWheelCollider;
        public WheelCollider rearRightWheelCollider;
        WheelCollider[] wheels;

        [SerializeField] Transform frontLeftWheelMesh;
        [SerializeField] Transform frontRightWheelMesh;
        [SerializeField] Transform rearLeftWheelMesh;
        [SerializeField] Transform rearRightWheelMesh;

        [SerializeField] Transform steeringWheel;

        [SerializeField] TMP_Text currentGearTMP_UI;
        [SerializeField] TMP_Text currentGearTMP_Dashboard;

        [SerializeField] TMP_Text currentSpeedTMP_UI;
        [SerializeField] TMP_Text currentSpeedTMP_Dashboard;
        [SerializeField] Slider accelerationSlider;

        [Header("Settings")]
        public bool isStarted = true;

        public float maxForwardSpeed = 100f; // 100f default
        public float maxReverseSpeed = 30f; // 30f default
        public float horsePower = 1000f; // 100f0 default
        public float brakePower = 2000f; // 2000f default
        public float handbrakeForce = 3000f; // 3000f default
        public float maxSteerAngle = 30f; // 30f default
        public float steeringSpeed = 5f; // 0.5f default
        public float stopThreshold = 1f; // 1f default. At what speed car will make a full stop
        public float decelerationSpeed = 0.5f; // 0.5f default
        public float maxSteeringWheelRotation = 360f; // 360 for real steering wheel. 120 would be more suitable for racing.

        [Header("My Settings")]
        public bool isHandbrakeActive = false;

        [Header("Drive Type")]
        public DriveTypes driveType = DriveTypes.RWD;

        [Header("Gearbox")]
        public AutomaticGears currentGear = AutomaticGears.Drive;

        // ********* Added piece of code for gear selection by frank and Javier. This is for the temporary solution, which will be replaced by the actual physical stick input in the future. ***********
        // Add these boolean trackers near the top of your script with your other variables
        [Header("Gear States")]
        public bool isDriveInputActive = false;
        public bool isReverseInputActive = false;

        // ********************** NEW: Direct references to the Input System Actions ---
        [Header("Input Action References")]
        public InputActionReference driveGearAction;
        public InputActionReference reverseGearAction;

        [Header("Debug Info")]
        public bool stationary = true;
        [SerializeField] float currentSpeed = 0f;
        [SerializeField] public float currentAccelerationValue = 0f;
        [SerializeField] float currentBrakeValue = 0f;
        [SerializeField] float currentHandbrakeValue = 0f;
        [SerializeField] float currentSteerAngle = 0f;
        [SerializeField] float targetSteerAngle = 0f;
        [SerializeField] float FrontLeftWheelRPM = 0f;
        [SerializeField] float FrontRightWheelRPM = 0f;
        [SerializeField] float RearLeftWheelRPM = 0f;
        [SerializeField] float RearRightWheelRPM = 0f;

        [SerializeField] float speedFactor = 0f; // Leave at zero. Responsible for smooth acceleration and near-top-speed slowdown.

        private void Awake()
        {
            wheels = new WheelCollider[]
            {
            frontLeftWheelCollider,
            frontRightWheelCollider,
            rearLeftWheelCollider,
            rearRightWheelCollider,
            };

            if (ezerealLightController == null)
            {
                Debug.LogWarning("EzerealLightController reference is missing. Ignore or attach one if you want to have light controls.");
            }

            if (ezerealSoundController == null)
            {
                Debug.LogWarning("EzerealSoundController reference is missing. Ignore or attach one if you want to have engine sounds.");
            }

            if (ezerealWheelFrictionController == null)
            {
                Debug.LogWarning("EzerealWheelFrictionController reference is missing. Ignore or attach one if you want to have friction controls.");
            }

            if (vehicleRB == null)
            {
                Debug.LogError("VehicleRB reference is missing for EzerealCarController!");
            }

            if (isStarted)
            {
                Debug.Log("Car is started.");

                if (ezerealLightController != null)
                {
                    ezerealLightController.MiscLightsOn();
                }

                if (ezerealSoundController != null)
                {
                    ezerealSoundController.TurnOnEngineSound();
                }
            }
        }

        void OnStartCar()
        {
            isStarted = !isStarted;

            if (isStarted)
            {
                Debug.Log("Car started.");

                if (ezerealLightController != null)
                {
                    ezerealLightController.MiscLightsOn();
                }

                if (ezerealSoundController != null)
                {
                    ezerealSoundController.TurnOnEngineSound();
                }

            }
            else if (!isStarted)
            {
                Debug.Log("Car turned off");

                if (ezerealLightController != null)
                {
                    ezerealLightController.AllLightsOff();
                }

                if (ezerealSoundController != null)
                {
                    ezerealSoundController.TurnOffEngineSound();
                }

                frontLeftWheelCollider.motorTorque = 0;
                frontRightWheelCollider.motorTorque = 0;
                rearLeftWheelCollider.motorTorque = 0;
                rearRightWheelCollider.motorTorque = 0;
            }


        }

        void OnRadio() 
        {
            if (isStarted && (realAIRadio != null))
            {
                realAIRadio.TurnOnRadio();
            }
            else if (realAIRadio == null)
            {
                Debug.LogWarning("RealAIRadio reference is missing or is not assigned in the EzrealCarController. Attach one to use the AI Radio feature.");
            }
        }

        void OnAccelerate(InputValue accelerationValue)
        {
            currentAccelerationValue = accelerationValue.Get<float>();
            Debug.Log("Acceleration: " + currentAccelerationValue.ToString());
        }

        void Acceleration()
        {
            if (isStarted)
            {
                if (currentGear == AutomaticGears.Drive)
                {
                    // Calculate how close the car is to top speed
                    // as a number from zero to one
                    speedFactor = Mathf.InverseLerp(0, maxForwardSpeed, currentSpeed);

                    // Use that to calculate how much torque is available 
                    // (zero torque at top speed)
                    float currentMotorTorque = Mathf.Lerp(horsePower, 0, speedFactor);

                    if (currentAccelerationValue > 0f && currentSpeed < maxForwardSpeed)
                    {
                        if (driveType == DriveTypes.RWD)
                        {
                            rearLeftWheelCollider.motorTorque = currentMotorTorque * currentAccelerationValue;
                            rearRightWheelCollider.motorTorque = currentMotorTorque * currentAccelerationValue;
                        }
                        else if (driveType == DriveTypes.FWD)
                        {
                            frontLeftWheelCollider.motorTorque = currentMotorTorque * currentAccelerationValue;
                            frontRightWheelCollider.motorTorque = currentMotorTorque * currentAccelerationValue;
                        }
                        else if (driveType == DriveTypes.AWD)
                        {
                            frontLeftWheelCollider.motorTorque = currentMotorTorque * currentAccelerationValue;
                            frontRightWheelCollider.motorTorque = currentMotorTorque * currentAccelerationValue;
                            rearLeftWheelCollider.motorTorque = currentMotorTorque * currentAccelerationValue;
                            rearRightWheelCollider.motorTorque = currentMotorTorque * currentAccelerationValue;
                        }
                    }
                    else
                    {
                        frontLeftWheelCollider.motorTorque = 0;
                        frontRightWheelCollider.motorTorque = 0;
                        rearLeftWheelCollider.motorTorque = 0;
                        rearRightWheelCollider.motorTorque = 0;
                    }
                }

                if (currentGear == AutomaticGears.Reverse)
                {
                    if (currentAccelerationValue > 0f && currentSpeed > -maxReverseSpeed)
                    {
                        currentAccelerationValue = 1; //Invert Acceleration value

                        if (driveType == DriveTypes.RWD)
                        {
                            rearLeftWheelCollider.motorTorque = -currentAccelerationValue * horsePower;
                            rearRightWheelCollider.motorTorque = -currentAccelerationValue * horsePower;
                        }
                        else if (driveType == DriveTypes.FWD)
                        {
                            frontLeftWheelCollider.motorTorque = -currentAccelerationValue * horsePower;
                            frontRightWheelCollider.motorTorque = -currentAccelerationValue * horsePower;
                        }
                        else if (driveType == DriveTypes.AWD)
                        {
                            frontLeftWheelCollider.motorTorque = -currentAccelerationValue * horsePower;
                            frontRightWheelCollider.motorTorque = -currentAccelerationValue * horsePower;
                            rearLeftWheelCollider.motorTorque = -currentAccelerationValue * horsePower;
                            rearRightWheelCollider.motorTorque = -currentAccelerationValue * horsePower;
                        }

                    }
                    else
                    {
                        frontLeftWheelCollider.motorTorque = 0;
                        frontRightWheelCollider.motorTorque = 0;
                        rearLeftWheelCollider.motorTorque = 0;
                        rearRightWheelCollider.motorTorque = 0;
                    }
                }

                UpdateAccelerationSlider();
            }
        }

        void OnBrake(InputValue brakeValue)
        {
            currentBrakeValue = brakeValue.Get<float>();
            //Debug.Log("Brake:" + currentBrakeValue.ToString());

            if (isStarted && ezerealLightController != null)
            {
                if (currentBrakeValue > 0)
                {
                    ezerealLightController.BrakeLightsOn();
                }
                else
                {
                    ezerealLightController.BrakeLightsOff();
                }
            }
        }

        void Braking()
        {
            if (currentBrakeValue > 0f)
            {
                frontLeftWheelCollider.brakeTorque = currentBrakeValue * brakePower;
                frontRightWheelCollider.brakeTorque = currentBrakeValue * brakePower;
            }
            else
            {
                frontLeftWheelCollider.brakeTorque = 0;
                frontRightWheelCollider.brakeTorque = 0;
            }
        }

        void OnHandbrake(InputValue handbrakeValue)
        {
            currentHandbrakeValue = handbrakeValue.Get<float>();

            isHandbrakeActive = !isHandbrakeActive;

            Debug.Log("Handbrake Value: " + currentHandbrakeValue);

            if (isStarted)
            {
                /// to add togglable handbrake (similar to a real one), add a bool "toggleMode". 
                /// Default to false to have normal controls for now. Later will default to True for realisticity
                /// if "toggleMode" is true, then probably do the drift, but with different values so that instead of removing friction from the wheels, it increases it?
                /// Maybe ask ChatGPT for some guidance on the overall function of fthe script. I have a feeling I need to change something in the WheelFrictionController script, too.

                /*
                 * 
                 * ORIGINAL CODE - to reactivate this, comment out anything to do with "isHandbrakeActive" and, in the inputActions, make "Handbrake" a Value instead of a Button
                if (currentHandbrakeValue > 0)
                {
                    if (ezerealWheelFrictionController != null)
                    {
                        ezerealWheelFrictionController.StartDrifting(currentHandbrakeValue);
                    }

                    if (ezerealLightController != null)
                    {
                        ezerealLightController.HandbrakeLightOn();
                    }
                }
                else
                {
                    if (ezerealWheelFrictionController != null)
                    {
                        ezerealWheelFrictionController.StopDrifting();
                    }

                    if (ezerealLightController != null)
                    {
                        ezerealLightController.HandbrakeLightOff();
                    }
                }*/

                if (isHandbrakeActive == true)
                {
                    if (ezerealWheelFrictionController != null)
                    {
                        ezerealWheelFrictionController.StartDrifting(currentHandbrakeValue);
                    }

                    if (ezerealLightController != null)
                    {
                        ezerealLightController.HandbrakeLightOn();
                    }
                }
                else if (isHandbrakeActive == false)
                {

                    currentHandbrakeValue = 0f;

                    if (ezerealWheelFrictionController != null)
                    {
                        ezerealWheelFrictionController.StopDrifting();
                    }

                    if (ezerealLightController != null)
                    {
                        ezerealLightController.HandbrakeLightOff();
                    }
                }
            }
        }

        void Handbraking()
        {
            if (currentHandbrakeValue > 0f)
            {
                rearLeftWheelCollider.motorTorque = 0;
                rearRightWheelCollider.motorTorque = 0;
                rearLeftWheelCollider.brakeTorque = currentHandbrakeValue * handbrakeForce;
                rearRightWheelCollider.brakeTorque = currentHandbrakeValue * handbrakeForce;


            }
            else
            {
                rearLeftWheelCollider.brakeTorque = 0;
                rearRightWheelCollider.brakeTorque = 0;
            }
        }

        void OnSteer(InputValue turnValue)
        {
            targetSteerAngle = turnValue.Get<float>() * maxSteerAngle;
        }

        void Steering()
        {
            float adjustedspeedFactor = Mathf.InverseLerp(20, maxForwardSpeed, currentSpeed); //minimum speed affecting steerAngle is 20
            float adjustedTurnAngle = targetSteerAngle * (1 - adjustedspeedFactor); //based on current speed.
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, adjustedTurnAngle, Time.deltaTime * steeringSpeed);

            frontLeftWheelCollider.steerAngle = currentSteerAngle;
            frontRightWheelCollider.steerAngle = currentSteerAngle;

            UpdateWheel(frontLeftWheelCollider, frontLeftWheelMesh);
            UpdateWheel(frontRightWheelCollider, frontRightWheelMesh);
            UpdateWheel(rearLeftWheelCollider, rearLeftWheelMesh);
            UpdateWheel(rearRightWheelCollider, rearRightWheelMesh);
        }

        void Slowdown()
        {
            if (vehicleRB != null)
            {
                if (currentAccelerationValue == 0 && currentBrakeValue == 0 && currentHandbrakeValue == 0)
                {
#if UNITY_6000_0_OR_NEWER
                    vehicleRB.linearVelocity = Vector3.Lerp(vehicleRB.linearVelocity, Vector3.zero, Time.deltaTime * decelerationSpeed);
#else
                    vehicleRB.velocity = Vector3.Lerp(vehicleRB.velocity, Vector3.zero, Time.deltaTime * decelerationSpeed);
#endif
                }
            }
        }

        //void OnDownShift()
        //{
        //    switch (currentGear)
        //    {
        //        case AutomaticGears.Reverse:
        //            //Debug.Log("Reverse, can't go any lower");
        //            break;

        //        case AutomaticGears.Neutral:
        //            currentGear--;
        //            UpdateGearText("R");
        //            if (isStarted && ezerealLightController != null)
        //            {
        //                ezerealLightController.ReverseLightsOn();
        //            }
        //            break;

        //        case AutomaticGears.Drive:
        //            currentGear--;
        //            UpdateGearText("N");
        //            break;
        //    }
        //}

        //void OnUpShift()
        //{
        //    switch (currentGear)
        //    {
        //        case AutomaticGears.Reverse:
        //            currentGear++;
        //            UpdateGearText("N");

        //            if (isStarted && ezerealLightController != null)
        //            {
        //                ezerealLightController.ReverseLightsOff();
        //            }

        //            break;
        //        case AutomaticGears.Neutral:
        //            currentGear++;
        //            UpdateGearText("D");
        //            break;
        //        case AutomaticGears.Drive:
        //            //Debug.Log("Drive, can't go any higher");
        //            break;
        //    }
        //}


        //// TEMPORARY SOLUTION TO GEAR SELECTION


        //// ------------------------------------------------------------------

        //// Replace OnUpShift and OnDownShift with these methods:

        //void OnGearDrive(InputValue value)
        //{
        //    // value.isPressed will be TRUE when the stick enters Gear 1
        //    // It will automatically become FALSE when the stick leaves Gear 1
        //    isDriveInputActive = value.isPressed;
        //    EvaluateGearState();
        //}

        //void OnGearReverse(InputValue value)
        //{
        //    // value.isPressed will be TRUE when the stick enters Reverse
        //    // It will automatically become FALSE when the stick leaves Reverse
        //    isReverseInputActive = value.isPressed;
        //    EvaluateGearState();
        //}

        //void EvaluateGearState()
        //{
        //    // 1. Check if the physical stick is currently in the Drive (Gear 1) slot
        //    if (isDriveInputActive)
        //    {
        //        currentGear = AutomaticGears.Drive;
        //        UpdateGearText("D");

        //        if (isStarted && ezerealLightController != null)
        //        {
        //            ezerealLightController.ReverseLightsOff();
        //        }
        //    }
        //    // 2. Check if the physical stick is currently in the Reverse slot
        //    else if (isReverseInputActive)
        //    {
        //        currentGear = AutomaticGears.Reverse;
        //        UpdateGearText("R");

        //        if (isStarted && ezerealLightController != null)
        //        {
        //            ezerealLightController.ReverseLightsOn();
        //        }
        //    }
        //    // 3. If neither slot is engaged, the stick is physically in the middle (Neutral)
        //    else
        //    {
        //        currentGear = AutomaticGears.Neutral;
        //        UpdateGearText("N");

        //        if (isStarted && ezerealLightController != null)
        //        {
        //            ezerealLightController.ReverseLightsOff();
        //        }
        //    }
        //}

        void PollGearState()
        {
            // Make sure the actions are assigned in the inspector before trying to read them
            if (driveGearAction == null || reverseGearAction == null) return;

            // 1. Read the raw physical state of the buttons every frame
            // This ignores "active devices" and just checks if the hardware button is closed/pressed
            isDriveInputActive = driveGearAction.action.IsPressed();
            isReverseInputActive = reverseGearAction.action.IsPressed();

            // 2. Evaluate and set the gears
            if (isDriveInputActive)
            {
                if (currentGear != AutomaticGears.Drive)
                {
                    currentGear = AutomaticGears.Drive;
                    UpdateGearText("D");
                    if (isStarted && ezerealLightController != null) ezerealLightController.ReverseLightsOff();
                }
            }
            else if (isReverseInputActive)
            {
                if (currentGear != AutomaticGears.Reverse)
                {
                    currentGear = AutomaticGears.Reverse;
                    UpdateGearText("R");
                    if (isStarted && ezerealLightController != null) ezerealLightController.ReverseLightsOn();
                }
            }
            else
            {
                // 3. If neither slot is engaged, the stick is physically in Neutral
                if (currentGear != AutomaticGears.Neutral)
                {
                    currentGear = AutomaticGears.Neutral;
                    UpdateGearText("N");
                    if (isStarted && ezerealLightController != null) ezerealLightController.ReverseLightsOff();
                }
            }
        }



        private void FixedUpdate()
        {

            // --- NEW: Call the polling method every physics frame ---
            // THE THING, THE THING!!
            PollGearState();



            Acceleration();

            Braking();

            Handbraking();

            Steering();

            Slowdown();

            RotateSteeringWheel();

            if
                (
                    Mathf.Abs(frontLeftWheelCollider.rpm) < stopThreshold &&
                    Mathf.Abs(frontRightWheelCollider.rpm) < stopThreshold &&
                    Mathf.Abs(rearLeftWheelCollider.rpm) < stopThreshold &&
                    Mathf.Abs(rearRightWheelCollider.rpm) < stopThreshold
                )
            {
                stationary = true;
            }
            else
            {
                stationary = false;
            }

            if (vehicleRB != null) // Unity uses m/s as for default. So I convert from m/s to km/h. For mph use 2.23694f instead of 3.6f.
            {
#if UNITY_6000_0_OR_NEWER
                currentSpeed = Vector3.Dot(vehicleRB.gameObject.transform.forward, vehicleRB.linearVelocity);
                currentSpeed *= 2.23694f;
                UpdateSpeedText(currentSpeed);
#else
                currentSpeed = Vector3.Dot(vehicleRB.gameObject.transform.forward, vehicleRB.velocity);
                currentSpeed *= 2.23694f; 
                UpdateSpeedText(currentSpeed);
#endif

            }


            FrontLeftWheelRPM = frontLeftWheelCollider.rpm;
            FrontRightWheelRPM = frontRightWheelCollider.rpm;
            RearLeftWheelRPM = rearLeftWheelCollider.rpm;
            RearRightWheelRPM = rearRightWheelCollider.rpm;
        }

        private void UpdateWheel(WheelCollider col, Transform mesh)
        {
            col.GetWorldPose(out Vector3 position, out Quaternion rotation);
            mesh.SetPositionAndRotation(position, rotation);
        }


        void RotateSteeringWheel()
        {
            float currentXAngle = steeringWheel.transform.localEulerAngles.x; // Maximum steer angle in degrees

            // Calculate the rotation based on the steer angle
            float normalizedSteerAngle = Mathf.Clamp(frontLeftWheelCollider.steerAngle, -maxSteerAngle, maxSteerAngle);
            float rotation = Mathf.Lerp(maxSteeringWheelRotation, -maxSteeringWheelRotation, (normalizedSteerAngle + maxSteerAngle) / (2 * maxSteerAngle));

            // Set the local rotation of the steering wheel
            steeringWheel.localRotation = Quaternion.Euler(currentXAngle, 0, rotation);
        }

        void UpdateGearText(string gear)
        {
            currentGearTMP_UI.text = gear;
            currentGearTMP_Dashboard.text = gear;
        }

        void UpdateSpeedText(float speed)
        {
            speed = Mathf.Abs(speed);

            currentSpeedTMP_UI.text = speed.ToString("F0");
            currentSpeedTMP_Dashboard.text = speed.ToString("F0");
        }

        void UpdateAccelerationSlider()
        {
            if (currentGear == AutomaticGears.Drive || currentGear == AutomaticGears.Reverse)
            {
                accelerationSlider.value = Mathf.Lerp(accelerationSlider.value, currentAccelerationValue, Time.deltaTime * 15f);
            }
            else
            {
                accelerationSlider.value = 0;
            }
        }

        public bool InAir()
        {
            foreach (WheelCollider wheel in wheels)
            {
                if (wheel.GetGroundHit(out _))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
