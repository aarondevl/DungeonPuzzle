using UnityEngine;

public class AudioMaster : MonoBehaviour
{
    public static AudioMaster Instance { get; private set; }

    AudioSource _sfx;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfx = GetComponent<AudioSource>();
        if (_sfx == null) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f; // 2D: mismo volumen sin importar la posición.

        Apply();
    }

    /// <summary>Reproduce un efecto de sonido puntual (2D), independiente de la escena.</summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip, volume);
    }

    public void Apply()
    {
        AudioListener.volume = GameProgress.MasterVolume;
        Screen.fullScreen = GameProgress.Fullscreen;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("AudioMaster");
        go.AddComponent<AudioMaster>();
    }
}
