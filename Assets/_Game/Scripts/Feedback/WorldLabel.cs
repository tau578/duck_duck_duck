using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Billboard text in the world (slot prices, enemy counts at spawn points).</summary>
    public class WorldLabel : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;
        Text text;
        Canvas canvas;

        public static WorldLabel Create(Transform target, Vector3 offset, int fontSize, Color color)
        {
            var go = new GameObject("WorldLabel", typeof(RectTransform));
            var l = go.AddComponent<WorldLabel>();
            l.target = target;
            l.offset = offset;
            l.canvas = go.AddComponent<Canvas>();
            l.canvas.renderMode = RenderMode.WorldSpace;
            l.canvas.sortingOrder = 6;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(400f, 120f);
            rt.localScale = Vector3.one * 0.01f;

            var tgo = new GameObject("Text", typeof(RectTransform));
            var trt = (RectTransform)tgo.transform;
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            l.text = tgo.AddComponent<Text>();
            l.text.font = UiFont.Get();
            l.text.fontSize = fontSize;
            l.text.fontStyle = FontStyle.Bold;
            l.text.alignment = TextAnchor.MiddleCenter;
            l.text.color = color;
            l.text.horizontalOverflow = HorizontalWrapMode.Overflow;
            l.text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = tgo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(3f, -3f);
            return l;
        }

        public void Set(string value, Color? color = null)
        {
            text.text = value;
            if (color.HasValue) text.color = color.Value;
        }

        public void SetVisible(bool v)
        {
            if (canvas != null) canvas.enabled = v;
        }

        void LateUpdate()
        {
            if (target == null) { Destroy(gameObject); return; }
            transform.position = target.position + offset;
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }

    public static class UiFont
    {
        static Font font;
        public static Font Get()
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }
}
