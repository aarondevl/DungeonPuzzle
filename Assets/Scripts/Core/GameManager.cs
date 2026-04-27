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
    }

    // Allows testing from any scene without going through MainMenu
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
        string next = _currentRoomScene == "Room_01" ? "Room_02" : "GameOver";
        LoadScene(next);
    }

    public void WinGame()
    {
        IsWin = true;
        SceneManager.LoadScene("GameOver");
    }
}
