using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    /// <summary>A built structure on a slot: has HP, is attacked by enemies; towers shoot, base and mines pay at dawn.</summary>
    public class Building : MonoBehaviour
    {
        public static readonly List<Building> All = new List<Building>();

        public Transform visual;
        public Transform turretHead;
        public Transform muzzle;

        public BuildingConfig Config { get; private set; }
        public int Level { get; private set; }
        public BuildSlot Slot { get; private set; }
        public Health Health { get; private set; }
        public bool IsAlive => Health != null && Health.IsAlive;
        public int Income => Config.Level(Level).income + (Config.type == BuildingType.Base ? ChapterIncomeBonus : 0);
        static int ChapterIncomeBonus
        {
            get
            {
                var gm = GameManager.Instance;
                return gm != null && gm.chapter != null ? gm.chapter.baseIncomeBonus : 0;
            }
        }

        WorldBar bar;
        float fireTimer, retarget, pop, hitSquash;
        Enemy target;

        public void Init(BuildingConfig cfg, int level, BuildSlot slot, float barHeight)
        {
            Config = cfg;
            Level = level;
            Slot = slot;
            Health = gameObject.AddComponent<Health>();
            Health.ResetTo(cfg.Level(level).hp * SkillSystem.BuildingHpMultiplier);
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            bar = WorldBar.Create(transform, barHeight, cfg.type == BuildingType.Base ? 2.4f : 1.5f, new Color(0.3f, 0.85f, 0.3f), true);
            pop = 1f;
            All.Add(this);
            if (cfg.type == BuildingType.Barracks) gameObject.AddComponent<Barracks>();
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (bar != null) Destroy(bar.gameObject);
        }

        /// <summary>Distance on XZ from a point to this building's footprint rectangle.</summary>
        public float DistanceTo(Vector3 p)
        {
            Vector3 l = p - transform.position;
            float dx = Mathf.Max(Mathf.Abs(l.x) - Config.halfExtents.x, 0f);
            float dz = Mathf.Max(Mathf.Abs(l.z) - Config.halfExtents.y, 0f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>"Last stand": a battered base takes less damage, so a close call can still be turned around.</summary>
        const float LastStandBelow = 0.35f, LastStandDamage = 0.3f;

        public void TakeDamage(float amount)
        {
            if (Config.type == BuildingType.Base && Health.Normalized < LastStandBelow) amount *= LastStandDamage;
            Health.TakeDamage(amount);
        }

        void OnDamaged(float amount)
        {
            bar.Set(Health.Normalized);
            hitSquash = 1f;
            if (Config.type == BuildingType.Base && HUD.Instance != null) HUD.Instance.BaseUnderAttack();
        }

        void OnDied()
        {
            Sfx.Play(SfxId.Explosion, 0.8f, 0.1f, 0.1f);
            CameraFollow.Shake(0.35f);
            DamageNumbers.ShowText(transform.position, Loc.F("fx.destroyed", Config.Name), new Color(1f, 0.4f, 0.3f), 40);
            if (Slot != null) Slot.OnBuildingDestroyed(this);
        }

        public void RefreshBar() => bar.Set(Health.Normalized);

        /// <summary>Re-reads the Fortify multiplier: new max HP, fully repaired.</summary>
        public void ApplyHpMultiplier()
        {
            Health.ResetTo(Config.Level(Level).hp * SkillSystem.BuildingHpMultiplier);
            bar.Set(1f);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Pop-in when built / upgraded, squash when hit.
            pop = Mathf.MoveTowards(pop, 0f, dt * 3f);
            hitSquash = Mathf.MoveTowards(hitSquash, 0f, dt * 8f);
            if (visual != null)
            {
                float s = 1f + Mathf.Sin(pop * Mathf.PI) * 0.25f * pop;
                float sq = hitSquash * 0.08f;
                visual.localScale = new Vector3(s + sq, s - sq, s + sq);
            }

            if (Config.type == BuildingType.Tower && IsAlive) TowerUpdate(dt);
        }

        void TowerUpdate(float dt)
        {
            var lvl = Config.Level(Level);
            if (EnemyManager.Instance == null || lvl.fireRate <= 0f) return;

            retarget -= dt;
            if (retarget <= 0f || target == null || !target.IsAlive)
            {
                retarget = 0.15f;
                target = EnemyManager.Instance.Nearest(transform.position, lvl.range);
            }
            fireTimer -= dt;
            if (target == null || !target.IsAlive) return;

            Vector3 dir = target.transform.position - transform.position;
            dir.y = 0f;
            if (turretHead != null && dir.sqrMagnitude > 0.001f)
                turretHead.rotation = Quaternion.Slerp(turretHead.rotation, Quaternion.LookRotation(dir), dt * 12f);

            if (fireTimer > 0f) return;
            fireTimer = 1f / lvl.fireRate;
            Vector3 from = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.5f;
            Vector3 aim = target.transform.position + Vector3.up * 0.6f - from;
            aim.y = 0f;
            int shots = Mathf.Max(1, lvl.shots);
            for (int i = 0; i < shots; i++)
            {
                float angle = (i - (shots - 1) * 0.5f) * 7f;
                Bullet.Fire(from, Quaternion.Euler(0f, angle, 0f) * aim, lvl.damage, false, 1.2f, 24f, 0, 0.8f);
            }
            Sfx.Play(SfxId.TowerShot, 0.3f, 0.1f, 0.05f);
            hitSquash = Mathf.Max(hitSquash, 0.4f);
        }
    }
}
