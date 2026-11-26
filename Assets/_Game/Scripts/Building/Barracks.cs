using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Barracks: keeps a squad of infantry (3 at level 1, 6 at level 2) at a post in front of the wall.
    /// Lost soldiers come back one by one; at dawn the squad is refilled and healed.
    /// </summary>
    public class Barracks : MonoBehaviour
    {
        /// <summary>Seconds to replace one lost soldier.</summary>
        public float respawnDelay = 8f;

        Building building;
        readonly List<Ally> squad = new List<Ally>();
        float respawnTimer;

        BuildingLevel Lvl => building.Config.Level(building.Level);
        Vector3 Rally => building.Slot != null ? building.Slot.rallyPoint : transform.position + Vector3.forward * 3f;

        void Start()
        {
            building = GetComponent<Building>();
            for (int i = 0; i < Lvl.allies; i++) SpawnOne();
        }

        void SpawnOne()
        {
            Vector3 door = transform.position + (Rally - transform.position).normalized * 1.2f;
            squad.Add(Ally.Spawn(this, door, Rally, Lvl.allyHp, Lvl.allyDamage));
            UpdatePosts();
        }

        void UpdatePosts()
        {
            // Small line across the road, centered on the rally point.
            int n = squad.Count;
            for (int i = 0; i < n; i++)
            {
                float x = (i - (n - 1) * 0.5f) * 0.9f;
                float z = (i % 2) * 0.6f;
                squad[i].SetPost(Rally + new Vector3(x, 0f, z));
            }
        }

        public void OnAllyDied(Ally a)
        {
            squad.Remove(a);
            UpdatePosts();
            if (respawnTimer <= 0f) respawnTimer = respawnDelay;
        }

        public void OnDawn()
        {
            foreach (var a in squad) a.HealFull();
            while (squad.Count < Lvl.allies) SpawnOne();
        }

        void Update()
        {
            if (!building.IsAlive || squad.Count >= Lvl.allies) return;
            if (respawnTimer <= 0f) respawnTimer = respawnDelay;
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f) SpawnOne();
        }

        void OnDestroy()
        {
            // Upgrade or destruction: the new building brings its own squad.
            foreach (var a in squad) if (a != null) Destroy(a.gameObject);
            squad.Clear();
        }
    }
}
