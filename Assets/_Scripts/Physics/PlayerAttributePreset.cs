using UnityEngine;

/// <summary>
/// ScriptableObject wrapper for PlayerAttributes.
/// Use this to create asset-based player presets (Speedster, Sniper, etc).
/// </summary>
[CreateAssetMenu(fileName = "NewPlayerPreset", menuName = "Hockey/Player Attribute Preset", order = 1)]
public class PlayerAttributePreset : ScriptableObject
{
    [Tooltip("Display name for this preset")]
    public string presetName = "Default";

    [Tooltip("The attribute values for this preset")]
    public PlayerAttributes attributes = PlayerAttributes.Default;

    /// <summary>
    /// Apply this preset to an IcePhysicsController.
    /// </summary>
    public void ApplyTo(IcePhysicsController controller)
    {
        if (controller != null)
        {
            controller.SetAttributes(attributes);
            Debug.Log($"[PlayerAttributePreset] Applied '{presetName}' preset: {attributes}");
        }
    }
}
