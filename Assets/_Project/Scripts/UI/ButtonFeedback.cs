using PassTheLight.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PassTheLight.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] float pressedScale = 0.9f;
        [SerializeField] float hoverScale = 1.05f;
        [SerializeField] float speed = 18f;

        Button button;
        float targetScale = 1f;
        bool hovered;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Sfx.Click);
        }

        void OnDisable()
        {
            targetScale = 1f;
            hovered = false;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float smoothing = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
            float scale = Mathf.Lerp(transform.localScale.x, targetScale, smoothing);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button.interactable) targetScale = pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => targetScale = hovered ? hoverScale : 1f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (button.interactable) targetScale = hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            targetScale = 1f;
        }
    }
}
