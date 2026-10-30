using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanWarriors
{
    /// <summary>
    /// Pauses gameplay, shows "BOSS INCOMING!" then the boss name, shakes the screen, plays the boss intro animation
    /// (animator switched to Unscaled Time just for the intro), then starts the fight. Call Begin() or use BossTriggerZone.
    /// </summary>
    public class BossIntro : MonoBehaviour
    {
        const string Reason = "BossIntro";
        [SerializeField] BossController boss;
        [SerializeField] Animator bossAnimator;
        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform incomingRect, nameRect;
        [SerializeField] TMP_Text incomingLabel, nameLabel;
        [SerializeField] Image redFlash;
        [SerializeField] float incomingTime = 1.6f, nameTime = 2.0f;
        bool running;

        void Awake() { root.SetActive(false); }

        public void Begin() { if (!running) StartCoroutine(Run()); }

        IEnumerator Run()
        {
            running = true;
            GameTime.Pause(Reason);
            root.SetActive(true);
            incomingLabel.text = "BOSS INCOMING!"; nameLabel.text = boss.BossName;
            incomingRect.gameObject.SetActive(true); nameRect.gameObject.SetActive(false);
            group.alpha = 0f;

            var prevMode = AnimatorUpdateMode.Normal;
            if (bossAnimator) { prevMode = bossAnimator.updateMode; bossAnimator.updateMode = AnimatorUpdateMode.UnscaledTime; }
            boss.PlayIntroAnimation();

            ScreenShake.Shake(ShakePreset.Medium);
            AudioManager.Play(Sfx.BossIntro);
            StartCoroutine(Flash());
            yield return Fade(0f, 1f, 0.25f);
            yield return Punch(incomingRect, incomingTime);

            incomingRect.gameObject.SetActive(false);
            nameRect.gameObject.SetActive(true);
            ScreenShake.Shake(ShakePreset.Light);
            yield return Punch(nameRect, nameTime);

            yield return Fade(1f, 0f, 0.35f);
            root.SetActive(false);
            if (bossAnimator) bossAnimator.updateMode = prevMode;
            GameTime.Resume(Reason);
            boss.StartFight();
            running = false;
        }

        IEnumerator Fade(float a, float b, float dur)
        {
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime) { group.alpha = Mathf.Lerp(a, b, t / dur); yield return null; }
            group.alpha = b;
        }

        IEnumerator Punch(RectTransform rt, float dur)
        {
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / 0.3f), eased = 1f - Mathf.Pow(1f - k, 3f);
                float s = Mathf.Lerp(2.4f, 1f, eased) + (t > 0.3f ? Mathf.Sin((t - 0.3f) * 6f) * 0.03f : 0f);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        IEnumerator Flash()
        {
            if (redFlash == null) yield break;
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                var c = redFlash.color; c.a = Mathf.Lerp(0.6f, 0f, t / 0.5f); redFlash.color = c; yield return null;
            }
            var e = redFlash.color; e.a = 0f; redFlash.color = e;
        }
    }
}
