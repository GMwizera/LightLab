using UnityEngine;

namespace PassTheLight.Optics
{
    public class Mirror : MonoBehaviour
    {
        public float LastIncidenceAngle { get; private set; }

        public bool IsInBeam => Time.frameCount - lastHitFrame <= 1;

        int lastHitFrame = -10;

        public void RegisterHit(Vector3 incomingDirection, Vector3 surfaceNormal)
        {
            LastIncidenceAngle = Vector3.Angle(-incomingDirection, surfaceNormal);
            lastHitFrame = Time.frameCount;
        }
    }
}
