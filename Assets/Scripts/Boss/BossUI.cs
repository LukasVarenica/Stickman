using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanWarriors
{
    /// <summary>Boss health bar with delayed "lag" bar, phase markers at 70%/35% and a phase label that punches on change.</summary>
    public class BossUI : MonoBehaviour
    {
        [SerializeField] BossController boss;
        [SerializeField] GameObject root;
        [SerializeField] Image fill, lagFill;                 // Image Type = Filled, Horizontal
        [SerializeField] TMP_Text nameLabel, phaseLabel;
        [SerializeField] RectTransform marker70, marker35;     // children of the bar; positioned automatically
        [SerializeField] Color[] phaseColors = { Color.white, new Color(1f, 0.7f, 0.3f), new Color(1f, 0.25f, 0.25f) };
        float target = 1f, lag = 1f;

        void Awake() { root.SetActive(false); }

        void Start()
        {
            boss.FightStarted += Show;
            boss.PhaseChanged += OnPhase;
            boss.Health.onHpChanged.AddListener(OnHp);
        }
        void OnDestroy()
        {
            if (boss == null) return;
            boss.FightStarted -= Show; boss.PhaseChanged -= OnPhase;
            if (boss.Health != null) boss.Health.onHpChanged.RemoveListener(OnHp);
        }

        void Show()
        {
            root.SetActive(true);
            nameLabel.text = boss.BossName;
            Anchor(marker70, boss.Phase2At); Anchor(marker35, boss.Phase3At);
            SetPhase(BossPhase.One, false);
        }

        static void Anchor(RectTransform r, float x) { if (r) { r.anchorMin = new Vector2(x, 0f); r.anchorMax = new Vector2(x, 1f); r.anchoredPosition = Vector2.zero; } }
        void OnHp(float cur, float max) { target = max > 0f ? cur / max : 0f; fill.fillAmount = target; }
        void OnPhase(BossPhase p) { SetPhase(p, true); }

        void SetPhase(BossPhase p, bool punch)
        {
            phaseLabel.text = "PHASE " + (int)p;
            phaseLabel.color = phaseColors[Mathf.Clamp((int)p - 1, 0, phaseColors.Length - 1)];
            fill.color = phaseLabel.color;
            if (punch) StartCoroutine(Punch());
        }

        IEnumerator Punch()
        {
            var rt = phaseLabel.rectTransform;
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { rt.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, t / 0.4f); yield return null; }
            rt.localScale = Vector3.one;
        }

        void Update() { lag = Mathf.MoveTowards(lag, target, 0.4f * Time.deltaTime); if (lagFill) lagFill.fillAmount = lag; }
    }
}

