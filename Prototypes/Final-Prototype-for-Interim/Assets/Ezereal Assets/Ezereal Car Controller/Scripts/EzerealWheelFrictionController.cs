using UnityEngine;

namespace Ezereal
{
    public class EzerealWheelFrictionController : MonoBehaviour
    {
        [Header("Ezereal References")]
        [SerializeField] EzerealCarController ezerealCarController;

        WheelFrictionCurve fLWSidewaysFriction;
        WheelFrictionCurve fRWSidewaysFriction;
        WheelFrictionCurve rLWSidewaysFriction;
        WheelFrictionCurve rRWSidewaysFriction;

        WheelFrictionCurve fLWForwardFriction;
        WheelFrictionCurve fRWForwardFriction;
        WheelFrictionCurve rLWForwardFriction;
        WheelFrictionCurve rRWForwardFriction;

        void Start()
        {
            if (ezerealCarController != null)
            {
                SetForwardFriction();
                SetSidewaysFriction();
            }
            else
            {
                Debug.LogWarning("ezerealWheelFrictionController is missing ezerealCarController. Ignore it or attach one if you want to have friction controls.");
            }

        }

        void SetForwardFriction()
        {
            fLWForwardFriction = new WheelFrictionCurve
            {
                extremumSlip = ezerealCarController.frontLeftWheelCollider.forwardFriction.extremumSlip,
                extremumValue = ezerealCarController.frontLeftWheelCollider.forwardFriction.extremumValue,
                asymptoteSlip = ezerealCarController.frontLeftWheelCollider.forwardFriction.asymptoteSlip,
                asymptoteValue = ezerealCarController.frontLeftWheelCollider.forwardFriction.asymptoteValue,
                stiffness = ezerealCarController.frontLeftWheelCollider.forwardFriction.stiffness
            };

            fRWForwardFriction = new WheelFrictionCurve
            {
                extremumSlip = ezerealCarController.frontRightWheelCollider.forwardFriction.extremumSlip,
                extremumValue = ezerealCarController.frontRightWheelCollider.forwardFriction.extremumValue,
                asymptoteSlip = ezerealCarController.frontRightWheelCollider.forwardFriction.asymptoteSlip,
                asymptoteValue = ezerealCarController.frontRightWheelCollider.forwardFriction.asymptoteValue,
                stiffness = ezerealCarController.frontRightWheelCollider.forwardFriction.stiffness
            };

            rLWForwardFriction = new WheelFrictionCurve
            {
                extremumSlip = ezerealCarController.rearLeftWheelCollider.forwardFriction.extremumSlip,
                extremumValue = ezerealCarController.rearLeftWheelCollider.forwardFriction.extremumValue,
                asymptoteSlip = ezerealCarController.rearLeftWheelCollider.forwardFriction.asymptoteSlip,
                asymptoteValue = ezerealCarController.rearLeftWheelCollider.forwardFriction.asymptoteValue,
                stiffness = ezerealCarController.rearLeftWheelCollider.forwardFriction.stiffness
            };

            rRWForwardFriction = new WheelFrictionCurve
            {
                extremumSlip = ezerealCarController.rearRightWheelCollider.forwardFriction.extremumSlip,
                extremumValue = ezerealCarController.rearRightWheelCollider.forwardFriction.extremumValue,
                asymptoteSlip = ezerealCarController.rearRightWheelCollider.forwardFriction.asymptoteSlip,
                asymptoteValue = ezerealCarController.rearRightWheelCollider.forwardFriction.asymptoteValue,
                stiffness = ezerealCarController.rearRightWheelCollider.forwardFriction.stiffness
            };
        }

        void SetSidewaysFriction()
        {
            fLWSidewaysFriction = new WheelFrictionCurve
            {
                extremumSlip = ezerealCarController.frontLeftWheelCollider.sidewaysFriction.extremumSlip,
                extremumValue = ezerealCarController.frontLeftWheelCollider.sidewaysFriction.extremumValue,
                asymptoteSlip = ezerealCarController.frontLeftWheelCollider.sidewaysFriction.asymptoteSlip,
                asymptoteValue = ezerealCarController.frontLeftWheelCollider.sidewaysFriction.asymptoteValue,
                stiffness = ezerealCarController.frontLeftWheelCollider.sidewaysFriction.stiffness
            };

            fRWSidewaysFriction = new WheelFrictionCurve
            {
                extremumSlip = ezerealCarController.frontRightWheelCollider.sidewaysFriction.extremumSlip,
                extremumValue = ezerealCarController.frontRightWheelCollider.sidewaysFriction.extremumValue,
                asymptoteSlip = ezerealCarController.frontRightWheelCollider.sidewaysFriction.asymptoteSlip,
                asymptoteValue = ezerealCarController.frontRightWheelCollider.sidewaysFriction.asymptoteValue,
                stiffness = ezerealCarController.frontRightWheelCollider.sidewaysFriction.stiffness
            };

            rLWSidewaysFriction = new WheelFrictionCurve
            {
                extremumSlip = ezerealCarController.rearLeftWheelCollider.sidewaysFriction.extremumSlip,
                extremumValue = ezerealCarController.rearLeftWheelCollider.sidewaysFriction.extremumValue,
                asymptoteSlip = ezerealCarController.rearLeftWheelCollider.sidewaysFriction.asymptoteSlip,
                asymptoteValue = ezerealCarController.rearLeftWheelCollider.sidewaysFriction.asymptoteValue,
                stiffness = ezerealCarController.rearLeftWheelCollider.sidewaysFriction.stiffness
            };

            rRWSidewaysFriction = new WheelFrictionCurve
            {
                extremumSlip = ezerealCarController.rearRightWheelCollider.sidewaysFriction.extremumSlip,
                extremumValue = ezerealCarController.rearRightWheelCollider.sidewaysFriction.extremumValue,
                asymptoteSlip = ezerealCarController.rearRightWheelCollider.sidewaysFriction.asymptoteSlip,
                asymptoteValue = ezerealCarController.rearRightWheelCollider.sidewaysFriction.asymptoteValue,
                stiffness = ezerealCarController.rearRightWheelCollider.sidewaysFriction.stiffness
            };
        }

        public void StartDrifting(float currentHandbrakeValue)
        {
            if (ezerealCarController != null)
            {
                //use if you need to

                //rLwheelForwardFriction.extremumSlip = 
                //rRwheelForwardFriction.extremumSlip = 
                //rLwheelForwardFriction.extremumValue = 
                //rRwheelForwardFriction.extremumValue = 

                rLWSidewaysFriction.extremumSlip = 3f * currentHandbrakeValue;
                rRWSidewaysFriction.extremumSlip = 3f * currentHandbrakeValue;
                rLWSidewaysFriction.extremumValue = 0.7f * currentHandbrakeValue;
                rRWSidewaysFriction.extremumValue = 0.7f * currentHandbrakeValue;

                //Debug.Log(rLWSidewaysFriction.extremumSlip.ToString());

                //ezerealCarController.rearLeftWheelCollider.forwardFriction = rLwheelForwardFriction;
                //ezerealCarController.rearRightWheelCollider.forwardFriction = rRwheelForwardFriction;

                ezerealCarController.rearLeftWheelCollider.sidewaysFriction = rLWSidewaysFriction;
                ezerealCarController.rearRightWheelCollider.sidewaysFriction = rRWSidewaysFriction;
            }
        }

        public void StopDrifting()
        {
            if (ezerealCarController != null)
            {
                //use if you need to

                //rLwheelForwardFriction.extremumSlip = 
                //rRwheelForwardFriction.extremumSlip = 
                //rLwheelForwardFriction.extremumValue = 
                //rRwheelForwardFriction.extremumValue = 

                //Set default value here
                rLWSidewaysFriction.extremumSlip = 0.2f;
                rRWSidewaysFriction.extremumSlip = 0.2f;
                rLWSidewaysFriction.extremumValue = 1f;
                rRWSidewaysFriction.extremumValue = 1f;

                //Debug.Log(rLWSidewaysFriction.extremumSlip.ToString());

                //ezerealCarController.rearLeftWheelCollider.forwardFriction = rLwheelForwardFriction;
                //ezerealCarController.rearRightWheelCollider.forwardFriction = rRwheelForwardFriction;

                ezerealCarController.rearLeftWheelCollider.sidewaysFriction = rLWSidewaysFriction;
                ezerealCarController.rearRightWheelCollider.sidewaysFriction = rRWSidewaysFriction;
            }
        }
    }
}
