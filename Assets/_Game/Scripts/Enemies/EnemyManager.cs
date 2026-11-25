using System;
using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    /// <summary>Registry of living enemies, target queries and cheap crowd separation.</summary>
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }
        public static event Action<Enemy> EnemyKilled;

        public readonly List<Enemy> Alive = new List<Enemy>(256);
        public int AliveCount => Alive.Count;

        void Awake() { Instance = this; }

        public void Register(Enemy e) => Alive.Add(e);

        public void Unregister(Enemy e, bool killed)
        {
            Alive.Remove(e);
            if (killed) EnemyKilled?.Invoke(e);
        }

        public Enemy Nearest(Vector3 from, float range, bool includeFlying = true)
        {
            Enemy best = null;
            float bestSq = range * range;
            for (int i = 0; i < Alive.Count; i++)
            {
                var e = Alive[i];
                if (!includeFlying && e.Flying) continue;
                Vector3 d = e.transform.position - from;
                d.y = 0f;
                float sq = d.sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = e; }
            }
            return best;
        }

        void LateUpdate()
        {
            // O(n^2) push-apart; fine for ~150 enemies.
            int n = Alive.Count;
            for (int i = 0; i < n; i++)
            {
                var a = Alive[i];
                Vector3 pa = a.transform.position;
                float ra = a.Radius;
                for (int j = i + 1; j < n; j++)
                {
                    var b = Alive[j];
                    if (a.Flying != b.Flying) continue;
                    Vector3 pb = b.transform.position;
                    float dx = pa.x - pb.x, dz = pa.z - pb.z;
                    float min = ra + b.Radius;
                    float sq = dx * dx + dz * dz;
                    if (sq >= min * min || sq < 1e-6f) continue;
                    float dist = Mathf.Sqrt(sq);
                    float push = (min - dist) * 0.5f / dist;
                    float wa = b.Mass / (a.Mass + b.Mass);
                    float wb = 1f - wa;
                    pa.x += dx * push * wa; pa.z += dz * push * wa;
                    pb.x -= dx * push * wb; pb.z -= dz * push * wb;
                    b.transform.position = pb;
                }
                a.transform.position = pa;
            }
        }

        public void ClearAll()
        {
            for (int i = Alive.Count - 1; i >= 0; i--) Alive[i].Despawn();
            Alive.Clear();
        }
    }
}
