using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>Flashes all sprite/line renderers on this character white when it takes damage. Add to player, enemies and boss.</summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] float duration = 0.08f;
        [SerializeField] Color flashColor = Color.white;
        IDamageable owner;
        SpriteRenderer[] sprites; LineRenderer[] lines;
        Color[] sc, ls, le;
        float timer;

        void Awake() { owner = GetComponent<IDamageable>(); if (owner == null) owner = GetComponentInParent<IDamageable>(); }
        void OnEnable() { CombatEvents.Damaged += OnDamaged; }
        void OnDisable() { CombatEvents.Damaged -= OnDamaged; if (timer > 0f) Restore(); }

        void OnDamaged(IDamageable t, DamageInfo info)
        {
            if (owner == null || t == null || t.Owner != owner.Owner) return;
            if (timer <= 0f) Capture();
            timer = duration;
            foreach (var s in sprites) if (s) s.color = flashColor;
            foreach (var l in lines) if (l) { l.startColor = flashColor; l.endColor = flashColor; }
        }

        void Capture()
        {
            sprites = GetComponentsInChildren<SpriteRenderer>();
            lines = GetComponentsInChildren<LineRenderer>();
            sc = new Color[sprites.Length]; ls = new Color[lines.Length]; le = new Color[lines.Length];
            for (int i = 0; i < sprites.Length; i++) sc[i] = sprites[i].color;
            for (int i = 0; i < lines.Length; i++) { ls[i] = lines[i].startColor; le[i] = lines[i].endColor; }
        }

        void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.unscaledDeltaTime;   // unscaled so the flash is visible during hit-stop and ends correctly
            if (timer <= 0f) Restore();
        }

        void Restore()
        {
            timer = 0f;
            if (sprites == null) return;
            for (int i = 0; i < sprites.Length; i++) if (sprites[i]) sprites[i].color = sc[i];
            for (int i = 0; i < lines.Length; i++) if (lines[i]) { lines[i].startColor = ls[i]; lines[i].endColor = le[i]; }
        }
    }
}
