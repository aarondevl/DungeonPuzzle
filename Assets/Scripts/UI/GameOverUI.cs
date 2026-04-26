using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI messageText;

    void Start()
    {
        if (GameManager.Instance != null)
            messageText.text = GameManager.Instance.IsWin ? "¡Escapaste!" : "Game Over";
    }

    public void OnRestartClicked() => GameManager.Instance.StartGame();
    public void OnQuitClicked() => Application.Quit();
}
