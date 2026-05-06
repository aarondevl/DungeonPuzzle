using UnityEngine;

public class AudioMaster : MonoBehaviour
{
    public static AudioMaster Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Apply();
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
