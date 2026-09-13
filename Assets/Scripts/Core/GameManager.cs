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

    /// <summary>true durante captura, salida de sala o viaje entre salas: el jugador no controla y no se puede pausar.</summary>
    public bool IsTransitioning => _isTransitioning;

    const float CaptureSlowMotion = 0.25f;
    const float CaptureSeconds = 1.3f;
    const float FadeSeconds = 0.45f;
    const float IntroSeconds = 1.6f;

    private string _currentRoomScene;
    private string _pendingSpawnId;
    private bool _trackTimer;
    private bool _isTransitioning;
    private System.Func<string, bool> _canLoadScene = Application.CanStreamedLevelBeLoaded;
    private System.Action<string, string> _startTravel;

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
            ShowRoomIntro(SceneManager.GetActiveScene().name);
        }
    }

    void Update()
    {
        if (_trackTimer && !IsPaused && !_isTransitioning)
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
        CancelSequences();
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

    // ---------- viaje explícito entre salas del castillo ----------

    public bool TravelTo(string destinationScene, string destinationEntryId)
    {
        return TravelTo(destinationScene, destinationEntryId, null);
    }

    public bool TravelTo(string destinationScene, string destinationEntryId, GameObject sourceDoor)
    {
        var canLoadScene = _canLoadScene ?? Application.CanStreamedLevelBeLoaded;
        if (!CastleTravelRules.CanStart(_isTransitioning, destinationScene, canLoadScene))
        {
            if (sourceDoor == null)
                Debug.LogError($"Castle travel rejected: '{destinationScene}'.");
            else
                Debug.LogError($"Castle travel rejected by door '{sourceDoor.name}': '{destinationScene}'.", sourceDoor);
            return false;
        }

        if (RoomIdentity.Current != null)
            GameProgress.MarkRoomCompleted(RoomIdentity.Current.RoomId);
        if (_startTravel != null)
            _startTravel(destinationScene, destinationEntryId);
        else
            StartCoroutine(TravelRoutine(destinationScene, destinationEntryId));
        return true;
    }

    IEnumerator TravelRoutine(string sceneName, string entryId)
    {
        _isTransitioning = true;
        _pendingSpawnId = entryId;

        // Cruzar una puerta no es caer por la trampilla: se corta el control y se
        // funde a negro, sin giro ni cartel de "sala superada".
        var feedback = FindPlayerFeedback();
        if (feedback != null) feedback.SetControl(false);
        yield return ScreenTransition.FadeOut(FadeSeconds);

        _currentRoomScene = sceneName;
        RoomTime = 0f;
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return null;                           // la escena nueva ya está activa
        _isTransitioning = false;
    }

    // ---------- captura ----------

    /// <summary>Un guardia ha confirmado que ve al héroe (o ha chocado con él).</summary>
    public void PlayerDetected() => Caught("¡TE ATRAPARON!", "SFX/detected");

    /// <summary>El héroe ha pisado una trampa armada.</summary>
    public void PlayerHitByTrap() => Caught("¡TRAMPA!", "SFX/stone_land");

    /// <summary>
    /// La navegación explícita (nueva partida, reintentar, menú) manda sobre cualquier
    /// captura, salida o viaje que esté a medias: se cortan sus corrutinas y se
    /// restablecen tiempo y bloqueo. Sin esto, una captura iniciada justo antes de
    /// cambiar de sala dejaba el juego "en transición" para siempre y recargaba la
    /// sala equivocada al terminar.
    /// </summary>
    void CancelSequences()
    {
        StopAllCoroutines();
        _isTransitioning = false;
        _pendingSpawnId = null;
        ScreenTransition.HideBanner();
        Time.timeScale = 1f;
    }

    void Caught(string title, string sfx)
    {
        if (_isTransitioning) return;                // dos guardias en el mismo frame: una sola captura
        StartCoroutine(CaptureSequence(title, sfx));
    }

    IEnumerator CaptureSequence(string title, string sfx)
    {
        _isTransitioning = true;
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
        _isTransitioning = false;
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
        if (_isTransitioning) return;
        float completedTime = RoomTime;
        LastWinTime = completedTime;
        LastWinIsBest = GameProgress.TrySetBestTime(CurrentLevel, completedTime);
        if (RoomIdentity.Current != null)
            GameProgress.MarkRoomCompleted(RoomIdentity.Current.RoomId);

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
        if (_isTransitioning) return;
        LastWinTime = RoomTime;
        LastWinIsBest = GameProgress.TrySetBestTime(CurrentLevel, RoomTime);
        if (RoomIdentity.Current != null)
            GameProgress.MarkRoomCompleted(RoomIdentity.Current.RoomId);
        IsWin = true;
        _trackTimer = false;
        StartCoroutine(ExitSequence("GameOver", "¡ESCAPASTE!", ExitSubtitle(LastWinTime, LastWinIsBest), hatch));
    }

    /// <summary>Texto bajo el cartel de salida: tiempo de la sala y si es récord.</summary>
    public static string ExitSubtitle(float seconds, bool isBest) =>
        $"TIEMPO {GameProgress.FormatTime(seconds)}" + (isBest ? " · ¡NUEVO RÉCORD!" : "");

    IEnumerator ExitSequence(string nextScene, string title, string subtitle, Vector3? hatch)
    {
        _isTransitioning = true;
        var feedback = FindPlayerFeedback();
        // El héroe se desliza hasta el centro del hueco mientras desaparece por él.
        if (feedback != null) feedback.PlayEscape(0.5f, hatch);
        ScreenTransition.ShowBanner(title, subtitle, 0.9f);
        yield return new WaitForSecondsRealtime(0.9f);
        yield return ScreenTransition.FadeOut(FadeSeconds);

        if (IsRoomScene(nextScene, out int idx))
        {
            CurrentLevel = idx;
            RoomTime = 0f;
            _currentRoomScene = nextScene;
        }
        ResumeTime();
        SceneManager.LoadScene(nextScene);
        yield return null;
        _isTransitioning = false;
    }

    // ---------- navegación ----------

    public void GoToMainMenu()
    {
        CancelSequences();
        ResumeTime();
        _trackTimer = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartCurrentRoom()
    {
        CancelSequences();
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
        1 => "WASD MOVER · E RECOGER · EVITA EL CONO DEL GUARDIA",
        2 => "F LANZA LA PIEDRA · EL RUIDO ATRAE A LOS GUARDIAS",
        _ => "ENCUENTRA LA SALIDA SIN QUE TE VEAN",
    };

    /// <summary>
    /// Título del cartel de entrada: el nombre del ala del castillo si la escena trae
    /// <see cref="RoomIdentity"/>, y "SALA NN" si no.
    /// </summary>
    public static string RoomIntroTitle(RoomIdentity identity, string sceneName, int level)
    {
        string fallback = $"SALA {level:00}";
        if (identity == null) return fallback;
        string name = identity.ResolvedDisplayName(sceneName);
        return string.Equals(name, sceneName, System.StringComparison.Ordinal)
            ? fallback
            : name.ToUpperInvariant();
    }

    void ShowRoomIntro(string sceneName)
    {
        var identity = RoomIdentity.Current;
        Color? accent = identity != null ? identity.AccentColor : (Color?)null;
        ScreenTransition.ShowBanner(RoomIntroTitle(identity, sceneName, CurrentLevel),
            RoomHint(CurrentLevel), IntroSeconds, titleColor: accent);
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
            ShowRoomIntro(scene.name);
        }
        else
        {
            _trackTimer = false;
        }

        // Toda escena entra fundiendo desde negro (si veníamos de un fundido a negro,
        // esto lo deshace; si no, es un fundido suave desde negro de todas formas).
        ScreenTransition.SetDark();
        ScreenTransition.FadeIn(FadeSeconds);

        var pendingSpawnId = _pendingSpawnId;
        _pendingSpawnId = null;

        // Quien decide si esta escena necesita SpawnPoint es el propio jugador: MainMenu
        // y GameOver no lo traen y tampoco deben exigirlo. Resolver el spawn antes de
        // mirar eso hacia que cada derrota y cada victoria escupieran un error rojo
        // ("No SpawnPoint exists in scene 'GameOver'") en una ruta perfectamente normal.
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var spawn = SpawnPoint.Resolve(
            Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None),
            pendingSpawnId);

        if (spawn == null)
        {
            Debug.LogError($"No SpawnPoint exists in scene '{scene.name}'.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(pendingSpawnId)
            && !string.Equals(spawn.Id, pendingSpawnId, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"Requested SpawnPoint '{pendingSpawnId}' was not found in destination scene '{scene.name}'; using default SpawnPoint '{spawn.Id}'.");
        }

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
