using UnityEngine;

namespace Ezereal
{
    public class EzerealSuspensionBumpDetector : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] float bumpForceThreshold = 12000f;
        [SerializeField] float cooldownTime = 0.1f; // Time in seconds to delay between sound plays
        [SerializeField] float nextPlayTime = 0f;

        WheelCollider wheelCollider;
        AudioSource bumpSound;

        void Start()
        {
            bumpSound = GetComponent<AudioSource>();
            wheelCollider = GetComponent<WheelCollider>();
        }

        void Update()
        {
            if (wheelCollider.GetGroundHit(out WheelHit wheelHit))
            {
                // Check the force applied to the suspension to detect a bump
                if (wheelHit.force > bumpForceThreshold)
                {
                    // Only play the sound if cooldown time has passed
                    if (Time.time >= nextPlayTime)
                    {
                        bumpSound.Play();
                        nextPlayTime = Time.time + cooldownTime;
                    }
                }
            }
        }
    }
}
