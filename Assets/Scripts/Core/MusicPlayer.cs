using UnityEngine;
using UnityEngine.SceneManagement;

// Música de fundo que continua tocando entre as cenas, mais os sons da interface.
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }

    public AudioSource music;
    public AudioSource ui;
    public float volume = 0.5f;
    public float duckedVolume = 0.18f;

    float targetVolume;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        targetVolume = volume;
        music.volume = volume;
        music.loop = true;
        music.Play();
        SceneManager.sceneLoaded += (_, _) => SetDucked(false);
    }

    public void SetDucked(bool ducked)
    {
        targetVolume = ducked ? duckedVolume : volume;
    }

    public void PlayUi(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null) ui.PlayOneShot(clip, volumeScale);
    }

    void Update()
    {
        music.volume = Mathf.MoveTowards(music.volume, targetVolume, Time.unscaledDeltaTime);
    }
}
