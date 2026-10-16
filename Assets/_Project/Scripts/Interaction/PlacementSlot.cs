using UnityEngine;

namespace PassTheLight.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class PlacementSlot : MonoBehaviour
    {
        [SerializeField] Renderer pad;
        [SerializeField] Color idleColor = new(0.2f, 0.55f, 0.6f);
        [SerializeField] Color activeColor = new(0.45f, 0.95f, 1f);

        bool highlighted;
        Vector3 padScale;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            padScale = pad.transform.localScale;
            SetHighlighted(false);
        }

        public void SetHighlighted(bool on)
        {
            highlighted = on;
            pad.material.color = on ? activeColor : idleColor;
            pad.transform.localScale = padScale;
        }

        void Update()
        {
            if (!highlighted) return;
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 10f);
            pad.transform.localScale = new Vector3(padScale.x * pulse, padScale.y, padScale.z * pulse);
        }
    }
}
