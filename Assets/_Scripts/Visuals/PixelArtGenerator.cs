using UnityEngine;

/// <summary>
/// Generates 64x64 isometric pixel art sprites for hockey players, equipment, and objects.
/// All art is generated procedurally at runtime - no external assets required.
/// </summary>
public static class PixelArtGenerator
{
    // Standard colors
    private static readonly Color32 SkinColor = new Color32(255, 213, 170, 255);
    private static readonly Color32 HelmetColor = new Color32(40, 40, 50, 255);
    private static readonly Color32 VisorColor = new Color32(180, 220, 255, 128);
    private static readonly Color32 SkateColor = new Color32(30, 30, 30, 255);
    private static readonly Color32 StickShaftColor = new Color32(139, 90, 43, 255);
    private static readonly Color32 StickBladeColor = new Color32(20, 20, 20, 255);
    private static readonly Color32 PuckColor = new Color32(15, 15, 15, 255);
    private static readonly Color32 IceHighlight = new Color32(200, 230, 255, 255);

    // Referee colors
    private static readonly Color32 RefBlack = new Color32(20, 20, 20, 255);
    private static readonly Color32 RefWhite = new Color32(240, 240, 240, 255);
    private static readonly Color32 RefOrange = new Color32(255, 100, 0, 255);

    /// <summary>
    /// Generate a 64x64 isometric hockey player sprite.
    /// </summary>
    public static Texture2D GeneratePlayerSprite(Color32 teamColor, PlayerPosition position, bool facingRight = true)
    {
        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        // Clear to transparent
        Color32[] pixels = new Color32[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 0);

        // Draw player from bottom up (isometric 2.5D view)
        // Player is centered at (32, 0) with height ~48 pixels

        // Skates (bottom, y = 0-6)
        DrawSkates(pixels, 64, facingRight);

        // Legs/pants (y = 6-18)
        DrawLegs(pixels, 64, teamColor);

        // Body/jersey (y = 18-38)
        DrawJersey(pixels, 64, teamColor, position);

        // Arms (y = 22-34)
        DrawArms(pixels, 64, teamColor, facingRight);

        // Head/helmet (y = 38-52)
        DrawHelmet(pixels, 64);

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Generate a 64x64 goalie sprite (bigger pads, different stance).
    /// </summary>
    public static Texture2D GenerateGoalieSprite(Color32 teamColor, bool facingRight = true)
    {
        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 0);

        // Goalie is wider and has leg pads
        Color32 padColor = new Color32(240, 240, 240, 255);

        // Leg pads (y = 0-20)
        DrawGoaliePads(pixels, 64, padColor);

        // Body (y = 20-40)
        DrawGoalieBody(pixels, 64, teamColor);

        // Blocker/glove arms
        DrawGoalieArms(pixels, 64, teamColor);

        // Mask (y = 40-54)
        DrawGoalieMask(pixels, 64, teamColor);

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Generate a 32x48 hockey stick sprite.
    /// </summary>
    public static Texture2D GenerateStickSprite()
    {
        Texture2D tex = new Texture2D(32, 48, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[32 * 48];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 0);

        // Shaft (diagonal line from top-left to center)
        for (int i = 0; i < 36; i++)
        {
            int x = 4 + i / 3;
            int y = 47 - i;
            if (x >= 0 && x < 32 && y >= 0 && y < 48)
            {
                SetPixelSafe(pixels, 32, x, y, StickShaftColor);
                SetPixelSafe(pixels, 32, x + 1, y, StickShaftColor);
            }
        }

        // Blade (bottom, horizontal curve)
        for (int x = 12; x < 28; x++)
        {
            int bladeY = 2 + (x < 20 ? 0 : (x - 20) / 2);
            SetPixelSafe(pixels, 32, x, bladeY, StickBladeColor);
            SetPixelSafe(pixels, 32, x, bladeY + 1, StickBladeColor);
            SetPixelSafe(pixels, 32, x, bladeY + 2, StickBladeColor);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Generate a 16x16 puck sprite.
    /// </summary>
    public static Texture2D GeneratePuckSprite()
    {
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 0);

        // Draw ellipse for isometric puck
        for (int y = 4; y < 12; y++)
        {
            for (int x = 2; x < 14; x++)
            {
                float dx = (x - 8) / 6f;
                float dy = (y - 8) / 3f;
                if (dx * dx + dy * dy <= 1f)
                {
                    // Highlight on top-left
                    Color32 color = (x < 7 && y > 7) ?
                        new Color32(50, 50, 50, 255) : PuckColor;
                    SetPixelSafe(pixels, 16, x, y, color);
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Generate a 64x64 referee sprite with striped jersey.
    /// </summary>
    public static Texture2D GenerateRefereeSprite(bool facingRight = true)
    {
        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 0);

        // Black pants
        DrawSkates(pixels, 64, facingRight);
        for (int y = 6; y < 18; y++)
        {
            for (int x = 26; x < 38; x++)
            {
                SetPixelSafe(pixels, 64, x, y, RefBlack);
            }
        }

        // Striped jersey (horizontal stripes)
        for (int y = 18; y < 38; y++)
        {
            bool isWhiteStripe = ((y - 18) / 3) % 2 == 0;
            Color32 stripeColor = isWhiteStripe ? RefWhite : RefBlack;
            for (int x = 24; x < 40; x++)
            {
                SetPixelSafe(pixels, 64, x, y, stripeColor);
            }
        }

        // Arms with stripes
        for (int y = 24; y < 34; y++)
        {
            bool isWhiteStripe = ((y - 18) / 3) % 2 == 0;
            Color32 stripeColor = isWhiteStripe ? RefWhite : RefBlack;
            // Left arm
            for (int x = 18; x < 24; x++)
                SetPixelSafe(pixels, 64, x, y, stripeColor);
            // Right arm
            for (int x = 40; x < 46; x++)
                SetPixelSafe(pixels, 64, x, y, stripeColor);
        }

        // Helmet (black)
        for (int y = 38; y < 50; y++)
        {
            for (int x = 26; x < 38; x++)
            {
                float dx = (x - 32) / 6f;
                float dy = (y - 44) / 6f;
                if (dx * dx + dy * dy <= 1f)
                {
                    SetPixelSafe(pixels, 64, x, y, HelmetColor);
                }
            }
        }

        // Face
        for (int y = 42; y < 48; y++)
        {
            for (int x = 28; x < 36; x++)
            {
                SetPixelSafe(pixels, 64, x, y, SkinColor);
            }
        }

        // Orange arm bands (referee identifier)
        for (int x = 18; x < 24; x++)
        {
            SetPixelSafe(pixels, 64, x, 32, RefOrange);
            SetPixelSafe(pixels, 64, x, 33, RefOrange);
        }
        for (int x = 40; x < 46; x++)
        {
            SetPixelSafe(pixels, 64, x, 32, RefOrange);
            SetPixelSafe(pixels, 64, x, 33, RefOrange);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    #region Player Parts Drawing

    private static void DrawSkates(Color32[] pixels, int width, bool facingRight)
    {
        // Left skate
        for (int y = 0; y < 6; y++)
        {
            for (int x = 26; x < 32; x++)
            {
                SetPixelSafe(pixels, width, x, y, SkateColor);
            }
        }
        // Right skate
        for (int y = 0; y < 6; y++)
        {
            for (int x = 32; x < 38; x++)
            {
                SetPixelSafe(pixels, width, x, y, SkateColor);
            }
        }
        // Blade highlights
        for (int x = 24; x < 40; x++)
        {
            SetPixelSafe(pixels, width, x, 0, IceHighlight);
        }
    }

    private static void DrawLegs(Color32[] pixels, int width, Color32 teamColor)
    {
        // Hockey pants (team color, slightly darker)
        Color32 pantsColor = DarkenColor(teamColor, 0.7f);
        for (int y = 6; y < 18; y++)
        {
            for (int x = 26; x < 38; x++)
            {
                SetPixelSafe(pixels, width, x, y, pantsColor);
            }
        }
    }

    private static void DrawJersey(Color32[] pixels, int width, Color32 teamColor, PlayerPosition position)
    {
        // Main jersey body
        for (int y = 18; y < 38; y++)
        {
            int bodyWidth = 16 - Mathf.Abs(y - 28) / 3;
            int startX = 32 - bodyWidth / 2;
            for (int x = startX; x < startX + bodyWidth; x++)
            {
                SetPixelSafe(pixels, width, x, y, teamColor);
            }
        }

        // Jersey number (simplified)
        int number = GetPositionNumber(position);
        DrawNumber(pixels, width, 29, 24, number, Color.white);
    }

    private static void DrawArms(Color32[] pixels, int width, Color32 teamColor, bool facingRight)
    {
        Color32 sleeveColor = teamColor;
        Color32 gloveColor = new Color32(60, 60, 60, 255);

        // Left arm
        for (int y = 22; y < 32; y++)
        {
            for (int x = 18; x < 26; x++)
            {
                Color32 color = y < 28 ? sleeveColor : gloveColor;
                SetPixelSafe(pixels, width, x, y, color);
            }
        }

        // Right arm (holding stick)
        for (int y = 22; y < 32; y++)
        {
            for (int x = 38; x < 46; x++)
            {
                Color32 color = y < 28 ? sleeveColor : gloveColor;
                SetPixelSafe(pixels, width, x, y, color);
            }
        }
    }

    private static void DrawHelmet(Color32[] pixels, int width)
    {
        // Helmet shell
        for (int y = 40; y < 52; y++)
        {
            for (int x = 26; x < 38; x++)
            {
                float dx = (x - 32) / 6f;
                float dy = (y - 46) / 6f;
                if (dx * dx + dy * dy <= 1f)
                {
                    SetPixelSafe(pixels, width, x, y, HelmetColor);
                }
            }
        }

        // Face/visor area
        for (int y = 42; y < 48; y++)
        {
            for (int x = 28; x < 36; x++)
            {
                SetPixelSafe(pixels, width, x, y, SkinColor);
            }
        }

        // Visor
        for (int x = 27; x < 37; x++)
        {
            SetPixelSafe(pixels, width, x, 48, VisorColor);
            SetPixelSafe(pixels, width, x, 49, VisorColor);
        }
    }

    #endregion

    #region Goalie Parts Drawing

    private static void DrawGoaliePads(Color32[] pixels, int width, Color32 padColor)
    {
        // Big leg pads
        for (int y = 0; y < 22; y++)
        {
            for (int x = 20; x < 44; x++)
            {
                SetPixelSafe(pixels, width, x, y, padColor);
            }
        }
        // Pad stripes
        for (int y = 2; y < 20; y += 4)
        {
            for (int x = 20; x < 44; x++)
            {
                SetPixelSafe(pixels, width, x, y, new Color32(100, 100, 100, 255));
            }
        }
    }

    private static void DrawGoalieBody(Color32[] pixels, int width, Color32 teamColor)
    {
        // Chest protector (team color)
        for (int y = 22; y < 42; y++)
        {
            for (int x = 22; x < 42; x++)
            {
                SetPixelSafe(pixels, width, x, y, teamColor);
            }
        }
    }

    private static void DrawGoalieArms(Color32[] pixels, int width, Color32 teamColor)
    {
        // Blocker (right arm)
        for (int y = 26; y < 38; y++)
        {
            for (int x = 42; x < 52; x++)
            {
                SetPixelSafe(pixels, width, x, y, new Color32(200, 200, 200, 255));
            }
        }

        // Glove/trapper (left arm)
        for (int y = 26; y < 40; y++)
        {
            for (int x = 12; x < 22; x++)
            {
                SetPixelSafe(pixels, width, x, y, teamColor);
            }
        }
        // Glove pocket
        for (int y = 30; y < 38; y++)
        {
            for (int x = 8; x < 16; x++)
            {
                SetPixelSafe(pixels, width, x, y, new Color32(220, 220, 220, 255));
            }
        }
    }

    private static void DrawGoalieMask(Color32[] pixels, int width, Color32 teamColor)
    {
        // Goalie mask (team color with cage)
        for (int y = 42; y < 56; y++)
        {
            for (int x = 24; x < 40; x++)
            {
                float dx = (x - 32) / 8f;
                float dy = (y - 49) / 7f;
                if (dx * dx + dy * dy <= 1f)
                {
                    SetPixelSafe(pixels, width, x, y, teamColor);
                }
            }
        }

        // Cage bars
        for (int y = 44; y < 52; y += 2)
        {
            for (int x = 26; x < 38; x++)
            {
                SetPixelSafe(pixels, width, x, y, new Color32(80, 80, 80, 255));
            }
        }
        for (int x = 28; x < 36; x += 2)
        {
            for (int y = 44; y < 52; y++)
            {
                SetPixelSafe(pixels, width, x, y, new Color32(80, 80, 80, 255));
            }
        }
    }

    #endregion

    #region Utility Methods

    private static void SetPixelSafe(Color32[] pixels, int width, int x, int y, Color32 color)
    {
        if (x >= 0 && x < width && y >= 0 && y < pixels.Length / width)
        {
            pixels[y * width + x] = color;
        }
    }

    private static Color32 DarkenColor(Color32 color, float factor)
    {
        return new Color32(
            (byte)(color.r * factor),
            (byte)(color.g * factor),
            (byte)(color.b * factor),
            color.a
        );
    }

    private static int GetPositionNumber(PlayerPosition position)
    {
        return position switch
        {
            PlayerPosition.Goalie => 1,
            PlayerPosition.LeftDefense => 4,
            PlayerPosition.RightDefense => 6,
            PlayerPosition.Center => 9,
            PlayerPosition.LeftWing => 11,
            PlayerPosition.RightWing => 17,
            _ => 0
        };
    }

    private static void DrawNumber(Color32[] pixels, int width, int startX, int startY, int number, Color32 color)
    {
        // Simple 3x5 pixel digits
        string numStr = number.ToString();
        int xOffset = 0;

        foreach (char c in numStr)
        {
            DrawDigit(pixels, width, startX + xOffset, startY, c - '0', color);
            xOffset += 4;
        }
    }

    private static void DrawDigit(Color32[] pixels, int width, int x, int y, int digit, Color32 color)
    {
        // 3x5 pixel font patterns for digits 0-9
        int[,] patterns = new int[10, 5]
        {
            {7, 5, 5, 5, 7}, // 0
            {2, 2, 2, 2, 2}, // 1
            {7, 1, 7, 4, 7}, // 2
            {7, 1, 7, 1, 7}, // 3
            {5, 5, 7, 1, 1}, // 4
            {7, 4, 7, 1, 7}, // 5
            {7, 4, 7, 5, 7}, // 6
            {7, 1, 1, 1, 1}, // 7
            {7, 5, 7, 5, 7}, // 8
            {7, 5, 7, 1, 7}  // 9
        };

        if (digit < 0 || digit > 9) return;

        for (int row = 0; row < 5; row++)
        {
            int pattern = patterns[digit, row];
            for (int col = 0; col < 3; col++)
            {
                if ((pattern & (4 >> col)) != 0)
                {
                    SetPixelSafe(pixels, width, x + col, y + (4 - row), color);
                }
            }
        }
    }

    #endregion
}
