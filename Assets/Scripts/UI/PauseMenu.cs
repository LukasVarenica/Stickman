using UnityEngine;
using UnityEngine.UI;

namespace StickmanWarriors
{
    /// <summary>ESC toggles. Freezes everything via GameTime (enemies, projectiles, cooldowns, physics, scaled animators).</summary>
    public class PauseMenu : MonoBehaviour
    {
        const string Reason = "Pause";
        [SerializeField] GameObject root, mainPanel, settingsPanel;
        [SerializeField] Button resume, restart, settings, mainMenu, settingsBack;
        [SerializeField] string mainMenuScene = "MainMenu";
        bool open;

        void Awake()
        {
            root.SetActive(false);
            resume.onClick.AddListener(Close);
            restart.onClick.AddListener(() => { AudioManager.Play(Sfx.UiClick); GameFlow.RestartLevel(); });
            settings.onClick.AddListener(() => { AudioManager.Play(Sfx.UiClick); ShowSettings(true); });
            settingsBack.onClick.AddListener(() => ShowSettings(false));
            mainMenu.onClick.AddListener(() => { AudioManager.Play(Sfx.UiClick); GameFlow.MainMenu(mainMenuScene); });
        }

        static bool EscPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        void Update()
        {
            if (!EscPressed()) return;
            if (open) { if (settingsPanel.activeSelf) ShowSettings(false); else Close(); }
            else if (!GameTime.IsPaused) Open();   // ignore ESC during boss intro / level complete / upgrade screens
        }

        void Open() { open = true; root.SetActive(true); ShowSettings(false); GameTime.Pause(Reason); }
        void Close() { open = false; root.SetActive(false); GameTime.Resume(Reason); }
        void ShowSettings(bool on) { settingsPanel.SetActive(on); mainPanel.SetActive(!on); }
        void OnDisable() { if (open) { open = false; GameTime.Resume(Reason); } }
    }
}
