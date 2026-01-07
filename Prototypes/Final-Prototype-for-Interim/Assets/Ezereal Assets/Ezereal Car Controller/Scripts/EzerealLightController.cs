using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace Ezereal
{
    public class EzerealLightController : MonoBehaviour // This system uses Input System and has no references. Some methods here are called from other scripts.
    {
        [Header("Beam Lights")]

        [SerializeField] LightBeam currentBeam = LightBeam.off;

        [SerializeField] GameObject[] lowBeamHeadlights;
        [SerializeField] GameObject[] highBeamHeadlights;
        [SerializeField] GameObject[] lowBeamSpotlights;
        [SerializeField] GameObject[] highBeamSpotlights;
        [SerializeField] GameObject[] rearLights;

        [Header("Brake Lights")]
        [SerializeField] GameObject[] brakeLights;

        [Header("Handbrake Light")]
        [SerializeField] GameObject[] handbrakeLight;

        [Header("Reverse Lights")]
        [SerializeField] GameObject[] reverseLights;

        [Header("Turn Lights")]
        [SerializeField] GameObject[] leftTurnLights;
        [SerializeField] GameObject[] rightTurnLights;

        [Header("Misc Lights")]
        [Tooltip("Any additional lights. Interior lights.")]
        [SerializeField] GameObject[] miscLights;

        [Header("Settings")]
        [SerializeField] float lightBlinkDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] bool leftTurnActive = false;
        [SerializeField] bool rightTurnActive = false;
        [SerializeField] bool hazardLightsActive = false;

        private void Start()
        {
            AllLightsOff();
        }

        public void AllLightsOff()
        {
            AllBeamsOff();
            ReverseLightsOff();
            TurnLightsOff();
            BrakeLightsOff();
            HandbrakeLightOff();
            //MiscLightsOff();
        }

        void OnLowBeamLight()
        {
            switch (currentBeam)
            {
                case LightBeam.off:
                    LowBeamOn();
                    break;
                case LightBeam.low:
                    AllBeamsOff();
                    break;
                case LightBeam.high:
                    AllBeamsOff();
                    break;
            }
        }

        void OnHighBeamLight()
        {
            switch (currentBeam)
            {
                case LightBeam.off:
                    HighBeamOn();
                    break;
                case LightBeam.low:
                    HighBeamOn();
                    break;
                case LightBeam.high:
                    AllBeamsOff();
                    break;
            }
        }
        void OnLeftTurnSignal()
        {
            if (!hazardLightsActive)
            {
                StopAllCoroutines();
                TurnLightsOff();
                rightTurnActive = false;
                leftTurnActive = !leftTurnActive;

                if (leftTurnActive)
                {
                    StartCoroutine(TurnSignalController(leftTurnLights, leftTurnActive));
                }
            }
        }

        void OnRightTurnSignal()
        {
            if (!hazardLightsActive)
            {
                StopAllCoroutines();
                TurnLightsOff();
                leftTurnActive = false;
                rightTurnActive = !rightTurnActive;

                if (rightTurnActive)
                {
                    StartCoroutine(TurnSignalController(rightTurnLights, rightTurnActive));
                }
            }
        }

        void OnHazardLights()
        {
            StopAllCoroutines();
            TurnLightsOff();
            leftTurnActive = false;
            rightTurnActive = false;
            hazardLightsActive = !hazardLightsActive;

            if (hazardLightsActive)
            {
                StartCoroutine(HazardLightsController());
            }
        }

        IEnumerator TurnSignalController(GameObject[] turnLights, bool isActive)
        {
            while (isActive)
            {
                SetLight(turnLights, true);
                yield return new WaitForSeconds(lightBlinkDelay);
                SetLight(turnLights, false);
                yield return new WaitForSeconds(lightBlinkDelay);
            }
        }

        IEnumerator HazardLightsController()
        {
            while (hazardLightsActive)
            {
                TurnLightsOn();
                yield return new WaitForSeconds(lightBlinkDelay);
                TurnLightsOff();
                yield return new WaitForSeconds(lightBlinkDelay);
            }
        }
        void SetLight(GameObject[] lights, bool isActive)
        {
            if (isActive)
            {
                foreach (var light in lights)
                {
                    light.SetActive(true);
                }
            }
            else
            {
                foreach (var light in lights)
                {
                    light.SetActive(false);
                }
            }
        }

        void AllBeamsOff()
        {
            SetLight(lowBeamHeadlights, false);
            SetLight(lowBeamSpotlights, false);
            SetLight(rearLights, false);

            SetLight(highBeamHeadlights, false);
            SetLight(highBeamSpotlights, false);

            currentBeam = LightBeam.off;
        }

        void LowBeamOn()
        {
            SetLight(lowBeamHeadlights, true);
            SetLight(lowBeamSpotlights, true);
            SetLight(rearLights, true);

            SetLight(highBeamHeadlights, false);
            SetLight(highBeamSpotlights, false);

            currentBeam = LightBeam.low;
        }

        void HighBeamOn()
        {
            SetLight(lowBeamHeadlights, true);
            SetLight(lowBeamSpotlights, false);
            SetLight(rearLights, true);

            SetLight(highBeamHeadlights, true);
            SetLight(highBeamSpotlights, true);

            currentBeam = LightBeam.high;
        }

        void TurnLightsOff()
        {
            SetLight(leftTurnLights, false);
            SetLight(rightTurnLights, false);
        }

        void TurnLightsOn()
        {
            SetLight(leftTurnLights, true);
            SetLight(rightTurnLights, true);
        }

        void SetHazardLightsOn()
        {
            SetLight(leftTurnLights, true);
            SetLight(rightTurnLights, true);
        }

        public void BrakeLightsOff()
        {
            SetLight(brakeLights, false);
        }

        public void BrakeLightsOn()
        {
            SetLight(brakeLights, true);
        }

        public void HandbrakeLightOff()
        {
            SetLight(handbrakeLight, false);
        }

        public void HandbrakeLightOn()
        {
            SetLight(handbrakeLight, true);
        }

        public void ReverseLightsOff()
        {
            SetLight(reverseLights, false);
        }

        public void ReverseLightsOn()
        {
            SetLight(reverseLights, true);
        }

        public void MiscLightsOff() // Interior Lights
        {
            SetLight(miscLights, false);
        }

        public void MiscLightsOn() // Interior Lights
        {
            SetLight(miscLights, true);
        }
    }
}
