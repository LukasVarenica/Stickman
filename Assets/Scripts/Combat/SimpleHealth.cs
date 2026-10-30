using UnityEngine;
using UnityEngine.Events;

namespace StickmanWarriors
{
    /// <summary>
    /// Ready-made IDamageable. Use it for the boss/new entities, or copy its interface members into
    /// your existing Health scripts. Player max HP scales with the Max HP upgrade automatically.
    /// </summary>
    public class SimpleHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] float baseMaxHp = 100f;
        [SerializeField] bool isPlayer, isBoss;
        [SerializeField] Transform headAnchor;
        [SerializeField] float headOffset = 0.4f;
        public UnityEvent onDeath;
        public UnityEvent<float, float> onHpChanged;   // current, max

        float cur, max;

        public GameObject Owner => gameObject;
        public bool IsPlayer => isPlayer;
        public bool IsBoss => isBoss;
        public bool IsDead { get; private set; }
        public bool Invulnerable { get; set; }
        public float CurrentHP => cur;
        public float MaxHP => max;

        public Vector3 HeadPosition
        {
            get
            {
                if (headAnchor) return headAnchor.position;
                var c = GetComponentInChildren<Collider2D>();
                if (c) return new Vector3(c.bounds.center.x, c.bounds.max.y + headOffset, 0f);
                return transform.position + Vector3.up * 1.5f;
            }
        }

        void Awake() { max = baseMaxHp * (isPlayer ? RunUpgrades.MaxHpMult : 1f); cur = max; }
        void Start() { Notify(); }
        void OnEnable() { if (isPlayer) RunUpgrades.Changed += Rescale; }
        void OnDisable() { if (isPlayer) RunUpgrades.Changed -= Rescale; }

        void Rescale()
        {
            float newMax = baseMaxHp * RunUpgrades.MaxHpMult;
            cur = Mathf.Clamp(cur + Mathf.Max(0f, newMax - max), 0f, newMax);
            max = newMax; Notify();
        }

        public void TakeDamageRaw(float amount)
        {
            if (IsDead || Invulnerable) return;
            cur = Mathf.Max(0f, cur - amount);
            Notify();
            if (cur <= 0f) { IsDead = true; onDeath?.Invoke(); }
        }

        public void HealRaw(float amount) { if (IsDead) return; cur = Mathf.Min(max, cur + amount); Notify(); }
        public void ResetHealth() { IsDead = false; cur = max; Notify(); }
        void Notify() { onHpChanged?.Invoke(cur, max); }
    }
}

