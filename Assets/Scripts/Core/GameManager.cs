using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Lives { get; private set; } = 3;
    public bool IsWin { get; private set; }

    private string _currentRoomScene;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _currentRoomScene = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
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

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
            Object.DontDestroyOnLoad(esGO);
        }
    }

    public void StartGame()
    {
        Lives = 3;
        IsWin = false;
        LoadScene("Room_01");
    }

    public void LoadScene(string sceneName)
    {
        _currentRoomScene = sceneName;
        SceneManager.LoadScene(sceneName);
    }

    public void PlayerDetected()
    {
        Lives--;
        if (Lives <= 0)
            SceneManager.LoadScene("GameOver");
        else
            SceneManager.LoadScene(_currentRoomScene);
    }

    public void LoadNextRoom()
    {
        string next = _currentRoomScene switch
        {
            "Room_01" => "Room_02",
            "Room_02" => "Room_03",
            "Room_03" => "Room_04",
            "Room_04" => "Room_05",
            _ => "GameOver",
        };
        if (next == "GameOver") IsWin = true;
        LoadScene(next);
    }

    public void WinGame()
    {
        IsWin = true;
        SceneManager.LoadScene("GameOver");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var spawn = Object.FindFirstObjectByType<SpawnPoint>();
        if (spawn == null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
