using UnityEngine;

namespace Duck
{
    /// <summary>All per-enemy balance and look. Theme (zombies/robots/bugs) lives only here.</summary>
    [CreateAssetMenu(menuName = "Duck/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public string displayName = "Зомби";
        public string displayNameEn = "Zombie";
        public string Name => Loc.Pick(displayName, displayNameEn);
        public float maxHp = 30f;
        public float speed = 1.6f;
        public float damage = 8f;
        public float attackInterval = 1f;
        public float attackRange = 0.9f;
        [Tooltip("Enemy starts chasing the duck when it is this close.")]
        public float aggroRange = 2.5f;
        public bool flying;
        public bool isElite;
        public bool isBoss;
        public float scale = 1f;
        [Range(0f, 1f)] public float knockResist;
        public Color bodyColor = new Color(0.45f, 0.65f, 0.35f);
        [Range(0f, 1f)] public float coinChance = 0.25f;
        public int coinValue = 1;
        public int xp = 1;
        [Tooltip("Optional model (e.g. from Tripo). Empty = default primitive body.")]
        public GameObject modelOverride;
    }
}
