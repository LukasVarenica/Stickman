using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>Prefab: sprite + TrailRenderer + Collider2D (Is Trigger) + kinematic Rigidbody2D. Damage is pre-scaled by the boss.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossProjectile : MonoBehaviour
    {
        [SerializeField] float lifetime = 5f;
        [SerializeField] LayerMask groundMask;
        float damage; Vector2 vel;

        public void Init(Vector2 dir, float dmg, float speed)
        {
            vel = dir.normalized * speed; damage = dmg;
            transform.right = new Vector3(vel.x, vel.y, 0f);
            Destroy(gameObject, lifetime);
        }

        void Update() { transform.position += (Vector3)(vel * Time.deltaTime); }

        void OnTriggerEnter2D(Collider2D other)
        {
            var d = other.GetComponentInParent<IDamageable>();
            if (d != null)
            {
                if (!d.IsPlayer) return;
                DamageResolver.EnemyHit(d, damage, DamageKind.Boss, transform.position, gameObject);
                CombatFXManager.Explosion(transform.position, 0.4f);
                Destroy(gameObject);
            }
            else if (((1 << other.gameObject.layer) & groundMask.value) != 0) Destroy(gameObject);
        }
    }
}

