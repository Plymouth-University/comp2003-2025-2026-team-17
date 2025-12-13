using UnityEngine;
using UnityEngine.InputSystem;

namespace Ezereal
{
    public class EzerealCameraController : MonoBehaviour
    {
        [SerializeField] CameraViews currentCameraView = CameraViews.cockpit;

        [SerializeField] private GameObject[] cameras; // Assume cameras are in order: cockpit, close, far, locked, wheel

        private void Awake()
        {
            SetCameraView(CameraViews.close);
        }

        void OnSwitchCamera()
        {
            currentCameraView = (CameraViews)(((int)currentCameraView + 1) % cameras.Length);
            SetCameraView(currentCameraView);
        }

        private void SetCameraView(CameraViews view)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].SetActive(i == (int)view);
            }
        }
    }
}
