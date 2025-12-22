using UnityEngine;

/// <summary>
/// Centralized arcade-retro visual theme for the menu system.
/// All colors, sizes, and styling constants defined here.
/// </summary>
public static class ArcadeTheme
{
    #region Color Palette

    // Primary colors
    public static readonly Color Primary = new Color32(26, 43, 74, 255);        // #1A2B4A - Deep ice blue
    public static readonly Color PrimaryLight = new Color32(42, 67, 110, 255);  // Lighter variant
    public static readonly Color PrimaryDark = new Color32(15, 25, 45, 255);    // Darker variant

    // Accent colors
    public static readonly Color Accent = new Color32(255, 107, 53, 255);       // #FF6B35 - Vibrant orange
    public static readonly Color AccentHover = new Color32(255, 130, 85, 255);  // Lighter on hover
    public static readonly Color AccentPressed = new Color32(220, 85, 35, 255); // Darker on press

    // Feedback colors
    public static readonly Color Success = new Color32(0, 255, 136, 255);       // #00FF88 - Neon green
    public static readonly Color Danger = new Color32(255, 20, 147, 255);       // #FF1493 - Hot pink
    public static readonly Color Warning = new Color32(255, 215, 0, 255);       // Gold/yellow
    public static readonly Color Ice = new Color32(0, 212, 255, 255);           // #00D4FF - Cyan glow

    // UI Element colors
    public static readonly Color Panel = new Color32(10, 22, 40, 204);          // #0A1628CC - Semi-transparent
    public static readonly Color PanelSolid = new Color32(20, 35, 60, 255);     // Solid panel
    public static readonly Color PanelBorder = new Color32(0, 212, 255, 180);   // Ice border
    public static readonly Color ButtonPrimary = new Color32(255, 107, 53, 255);// Orange button
    public static readonly Color ButtonSecondary = new Color32(60, 80, 120, 255);// Blue-gray button
    public static readonly Color ButtonDisabled = new Color32(80, 80, 80, 255); // Gray disabled

    // Text colors
    public static readonly Color TextPrimary = Color.white;
    public static readonly Color TextSecondary = new Color32(180, 180, 200, 255);
    public static readonly Color TextAccent = new Color32(0, 212, 255, 255);    // Ice cyan
    public static readonly Color TextDanger = new Color32(255, 100, 100, 255);  // Soft red

    // Rarity colors (for artifacts)
    public static readonly Color RarityCommon = new Color32(176, 176, 176, 255);    // #B0B0B0 Silver
    public static readonly Color RarityUncommon = new Color32(74, 222, 128, 255);   // #4ADE80 Green
    public static readonly Color RarityRare = new Color32(59, 130, 246, 255);       // #3B82F6 Blue
    public static readonly Color RarityLegendary = new Color32(255, 215, 0, 255);   // #FFD700 Gold

    #endregion

    #region Typography Sizes

    // Title sizes (chunky, bold)
    public const float TitleSize = 72f;
    public const float TitleSizeLarge = 96f;
    public const float TitleSizeSmall = 56f;

    // Header sizes
    public const float HeaderSize = 48f;
    public const float HeaderSizeSmall = 40f;

    // Body text
    public const float BodySize = 32f;
    public const float BodySizeSmall = 28f;
    public const float BodySizeLarge = 36f;

    // Button text
    public const float ButtonTextSize = 36f;
    public const float ButtonTextSizeSmall = 28f;
    public const float ButtonTextSizeLarge = 42f;

    // Labels and captions
    public const float LabelSize = 24f;
    public const float CaptionSize = 20f;

    #endregion

    #region Button Dimensions (Mobile-First)

    // Primary buttons (large CTAs)
    public static readonly Vector2 ButtonPrimarySize = new Vector2(400, 100);
    public static readonly Vector2 ButtonSecondarySize = new Vector2(300, 80);
    public static readonly Vector2 ButtonSmallSize = new Vector2(200, 60);
    public static readonly Vector2 ButtonIconSize = new Vector2(80, 80);

    // Minimum touch target (accessibility)
    public const float MinTouchTarget = 88f;

    // Border thickness
    public const float ButtonBorderWidth = 4f;
    public const float PanelBorderWidth = 3f;

    #endregion

    #region Spacing

    public const float ScreenPadding = 40f;
    public const float SectionSpacing = 48f;
    public const float ElementSpacing = 24f;
    public const float ButtonSpacing = 16f;
    public const float InnerPadding = 20f;

    #endregion

    #region Animation

    public const float TransitionDuration = 0.3f;
    public const float ButtonPressDuration = 0.1f;
    public const float FadeInDuration = 0.4f;
    public const float FadeOutDuration = 0.25f;
    public const float PulseDuration = 0.8f;
    public const float ShakeDuration = 0.3f;
    public const float ShakeIntensity = 10f;

    // Button animation values
    public const float ButtonPressScale = 0.95f;
    public const float ButtonReleaseScale = 1.05f;
    public const float ButtonHoverScale = 1.02f;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get rarity color for an artifact.
    /// </summary>
    public static Color GetRarityColor(ArtifactRarity rarity)
    {
        switch (rarity)
        {
            case ArtifactRarity.Common: return RarityCommon;
            case ArtifactRarity.Uncommon: return RarityUncommon;
            case ArtifactRarity.Rare: return RarityRare;
            case ArtifactRarity.Legendary: return RarityLegendary;
            default: return RarityCommon;
        }
    }

    /// <summary>
    /// Get glow color (lighter version) for rarity.
    /// </summary>
    public static Color GetRarityGlowColor(ArtifactRarity rarity)
    {
        Color baseColor = GetRarityColor(rarity);
        return new Color(
            Mathf.Min(1f, baseColor.r + 0.3f),
            Mathf.Min(1f, baseColor.g + 0.3f),
            Mathf.Min(1f, baseColor.b + 0.3f),
            0.6f
        );
    }

    /// <summary>
    /// Create a darker version of a color.
    /// </summary>
    public static Color Darken(Color color, float amount = 0.2f)
    {
        return new Color(
            color.r * (1f - amount),
            color.g * (1f - amount),
            color.b * (1f - amount),
            color.a
        );
    }

    /// <summary>
    /// Create a lighter version of a color.
    /// </summary>
    public static Color Lighten(Color color, float amount = 0.2f)
    {
        return new Color(
            Mathf.Min(1f, color.r + amount),
            Mathf.Min(1f, color.g + amount),
            Mathf.Min(1f, color.b + amount),
            color.a
        );
    }

    /// <summary>
    /// Get color with modified alpha.
    /// </summary>
    public static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// Interpolate between two colors with arcade-style steps (not smooth).
    /// </summary>
    public static Color LerpStepped(Color a, Color b, float t, int steps = 4)
    {
        float stepped = Mathf.Floor(t * steps) / steps;
        return Color.Lerp(a, b, stepped);
    }

    #endregion

    #region UI ColorBlock Presets

    /// <summary>
    /// Get ColorBlock for primary (orange) buttons.
    /// </summary>
    public static UnityEngine.UI.ColorBlock GetPrimaryButtonColors()
    {
        return new UnityEngine.UI.ColorBlock
        {
            normalColor = ButtonPrimary,
            highlightedColor = AccentHover,
            pressedColor = AccentPressed,
            selectedColor = ButtonPrimary,
            disabledColor = ButtonDisabled,
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
    }

    /// <summary>
    /// Get ColorBlock for secondary (blue) buttons.
    /// </summary>
    public static UnityEngine.UI.ColorBlock GetSecondaryButtonColors()
    {
        return new UnityEngine.UI.ColorBlock
        {
            normalColor = ButtonSecondary,
            highlightedColor = Lighten(ButtonSecondary, 0.15f),
            pressedColor = Darken(ButtonSecondary, 0.15f),
            selectedColor = ButtonSecondary,
            disabledColor = ButtonDisabled,
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
    }

    /// <summary>
    /// Get ColorBlock for danger/quit buttons.
    /// </summary>
    public static UnityEngine.UI.ColorBlock GetDangerButtonColors()
    {
        return new UnityEngine.UI.ColorBlock
        {
            normalColor = Danger,
            highlightedColor = Lighten(Danger, 0.15f),
            pressedColor = Darken(Danger, 0.2f),
            selectedColor = Danger,
            disabledColor = ButtonDisabled,
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
    }

    #endregion
}
