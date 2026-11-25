using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    /// <summary>Blades orbiting the duck. Awakened (lv.3): golden, double damage.</summary>
    public class SpinningBlades : MonoBehaviour
    {
        public float spinSpeed = 220f;
        public float hitInterval = 0.3f;

        int level;
        float radius = 2.2f, damage = 12f, angle;
        readonly List<Transform> blades = new List<Transform>();
        readonly Dictionary<Enemy, float> lastHit = new Dictionary<Enemy, float>();
        Transform root;

        public void SetLevel(int lv)
        {
            level = lv;
            int count = lv <= 1 ? 2 : lv == 2 ? 3 : 4;
            radius = lv >= 2 ? 2.6f : 2.2f;
            damage = lv >= 3 ? 24f : 12f;
            Color c = lv >= 3 ? new Color(1f, 0.8f, 0.2f) : new Color(0.8f, 0.85f, 0.9f);

            if (root == null) root = new GameObject("Blades").transform;
            foreach (var b in blades) Destroy(b.gameObject);
            blades.Clear();
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(root, false);
                go.transform.localScale = new Vector3(0.9f, 0.08f, 0.25f) * (lv >= 3 ? 1.3f : 1f);
                go.GetComponent<MeshRenderer>().sharedMaterial = GameAssets.Lit(c);
                blades.Add(go.transform);
            }
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void Update()
        {
            if (level == 0 || root == null) return;
            bool visible = PlayerHealth.Instance != null && PlayerHealth.Instance.IsAlive;
            if (root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible);
            if (!visible) return;

            float dt = Time.deltaTime;
            angle += spinSpeed * dt;
            Vector3 center = transform.position;
            for (int i = 0; i < blades.Count; i++)
            {
                float a = (angle + i * 360f / blades.Count) * Mathf.Deg2Rad;
                blades[i].position = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius + Vector3.up * 0.7f;
                blades[i].rotation = Quaternion.Euler(0f, -angle * 3f, 0f);
            }

            var mgr = EnemyManager.Instance;
            if (mgr == null) return;
            float now = Time.time;
            var list = mgr.Alive;
            for (int e = list.Count - 1; e >= 0; e--)
            {
                if (e >= list.Count) continue;
                var enemy = list[e];
                if (lastHit.TryGetValue(enemy, out float t) && now - t < hitInterval) continue;
                Vector3 ep = enemy.transform.position;
                float r = enemy.Radius + 0.6f;
                for (int i = 0; i < blades.Count; i++)
                {
                    Vector3 d = ep - blades[i].position;
                    if (d.x * d.x + d.z * d.z > r * r) continue;
                    lastHit[enemy] = now;
                    enemy.TakeHit(damage, ep - center, false, 2.5f);
                    Sfx.Play(SfxId.Hit, 0.25f, 0.2f, 0.05f);
                    break;
                }
            }
        }
    }
}
