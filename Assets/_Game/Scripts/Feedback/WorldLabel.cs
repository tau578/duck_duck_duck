using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Billboard text in the world (slot prices, enemy counts at spawn points).</summary>
    public class WorldLabel : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;
        /// <summary>Slide the text sideways so it is not cut off while its target is at the screen edge.</summary>
        public bool keepOnScreen;
        Text text;
        Canvas canvas;
        bool visible = true;

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
            l.text.fontSize = Mathf.RoundToInt(fontSize * GameSettings.TextScale);
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
            visible = v;
            if (canvas != null) canvas.enabled = v && target != null && target.gameObject.activeInHierarchy;
        }

        void LateUpdate()
        {
            if (target == null) { Destroy(gameObject); return; }
            bool on = visible && target.gameObject.activeInHierarchy;
            if (canvas.enabled != on) canvas.enabled = on;
            if (!on) return;
            transform.position = target.position + offset;
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = cam.transform.rotation;
            if (keepOnScreen) PullInside(cam);
        }

        void PullInside(Camera cam)
        {
            const float margin = 0.01f; // share of the screen width
            Vector3 at = cam.WorldToViewportPoint(target.position);
            // A target that is fully off screen takes its label with it.
            if (at.z <= 0f || at.x < -margin || at.x > 1f + margin) return;
            float halfWorld = text.preferredWidth * 0.5f * transform.localScale.x;
            Vector3 centre = cam.WorldToViewportPoint(transform.position);
            float half = cam.WorldToViewportPoint(transform.position + cam.transform.right * halfWorld).x - centre.x;
            if (half <= 0f || half >= 0.5f - margin) return;
            float x = Mathf.Clamp(centre.x, margin + half, 1f - margin - half);
            transform.position += cam.transform.right * ((x - centre.x) / half * halfWorld);
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
