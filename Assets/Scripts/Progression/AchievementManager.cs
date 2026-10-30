using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StickmanWarriors
{
    [Serializable]
    public class AchievementDef { public string id, displayName, description; public Sprite icon; }

    /// <summary>Put one in each scene (optionally with a toast UI). Counters live in SaveData; unlocks persist.</summary>
    public class AchievementManager : MonoBehaviour
    {
        static AchievementManager instance;
        public static event Action<AchievementDef> Unlocked;
        [SerializeField] List<AchievementDef> achievements = new List<AchievementDef>();
        [SerializeField] CanvasGroup toastGroup;
        [SerializeField] TMP_Text toastTitle, toastDescription;

        public static IReadOnlyList<AchievementDef> All { get { Ensure(); return instance.achievements; } }
        public static bool IsUnlocked(string id) { return SaveSystem.Data.achievements.Contains(id); }
        public static AchievementDef Find(string id) { foreach (var a in All) if (a.id == id) return a; return null; }
        static void Ensure() { if (instance == null) new GameObject("AchievementManager").AddComponent<AchievementManager>(); }

        static AchievementDef A(string id, string n, string d) { return new AchievementDef { id = id, displayName = n, description = d }; }
        void Reset() { achievements = Defaults(); }
        static List<AchievementDef> Defaults()
        {
            return new List<AchievementDef>
            {
                A("first_blood", "First Blood", "Defeat your first enemy."),
                A("slayer", "Slayer", "Defeat 50 enemies."),
                A("critical_master", "Critical Master", "Land 20 critical hits."),
                A("untouchable", "Untouchable", "Complete a level without taking damage."),
                A("survivor", "Survivor", "Finish a level with less than 10% HP."),
                A("boss_slayer", "Boss Slayer", "Defeat the final boss."),
                A("scythe_master", "Scythe Master", "Restore 1,000 HP using lifesteal."),
            };
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            if (achievements.Count == 0) achievements = Defaults();
            if (toastGroup) toastGroup.alpha = 0f;
        }
        void OnEnable() { CombatEvents.Damaged += OnDamaged; CombatEvents.Died += OnDied; CombatEvents.Healed += OnHealed; }
        void OnDisable() { CombatEvents.Damaged -= OnDamaged; CombatEvents.Died -= OnDied; CombatEvents.Healed -= OnHealed; }

        void OnDamaged(IDamageable t, DamageInfo i) { if (i.isCrit && SaveSystem.Data.lifetimeCrits >= 20) Unlock("critical_master"); }
        void OnHealed(IDamageable t, int amount, bool lifesteal) { if (lifesteal && SaveSystem.Data.lifetimeLifesteal >= 1000) Unlock("scythe_master"); }
        void OnDied(IDamageable t, GameObject killer)
        {
            if (t.IsPlayer) return;
            int k = SaveSystem.Data.lifetimeKills;
            if (k >= 1) Unlock("first_blood");
            if (k >= 50) Unlock("slayer");
            if (t.IsBoss) Unlock("boss_slayer");
        }

        public static void NotifyLevelComplete(LevelStats s)
        {
            Ensure();
            if (s.damageTaken <= 0f) instance.Unlock("untouchable");
            if (s.hpRemaining > 0f && s.HpPercent < 0.10f) instance.Unlock("survivor");
        }

        void Unlock(string id)
        {
            if (IsUnlocked(id)) return;
            SaveSystem.Data.achievements.Add(id);
            SaveSystem.Save();
            var def = Find(id);
            AudioManager.Play(Sfx.Unlock);
            if (Unlocked != null) Unlocked(def);
            if (def != null && toastGroup) StartCoroutine(Toast(def));
        }

        IEnumerator Toast(AchievementDef d)
        {
            toastTitle.text = "Achievement Unlocked: " + d.displayName; toastDescription.text = d.description;
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime) { toastGroup.alpha = t / 0.25f; yield return null; }
            toastGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(2.5f);
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime) { toastGroup.alpha = 1f - t / 0.5f; yield return null; }
            toastGroup.alpha = 0f;
        }
    }
}

