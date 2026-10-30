using System;
using System.Collections.Generic;
using UnityEngine;

namespace StickmanWarriors
{
    public enum UnlockType { Default, Coins, Level }

    [Serializable]
    public class WeaponData
    {
        public string id, displayName, description;
        public Sprite icon, weaponSprite;
        public CharacterClass[] allowedClasses;
        public float baseDamage = 20f, attackSpeed = 1f, range = 1.5f;   // attackSpeed = attacks per second
        public int animatorWeaponType;                                    // Animator int "WeaponType"
        public AnimatorOverrideController animatorOverride;              // optional unique attack animations
        public Color trailColor = Color.white;
        public ParticleSystem hitEffectPrefab;                           // optional weapon-specific hit FX
        public UnlockType unlock = UnlockType.Default;
        public int coinCost, requiredLevel;
        public bool UsableBy(CharacterClass c) { return Array.IndexOf(allowedClasses, c) >= 0; }
    }

    public class WeaponManager : MonoBehaviour
    {
        public static WeaponManager Instance { get { Ensure(); return instance; } }
        static WeaponManager instance;
        public static event Action Changed;
        [SerializeField] List<WeaponData> weapons = new List<WeaponData>();
        public IReadOnlyList<WeaponData> Weapons { get { return weapons; } }

        static void Ensure() { if (instance == null) new GameObject("WeaponManager").AddComponent<WeaponManager>(); }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
            if (weapons.Count == 0) weapons = Defaults();
        }
        void Reset() { weapons = Defaults(); }

        // Tanky: Sword (default), Heavy Sword (coins), Bow (Level 2). Mage: Magic Staff. Scythe user: Scythe.
        // Edit class compatibility in the inspector if the original game differs.
        static List<WeaponData> Defaults()
        {
            return new List<WeaponData>
            {
                new WeaponData { id = "sword", displayName = "Sword", description = "Balanced blade. Fast and reliable.", allowedClasses = new[] { CharacterClass.Tanky },
                    baseDamage = 20, attackSpeed = 2f, range = 1.6f, animatorWeaponType = 0, trailColor = Color.white, unlock = UnlockType.Default },
                new WeaponData { id = "heavy_sword", displayName = "Heavy Sword", description = "Slow, crushing swings.", allowedClasses = new[] { CharacterClass.Tanky },
                    baseDamage = 42, attackSpeed = 0.9f, range = 2.0f, animatorWeaponType = 1, trailColor = new Color(1f, 0.5f, 0.15f), unlock = UnlockType.Coins, coinCost = 250 },
                new WeaponData { id = "magic_staff", displayName = "Magic Staff", description = "Channels arcane energy.", allowedClasses = new[] { CharacterClass.Mage },
                    baseDamage = 18, attackSpeed = 1.4f, range = 8f, animatorWeaponType = 2, trailColor = new Color(0.3f, 0.8f, 1f), unlock = UnlockType.Default },
                new WeaponData { id = "scythe", displayName = "Scythe", description = "Reaping blade with wide arcs.", allowedClasses = new[] { CharacterClass.Scythe },
                    baseDamage = 26, attackSpeed = 1.2f, range = 2.4f, animatorWeaponType = 3, trailColor = new Color(0.7f, 0.3f, 1f), unlock = UnlockType.Default },
                new WeaponData { id = "bow", displayName = "Bow", description = "Long-range arrows.", allowedClasses = new[] { CharacterClass.Tanky },
                    baseDamage = 16, attackSpeed = 1.6f, range = 10f, animatorWeaponType = 4, trailColor = new Color(0.6f, 1f, 0.5f), unlock = UnlockType.Level, requiredLevel = 2 },
            };
        }

        public WeaponData Get(string id) { foreach (var w in weapons) if (w.id == id) return w; return null; }

        public bool IsUnlocked(WeaponData w)
        {
            if (w.unlock == UnlockType.Default) return true;
            var save = SaveSystem.Data;
            if (save.unlockedWeapons.Contains(w.id)) return true;
            return w.unlock == UnlockType.Level && save.highestLevelCompleted >= w.requiredLevel;
        }

        public string RequirementText(WeaponData w)
        {
            switch (w.unlock)
            {
                case UnlockType.Coins: return w.coinCost + " coins";
                case UnlockType.Level: return "Complete Level " + w.requiredLevel;
                default: return "Unlocked";
            }
        }

        public bool TryUnlock(WeaponData w)
        {
            if (IsUnlocked(w)) return true;
            if (w.unlock != UnlockType.Coins || !SaveSystem.SpendCoins(w.coinCost)) return false;
            SaveSystem.Data.unlockedWeapons.Add(w.id);
            SaveSystem.Save(); AudioManager.Play(Sfx.Unlock);
            if (Changed != null) Changed();
            return true;
        }

        public bool Equip(WeaponData w, CharacterClass c)
        {
            if (!w.UsableBy(c) || !IsUnlocked(w)) return false;
            SaveSystem.SetEquip("weapon:" + c, w.id);
            if (Changed != null) Changed();
            return true;
        }

        public WeaponData GetEquipped(CharacterClass c)
        {
            var w = Get(SaveSystem.GetEquip("weapon:" + c));
            if (w != null && w.UsableBy(c) && IsUnlocked(w)) return w;
            foreach (var x in weapons) if (x.UsableBy(c) && IsUnlocked(x)) return x;
            return null;
        }
    }
}
