using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    /// <summary>Spawns the groups of a wave along their roads.</summary>
    public class WaveSpawner : MonoBehaviour
    {
        public List<EnemyPath> paths = new List<EnemyPath>();
        public float hpMultiplier = 1f;

        [Header("Auto-leveling (enemies catch up with the player, about halfway)")]
        [Tooltip("Enemy HP per duck level above the free ones, applied at the start of each night.")]
        public float hpPerDuckLevel = 0.03f;
        [Tooltip("Duck levels that are free: scaling starts above this level.")]
        public int freeDuckLevels = 3;
        [Tooltip("Enemy HP per Workshop level bought.")]
        public float hpPerWorkshopLevel = 0.03f;
        [Tooltip("Extra enemies per Workshop level bought (bosses are never multiplied).")]
        public float countPerWorkshopLevel = 0f;

        /// <summary>HP multiplier of the current night, chapter multiplier included.</summary>
        public float NightHpMultiplier { get; private set; } = 1f;
        /// <summary>Enemies the current night will spawn in total.</summary>
        public int NightEnemyCount { get; private set; }

        /// <summary>Seconds between the "BOSS!" warning and the boss appearing.</summary>
        const float BossWarningLead = 1.5f;

        int running;
        public bool IsSpawning => running > 0;

        /// <summary>Enemies of one group still to come.</summary>
        class Pending
        {
            public SpawnGroup group;
            public int left;
            public float nextAt;
        }

        readonly List<Pending> pending = new List<Pending>();

        public int PendingCount
        {
            get
            {
                int n = 0;
                foreach (var p in pending) n += p.left;
                return n;
            }
        }

        /// <summary>Calls back for every enemy yet to spawn: config, seconds until it appears, seconds it walks, HP, damage.</summary>
        public void ForEachPending(System.Action<EnemyConfig, float, float, float, float> visit)
        {
            var gm = GameManager.Instance;
            var ch = gm != null ? gm.chapter : null;
            foreach (var p in pending)
            {
                var cfg = p.group.enemy;
                var road = Road(p.group);
                float length = 0f;
                for (int i = 1; i < road.points.Length; i++) length += Vector3.Distance(road.points[i - 1], road.points[i]);
                float walk = length / Mathf.Max(0.1f, cfg.speed * (ch != null ? ch.enemySpeedMultiplier : 1f) * Enemy.SpeedMultiplier);
                float hp = cfg.maxHp * HpMultiplierFor(cfg);
                float damage = cfg.damage * (ch != null ? ch.enemyDamageMultiplier : 1f);
                float wait = Mathf.Max(0f, p.nextAt - Time.time);
                for (int i = 0; i < p.left; i++) visit(cfg, wait + i * p.group.interval, walk, hp, damage);
            }
        }

        EnemyPath Road(SpawnGroup g) => paths[Mathf.Clamp(g.pathIndex, 0, paths.Count - 1)];

        float HpMultiplierFor(EnemyConfig cfg)
        {
            var gm = GameManager.Instance;
            var ch = gm != null ? gm.chapter : null;
            return NightHpMultiplier * (ch != null && cfg.isBoss ? ch.bossHpMultiplier : 1f);
        }

        public void StartWave(WaveConfig wave)
        {
            var gm = GameManager.Instance;
            bool autoLevel = gm == null || gm.chapter == null || gm.chapter.autoLevel;
            int duckLevel = autoLevel && SkillSystem.Instance != null ? SkillSystem.Instance.Level : 1;
            int workshop = autoLevel ? MetaUpgrades.TotalLevels : 0;
            NightHpMultiplier = hpMultiplier * wave.hpMultiplier
                * (1f + hpPerDuckLevel * Mathf.Max(0, duckLevel - freeDuckLevels))
                * (1f + hpPerWorkshopLevel * workshop);
            float countScale = 1f + countPerWorkshopLevel * workshop;

            NightEnemyCount = 0;
            pending.Clear();
            foreach (var g in wave.groups)
            {
                int count = ScaledCount(g, countScale);
                NightEnemyCount += count;
                StartCoroutine(SpawnGroup(g, count));
            }
            Airstrike.Instance?.OnNightStart(NightEnemyCount, gm != null && gm.chapter != null ? gm.chapter.airstrikeKills : 0);
        }

        static int ScaledCount(SpawnGroup g, float scale)
        {
            if (g.enemy != null && g.enemy.isBoss) return g.count;
            return Mathf.Max(g.count, Mathf.RoundToInt(g.count * scale));
        }

        IEnumerator SpawnGroup(SpawnGroup g, int count)
        {
            running++;
            var coming = new Pending { group = g, left = count, nextAt = Time.time + g.delay };
            pending.Add(coming);
            bool boss = g.enemy != null && g.enemy.isBoss;
            float warning = boss ? Mathf.Min(g.delay, BossWarningLead) : 0f;
            if (g.delay > warning) yield return new WaitForSeconds(g.delay - warning);
            if (boss)
            {
                GameManager.Instance?.AnnounceBoss();
                if (warning > 0f) yield return new WaitForSeconds(warning);
            }
            var road = Road(g);
            for (int i = 0; i < count; i++)
            {
                Vector3 jitter = new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-0.8f, 0.8f));
                var go = Pool.Spawn(GameAssets.I.enemyPrefab, road.Spawn + jitter, Quaternion.identity);
                var gm = GameManager.Instance;
                var ch = gm != null ? gm.chapter : null;
                go.GetComponent<Enemy>().Init(g.enemy, road,
                    HpMultiplierFor(g.enemy),
                    ch != null ? ch.enemyDamageMultiplier : 1f, ch != null ? ch.enemySpeedMultiplier : 1f);
                coming.left--;
                coming.nextAt = Time.time + g.interval;
                if (g.interval > 0f) yield return new WaitForSeconds(g.interval);
            }
            running--;
        }

        public void StopAll()
        {
            StopAllCoroutines();
            running = 0;
            pending.Clear();
        }
    }
}
