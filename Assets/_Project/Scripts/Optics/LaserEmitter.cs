using System.Collections.Generic;
using UnityEngine;

namespace PassTheLight.Optics
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserEmitter : MonoBehaviour
    {
        const float SurfaceOffset = 0.01f;

        [SerializeField] float maxDistance = 30f;
        [SerializeField] int maxBounces = 12;
        [SerializeField] LayerMask hitLayers = ~0;

        LineRenderer line;
        readonly List<Vector3> points = new();

        void Awake() => line = GetComponent<LineRenderer>();

        void Update()
        {
            TraceBeam();
            line.positionCount = points.Count;
            line.SetPositions(points.ToArray());
        }

        void TraceBeam()
        {
            points.Clear();
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;
            points.Add(origin);

            for (int i = 0; i < maxBounces; i++)
            {
                if (!Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, hitLayers))
                {
                    points.Add(origin + direction * maxDistance);
                    return;
                }
                points.Add(hit.point);

                if (hit.collider.TryGetComponent(out Mirror mirror))
                {
                    mirror.RegisterHit(direction, hit.normal);
                    direction = Vector3.Reflect(direction, hit.normal);
                    origin = hit.point + direction * SurfaceOffset;
                    continue;
                }

                if (hit.collider.TryGetComponent(out GlassBlock glass))
                {
                    if (glass.TryPassThrough(hit, direction, points, out origin, out direction))
                        continue;
                    return;
                }

                if (hit.collider.TryGetComponent(out StudentDesk desk))
                    desk.Illuminate();
                return;
            }
        }
    }
}
