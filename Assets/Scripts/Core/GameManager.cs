using System.Collections;
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

    /// <summary>true durante captura, salida de sala o fundido: el jugador no controla y no se puede pausar.</summary>
    public bool IsTransitioning { get; private set; }

    const float CaptureSlowMotion = 0.25f;
    const float CaptureSeconds = 1.3f;
    const float FadeSeconds = 0.45f;
    const float IntroSeconds = 1.6f;

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

    void Start()
    {
        // La primera escena ya estaba cargada cuando nos suscribimos: darle también
        // su cartel de entrada (útil al pulsar Play directamente sobre una sala).
        if (IsRoomScene(SceneManager.GetActiveScene().name, out int idx))
        {
            CurrentLevel = idx;
            _trackTimer = true;
            ShowRoomIntro();
        }
    }

    void Update()
    {
        if (_trackTimer && !IsPaused && !IsTransitioning)
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
        // Ninguna escena trae EventSystem: sin este objeto persistente los botones
        // de pausa, game over y menú no reciben clics.
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();
        Object.DontDestroyOnLoad(esGO);
    }

    public void StartGame() => StartLevel(1);

    /// <summary>
    /// "Reintentar" desde Game Over: si perdiste, repite la sala en la que caíste
    /// (antes te mandaba a la sala 1); si ganaste, vuelve a empezar la partida.
    /// </summary>
    public void RetryLevel() => StartLevel(IsWin ? 1 : CurrentLevel);

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

    // ---------- captura ----------

    /// <summary>Un guardia ha confirmado que ve al héroe (o ha chocado con él).</summary>
    public void PlayerDetected() => Caught("¡TE ATRAPARON!", "SFX/detected");

    /// <summary>El héroe ha pisado una trampa armada.</summary>
    public void PlayerHitByTrap() => Caught("¡TRAMPA!", "SFX/stone_land");

    void Caught(string title, string sfx)
    {
        if (IsTransitioning) return;                // dos guardias en el mismo frame: una sola captura
        StartCoroutine(CaptureSequence(title, sfx));
    }

    IEnumerator CaptureSequence(string title, string sfx)
    {
        IsTransitioning = true;
        Lives--;
        GameProgress.RegisterDeath();

        SfxLibrary.Play(sfx, 0.45f);
        CameraShake.Kick(0.6f);
        DetectionFlash.Flash();

        var feedback = FindPlayerFeedback();
        if (feedback != null)
        {
            Vfx.Alert(feedback.transform.position + Vector3.up * 0.6f);
            feedback.PlayCaptured(CaptureSeconds);
        }

        Time.timeScale = CaptureSlowMotion;          // cámara lenta: se ve QUIÉN te atrapó
        ScreenTransition.ShowBanner(title, CaptureSubtitle(Lives), CaptureSeconds - 0.4f, alarm: true);
        yield return new WaitForSecondsRealtime(CaptureSeconds);

        yield return ScreenTransition.FadeOut(FadeSeconds);
        Time.timeScale = 1f;

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
        yield return null;                           // la escena nueva ya está activa
        IsTransitioning = false;
    }

    /// <summary>Texto bajo el cartel de captura según las vidas que quedan.</summary>
    public static string CaptureSubtitle(int livesLeft)
    {
        if (livesLeft <= 0) return "SIN VIDAS · EL CALABOZO TE RETIENE";
        if (livesLeft == 1) return "ÚLTIMA VIDA · VUELVES AL INICIO DE LA SALA";
        return $"TE QUEDAN {livesLeft} VIDAS · VUELVES AL INICIO DE LA SALA";
    }

    // ---------- salida de sala ----------

    public void LoadNextRoom(Vector3? hatch = null)
    {
        if (IsTransitioning) return;
        float completedTime = RoomTime;
        LastWinTime = completedTime;
        LastWinIsBest = GameProgress.TrySetBestTime(CurrentLevel, completedTime);

        int next = CurrentLevel + 1;
        if (next > GameProgress.TotalLevels)
        {
            IsWin = true;
            _trackTimer = false;
            StartCoroutine(ExitSequence("GameOver", "¡ESCAPASTE!", ExitSubtitle(completedTime, LastWinIsBest), hatch));
            return;
        }

        GameProgress.Unlock(next);
        StartCoroutine(ExitSequence($"Room_{next:00}", "SALA SUPERADA", ExitSubtitle(completedTime, LastWinIsBest), hatch));
    }

    public void WinGame(Vector3? hatch = null)
    {
        if (IsTransitioning) return;
        LastWinTime = RoomTime;
        LastWinIsBest = GameProgress.TrySetBestTime(CurrentLevel, RoomTime);
        IsWin = true;
        _trackTimer = false;
        StartCoroutine(ExitSequence("GameOver", "¡ESCAPASTE!", ExitSubtitle(LastWinTime, LastWinIsBest), hatch));
    }

    /// <summary>Texto bajo el cartel de salida: tiempo de la sala y si es récord.</summary>
    public static string ExitSubtitle(float seconds, bool isBest) =>
        $"TIEMPO {GameProgress.FormatTime(seconds)}" + (isBest ? " · ¡NUEVO RÉCORD!" : "");

    IEnumerator ExitSequence(string nextScene, string title, string subtitle, Vector3? hatch)
    {
        IsTransitioning = true;
        var feedback = FindPlayerFeedback();
        Sprite[] walkFrames = feedback != null ? feedback.WalkRightFrames() : null;
        Sprite[] idleFrames = feedback != null ? feedback.IdleDownFrames() : null;
        // El héroe se desliza hasta el centro del hueco mientras desaparece por él.
        if (feedback != null) feedback.PlayEscape(0.5f, hatch);
        ScreenTransition.ShowBanner(title, subtitle, 0.9f);
        yield return new WaitForSecondsRealtime(0.9f);
        yield return ScreenTransition.FadeOut(FadeSeconds);

        // Escapaste de la última sala: secuencia final antes de la pantalla de victoria.
        if (IsWin && nextScene == "GameOver")
        {
            string stats = $"CAPTURAS {GameProgress.TotalDeaths}   ·   ÚLTIMA SALA {GameProgress.FormatTime(LastWinTime)}";
            yield return ScreenTransition.PlayEnding(walkFrames, idleFrames, stats);
        }

        if (nextScene.StartsWith("Room_"))
        {
            CurrentLevel = int.Parse(nextScene.Substring(5));
            RoomTime = 0f;
            _currentRoomScene = nextScene;
        }
        ResumeTime();
        SceneManager.LoadScene(nextScene);
        yield return null;
        IsTransitioning = false;
    }

    // ---------- navegación ----------

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

    static bool IsRoomScene(string sceneName, out int index)
    {
        index = 0;
        return sceneName.StartsWith("Room_") && int.TryParse(sceneName.Substring(5), out index);
    }

    /// <summary>Pista que acompaña al cartel de entrada de cada sala.</summary>
    public static string RoomHint(int level) => level switch
    {
        1 => "LA CELDA · WASD MOVER · ESQUIVA LOS CONOS DE LOS GUARDIAS",
        2 => "LA LLAVE · ESTÁ VIGILADA ARRIBA · ABRE LA REJA DE LA SALIDA",
        3 => "LOS PASILLOS · F LANZA LA PIEDRA · LA PALANCA ABRE LA SALIDA",
        4 => "LA ARMERÍA · LA PLACA DEL FONDO ABRE LA SALA DE LA SALIDA",
        5 => "EL PATIO · LA PLACA ABRE EL CUARTO DE LA LLAVE DEL PORTÓN",
        _ => "ENCUENTRA LA SALIDA SIN QUE TE VEAN",
    };

    void ShowRoomIntro()
    {
        ScreenTransition.ShowBanner($"SALA {CurrentLevel:00}", RoomHint(CurrentLevel), IntroSeconds);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResumeTime();
        EnsureEventSystem();
        // detect current level from scene name
        if (IsRoomScene(scene.name, out var idx))
        {
            CurrentLevel = idx;
            _trackTimer = true;
            ShowRoomIntro();
        }
        else
        {
            _trackTimer = false;
        }

        // Toda escena entra fundiendo desde negro (si veníamos de un fundido a negro,
        // esto lo deshace; si no, es un fundido suave desde negro de todas formas).
        ScreenTransition.SetDark();
        ScreenTransition.FadeIn(FadeSeconds);

        var spawn = Object.FindFirstObjectByType<SpawnPoint>();
        if (spawn == null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    static PlayerFeedback FindPlayerFeedback()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return null;
        return player.GetComponent<PlayerFeedback>() ?? player.AddComponent<PlayerFeedback>();
    }
}
