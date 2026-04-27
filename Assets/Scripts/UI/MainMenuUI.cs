using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnStartClicked()
    {
        Debug.Log("[MainMenu] JUGAR clicked");
        GameManager.Instance.StartGame();
    }

    public void OnQuitClicked()
    {
        Debug.Log("[MainMenu] SALIR clicked");
        Application.Quit();
    }
}
