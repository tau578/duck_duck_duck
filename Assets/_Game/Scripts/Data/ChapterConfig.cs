using System;
using UnityEngine;

namespace Duck
{
    public enum WaveKind { Normal, Elite, Boss }

    [Serializable]
    public class SpawnGroup
    {
        public EnemyConfig enemy;
        public int count = 10;
        [Tooltip("Index of the EnemyPath in the scene (0 = north).")]
        public int pathIndex;
        public float interval = 0.6f;
        public float delay;
    }

    [Serializable]
    public class WaveConfig
    {
        public WaveKind kind;
        public SpawnGroup[] groups;

        public int CountOnPath(int path)
        {
            int n = 0;
            foreach (var g in groups) if (g.pathIndex == path) n += g.count;
            return n;
        }
    }

    [CreateAssetMenu(menuName = "Duck/Chapter Config")]
    public class ChapterConfig : ScriptableObject
    {
        public string displayName = "Глава 1. Пустошь";
        public string displayNameEn = "Chapter 1. Wasteland";
        public string Name => Loc.Pick(displayName, displayNameEn);
        public int startCoins = 7;
        [Tooltip("Enemy HP multiplier for this chapter (x1.6-1.8 per chapter in the original).")]
        public float enemyHpMultiplier = 1f;
        [Tooltip("Gears for a victory; a defeat gives a share by waves survived.")]
        public int rewardGears = 60;
        [Tooltip("Highest level any building may reach in this chapter (keeps the early base modest).")]
        public int maxBuildingLevel = 99;
        [TextArea] public string newsRu;
        [TextArea] public string newsEn;
        public string News => Loc.Pick(newsRu, newsEn);
        public WaveConfig[] waves;
    }
}
