using UnityEngine;

namespace StickmanWarriors
{
    public static class SettingsManager
    {
        static SettingsData S => SaveSystem.Data.settings;

        public static float Master { get { return S.master; } set { S.master = Mathf.Clamp01(value); AudioManager.RefreshVolumes(); } }
        public static float Music  { get { return S.music; }  set { S.music = Mathf.Clamp01(value);  AudioManager.RefreshVolumes(); } }
        public static float Sfx    { get { return S.sfx; }    set { S.sfx = Mathf.Clamp01(value);    AudioManager.RefreshVolumes(); } }
        public static bool ScreenShakeEnabled { get { return S.screenShake; } set { S.screenShake = value; } }
        public static bool Fullscreen { get { return S.fullscreen; } set { S.fullscreen = value; Screen.fullScreen = value; } }
        public static int Quality
        {
            get { return Mathf.Clamp(S.quality, 0, QualitySettings.names.Length - 1); }
            set { S.quality = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1); QualitySettings.SetQualityLevel(S.quality, true); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { ApplyAll(); }

        public static void ApplyAll()
        {
            Screen.fullScreen = S.fullscreen;
            QualitySettings.SetQualityLevel(Quality, true);
            AudioManager.RefreshVolumes();
        }
    }
}
