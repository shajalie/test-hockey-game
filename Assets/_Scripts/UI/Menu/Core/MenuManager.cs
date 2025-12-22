using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// Central navigation manager for the menu system.
/// Handles screen registration, navigation stack, and transitions.
/// </summary>
public class MenuManager : MonoBehaviour
{
    #region Singleton

    public static MenuManager Instance { get; private set; }

    #endregion

    #region Serialized Fields

    [Header("Canvas Settings")]
    [SerializeField] private int canvasSortOrder = 0;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);

    [Header("Settings")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region Private Fields

    private Canvas mainCanvas;
    private CanvasScaler canvasScaler;

    private Dictionary<Type, ScreenBase> screens = new Dictionary<Type, ScreenBase>();
    private Stack<ScreenBase> navigationStack = new Stack<ScreenBase>();
    private ScreenBase currentScreen;

    private bool isTransitioning = false;

    #endregion

    #region Events

    /// <summary>
    /// Fired when the active screen changes.
    /// </summary>
    public event Action<ScreenBase> OnScreenChanged;

    /// <summary>
    /// Fired when navigation stack is emptied (no more screens to go back to).
    /// </summary>
    public event Action OnNavigationStackEmpty;

    #endregion

    #region Properties

    /// <summary>
    /// Currently active screen.
    /// </summary>
    public ScreenBase CurrentScreen => currentScreen;

    /// <summary>
    /// Whether a transition is currently in progress.
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    /// <summary>
    /// The main UI canvas.
    /// </summary>
    public Canvas Canvas => mainCanvas;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Create canvas
        CreateCanvas();

        // Register all screens
        RegisterScreens();

        Debug.Log("[MenuManager] Initialized");
    }

    private void Start()
    {
        Debug.Log("[MenuManager] Starting - navigating to RunHubScreen...");

        // Navigate to initial screen
        NavigateTo<RunHubScreen>(clearStack: true);

        Debug.Log("[MenuManager] Menu should now be visible!");
    }

    private void Update()
    {
        // Let current screen handle input
        currentScreen?.HandleInput();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Create the main UI canvas.
    /// </summary>
    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Menu Canvas");
        canvasObj.transform.SetParent(transform);

        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = canvasSortOrder;

        canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// Register all screen types.
    /// </summary>
    private void RegisterScreens()
    {
        RegisterScreen<RunHubScreen>();
        RegisterScreen<ModeSelectScreen>();
        RegisterScreen<SettingsScreen>();
        RegisterScreen<DraftScreen>();
        RegisterScreen<GameOverScreen>();
        RegisterScreen<PauseScreen>();

        Debug.Log($"[MenuManager] Registered {screens.Count} screens");
    }

    /// <summary>
    /// Register a screen type.
    /// </summary>
    private void RegisterScreen<T>() where T : ScreenBase
    {
        Type screenType = typeof(T);

        if (screens.ContainsKey(screenType))
        {
            Debug.LogWarning($"[MenuManager] Screen {screenType.Name} already registered");
            return;
        }

        // Create screen game object
        GameObject screenObj = new GameObject(screenType.Name);
        screenObj.transform.SetParent(transform);

        // Add screen component
        T screen = screenObj.AddComponent<T>();
        screen.Initialize(this, mainCanvas);

        screens[screenType] = screen;

        if (debugMode)
        {
            Debug.Log($"[MenuManager] Registered screen: {screenType.Name}");
        }
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Navigate to a screen by type.
    /// </summary>
    public void NavigateTo<T>(bool clearStack = false) where T : ScreenBase
    {
        Type screenType = typeof(T);
        NavigateTo(screenType, clearStack);
    }

    /// <summary>
    /// Navigate to a screen by type.
    /// </summary>
    public void NavigateTo(Type screenType, bool clearStack = false)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[MenuManager] Cannot navigate while transitioning");
            return;
        }

        if (!screens.ContainsKey(screenType))
        {
            Debug.LogError($"[MenuManager] Screen not registered: {screenType.Name}");
            return;
        }

        ScreenBase targetScreen = screens[screenType];

        if (targetScreen == currentScreen)
        {
            Debug.LogWarning($"[MenuManager] Already on screen: {screenType.Name}");
            return;
        }

        if (clearStack)
        {
            ClearNavigationStack();
        }
        else if (currentScreen != null)
        {
            // Push current screen to stack
            navigationStack.Push(currentScreen);
        }

        TransitionToScreen(targetScreen);
    }

    /// <summary>
    /// Navigate back to the previous screen.
    /// </summary>
    public void NavigateBack()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[MenuManager] Cannot navigate while transitioning");
            return;
        }

        if (navigationStack.Count == 0)
        {
            Debug.Log("[MenuManager] Navigation stack empty");
            OnNavigationStackEmpty?.Invoke();
            return;
        }

        ScreenBase previousScreen = navigationStack.Pop();
        TransitionToScreen(previousScreen);
    }

    /// <summary>
    /// Clear the navigation stack without navigating.
    /// </summary>
    public void ClearNavigationStack()
    {
        // Hide all stacked screens without animation
        while (navigationStack.Count > 0)
        {
            ScreenBase screen = navigationStack.Pop();
            screen.Hide(animated: false);
        }
    }

    /// <summary>
    /// Transition from current screen to target screen.
    /// </summary>
    private void TransitionToScreen(ScreenBase targetScreen)
    {
        isTransitioning = true;

        // Hide current screen
        if (currentScreen != null)
        {
            currentScreen.Hide(animated: true);
        }

        // Show target screen
        ScreenBase previousScreen = currentScreen;
        currentScreen = targetScreen;
        currentScreen.Show(animated: true);

        // Notify listeners
        OnScreenChanged?.Invoke(currentScreen);

        // Reset transitioning flag after a delay
        StartCoroutine(ResetTransitionFlag());

        if (debugMode)
        {
            Debug.Log($"[MenuManager] Navigated: {previousScreen?.ScreenName ?? "null"} -> {targetScreen.ScreenName}");
        }
    }

    private System.Collections.IEnumerator ResetTransitionFlag()
    {
        yield return new WaitForSecondsRealtime(ArcadeTheme.TransitionDuration * 1.1f);
        isTransitioning = false;
    }

    #endregion

    #region Screen Access

    /// <summary>
    /// Get a screen instance by type.
    /// </summary>
    public T GetScreen<T>() where T : ScreenBase
    {
        Type screenType = typeof(T);
        if (screens.ContainsKey(screenType))
        {
            return screens[screenType] as T;
        }
        return null;
    }

    /// <summary>
    /// Check if a screen type is registered.
    /// </summary>
    public bool HasScreen<T>() where T : ScreenBase
    {
        return screens.ContainsKey(typeof(T));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Show the pause menu (can be called from gameplay).
    /// </summary>
    public void ShowPauseMenu()
    {
        NavigateTo<PauseScreen>();
    }

    /// <summary>
    /// Show the draft screen (called after winning a match).
    /// </summary>
    public void ShowDraftScreen()
    {
        NavigateTo<DraftScreen>();
    }

    /// <summary>
    /// Show the game over screen.
    /// </summary>
    public void ShowGameOverScreen()
    {
        NavigateTo<GameOverScreen>(clearStack: true);
    }

    /// <summary>
    /// Return to the main menu (run hub).
    /// </summary>
    public void ReturnToMainMenu()
    {
        NavigateTo<RunHubScreen>(clearStack: true);
    }

    #endregion
}
