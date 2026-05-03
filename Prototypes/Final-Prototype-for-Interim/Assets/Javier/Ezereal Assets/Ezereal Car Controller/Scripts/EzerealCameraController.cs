using UnityEngine;
using UnityEngine.InputSystem;

namespace Ezereal
{
    public class EzerealCameraController : MonoBehaviour
    {
        [SerializeField] public CameraViews currentCameraView = CameraViews.cockpit;
        public CameraViews previousCameraView;

        [SerializeField] private GameObject[] cameras; // Assume cameras are in order: cockpit, close, far, locked, wheel

        private void Awake()
        {
            SetCameraView(currentCameraView);
        }

        void OnSwitchCamera()
        {
            currentCameraView = (CameraViews)(((int)currentCameraView + 1) % cameras.Length);
            //previousCameraView = currentCameraView;
            SetCameraView(currentCameraView);
        }

        public void SetCameraView(CameraViews view)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].SetActive(i == (int)view);
            }
        }
    }
}
