using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>
    /// Put on each playable character. Applies the equipped weapon (stats, animator override, trail, sprite) and
    /// equipped cosmetics (colours, eye glow, skins, particle trail, dash effect). Your attack code reads
    /// Weapon.baseDamage / AttackCooldown / AttackRange from here.
    /// </summary>
    public class PlayerLoadout : MonoBehaviour
    {
        [SerializeField] CharacterClass characterClass;
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer[] bodySprites;
        [SerializeField] LineRenderer[] bodyLines;
        [SerializeField] GameObject[] eyeGlowObjects;        // child sprites enabled when an eye glow is equipped
        [SerializeField] SpriteRenderer[] weaponSprites;     // [0] receives weapon/skin sprite
        [SerializeField] WeaponTrail weaponTrail;
        [SerializeField] Transform effectAnchor;             // where the particle trail is parented
        [SerializeField] GameObject defaultDashEffect;

        public CharacterClass Class { get { return characterClass; } }
        public WeaponData Weapon { get; private set; }
        public float AttackCooldown { get { return Weapon == null ? 0.5f : 1f / Mathf.Max(0.1f, Weapon.attackSpeed * RunUpgrades.AttackSpeedMult); } }
        public float AttackRange { get { return Weapon == null ? 1.5f : Weapon.range; } }

        RuntimeAnimatorController baseController; SafeAnimator anim;
        Color[] origBody, origLineStart, origLineEnd, origWeapon;
        GameObject trailInstance; GameObject dashPrefab;

        void Awake()
        {
            if (animator) { baseController = animator.runtimeAnimatorController; anim = new SafeAnimator(animator); }
            origBody = new Color[bodySprites.Length]; for (int i = 0; i < bodySprites.Length; i++) origBody[i] = bodySprites[i].color;
            origLineStart = new Color[bodyLines.Length]; origLineEnd = new Color[bodyLines.Length];
            for (int i = 0; i < bodyLines.Length; i++) { origLineStart[i] = bodyLines[i].startColor; origLineEnd[i] = bodyLines[i].endColor; }
            origWeapon = new Color[weaponSprites.Length]; for (int i = 0; i < weaponSprites.Length; i++) origWeapon[i] = weaponSprites[i].color;
        }

        void OnEnable() { WeaponManager.Changed += Apply; CosmeticManager.Changed += Apply; Apply(); }
        void OnDisable() { WeaponManager.Changed -= Apply; CosmeticManager.Changed -= Apply; }

        void Apply() { ApplyWeapon(); ApplyCosmetics(); }

        void ApplyWeapon()
        {
            Weapon = WeaponManager.Instance.GetEquipped(characterClass);
            if (Weapon == null) return;
            if (animator)
            {
                animator.runtimeAnimatorController = Weapon.animatorOverride != null ? Weapon.animatorOverride : baseController;
                anim = new SafeAnimator(animator);
                anim.SetInt("WeaponType", Weapon.animatorWeaponType);
            }
            if (Weapon.weaponSprite && weaponSprites.Length > 0) weaponSprites[0].sprite = Weapon.weaponSprite;
        }

        void ApplyCosmetics()
        {
            var cm = CosmeticManager.Instance;

            var body = cm.GetEquipped(CosmeticType.BodyColor);
            bool bodyOv = body != null && body.color.a > 0f;
            for (int i = 0; i < bodySprites.Length; i++) bodySprites[i].color = bodyOv ? body.color : origBody[i];
            for (int i = 0; i < bodyLines.Length; i++)
            {
                bodyLines[i].startColor = bodyOv ? body.color : origLineStart[i];
                bodyLines[i].endColor = bodyOv ? body.color : origLineEnd[i];
            }

            var eyes = cm.GetEquipped(CosmeticType.EyeGlow);
            bool glow = eyes != null && eyes.color.a > 0f;
            foreach (var g in eyeGlowObjects)
            {
                if (!g) continue;
                g.SetActive(glow);
                var sr = g.GetComponent<SpriteRenderer>(); if (sr && glow) sr.color = eyes.color;
            }

            var wc = cm.GetEquipped(CosmeticType.WeaponColor);
            bool wOv = wc != null && wc.color.a > 0f;
            for (int i = 0; i < weaponSprites.Length; i++) weaponSprites[i].color = wOv ? wc.color : origWeapon[i];
            if (weaponTrail) weaponTrail.SetColor(wOv ? wc.color : (Weapon != null ? Weapon.trailColor : Color.white));

            var skin = cm.GetEquipped(CosmeticType.WeaponSkin);
            if (skin != null && skin.sprite != null && Weapon != null && skin.weaponIdFilter == Weapon.id && weaponSprites.Length > 0)
                weaponSprites[0].sprite = skin.sprite;

            if (trailInstance) Destroy(trailInstance);
            var pt = cm.GetEquipped(CosmeticType.ParticleTrail);
            if (pt != null && pt.effectPrefab != null) trailInstance = Instantiate(pt.effectPrefab, effectAnchor ? effectAnchor : transform);
            var dash = cm.GetEquipped(CosmeticType.DashEffect);
            dashPrefab = dash != null ? dash.effectPrefab : null;
        }

        /// <summary>Call when dashing. Spawns the equipped dash effect (or the default).</summary>
        public void PlayDashEffect(Vector3 position, int facing)
        {
            var prefab = dashPrefab != null ? dashPrefab : defaultDashEffect;
            if (prefab == null) return;
            var fx = Instantiate(prefab, position, Quaternion.identity);
            var s = fx.transform.localScale; s.x = Mathf.Abs(s.x) * (facing >= 0 ? 1 : -1); fx.transform.localScale = s;
            Destroy(fx, 2f);
        }
    }
}
