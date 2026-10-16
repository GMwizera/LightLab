using System.Collections;
using UnityEngine;

namespace PassTheLight.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraBump : MonoBehaviour
    {
        [SerializeField] float zoomAmount = 4f;
        [SerializeField] float duration = 0.5f;

        Camera cam;
        float baseFov;

        void Awake()
        {
            cam = GetComponent<Camera>();
            baseFov = cam.fieldOfView;
        }

        public void Bump()
        {
            StopAllCoroutines();
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                float zoom = Mathf.Sin(time / duration * Mathf.PI);
                cam.fieldOfView = baseFov - zoomAmount * zoom;
                yield return null;
            }
            cam.fieldOfView = baseFov;
        }
    }
}
