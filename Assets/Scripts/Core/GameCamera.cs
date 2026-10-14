using UnityEngine;

// Mantém a arena inteira visível em qualquer proporção de tela e treme a câmera nos impactos.
[RequireComponent(typeof(Camera))]
public class GameCamera : MonoBehaviour
{
    public Vector2 visibleArea = new Vector2(28.8f, 18f);

    static GameCamera instance;

    Camera cam;
    Vector3 basePosition;
    float shakeUntil;
    float shakeDuration;
    float shakeStrength;

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
#if UNITY_EDITOR
        // No editor o jogo rodaria a centenas de FPS sem necessidade.
        Application.targetFrameRate = 60;
#endif
        basePosition = transform.position;
    }

    public static void Shake(float duration, float strength)
    {
        if (instance == null) return;
        instance.shakeDuration = duration;
        instance.shakeStrength = strength;
        instance.shakeUntil = Time.unscaledTime + duration;
    }

    void LateUpdate()
    {
        cam.orthographicSize = Mathf.Max(visibleArea.y / 2f, visibleArea.x / 2f / cam.aspect);

        float remaining = shakeUntil - Time.unscaledTime;
        if (remaining > 0f)
        {
            float strength = shakeStrength * (remaining / shakeDuration);
            transform.position = basePosition + (Vector3)(Random.insideUnitCircle * strength);
        }
        else
        {
            transform.position = basePosition;
        }
    }
}
