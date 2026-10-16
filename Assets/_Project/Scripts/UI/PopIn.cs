using UnityEngine;

namespace PassTheLight.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PopIn : MonoBehaviour
    {
        [SerializeField] float duration = 0.35f;
        [SerializeField] float startScale = 0.7f;

        CanvasGroup group;
        float elapsed;

        void Awake() => group = GetComponent<CanvasGroup>();

        void OnEnable()
        {
            elapsed = 0f;
            ShowProgress(0f);
        }

        void Update()
        {
            if (elapsed >= duration) return;
            elapsed += Time.unscaledDeltaTime;
            ShowProgress(Mathf.Clamp01(elapsed / duration));
        }

        void ShowProgress(float progress)
        {
            float scale = Mathf.LerpUnclamped(startScale, 1f, Overshoot(progress));
            transform.localScale = new Vector3(scale, scale, 1f);
            group.alpha = Mathf.Clamp01(progress * 2f);
        }

        static float Overshoot(float progress)
        {
            const float bounce = 1.7f;
            float remaining = progress - 1f;
            return 1f + (bounce + 1f) * remaining * remaining * remaining + bounce * remaining * remaining;
        }
    }
}
