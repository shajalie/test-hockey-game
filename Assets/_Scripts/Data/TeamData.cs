using UnityEngine;

/// <summary>
/// ScriptableObject containing all configuration data for a hockey team.
/// Defines team identity, colors, and player composition.
/// </summary>
[CreateAssetMenu(fileName = "New Team", menuName = "Hockey/Team Data", order = 2)]
public class TeamData : ScriptableObject
{
    [Header("Team Identity")]
    [Tooltip("Team name displayed in UI")]
    public string teamName = "New Team";

    [Tooltip("Team abbreviation (3 letters)")]
    public string abbreviation = "TEM";

    [Header("Team Colors")]
    [Tooltip("Primary team color (jerseys, UI)")]
    public Color primaryColor = Color.blue;

    [Tooltip("Secondary team color (trim, accents)")]
    public Color secondaryColor = Color.white;

    [Tooltip("Goalie jersey color")]
    public Color goalieColor = Color.red;

    [Header("Team Composition")]
    [Tooltip("Number of skaters on ice (3-5, default 5)")]
    [Range(3, 5)]
    public int skatersOnIce = 5;

    [Tooltip("Always 1 goalie per team")]
    public int goalies = 1;

    [Header("Player Stats")]
    [Tooltip("Base stats for skaters on this team")]
    public PlayerStats skaterStats;

    [Tooltip("Base stats for goalie (usually different speed/handling)")]
    public PlayerStats goalieStats;

    [Header("Formation Settings")]
    [Tooltip("Default formation for this team")]
    public FormationType defaultFormation = FormationType.Balanced;

    [Tooltip("How aggressive the team plays (0-1, affects AI positioning)")]
    [Range(0f, 1f)]
    public float aggressiveness = 0.5f;

    [Tooltip("AI difficulty for this team (0-1)")]
    [Range(0f, 1f)]
    public float aiDifficulty = 0.5f;

    [Header("Player Prefabs")]
    [Tooltip("Prefab to spawn for skaters")]
    public GameObject skaterPrefab;

    [Tooltip("Prefab to spawn for goalie")]
    public GameObject goaliePrefab;

    // Computed properties
    public int TotalPlayers => skatersOnIce + goalies;

    /// <summary>
    /// Get default position offset for a player based on their role and formation.
    /// </summary>
    public Vector3 GetPositionOffset(PlayerPosition position, bool defendingLeftSide)
    {
        float direction = defendingLeftSide ? -1f : 1f;

        switch (position)
        {
            case PlayerPosition.Goalie:
                return new Vector3(direction * 26f, 0f, 0f); // Near goal

            case PlayerPosition.LeftDefense:
                return new Vector3(direction * 18f, 0f, -4f);

            case PlayerPosition.RightDefense:
                return new Vector3(direction * 18f, 0f, 4f);

            case PlayerPosition.Center:
                return new Vector3(direction * 10f, 0f, 0f);

            case PlayerPosition.LeftWing:
                return new Vector3(direction * 8f, 0f, -6f);

            case PlayerPosition.RightWing:
                return new Vector3(direction * 8f, 0f, 6f);

            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// Validate team data on changes.
    /// </summary>
    private void OnValidate()
    {
        // Ensure abbreviation is exactly 3 characters
        if (abbreviation.Length != 3)
        {
            abbreviation = abbreviation.Length > 3
                ? abbreviation.Substring(0, 3)
                : abbreviation.PadRight(3, 'X');
        }

        abbreviation = abbreviation.ToUpper();
    }
}

/// <summary>
/// Player positions on the ice.
/// </summary>
public enum PlayerPosition
{
    Goalie,
    LeftDefense,
    RightDefense,
    Center,
    LeftWing,
    RightWing
}

/// <summary>
/// Team formation types affecting AI positioning.
/// </summary>
public enum FormationType
{
    Defensive,      // Drop back, protect goal
    Balanced,       // Standard positioning
    Offensive,      // Push forward, aggressive
    TrapDefense,    // Neutral zone trap
    Forecheck       // Aggressive forechecking
}
