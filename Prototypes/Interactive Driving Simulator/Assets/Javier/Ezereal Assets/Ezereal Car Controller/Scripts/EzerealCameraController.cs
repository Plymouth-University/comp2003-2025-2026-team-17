using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Ezereal
{
    public class EzerealCameraController : MonoBehaviour
    {
        [SerializeField] public CameraViews currentCameraView = CameraViews.cockpit;
        public CameraViews previousCameraView;

        [SerializeField] private GameObject[] cameras; // Assume cameras are in order: cockpit, close, far, locked, wheel

        [SerializeField] private CinemachinePanTilt cockpitPanTilt;

        // Target angles for looking
        private float lookLeftAngle = -51f;
        private float lookRightAngle = 51f;

        // Track if buttons are currently being held
        private bool isLookingLeft = false;
        private bool isLookingRight = false;

        private void Awake()
        {
            SetCameraView(currentCameraView);
        }

        void OnSwitchCamera()
        {
            currentCameraView = (CameraViews)(((int)currentCameraView + 1) % cameras.Length);
            previousCameraView = currentCameraView;
            SetCameraView(currentCameraView);
        }

        public void SetCameraView(CameraViews view)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].SetActive(i == (int)view);
            }
        }

        void OnLookLeft(InputValue value)
        {
            // value.isPressed is true when holding the left key on the thrustmaster thingy, false when released
            isLookingLeft = value.isPressed;
            UpdateCameraPan();
        }

        void OnLookRight(InputValue value)
        {
            // same as left, but right lol
            isLookingRight = value.isPressed;
            UpdateCameraPan();
        }

        // --- CAMERA PAN LOGIC ---

        private void UpdateCameraPan()
        {
            if (cockpitPanTilt == null)
            {
                Debug.LogWarning("Cockpit Pan Tilt component is not assigned in the EzerealCameraController!");
                return;
            }

            if (isLookingLeft && !isLookingRight)
            {
                cockpitPanTilt.PanAxis.Value = lookLeftAngle;
            }
            else if (isLookingRight && !isLookingLeft)
            {
                cockpitPanTilt.PanAxis.Value = lookRightAngle;
            }
            else
            {
                // If neither are held, or BOTH are held simultaneously, snap back to center
                cockpitPanTilt.PanAxis.Value = 0f;
            }
        }

    }
}
