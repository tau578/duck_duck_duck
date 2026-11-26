using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>
    /// Our own addition to the original: a super-bonus the player triggers. The meter charges from kills;
    /// at night the button calls a bomber that carpet-bombs a strip through the thickest part of the horde.
    /// Decision for the player: spend it now at the wall breach or save it for the boss.
    /// </summary>
    public class Airstrike : MonoBehaviour
    {
        public static Airstrike Instance { get; private set; }

        [Tooltip("Share of the current night's enemies to kill for one charge, so a stronger duck does not get more strikes.")]
        [Range(0.1f, 1f)] public float chargeShare = 0.35f;
        public int minKillsToCharge = 15;
        public int killsToCharge = 25;
        public int passes = 1;
        public int bombsPerPass = 9;
        public float stripLength = 14f;
        public float bombRadius = 2.3f;
        public float bombDamage = 90f;

        [Header("UI")]
        public Button button;
        public RectTransform fill;
        public Image buttonImage;
        public Text label;

        int charge;
        bool flying;

        public bool Ready => charge >= killsToCharge && !flying;
        public float Charge01 => Mathf.Clamp01(charge / (float)killsToCharge);

        void Awake() { Instance = this; }

        void Start()
        {
            EnemyManager.EnemyKilled += OnKill;
            passes = 1 + MetaUpgrades.BonusAirstrikePasses;
            if (button != null) button.onClick.AddListener(Trigger);
        }

        void OnDestroy() { EnemyManager.EnemyKilled -= OnKill; }

        void OnKill(Enemy e)
        {
            if (flying) return;
            bool wasReady = Ready;
            charge = Mathf.Min(killsToCharge, charge + 1);
            if (!wasReady && Ready)
            {
                Sfx.Play(SfxId.Build, 0.9f, 0f, 0f);
                DamageNumbers.ShowText(DuckController.Instance.transform.position + Vector3.up, Loc.T("air.ready"), new Color(0.5f, 0.9f, 1f), 50);
            }
        }

        /// <summary>Charge size follows the night's size: about two strikes per night at most.</summary>
        public void OnNightStart(int enemiesThisNight)
        {
            float was = Charge01;
            killsToCharge = Mathf.Max(minKillsToCharge, Mathf.RoundToInt(enemiesThisNight * chargeShare));
            charge = Mathf.RoundToInt(was * killsToCharge); // keep a carried-over charge as a share
        }

        public void Trigger()
        {
            var gm = GameManager.Instance;
            if (!Ready || gm == null || gm.Phase != GamePhase.Night || EnemyManager.Instance.AliveCount == 0)
            {
                Sfx.Play(SfxId.Denied, 0.7f, 0f, 0.1f);
                return;
            }
            charge = 0;
            StartCoroutine(Run());
        }

        Vector3 FindTarget()
        {
            // The enemy with the most neighbours = the thickest part of the horde.
            var list = EnemyManager.Instance.Alive;
            Vector3 best = list[0].transform.position;
            int bestCount = -1;
            for (int i = 0; i < list.Count; i += Mathf.Max(1, list.Count / 30))
            {
                Vector3 p = list[i].transform.position;
                int n = 0;
                for (int j = 0; j < list.Count; j++)
                    if ((list[j].transform.position - p).sqrMagnitude < 9f) n++;
                if (n > bestCount) { bestCount = n; best = p; }
            }
            best.y = 0f;
            return best;
        }

        IEnumerator Run()
        {
            flying = true;
            HUD.Instance?.Banner(Loc.T("air.banner"), new Color(0.5f, 0.9f, 1f), 1.5f);

            for (int pass = 0; pass < passes; pass++)
            {
                if (EnemyManager.Instance.AliveCount == 0) break;
                Vector3 target = FindTarget();
                Vector3 dir = Vector3.back; // flies along the road, north to south
                Vector3 start = target - dir * (stripLength * 0.5f + 14f) + Vector3.up * 9f;
                Vector3 end = target + dir * (stripLength * 0.5f + 14f) + Vector3.up * 9f;

                var plane = BuildPlane();
                plane.rotation = Quaternion.LookRotation(dir);
                const float speed = 22f;
                float total = Vector3.Distance(start, end) / speed;
                int dropped = 0;
                for (float t = 0f; t < total; t += Time.deltaTime)
                {
                    Vector3 p = Vector3.Lerp(start, end, t / total);
                    plane.position = p;
                    // Drop bombs evenly while over the strip.
                    float along = Vector3.Dot(p - target, dir) + stripLength * 0.5f;
                    int shouldHave = Mathf.Clamp(Mathf.FloorToInt(along / stripLength * bombsPerPass) + 1, 0, bombsPerPass);
                    if (along >= 0f && along <= stripLength) while (dropped < shouldHave)
                    {
                        dropped++;
                        Vector3 ground = new Vector3(p.x + Random.Range(-1f, 1f), 0f, p.z);
                        StartCoroutine(Bomb(p, ground));
                    }
                    yield return null;
                }
                Destroy(plane.gameObject);
            }
            flying = false;
        }

        IEnumerator Bomb(Vector3 from, Vector3 ground)
        {
            var bomb = Prim(PrimitiveType.Sphere, new Color(0.15f, 0.15f, 0.15f), null);
            bomb.transform.localScale = new Vector3(0.35f, 0.35f, 0.6f);
            const float fall = 0.45f;
            for (float t = 0f; t < fall; t += Time.deltaTime)
            {
                float k = t / fall;
                bomb.transform.position = Vector3.Lerp(from, ground, k * k);
                yield return null;
            }
            Destroy(bomb);

            var list = EnemyManager.Instance.Alive;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var e = list[i];
                Vector3 d = e.transform.position - ground;
                d.y = 0f;
                if (d.magnitude > bombRadius + e.Radius) continue;
                e.TakeHit(bombDamage, d.sqrMagnitude > 0.01f ? d : Vector3.forward, false, 8f);
            }
            Sfx.Play(SfxId.Explosion, 0.7f, 0.2f, 0.03f);
            CameraFollow.Shake(0.18f);

            var blast = Prim(PrimitiveType.Sphere, new Color(1f, 0.6f, 0.15f), null);
            blast.transform.position = ground;
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                float k = t / 0.3f;
                blast.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, bombRadius * 2.2f, k) * (1f - 0.4f * k);
                yield return null;
            }
            Destroy(blast);
        }

        Transform BuildPlane()
        {
            var root = new GameObject("Bomber").transform;
            Color body = new Color(0.35f, 0.42f, 0.3f);
            var f = Prim(PrimitiveType.Capsule, body, root);
            f.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            f.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);
            var w = Prim(PrimitiveType.Cube, body, root);
            w.transform.localScale = new Vector3(6f, 0.15f, 1.2f);
            var t = Prim(PrimitiveType.Cube, body, root);
            t.transform.localPosition = new Vector3(0f, 0.3f, -1.6f);
            t.transform.localScale = new Vector3(2.2f, 0.12f, 0.6f);
            var star = Prim(PrimitiveType.Cylinder, new Color(1f, 0.85f, 0.2f), root);
            star.transform.localPosition = new Vector3(1.8f, 0.09f, 0f);
            star.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);
            return root;
        }

        static GameObject Prim(PrimitiveType type, Color c, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            if (parent != null) go.transform.SetParent(parent, false);
            go.GetComponent<MeshRenderer>().sharedMaterial = GameAssets.Lit(c);
            return go;
        }

        void Update()
        {
            if (button == null) return;
            var gm = GameManager.Instance;
            bool show = gm != null && (gm.Phase == GamePhase.Night || gm.Phase == GamePhase.Day);
            if (button.gameObject.activeSelf != show) button.gameObject.SetActive(show);
            if (!show) return;
            fill.localScale = new Vector3(1f, Charge01, 1f);
            bool usable = Ready && gm.Phase == GamePhase.Night;
            float pulse = usable ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            buttonImage.color = usable ? Color.Lerp(new Color(0.2f, 0.5f, 0.9f), new Color(0.5f, 0.85f, 1f), pulse) : new Color(0.15f, 0.2f, 0.3f, 0.85f);
            button.transform.localScale = Vector3.one * (1f + 0.06f * pulse);
            label.text = Ready ? Loc.T("air.button") : $"{Mathf.RoundToInt(Charge01 * 100f)}%";
        }
    }
}
