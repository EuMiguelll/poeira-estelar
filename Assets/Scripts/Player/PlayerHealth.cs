using UnityEngine;

// Vidas da nave, invencibilidade temporária após um golpe e efeito de piscar.
[RequireComponent(typeof(PlayerMovement))]
public class PlayerHealth : MonoBehaviour
{
    public int maxLives = 3;
    public float invulnerableTime = 1.5f;
    public float spawnProtection = 1f;
    public float knockbackForce = 9f;

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip hurtClip;

    [Header("Visual")]
    public GameObject visual;
    public SpriteRenderer[] blinkRenderers;

    public int Lives { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    PlayerMovement movement;
    float invulnerableUntil;

    void Start()
    {
        Lives = maxLives;
        movement = GetComponent<PlayerMovement>();
        invulnerableUntil = Time.time + spawnProtection;
    }

    void Update()
    {
        bool visible = !IsInvulnerable || Mathf.Repeat(Time.time * 10f, 1f) > 0.45f;
        foreach (var r in blinkRenderers) r.enabled = visible;
    }

    // Meteoros usam trigger; drones colidem fisicamente.
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Inimigo")) TakeHit(other.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Inimigo")) TakeHit(collision.collider.gameObject);
    }

    void TakeHit(GameObject enemy)
    {
        var gm = GameManager.Instance;
        if (IsInvulnerable || Lives <= 0 || gm == null || !gm.IsPlaying) return;

        Lives--;
        invulnerableUntil = Time.time + invulnerableTime;
        audioSource.PlayOneShot(hurtClip);
        GameCamera.Shake(0.3f, 0.25f);
        gm.SpawnBurst(transform.position, new Color(1f, 0.45f, 0.3f));

        Vector2 away = (Vector2)(transform.position - enemy.transform.position);
        movement.ApplyKnockback((away.sqrMagnitude > 0.001f ? away.normalized : Vector2.up) * knockbackForce);

        var chaser = enemy.GetComponentInParent<EnemyChaser>();
        if (chaser != null) chaser.Stun(1.2f);

        if (Lives <= 0)
        {
            visual.SetActive(false);
            gm.SpawnBurst(transform.position, new Color(0.4f, 0.8f, 1f));
            gm.PlayerDestroyed();
        }
    }
}
