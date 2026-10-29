using UnityEngine;

// Attach to any GameObject with a Rigidbody2D (Gravity Scale = 0 for top-down).
// Picks a random direction, moves for a random time, then picks a new one.
[RequireComponent(typeof(Rigidbody2D))]
public class RandomWanderer : MonoBehaviour
{
    [Header("Movement")]
    public float minSpeed = 1f;
    public float maxSpeed = 4f;

    [Header("Timing (seconds)")]
    public float minChangeTime = 0.5f;
    public float maxChangeTime = 2.5f;

    [Header("Extras")]
    public bool randomColorOnTurn = true;
    public bool flipSpriteToDirection = true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 velocity;
    private float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        PickNewDirection();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) PickNewDirection();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = velocity; // use rb.velocity on Unity 2022 and older
    }

    void PickNewDirection()
    {
        velocity = Random.insideUnitCircle.normalized * Random.Range(minSpeed, maxSpeed);
        timer = Random.Range(minChangeTime, maxChangeTime);

        if (sr == null) return;
        if (flipSpriteToDirection && Mathf.Abs(velocity.x) > 0.01f)
            sr.flipX = velocity.x < 0f;
        if (randomColorOnTurn)
            sr.color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
    }

    // Bounce off walls by picking a fresh direction
    void OnCollisionEnter2D(Collision2D col)
    {
        PickNewDirection();
    }
}