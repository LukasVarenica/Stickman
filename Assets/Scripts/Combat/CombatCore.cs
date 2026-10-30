using System;
using UnityEngine;

namespace StickmanWarriors
{
    public enum CharacterClass { Tanky, Mage, Scythe }
    public enum DamageKind { Basic, Ability, Aoe, DoubleJump, Enemy, Boss }

    public struct DamageInfo
    {
        public float amount; public bool isCrit; public DamageKind kind; public Vector2 point; public GameObject source;
    }

    /// <summary>Implement this on any health component (player, enemies, boss). SimpleHealth is a ready-made one.</summary>
    public interface IDamageable
    {
        GameObject Owner { get; }
        Vector3 HeadPosition { get; }
        bool IsPlayer { get; }
        bool IsBoss { get; }
        bool IsDead { get; }
        bool Invulnerable { get; }
        float CurrentHP { get; }
        float MaxHP { get; }
        void TakeDamageRaw(float amount);
        void HealRaw(float amount);
    }

    public static class CombatEvents
    {
        public static event Action<IDamageable, DamageInfo> Damaged;
        public static event Action<IDamageable, int, bool> Healed;      // target, HP actually restored, isLifesteal
        public static event Action<IDamageable, GameObject> Died;
        public static event Action<Vector2, int> LifestealFlow;          // from point, HP restored

        public static void RaiseDamaged(IDamageable t, DamageInfo i) { Damaged?.Invoke(t, i); }
        public static void RaiseHealed(IDamageable t, int amount, bool lifesteal) { Healed?.Invoke(t, amount, lifesteal); }
        public static void RaiseDied(IDamageable t, GameObject killer) { Died?.Invoke(t, killer); }
        public static void RaiseLifestealFlow(Vector2 from, int amount) { LifestealFlow?.Invoke(from, amount); }
    }

    public class LevelStats
    {
        public int enemiesDefeated, crits;
        public float damageDealt, damageTaken, hpRemaining, hpMax;
        public bool bossDefeated;
        public float HpPercent { get { return hpMax > 0f ? hpRemaining / hpMax : 0f; } }
    }

    /// <summary>Per-level stats. Reset by LevelFlow.Start / GameFlow.NewGame (never saved).</summary>
    public static class RunTracker
    {
        public static LevelStats Level = new LevelStats();
        public static void BeginLevel() { Level = new LevelStats(); }
    }

    /// <summary>
    /// Single entry point for ALL damage and healing. Applies upgrades, crits, armor, lifesteal,
    /// stat tracking and raises events used by damage numbers, FX, audio and achievements.
    /// </summary>
    public static class DamageResolver
    {
        public static IDamageable Player { get; private set; }
        public static CharacterClass PlayerClass { get; private set; }
        static float lifestealRatio;

        /// <param name="baseLifestealRatio">Fraction of damage healed (Scythe user's ORIGINAL lifesteal value). 0 for other classes.</param>
        public static void RegisterPlayer(IDamageable player, CharacterClass cls, float baseLifestealRatio = 0f)
        {
            Player = player; PlayerClass = cls; lifestealRatio = baseLifestealRatio;
        }

        public static DamageInfo PlayerHit(IDamageable target, float baseDamage, DamageKind kind, Vector2 point,
                                           GameObject source = null, bool canCrit = true)
        {
            if (target == null || target.IsDead || target.Invulnerable) return default(DamageInfo);

            float dmg = baseDamage * RunUpgrades.DamageMult;
            if (kind == DamageKind.Aoe) dmg *= RunUpgrades.AoeMult;
            if (kind == DamageKind.DoubleJump) dmg *= RunUpgrades.DoubleJumpMult;

            bool crit = canCrit && UnityEngine.Random.value < RunUpgrades.CritChance;
            if (crit) dmg *= RunUpgrades.CritMultiplier;

            int final = Mathf.Max(1, Mathf.RoundToInt(dmg));
            float applied = Mathf.Min(final, target.CurrentHP);
            var info = new DamageInfo { amount = final, isCrit = crit, kind = kind, point = point, source = source };

            RunTracker.Level.damageDealt += applied;
            if (crit) { RunTracker.Level.crits++; SaveSystem.Data.lifetimeCrits++; }

            target.TakeDamageRaw(final);
            CombatEvents.RaiseDamaged(target, info);

            if (PlayerClass == CharacterClass.Scythe && lifestealRatio > 0f && Player != null && !Player.IsDead)
            {
                int restored = Heal(Player, applied * lifestealRatio * RunUpgrades.LifestealMult, true);
                if (restored > 0) CombatEvents.RaiseLifestealFlow(point, restored);
            }

            if (target.IsDead) OnKilled(target, source);
            return info;
        }

        public static DamageInfo EnemyHit(IDamageable target, float baseDamage, DamageKind kind, Vector2 point, GameObject source = null)
        {
            if (target == null || target.IsDead || target.Invulnerable) return default(DamageInfo);

            float dmg = baseDamage;
            if (target.IsPlayer) dmg *= 1f - RunUpgrades.Armor;
            int final = Mathf.Max(1, Mathf.RoundToInt(dmg));
            float applied = Mathf.Min(final, target.CurrentHP);
            var info = new DamageInfo { amount = final, isCrit = false, kind = kind, point = point, source = source };

            if (target.IsPlayer) RunTracker.Level.damageTaken += applied;
            target.TakeDamageRaw(final);
            CombatEvents.RaiseDamaged(target, info);
            if (target.IsDead) OnKilled(target, source);
            return info;
        }

        /// <summary>Returns the HP actually restored (clamped to missing HP). Healing effectiveness upgrades apply here.</summary>
        public static int Heal(IDamageable target, float amount, bool lifesteal = false)
        {
            if (target == null || target.IsDead || amount <= 0f) return 0;
            int missing = Mathf.FloorToInt(target.MaxHP - target.CurrentHP);
            int raw = Mathf.Max(1, Mathf.RoundToInt(amount * RunUpgrades.HealingMult));
            int restored = Mathf.Clamp(raw, 0, missing);
            if (restored <= 0) return 0;
            target.HealRaw(restored);
            if (lifesteal) SaveSystem.Data.lifetimeLifesteal += restored;
            CombatEvents.RaiseHealed(target, restored, lifesteal);
            return restored;
        }

        static void OnKilled(IDamageable t, GameObject src)
        {
            if (!t.IsPlayer)
            {
                RunTracker.Level.enemiesDefeated++;
                SaveSystem.Data.lifetimeKills++;
                if (t.IsBoss) RunTracker.Level.bossDefeated = true;
            }
            CombatEvents.RaiseDied(t, src);
        }
    }
}
