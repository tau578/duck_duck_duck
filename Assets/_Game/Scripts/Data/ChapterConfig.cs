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
        [Tooltip("Enemy HP multiplier of this night: later nights are tougher, so the duck does not outgrow them.")]
        public float hpMultiplier = 1f;
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
        [Tooltip("Lower level limit for gold mines; 0 = same as other buildings.")]
        public int maxMineLevel = 0;

        [Header("Systems per chapter (chapter 1 keeps the block11 tuning the player liked)")]
        [Tooltip("XP needed per duck level is multiplied by this.")]
        public float xpScale = 1f;
        [Tooltip("Coins from duck-gun combos per night (0 = none).")]
        public int comboCoinsPerNight = 0;
        [Tooltip("Enemy HP / count follow the duck level and the Workshop.")]
        public bool autoLevel = true;
        [Tooltip("Kills for one airstrike charge; 0 = a share of the night's enemies.")]
        public int airstrikeKills = 0;
        [Tooltip("Extra coins the base pays at dawn in this chapter.")]
        public int baseIncomeBonus = 0;
        [Tooltip("Enemy damage multiplier for this chapter.")]
        public float enemyDamageMultiplier = 1f;
        [Tooltip("Enemy walking speed multiplier for this chapter.")]
        public float enemySpeedMultiplier = 1f;
        [Tooltip("Extra HP multiplier for the boss only.")]
        public float bossHpMultiplier = 1f;
        [Tooltip("Highest duck level in a battle; 0 = the SkillSystem default.")]
        public int maxDuckLevel = 0;
        [Tooltip("XP for each duck level (the last value repeats); empty = the default curve.")]
        public int[] xpPerLevel = new int[0];
        [Tooltip("Free skill picks for the first elite killed in a battle (the original's elite chest).")]
        public int eliteChestPicks = 0;
        [Tooltip("Price of building a spot (level 1) in this chapter, by BuildingType; 0 or missing = the building's own price.")]
        public int[] buildCosts = new int[0];
        [Tooltip("The Workshop start coins apply in this chapter (off where the economy is counted to the coin).")]
        public bool workshopCoins = true;
        [TextArea] public string newsRu;
        [TextArea] public string newsEn;
        public string News => Loc.Pick(newsRu, newsEn);
        public WaveConfig[] waves;
    }
}
