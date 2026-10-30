using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanWarriors
{
    public class WeaponMenuUI : MonoBehaviour
    {
        [SerializeField] Transform container;
        [SerializeField] ShopCardUI cardPrefab;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] Button[] classButtons;   // order: Tanky, Mage, Scythe
        CharacterClass selected = CharacterClass.Tanky;

        void Awake()
        {
            for (int i = 0; i < classButtons.Length; i++)
            {
                var c = (CharacterClass)i;
                classButtons[i].onClick.AddListener(() => { selected = c; Refresh(); });
            }
        }
        void OnEnable() { SaveSystem.Changed += Refresh; Refresh(); }
        void OnDisable() { SaveSystem.Changed -= Refresh; }

        void Refresh()
        {
            foreach (Transform t in container) Destroy(t.gameObject);
            var wm = WeaponManager.Instance;
            var equipped = wm.GetEquipped(selected);
            if (coinsText) coinsText.text = "Coins: " + SaveSystem.Data.coins;

            foreach (var w in wm.Weapons)
            {
                var w2 = w;
                var card = Instantiate(cardPrefab, container);
                bool usable = w.UsableBy(selected), unlocked = wm.IsUnlocked(w);
                string statLine = "DMG " + w.baseDamage + "   SPD " + w.attackSpeed.ToString("0.0") + "/s   RNG " + w.range.ToString("0.0");
                string statusLine, btn; bool interact = false; System.Action act = null;

                if (!usable) { statusLine = "Usable by: " + string.Join(", ", System.Array.ConvertAll(w.allowedClasses, x => x.ToString())); btn = "Incompatible"; }
                else if (equipped == w) { statusLine = "Equipped"; btn = "Equipped"; }
                else if (unlocked) { statusLine = "Unlocked"; btn = "Equip"; interact = true; act = () => wm.Equip(w2, selected); }
                else if (w.unlock == UnlockType.Coins)
                { statusLine = "Locked - " + wm.RequirementText(w); btn = "Unlock (" + w.coinCost + ")"; interact = SaveSystem.Data.coins >= w.coinCost; act = () => wm.TryUnlock(w2); }
                else { statusLine = "Locked - " + wm.RequirementText(w); btn = "Locked"; }

                card.Bind(w.icon, w.displayName, w.description, statLine, statusLine, btn, interact, act, !usable || !unlocked);
            }
        }
    }
}
