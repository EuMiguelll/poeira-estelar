using UnityEngine;

// Meteoro que percorre uma rota fixa de pontos, em loop. Fica mais rápido conforme o jogador avança.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
    public Vector2[] waypoints;
    public float speed = 2.5f;
    public float extraSpeedAtEnd = 1f;
    public float spin = 40f;
    public Transform visual;

    Rigidbody2D rb;
    int index;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (waypoints.Length > 0) rb.position = waypoints[0];
        index = waypoints.Length > 1 ? 1 : 0;
    }

    void Update()
    {
        if (visual != null) visual.Rotate(0f, 0f, spin * Time.deltaTime);
    }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (waypoints.Length < 2 || (gm != null && !gm.IsPlaying)) return;

        float currentSpeed = speed + extraSpeedAtEnd * (gm != null ? gm.Progress : 0f);
        Vector2 target = waypoints[index];
        Vector2 next = Vector2.MoveTowards(rb.position, target, currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if ((next - target).sqrMagnitude < 0.0001f) index = (index + 1) % waypoints.Length;
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
            Gizmos.DrawLine(waypoints[i], waypoints[(i + 1) % waypoints.Length]);
    }
}
