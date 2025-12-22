using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically initializes the menu system when the game starts.
/// This ensures the menu appears even if no bootstrap is in the scene.
/// Uses RuntimeInitializeOnLoadMethod to run before any scene loads.
/// </summary>
public static class MenuAutoInitializer
{
    private static bool hasInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeScene()
    {
        Debug.Log("[MenuAutoInitializer] Preparing menu system (BeforeSceneLoad)...");
        hasInitialized = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeAfterScene()
    {
        if (hasInitialized) return;
        hasInitialized = true;

        Debug.Log("[MenuAutoInitializer] Initializing menu system (AfterSceneLoad)...");

        // Disable any GameBootstrap autoStartRun to prevent it from starting the game
        DisableAutoStart();

        // Create persistent game objects
        CreatePersistentSystems();
    }

    private static void DisableAutoStart()
    {
        // Find and disable GameBootstrap autoStartRun
        GameBootstrap[] bootstraps = Object.FindObjectsOfType<GameBootstrap>();
        foreach (var bootstrap in bootstraps)
        {
            // Use reflection to set autoStartRun to false
            var field = typeof(GameBootstrap).GetField("autoStartRun",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(bootstrap, false);
                Debug.Log("[MenuAutoInitializer] Disabled GameBootstrap autoStartRun");
            }

            // Also disable the bootstrap component temporarily
            bootstrap.enabled = false;
        }
    }

    private static void CreatePersistentSystems()
    {
        // Create GameManager if it doesn't exist
        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("[GameManager]");
            gmObj.AddComponent<GameManager>();
            Object.DontDestroyOnLoad(gmObj);
            Debug.Log("[MenuAutoInitializer] Created GameManager");
        }

        // Create Menu System
        GameObject menuSystemObj = new GameObject("[MenuSystem]");
        menuSystemObj.AddComponent<MenuSystemRunner>();
        Object.DontDestroyOnLoad(menuSystemObj);
        Debug.Log("[MenuAutoInitializer] Created MenuSystem");
    }
}

/// <summary>
/// Runs the menu system. Created by MenuAutoInitializer.
/// </summary>
public class MenuSystemRunner : MonoBehaviour
{
    private MenuManager menuManager;
    private bool isInitialized = false;

    private void Awake()
    {
        Debug.Log("[MenuSystemRunner] Awake called");
    }

    private void Start()
    {
        Debug.Log("[MenuSystemRunner] Start called - waiting one frame before initializing...");
        // Wait one frame to ensure everything is loaded
        StartCoroutine(InitializeDelayed());
    }

    private System.Collections.IEnumerator InitializeDelayed()
    {
        yield return null; // Wait one frame

        Debug.Log("[MenuSystemRunner] InitializeDelayed - now initializing...");
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        Debug.Log("[MenuSystemRunner] Initialize() running...");

        // Check if MainMenuBootstrap already exists (scene-based setup)
        MainMenuBootstrap existingBootstrap = FindObjectOfType<MainMenuBootstrap>();
        if (existingBootstrap != null)
        {
            Debug.Log("[MenuSystemRunner] Found existing MainMenuBootstrap - using that instead");
            Destroy(gameObject);
            return;
        }

        // Check if MenuManager already exists
        menuManager = FindObjectOfType<MenuManager>();
        if (menuManager != null)
        {
            Debug.Log("[MenuSystemRunner] Found existing MenuManager");
            isInitialized = true;
            return;
        }

        Debug.Log("[MenuSystemRunner] Creating EventSystem and MenuManager...");

        // Ensure EventSystem exists
        EnsureEventSystem();

        // Create MenuManager
        GameObject menuObj = new GameObject("MenuManager");
        menuObj.transform.SetParent(transform);
        menuManager = menuObj.AddComponent<MenuManager>();

        isInitialized = true;
        Debug.Log("[MenuSystemRunner] Menu system initialization COMPLETE!");
    }

    private void EnsureEventSystem()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventObj = new GameObject("EventSystem");
            eventObj.transform.SetParent(transform);
            eventObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[MenuSystemRunner] Created EventSystem");
        }
    }

    private void Update()
    {
        // Handle pause input globally
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuManager != null && menuManager.CurrentScreen != null)
            {
                // Already showing a menu screen - let it handle input
            }
            else if (GameManager.Instance != null && GameManager.Instance.IsMatchActive)
            {
                // In a match - show pause menu
                menuManager?.ShowPauseMenu();
            }
        }
    }

    private void OnGUI()
    {
        // Show a small debug indicator in the corner
        #if UNITY_EDITOR
        GUI.color = Color.green;
        GUI.Label(new Rect(Screen.width - 150, 10, 140, 20), "[Menu System Active]");
        GUI.color = Color.white;
        #endif
    }
}
