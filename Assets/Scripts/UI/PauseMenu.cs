using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        bool show = !(panel != null && panel.activeSelf);
        Show(show);
    }

    public void Show(bool visible)
    {
        if (panel != null) panel.SetActive(visible);
        if (GameManager.Instance == null) return;
        if (visible) GameManager.Instance.PauseTime();
        else GameManager.Instance.ResumeTime();
    }

    public void OnResumeClicked() { SfxLibrary.Play("UI/click"); Show(false); }

    public void OnRestartClicked()
    {
        SfxLibrary.Play("UI/click");
        Show(false);
        GameManager.Instance.RestartCurrentRoom();
    }

    public void OnMainMenuClicked()
    {
        SfxLibrary.Play("UI/click");
        Show(false);
        GameManager.Instance.GoToMainMenu();
    }
}
