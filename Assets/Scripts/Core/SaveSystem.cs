using System;
using System.Collections.Generic;
using UnityEngine;

namespace StickmanWarriors
{
    [Serializable]
    public class SettingsData
    {
        public float master = 1f, music = 0.8f, sfx = 1f;
        public bool screenShake = true, fullscreen = true;
        public int quality = 2;
    }

    [Serializable] public class EquipEntry { public string key, value; }

    /// <summary>Persistent data only. Temporary combat state (HP, cooldowns, run upgrades) is never stored.</summary>
    [Serializable]
    public class SaveData
    {
        public int coins;
        public int highestLevelCompleted;
        public List<string> unlockedWeapons = new List<string>();
        public List<string> unlockedCosmetics = new List<string>();
        public List<string> achievements = new List<string>();
        public List<EquipEntry> equipped = new List<EquipEntry>();
        public int lifetimeKills, lifetimeCrits, lifetimeLifesteal;   // achievement progress
        public SettingsData settings = new SettingsData();
    }

    public static class SaveSystem
    {
        const string Key = "StickmanWarriors.Save.v1";
        static SaveData data;
        public static event Action Changed;

        public static SaveData Data { get { if (data == null) Load(); return data; } }

        public static void Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e) { Debug.LogWarning("Save load failed, starting fresh: " + e.Message); }
            if (data == null) data = new SaveData();
        }

        public static void Save()
        {
            if (data == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void AddCoins(int amount) { Data.coins = Mathf.Max(0, Data.coins + amount); Save(); }

        public static bool SpendCoins(int amount)
        {
            if (Data.coins < amount) return false;
            Data.coins -= amount; Save(); return true;
        }

        public static string GetEquip(string key)
        {
            foreach (var e in Data.equipped) if (e.key == key) return e.value;
            return null;
        }

        public static void SetEquip(string key, string value)
        {
            foreach (var e in Data.equipped) if (e.key == key) { e.value = value; Save(); return; }
            Data.equipped.Add(new EquipEntry { key = key, value = value });
            Save();
        }

        /// <summary>Debug/"Reset progress" button.</summary>
        public static void WipeEverything() { data = new SaveData(); PlayerPrefs.DeleteKey(Key); Save(); }
    }
}

