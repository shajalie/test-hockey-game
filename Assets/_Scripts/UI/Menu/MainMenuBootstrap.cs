using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstrap for the new arcade-styled menu system.
/// Add this to a GameObject in your main menu scene to initialize the menu.
/// </summary>
public class MainMenuBootstrap : MonoBehaviour
{
    #region Serialized Fields

    [Header("Menu System")]
    [SerializeField] private bool useNewMenuSystem = true;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    #endregion

    #region Private Fields

    private MenuManager menuManager;
    private bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (!useNewMenuSystem)
        {
            Debug.Log("[MainMenuBootstrap] New menu system disabled - using legacy system");
            enabled = false;
            return;
        }

        // Ensure we're the only bootstrap
        MainMenuBootstrap[] bootstraps = FindObjectsOfType<MainMenuBootstrap>();
        if (bootstraps.Length > 1)
        {
            Debug.LogWarning("[MainMenuBootstrap] Multiple bootstraps found - destroying duplicate");
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    private void Start()
    {
        // Subscribe to game events
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        string debugText = "[Menu System Debug]\n";
        debugText += $"Initialized: {isInitialized}\n";
        debugText += $"MenuManager: {(menuManager != null ? "OK" : "NULL")}\n";
        debugText += $"Current Screen: {menuManager?.CurrentScreen?.ScreenName ?? "None"}\n";
        debugText += $"GameManager: {(GameManager.Instance != null ? "OK" : "NULL")}\n";
        debugText += $"Run Active: {GameManager.Instance?.IsRunActive ?? false}\n";

        GUI.Box(new Rect(10, 10, 250, 120), debugText, style);
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        Debug.Log("[MainMenuBootstrap] Initializing arcade menu system...");

        // Ensure GameManager exists
        EnsureGameManager();

        // Create MenuManager
        CreateMenuManager();

        isInitialized = true;
        Debug.Log("[MainMenuBootstrap] Arcade menu system ready!");
    }

    private void EnsureGameManager()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log("[MainMenuBootstrap] Creating GameManager...");
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }
    }

    private void CreateMenuManager()
    {
        GameObject menuObj = new GameObject("MenuManager");
        menuObj.transform.SetParent(transform);
        menuManager = menuObj.AddComponent<MenuManager>();

        Debug.Log("[MainMenuBootstrap] MenuManager created");
    }

    #endregion

    #region Event Handlers

    private void SubscribeToEvents()
    {
        GameEvents.OnDraftStarted += OnDraftStarted;
        GameEvents.OnMatchEnd += OnMatchEnd;

        if (menuManager != null)
        {
            menuManager.OnNavigationStackEmpty += OnNavigationStackEmpty;
        }
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnDraftStarted -= OnDraftStarted;
        GameEvents.OnMatchEnd -= OnMatchEnd;

        if (menuManager != null)
        {
            menuManager.OnNavigationStackEmpty -= OnNavigationStackEmpty;
        }
    }

    private void OnDraftStarted()
    {
        Debug.Log("[MainMenuBootstrap] Draft started - showing draft screen");
        menuManager?.ShowDraftScreen();
    }

    private void OnMatchEnd()
    {
        Debug.Log("[MainMenuBootstrap] Match ended");

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // Check if run is over
        if (!gm.IsRunActive)
        {
            menuManager?.ShowGameOverScreen();
        }
        // Otherwise draft screen will be shown via OnDraftStarted
    }

    private void OnNavigationStackEmpty()
    {
        Debug.Log("[MainMenuBootstrap] Navigation stack empty - could show quit confirmation");
        // Could show a quit confirmation dialog here
    }

    #endregion

    #region Public API

    /// <summary>
    /// Load the game scene.
    /// </summary>
    public void LoadGameScene()
    {
        Debug.Log($"[MainMenuBootstrap] Loading game scene: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Return to main menu scene.
    /// </summary>
    public void LoadMainMenuScene()
    {
        Debug.Log($"[MainMenuBootstrap] Loading main menu scene: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Show pause menu (can be called from gameplay).
    /// </summary>
    public void ShowPauseMenu()
    {
        menuManager?.ShowPauseMenu();
    }

    /// <summary>
    /// Hide pause menu and resume gameplay.
    /// </summary>
    public void HidePauseMenu()
    {
        menuManager?.NavigateBack();
    }

    /// <summary>
    /// Toggle pause menu.
    /// </summary>
    public void TogglePauseMenu()
    {
        if (menuManager?.CurrentScreen is PauseScreen)
        {
            HidePauseMenu();
        }
        else
        {
            ShowPauseMenu();
        }
    }

    #endregion

    #region Static Access

    private static MainMenuBootstrap _instance;

    /// <summary>
    /// Get the bootstrap instance (if exists).
    /// </summary>
    public static MainMenuBootstrap Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MainMenuBootstrap>();
            }
            return _instance;
        }
    }

    #endregion
}
