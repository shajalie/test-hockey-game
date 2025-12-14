using UnityEngine;

/// <summary>
/// Base statistics for a hockey player. These are the default values
/// that get modified by RunModifier artifacts during gameplay.
/// </summary>
[CreateAssetMenu(fileName = "New Player Stats", menuName = "Hockey/Player Stats", order = 1)]
public class PlayerStats : ScriptableObject
{
    [Header("Movement - Physics Based")]
    [Tooltip("Maximum skating speed (units/second)")]
    public float maxSpeed = 25f;

    [Tooltip("Force applied for acceleration (Newtons)")]
    public float accelerationForce = 120f;

    [Tooltip("How quickly the player decelerates when not inputting (drag factor)")]
    public float brakingDrag = 3f;

    [Tooltip("Normal movement drag on ice")]
    public float iceDrag = 0.35f;

    [Tooltip("Speed reduction when carving/turning sharply (0-1)")]
    [Range(0f, 1f)]
    public float carvingSpeedPenalty = 0.15f;

    [Tooltip("Turn rate in degrees per second")]
    public float turnSpeed = 1100f;

    [Header("Puck Handling")]
    [Tooltip("Distance at which puck magnetizes to stick")]
    public float puckMagnetRange = 1.5f;

    [Tooltip("Force keeping puck attached to stick")]
    public float puckMagnetStrength = 8f;

    [Tooltip("Impact force required to knock puck loose")]
    public float puckKnockLooseThreshold = 5f;

    [Header("Shooting")]
    [Tooltip("Minimum shot power")]
    public float minShotPower = 10f;

    [Tooltip("Maximum shot power when fully charged")]
    public float maxShotPower = 35f;

    [Tooltip("Time to reach max charge (seconds)")]
    public float chargeTime = 1.5f;

    [Header("Checking/Dash")]
    [Tooltip("Force applied during body check")]
    public float checkForce = 15f;

    [Tooltip("Dash/sprint speed multiplier")]
    public float dashMultiplier = 2.0f;

    [Tooltip("Dash duration in seconds")]
    public float dashDuration = 0.3f;

    [Tooltip("Dash cooldown in seconds")]
    public float dashCooldown = 2f;
}
