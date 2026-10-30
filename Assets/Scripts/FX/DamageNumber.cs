using System;
using TMPro;
using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>World-space TextMeshPro (not UGUI) floating number: pop, rise, fade. Uses scaled time so it pauses with the game.</summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class DamageNumber : MonoBehaviour
    {
        TextMeshPro text;
        float age, life, scale; Color color; Vector3 drift; Action<DamageNumber> release;

        void Awake()
        {
            text = GetComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            GetComponent<MeshRenderer>().sortingOrder = 1000;
        }

        public void Play(string s, Color c, float baseScale, Vector3 pos, float lifetime, Action<DamageNumber> onDone)
        {
            text.text = s; color = c; scale = baseScale; life = lifetime; release = onDone; age = 0f;
            transform.position = pos;
            transform.localScale = Vector3.one * scale * 0.4f;
            text.color = c;
            drift = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0f, 0f);
            gameObject.SetActive(true);
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / life;
            // pop: 0.4 -> 1.3 -> 1.0
            float pop = age < 0.12f ? Mathf.Lerp(0.4f, 1.3f, age / 0.12f)
                      : age < 0.22f ? Mathf.Lerp(1.3f, 1f, (age - 0.12f) / 0.10f) : 1f;
            transform.localScale = Vector3.one * scale * pop;
            transform.position += (Vector3.up * 1.4f * (1f - t * 0.6f) + drift) * Time.deltaTime;
            float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
            text.color = new Color(color.r, color.g, color.b, alpha);
            if (age >= life) { gameObject.SetActive(false); if (release != null) release(this); }
        }
    }
}
