using UnityEngine;
using UnityEngine.SceneManagement;

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
