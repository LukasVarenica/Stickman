using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StickmanWarriors
{
    public struct RewardBreakdown
    {
        public int levelBase, kills, crits, noDamage, health, boss;
        public int Total { get { return levelBase + kills + crits + noDamage + health + boss; } }
    }

    public static class LevelRewards
    {
        public static RewardBreakdown Calculate(int level, LevelStats s, bool finalBoss)
        {
            return new RewardBreakdown
            {
                levelBase = 25 + 25 * level,
                kills = s.enemiesDefeated * 5,
                crits = s.crits * 2,
                noDamage = s.damageTaken <= 0f ? 50 : 0,
                health = Mathf.RoundToInt(s.HpPercent * 30f),
                boss = finalBoss && s.bossDefeated ? 250 : 0
            };
        }
    }

    public static class GameFlow
    {
        /// <summary>Call from the Main Menu "New Game" button. Resets ALL temporary progress (upgrades, stats).</summary>
        public static void NewGame(string firstScene)
        {
            RunUpgrades.ResetRun(); RunTracker.BeginLevel(); GameTime.ClearAll();
            SceneManager.LoadScene(firstScene);
        }
        public static void RestartLevel() { GameTime.ClearAll(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        public static void MainMenu(string scene = "MainMenu") { GameTime.ClearAll(); SceneManager.LoadScene(scene); }
    }

    /// <summary>One per level scene. Call CompleteLevel() when the last enemy/boss is defeated (BossController does this itself).</summary>
    public class LevelFlow : MonoBehaviour
    {
        public static LevelFlow Instance { get; private set; }
        [SerializeField] int levelNumber = 1;
        [SerializeField] bool isFinalLevel;
        [SerializeField] int[] upgradeAfterLevels = { 1, 2 };
        [SerializeField] string nextSceneName;
        [SerializeField] string mainMenuScene = "MainMenu";
        [SerializeField] LevelCompleteUI completeUI;
        [SerializeField] UpgradeChoiceUI upgradeUI;
        bool completed;

        void Awake() { Instance = this; }
        void Start() { GameTime.ClearAll(); RunTracker.BeginLevel(); }

        public void CompleteLevelDelayed(float seconds) { StartCoroutine(Delay(seconds)); }
        IEnumerator Delay(float s) { yield return new WaitForSeconds(s); CompleteLevel(); }

        public void CompleteLevel()
        {
            if (completed) return;
            completed = true;

            var s = RunTracker.Level;
            var p = DamageResolver.Player;
            if (p != null) { s.hpRemaining = p.CurrentHP; s.hpMax = p.MaxHP; }

            var reward = LevelRewards.Calculate(levelNumber, s, isFinalLevel);
            var save = SaveSystem.Data;
            save.coins += reward.Total;
            save.highestLevelCompleted = Mathf.Max(save.highestLevelCompleted, levelNumber);
            SaveSystem.Save();
            AchievementManager.NotifyLevelComplete(s);

            if (completeUI != null) completeUI.Show(levelNumber, s, reward, save.coins, AfterSummary);
            else AfterSummary();
        }

        void AfterSummary()
        {
            bool offerUpgrade = upgradeUI != null && System.Array.IndexOf(upgradeAfterLevels, levelNumber) >= 0;
            if (offerUpgrade) upgradeUI.Show(Proceed); else Proceed();
        }

        void Proceed()
        {
            GameTime.ClearAll();
            if (isFinalLevel || string.IsNullOrEmpty(nextSceneName)) GameFlow.MainMenu(mainMenuScene);
            else SceneManager.LoadScene(nextSceneName);
        }
    }
}
