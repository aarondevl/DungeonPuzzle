using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Lives { get; private set; } = 3;
    public bool IsWin { get; private set; }
    public bool IsPaused { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public float RoomTime { get; private set; }
    public float LastWinTime { get; private set; }
    public bool LastWinIsBest { get; private set; }

    private string _currentRoomScene;
    private bool _trackTimer;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _currentRoomScene = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        if (_trackTimer && !IsPaused)
            RoomTime += Time.unscaledDeltaTime;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    // Después de cargar la escena: así vemos el EventSystem que la escena ya
    // trae y evitamos crear un duplicado (warning "2 event systems").
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();
        Object.DontDestroyOnLoad(esGO);
    }

    public void StartGame() => StartLevel(1);

    public void ContinueGame() => StartLevel(GameProgress.HighestUnlocked);

    public void StartLevel(int level)
    {
        Lives = 3;
        IsWin = false;
        ResumeTime();
        CurrentLevel = Mathf.Clamp(level, 1, GameProgress.TotalLevels);
        RoomTime = 0f;
        _trackTimer = true;
        LoadScene($"Room_{CurrentLevel:00}");
    }

    public void LoadScene(string sceneName)
    {
        _currentRoomScene = sceneName;
        ResumeTime();
        SceneManager.LoadScene(sceneName);
    }

    public void PlayerDetected()
    {
        SfxLibrary.Play("SFX/detected");
        CameraShake.Kick(0.6f);
        DetectionFlash.Flash();
        Lives--;
        GameProgress.RegisterDeath();
        if (Lives <= 0)
        {
            IsWin = false;
            _trackTimer = false;
            SceneManager.LoadScene("GameOver");
        }
        else
        {
            SceneManager.LoadScene(_currentRoomScene);
        }
    }

    public void LoadNextRoom()
    {
        float completedTime = RoomTime;
        LastWinTime = completedTime;
        LastWinIsBest = GameProgress.TrySetBestTime(CurrentLevel, completedTime);

        int next = CurrentLevel + 1;
        if (next > GameProgress.TotalLevels)
        {
            IsWin = true;
            _trackTimer = false;
            SceneManager.LoadScene("GameOver");
            return;
        }

        GameProgress.Unlock(next);
        CurrentLevel = next;
        RoomTime = 0f;
        LoadScene($"Room_{CurrentLevel:00}");
    }

    public void WinGame()
    {
        IsWin = true;
        _trackTimer = false;
        SceneManager.LoadScene("GameOver");
    }

    public void GoToMainMenu()
    {
        ResumeTime();
        _trackTimer = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartCurrentRoom()
    {
        ResumeTime();
        Lives = 3;
        RoomTime = 0f;
        SceneManager.LoadScene(_currentRoomScene);
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeTime();
        else PauseTime();
    }

    public void PauseTime()
    {
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeTime()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResumeTime();
        // detect current level from scene name
        if (scene.name.StartsWith("Room_") && int.TryParse(scene.name.Substring(5), out var idx))
        {
            CurrentLevel = idx;
            _trackTimer = true;
        }
        else
        {
            _trackTimer = false;
        }

        var spawn = Object.FindFirstObjectByType<SpawnPoint>();
        if (spawn == null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
