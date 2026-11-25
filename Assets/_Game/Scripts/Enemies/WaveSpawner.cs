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

        int running;
        public bool IsSpawning => running > 0;

        public void StartWave(WaveConfig wave)
        {
            foreach (var g in wave.groups) StartCoroutine(SpawnGroup(g));
        }

        IEnumerator SpawnGroup(SpawnGroup g)
        {
            running++;
            if (g.delay > 0f) yield return new WaitForSeconds(g.delay);
            var road = paths[Mathf.Clamp(g.pathIndex, 0, paths.Count - 1)];
            for (int i = 0; i < g.count; i++)
            {
                Vector3 jitter = new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-0.8f, 0.8f));
                var go = Pool.Spawn(GameAssets.I.enemyPrefab, road.Spawn + jitter, Quaternion.identity);
                go.GetComponent<Enemy>().Init(g.enemy, road, hpMultiplier);
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
