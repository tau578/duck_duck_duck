using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Pooled pop-up numbers on the HUD canvas (damage, crits, "+7" income).</summary>
    public class DamageNumbers : MonoBehaviour
    {
        static DamageNumbers instance;

        public int poolSize = 50;
        public float life = 0.65f;

        struct Entry
        {
            public RectTransform rt;
            public Text text;
            public Vector3 world;
            public float age;
            public float life;
            public float scale;
            public bool active;
        }

        Entry[] entries;
        int next;
        Camera cam;

        void Awake()
        {
            instance = this;
            Suppressed = false;
            entries = new Entry[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("Num", typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                rt.sizeDelta = new Vector2(300f, 80f);
                var t = go.AddComponent<Text>();
                t.font = UiFont.Get();
                t.alignment = TextAnchor.MiddleCenter;
                t.fontStyle = FontStyle.Bold;
                t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                var o = go.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0f, 0.85f);
                o.effectDistance = new Vector2(3f, -3f);
                go.SetActive(false);
                entries[i] = new Entry { rt = rt, text = t };
            }
        }

        /// <summary>True on the result screen: no new pop-ups over the result panel.</summary>
        public static bool Suppressed;

        /// <summary>Hide every pop-up at once (game over).</summary>
        public static void Clear()
        {
            if (instance == null) return;
            for (int i = 0; i < instance.entries.Length; i++)
            {
                instance.entries[i].active = false;
                instance.entries[i].rt.gameObject.SetActive(false);
            }
        }

        public static void Damage(Vector3 world, float amount, bool crit)
        {
            if (instance == null || Suppressed) return;
            string s = Mathf.CeilToInt(amount).ToString();
            if (crit) instance.Spawn(world, s + "!", new Color(1f, 0.85f, 0.1f), 58, 1.5f);
            else instance.Spawn(world, s, Color.white, 40, 1f);
        }

        public static void ShowText(Vector3 world, string s, Color color, int size = 48)
        {
            if (instance == null || Suppressed) return;
            instance.Spawn(world, s, color, size, 1.4f);
        }

        void Spawn(Vector3 world, string s, Color color, int size, float lifeMul)
        {
            ref Entry e = ref entries[next];
            next = (next + 1) % entries.Length;
            e.world = world + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.2f, 0.2f));
            e.age = 0f;
            e.life = life * lifeMul;
            e.scale = size >= 50 ? 1.6f : 1.3f;
            e.active = true;
            e.text.text = s;
            e.text.fontSize = size;
            e.text.color = color;
            e.rt.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < entries.Length; i++)
            {
                ref Entry e = ref entries[i];
                if (!e.active) continue;
                e.age += dt;
                float n = e.age / e.life;
                if (n >= 1f)
                {
                    e.active = false;
                    e.rt.gameObject.SetActive(false);
                    continue;
                }
                Vector3 sp = cam.WorldToScreenPoint(e.world + Vector3.up * (1.5f + n * 1.2f));
                if (sp.z < 0f) { e.rt.gameObject.SetActive(false); continue; }
                e.rt.position = sp;
                float pop = n < 0.15f ? Mathf.Lerp(e.scale, 1f, n / 0.15f) : 1f;
                e.rt.localScale = Vector3.one * pop;
                var c = e.text.color;
                c.a = n > 0.6f ? 1f - (n - 0.6f) / 0.4f : 1f;
                e.text.color = c;
            }
        }
    }
}
