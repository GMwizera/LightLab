using System.Collections.Generic;
using PassTheLight.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PassTheLight.Interaction
{
    public class PointerInteractor : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] float carryHeight = 0.9f;
        [SerializeField] float rotateStep = 15f;
        [SerializeField] float pickRadius = 0.6f;
        [SerializeField] Vector2 benchHalfSize = new(5.2f, 3.3f);

        LabControls controls;
        Grabbable heldPiece;
        Vector3 pickUpPosition;
        Vector3 grabOffset;
        bool rotateKeyWasDown;
        readonly List<RaycastResult> uiHits = new();

        public Grabbable Selected { get; private set; }
        public bool InteractionEnabled { get; set; }

        void Awake() => controls = new LabControls();
        void OnEnable() => controls.Lab.Enable();
        void OnDisable() => controls.Lab.Disable();
        void OnDestroy() => controls.Dispose();

        void Update()
        {
            if (controls.Lab.Pause.WasPressedThisFrame())
                GameManager.Instance.TogglePause();

            if (!InteractionEnabled)
            {
                if (heldPiece) DropHeldPiece();
                Select(null);
                return;
            }

            Vector2 pointer = controls.Lab.Point.ReadValue<Vector2>();

            if (controls.Lab.Select.WasPressedThisFrame() && !IsPointerOverButton(pointer))
                PickUpPieceUnder(pointer);

            if (heldPiece && controls.Lab.Select.IsPressed())
                MoveHeldPieceTo(pointer);
            else if (heldPiece)
                DropHeldPiece();

            HandleRotateKeys();
        }

        public void RotateSelected(float direction)
        {
            if (!InteractionEnabled || !Selected) return;
            Selected.RotateBy(direction * rotateStep);
            GameManager.Instance.RegisterMove();
        }

        void HandleRotateKeys()
        {
            float input = controls.Lab.Rotate.ReadValue<float>();
            bool keyIsDown = Mathf.Abs(input) > 0.5f;
            if (keyIsDown && !rotateKeyWasDown)
            {
                Sfx.Click();
                RotateSelected(Mathf.Sign(input));
            }
            rotateKeyWasDown = keyIsDown;
        }

        void PickUpPieceUnder(Vector2 pointer)
        {
            Ray ray = cam.ScreenPointToRay(pointer);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            Grabbable piece = hit.collider.GetComponentInParent<Grabbable>();
            if (!piece) piece = FindPieceNear(hit.point);

            Select(piece);
            if (!piece) return;

            heldPiece = piece;
            pickUpPosition = piece.transform.position;
            heldPiece.PickUp();

            if (TryGetPointAtCarryHeight(pointer, out Vector3 pointerOnTable))
                grabOffset = new Vector3(pickUpPosition.x - pointerOnTable.x, 0f, pickUpPosition.z - pointerOnTable.z);

            heldPiece.MoveTo(new Vector3(pickUpPosition.x, carryHeight, pickUpPosition.z));
        }

        Grabbable FindPieceNear(Vector3 point)
        {
            Grabbable closest = null;
            float closestDistance = float.MaxValue;

            foreach (Collider nearby in Physics.OverlapSphere(point, pickRadius))
            {
                Grabbable piece = nearby.GetComponentInParent<Grabbable>();
                if (!piece) continue;

                float distance = Vector3.Distance(nearby.ClosestPoint(point), point);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = piece;
                }
            }
            return closest;
        }

        void MoveHeldPieceTo(Vector2 pointer)
        {
            if (!TryGetPointAtCarryHeight(pointer, out Vector3 pointerOnTable)) return;

            Vector3 target = pointerOnTable + grabOffset;
            target.x = Mathf.Clamp(target.x, -benchHalfSize.x, benchHalfSize.x);
            target.z = Mathf.Clamp(target.z, -benchHalfSize.y, benchHalfSize.y);
            target.y = carryHeight;
            heldPiece.MoveTo(target);
        }

        void DropHeldPiece()
        {
            heldPiece.Drop();

            Vector3 moved = heldPiece.transform.position - pickUpPosition;
            moved.y = 0f;
            if (moved.magnitude > 0.05f) GameManager.Instance.RegisterMove();

            heldPiece = null;
        }

        void Select(Grabbable piece)
        {
            if (piece == Selected) return;
            if (Selected) Selected.SetSelected(false);
            Selected = piece;
            if (Selected) Selected.SetSelected(true);
        }

        bool TryGetPointAtCarryHeight(Vector2 pointer, out Vector3 point)
        {
            Plane carryPlane = new Plane(Vector3.up, new Vector3(0f, carryHeight, 0f));
            Ray ray = cam.ScreenPointToRay(pointer);

            bool hitPlane = carryPlane.Raycast(ray, out float distance);
            point = hitPlane ? ray.GetPoint(distance) : Vector3.zero;
            return hitPlane;
        }

        bool IsPointerOverButton(Vector2 pointer)
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = pointer };
            uiHits.Clear();
            EventSystem.current.RaycastAll(pointerData, uiHits);
            return uiHits.Count > 0;
        }
    }
}
