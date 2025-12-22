using UnityEngine;
using System;

/// <summary>
/// Data structure for player appearance customization.
/// Includes face, jersey, and equipment options.
/// </summary>
[Serializable]
public class PlayerCustomization
{
    [Header("Identity")]
    public string playerName = "Player";
    public int jerseyNumber = 99;

    [Header("Face")]
    [Range(0, 5)] public int skinToneIndex = 2;
    [Range(0, 7)] public int hairStyleIndex = 0;
    [Range(0, 4)] public int hairColorIndex = 0;
    [Range(0, 3)] public int eyeStyleIndex = 0;
    [Range(0, 3)] public int facialHairIndex = 0;

    [Header("Equipment")]
    [Range(0, 4)] public int helmetStyleIndex = 0;
    [Range(0, 2)] public int visorStyleIndex = 0;
    [Range(0, 3)] public int gloveStyleIndex = 0;

    [Header("Jersey")]
    public Color primaryColor = Color.blue;
    public Color secondaryColor = Color.white;
    public Color accentColor = Color.red;

    /// <summary>
    /// Create a random customization.
    /// </summary>
    public static PlayerCustomization Random()
    {
        return new PlayerCustomization
        {
            playerName = GenerateRandomName(),
            jerseyNumber = UnityEngine.Random.Range(1, 99),
            skinToneIndex = UnityEngine.Random.Range(0, 6),
            hairStyleIndex = UnityEngine.Random.Range(0, 8),
            hairColorIndex = UnityEngine.Random.Range(0, 5),
            eyeStyleIndex = UnityEngine.Random.Range(0, 4),
            facialHairIndex = UnityEngine.Random.Range(0, 4),
            helmetStyleIndex = UnityEngine.Random.Range(0, 5),
            visorStyleIndex = UnityEngine.Random.Range(0, 3),
            gloveStyleIndex = UnityEngine.Random.Range(0, 4)
        };
    }

    private static string GenerateRandomName()
    {
        string[] firstNames = { "Mike", "Alex", "John", "Chris", "Steve", "Nick", "Tom", "Dave", "Ryan", "Matt" };
        string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Davis", "Miller", "Wilson" };

        return $"{firstNames[UnityEngine.Random.Range(0, firstNames.Length)]} {lastNames[UnityEngine.Random.Range(0, lastNames.Length)]}";
    }

    /// <summary>
    /// Clone this customization.
    /// </summary>
    public PlayerCustomization Clone()
    {
        return new PlayerCustomization
        {
            playerName = this.playerName,
            jerseyNumber = this.jerseyNumber,
            skinToneIndex = this.skinToneIndex,
            hairStyleIndex = this.hairStyleIndex,
            hairColorIndex = this.hairColorIndex,
            eyeStyleIndex = this.eyeStyleIndex,
            facialHairIndex = this.facialHairIndex,
            helmetStyleIndex = this.helmetStyleIndex,
            visorStyleIndex = this.visorStyleIndex,
            gloveStyleIndex = this.gloveStyleIndex,
            primaryColor = this.primaryColor,
            secondaryColor = this.secondaryColor,
            accentColor = this.accentColor
        };
    }
}

/// <summary>
/// ScriptableObject containing all customization asset references.
/// </summary>
[CreateAssetMenu(fileName = "CustomizationAssets", menuName = "Hockey/Customization Assets", order = 5)]
public class CustomizationAssets : ScriptableObject
{
    [Header("Skin Tones")]
    public Color[] skinTones = new Color[]
    {
        new Color(1f, 0.87f, 0.77f),      // Light
        new Color(0.96f, 0.80f, 0.69f),   // Fair
        new Color(0.87f, 0.72f, 0.53f),   // Medium
        new Color(0.76f, 0.57f, 0.42f),   // Olive
        new Color(0.55f, 0.38f, 0.28f),   // Brown
        new Color(0.36f, 0.25f, 0.18f)    // Dark
    };

    [Header("Hair Colors")]
    public Color[] hairColors = new Color[]
    {
        new Color(0.1f, 0.07f, 0.05f),    // Black
        new Color(0.35f, 0.23f, 0.13f),   // Brown
        new Color(0.6f, 0.4f, 0.2f),      // Light Brown
        new Color(0.9f, 0.75f, 0.35f),    // Blonde
        new Color(0.6f, 0.2f, 0.1f)       // Red
    };

    [Header("Hair Styles")]
    public Sprite[] hairSprites;

    [Header("Face Parts")]
    public Sprite[] eyeSprites;
    public Sprite[] facialHairSprites;

    [Header("Equipment")]
    public Sprite[] helmetSprites;
    public Sprite[] visorSprites;
    public Sprite[] gloveSprites;

    /// <summary>
    /// Get skin color for an index.
    /// </summary>
    public Color GetSkinTone(int index)
    {
        if (skinTones == null || skinTones.Length == 0) return Color.white;
        return skinTones[Mathf.Clamp(index, 0, skinTones.Length - 1)];
    }

    /// <summary>
    /// Get hair color for an index.
    /// </summary>
    public Color GetHairColor(int index)
    {
        if (hairColors == null || hairColors.Length == 0) return Color.black;
        return hairColors[Mathf.Clamp(index, 0, hairColors.Length - 1)];
    }

    /// <summary>
    /// Get hair sprite for an index.
    /// </summary>
    public Sprite GetHairSprite(int index)
    {
        if (hairSprites == null || hairSprites.Length == 0) return null;
        return hairSprites[Mathf.Clamp(index, 0, hairSprites.Length - 1)];
    }
}

/// <summary>
/// Component that applies customization to player visuals.
/// </summary>
public class PlayerCustomizer : MonoBehaviour
{
    [Header("Customization Data")]
    [SerializeField] private PlayerCustomization customization;
    [SerializeField] private CustomizationAssets assets;

    [Header("Face Sprites")]
    [SerializeField] private SpriteRenderer faceSprite;
    [SerializeField] private SpriteRenderer hairSprite;
    [SerializeField] private SpriteRenderer facialHairSprite;
    [SerializeField] private SpriteRenderer helmetSprite;
    [SerializeField] private SpriteRenderer visorSprite;

    [Header("Name Display")]
    [SerializeField] private TextMesh nameText;
    [SerializeField] private TextMesh numberText;

    private MaterialPropertyBlock propertyBlock;

    public PlayerCustomization Customization
    {
        get => customization;
        set
        {
            customization = value;
            ApplyCustomization();
        }
    }

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (customization == null)
        {
            customization = PlayerCustomization.Random();
        }
    }

    private void Start()
    {
        ApplyCustomization();
    }

    /// <summary>
    /// Apply current customization to visuals.
    /// </summary>
    public void ApplyCustomization()
    {
        if (customization == null || assets == null) return;

        // Apply skin tone
        if (faceSprite != null)
        {
            faceSprite.color = assets.GetSkinTone(customization.skinToneIndex);
        }

        // Apply hair
        if (hairSprite != null)
        {
            hairSprite.sprite = assets.GetHairSprite(customization.hairStyleIndex);
            hairSprite.color = assets.GetHairColor(customization.hairColorIndex);
        }

        // Apply facial hair
        if (facialHairSprite != null && assets.facialHairSprites != null)
        {
            if (customization.facialHairIndex > 0 && customization.facialHairIndex <= assets.facialHairSprites.Length)
            {
                facialHairSprite.sprite = assets.facialHairSprites[customization.facialHairIndex - 1];
                facialHairSprite.color = assets.GetHairColor(customization.hairColorIndex);
                facialHairSprite.enabled = true;
            }
            else
            {
                facialHairSprite.enabled = false;
            }
        }

        // Apply helmet
        if (helmetSprite != null && assets.helmetSprites != null && assets.helmetSprites.Length > 0)
        {
            int helmetIdx = Mathf.Clamp(customization.helmetStyleIndex, 0, assets.helmetSprites.Length - 1);
            helmetSprite.sprite = assets.helmetSprites[helmetIdx];
        }

        // Apply visor
        if (visorSprite != null && assets.visorSprites != null)
        {
            if (customization.visorStyleIndex > 0 && customization.visorStyleIndex <= assets.visorSprites.Length)
            {
                visorSprite.sprite = assets.visorSprites[customization.visorStyleIndex - 1];
                visorSprite.enabled = true;
            }
            else
            {
                visorSprite.enabled = false;
            }
        }

        // Apply name/number
        if (nameText != null)
        {
            nameText.text = customization.playerName.Split(' ')[^1].ToUpper(); // Last name
        }

        if (numberText != null)
        {
            numberText.text = customization.jerseyNumber.ToString();
        }

        // Apply jersey colors to visual controller
        PlayerVisualController visual = GetComponent<PlayerVisualController>();
        if (visual != null)
        {
            visual.SetTeamColors(customization.primaryColor, customization.secondaryColor);
        }
    }

    /// <summary>
    /// Set customization from a preset.
    /// </summary>
    public void SetFromPreset(PlayerCustomization preset)
    {
        customization = preset.Clone();
        ApplyCustomization();
    }

    /// <summary>
    /// Randomize appearance.
    /// </summary>
    public void Randomize()
    {
        customization = PlayerCustomization.Random();
        ApplyCustomization();
    }
}
