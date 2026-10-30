using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>Hit sparks, crit bursts, death bursts, lifesteal orbs, hit-stop and damage/heal/death sounds. Static helpers for ability FX.</summary>
    public class CombatFXManager : MonoBehaviour
    {
        static CombatFXManager instance;
        [SerializeField] ParticleSystem hitSpark, critBurst, deathBurst, explosion, groundImpact, dust;
        [SerializeField] LifestealOrb lifestealOrbPrefab;

        void Awake() { instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }
        void OnEnable()
        {
            CombatEvents.Damaged += OnDamaged; CombatEvents.Healed += OnHealed;
            CombatEvents.Died += OnDied; CombatEvents.LifestealFlow += OnLifestealFlow;
        }
        void OnDisable()
        {
            CombatEvents.Damaged -= OnDamaged; CombatEvents.Healed -= OnHealed;
            CombatEvents.Died -= OnDied; CombatEvents.LifestealFlow -= OnLifestealFlow;
        }

        void OnDamaged(IDamageable t, DamageInfo i)
        {
            Vector2 p = i.point == Vector2.zero ? (Vector2)t.HeadPosition : i.point;
            Burst(i.isCrit ? critBurst : hitSpark, p, i.isCrit ? 1.3f : 1f);

            bool heavy = i.kind == DamageKind.Ability || i.kind == DamageKind.Aoe || i.kind == DamageKind.DoubleJump;
            if (t.IsPlayer) AudioManager.Play(Sfx.PlayerHurt);
            else AudioManager.Play(i.isCrit ? Sfx.Crit : heavy ? Sfx.Ability : Sfx.Hit);

            if (!t.IsPlayer) { if (i.isCrit) GameTime.HitStop(0.06f); else if (heavy) GameTime.HitStop(0.04f); }
            else if (i.amount >= t.MaxHP * 0.15f) GameTime.HitStop(0.05f);
        }

        void OnHealed(IDamageable t, int amount, bool lifesteal) { AudioManager.Play(lifesteal ? Sfx.Lifesteal : Sfx.Heal); }
        void OnDied(IDamageable t, GameObject killer)
        {
            if (t.IsBoss) return;   // BossController plays its own death sequence
            AudioManager.Play(Sfx.Death);
            Burst(deathBurst, t.Owner.transform.position + Vector3.up, 1f);
        }

        void OnLifestealFlow(Vector2 from, int restored)
        {
            if (lifestealOrbPrefab == null || DamageResolver.Player == null) return;
            int n = Mathf.Clamp(restored / 4 + 1, 1, 6);
            for (int k = 0; k < n; k++)
            {
                var orb = Instantiate(lifestealOrbPrefab, from, Quaternion.identity);
                orb.Init(DamageResolver.Player.Owner.transform);
            }
        }

        static void Burst(ParticleSystem prefab, Vector2 pos, float scale)
        {
            if (prefab == null) return;
            var inst = Instantiate(prefab, pos, Quaternion.identity);
            inst.transform.localScale *= scale;
            inst.Play();
            var m = inst.main;
            Destroy(inst.gameObject, m.duration + m.startLifetime.constantMax + 0.1f);
        }

        // --- helpers for ability code ---
        public static void Explosion(Vector2 pos, float scale = 1f) { if (instance) { Burst(instance.explosion, pos, scale); AudioManager.Play(Sfx.Explosion); } }
        public static void GroundImpact(Vector2 pos, float scale = 1f) { if (instance) { Burst(instance.groundImpact, pos, scale); AudioManager.Play(Sfx.Slam); } }
        public static void Dust(Vector2 pos, float scale = 1f) { if (instance) Burst(instance.dust, pos, scale); }
    }
}
