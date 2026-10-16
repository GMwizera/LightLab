using UnityEngine;

namespace PassTheLight.Interaction
{
    [RequireComponent(typeof(LineRenderer))]
    public class SelectionRing : MonoBehaviour
    {
        [SerializeField] PointerInteractor interactor;
        [SerializeField] float radius = 0.95f;
        [SerializeField] int segments = 48;
        [SerializeField] float height = 0.13f;

        LineRenderer line;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }

        void LateUpdate()
        {
            Grabbable selected = interactor.Selected;
            line.enabled = selected;
            if (!selected) return;

            Vector3 piecePosition = selected.transform.position;
            transform.position = new Vector3(piecePosition.x, height, piecePosition.z);
            float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 6f);
            transform.localScale = Vector3.one * pulse;
        }
    }
}
