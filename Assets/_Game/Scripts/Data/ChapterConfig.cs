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
        public WaveConfig[] waves;
    }
}
