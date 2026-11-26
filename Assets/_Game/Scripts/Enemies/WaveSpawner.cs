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
        public float hpPerWorkshopLevel = 0.06f;
        [Tooltip("Extra enemies per Workshop level bought (bosses are never multiplied).")]
        public float countPerWorkshopLevel = 0.04f;

        /// <summary>HP multiplier of the current night, chapter multiplier included.</summary>
        public float NightHpMultiplier { get; private set; } = 1f;
        /// <summary>Enemies the current night will spawn in total.</summary>
        public int NightEnemyCount { get; private set; }

        int running;
        public bool IsSpawning => running > 0;

        public void StartWave(WaveConfig wave)
        {
            var gm = GameManager.Instance;
            bool autoLevel = gm == null || gm.chapter == null || gm.chapter.autoLevel;
            int duckLevel = autoLevel && SkillSystem.Instance != null ? SkillSystem.Instance.Level : 1;
            int workshop = autoLevel ? MetaUpgrades.TotalLevels : 0;
            NightHpMultiplier = hpMultiplier
                * (1f + hpPerDuckLevel * Mathf.Max(0, duckLevel - freeDuckLevels))
                * (1f + hpPerWorkshopLevel * workshop);
            float countScale = 1f + countPerWorkshopLevel * workshop;

            NightEnemyCount = 0;
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
            if (g.delay > 0f) yield return new WaitForSeconds(g.delay);
            var road = paths[Mathf.Clamp(g.pathIndex, 0, paths.Count - 1)];
            for (int i = 0; i < count; i++)
            {
                Vector3 jitter = new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-0.8f, 0.8f));
                var go = Pool.Spawn(GameAssets.I.enemyPrefab, road.Spawn + jitter, Quaternion.identity);
                var gm = GameManager.Instance;
                var ch = gm != null ? gm.chapter : null;
                go.GetComponent<Enemy>().Init(g.enemy, road,
                    NightHpMultiplier * (ch != null && g.enemy.isBoss ? ch.bossHpMultiplier : 1f),
                    ch != null ? ch.enemyDamageMultiplier : 1f, ch != null ? ch.enemySpeedMultiplier : 1f);
                if (g.interval > 0f) yield return new WaitForSeconds(g.interval);
            }
            running--;
        }

        public void StopAll()
        {
            StopAllCoroutines();
            running = 0;
        }
    }
}
