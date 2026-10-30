using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanWarriors
{
    public class CosmeticMenuUI : MonoBehaviour
    {
        [SerializeField] Transform container;
        [SerializeField] ShopCardUI cardPrefab;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] Button[] typeButtons;    // order matches CosmeticType enum
        CosmeticType selected = CosmeticType.BodyColor;

        void Awake()
        {
            for (int i = 0; i < typeButtons.Length; i++)
            {
                var t = (CosmeticType)i;
                typeButtons[i].onClick.AddListener(() => { selected = t; Refresh(); });
            }
        }
        void OnEnable() { SaveSystem.Changed += Refresh; Refresh(); }
        void OnDisable() { SaveSystem.Changed -= Refresh; }

        void Refresh()
        {
            foreach (Transform t in container) Destroy(t.gameObject);
            var cm = CosmeticManager.Instance;
            var equipped = cm.GetEquipped(selected);
            if (coinsText) coinsText.text = "Coins: " + SaveSystem.Data.coins;

            foreach (var c in cm.OfType(selected))
            {
                var c2 = c;
                var card = Instantiate(cardPrefab, container);
                bool unlocked = cm.IsUnlocked(c);
                string statusLine, btn; bool interact = false; System.Action act = null;

                if (equipped == c) { statusLine = "Equipped"; btn = "Equipped"; }
                else if (unlocked) { statusLine = "Unlocked"; btn = "Equip"; interact = true; act = () => cm.Equip(c2); }
                else if (c.coinCost > 0)
                { statusLine = "Locked - " + cm.RequirementText(c); btn = "Unlock (" + c.coinCost + ")"; interact = SaveSystem.Data.coins >= c.coinCost; act = () => cm.TryUnlock(c2); }
                else { statusLine = "Locked - " + cm.RequirementText(c); btn = "Locked"; }

                card.Bind(c.icon, c.displayName, "Cosmetic only - no gameplay effect.", "", statusLine, btn, interact, act, !unlocked);
            }
        }
    }
}
