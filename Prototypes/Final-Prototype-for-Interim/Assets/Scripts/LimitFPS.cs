using UnityEngine;

public class LimitFPS : MonoBehaviour
{

    [SerializeField] private int FPSTarget = 60;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        #if UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FPSTarget;
        #endif
    }

  
}
