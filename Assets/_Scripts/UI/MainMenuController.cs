using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Complete main menu system with state machine navigation.
/// Creates all UI programmatically without requiring prefabs.
/// Handles all menu screens, game modes, team selection, and settings.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// Menu navigation state machine.
    /// </summary>
    public enum MenuState
    {
        MainMenu,
        ModeSelect,
        TeamSelection,
        MatchSettings,
        PracticeOptions,
        Settings,
        HowToPlay,
        Credits,
        Loading
    }

    /// <summary>
    /// Available game modes.
    /// </summary>
    public enum GameMode
    {
        QuickMatch,
        PracticeMode,
        SeasonMode,
        Tournament,
        Shootout
    }

    /// <summary>
    /// Practice mode variations.
    /// </summary>
    public enum PracticeType
    {
        FreeSkate,
        ShootingPractice,
        GoaliePractice
    }

    /// <summary>
    /// Match difficulty levels.
    /// </summary>
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when menu state changes.
    /// </summary>
    public static event Action<MenuState> OnMenuStateChanged;

    /// <summary>
    /// Fired when game mode is selected.
    /// </summary>
    public static event Action<GameMode> OnGameModeSelected;

    /// <summary>
    /// Fired when game is ready to start.
    /// </summary>
    public static event Action<MatchConfiguration> OnGameStart;

    #endregion

    #region Serialized Fields

    [Header("Canvas Settings")]
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private int canvasSortOrder = 0;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Animation Settings")]
    [SerializeField] private float fadeTransitionTime = 0.3f;
    [SerializeField] private float buttonHoverScale = 1.05f;

    [Header("Audio Settings")]
    [SerializeField] private bool enableButtonSounds = true;

    #endregion

    #region Private Fields - UI Elements

    private Canvas mainCanvas;
    private CanvasGroup canvasGroup;

    // Screen containers
    private GameObject mainMenuScreen;
    private GameObject modeSelectScreen;
    private GameObject teamSelectionScreen;
    private GameObject matchSettingsScreen;
    private GameObject practiceOptionsScreen;
    private GameObject settingsScreen;
    private GameObject howToPlayScreen;
    private GameObject creditsScreen;
    private GameObject loadingScreen;

    // UI Element references (populated during creation)
    private Dictionary<string, Button> buttons = new Dictionary<string, Button>();
    private Dictionary<string, TextMeshProUGUI> labels = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, Slider> sliders = new Dictionary<string, Slider>();
    private Dictionary<string, TMP_Dropdown> dropdowns = new Dictionary<string, TMP_Dropdown>();
    private Dictionary<string, TMP_InputField> inputFields = new Dictionary<string, TMP_InputField>();
    private Dictionary<string, Toggle> toggles = new Dictionary<string, Toggle>();
    private Dictionary<string, Image> images = new Dictionary<string, Image>();

    #endregion

    #region Private Fields - State

    private MenuState currentState = MenuState.MainMenu;
    private MenuState previousState = MenuState.MainMenu;
    private bool isTransitioning = false;

    // Match configuration
    private MatchConfiguration matchConfig = new MatchConfiguration();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (autoCreateUI)
        {
            CreateAllUI();
        }

        LoadPlayerPreferences();
    }

    private void Start()
    {
        ChangeState(MenuState.MainMenu);
        StartCoroutine(FadeIn());
    }

    private void OnDestroy()
    {
        SavePlayerPreferences();
        ClearEventListeners();
    }

    #endregion

    #region UI Creation - Canvas Setup

    /// <summary>
    /// Create all UI elements programmatically.
    /// </summary>
    private void CreateAllUI()
    {
        CreateCanvas();
        CreateScreens();
        PopulateMainMenuScreen();
        PopulateModeSelectScreen();
        PopulateTeamSelectionScreen();
        PopulateMatchSettingsScreen();
        PopulatePracticeOptionsScreen();
        PopulateSettingsScreen();
        PopulateHowToPlayScreen();
        PopulateCreditsScreen();
        PopulateLoadingScreen();

        Debug.Log("[MainMenuController] All UI created successfully!");
    }

    /// <summary>
    /// Create main canvas and setup.
    /// </summary>
    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Main Menu Canvas");
        canvasObj.transform.SetParent(transform);

        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = canvasSortOrder;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Create all screen containers.
    /// </summary>
    private void CreateScreens()
    {
        mainMenuScreen = CreateScreen("Main Menu Screen");
        modeSelectScreen = CreateScreen("Mode Select Screen");
        teamSelectionScreen = CreateScreen("Team Selection Screen");
        matchSettingsScreen = CreateScreen("Match Settings Screen");
        practiceOptionsScreen = CreateScreen("Practice Options Screen");
        settingsScreen = CreateScreen("Settings Screen");
        howToPlayScreen = CreateScreen("How To Play Screen");
        creditsScreen = CreateScreen("Credits Screen");
        loadingScreen = CreateScreen("Loading Screen");
    }

    /// <summary>
    /// Create a full-screen container.
    /// </summary>
    private GameObject CreateScreen(string name)
    {
        GameObject screen = CreateUIElement(name, mainCanvas.transform);
        RectTransform rect = screen.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        screen.SetActive(false);
        return screen;
    }

    #endregion

    #region UI Creation - Main Menu

    private void PopulateMainMenuScreen()
    {
        // Background
        CreateBackground(mainMenuScreen.transform, new Color(0.05f, 0.05f, 0.15f, 1f));

        // Title
        TextMeshProUGUI title = CreateText("Title", mainMenuScreen.transform, "HOCKEY LEGENDS");
        SetupTitle(title);

        // Button container
        GameObject buttonContainer = CreateUIElement("Button Container", mainMenuScreen.transform);
        SetupButtonContainer(buttonContainer.GetComponent<RectTransform>(), new Vector2(400, 600));

        // Buttons
        CreateMenuButton(buttonContainer.transform, "Play Button", "PLAY", OnPlayClicked, 0);
        CreateMenuButton(buttonContainer.transform, "Settings Button", "SETTINGS", OnSettingsClicked, 1);
        CreateMenuButton(buttonContainer.transform, "How To Play Button", "HOW TO PLAY", OnHowToPlayClicked, 2);
        CreateMenuButton(buttonContainer.transform, "Credits Button", "CREDITS", OnCreditsClicked, 3);
        CreateMenuButton(buttonContainer.transform, "Quit Button", "QUIT", OnQuitClicked, 4);

        // Version text
        TextMeshProUGUI version = CreateText("Version", mainMenuScreen.transform, "v1.0.0");
        SetupVersionText(version);
    }

    #endregion

    #region UI Creation - Mode Select

    private void PopulateModeSelectScreen()
    {
        CreateBackground(modeSelectScreen.transform, new Color(0.05f, 0.1f, 0.15f, 1f));

        // Title
        TextMeshProUGUI title = CreateText("Title", modeSelectScreen.transform, "SELECT GAME MODE");
        SetupTitle(title);

        // Mode buttons grid
        GameObject gridContainer = CreateUIElement("Grid Container", modeSelectScreen.transform);
        SetupGridContainer(gridContainer.GetComponent<RectTransform>(), 2, 3);

        // Game mode buttons
        CreateModeButton(gridContainer.transform, "Quick Match", "QUICK MATCH",
            "Jump right into the action", GameMode.QuickMatch, 0);
        CreateModeButton(gridContainer.transform, "Practice", "PRACTICE MODE",
            "Train without pressure", GameMode.PracticeMode, 1);
        CreateModeButton(gridContainer.transform, "Season", "SEASON MODE",
            "Play multiple games", GameMode.SeasonMode, 2);
        CreateModeButton(gridContainer.transform, "Tournament", "TOURNAMENT",
            "Bracket-style competition", GameMode.Tournament, 3);
        CreateModeButton(gridContainer.transform, "Shootout", "SHOOTOUT",
            "Penalty shots only", GameMode.Shootout, 4);

        // Back button
        CreateBackButton(modeSelectScreen.transform, OnModeSelectBackClicked);
    }

    #endregion

    #region UI Creation - Team Selection

    private void PopulateTeamSelectionScreen()
    {
        CreateBackground(teamSelectionScreen.transform, new Color(0.1f, 0.05f, 0.15f, 1f));

        TextMeshProUGUI title = CreateText("Title", teamSelectionScreen.transform, "TEAM SELECTION");
        SetupTitle(title);

        // Two-column layout
        float columnWidth = 800f;
        float columnSpacing = 100f;

        // Home team (left)
        GameObject homeTeamPanel = CreatePanel(teamSelectionScreen.transform, "Home Team Panel",
            new Vector2(-columnWidth/2 - columnSpacing/2, 0), new Vector2(columnWidth, 800));
        PopulateTeamPanel(homeTeamPanel.transform, "Home", true);

        // Away team (right)
        GameObject awayTeamPanel = CreatePanel(teamSelectionScreen.transform, "Away Team Panel",
            new Vector2(columnWidth/2 + columnSpacing/2, 0), new Vector2(columnWidth, 800));
        PopulateTeamPanel(awayTeamPanel.transform, "Away", false);

        // Player count selection
        GameObject playerCountPanel = CreatePanel(teamSelectionScreen.transform, "Player Count Panel",
            new Vector2(0, -450), new Vector2(600, 150));
        PopulatePlayerCountPanel(playerCountPanel.transform);

        // Continue and Back buttons
        CreateActionButton(teamSelectionScreen.transform, "Continue Button", "CONTINUE",
            OnTeamSelectionContinueClicked, new Vector2(300, -550));
        CreateBackButton(teamSelectionScreen.transform, OnTeamSelectionBackClicked);
    }

    private void PopulateTeamPanel(Transform parent, string prefix, bool isHome)
    {
        // Team name label
        TextMeshProUGUI teamLabel = CreateText($"{prefix} Label", parent, $"{prefix.ToUpper()} TEAM");
        RectTransform labelRect = teamLabel.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, 350);
        labelRect.sizeDelta = new Vector2(700, 60);
        teamLabel.fontSize = 48;
        teamLabel.fontStyle = FontStyles.Bold;
        teamLabel.alignment = TextAlignmentOptions.Center;

        // Team name input
        TMP_InputField nameInput = CreateInputField(parent, $"{prefix} Name Input",
            isHome ? "Home Team" : "Away Team", new Vector2(0, 260), new Vector2(700, 60));
        inputFields[$"{prefix}TeamName"] = nameInput;

        // Color preview
        Image colorPreview = CreateImage(parent, $"{prefix} Color Preview",
            isHome ? Color.blue : Color.red, new Vector2(0, 150), new Vector2(200, 200));
        images[$"{prefix}TeamColor"] = colorPreview;

        // RGB Sliders
        CreateColorSlider(parent, $"{prefix} Red", "R", 0, new Vector2(0, 0), isHome);
        CreateColorSlider(parent, $"{prefix} Green", "G", 1, new Vector2(0, -80), isHome);
        CreateColorSlider(parent, $"{prefix} Blue", "B", 2, new Vector2(0, -160), isHome);
    }

    private void PopulatePlayerCountPanel(Transform parent)
    {
        TextMeshProUGUI label = CreateText("Player Count Label", parent, "PLAYERS PER TEAM");
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, 40);
        labelRect.sizeDelta = new Vector2(500, 40);
        label.fontSize = 32;
        label.alignment = TextAlignmentOptions.Center;

        // Toggle group for player count
        GameObject toggleGroup = CreateUIElement("Toggle Group", parent);
        RectTransform toggleRect = toggleGroup.GetComponent<RectTransform>();
        toggleRect.anchoredPosition = new Vector2(0, -20);
        toggleRect.sizeDelta = new Vector2(500, 60);

        CreatePlayerCountToggle(toggleGroup.transform, "3v3", 0, 3);
        CreatePlayerCountToggle(toggleGroup.transform, "4v4", 1, 4);
        CreatePlayerCountToggle(toggleGroup.transform, "5v5", 2, 5);
    }

    #endregion

    #region UI Creation - Match Settings

    private void PopulateMatchSettingsScreen()
    {
        CreateBackground(matchSettingsScreen.transform, new Color(0.15f, 0.1f, 0.05f, 1f));

        TextMeshProUGUI title = CreateText("Title", matchSettingsScreen.transform, "MATCH SETTINGS");
        SetupTitle(title);

        // Settings panel
        GameObject settingsPanel = CreatePanel(matchSettingsScreen.transform, "Settings Panel",
            new Vector2(0, 0), new Vector2(1000, 700));

        float yPos = 270;
        float spacing = 120;

        // Period length
        CreateSettingRow(settingsPanel.transform, "Period Length", "PERIOD LENGTH:",
            new List<string> { "1 Minute", "3 Minutes", "5 Minutes", "10 Minutes" },
            yPos, "PeriodLength");
        yPos -= spacing;

        // Number of periods
        CreateSettingRow(settingsPanel.transform, "Periods", "NUMBER OF PERIODS:",
            new List<string> { "1 Period", "3 Periods" },
            yPos, "NumPeriods");
        yPos -= spacing;

        // Difficulty
        CreateSettingRow(settingsPanel.transform, "Difficulty", "DIFFICULTY:",
            new List<string> { "Easy", "Normal", "Hard" },
            yPos, "Difficulty");
        yPos -= spacing;

        // Rules toggles
        CreateToggleRow(settingsPanel.transform, "Offsides", "Enable Offsides", yPos, "Offsides");
        yPos -= spacing;

        CreateToggleRow(settingsPanel.transform, "Icing", "Enable Icing", yPos, "Icing");
        yPos -= spacing;

        CreateToggleRow(settingsPanel.transform, "Penalties", "Enable Penalties", yPos, "Penalties");

        // Start and Back buttons
        CreateActionButton(matchSettingsScreen.transform, "Start Button", "START GAME",
            OnStartGameClicked, new Vector2(300, -450));
        CreateBackButton(matchSettingsScreen.transform, OnMatchSettingsBackClicked);
    }

    #endregion

    #region UI Creation - Practice Options

    private void PopulatePracticeOptionsScreen()
    {
        CreateBackground(practiceOptionsScreen.transform, new Color(0.05f, 0.15f, 0.1f, 1f));

        TextMeshProUGUI title = CreateText("Title", practiceOptionsScreen.transform, "PRACTICE MODE");
        SetupTitle(title);

        // Practice mode buttons
        GameObject buttonContainer = CreateUIElement("Button Container", practiceOptionsScreen.transform);
        SetupButtonContainer(buttonContainer.GetComponent<RectTransform>(), new Vector2(500, 500));

        CreatePracticeButton(buttonContainer.transform, "Free Skate", "FREE SKATE",
            "Skate around freely", PracticeType.FreeSkate, 0);
        CreatePracticeButton(buttonContainer.transform, "Shooting Practice", "SHOOTING PRACTICE",
            "Practice your shots", PracticeType.ShootingPractice, 1);
        CreatePracticeButton(buttonContainer.transform, "Goalie Practice", "GOALIE PRACTICE",
            "Practice goaltending", PracticeType.GoaliePractice, 2);

        CreateBackButton(practiceOptionsScreen.transform, OnPracticeOptionsBackClicked);
    }

    #endregion

    #region UI Creation - Settings

    private void PopulateSettingsScreen()
    {
        CreateBackground(settingsScreen.transform, new Color(0.1f, 0.1f, 0.1f, 1f));

        TextMeshProUGUI title = CreateText("Title", settingsScreen.transform, "SETTINGS");
        SetupTitle(title);

        // Settings panel
        GameObject settingsPanel = CreatePanel(settingsScreen.transform, "Settings Panel",
            new Vector2(0, 0), new Vector2(1000, 700));

        float yPos = 250;
        float spacing = 100;

        // Master Volume
        CreateVolumeSlider(settingsPanel.transform, "Master Volume", "MASTER VOLUME",
            yPos, "MasterVolume");
        yPos -= spacing;

        // SFX Volume
        CreateVolumeSlider(settingsPanel.transform, "SFX Volume", "SFX VOLUME",
            yPos, "SFXVolume");
        yPos -= spacing;

        // Music Volume
        CreateVolumeSlider(settingsPanel.transform, "Music Volume", "MUSIC VOLUME",
            yPos, "MusicVolume");
        yPos -= spacing;

        // Fullscreen toggle
        CreateToggleRow(settingsPanel.transform, "Fullscreen", "Fullscreen", yPos, "Fullscreen");
        yPos -= spacing;

        // VSync toggle
        CreateToggleRow(settingsPanel.transform, "VSync", "VSync", yPos, "VSync");

        CreateBackButton(settingsScreen.transform, OnSettingsBackClicked);
    }

    #endregion

    #region UI Creation - How To Play

    private void PopulateHowToPlayScreen()
    {
        CreateBackground(howToPlayScreen.transform, new Color(0.05f, 0.1f, 0.1f, 1f));

        TextMeshProUGUI title = CreateText("Title", howToPlayScreen.transform, "HOW TO PLAY");
        SetupTitle(title);

        // Text panel with scrolling
        GameObject textPanel = CreatePanel(howToPlayScreen.transform, "Text Panel",
            new Vector2(0, -50), new Vector2(1400, 800));

        TextMeshProUGUI instructions = CreateText("Instructions", textPanel.transform, GetHowToPlayText());
        RectTransform instructRect = instructions.GetComponent<RectTransform>();
        instructRect.anchoredPosition = Vector2.zero;
        instructRect.sizeDelta = new Vector2(1300, 750);
        instructions.fontSize = 28;
        instructions.alignment = TextAlignmentOptions.TopLeft;
        instructions.margin = new Vector4(50, 50, 50, 50);

        CreateBackButton(howToPlayScreen.transform, OnHowToPlayBackClicked);
    }

    #endregion

    #region UI Creation - Credits

    private void PopulateCreditsScreen()
    {
        CreateBackground(creditsScreen.transform, new Color(0.1f, 0.05f, 0.1f, 1f));

        TextMeshProUGUI title = CreateText("Title", creditsScreen.transform, "CREDITS");
        SetupTitle(title);

        // Credits text
        GameObject creditsPanel = CreatePanel(creditsScreen.transform, "Credits Panel",
            new Vector2(0, -50), new Vector2(1000, 700));

        TextMeshProUGUI credits = CreateText("Credits Text", creditsPanel.transform, GetCreditsText());
        RectTransform creditsRect = credits.GetComponent<RectTransform>();
        creditsRect.anchoredPosition = Vector2.zero;
        creditsRect.sizeDelta = new Vector2(900, 650);
        credits.fontSize = 32;
        credits.alignment = TextAlignmentOptions.Center;

        CreateBackButton(creditsScreen.transform, OnCreditsBackClicked);
    }

    #endregion

    #region UI Creation - Loading

    private void PopulateLoadingScreen()
    {
        CreateBackground(loadingScreen.transform, new Color(0, 0, 0, 1f));

        TextMeshProUGUI title = CreateText("Loading Title", loadingScreen.transform, "LOADING...");
        SetupTitle(title);

        // Progress bar
        GameObject progressBarBg = CreatePanel(loadingScreen.transform, "Progress Bar BG",
            new Vector2(0, -100), new Vector2(800, 50));
        progressBarBg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject progressBarFill = CreateUIElement("Progress Bar Fill", progressBarBg.transform);
        RectTransform fillRect = progressBarFill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(800, 0);

        Image fillImage = progressBarFill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        images["LoadingProgressFill"] = fillImage;

        // Loading text
        TextMeshProUGUI loadingText = CreateText("Loading Text", loadingScreen.transform, "Preparing the ice...");
        RectTransform loadingRect = loadingText.GetComponent<RectTransform>();
        loadingRect.anchoredPosition = new Vector2(0, -200);
        loadingRect.sizeDelta = new Vector2(800, 60);
        loadingText.fontSize = 36;
        loadingText.alignment = TextAlignmentOptions.Center;
        labels["LoadingText"] = loadingText;
    }

    #endregion

    #region UI Creation - Helper Methods

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        return obj;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text)
    {
        GameObject obj = CreateUIElement(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Image CreateImage(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        GameObject obj = CreateUIElement(name, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private void CreateBackground(Transform parent, Color color)
    {
        GameObject bg = CreateUIElement("Background", parent);
        RectTransform rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image img = bg.AddComponent<Image>();
        img.color = color;
    }

    private GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject panel = CreateUIElement(name, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        return panel;
    }

    private Button CreateButton(Transform parent, string name, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObj = CreateUIElement(name, parent);
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = img;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.9f, 1f);
        colors.pressedColor = new Color(0.1f, 0.3f, 0.7f, 1f);
        colors.selectedColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        button.colors = colors;

        if (callback != null)
        {
            button.onClick.AddListener(callback);
        }

        // Button text
        TextMeshProUGUI buttonText = CreateText($"{name} Text", buttonObj.transform, text);
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        buttonText.fontSize = 32;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.raycastTarget = false;

        buttons[name] = button;
        return button;
    }

    private void CreateMenuButton(Transform parent, string name, string text, UnityEngine.Events.UnityAction callback, int index)
    {
        float yPos = 200 - (index * 100);
        CreateButton(parent, name, text, new Vector2(0, yPos), new Vector2(400, 80), callback);
    }

    private void CreateModeButton(Transform parent, string name, string text, string description, GameMode mode, int index)
    {
        int row = index / 2;
        int col = index % 2;
        float xPos = (col == 0) ? -300 : 300;
        float yPos = 200 - (row * 200);

        GameObject container = CreateUIElement($"{name} Container", parent);
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(xPos, yPos);
        rect.sizeDelta = new Vector2(500, 150);

        Button button = CreateButton(container.transform, name, text, Vector2.zero, new Vector2(500, 100),
            () => OnGameModeClicked(mode));

        // Description text
        TextMeshProUGUI desc = CreateText($"{name} Desc", container.transform, description);
        RectTransform descRect = desc.GetComponent<RectTransform>();
        descRect.anchoredPosition = new Vector2(0, -60);
        descRect.sizeDelta = new Vector2(500, 30);
        desc.fontSize = 20;
        desc.color = new Color(0.8f, 0.8f, 0.8f, 1f);
    }

    private void CreatePracticeButton(Transform parent, string name, string text, string description, PracticeType mode, int index)
    {
        float yPos = 150 - (index * 150);

        GameObject container = CreateUIElement($"{name} Container", parent);
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, yPos);
        rect.sizeDelta = new Vector2(500, 120);

        CreateButton(container.transform, name, text, Vector2.zero, new Vector2(500, 80),
            () => OnPracticeModeClicked(mode));

        TextMeshProUGUI desc = CreateText($"{name} Desc", container.transform, description);
        RectTransform descRect = desc.GetComponent<RectTransform>();
        descRect.anchoredPosition = new Vector2(0, -50);
        descRect.sizeDelta = new Vector2(500, 30);
        desc.fontSize = 20;
        desc.color = new Color(0.8f, 0.8f, 0.8f, 1f);
    }

    private void CreateBackButton(Transform parent, UnityEngine.Events.UnityAction callback)
    {
        CreateButton(parent, "Back Button", "BACK", new Vector2(-700, -450), new Vector2(300, 70), callback);
    }

    private void CreateActionButton(Transform parent, string name, string text, UnityEngine.Events.UnityAction callback, Vector2 position)
    {
        CreateButton(parent, name, text, position, new Vector2(300, 70), callback);
    }

    private TMP_InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 position, Vector2 size)
    {
        GameObject inputObj = CreateUIElement(name, parent);
        RectTransform rect = inputObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image img = inputObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();

        // Text area
        GameObject textArea = CreateUIElement("Text Area", inputObj.transform);
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = new Vector2(-20, -10);

        // Placeholder
        TextMeshProUGUI placeholderText = CreateText("Placeholder", textArea.transform, placeholder);
        placeholderText.fontSize = 28;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.alignment = TextAlignmentOptions.Left;
        RectTransform phRect = placeholderText.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.sizeDelta = Vector2.zero;

        // Input text
        TextMeshProUGUI inputText = CreateText("Text", textArea.transform, "");
        inputText.fontSize = 28;
        inputText.alignment = TextAlignmentOptions.Left;
        RectTransform itRect = inputText.GetComponent<RectTransform>();
        itRect.anchorMin = Vector2.zero;
        itRect.anchorMax = Vector2.one;
        itRect.sizeDelta = Vector2.zero;

        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;

        return inputField;
    }

    private Slider CreateSlider(Transform parent, string name, float minValue, float maxValue, float defaultValue, Vector2 position, Vector2 size)
    {
        GameObject sliderObj = CreateUIElement(name, parent);
        RectTransform rect = sliderObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = defaultValue;

        // Background
        GameObject bg = CreateUIElement("Background", sliderObj.transform);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Fill area
        GameObject fillArea = CreateUIElement("Fill Area", sliderObj.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fill = CreateUIElement("Fill", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        slider.fillRect = fillRect;

        // Handle
        GameObject handleArea = CreateUIElement("Handle Slide Area", sliderObj.transform);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;

        GameObject handle = CreateUIElement("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(30, 30);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private void CreateColorSlider(Transform parent, string name, string label, int colorIndex, Vector2 position, bool isHome)
    {
        GameObject row = CreateUIElement($"{name} Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchoredPosition = position;
        rowRect.sizeDelta = new Vector2(700, 60);

        TextMeshProUGUI labelText = CreateText($"{name} Label", row.transform, $"{label}:");
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-300, 0);
        labelRect.sizeDelta = new Vector2(100, 60);
        labelText.fontSize = 32;
        labelText.alignment = TextAlignmentOptions.Left;

        Slider slider = CreateSlider(row.transform, $"{name} Slider", 0f, 1f,
            colorIndex == 0 ? (isHome ? 0f : 1f) : colorIndex == 2 ? (isHome ? 1f : 0f) : 0f,
            new Vector2(100, 0), new Vector2(450, 40));

        slider.onValueChanged.AddListener((value) => OnTeamColorChanged(isHome));
        sliders[$"{(isHome ? "Home" : "Away")}Color{colorIndex}"] = slider;

        TextMeshProUGUI valueText = CreateText($"{name} Value", row.transform, "0.5");
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchoredPosition = new Vector2(320, 0);
        valueRect.sizeDelta = new Vector2(80, 60);
        valueText.fontSize = 28;
        labels[$"{name}Value"] = valueText;
    }

    private void CreatePlayerCountToggle(Transform parent, string label, int index, int playerCount)
    {
        float xPos = -150 + (index * 150);

        GameObject toggleObj = CreateUIElement($"Toggle {label}", parent);
        RectTransform rect = toggleObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(xPos, 0);
        rect.sizeDelta = new Vector2(120, 60);

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = (playerCount == 4); // Default to 4v4

        // Background
        GameObject bg = CreateUIElement("Background", toggleObj.transform);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        toggle.targetGraphic = bgImage;

        // Label
        TextMeshProUGUI labelText = CreateText("Label", toggleObj.transform, label);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelText.fontSize = 32;
        labelText.fontStyle = FontStyles.Bold;

        ColorBlock colors = toggle.colors;
        colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        colors.selectedColor = new Color(0.2f, 0.6f, 0.2f, 1f);
        toggle.colors = colors;

        toggle.onValueChanged.AddListener((isOn) => {
            if (isOn) OnPlayerCountChanged(playerCount);
        });

        toggles[$"PlayerCount{playerCount}"] = toggle;
    }

    private void CreateSettingRow(Transform parent, string name, string label, List<string> options, float yPos, string key)
    {
        GameObject row = CreateUIElement($"{name} Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchoredPosition = new Vector2(0, yPos);
        rowRect.sizeDelta = new Vector2(900, 80);

        TextMeshProUGUI labelText = CreateText($"{name} Label", row.transform, label);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-250, 0);
        labelRect.sizeDelta = new Vector2(400, 60);
        labelText.fontSize = 28;
        labelText.alignment = TextAlignmentOptions.Left;

        GameObject dropdownObj = CreateUIElement($"{name} Dropdown", row.transform);
        RectTransform dropRect = dropdownObj.GetComponent<RectTransform>();
        dropRect.anchoredPosition = new Vector2(250, 0);
        dropRect.sizeDelta = new Vector2(400, 60);

        Image dropImage = dropdownObj.AddComponent<Image>();
        dropImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(options);

        // Label for dropdown
        TextMeshProUGUI dropLabel = CreateText("Label", dropdownObj.transform, options[0]);
        RectTransform dropLabelRect = dropLabel.GetComponent<RectTransform>();
        dropLabelRect.anchorMin = new Vector2(0, 0);
        dropLabelRect.anchorMax = new Vector2(1, 1);
        dropLabelRect.offsetMin = new Vector2(10, 0);
        dropLabelRect.offsetMax = new Vector2(-30, 0);
        dropLabel.fontSize = 24;
        dropLabel.alignment = TextAlignmentOptions.Left;

        dropdown.captionText = dropLabel;
        dropdowns[key] = dropdown;
    }

    private void CreateToggleRow(Transform parent, string name, string label, float yPos, string key)
    {
        GameObject row = CreateUIElement($"{name} Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchoredPosition = new Vector2(0, yPos);
        rowRect.sizeDelta = new Vector2(900, 60);

        TextMeshProUGUI labelText = CreateText($"{name} Label", row.transform, label);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-200, 0);
        labelRect.sizeDelta = new Vector2(500, 60);
        labelText.fontSize = 28;
        labelText.alignment = TextAlignmentOptions.Left;

        GameObject toggleObj = CreateUIElement($"{name} Toggle", row.transform);
        RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.anchoredPosition = new Vector2(300, 0);
        toggleRect.sizeDelta = new Vector2(60, 60);

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = true;

        GameObject checkmarkBg = CreateUIElement("Checkmark BG", toggleObj.transform);
        RectTransform checkBgRect = checkmarkBg.GetComponent<RectTransform>();
        checkBgRect.anchorMin = Vector2.zero;
        checkBgRect.anchorMax = Vector2.one;
        checkBgRect.sizeDelta = Vector2.zero;
        Image checkBg = checkmarkBg.AddComponent<Image>();
        checkBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        toggle.targetGraphic = checkBg;

        GameObject checkmark = CreateUIElement("Checkmark", toggleObj.transform);
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkRect.sizeDelta = Vector2.zero;
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        toggle.graphic = checkImage;

        toggles[key] = toggle;
    }

    private void CreateVolumeSlider(Transform parent, string name, string label, float yPos, string key)
    {
        GameObject row = CreateUIElement($"{name} Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchoredPosition = new Vector2(0, yPos);
        rowRect.sizeDelta = new Vector2(900, 60);

        TextMeshProUGUI labelText = CreateText($"{name} Label", row.transform, label);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-300, 0);
        labelRect.sizeDelta = new Vector2(200, 60);
        labelText.fontSize = 28;
        labelText.alignment = TextAlignmentOptions.Left;

        Slider slider = CreateSlider(row.transform, $"{name} Slider", 0f, 1f, 1f,
            new Vector2(100, 0), new Vector2(500, 40));

        TextMeshProUGUI valueText = CreateText($"{name} Value", row.transform, "100%");
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchoredPosition = new Vector2(380, 0);
        valueRect.sizeDelta = new Vector2(100, 60);
        valueText.fontSize = 28;
        labels[$"{key}Value"] = valueText;

        slider.onValueChanged.AddListener((value) => {
            labels[$"{key}Value"].text = $"{Mathf.RoundToInt(value * 100)}%";
            OnVolumeChanged(key, value);
        });

        sliders[key] = slider;
    }

    private void SetupTitle(TextMeshProUGUI title)
    {
        RectTransform rect = title.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, 450);
        rect.sizeDelta = new Vector2(1200, 100);
        title.fontSize = 72;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
    }

    private void SetupVersionText(TextMeshProUGUI version)
    {
        RectTransform rect = version.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-20, 20);
        rect.sizeDelta = new Vector2(200, 40);
        version.fontSize = 20;
        version.alignment = TextAlignmentOptions.BottomRight;
        version.color = new Color(0.7f, 0.7f, 0.7f, 1f);
    }

    private void SetupButtonContainer(RectTransform rect, Vector2 size)
    {
        rect.anchoredPosition = new Vector2(0, -50);
        rect.sizeDelta = size;
    }

    private void SetupGridContainer(RectTransform rect, int columns, int rows)
    {
        rect.anchoredPosition = new Vector2(0, -50);
        rect.sizeDelta = new Vector2(1400, 800);
    }

    #endregion

    #region State Management

    /// <summary>
    /// Change menu state with transition.
    /// </summary>
    private void ChangeState(MenuState newState)
    {
        if (isTransitioning || currentState == newState) return;

        previousState = currentState;
        currentState = newState;

        StartCoroutine(TransitionToState(newState));
        OnMenuStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// Transition animation between states.
    /// </summary>
    private IEnumerator TransitionToState(MenuState state)
    {
        isTransitioning = true;

        // Fade out
        yield return StartCoroutine(FadeTransition(false));

        // Hide all screens
        HideAllScreens();

        // Show target screen
        ShowScreen(state);

        // Fade in
        yield return StartCoroutine(FadeTransition(true));

        isTransitioning = false;
    }

    private void HideAllScreens()
    {
        mainMenuScreen.SetActive(false);
        modeSelectScreen.SetActive(false);
        teamSelectionScreen.SetActive(false);
        matchSettingsScreen.SetActive(false);
        practiceOptionsScreen.SetActive(false);
        settingsScreen.SetActive(false);
        howToPlayScreen.SetActive(false);
        creditsScreen.SetActive(false);
        loadingScreen.SetActive(false);
    }

    private void ShowScreen(MenuState state)
    {
        switch (state)
        {
            case MenuState.MainMenu:
                mainMenuScreen.SetActive(true);
                break;
            case MenuState.ModeSelect:
                modeSelectScreen.SetActive(true);
                break;
            case MenuState.TeamSelection:
                teamSelectionScreen.SetActive(true);
                UpdateTeamColorPreviews();
                break;
            case MenuState.MatchSettings:
                matchSettingsScreen.SetActive(true);
                break;
            case MenuState.PracticeOptions:
                practiceOptionsScreen.SetActive(true);
                break;
            case MenuState.Settings:
                settingsScreen.SetActive(true);
                break;
            case MenuState.HowToPlay:
                howToPlayScreen.SetActive(true);
                break;
            case MenuState.Credits:
                creditsScreen.SetActive(true);
                break;
            case MenuState.Loading:
                loadingScreen.SetActive(true);
                break;
        }
    }

    #endregion

    #region Button Callbacks

    private void OnPlayClicked()
    {
        ChangeState(MenuState.ModeSelect);
    }

    private void OnSettingsClicked()
    {
        ChangeState(MenuState.Settings);
    }

    private void OnHowToPlayClicked()
    {
        ChangeState(MenuState.HowToPlay);
    }

    private void OnCreditsClicked()
    {
        ChangeState(MenuState.Credits);
    }

    private void OnQuitClicked()
    {
        SavePlayerPreferences();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void OnGameModeClicked(GameMode mode)
    {
        matchConfig.gameMode = mode;
        OnGameModeSelected?.Invoke(mode);

        if (mode == GameMode.PracticeMode)
        {
            ChangeState(MenuState.PracticeOptions);
        }
        else
        {
            ChangeState(MenuState.TeamSelection);
        }
    }

    private void OnPracticeModeClicked(PracticeType mode)
    {
        matchConfig.practiceMode = mode;
        matchConfig.gameMode = GameMode.PracticeMode;

        // For practice mode, skip team/match settings and load directly
        ApplyMatchConfiguration();
        StartGame();
    }

    private void OnTeamSelectionContinueClicked()
    {
        // Save team selections to match config
        if (inputFields.ContainsKey("HomeTeamName"))
            matchConfig.homeTeamName = inputFields["HomeTeamName"].text;
        if (inputFields.ContainsKey("AwayTeamName"))
            matchConfig.awayTeamName = inputFields["AwayTeamName"].text;

        matchConfig.homeTeamColor = GetHomeTeamColor();
        matchConfig.awayTeamColor = GetAwayTeamColor();

        ChangeState(MenuState.MatchSettings);
    }

    private void OnStartGameClicked()
    {
        ApplyMatchConfiguration();
        StartGame();
    }

    private void OnModeSelectBackClicked()
    {
        ChangeState(MenuState.MainMenu);
    }

    private void OnTeamSelectionBackClicked()
    {
        ChangeState(MenuState.ModeSelect);
    }

    private void OnMatchSettingsBackClicked()
    {
        ChangeState(MenuState.TeamSelection);
    }

    private void OnPracticeOptionsBackClicked()
    {
        ChangeState(MenuState.ModeSelect);
    }

    private void OnSettingsBackClicked()
    {
        SavePlayerPreferences();
        ChangeState(MenuState.MainMenu);
    }

    private void OnHowToPlayBackClicked()
    {
        ChangeState(MenuState.MainMenu);
    }

    private void OnCreditsBackClicked()
    {
        ChangeState(MenuState.MainMenu);
    }

    #endregion

    #region Input Callbacks

    private void OnTeamColorChanged(bool isHome)
    {
        UpdateTeamColorPreviews();
    }

    private void OnPlayerCountChanged(int count)
    {
        matchConfig.playersPerTeam = count;

        // Ensure only one toggle is active
        foreach (var kvp in toggles)
        {
            if (kvp.Key.StartsWith("PlayerCount"))
            {
                int toggleCount = int.Parse(kvp.Key.Replace("PlayerCount", ""));
                kvp.Value.SetIsOnWithoutNotify(toggleCount == count);
            }
        }
    }

    private void OnVolumeChanged(string key, float value)
    {
        // Apply audio settings
        if (key == "MasterVolume")
        {
            AudioListener.volume = value;
        }
        // Can connect to SoundManager here if available
    }

    private void UpdateTeamColorPreviews()
    {
        if (images.ContainsKey("HomeTeamColor"))
        {
            images["HomeTeamColor"].color = GetHomeTeamColor();
        }
        if (images.ContainsKey("AwayTeamColor"))
        {
            images["AwayTeamColor"].color = GetAwayTeamColor();
        }

        // Update value labels
        if (sliders.ContainsKey("HomeColor0") && labels.ContainsKey("Home RedValue"))
            labels["Home RedValue"].text = sliders["HomeColor0"].value.ToString("F2");
        if (sliders.ContainsKey("HomeColor1") && labels.ContainsKey("Home GreenValue"))
            labels["Home GreenValue"].text = sliders["HomeColor1"].value.ToString("F2");
        if (sliders.ContainsKey("HomeColor2") && labels.ContainsKey("Home BlueValue"))
            labels["Home BlueValue"].text = sliders["HomeColor2"].value.ToString("F2");

        if (sliders.ContainsKey("AwayColor0") && labels.ContainsKey("Away RedValue"))
            labels["Away RedValue"].text = sliders["AwayColor0"].value.ToString("F2");
        if (sliders.ContainsKey("AwayColor1") && labels.ContainsKey("Away GreenValue"))
            labels["Away GreenValue"].text = sliders["AwayColor1"].value.ToString("F2");
        if (sliders.ContainsKey("AwayColor2") && labels.ContainsKey("Away BlueValue"))
            labels["Away BlueValue"].text = sliders["AwayColor2"].value.ToString("F2");
    }

    #endregion

    #region Match Configuration

    private void ApplyMatchConfiguration()
    {
        // Period settings
        if (dropdowns.ContainsKey("PeriodLength"))
        {
            string periodText = dropdowns["PeriodLength"].options[dropdowns["PeriodLength"].value].text;
            if (periodText.Contains("1")) matchConfig.periodLengthMinutes = 1;
            else if (periodText.Contains("3")) matchConfig.periodLengthMinutes = 3;
            else if (periodText.Contains("5")) matchConfig.periodLengthMinutes = 5;
            else if (periodText.Contains("10")) matchConfig.periodLengthMinutes = 10;
        }

        if (dropdowns.ContainsKey("NumPeriods"))
        {
            matchConfig.numPeriods = dropdowns["NumPeriods"].value == 0 ? 1 : 3;
        }

        // Difficulty
        if (dropdowns.ContainsKey("Difficulty"))
        {
            matchConfig.difficulty = (Difficulty)dropdowns["Difficulty"].value;
        }

        // Rules
        if (toggles.ContainsKey("Offsides"))
            matchConfig.enableOffsides = toggles["Offsides"].isOn;
        if (toggles.ContainsKey("Icing"))
            matchConfig.enableIcing = toggles["Icing"].isOn;
        if (toggles.ContainsKey("Penalties"))
            matchConfig.enablePenalties = toggles["Penalties"].isOn;

        Debug.Log($"[MainMenuController] Match configured: {matchConfig}");
    }

    private Color GetHomeTeamColor()
    {
        float r = sliders.ContainsKey("HomeColor0") ? sliders["HomeColor0"].value : 0f;
        float g = sliders.ContainsKey("HomeColor1") ? sliders["HomeColor1"].value : 0f;
        float b = sliders.ContainsKey("HomeColor2") ? sliders["HomeColor2"].value : 1f;
        return new Color(r, g, b, 1f);
    }

    private Color GetAwayTeamColor()
    {
        float r = sliders.ContainsKey("AwayColor0") ? sliders["AwayColor0"].value : 1f;
        float g = sliders.ContainsKey("AwayColor1") ? sliders["AwayColor1"].value : 0f;
        float b = sliders.ContainsKey("AwayColor2") ? sliders["AwayColor2"].value : 0f;
        return new Color(r, g, b, 1f);
    }

    #endregion

    #region Game Start

    private void StartGame()
    {
        OnGameStart?.Invoke(matchConfig);
        StartCoroutine(LoadGameScene());
    }

    private IEnumerator LoadGameScene()
    {
        ChangeState(MenuState.Loading);

        // Wait a frame
        yield return null;

        // Start async load
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
        asyncLoad.allowSceneActivation = false;

        // Update progress
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Update progress bar
            if (images.ContainsKey("LoadingProgressFill"))
            {
                RectTransform fillRect = images["LoadingProgressFill"].GetComponent<RectTransform>();
                fillRect.sizeDelta = new Vector2(800 * progress, 0);
            }

            if (labels.ContainsKey("LoadingText"))
            {
                labels["LoadingText"].text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
            }

            // Allow scene activation when ready
            if (asyncLoad.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    #endregion

    #region Player Preferences

    private void SavePlayerPreferences()
    {
        // Save volume settings
        if (sliders.ContainsKey("MasterVolume"))
            PlayerPrefs.SetFloat("MasterVolume", sliders["MasterVolume"].value);
        if (sliders.ContainsKey("SFXVolume"))
            PlayerPrefs.SetFloat("SFXVolume", sliders["SFXVolume"].value);
        if (sliders.ContainsKey("MusicVolume"))
            PlayerPrefs.SetFloat("MusicVolume", sliders["MusicVolume"].value);

        // Save display settings
        if (toggles.ContainsKey("Fullscreen"))
            PlayerPrefs.SetInt("Fullscreen", toggles["Fullscreen"].isOn ? 1 : 0);
        if (toggles.ContainsKey("VSync"))
            PlayerPrefs.SetInt("VSync", toggles["VSync"].isOn ? 1 : 0);

        // Save team colors
        PlayerPrefs.SetFloat("HomeTeamR", GetHomeTeamColor().r);
        PlayerPrefs.SetFloat("HomeTeamG", GetHomeTeamColor().g);
        PlayerPrefs.SetFloat("HomeTeamB", GetHomeTeamColor().b);
        PlayerPrefs.SetFloat("AwayTeamR", GetAwayTeamColor().r);
        PlayerPrefs.SetFloat("AwayTeamG", GetAwayTeamColor().g);
        PlayerPrefs.SetFloat("AwayTeamB", GetAwayTeamColor().b);

        PlayerPrefs.Save();
    }

    private void LoadPlayerPreferences()
    {
        // This will be called after UI is created
    }

    private void ApplyLoadedPreferences()
    {
        // Load and apply volume settings
        if (sliders.ContainsKey("MasterVolume"))
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            sliders["MasterVolume"].value = volume;
            AudioListener.volume = volume;
        }

        if (sliders.ContainsKey("SFXVolume"))
            sliders["SFXVolume"].value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (sliders.ContainsKey("MusicVolume"))
            sliders["MusicVolume"].value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);

        // Load display settings
        if (toggles.ContainsKey("Fullscreen"))
        {
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            toggles["Fullscreen"].isOn = fullscreen;
            Screen.fullScreen = fullscreen;
        }

        if (toggles.ContainsKey("VSync"))
        {
            bool vsync = PlayerPrefs.GetInt("VSync", 1) == 1;
            toggles["VSync"].isOn = vsync;
            QualitySettings.vSyncCount = vsync ? 1 : 0;
        }

        // Load team colors
        if (sliders.ContainsKey("HomeColor0"))
            sliders["HomeColor0"].value = PlayerPrefs.GetFloat("HomeTeamR", 0f);
        if (sliders.ContainsKey("HomeColor1"))
            sliders["HomeColor1"].value = PlayerPrefs.GetFloat("HomeTeamG", 0f);
        if (sliders.ContainsKey("HomeColor2"))
            sliders["HomeColor2"].value = PlayerPrefs.GetFloat("HomeTeamB", 1f);

        if (sliders.ContainsKey("AwayColor0"))
            sliders["AwayColor0"].value = PlayerPrefs.GetFloat("AwayTeamR", 1f);
        if (sliders.ContainsKey("AwayColor1"))
            sliders["AwayColor1"].value = PlayerPrefs.GetFloat("AwayTeamG", 0f);
        if (sliders.ContainsKey("AwayColor2"))
            sliders["AwayColor2"].value = PlayerPrefs.GetFloat("AwayTeamB", 0f);

        UpdateTeamColorPreviews();
    }

    #endregion

    #region Animations

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeTransitionTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeTransitionTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // Apply loaded preferences after fade in
        ApplyLoadedPreferences();
    }

    private IEnumerator FadeTransition(bool fadeIn)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float start = fadeIn ? 0f : 1f;
        float end = fadeIn ? 1f : 0f;

        while (elapsed < fadeTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTransitionTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        canvasGroup.alpha = end;
    }

    #endregion

    #region Text Content

    private string GetHowToPlayText()
    {
        return @"<b><size=48>CONTROLS</size></b>

<b>MOVEMENT</b>
WASD / Arrow Keys - Move player
Left Shift - Sprint (drains stamina)
Space - Jump

<b>PUCK CONTROL</b>
Left Mouse Button - Shoot/Pass
Hold Left Mouse - Charge shot power
Right Mouse Button - Slap shot
E - Body check opponent
Q - Switch controlled player

<b>GOALIE CONTROLS</b>
WASD - Move in crease
Space - Dive save
Left Mouse - Catch/Block puck

<b><size=48>GAME MODES</size></b>

<b>QUICK MATCH</b> - Jump straight into a game
<b>PRACTICE MODE</b> - Train without opponents
<b>SEASON MODE</b> - Play multiple games in a season
<b>TOURNAMENT</b> - Compete in bracket-style tournament
<b>SHOOTOUT</b> - Penalty shot competition

<b><size=48>TIPS</size></b>

• Manage your stamina - sprinting drains it quickly
• Position yourself well for rebounds
• Use body checks strategically to steal the puck
• Time your goalie dives carefully
• Pass to open teammates for better scoring chances";
    }

    private string GetCreditsText()
    {
        return @"<size=64><b>HOCKEY LEGENDS</b></size>

<size=40><b>DEVELOPMENT</b></size>

<size=32>Game Design & Programming
Unity Engine
TextMeshPro

<b>SPECIAL THANKS</b>

Created with Unity
Powered by passion for hockey

<b>VERSION</b>
v1.0.0

© 2025 Hockey Legends</size>";
    }

    #endregion

    #region Utilities

    private void ClearEventListeners()
    {
        OnMenuStateChanged = null;
        OnGameModeSelected = null;
        OnGameStart = null;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get current menu state.
    /// </summary>
    public MenuState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Get current match configuration.
    /// </summary>
    public MatchConfiguration GetMatchConfiguration()
    {
        return matchConfig;
    }

    /// <summary>
    /// Force state change (for external systems).
    /// </summary>
    public void SetState(MenuState state)
    {
        ChangeState(state);
    }

    #endregion
}

/// <summary>
/// Complete match configuration data.
/// Passed to game scene when starting a match.
/// </summary>
[System.Serializable]
public class MatchConfiguration
{
    public MainMenuController.GameMode gameMode = MainMenuController.GameMode.QuickMatch;
    public MainMenuController.PracticeType practiceMode = MainMenuController.PracticeType.FreeSkate;

    public string homeTeamName = "Home Team";
    public string awayTeamName = "Away Team";
    public Color homeTeamColor = Color.blue;
    public Color awayTeamColor = Color.red;

    public int playersPerTeam = 4; // 3v3, 4v4, or 5v5
    public int periodLengthMinutes = 5;
    public int numPeriods = 3;

    public MainMenuController.Difficulty difficulty = MainMenuController.Difficulty.Normal;

    public bool enableOffsides = true;
    public bool enableIcing = true;
    public bool enablePenalties = true;

    public override string ToString()
    {
        return $"Mode: {gameMode}, Teams: {homeTeamName} vs {awayTeamName}, " +
               $"Players: {playersPerTeam}v{playersPerTeam}, Periods: {numPeriods}x{periodLengthMinutes}min, " +
               $"Difficulty: {difficulty}";
    }
}
