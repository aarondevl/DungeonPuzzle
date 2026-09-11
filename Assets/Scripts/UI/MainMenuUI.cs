using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    static readonly Color UnlockedNode = new Color(0.93f, 0.67f, 0.28f, 1f);
    static readonly Color CompletedNode = new Color(0.38f, 0.78f, 0.55f, 1f);
    static readonly Color LockedNode = new Color(0.22f, 0.22f, 0.27f, 1f);
    static readonly Color LockedText = new Color(0.62f, 0.61f, 0.66f, 1f);

    static readonly string[] LevelTitles =
    {
        "THE OUTER WATCH",
        "THE SPIKE PASSAGE",
        "THE GUARDED HALL",
        "CROSSING PATROL",
        "THE FINAL CRYPT"
    };

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
        BuildLevelPath();
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

    void BuildLevelPath()
    {
        if (levelSelectPanel == null) return;

        for (int i = 0; i < 4; i++)
        {
            GameObject connector = new GameObject($"MapConnector_{i + 1:00}", typeof(RectTransform), typeof(Image));
            connector.transform.SetParent(levelSelectPanel.transform, false);

            RectTransform rect = connector.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-180f, 145f - i * 110f);
            rect.sizeDelta = new Vector2(8f, 72f);

            Image image = connector.GetComponent<Image>();
            image.color = new Color(0.78f, 0.58f, 0.28f, 0.55f);
            image.raycastTarget = false;
            rect.SetAsFirstSibling();
        }
    }

    void RefreshContinueButton()
    {
        if (continueButton == null) return;
        // Room 01 is a valid continuation when no progress exists yet.
        continueButton.interactable = true;
    }

    void RefreshLevelButtons()
    {
        if (levelButtons == null) return;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int level = i + 1;
            bool unlocked = GameProgress.IsUnlocked(level);
            bool completed = GameProgress.GetBestTime(level) > 0f;
            Button button = levelButtons[i];
            button.interactable = unlocked;
            int captured = level;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnLevelClicked(captured));

            ApplyNodeStyle(button, unlocked, completed);

            if (levelBestTimes != null && i < levelBestTimes.Length && levelBestTimes[i] != null)
            {
                if (!unlocked) levelBestTimes[i].text = "LOCKED";
                else
                {
                    float best = GameProgress.GetBestTime(level);
                    levelBestTimes[i].text = best > 0f ? $"BEST  {GameProgress.FormatTime(best)}" : "READY";
                }
                levelBestTimes[i].color = unlocked ? Color.white : LockedText;
            }
        }
    }

    void ApplyNodeStyle(Button button, bool unlocked, bool completed)
    {
        if (button == null) return;

        Color nodeColor = !unlocked ? LockedNode : completed ? CompletedNode : UnlockedNode;
        Image image = button.GetComponent<Image>();
        if (image != null) image.color = nodeColor;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            int index = System.Array.IndexOf(levelButtons, button);
            string title = index >= 0 && index < LevelTitles.Length ? LevelTitles[index] : "DUNGEON ROOM";
            label.text = unlocked ? $"ROOM {index + 1:00}\n{title}" : $"ROOM {index + 1:00}\nLOCKED";
            label.color = unlocked ? Color.white : LockedText;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = nodeColor;
        colors.highlightedColor = unlocked ? Color.Lerp(nodeColor, Color.white, 0.22f) : nodeColor;
        colors.pressedColor = unlocked ? Color.Lerp(nodeColor, Color.black, 0.18f) : nodeColor;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = LockedNode;
        button.colors = colors;
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
    public void OnPlayClicked() { SfxLibrary.Play("UI/click"); GameManager.Instance.StartGame(); }
    public void OnContinueClicked() { SfxLibrary.Play("UI/click"); GameManager.Instance.ContinueGame(); }
    public void OnLevelSelectClicked() { SfxLibrary.Play("UI/click"); SetPanel(rootPanel, false); SetPanel(levelSelectPanel, true); RefreshLevelButtons(); }
    public void OnOptionsClicked() { SfxLibrary.Play("UI/click"); SetPanel(rootPanel, false); SetPanel(optionsPanel, true); }
    public void OnCreditsClicked() { SfxLibrary.Play("UI/click"); SetPanel(rootPanel, false); SetPanel(creditsPanel, true); }
    public void OnQuitClicked() { SfxLibrary.Play("UI/click"); Application.Quit(); }

    // Sub-panel actions
    public void OnBackClicked() { SfxLibrary.Play("UI/click"); ShowRoot(); RefreshContinueButton(); RefreshStats(); }
    void OnLevelClicked(int level) { SfxLibrary.Play("UI/click"); GameManager.Instance.StartLevel(level); }

    public void OnResetProgressClicked()
    {
        SfxLibrary.Play("UI/click");
        GameProgress.ResetAll();
        RefreshContinueButton();
        RefreshLevelButtons();
        RefreshStats();
    }

    void OnMasterVol(float v) { GameProgress.MasterVolume = v; if (AudioMaster.Instance != null) AudioMaster.Instance.Apply(); }
    void OnMusicVol(float v) { GameProgress.MusicVolume = v; if (AudioMaster.Instance != null) AudioMaster.Instance.Apply(); }
    void OnSfxVol(float v) { GameProgress.SfxVolume = v; }
    void OnFullscreen(bool v) { GameProgress.Fullscreen = v; if (AudioMaster.Instance != null) AudioMaster.Instance.Apply(); }
}
