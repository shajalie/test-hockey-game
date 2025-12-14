using UnityEngine;

/// <summary>
/// Represents a roguelite artifact/modifier that affects gameplay during a run.
/// Inspired by "Tape to Tape" hockey roguelite.
/// </summary>
[CreateAssetMenu(fileName = "New Artifact", menuName = "Hockey/Run Modifier", order = 0)]
public class RunModifier : ScriptableObject
{
    [Header("Artifact Identity")]
    public string artifactName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Rarity")]
    public ArtifactRarity rarity = ArtifactRarity.Common;

    [Header("Stat Modifiers")]
    [Range(-1f, 2f)] public float skatingSpeedMultiplier = 0f;
    [Range(-1f, 2f)] public float accelerationMultiplier = 0f;
    [Range(-1f, 2f)] public float shotPowerMultiplier = 0f;
    [Range(-1f, 2f)] public float checkForceMultiplier = 0f;
    [Range(-1f, 2f)] public float puckControlMultiplier = 0f;

    [Header("Special Effects")]
    public bool explosivePucks;
    public bool slipperyIce;
    public bool magneticStick;
    public bool heavyHitter;

    [Header("Numeric Bonuses")]
    public float bonusDamage = 0f;
    public float bonusHealth = 0f;
    public float cooldownReduction = 0f;
}

public enum ArtifactRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}
