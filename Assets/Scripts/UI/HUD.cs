using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Interface durante a partida: estrelas, pontos, oxigênio, tempo, vidas e energia.
public class HUD : MonoBehaviour
{
    public TMP_Text starsText;
    public TMP_Text scoreText;
    public TMP_Text oxygenText;
    public TMP_Text timeText;
    public TMP_Text messageText;
    public Image oxygenFill;
    public Image staminaFill;
    public Image[] hearts;

    public PlayerHealth playerHealth;
    public PlayerMovement playerMovement;

    public Color oxygenColor = new Color(0.35f, 0.85f, 1f);
    public Color lowOxygenColor = new Color(1f, 0.3f, 0.3f);

    float messageUntil;

    void Start()
    {
        var gm = GameManager.Instance;
        gm.PortalOpened += OnPortalOpened;
        ShowMessage($"Colete as {gm.TotalStars} estrelas antes que o oxigênio acabe!", 4f);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.PortalOpened -= OnPortalOpened;
    }

    void OnPortalOpened()
    {
        ShowMessage("Portal aberto! Voe até o centro!", float.PositiveInfinity);
    }

    void ShowMessage(string text, float duration)
    {
        messageText.text = text;
        messageUntil = Time.time + duration;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        starsText.text = $"{gm.StarsCollected} de {gm.TotalStars}";
        scoreText.text = $"PONTOS  {gm.Score}";
        timeText.text = $"TEMPO  {GameManager.FormatTime(gm.ElapsedTime)}";

        float oxygenRatio = gm.Oxygen / gm.maxOxygen;
        bool low = gm.Oxygen <= gm.lowOxygenWarning;
        oxygenText.text = $"OXIGÊNIO  {Mathf.CeilToInt(gm.Oxygen)}";
        oxygenFill.fillAmount = oxygenRatio;
        Color oxygenNow = low ? Color.Lerp(lowOxygenColor, Color.white, Mathf.PingPong(Time.time * 3f, 0.5f)) : oxygenColor;
        oxygenFill.color = oxygenNow;
        oxygenText.color = low ? lowOxygenColor : Color.white;

        staminaFill.fillAmount = playerMovement.Stamina / playerMovement.maxStamina;
        staminaFill.color = playerMovement.Stamina >= playerMovement.dashCost
            ? new Color(1f, 0.82f, 0.25f)
            : new Color(0.6f, 0.5f, 0.3f);

        for (int i = 0; i < hearts.Length; i++)
            hearts[i].color = i < playerHealth.Lives ? Color.white : new Color(1f, 1f, 1f, 0.18f);

        float alpha = Mathf.Clamp01(messageUntil - Time.time);
        if (gm.PortalOpen) alpha = 0.65f + 0.35f * Mathf.Sin(Time.time * 6f);
        if (!gm.IsPlaying && gm.State != GameState.Paused) alpha = 0f;
        messageText.alpha = alpha;
    }
}
