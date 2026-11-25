using UnityEngine;
using UnityEngine.EventSystems;

namespace Duck
{
    /// <summary>Floating stick: appears where the finger touches inside the area, returns a -1..1 vector.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform area;
        public RectTransform stickBase;
        public RectTransform handle;
        public float radius = 120f;
        [Range(0f, 0.5f)] public float deadZone = 0.1f;

        public Vector2 Value { get; private set; }

        Vector2 restPosition;

        void Awake()
        {
            if (area == null) area = (RectTransform)transform;
            restPosition = stickBase.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out Vector2 local))
                stickBase.anchoredPosition = local;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(stickBase, e.position, e.pressEventCamera, out Vector2 local))
                return;
            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            handle.anchoredPosition = clamped;
            Vector2 v = clamped / radius;
            Value = v.magnitude < deadZone ? Vector2.zero : v;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Value = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
            stickBase.anchoredPosition = restPosition;
        }

        void OnDisable()
        {
            Value = Vector2.zero;
        }
    }
}
