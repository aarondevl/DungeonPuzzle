using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnStartClicked() => GameManager.Instance.StartGame();
    public void OnQuitClicked() => Application.Quit();
}
