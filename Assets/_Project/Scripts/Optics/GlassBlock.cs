using System.Collections.Generic;
using UnityEngine;

namespace PassTheLight.Optics
{
    public class GlassBlock : MonoBehaviour
    {
        const float AirIndex = 1f;
        const float SurfaceOffset = 0.01f;

        [SerializeField] float refractiveIndex = 1.5f;

        public bool TryPassThrough(RaycastHit entry, Vector3 incoming, List<Vector3> points,
                                   out Vector3 exitPoint, out Vector3 exitDirection)
        {
            exitPoint = Vector3.zero;
            exitDirection = Vector3.zero;

            if (!TryBend(incoming, entry.normal, AirIndex, refractiveIndex, out Vector3 insideDirection))
                return false;

            if (!TryFindExit(entry, insideDirection, out RaycastHit exit))
                return false;

            points.Add(exit.point);

            if (!TryBend(insideDirection, exit.normal, refractiveIndex, AirIndex, out exitDirection))
                return false;

            exitPoint = exit.point + exitDirection * SurfaceOffset;
            return true;
        }

        static bool TryFindExit(RaycastHit entry, Vector3 insideDirection, out RaycastHit exit)
        {
            Collider block = entry.collider;
            float distanceAcross = block.bounds.size.magnitude * 2f;
            Vector3 pointBeyondBlock = entry.point + insideDirection * distanceAcross;
            Ray rayBackTowardsBlock = new Ray(pointBeyondBlock, -insideDirection);
            return block.Raycast(rayBackTowardsBlock, out exit, distanceAcross);
        }

        static bool TryBend(Vector3 direction, Vector3 normal, float fromIndex, float toIndex, out Vector3 bent)
        {
            float ratio = fromIndex / toIndex;

            float cosIncoming = -Vector3.Dot(normal, direction);
            if (cosIncoming < 0f)
            {
                normal = -normal;
                cosIncoming = -cosIncoming;
            }

            float sinOutgoingSquared = ratio * ratio * (1f - cosIncoming * cosIncoming);
            bool totalInternalReflection = sinOutgoingSquared > 1f;
            if (totalInternalReflection)
            {
                bent = Vector3.zero;
                return false;
            }

            float cosOutgoing = Mathf.Sqrt(1f - sinOutgoingSquared);
            bent = (ratio * direction + (ratio * cosIncoming - cosOutgoing) * normal).normalized;
            return true;
        }
    }
}
