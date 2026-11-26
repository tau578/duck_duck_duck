using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Red screen-edge flash on damage and a pulsing vignette when HP is low.</summary>
    [RequireComponent(typeof(Image))]
    public class DamageVignette : MonoBehaviour
    {
        static DamageVignette instance;

        [Range(0f, 1f)] public float lowHpThreshold = 0.3f;
        Image image;
        float flash;
        float lowHp; // 0..1 how low
        float heartTimer;

        void Awake()
        {
            instance = this;
            image = GetComponent<Image>();
            image.sprite = MakeSprite();
            image.raycastTarget = false;
            image.color = new Color(0.9f, 0f, 0f, 0f);
        }

        public static void Flash(float strength = 0.6f)
        {
            if (instance != null) instance.flash = Mathf.Max(instance.flash, strength * GameSettings.FlashScale);
        }

        public static void SetHealth(float normalized)
        {
            if (instance == null) return;
            instance.lowHp = normalized <= 0f || normalized >= instance.lowHpThreshold
                ? 0f
                : 1f - normalized / instance.lowHpThreshold;
        }

        void Update()
        {
            flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.5f);
            float pulse = 0f;
            if (lowHp > 0f)
            {
                pulse = (0.25f + 0.25f * lowHp) * (0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 7f));
                heartTimer -= Time.unscaledDeltaTime;
                if (heartTimer <= 0f)
                {
                    heartTimer = Mathf.Lerp(1.1f, 0.6f, lowHp);
                    Sfx.Play(SfxId.Heartbeat, 0.9f, 0f, 0.3f);
                }
            }
            var c = image.color;
            c.a = Mathf.Max(flash, pulse);
            image.color = c;
        }

        static Sprite MakeSprite()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.6f + Mathf.Sqrt(dx * dx + dy * dy) * 0.4f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.05f, d));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
