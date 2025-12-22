using UnityEngine;
using System;

/// <summary>
/// Core player attributes that affect skating and shooting.
/// These are the base values that can be modified by buffs/artifacts.
/// Values are normalized 0-100 for easy balancing.
/// </summary>
[Serializable]
public struct PlayerAttributes
{
    [Header("Skating")]
    [Tooltip("Maximum skating speed (0-100)")]
    [Range(0, 100)]
    public float SkatingSpeed;

    [Tooltip("How quickly the player accelerates (0-100)")]
    [Range(0, 100)]
    public float Acceleration;

    [Tooltip("Turning ability and edge control (0-100)")]
    [Range(0, 100)]
    public float Agility;

    [Tooltip("Resistance to being knocked off balance (0-100)")]
    [Range(0, 100)]
    public float Balance;

    [Header("Shooting")]
    [Tooltip("Shot power/velocity (0-100)")]
    [Range(0, 100)]
    public float ShotPower;

    [Tooltip("Shot accuracy/precision (0-100)")]
    [Range(0, 100)]
    public float ShotAccuracy;

    /// <summary>
    /// Create default balanced attributes.
    /// </summary>
    public static PlayerAttributes Default => new PlayerAttributes
    {
        SkatingSpeed = 50f,
        Acceleration = 50f,
        Agility = 50f,
        Balance = 50f,
        ShotPower = 50f,
        ShotAccuracy = 50f
    };

    /// <summary>
    /// Create a speedy skater build.
    /// </summary>
    public static PlayerAttributes Speedster => new PlayerAttributes
    {
        SkatingSpeed = 85f,
        Acceleration = 75f,
        Agility = 70f,
        Balance = 35f,
        ShotPower = 40f,
        ShotAccuracy = 45f
    };

    /// <summary>
    /// Create a power forward build.
    /// </summary>
    public static PlayerAttributes PowerForward => new PlayerAttributes
    {
        SkatingSpeed = 45f,
        Acceleration = 50f,
        Agility = 40f,
        Balance = 85f,
        ShotPower = 80f,
        ShotAccuracy = 55f
    };

    /// <summary>
    /// Create a sniper build.
    /// </summary>
    public static PlayerAttributes Sniper => new PlayerAttributes
    {
        SkatingSpeed = 55f,
        Acceleration = 50f,
        Agility = 60f,
        Balance = 45f,
        ShotPower = 65f,
        ShotAccuracy = 90f
    };

    /// <summary>
    /// Create a playmaker build.
    /// </summary>
    public static PlayerAttributes Playmaker => new PlayerAttributes
    {
        SkatingSpeed = 60f,
        Acceleration = 65f,
        Agility = 85f,
        Balance = 50f,
        ShotPower = 45f,
        ShotAccuracy = 60f
    };

    /// <summary>
    /// Lerp between two attribute sets.
    /// </summary>
    public static PlayerAttributes Lerp(PlayerAttributes a, PlayerAttributes b, float t)
    {
        return new PlayerAttributes
        {
            SkatingSpeed = Mathf.Lerp(a.SkatingSpeed, b.SkatingSpeed, t),
            Acceleration = Mathf.Lerp(a.Acceleration, b.Acceleration, t),
            Agility = Mathf.Lerp(a.Agility, b.Agility, t),
            Balance = Mathf.Lerp(a.Balance, b.Balance, t),
            ShotPower = Mathf.Lerp(a.ShotPower, b.ShotPower, t),
            ShotAccuracy = Mathf.Lerp(a.ShotAccuracy, b.ShotAccuracy, t)
        };
    }

    /// <summary>
    /// Apply a multiplier to all attributes.
    /// </summary>
    public PlayerAttributes WithMultiplier(float multiplier)
    {
        return new PlayerAttributes
        {
            SkatingSpeed = Mathf.Clamp(SkatingSpeed * multiplier, 0f, 100f),
            Acceleration = Mathf.Clamp(Acceleration * multiplier, 0f, 100f),
            Agility = Mathf.Clamp(Agility * multiplier, 0f, 100f),
            Balance = Mathf.Clamp(Balance * multiplier, 0f, 100f),
            ShotPower = Mathf.Clamp(ShotPower * multiplier, 0f, 100f),
            ShotAccuracy = Mathf.Clamp(ShotAccuracy * multiplier, 0f, 100f)
        };
    }

    public override string ToString()
    {
        return $"SPD:{SkatingSpeed:F0} ACC:{Acceleration:F0} AGI:{Agility:F0} BAL:{Balance:F0} PWR:{ShotPower:F0} ACU:{ShotAccuracy:F0}";
    }
}
