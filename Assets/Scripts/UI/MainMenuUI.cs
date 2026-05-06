using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject rootPanel;
    [SerializeField] GameObject levelSelectPanel;
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject creditsPanel;

    [Header("Continue button (root panel)")]
    [SerializeField] Button continueButton;

    [Header("Level select")]
    [SerializeField] Button[] levelButtons;
    [SerializeField] TextMeshProUGUI[] levelBestTimes;

    [Header("Options")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Toggle fullscreenToggle;

    [Header("Stats display")]
    [SerializeField] TextMeshProUGUI statsLabel;

    void Start()
    {
        ShowRoot();
        RefreshContinueButton();
        RefreshLevelButtons();
        RefreshOptions();
        RefreshStats();
    }

    void ShowRoot()
    {
        SetPanel(rootPanel, true);
        SetPanel(levelSelectPanel, false);
        SetPanel(optionsPanel, false);
        SetPanel(creditsPanel, false);
    }

    void SetPanel(GameObject p, bool v) { if (p != null) p.SetActive(v); }

    void RefreshContinueButton()
    {
        if (continueButton == null) return;
        bool canContinue = GameProgress.HighestUnlocked > 1;
        continueButton.interactable = canContinue;
    }

    void RefreshLevelButtons()
    {
        if (levelButtons == null) return;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int level = i + 1;
            bool unlocked = GameProgress.IsUnlocked(level);
            levelButtons[i].interactable = unlocked;
            int captured = level;
            levelButtons[i].onClick.RemoveAllListeners();
            levelButtons[i].onClick.AddListener(() => OnLevelClicked(captured));

            if (levelBestTimes != null && i < levelBestTimes.Length && levelBestTimes[i] != null)
            {
                if (!unlocked) levelBestTimes[i].text = "LOCKED";
                else
                {
                    float best = GameProgress.GetBestTime(level);
                    levelBestTimes[i].text = best > 0f ? $"BEST  {GameProgress.FormatTime(best)}" : "—";
                }
            }
        }
    }

    void RefreshOptions()
    {
        if (masterVolumeSlider != null) { masterVolumeSlider.SetValueWithoutNotify(GameProgress.MasterVolume); masterVolumeSlider.onValueChanged.AddListener(OnMasterVol); }
        if (musicVolumeSlider != null) { musicVolumeSlider.SetValueWithoutNotify(GameProgress.MusicVolume); musicVolumeSlider.onValueChanged.AddListener(OnMusicVol); }
        if (sfxVolumeSlider != null) { sfxVolumeSlider.SetValueWithoutNotify(GameProgress.SfxVolume); sfxVolumeSlider.onValueChanged.AddListener(OnSfxVol); }
        if (fullscreenToggle != null) { fullscreenToggle.SetIsOnWithoutNotify(GameProgress.Fullscreen); fullscreenToggle.onValueChanged.AddListener(OnFullscreen); }
    }

    void RefreshStats()
    {
        if (statsLabel == null) return;
        statsLabel.text = $"DEATHS  {GameProgress.TotalDeaths}    UNLOCKED  {GameProgress.HighestUnlocked}/{GameProgress.TotalLevels}";
    }

    // Root buttons
    public void OnPlayClicked() => GameManager.Instance.StartGame();
    public void OnContinueClicked() => GameManager.Instance.ContinueGame();
    public void OnLevelSelectClicked() { SetPanel(rootPanel, false); SetPanel(levelSelectPanel, true); RefreshLevelButtons(); }
    public void OnOptionsClicked() { SetPanel(rootPanel, false); SetPanel(optionsPanel, true); }
    public void OnCreditsClicked() { SetPanel(rootPanel, false); SetPanel(creditsPanel, true); }
    public void OnQuitClicked() { Application.Quit(); }

    // Sub-panel actions
    public void OnBackClicked() { ShowRoot(); RefreshContinueButton(); RefreshStats(); }
    void OnLevelClicked(int level) => GameManager.Instance.StartLevel(level);

    public void OnResetProgressClicked()
    {
        GameProgress.ResetAll();
        RefreshContinueButton();
        RefreshLevelButtons();
        RefreshStats();
    }

    void OnMasterVol(float v) { GameProgress.MasterVolume = v; if (AudioMaster.Instance != null) AudioMaster.Instance.Apply(); }
    void OnMusicVol(float v) { GameProgress.MusicVolume = v; }
    void OnSfxVol(float v) { GameProgress.SfxVolume = v; }
    void OnFullscreen(bool v) { GameProgress.Fullscreen = v; if (AudioMaster.Instance != null) AudioMaster.Instance.Apply(); }
}
