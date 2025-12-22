using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to set up the arcade menu system.
/// Provides menu items to create/setup scenes properly.
/// </summary>
public class MenuSystemSetup : Editor
{
    private const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenu.unity";
    private const string GAME_SCENE_PATH = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Hockey Game/Create Main Menu Scene", false, 100)]
    public static void CreateMainMenuScene()
    {
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Add required GameObjects
        SetupMainMenuScene();

        // Save scene
        EditorSceneManager.SaveScene(newScene, MAIN_MENU_SCENE_PATH);

        Debug.Log("[MenuSystemSetup] Main Menu scene created at: " + MAIN_MENU_SCENE_PATH);

        // Add to build settings
        AddSceneToBuildSettings(MAIN_MENU_SCENE_PATH, 0);

        EditorUtility.DisplayDialog("Menu System Setup",
            "Main Menu scene created successfully!\n\nThe scene has been added to Build Settings.\nPress Play to test the menu.",
            "OK");
    }

    [MenuItem("Hockey Game/Setup Menu In Current Scene", false, 101)]
    public static void SetupMenuInCurrentScene()
    {
        SetupMainMenuScene();

        // Mark scene as dirty so it saves
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[MenuSystemSetup] Menu system added to current scene");

        EditorUtility.DisplayDialog("Menu System Setup",
            "Menu system has been added to the current scene!\n\nPress Play to test.",
            "OK");
    }

    [MenuItem("Hockey Game/Add Menu Bootstrap To Scene", false, 102)]
    public static void AddMenuBootstrap()
    {
        // Check if already exists
        MainMenuBootstrap existing = Object.FindObjectOfType<MainMenuBootstrap>();
        if (existing != null)
        {
            Debug.LogWarning("[MenuSystemSetup] MainMenuBootstrap already exists in scene");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create menu system root
        GameObject menuRoot = new GameObject("=== MENU SYSTEM ===");
        menuRoot.AddComponent<MainMenuBootstrap>();

        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Selection.activeGameObject = menuRoot;
        Debug.Log("[MenuSystemSetup] MainMenuBootstrap added to scene");
    }

    private static void SetupMainMenuScene()
    {
        // Remove default objects if this is a new empty scene
        Camera existingCam = Object.FindObjectOfType<Camera>();
        if (existingCam != null && existingCam.name == "Main Camera")
        {
            // Keep the camera but remove default light
            Light defaultLight = Object.FindObjectOfType<Light>();
            if (defaultLight != null && defaultLight.name == "Directional Light")
            {
                DestroyImmediate(defaultLight.gameObject);
            }
        }

        // Check if GameManager exists
        GameManager gm = Object.FindObjectOfType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
            Debug.Log("[MenuSystemSetup] Created GameManager");
        }

        // Check if MenuBootstrap exists
        MainMenuBootstrap bootstrap = Object.FindObjectOfType<MainMenuBootstrap>();
        if (bootstrap == null)
        {
            GameObject menuRoot = new GameObject("=== MENU SYSTEM ===");
            bootstrap = menuRoot.AddComponent<MainMenuBootstrap>();
            Debug.Log("[MenuSystemSetup] Created MainMenuBootstrap");
        }

        // Add Event System if missing
        UnityEngine.EventSystems.EventSystem eventSystem = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventObj = new GameObject("EventSystem");
            eventObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[MenuSystemSetup] Created EventSystem");
        }

        // Ensure camera exists
        Camera cam = Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
            camObj.AddComponent<AudioListener>();
            Debug.Log("[MenuSystemSetup] Created Main Camera");
        }
    }

    private static void AddSceneToBuildSettings(string scenePath, int index)
    {
        // Get current build scenes
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // Check if already added
        foreach (var scene in buildScenes)
        {
            if (scene.path == scenePath)
            {
                Debug.Log("[MenuSystemSetup] Scene already in build settings: " + scenePath);
                return;
            }
        }

        // Add new scene at specified index
        EditorBuildSettingsScene newScene = new EditorBuildSettingsScene(scenePath, true);

        if (index >= 0 && index < buildScenes.Count)
        {
            buildScenes.Insert(index, newScene);
        }
        else
        {
            buildScenes.Add(newScene);
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log("[MenuSystemSetup] Added scene to build settings: " + scenePath);
    }

    [MenuItem("Hockey Game/Open Main Menu Scene", false, 200)]
    public static void OpenMainMenuScene()
    {
        if (System.IO.File.Exists(MAIN_MENU_SCENE_PATH))
        {
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH);
        }
        else
        {
            if (EditorUtility.DisplayDialog("Scene Not Found",
                "MainMenu scene doesn't exist. Would you like to create it?",
                "Create", "Cancel"))
            {
                CreateMainMenuScene();
            }
        }
    }

    [MenuItem("Hockey Game/Open Game Scene", false, 201)]
    public static void OpenGameScene()
    {
        if (System.IO.File.Exists(GAME_SCENE_PATH))
        {
            EditorSceneManager.OpenScene(GAME_SCENE_PATH);
        }
        else
        {
            Debug.LogError("[MenuSystemSetup] Game scene not found: " + GAME_SCENE_PATH);
        }
    }
}
