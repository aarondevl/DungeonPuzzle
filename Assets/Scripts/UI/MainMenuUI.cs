using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    static readonly Color UnlockedNode = new Color(0.27f, 0.22f, 0.16f, 1f);
    static readonly Color CompletedNode = new Color(0.18f, 0.3f, 0.23f, 1f);
    static readonly Color LockedNode = new Color(0.19f, 0.19f, 0.23f, 1f);
    static readonly Color UnlockedBorder = new Color(0.69f, 0.48f, 0.18f, 1f);
    static readonly Color CompletedBorder = new Color(0.42f, 0.78f, 0.55f, 1f);
    static readonly Color LockedBorder = new Color(0.34f, 0.34f, 0.39f, 1f);
    static readonly Color PrimaryText = new Color(0.93f, 0.89f, 0.78f, 1f);
    static readonly Color LockedText = new Color(0.57f, 0.57f, 0.62f, 1f);

    static readonly string[] LevelTitles =
    {
        "OUTER WATCH",
        "SPIKE PASS",
        "GUARDED HALL",
        "CROSSING",
        "FINAL CRYPT"
    };

    static readonly Vector2[] LevelNodePositions =
    {
        new Vector2(-360f, 95f),
        new Vector2(-180f, -70f),
        new Vector2(0f, 95f),
        new Vector2(180f, -70f),
        new Vector2(360f, 95f)
    };

    [Header("Panels")]
    [SerializeField] GameObject rootPanel;
    [SerializeField] GameObject levelSelectPanel;
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject creditsPanel;
    [SerializeField] GameObject menuTitle;

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
        SetPanel(ResolveMenuTitle(), true);
    }

    void SetPanel(GameObject p, bool v) { if (p != null) p.SetActive(v); }

    GameObject ResolveMenuTitle()
    {
        if (menuTitle != null) return menuTitle;
        Transform title = transform.Find("Title");
        return title != null ? title.gameObject : null;
    }

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

            Vector2 start = LevelNodePositions[i];
            Vector2 end = LevelNodePositions[i + 1];
            Vector2 delta = end - start;
            rect.anchoredPosition = (start + end) * 0.5f;
                rect.sizeDelta = new Vector2(delta.magnitude, 3f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            Image image = connector.GetComponent<Image>();
            image.color = new Color(0.48f, 0.36f, 0.2f, 0.7f);
            image.raycastTarget = false;
            rect.SetAsFirstSibling();
        }

        for (int i = 0; i < levelButtons.Length && i < LevelNodePositions.Length; i++)
        {
            RectTransform node = levelButtons[i].GetComponent<RectTransform>();
            if (node != null)
            {
                node.anchorMin = new Vector2(0.5f, 0.5f);
                node.anchorMax = new Vector2(0.5f, 0.5f);
                node.anchoredPosition = LevelNodePositions[i];
                    node.sizeDelta = new Vector2(160f, 86f);
            }

            if (levelBestTimes != null && i < levelBestTimes.Length && levelBestTimes[i] != null)
            {
                RectTransform time = levelBestTimes[i].rectTransform;
                time.anchorMin = new Vector2(0.5f, 0.5f);
                time.anchorMax = new Vector2(0.5f, 0.5f);
                time.anchoredPosition = LevelNodePositions[i] + new Vector2(0f, -62f);
            }
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

        for (int i = 0; i < 4; i++)
        {
            GameObject connector = levelSelectPanel != null
                ? levelSelectPanel.transform.Find($"MapConnector_{i + 1:00}")?.gameObject
                : null;
            Image image = connector != null ? connector.GetComponent<Image>() : null;
            if (image == null) continue;

            bool completedPath = GameProgress.GetBestTime(i + 1) > 0f;
            bool openPath = GameProgress.IsUnlocked(i + 2);
            image.color = completedPath
                ? new Color(0.3f, 0.58f, 0.42f, 0.82f)
                : openPath
                    ? new Color(0.69f, 0.48f, 0.18f, 0.82f)
                    : new Color(0.3f, 0.3f, 0.34f, 0.65f);
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
                label.color = unlocked ? PrimaryText : LockedText;
                label.fontSize = 18f;
                label.enableAutoSizing = false;
                label.enableWordWrapping = true;
                label.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                label.alignment = TMPro.TextAlignmentOptions.Center;
                label.margin = new Vector4(6f, 4f, 6f, 4f);
        }

            Outline outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            Color borderColor = !unlocked ? LockedBorder : completed ? CompletedBorder : UnlockedBorder;
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(3f, 3f);
            outline.useGraphicAlpha = true;

        ColorBlock colors = button.colors;
        colors.normalColor = nodeColor;
            colors.highlightedColor = unlocked ? Color.Lerp(nodeColor, borderColor, 0.2f) : nodeColor;
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
    public void OnLevelSelectClicked() { SfxLibrary.Play("UI/click"); SetPanel(rootPanel, false); SetPanel(levelSelectPanel, true); SetPanel(ResolveMenuTitle(), false); RefreshLevelButtons(); }
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
