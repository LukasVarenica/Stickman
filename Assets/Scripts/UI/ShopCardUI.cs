using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanWarriors
{
    /// <summary>One reusable card for weapons, cosmetics and achievements.</summary>
    public class ShopCardUI : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title, description, stats, status, buttonLabel;
        [SerializeField] Button button;
        [SerializeField] CanvasGroup group;

        /// <param name="buttonText">null hides the button (achievements).</param>
        public void Bind(Sprite ic, string t, string desc, string statLine, string statusLine,
                         string buttonText, bool interactable, Action onClick, bool dimmed)
        {
            if (icon) { icon.sprite = ic; icon.enabled = ic != null; }
            title.text = t; description.text = desc;
            if (stats) stats.text = statLine ?? "";
            if (status) status.text = statusLine ?? "";
            if (group) group.alpha = dimmed ? 0.55f : 1f;
            if (button)
            {
                button.gameObject.SetActive(buttonText != null);
                if (buttonText != null)
                {
                    if (buttonLabel) buttonLabel.text = buttonText;
                    button.interactable = interactable;
                    button.onClick.RemoveAllListeners();
                    if (onClick != null) button.onClick.AddListener(() => { AudioManager.Play(Sfx.UiClick); onClick(); });
                }
            }
        }
    }
}
