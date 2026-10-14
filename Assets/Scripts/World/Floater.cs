using UnityEngine;

// Animação simples de flutuar, girar e pulsar (estrelas, cápsulas e decoração).
public class Floater : MonoBehaviour
{
    public float bobAmplitude = 0.08f;
    public float bobSpeed = 2.5f;
    public float spinSpeed = 0f;
    public float pulseAmount = 0.06f;
    public float pulseSpeed = 4f;

    Vector3 startPosition;
    Vector3 startScale;
    float phase;

    void Start()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float t = Time.time + phase;
        transform.localPosition = startPosition + Vector3.up * Mathf.Sin(t * bobSpeed) * bobAmplitude;
        transform.localScale = startScale * (1f + Mathf.Sin(t * pulseSpeed) * pulseAmount);
        if (spinSpeed != 0f) transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }
}
