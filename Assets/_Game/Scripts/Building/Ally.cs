using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Infantry soldier from the Barracks. Holds a post in front of the wall, charges ground enemies nearby,
    /// blocks them in melee and returns to its post. With nothing to fight nearby it runs to defend the base.
    /// Enemies stop to fight it.
    /// </summary>
    public class Ally : MonoBehaviour
    {
        public static readonly List<Ally> All = new List<Ally>();

        public const float Radius = 0.35f;
        const float Speed = 3.4f;
        const float Aggro = 4.5f;
        const float Leash = 6f;
        /// <summary>An enemy this close to the base is a threat worth leaving the post for.</summary>
        const float BaseGuard = 4f;
        const float MeleeRange = 0.9f;
        const float AttackInterval = 0.8f;

        public bool IsAlive => hp > 0f;

        Barracks owner;
        Vector3 post;
        float hp, maxHp, damage;
        float attackTimer, retarget, walkPhase, lunge, flash;
        Enemy target;
        Transform visual;
        WorldBar bar;

        public static Ally Spawn(Barracks owner, Vector3 at, Vector3 post, float hp, float damage)
        {
            var go = new GameObject("Ally");
            go.transform.position = at;
            var a = go.AddComponent<Ally>();
            a.owner = owner;
            a.post = post;
            a.maxHp = a.hp = hp;
            a.damage = damage;
            a.BuildModel();
            a.bar = WorldBar.Create(go.transform, 1.5f, 0.8f, new Color(0.35f, 0.6f, 1f), true);
            All.Add(a);
            return a;
        }

        void BuildModel()
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            Part(PrimitiveType.Capsule, new Color(0.25f, 0.4f, 0.75f), new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
            Part(PrimitiveType.Sphere, new Color(0.95f, 0.8f, 0.6f), new Vector3(0f, 1.05f, 0f), Vector3.one * 0.35f);
            Part(PrimitiveType.Sphere, new Color(0.3f, 0.35f, 0.3f), new Vector3(0f, 1.15f, 0f), new Vector3(0.42f, 0.2f, 0.42f));
            Part(PrimitiveType.Cube, new Color(0.75f, 0.75f, 0.8f), new Vector3(0.3f, 0.6f, 0.25f), new Vector3(0.08f, 0.08f, 0.7f));
            Part(PrimitiveType.Cylinder, new Color(0.55f, 0.38f, 0.22f), new Vector3(-0.3f, 0.55f, 0.1f), new Vector3(0.45f, 0.04f, 0.45f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        GameObject Part(PrimitiveType type, Color c, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(visual, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = GameAssets.Lit(c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public void SetPost(Vector3 p) => post = p;

        public void HealFull()
        {
            hp = maxHp;
            bar.Set(1f);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;
            hp -= amount;
            flash = 1f;
            bar.Set(Mathf.Clamp01(hp / maxHp));
            if (hp <= 0f) Die();
        }

        void Die()
        {
            All.Remove(this);
            if (owner != null) owner.OnAllyDied(this);
            Sfx.Play(SfxId.Hurt, 0.4f, 0.15f, 0.05f);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (bar != null) Destroy(bar.gameObject);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            attackTimer -= dt;
            retarget -= dt;
            Vector3 pos = transform.position;

            if (retarget <= 0f || target == null || !target.IsAlive)
            {
                retarget = 0.25f;
                target = null;
                var em = EnemyManager.Instance;
                if (em != null)
                {
                    var e = em.Nearest(pos, Aggro, false);
                    if (e != null && (e.transform.position - post).sqrMagnitude < Leash * Leash) target = e;
                    else target = BaseAttacker(em, pos);
                }
            }

            bool moving = false;
            if (target != null)
            {
                Vector3 to = target.transform.position - pos;
                to.y = 0f;
                float reach = MeleeRange + target.Radius;
                if (to.magnitude > reach)
                {
                    pos += to.normalized * Speed * dt;
                    moving = true;
                }
                else if (attackTimer <= 0f)
                {
                    attackTimer = AttackInterval;
                    lunge = 1f;
                    target.TakeHit(damage, to, false, 1.2f);
                    Sfx.Play(SfxId.Hit, 0.25f, 0.2f, 0.05f);
                }
                Face(to, dt);
            }
            else
            {
                Vector3 to = post - pos;
                to.y = 0f;
                if (to.sqrMagnitude > 0.04f)
                {
                    pos += to.normalized * Mathf.Min(Speed * dt, to.magnitude);
                    moving = true;
                    Face(to, dt);
                }
            }

            transform.position = new Vector3(pos.x, 0f, pos.z);

            walkPhase += dt * (moving ? 12f : 0f);
            lunge = Mathf.MoveTowards(lunge, 0f, dt * 5f);
            flash = Mathf.MoveTowards(flash, 0f, dt * 6f);
            visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(walkPhase)) * 0.1f, lunge * 0.3f);
            visual.localScale = Vector3.one * (1f + flash * 0.15f);
        }

        /// <summary>Nearest ground enemy that is hitting the base or closing in on it, however far from the post.</summary>
        static Enemy BaseAttacker(EnemyManager em, Vector3 from)
        {
            var gm = GameManager.Instance;
            var home = gm != null && gm.baseSlot != null ? gm.baseSlot.Current : null;
            if (home == null || !home.IsAlive) return null;
            Enemy best = null;
            float bestSq = float.MaxValue;
            for (int i = 0; i < em.Alive.Count; i++)
            {
                var e = em.Alive[i];
                if (e.Flying) continue;
                Vector3 at = e.transform.position;
                if (e.TargetBuilding != home && home.DistanceTo(at) - e.Radius > BaseGuard) continue;
                float sq = (at - from).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = e; }
            }
            return best;
        }

        void Face(Vector3 dir, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 10f);
        }
    }
}
