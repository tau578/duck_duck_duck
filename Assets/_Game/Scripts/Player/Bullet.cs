using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    /// <summary>Fast projectile with a tracer; checks hits against living enemies on the XZ plane.</summary>
    public class Bullet : MonoBehaviour
    {
        Vector3 dir;
        float speed, damage, knock, life;
        bool crit;
        int pierce;
        TrailRenderer trail;
        readonly List<Enemy> hit = new List<Enemy>(4);

        public static void Fire(Vector3 pos, Vector3 direction, float damage, bool crit, float knockback,
            float speed = 28f, int pierce = 0, float scale = 1f)
        {
            if (GameAssets.I == null || GameAssets.I.bulletPrefab == null) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            direction.Normalize();
            var go = Pool.Spawn(GameAssets.I.bulletPrefab, pos, Quaternion.LookRotation(direction));
            go.GetComponent<Bullet>().Setup(direction, damage, crit, knockback, speed, pierce, scale);
        }

        void Setup(Vector3 direction, float dmg, bool isCrit, float knockback, float spd, int pierceCount, float scale)
        {
            if (trail == null) trail = GetComponent<TrailRenderer>();
            if (trail != null) trail.Clear();
            dir = direction;
            damage = dmg;
            crit = isCrit;
            knock = knockback;
            speed = spd;
            pierce = pierceCount;
            life = 0.6f;
            hit.Clear();
            transform.localScale = Vector3.one * (crit ? 1.5f : 1f) * scale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 p = transform.position + dir * (speed * dt);
            transform.position = p;

            var mgr = EnemyManager.Instance;
            if (mgr != null)
            {
                var list = mgr.Alive;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (i >= list.Count) continue;
                    var e = list[i];
                    if (hit.Contains(e)) continue;
                    Vector3 d = e.transform.position - p;
                    float r = e.Radius + 0.3f;
                    if (d.x * d.x + d.z * d.z > r * r) continue;

                    hit.Add(e);
                    e.TakeHit(damage, dir, crit, knock);
                    Sfx.Play(SfxId.Hit, 0.3f, 0.2f, 0.03f);
                    if (crit) CameraFollow.Shake(0.06f);
                    if (pierce-- <= 0)
                    {
                        Pool.Despawn(gameObject);
                        return;
                    }
                }
            }

            life -= dt;
            if (life <= 0f) Pool.Despawn(gameObject);
        }
    }
}
