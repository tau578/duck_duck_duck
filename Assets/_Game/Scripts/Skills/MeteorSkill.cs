using System.Collections;
using UnityEngine;

namespace Duck
{
    /// <summary>Every few seconds meteors fall on random enemies near the duck: warning ring, fall, explosion.</summary>
    public class MeteorSkill : MonoBehaviour
    {
        public float interval = 5f;
        public float damage = 45f;
        public float searchRadius = 12f;

        int level, count = 1;
        float radius = 2f, timer = 1f;

        public void SetLevel(int lv)
        {
            level = lv;
            count = lv <= 1 ? 1 : lv == 2 ? 2 : 3;
            radius = lv >= 3 ? 2.6f : 2f;
        }

        void Update()
        {
            if (level == 0 || EnemyManager.Instance == null) return;
            if (PlayerHealth.Instance != null && !PlayerHealth.Instance.IsAlive) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            var list = EnemyManager.Instance.Alive;
            if (list.Count == 0) return;
            timer = interval;
            for (int i = 0; i < count; i++)
            {
                var e = list[Random.Range(0, list.Count)];
                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > searchRadius) e = EnemyManager.Instance.Nearest(transform.position, searchRadius);
                if (e == null) continue;
                StartCoroutine(Drop(e.transform.position, i * 0.15f));
            }
        }

        IEnumerator Drop(Vector3 target, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            target.y = 0f;

            var ring = Prim(PrimitiveType.Cylinder, new Color(1f, 0.25f, 0.1f));
            ring.transform.position = target + Vector3.up * 0.04f;

            var rock = Prim(PrimitiveType.Sphere, new Color(1f, 0.5f, 0.15f));
            rock.transform.localScale = Vector3.one * 0.9f;
            Vector3 from = target + new Vector3(-4f, 14f, 4f);
            const float fall = 0.55f;
            for (float t = 0f; t < fall; t += Time.deltaTime)
            {
                float k = t / fall;
                rock.transform.position = Vector3.Lerp(from, target, k * k);
                float s = radius * 2f * (0.4f + 0.6f * k);
                ring.transform.localScale = new Vector3(s, 0.02f, s);
                yield return null;
            }
            Destroy(rock);
            Destroy(ring);

            var list = EnemyManager.Instance.Alive;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var e = list[i];
                Vector3 d = e.transform.position - target;
                d.y = 0f;
                if (d.magnitude > radius + e.Radius) continue;
                e.TakeHit(damage, d.sqrMagnitude > 0.01f ? d : Vector3.forward, false, 7f);
            }
            Sfx.Play(SfxId.Explosion, 0.6f, 0.15f, 0.05f);
            CameraFollow.Shake(0.2f);

            var blast = Prim(PrimitiveType.Sphere, new Color(1f, 0.7f, 0.2f));
            blast.transform.position = target;
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                float k = t / 0.25f;
                blast.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, radius * 2.2f, k) * (1f - k * 0.3f);
                yield return null;
            }
            Destroy(blast);
        }

        static GameObject Prim(PrimitiveType type, Color c)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = GameAssets.Lit(c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
