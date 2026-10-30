using TMPro;
using UnityEngine;

namespace StickmanWarriors
{
    public class AchievementsMenuUI : MonoBehaviour
    {
        [SerializeField] Transform container;
        [SerializeField] ShopCardUI cardPrefab;
        [SerializeField] TMP_Text counter;

        void OnEnable() { SaveSystem.Changed += Refresh; Refresh(); }
        void OnDisable() { SaveSystem.Changed -= Refresh; }

        void Refresh()
        {
            foreach (Transform t in container) Destroy(t.gameObject);
            int done = 0;
            foreach (var a in AchievementManager.All)
            {
                bool u = AchievementManager.IsUnlocked(a.id);
                if (u) done++;
                Instantiate(cardPrefab, container).Bind(a.icon, a.displayName, a.description, "", u ? "UNLOCKED" : "Locked", null, false, null, !u);
            }
            if (counter) counter.text = done + " / " + AchievementManager.All.Count + " unlocked";
        }
    }
}
