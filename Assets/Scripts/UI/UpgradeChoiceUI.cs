using System;
using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>Shows 3 random upgrades; exactly one can be picked. Upgrades are applied to RunUpgrades immediately.</summary>
    public class UpgradeChoiceUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] UpgradeCardUI[] cards;   // 3 cards
        Action onDone; bool chosen;

        void Awake() { if (root) root.SetActive(false); }

        public void Show(Action done)
        {
            onDone = done; chosen = false;
            GameTime.Pause("Upgrade");
            var choices = UpgradeCatalog.Roll(cards.Length, DamageResolver.PlayerClass);
            for (int i = 0; i < cards.Length; i++)
            {
                if (i < choices.Count) cards[i].Bind(choices[i], Choose);
                else cards[i].gameObject.SetActive(false);
            }
            root.SetActive(true);
        }

        void Choose(UpgradeDef d)
        {
            if (chosen) return;
            chosen = true;
            foreach (var c in cards) c.SetInteractable(false);
            RunUpgrades.Apply(d);
            AudioManager.Play(Sfx.UiClick);
            root.SetActive(false);
            var cb = onDone; onDone = null;
            if (cb != null) cb();
            GameTime.Resume("Upgrade");
        }
    }
}
