using System.Collections.Generic;
using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>Listens to CombatEvents and spawns pooled floating numbers. Prevents overlap by nudging numbers apart.</summary>
    public class DamageNumberManager : MonoBehaviour
    {
        [SerializeField] DamageNumber prefab;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color critColor = new Color(1f, 0.65f, 0.1f);
        [SerializeField] Color healColor = new Color(0.3f, 1f, 0.4f);
        [SerializeField] float bossScale = 1.3f, critScale = 1.35f, lifetime = 1.1f, minSpacing = 0.5f;

        readonly Stack<DamageNumber> pool = new Stack<DamageNumber>();
        readonly List<Vector3> recentPos = new List<Vector3>();
        readonly List<float> recentTime = new List<float>();

        void OnEnable() { CombatEvents.Damaged += OnDamaged; CombatEvents.Healed += OnHealed; }
        void OnDisable() { CombatEvents.Damaged -= OnDamaged; CombatEvents.Healed -= OnHealed; }

        void OnDamaged(IDamageable target, DamageInfo info)
        {
            int a = Mathf.RoundToInt(info.amount);
            bool big = target.IsBoss || info.kind == DamageKind.Boss;
            float sc = (big ? bossScale : 1f) * (info.isCrit ? critScale : 1f);
            Vector3 head = target.HeadPosition;
            Spawn(info.isCrit ? "-" + a + "!" : "-" + a, info.isCrit ? critColor : normalColor, sc, head, lifetime);
            if (info.isCrit) Spawn("CRIT!", critColor, 0.55f, head + Vector3.up * 0.55f, lifetime * 0.8f);
        }

        void OnHealed(IDamageable target, int amount, bool lifesteal)
        {
            Spawn("+" + amount, healColor, lifesteal ? 1.05f : 1f, target.HeadPosition, lifetime);
        }

        void Spawn(string s, Color c, float scale, Vector3 pos, float life)
        {
            if (prefab == null) return;
            pos = Resolve(pos);
            var n = pool.Count > 0 ? pool.Pop() : Instantiate(prefab, transform);
            n.Play(s, c, scale, pos, life, x => pool.Push(x));
        }

        Vector3 Resolve(Vector3 p)
        {
            for (int i = recentTime.Count - 1; i >= 0; i--)
                if (Time.time - recentTime[i] > 0.6f) { recentTime.RemoveAt(i); recentPos.RemoveAt(i); }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                bool clash = false;
                foreach (var r in recentPos) if ((r - p).sqrMagnitude < minSpacing * minSpacing) { clash = true; break; }
                if (!clash) break;
                p += new Vector3((attempt % 2 == 0 ? 1f : -1f) * 0.3f, 0.35f, 0f);
            }
            recentPos.Add(p); recentTime.Add(Time.time);
            return p;
        }
    }
}
