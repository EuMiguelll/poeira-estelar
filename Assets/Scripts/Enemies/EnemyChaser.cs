using UnityEngine;

// Drone que patrulha e passa a perseguir o jogador quando o vê (sem parede no caminho).
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChaser : MonoBehaviour
{
    public Vector2[] patrolPoints;
    public float patrolSpeed = 1.6f;
    public float chaseSpeed = 2.7f;
    public float extraSpeedAtEnd = 1.3f;
    public float detectRadius = 5.5f;
    public float loseRadius = 8f;
    public LayerMask wallMask;

    [Header("Visual")]
    public Transform visual;
    public SpriteRenderer body;
    public SpriteRenderer radiusRing;
    public Color calmColor = Color.white;
    public Color alertColor = new Color(1f, 0.55f, 0.55f);

    public bool IsChasing { get; private set; }

    Rigidbody2D rb;
    Transform player;
    int patrolIndex;
    float stunnedUntil;
    Color ringColor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        if (radiusRing != null)
        {
            ringColor = radiusRing.color;
            radiusRing.transform.localScale = Vector3.one * detectRadius * 2f / transform.lossyScale.x;
        }
    }

    public void Stun(float seconds)
    {
        stunnedUntil = Time.time + seconds;
        rb.linearVelocity = -rb.linearVelocity * 0.5f;
    }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (player == null || (gm != null && !gm.IsPlaying) || Time.time < stunnedUntil)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, 10f * Time.fixedDeltaTime);
            IsChasing = false;
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float range = IsChasing ? loseRadius : detectRadius;
        IsChasing = toPlayer.magnitude < range && !Physics2D.Linecast(rb.position, player.position, wallMask);

        Vector2 target;
        float currentSpeed;
        if (IsChasing)
        {
            target = player.position;
            currentSpeed = chaseSpeed + extraSpeedAtEnd * (gm != null ? gm.Progress : 0f);
        }
        else
        {
            target = patrolPoints.Length > 0 ? patrolPoints[patrolIndex] : rb.position;
            currentSpeed = patrolSpeed;
            if (patrolPoints.Length > 0 && Vector2.Distance(rb.position, target) < 0.25f)
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }

        Vector2 desired = (target - rb.position).normalized * currentSpeed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, 14f * Time.fixedDeltaTime);
    }

    void Update()
    {
        Vector2 v = rb.linearVelocity;
        if (visual != null && v.sqrMagnitude > 0.05f)
        {
            // O sprite do drone aponta para baixo.
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 90f;
            visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.Euler(0f, 0f, angle), 8f * Time.deltaTime);
        }

        if (body != null)
            body.color = Color.Lerp(body.color, IsChasing ? alertColor : calmColor, 8f * Time.deltaTime);

        if (radiusRing != null)
        {
            var target = IsChasing ? new Color(1f, 0.3f, 0.3f, 0.18f) : ringColor;
            radiusRing.color = Color.Lerp(radiusRing.color, target, 6f * Time.deltaTime);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
