using PassTheLight.Core;
using UnityEngine;

namespace PassTheLight.Interaction
{
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : MonoBehaviour
    {
        [SerializeField] Renderer body;
        [SerializeField] Color selectedTint = new(1f, 0.8f, 0.3f);
        [SerializeField] float dropSoundSpeed = 0.8f;

        Rigidbody rb;
        PlacementSlot slotUnderneath;
        Color normalColor;
        Vector3 normalSize;
        Vector3 startPosition;
        Quaternion startRotation;
        float angle;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            normalColor = body.material.color;
            normalSize = body.transform.localScale;
            startPosition = transform.position;
            startRotation = transform.rotation;
            angle = transform.eulerAngles.y;
        }

        public void PickUp()
        {
            rb.isKinematic = true;
            body.transform.localScale = normalSize * 1.12f;
            Sfx.PickUp();
        }

        public void MoveTo(Vector3 position) => transform.position = position;

        public void Drop()
        {
            if (slotUnderneath)
            {
                Vector3 slotCentre = slotUnderneath.transform.position;
                transform.position = new Vector3(slotCentre.x, transform.position.y, slotCentre.z);
            }
            body.transform.localScale = normalSize;
            rb.isKinematic = false;
            rb.WakeUp();
        }

        public void RotateBy(float degrees)
        {
            float steps = Mathf.Round((angle + degrees) / degrees);
            angle = Mathf.Repeat(steps * degrees, 360f);

            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            transform.rotation = rotation;
            rb.rotation = rotation;
        }

        public void SetSelected(bool selected)
        {
            body.material.color = selected ? Color.Lerp(normalColor, selectedTint, 0.6f) : normalColor;
        }

        public void ResetPose()
        {
            rb.isKinematic = true;
            transform.SetPositionAndRotation(startPosition, startRotation);
            angle = startRotation.eulerAngles.y;
            rb.isKinematic = false;

            SetSelected(false);
            if (slotUnderneath) slotUnderneath.SetHighlighted(false);
            slotUnderneath = null;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out PlacementSlot slot)) return;
            if (slotUnderneath) slotUnderneath.SetHighlighted(false);
            slotUnderneath = slot;
            slot.SetHighlighted(true);
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out PlacementSlot slot) || slot != slotUnderneath) return;
            slot.SetHighlighted(false);
            slotUnderneath = null;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > dropSoundSpeed) Sfx.Drop();
        }
    }
}
