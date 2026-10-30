using UnityEngine;

namespace StickmanWarriors
{
    /// <summary>Small glowing orb (add a TrailRenderer to the prefab) that bursts out then homes in on the Scythe user.</summary>
    public class LifestealOrb : MonoBehaviour
    {
        Transform target; Vector3 vel;

        public void Init(Transform t)
        {
            target = t;
            vel = (Vector3)(Random.insideUnitCircle.normalized * Random.Range(3f, 6f)) + Vector3.up * 2f;
        }

        void Update()
        {
            if (target == null) { Destroy(gameObject); return; }
            Vector3 to = target.position + Vector3.up * 0.8f - transform.position;
            Vector3 desired = to.normalized * Mathf.Max(12f, vel.magnitude * 1.1f);
            vel = Vector3.Lerp(vel, desired, 1f - Mathf.Exp(-6f * Time.deltaTime));
            transform.position += vel * Time.deltaTime;
            if (to.magnitude < 0.4f) Destroy(gameObject);
        }
    }
}
