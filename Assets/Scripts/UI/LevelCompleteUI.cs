using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanWarriors
{
    public class LevelCompleteUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text title, enemies, damage, crits, hp, rewards, totalCoins;
        [SerializeField] Button continueButton;
        Action onContinue;

        void Awake() { if (root) root.SetActive(false); }

        public void Show(int level, LevelStats s, RewardBreakdown r, int coinsAfter, Action cont)
        {
            onContinue = cont;
            GameTime.Pause("LevelComplete");
            root.SetActive(true);
            title.text = "LEVEL " + level + " COMPLETE";
            enemies.text = "Enemies defeated: " + s.enemiesDefeated;
            damage.text = "Total damage dealt: " + Mathf.RoundToInt(s.damageDealt);
            crits.text = "Critical hits: " + s.crits;
            hp.text = "Remaining HP: " + Mathf.RoundToInt(s.hpRemaining) + " / " + Mathf.RoundToInt(s.hpMax) + " (" + Mathf.RoundToInt(s.HpPercent * 100f) + "%)";

            var sb = new StringBuilder();
            sb.AppendLine("Level reward  +" + r.levelBase);
            sb.AppendLine("Enemies  +" + r.kills);
            sb.AppendLine("Critical hits  +" + r.crits);
            if (r.noDamage > 0) sb.AppendLine("No damage bonus  +" + r.noDamage);
            sb.AppendLine("Health bonus  +" + r.health);
            if (r.boss > 0) sb.AppendLine("Boss bonus  +" + r.boss);
            rewards.text = sb.ToString();
            totalCoins.text = "+" + r.Total + " coins   (Total: " + coinsAfter + ")";

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(Continue);
            AudioManager.Play(Sfx.Coin);
            StartCoroutine(Reveal());
        }

        IEnumerator Reveal()
        {
            // Staggered fade-in using unscaled time (the game is paused while this screen is open).
            var texts = new[] { enemies, damage, crits, hp, rewards, totalCoins };
            foreach (var t in texts) t.alpha = 0f;
            foreach (var t in texts)
            {
                for (float k = 0; k < 0.2f; k += Time.unscaledDeltaTime) { t.alpha = k / 0.2f; yield return null; }
                t.alpha = 1f;
            }
        }

        void Continue()
        {
            AudioManager.Play(Sfx.UiClick);
            root.SetActive(false);
            var cb = onContinue; onContinue = null;
            if (cb != null) cb();
            GameTime.Resume("LevelComplete");
        }
    }
}

