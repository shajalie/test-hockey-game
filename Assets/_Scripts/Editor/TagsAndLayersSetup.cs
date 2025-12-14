using UnityEngine;
using UnityEditor;

/// <summary>
/// Sets up required tags and layers for the hockey game.
/// </summary>
public class TagsAndLayersSetup
{
    [MenuItem("Hockey Game/Setup Tags and Layers", false, 50)]
    public static void SetupTagsAndLayers()
    {
        // Add Tags
        AddTag("Player");
        AddTag("Puck");
        AddTag("Goal");
        AddTag("Wall");

        // Add Layers
        AddLayer("Player");
        AddLayer("Puck");
        AddLayer("Ground");
        AddLayer("Wall");

        Debug.Log("[TagsAndLayersSetup] Tags and Layers configured!");
        EditorUtility.DisplayDialog("Setup Complete",
            "Tags and Layers have been configured:\n\n" +
            "Tags: Player, Puck, Goal, Wall\n" +
            "Layers: Player, Puck, Ground, Wall", "OK");
    }

    private static void AddTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // Check if tag already exists
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                Debug.Log($"Tag '{tagName}' already exists");
                return;
            }
        }

        // Add new tag
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();

        Debug.Log($"Added tag: {tagName}");
    }

    private static void AddLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layersProp = tagManager.FindProperty("layers");

        // Check if layer already exists
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName)
            {
                Debug.Log($"Layer '{layerName}' already exists");
                return;
            }
        }

        // Find first empty user layer (8-31)
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"Added layer: {layerName} at index {i}");
                return;
            }
        }

        Debug.LogWarning($"Could not add layer '{layerName}' - no empty slots available");
    }

    // Auto-run on first import
    [InitializeOnLoadMethod]
    private static void OnProjectLoaded()
    {
        // Only run once per session
        if (SessionState.GetBool("HockeyTagsSetup", false))
            return;

        SessionState.SetBool("HockeyTagsSetup", true);

        // Check if tags exist, if not offer to create them
        if (!TagExists("Goal") || !TagExists("Puck"))
        {
            if (EditorUtility.DisplayDialog("Hockey Game Setup",
                "Required tags/layers are missing.\n\nWould you like to set them up now?",
                "Yes", "Later"))
            {
                SetupTagsAndLayers();
            }
        }
    }

    private static bool TagExists(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                return true;
            }
        }

        return false;
    }
}
