using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Complete main menu UI system for hockey game.
/// Handles menu navigation, settings, team selection, and scene transitions.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    #region Menu States
    public enum MenuState
    {
        MainMenu,
        Settings,
        TeamSelect,
        HowToPlay
    }
    #endregion

    #region Serialized Fields - Main Canvas
    [Header("Main Canvas")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    #endregion

    #region Serialized Fields - Panels
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject teamSelectPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject loadingPanel;
    #endregion

    #region Serialized Fields - Main Menu Buttons
    [Header("Main Menu Buttons")]
    [SerializeField] private Button quickMatchButton;
    [SerializeField] private Button rogueliteRunButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button quitButton;
    #endregion

    #region Serialized Fields - Settings Elements
    [Header("Settings Panel Elements")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private TMP_Dropdown teamSizeDropdown;
    [SerializeField] private TMP_Dropdown periodLengthDropdown;
    [SerializeField] private Button settingsBackButton;
    #endregion

    #region Serialized Fields - Team Select Elements
    [Header("Team Select Panel Elements")]
    [SerializeField] private Image homeTeamColorPreview;
    [SerializeField] private Slider homeTeamRedSlider;
    [SerializeField] private Slider homeTeamGreenSlider;
    [SerializeField] private Slider homeTeamBlueSlider;
    [SerializeField] private Image awayTeamColorPreview;
    [SerializeField] private Slider awayTeamRedSlider;
    [SerializeField] private Slider awayTeamGreenSlider;
    [SerializeField] private Slider awayTeamBlueSlider;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button teamSelectBackButton;
    #endregion

    #region Serialized Fields - How To Play Elements
    [Header("How To Play Panel Elements")]
    [SerializeField] private TextMeshProUGUI howToPlayText;
    [SerializeField] private Button howToPlayBackButton;
    #endregion

    #region Serialized Fields - Loading Screen Elements
    [Header("Loading Screen Elements")]
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    #endregion

    #region Serialized Fields - Scene Names
    [Header("Scene Configuration")]
    [SerializeField] private string gameSceneName = "GameScene";
    #endregion

    #region Serialized Fields - Animation Settings
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float panelTransitionDuration = 0.25f;
    #endregion

    #region Private Fields
    private MenuState currentState = MenuState.MainMenu;
    private bool isTransitioning = false;
    private Color homeTeamColor = Color.blue;
    private Color awayTeamColor = Color.red;

    // PlayerPrefs Keys
    private const string PREF_MASTER_VOLUME = "MasterVolume";
    private const string PREF_SFX_VOLUME = "SFXVolume";
    private const string PREF_MUSIC_VOLUME = "MusicVolume";
    private const string PREF_DIFFICULTY = "Difficulty";
    private const string PREF_TEAM_SIZE = "TeamSize";
    private const string PREF_PERIOD_LENGTH = "PeriodLength";
    private const string PREF_PLAYER_NAME = "PlayerName";
    private const string PREF_HOME_TEAM_R = "HomeTeamR";
    private const string PREF_HOME_TEAM_G = "HomeTeamG";
    private const string PREF_HOME_TEAM_B = "HomeTeamB";
    private const string PREF_AWAY_TEAM_R = "AwayTeamR";
    private const string PREF_AWAY_TEAM_G = "AwayTeamG";
    private const string PREF_AWAY_TEAM_B = "AwayTeamB";
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeUI();
        LoadSettings();
        SetupButtonListeners();
    }

    private void Start()
    {
        // Start with fade in animation
        StartCoroutine(FadeIn());
        ShowMainMenu();
    }
    #endregion

    #region Initialization
    private void InitializeUI()
    {
        // Ensure canvas group exists for fading
        if (canvasGroup == null && mainCanvas != null)
        {
            canvasGroup = mainCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = mainCanvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Set initial alpha to 0 for fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        // Setup dropdown options
        SetupDropdowns();

        // Initialize color sliders
        InitializeColorSliders();

        // Set default How To Play text
        SetupHowToPlayText();
    }

    private void SetupDropdowns()
    {
        // Difficulty dropdown
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string> { "Easy", "Normal", "Hard" });
        }

        // Team size dropdown
        if (teamSizeDropdown != null)
        {
            teamSizeDropdown.ClearOptions();
            teamSizeDropdown.AddOptions(new System.Collections.Generic.List<string> { "3v3", "4v4", "5v5" });
        }

        // Period length dropdown
        if (periodLengthDropdown != null)
        {
            periodLengthDropdown.ClearOptions();
            periodLengthDropdown.AddOptions(new System.Collections.Generic.List<string> { "3 Minutes", "5 Minutes", "10 Minutes" });
        }
    }

    private void InitializeColorSliders()
    {
        // Home team color sliders
        if (homeTeamRedSlider != null)
        {
            homeTeamRedSlider.minValue = 0f;
            homeTeamRedSlider.maxValue = 1f;
            homeTeamRedSlider.value = homeTeamColor.r;
        }
        if (homeTeamGreenSlider != null)
        {
            homeTeamGreenSlider.minValue = 0f;
            homeTeamGreenSlider.maxValue = 1f;
            homeTeamGreenSlider.value = homeTeamColor.g;
        }
        if (homeTeamBlueSlider != null)
        {
            homeTeamBlueSlider.minValue = 0f;
            homeTeamBlueSlider.maxValue = 1f;
            homeTeamBlueSlider.value = homeTeamColor.b;
        }

        // Away team color sliders
        if (awayTeamRedSlider != null)
        {
            awayTeamRedSlider.minValue = 0f;
            awayTeamRedSlider.maxValue = 1f;
            awayTeamRedSlider.value = awayTeamColor.r;
        }
        if (awayTeamGreenSlider != null)
        {
            awayTeamGreenSlider.minValue = 0f;
            awayTeamGreenSlider.maxValue = 1f;
            awayTeamGreenSlider.value = awayTeamColor.g;
        }
        if (awayTeamBlueSlider != null)
        {
            awayTeamBlueSlider.minValue = 0f;
            awayTeamBlueSlider.maxValue = 1f;
            awayTeamBlueSlider.value = awayTeamColor.b;
        }

        UpdateColorPreviews();
    }

    private void SetupHowToPlayText()
    {
        if (howToPlayText != null)
        {
            howToPlayText.text = @"<b>HOCKEY CONTROLS</b>

<b>Movement:</b>
WASD or Arrow Keys - Move player
Left Shift - Sprint
Space - Jump

<b>Puck Control:</b>
Left Mouse Button - Shoot/Pass
Right Mouse Button - Slap Shot
E - Check/Hit opponent
Q - Switch controlled player

<b>Goalie Controls:</b>
WASD - Move goalie
Space - Dive save
Left Mouse - Catch puck

<b>Game Objectives:</b>
- Score more goals than the opponent
- Matches are won by the first team to score 3 goals
- In Roguelite mode, collect artifacts to boost your stats
- Survive 3 losses to continue your run

<b>Tips:</b>
- Use sprint sparingly to manage stamina
- Aim your shots carefully for better accuracy
- Check opponents to steal the puck
- Position your goalie well to block shots";
        }
    }

    private void SetupButtonListeners()
    {
        // Main Menu buttons
        if (quickMatchButton != null)
            quickMatchButton.onClick.AddListener(OnQuickMatchClicked);
        if (rogueliteRunButton != null)
            rogueliteRunButton.onClick.AddListener(OnRogueliteRunClicked);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(OnHowToPlayClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // Settings panel
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(OnSettingsBackClicked);

        // Team Select panel
        if (homeTeamRedSlider != null)
            homeTeamRedSlider.onValueChanged.AddListener(OnHomeTeamColorChanged);
        if (homeTeamGreenSlider != null)
            homeTeamGreenSlider.onValueChanged.AddListener(OnHomeTeamColorChanged);
        if (homeTeamBlueSlider != null)
            homeTeamBlueSlider.onValueChanged.AddListener(OnHomeTeamColorChanged);
        if (awayTeamRedSlider != null)
            awayTeamRedSlider.onValueChanged.AddListener(OnAwayTeamColorChanged);
        if (awayTeamGreenSlider != null)
            awayTeamGreenSlider.onValueChanged.AddListener(OnAwayTeamColorChanged);
        if (awayTeamBlueSlider != null)
            awayTeamBlueSlider.onValueChanged.AddListener(OnAwayTeamColorChanged);
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);
        if (teamSelectBackButton != null)
            teamSelectBackButton.onClick.AddListener(OnTeamSelectBackClicked);

        // How To Play panel
        if (howToPlayBackButton != null)
            howToPlayBackButton.onClick.AddListener(OnHowToPlayBackClicked);
    }
    #endregion

    #region Settings Management
    private void LoadSettings()
    {
        // Load volume settings
        float masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
        float sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
        float musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 0.7f);

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfxVolume;
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = musicVolume;

        UpdateVolumeTexts();

        // Load game settings
        int difficulty = PlayerPrefs.GetInt(PREF_DIFFICULTY, 1); // Default: Normal
        int teamSize = PlayerPrefs.GetInt(PREF_TEAM_SIZE, 1); // Default: 4v4
        int periodLength = PlayerPrefs.GetInt(PREF_PERIOD_LENGTH, 1); // Default: 5 minutes

        if (difficultyDropdown != null)
            difficultyDropdown.value = difficulty;
        if (teamSizeDropdown != null)
            teamSizeDropdown.value = teamSize;
        if (periodLengthDropdown != null)
            periodLengthDropdown.value = periodLength;

        // Load player name
        string playerName = PlayerPrefs.GetString(PREF_PLAYER_NAME, "Player");
        if (playerNameInput != null)
            playerNameInput.text = playerName;

        // Load team colors
        homeTeamColor = new Color(
            PlayerPrefs.GetFloat(PREF_HOME_TEAM_R, 0f),
            PlayerPrefs.GetFloat(PREF_HOME_TEAM_G, 0f),
            PlayerPrefs.GetFloat(PREF_HOME_TEAM_B, 1f)
        );

        awayTeamColor = new Color(
            PlayerPrefs.GetFloat(PREF_AWAY_TEAM_R, 1f),
            PlayerPrefs.GetFloat(PREF_AWAY_TEAM_G, 0f),
            PlayerPrefs.GetFloat(PREF_AWAY_TEAM_B, 0f)
        );

        // Update color sliders to match loaded colors
        if (homeTeamRedSlider != null) homeTeamRedSlider.value = homeTeamColor.r;
        if (homeTeamGreenSlider != null) homeTeamGreenSlider.value = homeTeamColor.g;
        if (homeTeamBlueSlider != null) homeTeamBlueSlider.value = homeTeamColor.b;
        if (awayTeamRedSlider != null) awayTeamRedSlider.value = awayTeamColor.r;
        if (awayTeamGreenSlider != null) awayTeamGreenSlider.value = awayTeamColor.g;
        if (awayTeamBlueSlider != null) awayTeamBlueSlider.value = awayTeamColor.b;

        UpdateColorPreviews();

        // Apply audio settings
        ApplyAudioSettings();
    }

    private void SaveSettings()
    {
        // Save volume settings
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterVolumeSlider.value);
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolumeSlider.value);
        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicVolumeSlider.value);

        // Save game settings
        if (difficultyDropdown != null)
            PlayerPrefs.SetInt(PREF_DIFFICULTY, difficultyDropdown.value);
        if (teamSizeDropdown != null)
            PlayerPrefs.SetInt(PREF_TEAM_SIZE, teamSizeDropdown.value);
        if (periodLengthDropdown != null)
            PlayerPrefs.SetInt(PREF_PERIOD_LENGTH, periodLengthDropdown.value);

        // Save player name
        if (playerNameInput != null)
            PlayerPrefs.SetString(PREF_PLAYER_NAME, playerNameInput.text);

        // Save team colors
        PlayerPrefs.SetFloat(PREF_HOME_TEAM_R, homeTeamColor.r);
        PlayerPrefs.SetFloat(PREF_HOME_TEAM_G, homeTeamColor.g);
        PlayerPrefs.SetFloat(PREF_HOME_TEAM_B, homeTeamColor.b);
        PlayerPrefs.SetFloat(PREF_AWAY_TEAM_R, awayTeamColor.r);
        PlayerPrefs.SetFloat(PREF_AWAY_TEAM_G, awayTeamColor.g);
        PlayerPrefs.SetFloat(PREF_AWAY_TEAM_B, awayTeamColor.b);

        PlayerPrefs.Save();
    }

    private void ApplyAudioSettings()
    {
        // Apply volume to AudioListener
        AudioListener.volume = masterVolumeSlider != null ? masterVolumeSlider.value : 1f;

        // If you have an AudioManager, you could set individual volumes:
        // AudioManager.Instance?.SetSFXVolume(sfxVolumeSlider.value);
        // AudioManager.Instance?.SetMusicVolume(musicVolumeSlider.value);
    }

    private void UpdateVolumeTexts()
    {
        if (masterVolumeText != null && masterVolumeSlider != null)
            masterVolumeText.text = Mathf.RoundToInt(masterVolumeSlider.value * 100) + "%";
        if (sfxVolumeText != null && sfxVolumeSlider != null)
            sfxVolumeText.text = Mathf.RoundToInt(sfxVolumeSlider.value * 100) + "%";
        if (musicVolumeText != null && musicVolumeSlider != null)
            musicVolumeText.text = Mathf.RoundToInt(musicVolumeSlider.value * 100) + "%";
    }
    #endregion

    #region Color Management
    private void OnHomeTeamColorChanged(float value)
    {
        if (homeTeamRedSlider != null && homeTeamGreenSlider != null && homeTeamBlueSlider != null)
        {
            homeTeamColor = new Color(
                homeTeamRedSlider.value,
                homeTeamGreenSlider.value,
                homeTeamBlueSlider.value
            );
            UpdateColorPreviews();
        }
    }

    private void OnAwayTeamColorChanged(float value)
    {
        if (awayTeamRedSlider != null && awayTeamGreenSlider != null && awayTeamBlueSlider != null)
        {
            awayTeamColor = new Color(
                awayTeamRedSlider.value,
                awayTeamGreenSlider.value,
                awayTeamBlueSlider.value
            );
            UpdateColorPreviews();
        }
    }

    private void UpdateColorPreviews()
    {
        if (homeTeamColorPreview != null)
            homeTeamColorPreview.color = homeTeamColor;
        if (awayTeamColorPreview != null)
            awayTeamColorPreview.color = awayTeamColor;
    }
    #endregion

    #region Menu Navigation
    private void ShowMainMenu()
    {
        ChangeMenuState(MenuState.MainMenu);
    }

    private void ChangeMenuState(MenuState newState)
    {
        if (isTransitioning) return;

        currentState = newState;
        StartCoroutine(TransitionToPanel(newState));
    }

    private IEnumerator TransitionToPanel(MenuState targetState)
    {
        isTransitioning = true;

        // Fade out current panel
        yield return StartCoroutine(FadeOutCurrentPanel());

        // Hide all panels
        HideAllPanels();

        // Show target panel
        switch (targetState)
        {
            case MenuState.MainMenu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;
            case MenuState.Settings:
                if (settingsPanel != null) settingsPanel.SetActive(true);
                break;
            case MenuState.TeamSelect:
                if (teamSelectPanel != null) teamSelectPanel.SetActive(true);
                break;
            case MenuState.HowToPlay:
                if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
                break;
        }

        // Fade in new panel
        yield return StartCoroutine(FadeInCurrentPanel());

        isTransitioning = false;
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (teamSelectPanel != null) teamSelectPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    #endregion

    #region Button Callbacks - Main Menu
    private void OnQuickMatchClicked()
    {
        Debug.Log("[MainMenuUI] Quick Match selected");
        SaveSettings();
        ChangeMenuState(MenuState.TeamSelect);
    }

    private void OnRogueliteRunClicked()
    {
        Debug.Log("[MainMenuUI] Roguelite Run selected");
        SaveSettings();

        // Start roguelite run through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewRun();
        }
        else
        {
            Debug.LogWarning("[MainMenuUI] GameManager not found! Creating temporary instance.");
            // Create GameManager if it doesn't exist
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewRun();
            }
        }

        // Load game scene
        StartCoroutine(LoadGameSceneAsync());
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[MainMenuUI] Settings selected");
        ChangeMenuState(MenuState.Settings);
    }

    private void OnHowToPlayClicked()
    {
        Debug.Log("[MainMenuUI] How To Play selected");
        ChangeMenuState(MenuState.HowToPlay);
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuUI] Quit selected");
        SaveSettings();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    #endregion

    #region Button Callbacks - Settings
    private void OnMasterVolumeChanged(float value)
    {
        UpdateVolumeTexts();
        ApplyAudioSettings();
    }

    private void OnSFXVolumeChanged(float value)
    {
        UpdateVolumeTexts();
        // Play a test sound effect here if you have an AudioManager
    }

    private void OnMusicVolumeChanged(float value)
    {
        UpdateVolumeTexts();
        // Adjust music volume if you have an AudioManager
    }

    private void OnSettingsBackClicked()
    {
        Debug.Log("[MainMenuUI] Settings back clicked");
        SaveSettings();
        ChangeMenuState(MenuState.MainMenu);
    }
    #endregion

    #region Button Callbacks - Team Select
    private void OnStartGameClicked()
    {
        Debug.Log("[MainMenuUI] Start Game clicked");
        SaveSettings();
        StartCoroutine(LoadGameSceneAsync());
    }

    private void OnTeamSelectBackClicked()
    {
        Debug.Log("[MainMenuUI] Team Select back clicked");
        ChangeMenuState(MenuState.MainMenu);
    }
    #endregion

    #region Button Callbacks - How To Play
    private void OnHowToPlayBackClicked()
    {
        Debug.Log("[MainMenuUI] How To Play back clicked");
        ChangeMenuState(MenuState.MainMenu);
    }
    #endregion

    #region Scene Loading
    private IEnumerator LoadGameSceneAsync()
    {
        Debug.Log($"[MainMenuUI] Loading scene: {gameSceneName}");

        // Show loading panel
        HideAllPanels();
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // Start async load operation
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
        asyncLoad.allowSceneActivation = false;

        // Update progress bar
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (loadingProgressBar != null)
                loadingProgressBar.value = progress;

            if (loadingText != null)
                loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";

            // Check if the load has finished
            if (asyncLoad.progress >= 0.9f)
            {
                // Wait a moment before activating
                yield return new WaitForSeconds(0.5f);

                if (loadingText != null)
                    loadingText.text = "Press any key to continue...";

                // Wait for input or auto-continue after delay
                float timer = 0f;
                while (timer < 2f)
                {
                    if (Input.anyKeyDown)
                        break;
                    timer += Time.deltaTime;
                    yield return null;
                }

                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
    #endregion

    #region Fade Animations
    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 1f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    private IEnumerator FadeOutCurrentPanel()
    {
        // Simple fade effect - you can enhance this with panel-specific animations
        yield return new WaitForSeconds(panelTransitionDuration * 0.5f);
    }

    private IEnumerator FadeInCurrentPanel()
    {
        // Simple fade effect - you can enhance this with panel-specific animations
        yield return new WaitForSeconds(panelTransitionDuration * 0.5f);
    }
    #endregion

    #region Public Accessors
    /// <summary>
    /// Get the currently selected difficulty.
    /// </summary>
    public string GetDifficulty()
    {
        if (difficultyDropdown == null) return "Normal";
        return difficultyDropdown.options[difficultyDropdown.value].text;
    }

    /// <summary>
    /// Get the currently selected team size.
    /// </summary>
    public string GetTeamSize()
    {
        if (teamSizeDropdown == null) return "4v4";
        return teamSizeDropdown.options[teamSizeDropdown.value].text;
    }

    /// <summary>
    /// Get the currently selected period length in minutes.
    /// </summary>
    public int GetPeriodLengthMinutes()
    {
        if (periodLengthDropdown == null) return 5;

        string selected = periodLengthDropdown.options[periodLengthDropdown.value].text;
        if (selected.Contains("3")) return 3;
        if (selected.Contains("5")) return 5;
        if (selected.Contains("10")) return 10;
        return 5;
    }

    /// <summary>
    /// Get the home team color.
    /// </summary>
    public Color GetHomeTeamColor()
    {
        return homeTeamColor;
    }

    /// <summary>
    /// Get the away team color.
    /// </summary>
    public Color GetAwayTeamColor()
    {
        return awayTeamColor;
    }

    /// <summary>
    /// Get the player name.
    /// </summary>
    public string GetPlayerName()
    {
        if (playerNameInput == null) return "Player";
        string name = playerNameInput.text.Trim();
        return string.IsNullOrEmpty(name) ? "Player" : name;
    }
    #endregion
}
