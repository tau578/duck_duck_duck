using System;
using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>
    /// "Here is your chest, player": a short full-screen animation before the elite chest's free picks.
    /// The chest drops in, shakes, throws its lid open over spinning rays and closes by itself.
    /// Runs on unscaled time (the game is paused) and builds its own UI, so the scene needs nothing.
    /// </summary>
    public class ChestReveal : MonoBehaviour
    {
        const float Drop = 0.35f;
        const float Shake = 0.55f;
        const float Length = 2.2f;

        static readonly Color Wood = new Color(0.55f, 0.32f, 0.14f);
        static readonly Color WoodDark = new Color(0.3f, 0.16f, 0.07f);
        static readonly Color Gold = new Color(1f, 0.8f, 0.2f);

        static Sprite raySprite;

        RectTransform chest, lid, rays;
        Image raysImage, glow, flash;
        RectTransform title, subtitle;
        RectTransform[] sparks;
        Action onDone;
        float time;
        bool opened;

        /// <summary>Play over the given UI parent; onDone fires once the animation ends.</summary>
        public static ChestReveal Show(Transform parent, Sprite rounded, int picks, Action onDone)
        {
            var root = Rect("ChestReveal", parent, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.SetAsLastSibling();
            // Blocks taps to the game underneath.
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.82f);
            var reveal = root.gameObject.AddComponent<ChestReveal>();
            reveal.onDone = onDone;
            reveal.Build(root, rounded, picks);
            Sfx.Play(SfxId.Coin, 1f, 0f, 0f);
            return reveal;
        }

        void Build(RectTransform root, Sprite rounded, int picks)
        {
            rays = Rect("Rays", root, Vector2.zero, new Vector2(1300f, 1300f));
            raysImage = Paint(rays, RaySprite(), new Color(1f, 0.85f, 0.3f, 0f));

            chest = Rect("Chest", root, Vector2.zero, new Vector2(10f, 10f));
            glow = Box("Glow", chest, new Vector2(0f, 70f), new Vector2(340f, 60f), new Color(1f, 0.95f, 0.6f, 0f), rounded);
            Box("Body", chest, new Vector2(0f, -60f), new Vector2(420f, 230f), Wood, rounded);
            Box("BodyShade", chest, new Vector2(0f, -140f), new Vector2(420f, 70f), WoodDark, rounded);
            Box("BandL", chest, new Vector2(-135f, -60f), new Vector2(44f, 240f), Gold, rounded);
            Box("BandR", chest, new Vector2(135f, -60f), new Vector2(44f, 240f), Gold, rounded);

            // The lid hinges on its upper edge, so opening it lifts it up and away.
            lid = Rect("Lid", chest, new Vector2(0f, 55f), new Vector2(450f, 140f));
            lid.pivot = new Vector2(0.5f, 0f);
            Box("Top", lid, Vector2.zero, new Vector2(450f, 140f), new Color(0.66f, 0.4f, 0.18f), rounded);
            Box("Rim", lid, new Vector2(0f, -54f), new Vector2(450f, 34f), Gold, rounded);
            Box("TopBandL", lid, new Vector2(-135f, 0f), new Vector2(44f, 146f), Gold, rounded);
            Box("TopBandR", lid, new Vector2(135f, 0f), new Vector2(44f, 146f), Gold, rounded);

            Box("Lock", chest, new Vector2(0f, 20f), new Vector2(84f, 96f), Gold, rounded);
            Box("Keyhole", chest, new Vector2(0f, 12f), new Vector2(18f, 38f), WoodDark, rounded);

            sparks = new RectTransform[16];
            for (int i = 0; i < sparks.Length; i++)
            {
                var s = Box("Spark", root, Vector2.zero, Vector2.one * (i % 2 == 0 ? 34f : 22f), Gold, null);
                s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                sparks[i] = s.rectTransform;
                s.gameObject.SetActive(false);
            }

            title = Words(root, Loc.T("chest.title"), new Vector2(0f, 480f), 96, new Color(1f, 0.85f, 0.2f));
            subtitle = Words(root, Loc.F("chest.sub", picks), new Vector2(0f, -400f), 54, Color.white);
            subtitle.localScale = Vector3.zero;

            flash = Box("Flash", root, Vector2.zero, new Vector2(4000f, 4000f), new Color(1f, 0.95f, 0.75f, 0f), null);
            Animate();
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            if (!opened && time >= Drop + Shake)
            {
                opened = true;
                foreach (var s in sparks) s.gameObject.SetActive(true);
                Sfx.Play(SfxId.Build, 1f, 0f, 0f);
                Sfx.Play(SfxId.Explosion, 0.5f, 0f, 0f);
            }
            Animate();
            if (time < Length) return;
            var done = onDone;
            onDone = null;
            Close();
            done?.Invoke();
        }

        /// <summary>Remove the overlay without waiting for the end.</summary>
        public void Close()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void Animate()
        {
            // Drop from above with a squash on landing.
            float fall = Mathf.Clamp01(time / Drop);
            float y = Mathf.Lerp(900f, 0f, fall * fall);
            float since = time - Drop;
            float squash = since > 0f && since < 0.25f ? Mathf.Sin(since / 0.25f * Mathf.PI) * 0.18f : 0f;
            chest.anchoredPosition = new Vector2(0f, y);
            chest.localScale = new Vector3(1f + squash, 1f - squash, 1f);

            // Rattle harder and harder until the lid gives way.
            float rattle = !opened && since > 0f ? Mathf.Clamp01(since / Shake) : 0f;
            chest.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 55f) * 9f * rattle);

            float open = Mathf.Clamp01((time - Drop - Shake) / 0.25f);
            lid.anchoredPosition = new Vector2(0f, 55f + 130f * Back(open));
            lid.localScale = new Vector3(1f, 1f - 0.35f * open, 1f);
            glow.color = new Color(1f, 0.95f, 0.6f, open);
            raysImage.color = new Color(1f, 0.85f, 0.3f, 0.55f * open);
            rays.localRotation = Quaternion.Euler(0f, 0f, time * 40f);
            rays.localScale = Vector3.one * (0.4f + 0.6f * open);
            flash.color = new Color(1f, 0.95f, 0.75f, opened ? Mathf.Clamp01(1f - (time - Drop - Shake) / 0.3f) * 0.8f : 0f);

            float burst = Mathf.Clamp01((time - Drop - Shake) / 0.9f);
            for (int i = 0; i < sparks.Length; i++)
            {
                float a = i / (float)sparks.Length * Mathf.PI * 2f + 0.2f;
                float reach = (i % 2 == 0 ? 520f : 360f) * (1f - (1f - burst) * (1f - burst));
                sparks[i].anchoredPosition = new Vector2(Mathf.Cos(a) * reach, 60f + Mathf.Sin(a) * reach - 220f * burst * burst);
                sparks[i].localScale = Vector3.one * (1f - burst * burst);
            }

            title.localScale = Vector3.one * Back(Mathf.Clamp01(time / 0.4f));
            subtitle.localScale = Vector3.one * Back(open);
        }

        /// <summary>Ease out with a small overshoot.</summary>
        static float Back(float k)
        {
            float m = k - 1f;
            return 1f + 2.7f * m * m * m + 1.7f * m * m;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static Image Paint(RectTransform rt, Sprite sprite, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Image Box(string name, Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite)
            => Paint(Rect(name, parent, pos, size), sprite, color);

        static RectTransform Words(Transform parent, string text, Vector2 pos, int fontSize, Color color)
        {
            var rt = Rect("Words", parent, pos, new Vector2(1000f, 200f));
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.text = text;
            t.raycastTarget = false;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.85f);
            o.effectDistance = new Vector2(4f, -4f);
            return rt;
        }

        /// <summary>A soft sunburst, generated once.</summary>
        static Sprite RaySprite()
        {
            if (raySprite != null) return raySprite;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ray = Mathf.Clamp01(Mathf.Sin(Mathf.Atan2(dy, dx) * 12f) * 2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, ray * Mathf.Clamp01(1f - r));
                }
            tex.SetPixels(pixels);
            tex.Apply();
            raySprite = Sprite.Create(tex, new UnityEngine.Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            return raySprite;
        }
    }
}
