using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PassTheLight.Optics
{
    [RequireComponent(typeof(LineRenderer))]
    public class BeamEffects : MonoBehaviour
    {
        [SerializeField] Material glowMaterial;
        [SerializeField] Mesh glowMesh;
        [SerializeField] float glowSize = 0.35f;
        [SerializeField] float shimmerAmount = 0.25f;
        [SerializeField] float shimmerSpeed = 9f;

        LineRenderer line;
        Camera cam;
        float normalWidth;
        Vector3[] beamPoints = new Vector3[32];
        readonly List<Transform> glowDots = new();

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            cam = Camera.main;
            normalWidth = line.widthMultiplier;
        }

        void OnDisable()
        {
            foreach (Transform dot in glowDots) dot.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            float wave = Mathf.Sin(Time.time * shimmerSpeed) * 0.5f + 0.5f;
            line.widthMultiplier = normalWidth * (1f + shimmerAmount * wave);

            int pointCount = line.positionCount;
            if (beamPoints.Length < pointCount) beamPoints = new Vector3[pointCount * 2];
            line.GetPositions(beamPoints);

            int cornerCount = Mathf.Max(0, pointCount - 1);
            while (glowDots.Count < cornerCount) glowDots.Add(CreateGlowDot());

            float dotSize = glowSize * (0.85f + 0.3f * wave);
            for (int i = 0; i < glowDots.Count; i++)
            {
                bool used = i < cornerCount;
                glowDots[i].gameObject.SetActive(used);
                if (!used) continue;

                glowDots[i].position = beamPoints[i + 1];
                glowDots[i].rotation = cam.transform.rotation;
                glowDots[i].localScale = Vector3.one * dotSize;
            }
        }

        Transform CreateGlowDot()
        {
            GameObject dot = new GameObject("BeamGlow");
            dot.transform.SetParent(transform, false);
            dot.AddComponent<MeshFilter>().sharedMesh = glowMesh;

            MeshRenderer dotRenderer = dot.AddComponent<MeshRenderer>();
            dotRenderer.sharedMaterial = glowMaterial;
            dotRenderer.shadowCastingMode = ShadowCastingMode.Off;
            dotRenderer.receiveShadows = false;
            return dot.transform;
        }
    }
}
