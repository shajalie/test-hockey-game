using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Database of all available artifacts. Can generate default artifacts via editor.
/// </summary>
[CreateAssetMenu(fileName = "ArtifactDatabase", menuName = "Hockey/Artifact Database", order = 10)]
public class ArtifactDatabase : ScriptableObject
{
    [SerializeField] private List<RunModifier> allArtifacts = new List<RunModifier>();

    public IReadOnlyList<RunModifier> AllArtifacts => allArtifacts;

    /// <summary>
    /// Get artifacts by rarity.
    /// </summary>
    public List<RunModifier> GetByRarity(ArtifactRarity rarity)
    {
        return allArtifacts.FindAll(a => a.rarity == rarity);
    }

    /// <summary>
    /// Get random artifacts (for draft).
    /// </summary>
    public List<RunModifier> GetRandom(int count, List<RunModifier> exclude = null)
    {
        List<RunModifier> available = new List<RunModifier>(allArtifacts);

        if (exclude != null)
        {
            foreach (var ex in exclude)
            {
                available.Remove(ex);
            }
        }

        List<RunModifier> result = new List<RunModifier>();

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            result.Add(available[index]);
            available.RemoveAt(index);
        }

        return result;
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Default Artifacts")]
    public void GenerateDefaultArtifacts()
    {
        string folderPath = "Assets/_Scripts/Data/Artifacts";

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/_Scripts/Data", "Artifacts");
        }

        // Clear existing
        allArtifacts.Clear();

        // === COMMON ARTIFACTS ===

        CreateArtifact("Speed Skates", "Slightly increases skating speed.",
            ArtifactRarity.Common, folderPath,
            speedMult: 0.1f);

        CreateArtifact("Grip Tape", "Better puck control.",
            ArtifactRarity.Common, folderPath,
            puckControlMult: 0.15f);

        CreateArtifact("Power Workout", "Slightly stronger shots.",
            ArtifactRarity.Common, folderPath,
            shotPowerMult: 0.1f);

        CreateArtifact("Quick Start", "Better acceleration off the line.",
            ArtifactRarity.Common, folderPath,
            accelMult: 0.15f);

        // === UNCOMMON ARTIFACTS ===

        CreateArtifact("Heavy Hitter", "Body checks send opponents flying!",
            ArtifactRarity.Uncommon, folderPath,
            checkForceMult: 0.2f, heavyHitter: true);

        CreateArtifact("Rocket Skates", "Significantly faster skating.",
            ArtifactRarity.Uncommon, folderPath,
            speedMult: 0.2f, accelMult: 0.1f);

        CreateArtifact("Sniper Stick", "Powerful, accurate shots.",
            ArtifactRarity.Uncommon, folderPath,
            shotPowerMult: 0.25f);

        CreateArtifact("Magnetic Blade", "Puck sticks to your stick better.",
            ArtifactRarity.Uncommon, folderPath,
            puckControlMult: 0.3f, magneticStick: true);

        // === RARE ARTIFACTS ===

        CreateArtifact("Slippery Ice", "Less friction everywhere. Chaotic!",
            ArtifactRarity.Rare, folderPath,
            speedMult: 0.15f, slipperyIce: true);

        CreateArtifact("Cannon Arm", "Devastating shot power.",
            ArtifactRarity.Rare, folderPath,
            shotPowerMult: 0.4f, checkForceMult: 0.1f);

        CreateArtifact("Lightning Reflexes", "Maximum speed and acceleration.",
            ArtifactRarity.Rare, folderPath,
            speedMult: 0.25f, accelMult: 0.25f);

        CreateArtifact("Iron Body", "Hard to knock off the puck.",
            ArtifactRarity.Rare, folderPath,
            puckControlMult: 0.4f, checkForceMult: 0.15f);

        // === LEGENDARY ARTIFACTS ===

        CreateArtifact("Explosive Puck", "Your shots EXPLODE on impact!",
            ArtifactRarity.Legendary, folderPath,
            shotPowerMult: 0.3f, explosivePucks: true);

        CreateArtifact("The Great One", "All stats significantly boosted.",
            ArtifactRarity.Legendary, folderPath,
            speedMult: 0.2f, accelMult: 0.2f, shotPowerMult: 0.2f,
            checkForceMult: 0.2f, puckControlMult: 0.2f);

        CreateArtifact("Phantom Skater", "Blazing speed, untouchable.",
            ArtifactRarity.Legendary, folderPath,
            speedMult: 0.4f, accelMult: 0.3f);

        // Save database
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ArtifactDatabase] Generated {allArtifacts.Count} artifacts!");
    }

    private void CreateArtifact(string artifactName, string description, ArtifactRarity rarity, string folderPath,
        float speedMult = 0f, float accelMult = 0f, float shotPowerMult = 0f,
        float checkForceMult = 0f, float puckControlMult = 0f,
        bool explosivePucks = false, bool slipperyIce = false,
        bool magneticStick = false, bool heavyHitter = false)
    {
        RunModifier artifact = ScriptableObject.CreateInstance<RunModifier>();

        artifact.artifactName = artifactName;
        artifact.description = description;
        artifact.rarity = rarity;

        artifact.skatingSpeedMultiplier = speedMult;
        artifact.accelerationMultiplier = accelMult;
        artifact.shotPowerMultiplier = shotPowerMult;
        artifact.checkForceMultiplier = checkForceMult;
        artifact.puckControlMultiplier = puckControlMult;

        artifact.explosivePucks = explosivePucks;
        artifact.slipperyIce = slipperyIce;
        artifact.magneticStick = magneticStick;
        artifact.heavyHitter = heavyHitter;

        // Save as asset
        string assetPath = $"{folderPath}/{artifactName.Replace(" ", "_")}.asset";
        AssetDatabase.CreateAsset(artifact, assetPath);

        allArtifacts.Add(artifact);
    }
#endif
}
