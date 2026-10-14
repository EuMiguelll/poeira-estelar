using UnityEngine;

// Portal de saída: fica fechado até todas as estrelas serem coletadas.
public class Portal : MonoBehaviour
{
    public SpriteRenderer ring;
    public SpriteRenderer innerRing;
    public SpriteRenderer glow;
    public Collider2D entrance;
    public Color closedColor = new Color(0.6f, 0.6f, 0.75f, 0.35f);
    public Color openColor = new Color(0.45f, 0.95f, 1f, 1f);

    bool isOpen;
    float openedAt;

    void Start()
    {
        entrance.enabled = false;
        ring.color = closedColor;
        innerRing.color = closedColor;
        glow.color = new Color(openColor.r, openColor.g, openColor.b, 0f);
    }

    public void Open()
    {
        isOpen = true;
        openedAt = Time.time;
        entrance.enabled = true;
    }

    void Update()
    {
        ring.transform.Rotate(0f, 0f, (isOpen ? 160f : 25f) * Time.deltaTime);
        innerRing.transform.Rotate(0f, 0f, (isOpen ? -240f : -35f) * Time.deltaTime);

        if (!isOpen) return;

        float t = Mathf.Clamp01((Time.time - openedAt) * 2f);
        float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.06f;
        ring.color = Color.Lerp(closedColor, openColor, t);
        innerRing.color = Color.Lerp(closedColor, Color.white, t);
        glow.color = new Color(openColor.r, openColor.g, openColor.b, 0.55f * t);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, t) * pulse;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen && other.CompareTag("Player")) GameManager.Instance.PlayerReachedPortal();
    }
}
