using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu inicial: jogar, instruções e (fora do navegador) sair.
public class MainMenu : MonoBehaviour
{
    public const string GameScene = "Game";

    public GameObject mainPanel;
    public GameObject howToPanel;
    public Button playButton;
    public Button howToButton;
    public Button quitButton;
    public Button backButton;
    public TMP_Text bestTimeText;

    void Start()
    {
        Time.timeScale = 1f;
        playButton.onClick.AddListener(() => SceneManager.LoadScene(GameScene));
        howToButton.onClick.AddListener(() => ShowHowTo(true));
        backButton.onClick.AddListener(() => ShowHowTo(false));
        quitButton.onClick.AddListener(Application.Quit);

        // No navegador (WebGL) não existe "sair do jogo"; o recorde sobe para ocupar o lugar do botão.
        bool web = Application.platform == RuntimePlatform.WebGLPlayer;
        quitButton.gameObject.SetActive(!web);
        if (web) bestTimeText.rectTransform.anchoredPosition += new Vector2(0f, 70f);

        float best = GameManager.BestTime;
        bestTimeText.text = best > 0f ? $"Melhor tempo: {GameManager.FormatTime(best)}" : "Ainda sem recorde. Seja o primeiro!";

        ShowHowTo(false);
    }

    void ShowHowTo(bool show)
    {
        mainPanel.SetActive(!show);
        howToPanel.SetActive(show);
        EventSystem.current.SetSelectedGameObject(show ? backButton.gameObject : playButton.gameObject);
    }
}
