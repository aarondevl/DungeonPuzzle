using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioMaster : MonoBehaviour
{
    public static AudioMaster Instance { get; private set; }

    AudioSource _sfx;
    AudioSource _music;
    AudioSource _nextMusic;
    AudioListener _fallbackListener;
    float _musicBaseVolume = 0.6f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f; // 2D: mismo volumen sin importar la posición.

        _music = gameObject.AddComponent<AudioSource>();
        _music.playOnAwake = false;
        _music.spatialBlend = 0f;
        _music.loop = true;

        _nextMusic = gameObject.AddComponent<AudioSource>();
        _nextMusic.playOnAwake = false;
        _nextMusic.spatialBlend = 0f;
        _nextMusic.loop = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureListener();
        Apply();
        PlayMusic(SfxLibrary.Get("Music/dungeon_ambient"));
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureListener();
        StartCoroutine(ApplyRoomNextFrame());
    }

    IEnumerator ApplyRoomNextFrame()
    {
        yield return null;
        ApplyRoom(RoomIdentity.Current);
    }

    /// <summary>
    /// Garantiza que siempre exista exactamente un AudioListener: si la escena
    /// no trae ninguno (p.ej. cámaras sin listener), añade uno aquí; si la
    /// escena ya trae el suyo, retira el fallback. Todo nuestro audio es 2D,
    /// así que la posición del listener no afecta el volumen.
    /// </summary>
    void EnsureListener()
    {
        bool sceneHasOther = false;
        foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (l.gameObject != gameObject) { sceneHasOther = true; break; }

        if (sceneHasOther && _fallbackListener != null)
        {
            Destroy(_fallbackListener);
            _fallbackListener = null;
        }
        else if (!sceneHasOther && _fallbackListener == null)
        {
            _fallbackListener = gameObject.AddComponent<AudioListener>();
        }
    }

    public void Apply()
    {
        AudioListener.volume = GameProgress.MasterVolume;
        Screen.fullScreen = GameProgress.Fullscreen;
        if (_music != null) _music.volume = _musicBaseVolume * GameProgress.MusicVolume;
    }

    /// <summary>Reproduce un efecto de sonido puntual (2D), independiente de la escena.</summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip, volume * GameProgress.SfxVolume);
    }

    /// <summary>Reproduce música en loop en un canal dedicado, respetando MusicVolume.</summary>
    public void PlayMusic(AudioClip clip, float baseVolume = 0.6f)
    {
        if (clip == null || _music == null) return;
        _musicBaseVolume = baseVolume;
        _music.clip = clip;
        _music.volume = _musicBaseVolume * GameProgress.MusicVolume;
        _music.Play();
    }

    public static bool ShouldSwitchAmbience(AudioClip current, AudioClip next, bool currentPlaying)
    {
        return next != null && (current != next || !currentPlaying);
    }

    public void ApplyRoom(RoomIdentity identity)
    {
        if (identity == null) return;
        AudioClip next = SfxLibrary.Get(identity.ResolvedAmbienceKey);
        if (!ShouldSwitchAmbience(_music.clip, next, _music.isPlaying)) return;
        StopAllCoroutines();
        StartCoroutine(CrossfadeMusic(next, 0.6f));
    }

    IEnumerator CrossfadeMusic(AudioClip next, float seconds)
    {
        _nextMusic.clip = next;
        _nextMusic.loop = true;
        _nextMusic.volume = 0f;
        _nextMusic.Play();
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            _music.volume = (1f - t) * _musicBaseVolume * GameProgress.MusicVolume;
            _nextMusic.volume = t * _musicBaseVolume * GameProgress.MusicVolume;
            yield return null;
        }
        _music.Stop();
        (_music, _nextMusic) = (_nextMusic, _music);
        Apply();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("AudioMaster");
        go.AddComponent<AudioMaster>();
    }
}
