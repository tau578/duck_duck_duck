using System;
using UnityEngine;

namespace Duck
{
    public enum BuildingType { Base, Mine, Tower, Wall }

    [Serializable]
    public class BuildingLevel
    {
        public int cost = 1;
        public float hp = 100f;
        [Tooltip("Coins at dawn (base, mine).")]
        public int income;
        [Header("Tower")]
        public float damage;
        public float fireRate;
        public float range;
        public int shots = 1;
    }

    [CreateAssetMenu(menuName = "Duck/Building Config")]
    public class BuildingConfig : ScriptableObject
    {
        public BuildingType type;
        public string displayName = "Башня";
        [Tooltip("Half size of the footprint on XZ, used for enemy contact.")]
        public Vector2 halfExtents = new Vector2(0.8f, 0.8f);
        [Tooltip("Ground enemies stop at it, flying ignore it.")]
        public bool wallOnly;
        public BuildingLevel[] levels;

        public int MaxLevel => levels.Length;
        public BuildingLevel Level(int level) => levels[Mathf.Clamp(level - 1, 0, levels.Length - 1)];
    }
}
