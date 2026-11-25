using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Billboard health bar above a unit or building (world-space canvas, built in code).</summary>
    public class WorldBar : MonoBehaviour
    {
        public Transform target;
        public float height = 2f;
        public bool hideWhenFull = true;

        RectTransform fill;
        Image fillImage;
        Canvas canvas;
        Color fullColor, lowColor;
        float value = 1f;
        float flash;

        public static WorldBar Create(Transform target, float height, float width, Color color, bool hideWhenFull)
        {
            var go = new GameObject("WorldBar", typeof(RectTransform));
            var bar = go.AddComponent<WorldBar>();
            bar.target = target;
            bar.height = height;
            bar.hideWhenFull = hideWhenFull;
            bar.Build(width, color);
            return bar;
        }

        void Build(float width, Color color)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(width * 100f, 18f);
            rt.localScale = Vector3.one * 0.01f;

            var bg = Child("BG", rt);
            bg.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            fill = Child("Fill", rt);
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);
            fill.pivot = new Vector2(0f, 0.5f);
            fillImage = fill.gameObject.AddComponent<Image>();
            fullColor = color;
            lowColor = new Color(0.95f, 0.2f, 0.15f);
            fillImage.color = color;
        }

        static RectTransform Child(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public void Set(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (normalized < value) flash = 1f;
            value = normalized;
        }

        void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                canvas.enabled = false;
                if (target == null) Destroy(gameObject);
                return;
            }

            bool visible = !(hideWhenFull && value >= 0.999f) && value > 0f;
            canvas.enabled = visible;
            if (!visible) return;

            transform.position = target.position + Vector3.up * height;
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;

            fill.localScale = new Vector3(value, 1f, 1f);
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 6f);
            Color baseColor = value < 0.3f ? lowColor : fullColor;
            fillImage.color = Color.Lerp(baseColor, Color.white, flash);
        }
    }
}
