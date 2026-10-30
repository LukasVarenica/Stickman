using System.Collections;
using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>
    /// Put on a child at the blade tip with a TrailRenderer (also use a plain TrailRenderer on projectile prefabs).
    /// Call Begin()/End() from animation events, or Pulse(seconds) from attack code.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class WeaponTrail : MonoBehaviour
    {
        TrailRenderer trail; Coroutine routine;

        void Awake() { trail = GetComponent<TrailRenderer>(); trail.emitting = false; }
        public void Begin() { trail.Clear(); trail.emitting = true; }
        public void End() { trail.emitting = false; }
        public void Pulse(float seconds) { if (routine != null) StopCoroutine(routine); routine = StartCoroutine(PulseRoutine(seconds)); }
        IEnumerator PulseRoutine(float s) { Begin(); yield return new WaitForSeconds(s); End(); }

        public void SetColor(Color c)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;
        }
    }
}

