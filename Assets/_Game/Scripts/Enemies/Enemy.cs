using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Walks its road to the base, stops to attack the first building in reach (walls block ground units,
    /// flying units ignore walls), chases the duck when it is close. Juice: white flash, knockback, numbers.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        /// <summary>Global speed multiplier ("Rage moment" when a night drags on).</summary>
        public static float SpeedMultiplier = 1f;

        /// <summary>Set by Bullet around TakeHit: the hit comes from the duck's own gun.</summary>
        public static bool HitFromDuckGun;

        /// <summary>True if the killing blow was the duck's bullet (skills, towers and airstrike don't count).</summary>
        public bool KilledByDuckGun { get; private set; }

        public Transform visual;

        /// <summary>How far before a turn of the road it already counts as passed.</summary>
        const float TurnReach = 0.8f;

        /// <summary>Share of its walking speed an enemy keeps however hard it is being pushed back.</summary>
        const float MinAdvance = 0.4f;

        public EnemyConfig Config { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public float Damage { get; private set; }
        float moveSpeed;
        public bool IsAlive => Hp > 0f;
        public bool Flying => Config != null && Config.flying;
        public float Radius { get; private set; } = 0.4f;
        public float Mass { get; private set; } = 1f;
        /// <summary>The building this enemy is attacking right now, if any.</summary>
        public Building TargetBuilding => targetBuilding;
        /// <summary>Current walking speed.</summary>
        public float Speed => moveSpeed * SpeedMultiplier;
        /// <summary>Running total of enemy HP taken by all sources (only differences are meaningful).</summary>
        public static float DamageDealt { get; private set; }

        Vector3[] path;
        int waypoint;
        float lane;
        Vector3 knock;
        float attackTimer, retargetTimer, flashTimer, dieTimer, lunge, walkPhase;
        Building targetBuilding;
        Ally targetAlly;
        bool chasePlayer;
        Renderer[] renderers;
        Material[] baseMaterials;
        GameObject model;
        WorldBar bar;
        float baseY;

        public void Init(EnemyConfig cfg, EnemyPath road, float hpMultiplier, float damageMultiplier = 1f, float speedMultiplier = 1f)
        {
            Config = cfg;
            Damage = cfg.damage * damageMultiplier;
            moveSpeed = cfg.speed * speedMultiplier;
            MaxHp = cfg.maxHp * hpMultiplier;
            Hp = MaxHp;
            Radius = 0.4f * cfg.scale;
            Mass = cfg.scale * cfg.scale * (cfg.isBoss ? 4f : 1f);
            path = road.points;
            waypoint = 1;
            lane = Random.Range(-road.laneHalfWidth, road.laneHalfWidth);
            knock = Vector3.zero;
            attackTimer = Random.Range(0f, cfg.attackInterval);
            retargetTimer = 0f;
            flashTimer = 0f;
            dieTimer = 0f;
            targetBuilding = null;
            chasePlayer = false;
            baseY = cfg.flying ? 1.6f : 0f;

            SetupModel(cfg);
            visual.localScale = Vector3.one * cfg.scale;

            if (cfg.isBoss || cfg.isElite)
            {
                if (bar == null) bar = WorldBar.Create(transform, 1.9f * cfg.scale + 0.6f, cfg.isBoss ? 2.6f : 1.4f, new Color(0.9f, 0.2f, 0.2f), false);
                bar.gameObject.SetActive(true);
                bar.Set(1f);
            }
            else if (bar != null)
            {
                bar.gameObject.SetActive(false);
            }

            EnemyManager.Instance.Register(this);
        }

        void SetupModel(EnemyConfig cfg)
        {
            if (model != null) { Destroy(model); model = null; }
            Transform defaultBody = visual.Find("Default");
            if (cfg.modelOverride != null)
            {
                if (defaultBody != null) defaultBody.gameObject.SetActive(false);
                model = Instantiate(cfg.modelOverride, visual);
            }
            else if (defaultBody != null)
            {
                defaultBody.gameObject.SetActive(true);
                foreach (var r in defaultBody.GetComponentsInChildren<Renderer>(true))
                    if (r.name == "Body" || r.name == "Arm") r.sharedMaterial = GameAssets.Lit(cfg.bodyColor);
            }

            renderers = visual.GetComponentsInChildren<Renderer>(false);
            baseMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseMaterials[i] = renderers[i].sharedMaterial;
        }

        public void TakeHit(float damage, Vector3 direction, bool crit, float knockback)
        {
            if (!IsAlive) return;
            DamageDealt += Mathf.Min(damage, Hp);
            Hp -= damage;
            DamageNumbers.Damage(transform.position + Vector3.up * (baseY + Config.scale), damage, crit);
            Flash();
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                knock += direction.normalized * knockback * (1f - Config.knockResist);
            if (bar != null) bar.Set(Hp / MaxHp);
            if (Hp <= 0f)
            {
                KilledByDuckGun = HitFromDuckGun;
                Die();
            }
        }

        void Flash()
        {
            var flashMat = GameAssets.I != null ? GameAssets.I.flashMaterial : null;
            if (flashMat == null) return;
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].sharedMaterial = flashMat;
            flashTimer = 0.07f;
        }

        void RestoreMaterials()
        {
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].sharedMaterial = baseMaterials[i];
        }

        void Die()
        {
            Hp = 0f;
            EnemyManager.Instance.Unregister(this, true);
            dieTimer = 0.15f;
            if (bar != null) bar.gameObject.SetActive(false);
            Sfx.Play(SfxId.EnemyDie, 0.5f, 0.15f, 0.04f);

            // No regular coin drops: coins come from the base/mines at dawn and from kill combos (ComboTracker).
            if (Config.isBoss)
            {
                HitStop.Do(0.15f);
                CameraFollow.Shake(0.8f);
                Sfx.Play(SfxId.Explosion, 1f, 0.05f, 0f);
            }
            else if (Config.isElite)
            {
                HitStop.Do(0.07f);
                CameraFollow.Shake(0.4f);
            }
        }

        public void Despawn()
        {
            RestoreMaterials();
            if (bar != null) bar.gameObject.SetActive(false);
            Pool.Despawn(gameObject);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f) RestoreMaterials();
            }

            if (!IsAlive)
            {
                // Death pop: squash and shrink, then back to the pool.
                dieTimer -= dt;
                float k = Mathf.Clamp01(dieTimer / 0.15f);
                visual.localScale = new Vector3(1.3f - 0.3f * k, k, 1.3f - 0.3f * k) * Config.scale;
                if (dieTimer <= 0f) Despawn();
                return;
            }

            Vector3 pos = transform.position;
            Vector3 heading = Vector3.zero; // where it walks this frame; stays zero while it stands and attacks

            retargetTimer -= dt;
            if (retargetTimer <= 0f)
            {
                retargetTimer = 0.2f;
                FindTarget(pos);
            }

            attackTimer -= dt;
            bool busy = false;
            var duck = PlayerHealth.Instance;

            if (targetAlly != null && targetAlly.IsAlive && targetBuilding == null)
            {
                // Infantry blocks the way: stop and fight it.
                busy = true;
                Face(targetAlly.transform.position - pos, dt);
                if (attackTimer <= 0f)
                {
                    attackTimer = Config.attackInterval;
                    lunge = 1f;
                    targetAlly.TakeDamage(Damage);
                }
            }
            else if (targetBuilding != null && targetBuilding.IsAlive)
            {
                busy = true;
                Face(targetBuilding.transform.position - pos, dt);
                if (attackTimer <= 0f)
                {
                    attackTimer = Config.attackInterval;
                    lunge = 1f;
                    targetBuilding.TakeDamage(Damage);
                }
            }
            else if (chasePlayer && duck != null && duck.IsAlive)
            {
                Vector3 to = duck.transform.position - pos;
                to.y = 0f;
                if (to.magnitude <= Config.attackRange + 0.4f)
                {
                    busy = true;
                    Face(to, dt);
                    if (attackTimer <= 0f)
                    {
                        attackTimer = Config.attackInterval;
                        lunge = 1f;
                        duck.TakeDamage(Damage);
                    }
                }
                else
                {
                    busy = true;
                    heading = Step(ref pos, to, dt);
                }
            }

            if (!busy) heading = FollowPath(ref pos, dt);
            ApplyKnockback(ref pos, heading, dt);

            transform.position = new Vector3(pos.x, 0f, pos.z);
            Animate(dt, busy);
        }

        void FindTarget(Vector3 pos)
        {
            targetBuilding = null;
            targetAlly = null;
            if (!Flying)
            {
                float bestAlly = Config.attackRange + Ally.Radius + Radius;
                var allies = Ally.All;
                for (int i = 0; i < allies.Count; i++)
                {
                    Vector3 d = allies[i].transform.position - pos;
                    d.y = 0f;
                    float m = d.magnitude;
                    if (m < bestAlly) { bestAlly = m; targetAlly = allies[i]; }
                }
            }
            float best = Config.attackRange + 0.05f;
            var all = Building.All;
            for (int i = 0; i < all.Count; i++)
            {
                var b = all[i];
                if (!b.IsAlive) continue;
                if (Flying && b.Config.wallOnly) continue;
                float d = b.DistanceTo(pos) - Radius;
                if (d < best) { best = d; targetBuilding = b; }
            }

            var duck = PlayerHealth.Instance;
            chasePlayer = false;
            if (duck != null && duck.IsAlive)
            {
                Vector3 to = duck.transform.position - pos;
                to.y = 0f;
                float dd = to.magnitude;
                chasePlayer = dd < Config.aggroRange;
                // Prefer the duck if it is right here and closer than the building.
                if (targetBuilding != null && dd <= Config.attackRange + 0.4f && dd < best) targetBuilding = null;
                else if (targetBuilding != null) chasePlayer = false;
            }
        }

        /// <summary>
        /// Hits push the enemy, but never stop it: against its walking direction the push takes at most
        /// (1 - MinAdvance) of the speed, and an enemy that is attacking is not pushed off its target.
        /// </summary>
        void ApplyKnockback(ref Vector3 pos, Vector3 heading, float dt)
        {
            if (knock.sqrMagnitude <= 0.0001f) return;
            Vector3 push = knock;
            knock = Vector3.MoveTowards(knock, Vector3.zero, dt * 25f);

            bool walking = heading != Vector3.zero;
            Vector3 forward = walking ? heading : transform.forward;
            forward.y = 0f;
            forward.Normalize();
            float back = -Vector3.Dot(push, forward);
            float maxBack = walking ? moveSpeed * SpeedMultiplier * (1f - MinAdvance) : 0f;
            if (back > maxBack) push += forward * (back - maxBack);
            pos += push * dt;
        }

        Vector3 FollowPath(ref Vector3 pos, float dt)
        {
            if (path == null || path.Length == 0) return Vector3.zero;
            int last = path.Length - 1;
            Vector3 target = path[last];
            // A turn is passed once the enemy crosses the line across the road just before it. Requiring a touch of
            // the point itself jams a horde: neighbours push each other off it, and whoever overshot walks back.
            while (waypoint < last)
            {
                Vector3 dir = path[waypoint] - path[waypoint - 1];
                dir.y = 0f;
                dir.Normalize();
                Vector3 point = path[waypoint] + Vector3.Cross(Vector3.up, dir) * lane;
                if (Vector3.Dot(pos - point, dir) < -TurnReach)
                {
                    target = point;
                    break;
                }
                waypoint++;
            }
            Vector3 to = target - pos;
            to.y = 0f;
            return Step(ref pos, to, dt);
        }

        /// <summary>Moves towards <paramref name="to"/> and returns the walking direction.</summary>
        Vector3 Step(ref Vector3 pos, Vector3 to, float dt)
        {
            if (to.sqrMagnitude < 0.0001f) return Vector3.zero;
            Vector3 dir = to.normalized;
            pos += dir * (moveSpeed * SpeedMultiplier * dt);
            Face(dir, dt);
            return dir;
        }

        void Face(Vector3 dir, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 10f);
        }

        void Animate(float dt, bool attacking)
        {
            walkPhase += dt * (Config.flying ? 10f : 8f) * Config.speed;
            lunge = Mathf.MoveTowards(lunge, 0f, dt * 5f);
            float bob = Config.flying ? Mathf.Sin(walkPhase) * 0.25f : Mathf.Abs(Mathf.Sin(walkPhase)) * 0.12f;
            visual.localPosition = new Vector3(0f, baseY + bob, lunge * 0.4f);
            float wobble = Config.flying ? 0f : Mathf.Sin(walkPhase) * 6f;
            visual.localRotation = Quaternion.Euler(lunge * 20f, 0f, wobble);
            if (flashTimer <= 0f) visual.localScale = Vector3.one * Config.scale;
        }
    }
}
