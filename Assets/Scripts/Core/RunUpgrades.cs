using System;
using System.Collections.Generic;
using UnityEngine;

namespace StickmanWarriors
{
    public enum UpgradeCategory { Offensive, Defensive, Special }
    public enum UpgradeStat
    {
        AttackDamage, AttackSpeed, CritChance, CritDamage, AoeDamage,
        MaxHp, Armor, MoveSpeed, HealingEffectiveness,
        CooldownReduction, DashDistance, DashSpeed, Lifesteal, DoubleJumpDamage
    }

    [Serializable]
    public class UpgradeDef
    {
        public string id, displayName, description;
        public UpgradeCategory category;
        public UpgradeStat stat;
        public float value;          // 0.15 = +15%
        public int maxStacks = 99;
        public bool scytheOnly;      // lifesteal is meaningless for other classes
        public Sprite icon;          // loaded from Resources/UpgradeIcons/<id>
    }

    public static class UpgradeCatalog
    {
        static List<UpgradeDef> all;
        public static IReadOnlyList<UpgradeDef> All { get { if (all == null) Build(); return all; } }

        static UpgradeDef D(string id, string name, string desc, UpgradeCategory c, UpgradeStat s, float v, bool scythe = false)
        {
            return new UpgradeDef { id = id, displayName = name, description = desc, category = c, stat = s, value = v, scytheOnly = scythe,
                                    icon = Resources.Load<Sprite>("UpgradeIcons/" + id) };
        }

        static void Build()
        {
            all = new List<UpgradeDef>
            {
                D("atk_damage", "Brutal Strength", "+15% attack damage", UpgradeCategory.Offensive, UpgradeStat.AttackDamage, 0.15f),
                D("atk_speed", "Swift Strikes", "+10% attack speed", UpgradeCategory.Offensive, UpgradeStat.AttackSpeed, 0.10f),
                D("crit_chance", "Keen Eye", "+10% critical hit chance", UpgradeCategory.Offensive, UpgradeStat.CritChance, 0.10f),
                D("crit_damage", "Deadly Precision", "+25% critical hit damage", UpgradeCategory.Offensive, UpgradeStat.CritDamage, 0.25f),
                D("aoe_damage", "Shockwave", "+20% AOE damage", UpgradeCategory.Offensive, UpgradeStat.AoeDamage, 0.20f),
                D("max_hp", "Vitality", "+20% maximum HP", UpgradeCategory.Defensive, UpgradeStat.MaxHp, 0.20f),
                D("armor", "Iron Skin", "+15% armor (damage taken reduced)", UpgradeCategory.Defensive, UpgradeStat.Armor, 0.15f),
                D("move_speed", "Fleet Foot", "+10% movement speed", UpgradeCategory.Defensive, UpgradeStat.MoveSpeed, 0.10f),
                D("healing", "Restoration", "+15% healing effectiveness", UpgradeCategory.Defensive, UpgradeStat.HealingEffectiveness, 0.15f),
                D("cooldowns", "Quick Recovery", "-10% ability cooldowns", UpgradeCategory.Special, UpgradeStat.CooldownReduction, 0.10f),
                D("dash_distance", "Long Dash", "+20% dash distance", UpgradeCategory.Special, UpgradeStat.DashDistance, 0.20f),
                D("dash_speed", "Rapid Dash", "+15% dash speed", UpgradeCategory.Special, UpgradeStat.DashSpeed, 0.15f),
                D("lifesteal", "Vampiric Edge", "+10% lifesteal effectiveness", UpgradeCategory.Special, UpgradeStat.Lifesteal, 0.10f, true),
                D("double_jump", "Skyfall", "+25% double-jump damage", UpgradeCategory.Special, UpgradeStat.DoubleJumpDamage, 0.25f),
            };
        }

        public static UpgradeDef Get(string id) { foreach (var d in All) if (d.id == id) return d; return null; }

        /// <summary>Random distinct choices, skipping maxed upgrades and upgrades useless for this class.</summary>
        public static List<UpgradeDef> Roll(int count, CharacterClass cls)
        {
            var pool = new List<UpgradeDef>();
            foreach (var d in All)
            {
                if (d.scytheOnly && cls != CharacterClass.Scythe) continue;
                if (RunUpgrades.Stacks(d.id) >= d.maxStacks) continue;
                pool.Add(d);
            }
            for (int i = pool.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var t = pool[i]; pool[i] = pool[j]; pool[j] = t; }
            return pool.GetRange(0, Mathf.Min(count, pool.Count));
        }
    }

    /// <summary>Temporary per-run upgrades. Never saved; cleared by RunUpgrades.ResetRun() on New Game.</summary>
    public static class RunUpgrades
    {
        static readonly Dictionary<string, int> stacks = new Dictionary<string, int>();
        static readonly float[] sums = new float[Enum.GetValues(typeof(UpgradeStat)).Length];
        static float cooldownMult = 1f;
        public static event Action Changed;

        public static int Stacks(string id) { int n; return stacks.TryGetValue(id, out n) ? n : 0; }
        public static void ResetRun() { stacks.Clear(); Recalc(); Changed?.Invoke(); }
        public static void Apply(UpgradeDef d) { stacks[d.id] = Stacks(d.id) + 1; Recalc(); Changed?.Invoke(); }

        static void Recalc()
        {
            Array.Clear(sums, 0, sums.Length);
            cooldownMult = 1f;
            foreach (var kv in stacks)
            {
                var d = UpgradeCatalog.Get(kv.Key);
                if (d == null) continue;
                sums[(int)d.stat] += d.value * kv.Value;
                if (d.stat == UpgradeStat.CooldownReduction) cooldownMult *= Mathf.Pow(1f - d.value, kv.Value);
            }
        }

        static float S(UpgradeStat s) { return sums[(int)s]; }

        public static float DamageMult => 1f + S(UpgradeStat.AttackDamage);
        public static float AttackSpeedMult => 1f + S(UpgradeStat.AttackSpeed);
        public static float CritChance => Mathf.Clamp01(0.10f + S(UpgradeStat.CritChance));
        public static float CritMultiplier => 2f + S(UpgradeStat.CritDamage);
        public static float AoeMult => 1f + S(UpgradeStat.AoeDamage);
        public static float MaxHpMult => 1f + S(UpgradeStat.MaxHp);
        public static float Armor => Mathf.Min(0.75f, S(UpgradeStat.Armor));
        public static float MoveSpeedMult => 1f + S(UpgradeStat.MoveSpeed);
        public static float HealingMult => 1f + S(UpgradeStat.HealingEffectiveness);
        public static float CooldownMult => Mathf.Max(0.35f, cooldownMult);
        public static float DashDistanceMult => 1f + S(UpgradeStat.DashDistance);
        public static float DashSpeedMult => 1f + S(UpgradeStat.DashSpeed);
        public static float LifestealMult => 1f + S(UpgradeStat.Lifesteal);
        public static float DoubleJumpMult => 1f + S(UpgradeStat.DoubleJumpDamage);
    }
}
