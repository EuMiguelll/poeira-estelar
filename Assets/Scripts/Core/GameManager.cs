using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Paused, Won, Lost }

// Controlador estático da partida: tempo, oxigênio, pontuação e fim de jogo.
public class GameManager : MonoBehaviour
{
    public const string BestTimeKey = "PoeiraEstelar.MelhorTempo";
    public const string MenuScene = "MainMenu";

    public static GameManager Instance { get; private set; }

    [Header("Oxigênio (tempo como mecânica)")]
    public float startingOxygen = 45f;
    public float maxOxygen = 60f;
    public float lowOxygenWarning = 10f;

    [Header("Pontuação")]
    public int pointsPerStar = 100;
    public int pointsPerOxygenSecond = 10;
    public int pointsPerLife = 250;

    [Header("Áudio")]
    public AudioSource sfx;
    public AudioClip winClip;
    public AudioClip loseClip;
    public AudioClip portalOpenClip;
    public AudioClip lowOxygenClip;

    [Header("Efeitos")]
    public ParticleSystem burstPrefab;

    public GameState State { get; private set; } = GameState.Playing;
    public int Score { get; private set; }
    public int StarsCollected { get; private set; }
    public int TotalStars { get; private set; }
    public float ElapsedTime { get; private set; }
    public float Oxygen { get; private set; }
    public bool PortalOpen { get; private set; }
    public bool NewRecord { get; private set; }
    public string EndReason { get; private set; }

    public float Progress => TotalStars == 0 ? 1f : (float)StarsCollected / TotalStars;
    public bool IsPlaying => State == GameState.Playing;
    public static float BestTime => PlayerPrefs.GetFloat(BestTimeKey, 0f);

    public event Action<bool> PauseChanged;
    public event Action<bool> GameEnded;
    public event Action PortalOpened;

    Portal portal;
    PlayerHealth player;
    float nextWarningBeep;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Oxygen = startingOxygen;
        TotalStars = GameObject.FindGameObjectsWithTag("Coletavel").Length;
        portal = FindAnyObjectByType<Portal>();
        player = FindAnyObjectByType<PlayerHealth>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Controls.Pause.WasPressedThisFrame() && (State == GameState.Playing || State == GameState.Paused))
            SetPaused(State == GameState.Playing);

        if (State != GameState.Playing) return;

        ElapsedTime += Time.deltaTime;
        Oxygen -= Time.deltaTime;

        if (Oxygen <= lowOxygenWarning && Time.time >= nextWarningBeep)
        {
            sfx.PlayOneShot(lowOxygenClip, 0.7f);
            nextWarningBeep = Time.time + (Oxygen < 5f ? 0.5f : 1f);
        }

        if (Oxygen <= 0f)
        {
            Oxygen = 0f;
            EndGame(false, "Seu oxigênio acabou!");
        }
    }

    public void CollectStar(Vector3 position)
    {
        if (!IsPlaying) return;
        StarsCollected++;
        Score += pointsPerStar;
        SpawnBurst(position, new Color(1f, 0.85f, 0.3f));
        if (StarsCollected >= TotalStars && !PortalOpen) OpenPortal();
    }

    public void AddOxygen(float seconds, Vector3 position)
    {
        Oxygen = Mathf.Min(maxOxygen, Oxygen + seconds);
        SpawnBurst(position, new Color(0.4f, 0.8f, 1f));
    }

    public void SpawnBurst(Vector3 position, Color color)
    {
        if (burstPrefab == null) return;
        var burst = Instantiate(burstPrefab, position, Quaternion.identity);
        var main = burst.main;
        main.startColor = color;
        burst.Play();
    }

    void OpenPortal()
    {
        PortalOpen = true;
        portal.Open();
        sfx.PlayOneShot(portalOpenClip);
        PortalOpened?.Invoke();
    }

    public void PlayerDestroyed() => EndGame(false, "Sua nave foi destruída!");

    public void PlayerReachedPortal() => EndGame(true, "Você escapou pelo portal!");

    void EndGame(bool won, string reason)
    {
        if (State == GameState.Won || State == GameState.Lost) return;

        State = won ? GameState.Won : GameState.Lost;
        EndReason = reason;
        Time.timeScale = 1f;

        if (won)
        {
            Score += Mathf.CeilToInt(Oxygen) * pointsPerOxygenSecond + player.Lives * pointsPerLife;
            if (BestTime <= 0f || ElapsedTime < BestTime)
            {
                PlayerPrefs.SetFloat(BestTimeKey, ElapsedTime);
                PlayerPrefs.Save();
                NewRecord = true;
            }
        }

        sfx.PlayOneShot(won ? winClip : loseClip);
        if (MusicPlayer.Instance != null) MusicPlayer.Instance.SetDucked(true);
        GameEnded?.Invoke(won);
    }

    public void SetPaused(bool paused)
    {
        if (State != GameState.Playing && State != GameState.Paused) return;
        State = paused ? GameState.Paused : GameState.Playing;
        Time.timeScale = paused ? 0f : 1f;
        PauseChanged?.Invoke(paused);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuScene);
    }

    public static string FormatTime(float seconds)
    {
        seconds = Mathf.Floor(Mathf.Max(0f, seconds) * 10f) / 10f;
        int minutes = (int)(seconds / 60f);
        return $"{minutes:00}:{seconds - minutes * 60:00.0}";
    }
}
