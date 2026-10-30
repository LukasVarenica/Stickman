using System;
using System.Collections.Generic;
using UnityEngine;

namespace StickmanWarriors
{
    public enum CosmeticType { BodyColor, EyeGlow, WeaponColor, ParticleTrail, DashEffect, WeaponSkin }

    [Serializable]
    public class CosmeticData
    {
        public string id, displayName;
        public CosmeticType type;
        public Sprite icon;
        public Color color = new Color(0, 0, 0, 0);   // alpha 0 = "no override" (use the character's original look)
        public GameObject effectPrefab;               // ParticleTrail / DashEffect
        public Sprite sprite;                         // WeaponSkin
        public string weaponIdFilter;                 // WeaponSkin only: "sword", "scythe"...
        public bool defaultUnlocked;
        public int coinCost;                          // 0 = not purchasable
        public string achievementId;                  // unlocks automatically when this achievement is earned
    }

    /// <summary>Purely visual. Nothing here touches stats.</summary>
    public class CosmeticManager : MonoBehaviour
    {
        public static CosmeticManager Instance { get { Ensure(); return instance; } }
        static CosmeticManager instance;
        public static event Action Changed;
        [SerializeField] List<CosmeticData> cosmetics = new List<CosmeticData>();
        public IReadOnlyList<CosmeticData> All { get { return cosmetics; } }

        static void Ensure() { if (instance == null) new GameObject("CosmeticManager").AddComponent<CosmeticManager>(); }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
            if (cosmetics.Count == 0) cosmetics = Defaults();
        }
        void Reset() { cosmetics = Defaults(); }

        static CosmeticData C(string id, string n, CosmeticType t, Color col, int cost = 0, bool def = false, string ach = null, string weapon = null)
        {
            return new CosmeticData { id = id, displayName = n, type = t, color = col, coinCost = cost, defaultUnlocked = def, achievementId = ach, weaponIdFilter = weapon };
        }

        // Colour cosmetics work out of the box; assign effectPrefab/sprite in the inspector for trails, dash FX and weapon skins.
        static List<CosmeticData> Defaults()
        {
            var none = new Color(0, 0, 0, 0);
            return new List<CosmeticData>
            {
                C("body_default", "Classic Black", CosmeticType.BodyColor, none, 0, true),
                C("body_red", "Crimson", CosmeticType.BodyColor, new Color(0.85f, 0.15f, 0.15f), 100),
                C("body_blue", "Azure", CosmeticType.BodyColor, new Color(0.2f, 0.45f, 0.95f), 100),
                C("body_gold", "Golden Champion", CosmeticType.BodyColor, new Color(1f, 0.82f, 0.2f), 0, false, "boss_slayer"),
                C("eyes_none", "No Glow", CosmeticType.EyeGlow, none, 0, true),
                C("eyes_cyan", "Cyan Eyes", CosmeticType.EyeGlow, new Color(0.3f, 1f, 1f), 150),
                C("eyes_red", "Red Eyes", CosmeticType.EyeGlow, new Color(1f, 0.2f, 0.2f), 0, false, "slayer"),
                C("wcolor_default", "Default Weapon", CosmeticType.WeaponColor, none, 0, true),
                C("wcolor_crimson", "Crimson Weapon", CosmeticType.WeaponColor, new Color(0.9f, 0.1f, 0.2f), 120),
                C("wcolor_emerald", "Emerald Weapon", CosmeticType.WeaponColor, new Color(0.2f, 0.9f, 0.5f), 120),
                C("wcolor_aurora", "Aurora Weapon", CosmeticType.WeaponColor, new Color(0.6f, 0.4f, 1f), 0, false, "critical_master"),
                C("trail_none", "No Trail", CosmeticType.ParticleTrail, none, 0, true),
                C("trail_embers", "Ember Trail", CosmeticType.ParticleTrail, none, 200),
                C("trail_frost", "Frost Trail", CosmeticType.ParticleTrail, none, 200),
                C("dash_default", "Standard Dash", CosmeticType.DashEffect, none, 0, true),
                C("dash_shock", "Shockwave Dash", CosmeticType.DashEffect, none, 200),
                C("skin_sword_gold", "Golden Sword", CosmeticType.WeaponSkin, none, 300, false, null, "sword"),
                C("skin_scythe_bone", "Bone Scythe", CosmeticType.WeaponSkin, none, 300, false, null, "scythe"),
            };
        }

        public CosmeticData Get(string id) { foreach (var c in cosmetics) if (c.id == id) return c; return null; }
        public IEnumerable<CosmeticData> OfType(CosmeticType t) { foreach (var c in cosmetics) if (c.type == t) yield return c; }

        public bool IsUnlocked(CosmeticData c)
        {
            if (c.defaultUnlocked || SaveSystem.Data.unlockedCosmetics.Contains(c.id)) return true;
            return !string.IsNullOrEmpty(c.achievementId) && AchievementManager.IsUnlocked(c.achievementId);
        }

        public string RequirementText(CosmeticData c)
        {
            string s = c.coinCost > 0 ? c.coinCost + " coins" : "";
            if (!string.IsNullOrEmpty(c.achievementId))
            {
                var a = AchievementManager.Find(c.achievementId);
                s += (s.Length > 0 ? " or " : "") + "Achievement: " + (a != null ? a.displayName : c.achievementId);
            }
            return s.Length > 0 ? s : "Unlocked";
        }

        public bool TryUnlock(CosmeticData c)
        {
            if (IsUnlocked(c)) return true;
            if (c.coinCost <= 0 || !SaveSystem.SpendCoins(c.coinCost)) return false;
            SaveSystem.Data.unlockedCosmetics.Add(c.id);
            SaveSystem.Save(); AudioManager.Play(Sfx.Unlock);
            if (Changed != null) Changed();
            return true;
        }

        public bool Equip(CosmeticData c)
        {
            if (!IsUnlocked(c)) return false;
            SaveSystem.SetEquip("cosmetic:" + c.type, c.id);
            if (Changed != null) Changed();
            return true;
        }

        public CosmeticData GetEquipped(CosmeticType t)
        {
            var c = Get(SaveSystem.GetEquip("cosmetic:" + t));
            if (c != null && c.type == t && IsUnlocked(c)) return c;
            foreach (var x in cosmetics) if (x.type == t && x.defaultUnlocked) return x;
            return null;
        }
    }
}
