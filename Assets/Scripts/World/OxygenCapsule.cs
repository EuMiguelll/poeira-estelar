using UnityEngine;

// Cápsula de oxigênio: soma segundos ao tempo restante e reaparece depois de um tempo.
public class OxygenCapsule : MonoBehaviour
{
    public float seconds = 10f;
    public float respawnTime = 15f;
    public GameObject visual;

    Collider2D trigger;
    float respawnAt = -1f;

    void Awake()
    {
        trigger = GetComponent<Collider2D>();
    }

    public bool Collect()
    {
        if (!trigger.enabled) return false;
        trigger.enabled = false;
        visual.SetActive(false);
        respawnAt = Time.time + respawnTime;
        return true;
    }

    void Update()
    {
        if (respawnAt > 0f && Time.time >= respawnAt)
        {
            respawnAt = -1f;
            trigger.enabled = true;
            visual.SetActive(true);
        }
    }
}
