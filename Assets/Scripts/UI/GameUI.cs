using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menus dentro da partida: pausa e tela de fim (vitória ou derrota). Sempre há uma saída.
public class GameUI : MonoBehaviour
{
    [Header("Pausa")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button pauseRestartButton;
    public Button pauseMenuButton;

    [Header("Fim de jogo")]
    public GameObject endPanel;
    public TMP_Text endTitle;
    public TMP_Text endReason;
    public TMP_Text endStats;
    public Button playAgainButton;
    public Button endMenuButton;
    public float endPanelDelay = 1.2f;

    public Color winColor = new Color(0.45f, 0.95f, 1f);
    public Color loseColor = new Color(1f, 0.4f, 0.4f);

    void Start()
    {
        pausePanel.SetActive(false);
        endPanel.SetActive(false);

        var gm = GameManager.Instance;
        resumeButton.onClick.AddListener(() => gm.SetPaused(false));
        pauseRestartButton.onClick.AddListener(gm.Restart);
        pauseMenuButton.onClick.AddListener(gm.GoToMenu);
        playAgainButton.onClick.AddListener(gm.Restart);
        endMenuButton.onClick.AddListener(gm.GoToMenu);

        gm.PauseChanged += OnPauseChanged;
        gm.GameEnded += OnGameEnded;
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.PauseChanged -= OnPauseChanged;
        gm.GameEnded -= OnGameEnded;
    }

    void OnPauseChanged(bool paused)
    {
        pausePanel.SetActive(paused);
        EventSystem.current.SetSelectedGameObject(paused ? resumeButton.gameObject : null);
    }

    void OnGameEnded(bool won)
    {
        pausePanel.SetActive(false);
        StartCoroutine(ShowEndPanel(won));
    }

    IEnumerator ShowEndPanel(bool won)
    {
        var gm = GameManager.Instance;
        endTitle.text = won ? "MISSÃO CUMPRIDA!" : "FIM DE JOGO";
        endTitle.color = won ? winColor : loseColor;
        endReason.text = gm.EndReason;

        string best = GameManager.BestTime > 0f ? GameManager.FormatTime(GameManager.BestTime) : "--:--";
        string record = gm.NewRecord ? "<color=#FFD54A>NOVO RECORDE!</color>\n" : "";
        endStats.text =
            $"Tempo final: <b>{GameManager.FormatTime(gm.ElapsedTime)}</b>\n{record}" +
            $"Estrelas: <b>{gm.StarsCollected} de {gm.TotalStars}</b>\n" +
            $"Pontuação: <b>{gm.Score}</b>\n" +
            $"Melhor tempo: <b>{best}</b>";

        // Espera um pouco para o jogador ver o que aconteceu e não apertar um botão sem querer.
        yield return new WaitForSecondsRealtime(endPanelDelay);
        endPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(playAgainButton.gameObject);
    }
}
