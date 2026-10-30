using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>Trigger2D in front of the boss arena: starts the intro when the player walks in.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossTriggerZone : MonoBehaviour
    {
        [SerializeField] BossIntro intro;
        void OnTriggerEnter2D(Collider2D other)
        {
            var d = other.GetComponentInParent<IDamageable>();
            if (d == null || !d.IsPlayer) return;
            intro.Begin();
            gameObject.SetActive(false);
        }
    }
}

