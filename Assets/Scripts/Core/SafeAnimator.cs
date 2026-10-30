using System.Collections.Generic;
using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>Sets Animator parameters only if they exist, so new systems never spam warnings on older controllers.</summary>
    public class SafeAnimator
    {
        readonly Animator a;
        readonly HashSet<int> has = new HashSet<int>();

        public SafeAnimator(Animator animator)
        {
            a = animator;
            if (a != null && a.runtimeAnimatorController != null)
                foreach (var p in a.parameters) has.Add(p.nameHash);
        }
        public void Trigger(string n) { int h = Animator.StringToHash(n); if (has.Contains(h)) a.SetTrigger(h); }
        public void SetInt(string n, int v) { int h = Animator.StringToHash(n); if (has.Contains(h)) a.SetInteger(h, v); }
        public void SetBool(string n, bool v) { int h = Animator.StringToHash(n); if (has.Contains(h)) a.SetBool(h, v); }
        public void SetFloat(string n, float v) { int h = Animator.StringToHash(n); if (has.Contains(h)) a.SetFloat(h, v); }
    }
}
