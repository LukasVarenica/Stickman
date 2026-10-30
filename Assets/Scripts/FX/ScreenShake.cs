using UnityEngine;

namespace StickmanWarriors
{
    public enum ShakePreset { Light, Medium, Heavy, Massive }

    /// <summary>
    /// Put this on a camera that is a CHILD of your follow-rig (follow scripts move the parent; this only offsets locally).
    /// Trauma-based: shakes decay quickly and never stack past 1.0, so the camera cannot shake constantly.
    /// Uses unscaled time so it still works during hit-stop and boss intro.
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        static ScreenShake instance;
        [SerializeField] float maxOffset = 0.8f, maxRollDegrees = 2.5f, decayPerSecond = 1.9f, noiseSpeed = 28f;
        Vector3 basePos; Quaternion baseRot; float trauma; bool active;

        void Awake() { instance = this; basePos = transform.localPosition; baseRot = transform.localRotation; }
        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>Light: normal hits/dash. Medium: heavy enemy, boss swings. Heavy: slam/explosion/massive slash. Massive: boss death/ultimate.</summary>
        public static void Shake(ShakePreset p)
        {
            switch (p)
            {
                case ShakePreset.Light: Add(0.4f); break;
                case ShakePreset.Medium: Add(0.6f); break;
                case ShakePreset.Heavy: Add(0.8f); break;
                default: Add(1f); break;
            }
        }

        public static void Add(float t)
        {
            if (instance == null || !SettingsManager.ScreenShakeEnabled) return;
            instance.trauma = Mathf.Min(1f, Mathf.Max(instance.trauma, t));
        }

        void LateUpdate()
        {
            if (trauma <= 0.001f)
            {
                trauma = 0f;
                if (active) { transform.localPosition = basePos; transform.localRotation = baseRot; active = false; }
                return;
            }
            active = true;
            trauma = Mathf.Max(0f, trauma - decayPerSecond * Time.unscaledDeltaTime);
            float s = trauma * trauma, t = Time.unscaledTime * noiseSpeed;
            float x = Mathf.PerlinNoise(t, 0f) * 2f - 1f;
            float y = Mathf.PerlinNoise(0f, t + 17f) * 2f - 1f;
            float r = Mathf.PerlinNoise(t + 33f, t) * 2f - 1f;
            transform.localPosition = basePos + new Vector3(x, y, 0f) * maxOffset * s;
            transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, r * maxRollDegrees * s);
        }
    }
}
