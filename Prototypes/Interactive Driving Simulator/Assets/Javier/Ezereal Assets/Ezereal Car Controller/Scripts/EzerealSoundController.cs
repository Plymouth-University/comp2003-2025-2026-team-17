using UnityEngine;

namespace Ezereal
{
    public class EzerealSoundController : MonoBehaviour // This system plays tire and engine sounds.
    {
        [Header("References")]
        [SerializeField] bool useSounds = true;
        [SerializeField] EzerealCarController ezerealCarController;
        [SerializeField] AudioSource tireAudio;
        [SerializeField] public AudioSource engineAudio;
        [SerializeField] float enginePitchValue = 1f;

        [Header("Settings")]
        public float maxVolume = 0.5f; // Maximum volume for high speeds

        [Header("Debug")]
        [SerializeField] bool alreadyPlaying;

        void Start()
        {
            if (useSounds)
            {
                alreadyPlaying = false;

                if (ezerealCarController == null || ezerealCarController.vehicleRB == null || tireAudio == null || engineAudio == null)
                {
                    Debug.LogWarning("ezerealSoundController is missing some references. Ignore or attach them if you want to have sound controls.");


                }

                if (tireAudio != null)
                {
                    tireAudio.volume = 0f; // Start with zero volume
                    tireAudio.Stop();
                }
            }
        }

        public void TurnOnEngineSound()
        {
            if (useSounds)
            {
                if (engineAudio != null)
                {
                    if (!engineAudio.isPlaying)
                    {
                        // will start playing once, if it isn't playing already
                        engineAudio.Play();
                    }
                }
            }
        }

        public void TurnOffEngineSound()
        {
            // remove this because the audio will always be playing, just at 0 volume
            //if (useSounds)
            //{
            //    if (engineAudio != null)
            //    {
            //        engineAudio.Stop();
            //    }
            //}
        }

        void Update()
        {
            if (useSounds)
            {
#if UNITY_6000_0_OR_NEWER
                if (ezerealCarController != null && ezerealCarController.vehicleRB != null && tireAudio != null && engineAudio != null)
                {
                    if (!ezerealCarController.stationary && !alreadyPlaying && !ezerealCarController.InAir())
                    {
                        tireAudio.Play();
                        alreadyPlaying = true;
                        Debug.Log("Playing new");
                    }
                    else if (ezerealCarController.stationary || ezerealCarController.InAir())
                    {
                        tireAudio.Stop();
                        alreadyPlaying = false;
                    }

                    // Get the car's current speed
                    float speed = ezerealCarController.vehicleRB.linearVelocity.magnitude;

                    // Calculate the volume based on speed
                    float targetVolume = Mathf.Clamp01(speed / 15) * maxVolume;


                    tireAudio.volume = targetVolume;

                    //Tire Pitch

                    float tireSoundPitch = 0.8f + (Mathf.Abs(ezerealCarController.vehicleRB.linearVelocity.magnitude) / 50f);
                    tireAudio.pitch = tireSoundPitch;

                    //Engine Pitch

                    /* ORIGINAL CODE BLOCK
                    //float engineSoundPitch = 0.8f + (Mathf.Abs(ezerealCarController.vehicleRB.linearVelocity.magnitude) / 25f);
                    //engineAudio.pitch = engineSoundPitch;
                    */

                    float targetPitch = 0.7f + (ezerealCarController.currentAccelerationValue * enginePitchValue);

                    float lerpSpeed = (targetPitch > engineAudio.pitch) ? 5f : 2f; // change this to control how fast the pitch goes up/down
                    engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, targetPitch, Time.deltaTime * lerpSpeed);

                    //Engine Volume

                    // Determine our target volume based on if the car is started
                    float targetEngineVolume = ezerealCarController.isStarted ? 0.15f : 0f;
                    
                    // Smoothly transition the volume (adjust '5f' to make the fade faster or slower)
                    engineAudio.volume = Mathf.Lerp(engineAudio.volume, targetEngineVolume, Time.deltaTime * 5f);
#else
                if (ezerealCarController != null && ezerealCarController.vehicleRB != null && tireAudio != null && engineAudio != null)
            {
                if (!ezerealCarController.stationary && !alreadyPlaying && !ezerealCarController.InAir())
                {
                    tireAudio.Play();
                    alreadyPlaying = true;
                        Debug.Log("Playing old");
                    }
                else if (ezerealCarController.stationary || ezerealCarController.InAir())
                {
                    tireAudio.Stop();
                    alreadyPlaying = false;
                }

                // Get the car's current speed
                float speed = ezerealCarController.vehicleRB.velocity.magnitude;

                // Calculate the volume based on speed
                float targetVolume = Mathf.Clamp01(speed / 15) * maxVolume;


                tireAudio.volume = targetVolume;

                //Tire Pitch

                float tireSoundPitch = 0.8f + (Mathf.Abs(ezerealCarController.vehicleRB.velocity.magnitude) / 50f);
                tireAudio.pitch = tireSoundPitch;

                //Engine Pitch

                float engineSoundPitch = 0.8f + (Mathf.Abs(ezerealCarController.vehicleRB.velocity.magnitude) / 25f);
                engineAudio.pitch = engineSoundPitch;
#endif
                }
            }
        }
    }
}
