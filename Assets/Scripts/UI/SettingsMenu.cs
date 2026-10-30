using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanWarriors
{
    /// <summary>Works from both the Pause menu and the Main Menu. Applies instantly, saves when the panel closes.</summary>
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] Slider master, music, sfx;
        [SerializeField] Toggle screenShake, fullscreen;
        [SerializeField] TMP_Dropdown quality;

        void Awake()
        {
            master.onValueChanged.AddListener(v => SettingsManager.Master = v);
            music.onValueChanged.AddListener(v => SettingsManager.Music = v);
            sfx.onValueChanged.AddListener(v => SettingsManager.Sfx = v);
            screenShake.onValueChanged.AddListener(v => SettingsManager.ScreenShakeEnabled = v);
            fullscreen.onValueChanged.AddListener(v => SettingsManager.Fullscreen = v);
            quality.onValueChanged.AddListener(v => SettingsManager.Quality = v);
        }

        void OnEnable()
        {
            master.SetValueWithoutNotify(SettingsManager.Master);
            music.SetValueWithoutNotify(SettingsManager.Music);
            sfx.SetValueWithoutNotify(SettingsManager.Sfx);
            screenShake.SetIsOnWithoutNotify(SettingsManager.ScreenShakeEnabled);
            fullscreen.SetIsOnWithoutNotify(SettingsManager.Fullscreen);
            quality.ClearOptions();
            quality.AddOptions(new List<string>(QualitySettings.names));
            quality.SetValueWithoutNotify(SettingsManager.Quality);
        }

        void OnDisable() { SaveSystem.Save(); }
    }
}
