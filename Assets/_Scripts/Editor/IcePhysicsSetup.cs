using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utilities for setting up the ice physics system.
/// </summary>
public class IcePhysicsSetup : Editor
{
    private const string SETTINGS_PATH = "Assets/GameData/IcePhysicsSettings.asset";
    private const string PHYSICS_FOLDER = "Assets/GameData";

    [MenuItem("Hockey Game/Create Ice Physics Settings", false, 300)]
    public static void CreateIcePhysicsSettings()
    {
        // Check if already exists
        IcePhysicsSettings existing = AssetDatabase.LoadAssetAtPath<IcePhysicsSettings>(SETTINGS_PATH);
        if (existing != null)
        {
            Debug.Log("[IcePhysicsSetup] Settings already exist at: " + SETTINGS_PATH);
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(PHYSICS_FOLDER))
        {
            string[] folders = PHYSICS_FOLDER.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }

        // Create instance
        IcePhysicsSettings settings = ScriptableObject.CreateInstance<IcePhysicsSettings>();

        // Save asset
        AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log("[IcePhysicsSetup] Created Ice Physics Settings at: " + SETTINGS_PATH);

        // Select in project
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);

        EditorUtility.DisplayDialog("Ice Physics Setup",
            "Ice Physics Settings created successfully!\n\nPath: " + SETTINGS_PATH +
            "\n\nYou can adjust the values in the Inspector to tune the skating feel.",
            "OK");
    }

    [MenuItem("Hockey Game/Setup Player with Ice Physics", false, 301)]
    public static void SetupPlayerWithIcePhysics()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No Selection",
                "Please select a GameObject in the scene to add ice physics to.",
                "OK");
            return;
        }

        // Add required components if missing
        bool addedComponents = false;

        if (selected.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = selected.AddComponent<Rigidbody>();
            rb.mass = 80f;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            addedComponents = true;
            Debug.Log("[IcePhysicsSetup] Added Rigidbody");
        }

        if (selected.GetComponent<CapsuleCollider>() == null)
        {
            CapsuleCollider capsule = selected.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.4f;
            capsule.center = new Vector3(0f, 1f, 0f);
            addedComponents = true;
            Debug.Log("[IcePhysicsSetup] Added CapsuleCollider");
        }

        IcePhysicsController controller = selected.GetComponent<IcePhysicsController>();
        if (controller == null)
        {
            controller = selected.AddComponent<IcePhysicsController>();
            addedComponents = true;
            Debug.Log("[IcePhysicsSetup] Added IcePhysicsController");
        }

        // Try to assign settings
        IcePhysicsSettings settings = AssetDatabase.LoadAssetAtPath<IcePhysicsSettings>(SETTINGS_PATH);
        if (settings != null)
        {
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty settingsProp = so.FindProperty("settings");
            settingsProp.objectReferenceValue = settings;
            so.ApplyModifiedProperties();
            Debug.Log("[IcePhysicsSetup] Assigned IcePhysicsSettings");
        }
        else
        {
            Debug.LogWarning("[IcePhysicsSetup] No IcePhysicsSettings found. Use 'Hockey Game/Create Ice Physics Settings' first.");
        }

        if (addedComponents)
        {
            EditorUtility.SetDirty(selected);
            EditorUtility.DisplayDialog("Ice Physics Setup",
                $"Added ice physics components to '{selected.name}'.\n\n" +
                "Components added:\n" +
                "- Rigidbody (if missing)\n" +
                "- CapsuleCollider (if missing)\n" +
                "- IcePhysicsController",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Ice Physics Setup",
                $"'{selected.name}' already has ice physics components.",
                "OK");
        }
    }

    [MenuItem("Hockey Game/Add Billboard to Selected", false, 302)]
    public static void AddBillboardToSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No Selection",
                "Please select a sprite GameObject to add billboarding to.",
                "OK");
            return;
        }

        // Check for SpriteRenderer
        SpriteRenderer sr = selected.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            EditorUtility.DisplayDialog("No SpriteRenderer",
                "Selected object needs a SpriteRenderer component for billboarding.\n\n" +
                "Add a SpriteRenderer first, then try again.",
                "OK");
            return;
        }

        // Add BillboardFace if missing
        if (selected.GetComponent<BillboardFace>() == null)
        {
            selected.AddComponent<BillboardFace>();
            EditorUtility.SetDirty(selected);
            Debug.Log("[IcePhysicsSetup] Added BillboardFace to " + selected.name);

            EditorUtility.DisplayDialog("Billboard Added",
                $"Added BillboardFace to '{selected.name}'.\n\n" +
                "The sprite will now always face the camera and flip based on movement direction.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Already Has Billboard",
                $"'{selected.name}' already has a BillboardFace component.",
                "OK");
        }
    }

    [MenuItem("Hockey Game/Create Player Attribute Presets", false, 303)]
    public static void CreatePlayerAttributePresets()
    {
        string folder = "Assets/GameData/PlayerPresets";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/GameData", "PlayerPresets");
        }

        // Create presets as ScriptableObjects
        CreatePreset(folder, "Balanced", PlayerAttributes.Default);
        CreatePreset(folder, "Speedster", PlayerAttributes.Speedster);
        CreatePreset(folder, "PowerForward", PlayerAttributes.PowerForward);
        CreatePreset(folder, "Sniper", PlayerAttributes.Sniper);
        CreatePreset(folder, "Playmaker", PlayerAttributes.Playmaker);

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Player Presets",
            "Created player attribute preset assets in:\n" + folder,
            "OK");
    }

    private static void CreatePreset(string folder, string name, PlayerAttributes attributes)
    {
        string path = $"{folder}/{name}Preset.asset";

        if (AssetDatabase.LoadAssetAtPath<PlayerAttributePreset>(path) != null)
        {
            Debug.Log("[IcePhysicsSetup] Preset already exists: " + path);
            return;
        }

        PlayerAttributePreset preset = ScriptableObject.CreateInstance<PlayerAttributePreset>();
        preset.attributes = attributes;
        preset.presetName = name;

        AssetDatabase.CreateAsset(preset, path);
        Debug.Log("[IcePhysicsSetup] Created preset: " + path);
    }
}
