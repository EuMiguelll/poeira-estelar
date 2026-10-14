using UnityEngine;

// Movimento da nave (Rigidbody 2D sem gravidade), impulso com energia e coleta de itens.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    [Header("Impulso (dash)")]
    public float dashSpeed = 13f;
    public float dashDuration = 0.16f;
    public float dashCost = 35f;
    public float maxStamina = 100f;
    public float staminaRegen = 30f;
    public float regenDelay = 0.6f;

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip oxygenClip;
    public AudioClip dashClip;

    [Header("Visual")]
    public Transform visual;
    public Transform flame;
    public TrailRenderer trail;

    public float Stamina { get; private set; }

    Rigidbody2D rb;
    Vector2 movement;
    Vector2 dashDirection;
    Vector2 knockback;
    float dashTimer;
    float lastDashTime = -10f;
    Vector3 flameScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Stamina = maxStamina;
        if (flame != null) flameScale = flame.localScale;
    }

    void Update()
    {
        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;

        movement = playing ? Controls.Move.ReadValue<Vector2>() : Vector2.zero;
        // Movimentação diagonal: o vetor nunca passa de tamanho 1.
        if (movement.sqrMagnitude > 1f) movement.Normalize();

        if (playing && Controls.Dash.WasPressedThisFrame() && Stamina >= dashCost && dashTimer <= 0f)
            StartDash();

        if (Time.time - lastDashTime > regenDelay)
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * Time.deltaTime);

        UpdateVisual();
    }

    void FixedUpdate()
    {
        Vector2 velocity = movement * speed;
        if (dashTimer > 0f)
        {
            velocity = dashDirection * dashSpeed;
            dashTimer -= Time.fixedDeltaTime;
        }

        rb.linearVelocity = velocity + knockback;
        knockback = Vector2.MoveTowards(knockback, Vector2.zero, 45f * Time.fixedDeltaTime);
    }

    void StartDash()
    {
        dashDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : (Vector2)visual.up;
        dashTimer = dashDuration;
        Stamina -= dashCost;
        lastDashTime = Time.time;
        audioSource.PlayOneShot(dashClip, 0.6f);
    }

    public void ApplyKnockback(Vector2 force)
    {
        knockback = force;
        dashTimer = 0f;
    }

    void UpdateVisual()
    {
        if (movement.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg - 90f;
            visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.Euler(0f, 0f, angle), 12f * Time.deltaTime);
        }

        if (flame != null)
        {
            float thrust = dashTimer > 0f ? 1.6f : Mathf.Lerp(0.35f, 1f, movement.magnitude);
            float flicker = 1f + Mathf.Sin(Time.time * 40f) * 0.08f;
            flame.localScale = new Vector3(flameScale.x, flameScale.y * thrust * flicker, flameScale.z);
        }

        if (trail != null) trail.emitting = dashTimer > 0f || movement.sqrMagnitude > 0.04f;
    }

    // Coletáveis usam colisores com "Is Trigger" ligado.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        if (other.CompareTag("Coletavel"))
        {
            audioSource.Play();
            GameManager.Instance.CollectStar(other.transform.position);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Oxigenio"))
        {
            var capsule = other.GetComponent<OxygenCapsule>();
            if (capsule.Collect())
            {
                audioSource.PlayOneShot(oxygenClip);
                GameManager.Instance.AddOxygen(capsule.seconds, other.transform.position);
            }
        }
    }
}
