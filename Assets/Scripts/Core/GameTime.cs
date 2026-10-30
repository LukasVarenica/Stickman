using System.Collections.Generic;
using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>
    /// The ONLY owner of Time.timeScale. Pause reasons stack ("Pause", "BossIntro", "Upgrade"...).
    /// All gameplay (enemies, projectiles, cooldowns, gameplay animators) must use scaled time.
    /// </summary>
    public class GameTime : MonoBehaviour
    {
        const float HitStopScale = 0.02f;
        static GameTime instance;
        readonly HashSet<string> reasons = new HashSet<string>();
        float hitStopUntil;

        public static bool IsPaused => instance != null && instance.reasons.Count > 0;
        public static bool Has(string reason) => instance != null && instance.reasons.Contains(reason);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { Ensure(); }

        static GameTime Ensure()
        {
            if (instance == null)
            {
                var go = new GameObject("GameTime");
                instance = go.AddComponent<GameTime>();
            }
            return instance;
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Pause(string reason) { var i = Ensure(); i.reasons.Add(reason); i.Apply(); }
        public static void Resume(string reason) { if (instance == null) return; instance.reasons.Remove(reason); instance.Apply(); }
        public static void ClearAll() { var i = Ensure(); i.reasons.Clear(); i.hitStopUntil = 0f; i.Apply(); }

        /// <summary>Very short freeze on powerful hits. Ignored while paused.</summary>
        public static void HitStop(float seconds)
        {
            var i = Ensure();
            if (i.reasons.Count > 0) return;
            i.hitStopUntil = Mathf.Max(i.hitStopUntil, Time.unscaledTime + seconds);
            i.Apply();
        }

        void Update() { Apply(); }
        void Apply() { Time.timeScale = reasons.Count > 0 ? 0f : (Time.unscaledTime < hitStopUntil ? HitStopScale : 1f); }
        void OnApplicationQuit() { SaveSystem.Save(); }
        void OnApplicationPause(bool paused) { if (paused) SaveSystem.Save(); }
    }
}
