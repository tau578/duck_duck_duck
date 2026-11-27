using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    /// <summary>
    /// A helping hand on the late nights. When only the last quarter of the wave is left, the game forecasts
    /// whether the base survives at the player's current pace; if not, a chest with two bonuses drops
    /// (the first one is always the duck's health). At most one chest per night.
    /// </summary>
    public class RescueChest : MonoBehaviour
    {
        /// <summary>First night (1-based) that can get the chest.</summary>
        const int FirstNight = 4;
        const float LastShare = 0.25f;
        /// <summary>Losing: the forecast damage takes more than this share of what still protects the base.</summary>
        const float DangerShare = 0.8f;
        /// <summary>A base this battered with enemies left counts as losing whatever the forecast says.</summary>
        const float CriticalBase = 0.25f;
        /// <summary>No more enemies than this can hit the base at once.</summary>
        const int MaxAttackers = 12;
        /// <summary>Seconds of damage dealt that measure the player's current firepower.</summary>
        const int Window = 8;

        struct Threat
        {
            public float arrival, hp, dps;
        }

        /// <summary>Enemy.DamageDealt sampled once a second.</summary>
        readonly Queue<float> samples = new Queue<float>();
        readonly List<Threat> threats = new List<Threat>(256);
        readonly List<float> attackers = new List<float>(256);
        float tick;
        int givenNight = -1;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Phase != GamePhase.Night)
            {
                samples.Clear();
                if (gm == null || gm.Phase != GamePhase.Day) givenNight = -1;
                return;
            }
            tick -= Time.deltaTime;
            if (tick > 0f) return;
            tick = 1f;
            samples.Enqueue(Enemy.DamageDealt);
            while (samples.Count > Window + 1) samples.Dequeue();

            int night = gm.WaveIndex + 1;
            if (night < FirstNight || givenNight == night || SkillSystem.Instance == null) return;
            int left = gm.spawner.PendingCount + EnemyManager.Instance.AliveCount;
            if (left == 0 || left > gm.spawner.NightEnemyCount * LastShare) return;
            if (!Losing(gm)) return;

            if (SkillSystem.Instance.GrantRescueChest()) givenNight = night;
        }

        bool Losing(GameManager gm)
        {
            var home = gm.baseSlot != null ? gm.baseSlot.Current : null;
            if (home == null || !home.IsAlive) return false;
            if (home.Health.Normalized < CriticalBase) return true;

            float dps = Firepower();
            if (dps <= 0f) return true;

            threats.Clear();
            var alive = EnemyManager.Instance.Alive;
            for (int i = 0; i < alive.Count; i++)
            {
                var e = alive[i];
                threats.Add(new Threat
                {
                    arrival = home.DistanceTo(e.transform.position) / Mathf.Max(0.1f, e.Speed),
                    hp = e.Hp,
                    dps = e.Damage / Mathf.Max(0.1f, e.Config.attackInterval),
                });
            }
            gm.spawner.ForEachPending((cfg, wait, walk, hp, damage) => threats.Add(new Threat
            {
                arrival = wait + walk,
                hp = hp,
                dps = damage / Mathf.Max(0.1f, cfg.attackInterval),
            }));
            // The nearest die first: each enemy hits the base from its arrival until its turn to die.
            threats.Sort((a, b) => a.arrival.CompareTo(b.arrival));
            float killed = 0f, damageTaken = 0f;
            attackers.Clear();
            foreach (var t in threats)
            {
                killed += t.hp;
                damageTaken += Mathf.Max(0f, killed / dps - t.arrival) * t.dps;
                attackers.Add(t.dps);
            }
            attackers.Sort((a, b) => b.CompareTo(a));
            float crowd = 0f;
            for (int i = 0; i < Mathf.Min(MaxAttackers, attackers.Count); i++) crowd += attackers[i];
            damageTaken = Mathf.Min(damageTaken, crowd * Mathf.Max(0f, killed / dps - threats[0].arrival));

            float shield = home.EffectiveHp;
            foreach (var b in Building.All) if (b.IsAlive && b.Config.wallOnly) shield += b.Health.Current * 0.5f;
            return damageTaken > shield * DangerShare;
        }

        /// <summary>Damage per second over the last few seconds, but never below most of the duck's own gun.</summary>
        float Firepower()
        {
            float measured = samples.Count > 1 ? (Enemy.DamageDealt - samples.Peek()) / (samples.Count - 1) : 0f;
            var duck = DuckController.Instance;
            var gun = duck != null ? duck.GetComponent<AutoShooter>() : null;
            return gun != null ? Mathf.Max(measured, gun.Dps * 0.7f) : measured;
        }
    }
}
