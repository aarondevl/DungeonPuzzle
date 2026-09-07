using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI messageText;
    [SerializeField] TextMeshProUGUI subtitleText;
    [SerializeField] TextMeshProUGUI statsText;

    static readonly Color Cream   = new Color(0.910f, 0.875f, 0.784f, 1f);
    static readonly Color Crimson = new Color(0.722f, 0.137f, 0.173f, 1f);

    void Start()
    {
        if (GameManager.Instance == null) return;

        bool win = GameManager.Instance.IsWin;
        if (messageText != null)
        {
            messageText.text  = win ? "ESCAPE\nCOMPLETE" : "ATTEMPT\nFAILED";
            messageText.color = Cream;
        }
        if (subtitleText != null)
        {
            subtitleText.text  = win ? "—   YOU SLIPPED THROUGH THE DUNGEON"
                                     : "—   THE DUNGEON KEEPS YOU FOR ANOTHER TRY";
            subtitleText.color = win ? Cream : Crimson;
        }
        if (statsText != null)
        {
            if (win)
            {
                string best = GameProgress.FormatTime(GameProgress.GetBestTime(GameProgress.TotalLevels));
                string runLast = GameProgress.FormatTime(GameManager.Instance.LastWinTime);
                string newBest = GameManager.Instance.LastWinIsBest ? "  NEW BEST" : "";
                statsText.text = $"FINAL ROOM  {runLast}{newBest}\nBEST  {best}\nDEATHS  {GameProgress.TotalDeaths}";
            }
            else
            {
                statsText.text = $"DEATHS  {GameProgress.TotalDeaths}    UNLOCKED  {GameProgress.HighestUnlocked}/{GameProgress.TotalLevels}";
            }
            statsText.color = Cream;
        }
    }

    void Update()
    {
        // Reintentar rápido con R (backup del botón).
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
            OnRestartClicked();
    }

    public void OnRestartClicked() => GameManager.Instance.StartGame();
    public void OnMainMenuClicked() => GameManager.Instance.GoToMainMenu();
    public void OnQuitClicked()    => Application.Quit();
}
