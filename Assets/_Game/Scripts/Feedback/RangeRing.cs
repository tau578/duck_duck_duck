using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Flat circle on the ground that shows an attack radius. Solid or dashed, smoothly follows radius changes.
    /// Used for the duck's gun range and for tower ranges when choosing what to build.
    /// </summary>
    public class RangeRing : MonoBehaviour
    {
        const int Segments = 72;
        static Texture2D dashTexture;

        LineRenderer line;
        float shownRadius = -1f;
        float targetRadius;
        Color color;
        float alpha, targetAlpha;

        public static RangeRing Create(Transform parent, string name, Color color, bool dashed, float width = 0.12f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ring = go.AddComponent<RangeRing>();
            ring.Init(color, dashed, width);
            return ring;
        }

        void Init(Color c, bool dashed, float width)
        {
            color = GameSettings.Safe(c);
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.alignment = LineAlignment.TransformZ;
            line.widthMultiplier = width;
            line.positionCount = Segments;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            var baseMat = GameAssets.I != null ? GameAssets.I.lineMaterial : null;
            var mat = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Sprites/Default"));
            if (dashed)
            {
                mat.mainTexture = DashTexture();
                line.textureMode = LineTextureMode.Tile;
                mat.mainTextureScale = new Vector2(1.6f, 1f);
            }
            line.sharedMaterial = mat;
            line.enabled = false;
        }

        static Texture2D DashTexture()
        {
            if (dashTexture != null) return dashTexture;
            dashTexture = new Texture2D(8, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            for (int x = 0; x < 8; x++) dashTexture.SetPixel(x, 0, x < 5 ? Color.white : new Color(1f, 1f, 1f, 0f));
            dashTexture.Apply();
            return dashTexture;
        }

        /// <summary>Show at the given radius and opacity (0 hides with a fade).</summary>
        public void Show(float radius, float opacity)
        {
            targetRadius = radius;
            targetAlpha = radius > 0.01f ? opacity : 0f;
            if (shownRadius < 0f) shownRadius = radius;
        }

        public void Hide() => targetAlpha = 0f;

        public void HideNow()
        {
            targetAlpha = alpha = 0f;
            if (line != null) line.enabled = false;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            alpha = Mathf.MoveTowards(alpha, targetAlpha, dt * 3f);
            bool on = alpha > 0.005f;
            if (line.enabled != on) line.enabled = on;
            if (!on) return;

            if (Mathf.Abs(shownRadius - targetRadius) > 0.001f)
                shownRadius = Mathf.Lerp(shownRadius, targetRadius, 1f - Mathf.Exp(-dt * 8f));
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * shownRadius, Mathf.Sin(a) * shownRadius, 0f));
            }
            var c = color;
            c.a *= alpha;
            line.startColor = line.endColor = c;
        }
    }
}
