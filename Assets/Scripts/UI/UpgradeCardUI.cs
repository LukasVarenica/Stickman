using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanWarriors
{
    public class UpgradeCardUI : MonoBehaviour
    {
        [SerializeField] Image icon, frame;
        [SerializeField] TMP_Text title, description, category, owned;
        [SerializeField] Button button;
        [SerializeField] Color offensive = new Color(1f, 0.4f, 0.3f), defensive = new Color(0.3f, 0.7f, 1f), special = new Color(0.8f, 0.5f, 1f);

        public void Bind(UpgradeDef d, Action<UpgradeDef> onPick)
        {
            Color c = d.category == UpgradeCategory.Offensive ? offensive : d.category == UpgradeCategory.Defensive ? defensive : special;
            if (frame) frame.color = c;
            if (icon) { icon.sprite = d.icon; icon.enabled = d.icon != null; icon.color = d.icon != null ? Color.white : c; }
            title.text = d.displayName;
            description.text = d.description;
            if (category) { category.text = d.category.ToString().ToUpper(); category.color = c; }
            int n = RunUpgrades.Stacks(d.id);
            if (owned) owned.text = n > 0 ? "Owned x" + n : "";
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onPick(d));
            button.interactable = true;
            gameObject.SetActive(true);
        }

        public void SetInteractable(bool v) { button.interactable = v; }
    }
}
