using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace StickmanWarriors
{
    public enum BossPhase { One = 1, Two = 2, Three = 3 }

    /// <summary>
    /// 3-phase final boss. Phase thresholds are read from the SimpleHealth HP ratio (70% / 35%).
    /// Attacks are timed by coroutines (scaled time => pause-safe), animator triggers are optional extras.
    /// Required components: SimpleHealth (isBoss = true), Rigidbody2D, Collider2D.
    /// </summary>
    [RequireComponent(typeof(SimpleHealth), typeof(Rigidbody2D))]
    public class BossController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] string bossName = "THE WARLORD";
        [SerializeField] bool autoStart;                       // true if you don't use BossIntro
        [SerializeField] bool artFacesRight = true;
        [SerializeField] float completeLevelDelay = 3f;

        [Header("References")]
        [SerializeField] Animator animator;
        [SerializeField] Transform meleePoint, firePoint;
        [SerializeField] SpriteRenderer[] renderers;
        [SerializeField] ParticleSystem redAura;
        [SerializeField] BossProjectile projectilePrefab;
        [SerializeField] GameObject telegraphPrefab;           // unit-diameter circle sprite (optional)

        [Header("Phase thresholds")]
        [SerializeField, Range(0f, 1f)] float phase2At = 0.70f;
        [SerializeField, Range(0f, 1f)] float phase3At = 0.35f;
        [SerializeField] Color phase2Tint = new Color(1f, 0.75f, 0.55f), phase3Tint = new Color(1f, 0.35f, 0.35f);
        [SerializeField] float[] speedMult = { 1f, 1.35f, 1.8f };
        [SerializeField] float[] attackSpeedMult = { 1f, 1.3f, 1.7f };
        [SerializeField] float[] damageMult = { 1f, 1f, 1.35f };

        [Header("Stats")]
        [SerializeField] float moveSpeed = 3f, meleeRange = 1.8f, meleeRadius = 1.2f, airDodgeHeight = 1.5f;
        [SerializeField] float meleeDamage = 18f, slamDamage = 28f, slamRadius = 3.5f;
        [SerializeField] float projectileDamage = 14f, projectileSpeed = 9f;
        [SerializeField] float aoeDamage = 34f, aoeRadius = 5f;
        [SerializeField] float ultimateDamage = 60f, ultimateRadius = 10f;
        [SerializeField] float baseRecovery = 0.7f;

        public event Action FightStarted;
        public event Action<BossPhase> PhaseChanged;
        public UnityEvent onDefeated;

        public string BossName { get { return bossName; } }
        public SimpleHealth Health { get; private set; }
        public BossPhase CurrentPhase { get; private set; } = BossPhase.One;
        public float Phase2At { get { return phase2At; } }
        public float Phase3At { get { return phase3At; } }

        enum Atk { None, Melee, Slam, Shoot, Aoe, Combo, Ultimate }

        Rigidbody2D rb; SafeAnimator anim; Color[] baseColors;
        bool fighting, transitioning, transRunning, attacking, dead, moving;
        BossPhase targetPhase = BossPhase.One;
        readonly float[] readyAt = new float[7];
        float nextDecide;

        int P { get { return (int)CurrentPhase; } }
        float AtkSpeed { get { return attackSpeedMult[P - 1]; } }
        float Dmg(float b) { return b * damageMult[P - 1]; }
        int Facing { get { return Target != null && Target.position.x < transform.position.x ? -1 : 1; } }
        Transform Target { get { return DamageResolver.Player != null ? DamageResolver.Player.Owner.transform : null; } }

        void Awake()
        {
            Health = GetComponent<SimpleHealth>();
            rb = GetComponent<Rigidbody2D>();
            anim = new SafeAnimator(animator);
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
            if (!autoStart) Health.Invulnerable = true;      // untouchable until the intro ends
        }

        void OnEnable() { Health.onHpChanged.AddListener(OnHp); Health.onDeath.AddListener(OnDeath); }
        void OnDisable() { Health.onHpChanged.RemoveListener(OnHp); Health.onDeath.RemoveListener(OnDeath); }
        void Start() { if (autoStart) StartFight(); }

        public void PlayIntroAnimation() { anim.Trigger("Intro"); }

        public void StartFight()
        {
            if (fighting || dead) return;
            fighting = true; Health.Invulnerable = false;
            anim.SetInt("Phase", 1);
            if (FightStarted != null) FightStarted();
            StartCoroutine(FightLoop());
        }

        // ---------- movement ----------
        void FixedUpdate()
        {
            if (!fighting || dead) return;
            SetVx(moving ? Facing * moveSpeed * speedMult[P - 1] : 0f);
        }

        void SetVx(float x)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
#else
            rb.velocity = new Vector2(x, rb.velocity.y);
#endif
        }

        void Face()
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * Facing * (artFacesRight ? 1 : -1);
            transform.localScale = s;
        }

        // ---------- phases ----------
        void OnHp(float cur, float max)
        {
            if (!fighting || dead || max <= 0f) return;
            float r = cur / max;
            BossPhase t = r <= phase3At ? BossPhase.Three : r <= phase2At ? BossPhase.Two : BossPhase.One;
            if (t > targetPhase) { targetPhase = t; if (!transRunning) StartCoroutine(TransitionRoutine()); }
        }

        IEnumerator TransitionRoutine()
        {
            transRunning = true; transitioning = true;
            while (CurrentPhase < targetPhase)
            {
                while (attacking) yield return null;                       // let the current attack finish
                BossPhase next = (BossPhase)((int)CurrentPhase + 1);
                Health.Invulnerable = true; moving = false; SetVx(0f);
                anim.Trigger("PhaseChange"); anim.SetInt("Phase", (int)next);
                AudioManager.Play(Sfx.BossPhase);
                ScreenShake.Shake(ShakePreset.Heavy);
                GameTime.HitStop(0.15f);
                if (next == BossPhase.Three && redAura) redAura.Play();
                yield return StartCoroutine(Tint(next == BossPhase.Two ? phase2Tint : phase3Tint, 1.0f));
                CurrentPhase = next;
                if (PhaseChanged != null) PhaseChanged(next);
                yield return new WaitForSeconds(0.6f);
                Health.Invulnerable = false;
            }
            transitioning = false; transRunning = false;
        }

        IEnumerator Tint(Color tint, float dur)
        {
            var from = new Color[renderers.Length];
            for (int i = 0; i < from.Length; i++) from[i] = renderers[i].color;
            Vector3 s0 = transform.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
            {
                for (int i = 0; i < renderers.Length; i++) renderers[i].color = Color.Lerp(from[i], baseColors[i] * tint, t);
                transform.localScale = s0 * (1f + 0.12f * Mathf.Sin(Mathf.PI * t));    // dramatic swell
                yield return null;
            }
            for (int i = 0; i < renderers.Length; i++) renderers[i].color = baseColors[i] * tint;
            transform.localScale = s0;
        }

        // ---------- AI ----------
        IEnumerator FightLoop()
        {
            yield return null;
            while (!dead)
            {
                if (transitioning || Target == null) { moving = false; yield return null; continue; }
                Face();
                float dist = Vector2.Distance(transform.position, Target.position);
                Atk a = Choose(dist);
                if (a == Atk.None) { moving = true; anim.SetBool("Moving", true); yield return null; continue; }

                moving = false; anim.SetBool("Moving", false);
                attacking = true;
                yield return StartCoroutine(Run(a));
                attacking = false;
                yield return new WaitForSeconds(Mathf.Max(0.15f, baseRecovery / AtkSpeed));
            }
        }

        bool Ready(Atk a) { return Time.time >= readyAt[(int)a]; }
        void Cooldown(Atk a, float s) { readyAt[(int)a] = Time.time + s / AtkSpeed; }

        Atk Choose(float d)
        {
            if (P >= 3 && Ready(Atk.Ultimate)) return Atk.Ultimate;
            if (d <= meleeRange)
            {
                float r = UnityEngine.Random.value;
                if (P >= 3 && Ready(Atk.Combo) && r < 0.45f) return Atk.Combo;
                if (P >= 2 && Ready(Atk.Aoe) && r < 0.30f) return Atk.Aoe;
                if (Ready(Atk.Slam) && r < 0.25f) return Atk.Slam;
                return Atk.Melee;
            }
            if (Time.time < nextDecide) return Atk.None;
            nextDecide = Time.time + 0.4f;
            if (P >= 2 && d <= aoeRadius && Ready(Atk.Aoe) && UnityEngine.Random.value < 0.5f) return Atk.Aoe;
            if (d <= slamRadius + 1.5f && Ready(Atk.Slam) && UnityEngine.Random.value < 0.5f) return Atk.Slam;
            if (d >= 5f && Ready(Atk.Shoot) && UnityEngine.Random.value < 0.6f) return Atk.Shoot;
            return Atk.None;
        }

        IEnumerator Run(Atk a)
        {
            switch (a)
            {
                case Atk.Melee: return Melee();
                case Atk.Slam: return Slam();
                case Atk.Shoot: return Shoot();
                case Atk.Aoe: return AoeBurst();
                case Atk.Combo: return Combo();
                default: return Ultimate();
            }
        }

        WaitForSeconds W(float t) { return new WaitForSeconds(t / AtkSpeed); }
        Vector2 Feet { get { return transform.position; } }

        void HitCircle(Vector2 c, float r, float dmg, DamageKind kind, bool airDodge)
        {
            var p = DamageResolver.Player;
            if (p == null || p.IsDead) return;
            Vector2 pos = p.Owner.transform.position;
            if (airDodge && pos.y > transform.position.y + airDodgeHeight) return;   // jump over shockwaves
            if (Vector2.Distance(c, pos + Vector2.up * 0.8f) <= r) DamageResolver.EnemyHit(p, Dmg(dmg), kind, pos + Vector2.up, gameObject);
        }

        void Telegraph(Vector2 pos, float radius, float seconds)
        {
            if (telegraphPrefab == null) return;
            var t = Instantiate(telegraphPrefab, pos, Quaternion.identity);
            t.transform.localScale = Vector3.one * radius * 2f;
            Destroy(t, seconds);
        }

        IEnumerator Melee()
        {
            Cooldown(Atk.Melee, 0.8f);
            anim.Trigger("Melee"); AudioManager.Play(Sfx.BossAttack);
            yield return W(0.35f);
            if (dead) yield break;
            Vector2 c = meleePoint ? (Vector2)meleePoint.position : Feet + new Vector2(Facing * meleeRange * 0.6f, 1f);
            HitCircle(c, meleeRadius, meleeDamage, DamageKind.Boss, false);
            if (P >= 2) ScreenShake.Shake(ShakePreset.Light);
            yield return W(0.25f);
        }

        IEnumerator Slam()
        {
            Cooldown(Atk.Slam, 5f);
            anim.Trigger("Slam"); AudioManager.Play(Sfx.BossAttack);
            Telegraph(Feet, slamRadius, 0.6f / AtkSpeed);
            yield return W(0.6f);
            if (dead) yield break;
            HitCircle(Feet, slamRadius, slamDamage, DamageKind.Boss, true);
            CombatFXManager.GroundImpact(Feet, 1.5f);
            ScreenShake.Shake(ShakePreset.Heavy);
            GameTime.HitStop(0.05f);
            yield return W(0.3f);
        }

        IEnumerator Shoot()
        {
            Cooldown(Atk.Shoot, P == 1 ? 7f : 4f);
            anim.Trigger("Shoot");
            yield return W(0.4f);
            if (dead || projectilePrefab == null || Target == null) yield break;
            Vector2 origin = firePoint ? (Vector2)firePoint.position : Feet + Vector2.up;
            Vector2 dir = ((Vector2)Target.position + Vector2.up * 0.8f - origin).normalized;
            int count = P >= 3 ? 3 : 1;
            for (int i = 0; i < count; i++)
            {
                float ang = count == 1 ? 0f : (i - 1) * 12f;
                var pr = Instantiate(projectilePrefab, origin, Quaternion.identity);
                pr.Init(Quaternion.Euler(0, 0, ang) * dir, Dmg(projectileDamage), projectileSpeed);
            }
            AudioManager.Play(Sfx.BossAttack);
            if (P >= 2) ScreenShake.Shake(ShakePreset.Light);
            yield return W(0.25f);
        }

        IEnumerator AoeBurst()
        {
            Cooldown(Atk.Aoe, 7f);
            anim.Trigger("Aoe"); AudioManager.Play(Sfx.BossAttack);
            Telegraph(Feet, aoeRadius, 0.9f / AtkSpeed);
            yield return W(0.9f);
            if (dead) yield break;
            HitCircle(Feet + Vector2.up, aoeRadius, aoeDamage, DamageKind.Boss, false);
            CombatFXManager.Explosion(Feet + Vector2.up, 1.8f);
            ScreenShake.Shake(ShakePreset.Heavy);
            GameTime.HitStop(0.06f);
            yield return W(0.35f);
        }

        IEnumerator Combo()
        {
            Cooldown(Atk.Combo, 4f);
            for (int i = 0; i < 3 && !dead; i++)
            {
                Face();
                anim.Trigger("Melee"); AudioManager.Play(Sfx.BossAttack);
                SetVx(Facing * 7f);                                         // short lunge
                yield return W(0.22f);
                SetVx(0f);
                Vector2 c = meleePoint ? (Vector2)meleePoint.position : Feet + new Vector2(Facing * meleeRange * 0.6f, 1f);
                HitCircle(c, meleeRadius, meleeDamage * (i == 2 ? 1.4f : 1f), DamageKind.Boss, false);
                ScreenShake.Shake(i == 2 ? ShakePreset.Medium : ShakePreset.Light);
                yield return W(0.12f);
            }
        }

        IEnumerator Ultimate()
        {
            Cooldown(Atk.Ultimate, 14f);
            anim.Trigger("Ultimate"); AudioManager.Play(Sfx.BossIntro);
            Telegraph(Feet, ultimateRadius, 1.8f);
            float charge = 1.8f;
            for (float t = 0f; t < charge; t += 0.3f)                      // rising rumble while charging
            {
                ScreenShake.Shake(ShakePreset.Light);
                yield return new WaitForSeconds(0.3f);
                if (dead) yield break;
            }
            HitCircle(Feet + Vector2.up, ultimateRadius, ultimateDamage, DamageKind.Boss, true);   // jump to avoid
            CombatFXManager.Explosion(Feet + Vector2.up, 3f);
            ScreenShake.Shake(ShakePreset.Massive);
            GameTime.HitStop(0.1f);
            if (projectilePrefab != null)
            {
                for (int i = 0; i < 12; i++)
                {
                    var pr = Instantiate(projectilePrefab, Feet + Vector2.up, Quaternion.identity);
                    pr.Init(Quaternion.Euler(0, 0, i * 30f) * Vector2.right, Dmg(projectileDamage), projectileSpeed);
                }
            }
            yield return new WaitForSeconds(0.7f);
        }

        // ---------- death ----------
        void OnDeath()
        {
            if (dead) return;
            dead = true; fighting = false; moving = false;
            StopAllCoroutines();
            SetVx(0f);
            anim.Trigger("Death");
            if (redAura) redAura.Stop();
            AudioManager.Play(Sfx.Death);
            ScreenShake.Shake(ShakePreset.Massive);
            GameTime.HitStop(0.25f);
            CombatFXManager.Explosion(transform.position + Vector3.up, 2.5f);
            if (onDefeated != null) onDefeated.Invoke();
            if (LevelFlow.Instance != null) LevelFlow.Instance.CompleteLevelDelayed(completeLevelDelay);
        }
    }
}

