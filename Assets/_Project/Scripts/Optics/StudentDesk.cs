using System;
using System.Collections;
using PassTheLight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PassTheLight.Optics
{
    public class StudentDesk : MonoBehaviour
    {
        [SerializeField] string studentDream = "Future engineer";
        [SerializeField] Transform student;
        [SerializeField] Light glow;
        [SerializeField] Color darkColor = new(0.3f, 0.3f, 0.38f);
        [SerializeField] Color litColor = Color.white;
        [SerializeField] LaserEmitter relayBeam;
        [SerializeField] GameObject relayHint;
        [SerializeField] ParticleSystem sparkles;
        [SerializeField] Text label;
        [SerializeField] Transform waitingMarker;

        public bool IsLit { get; private set; }
        public event Action<StudentDesk> LitChanged;

        int lastFrameHitByBeam = -1;
        Renderer[] studentRenderers = new Renderer[0];
        Vector3 studentSize;
        Vector3 markerStartPosition;

        void Awake()
        {
            studentRenderers = student.GetComponentsInChildren<Renderer>();
            studentSize = student.localScale;
            markerStartPosition = waitingMarker.localPosition;
            label.text = studentDream;
        }

        void Start() => ShowLitState();

        public void Illuminate() => lastFrameHitByBeam = Time.frameCount;

        public void ResetState()
        {
            lastFrameHitByBeam = -1;
            IsLit = false;
            ShowLitState();
        }

        void Update()
        {
            if (IsLit) return;
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 3f + transform.position.x)) * 0.25f;
            waitingMarker.localPosition = markerStartPosition + Vector3.up * bob;
        }

        void LateUpdate()
        {
            bool hitThisFrame = lastFrameHitByBeam == Time.frameCount;
            if (hitThisFrame == IsLit) return;

            IsLit = hitThisFrame;
            ShowLitState();
            if (IsLit) Celebrate();
            LitChanged?.Invoke(this);
        }

        void ShowLitState()
        {
            foreach (Renderer part in studentRenderers)
                foreach (Material material in part.materials)
                    material.color = IsLit ? litColor : darkColor;

            glow.enabled = IsLit;
            label.gameObject.SetActive(IsLit);
            waitingMarker.gameObject.SetActive(!IsLit);

            if (relayBeam) relayBeam.gameObject.SetActive(IsLit);
            if (relayHint) relayHint.SetActive(!IsLit);
        }

        void Celebrate()
        {
            Sfx.DeskLit();
            sparkles.Play();
            StartCoroutine(Jump());
        }

        IEnumerator Jump()
        {
            const float duration = 0.4f;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float height = Mathf.Sin(time / duration * Mathf.PI);
                student.localScale = studentSize * (1f + 0.3f * height);
                yield return null;
            }
            student.localScale = studentSize;
        }
    }
}
