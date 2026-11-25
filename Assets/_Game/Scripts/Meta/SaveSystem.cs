using System;
using UnityEngine;

namespace Duck
{
    [Serializable]
    public class SaveData
    {
        public int unlockedChapter = 1;
        public int gears;
        public int[] upgrades = new int[0];
        public int[] bestWaves = new int[0];
    }

    /// <summary>Progress between battles, stored as JSON in PlayerPrefs.</summary>
    public static class SaveSystem
    {
        const string Key = "save_v1";
        static SaveData data;

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            data = new SaveData();
            Save();
        }

        public static int Upgrade(int index) => index < Data.upgrades.Length ? Data.upgrades[index] : 0;

        public static void SetUpgrade(int index, int level)
        {
            if (Data.upgrades.Length <= index) Array.Resize(ref Data.upgrades, index + 1);
            Data.upgrades[index] = level;
        }
    }
}
