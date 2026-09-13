using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>Pantalla final: derrota (sin vidas) o victoria (escapaste del calabozo).</summary>
public class GameOverUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI messageText;
    [SerializeField] TextMeshProUGUI subtitleText;
    [SerializeField] TextMeshProUGUI statsText;

    static readonly Color Cream   = new Color(0.910f, 0.875f, 0.784f, 1f);
    static readonly Color Crimson = new Color(0.722f, 0.137f, 0.173f, 1f);
    static readonly Color Gold    = new Color(0.95f, 0.80f, 0.35f, 1f);

    void Start()
    {
        if (GameManager.Instance == null) return;

        bool win = GameManager.Instance.IsWin;
        if (messageText != null)
        {
            messageText.text  = win ? "¡ESCAPASTE!" : "TE ATRAPARON";
            messageText.color = win ? Gold : Cream;
        }
        if (subtitleText != null)
        {
            subtitleText.text  = win ? "SALISTE DEL CALABOZO Y VUELVES CON TU FAMILIA"
                                     : $"EL CALABOZO TE RETIENE EN LA SALA {GameManager.Instance.CurrentLevel:00}";
            subtitleText.color = win ? Cream : Crimson;
        }
        if (statsText != null)
        {
            statsText.text = StatsFor(win, GameManager.Instance.LastWinTime, GameManager.Instance.LastWinIsBest,
                GameProgress.GetBestTime(GameProgress.TotalLevels), GameProgress.TotalDeaths,
                GameProgress.HighestUnlocked, GameProgress.TotalLevels);
            statsText.color = Cream;
        }
    }

    /// <summary>Texto de estadísticas. Función pura, cubierta por tests.</summary>
    public static string StatsFor(bool win, float lastTime, bool isBest, float bestFinal, int deaths, int unlocked, int total)
    {
        if (win)
        {
            string record = isBest ? "   ·   ¡NUEVO RÉCORD!" : "";
            return $"ÚLTIMA SALA  {GameProgress.FormatTime(lastTime)}{record}\n" +
                   $"MEJOR TIEMPO  {GameProgress.FormatTime(bestFinal)}\n" +
                   $"CAPTURAS  {deaths}";
        }
        return $"CAPTURAS  {deaths}      SALAS DESBLOQUEADAS  {unlocked}/{total}";
    }

    void Update()
    {
        // Reintentar rápido con R (respaldo del botón).
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
            OnRestartClicked();
    }

    public void OnRestartClicked() => GameManager.Instance.RetryLevel();
    public void OnMainMenuClicked() => GameManager.Instance.GoToMainMenu();
    public void OnQuitClicked()    => Application.Quit();
}
