using UnityEngine;

/// <summary>
/// Global ice physics settings for tuning the skating feel.
/// Adjust these to get the "slippery but responsive" arcade feel.
/// </summary>
[CreateAssetMenu(fileName = "IcePhysicsSettings", menuName = "Hockey/Ice Physics Settings", order = 0)]
public class IcePhysicsSettings : ScriptableObject
{
    [Header("Attribute Conversion")]
    [Tooltip("Multiplier to convert 0-100 attribute to actual max speed (units/sec)")]
    public float speedAttributeScale = 0.4f; // 50 attr = 20 u/s, 100 attr = 40 u/s

    [Tooltip("Multiplier to convert 0-100 attribute to force (Newtons)")]
    public float accelerationAttributeScale = 3f; // 50 attr = 150 N

    [Tooltip("Multiplier to convert 0-100 attribute to turn rate (deg/sec)")]
    public float agilityAttributeScale = 15f; // 50 attr = 750 deg/s

    [Tooltip("Multiplier to convert 0-100 attribute to knockback resistance")]
    public float balanceAttributeScale = 0.02f; // Higher = more stable

    [Header("Ice Friction - The Glide")]
    [Tooltip("Base linear drag on ice (lower = more slippery). This is the core of 'The Glide'.")]
    [Range(0f, 2f)]
    public float baseDrag = 0.3f;

    [Tooltip("Per-frame velocity multiplier when coasting (no input). 1.0 = no friction, 0.9 = heavy friction.")]
    [Range(0.9f, 1f)]
    public float coastingFriction = 0.985f;

    [Tooltip("Drag multiplier when actively braking/stopping")]
    [Range(1f, 10f)]
    public float brakingDragMultiplier = 4f;

    [Header("Turning - Edge Bite")]
    [Tooltip("Force applied sideways to simulate skate edge 'bite' during turns")]
    [Range(0f, 500f)]
    public float edgeBiteForce = 150f;

    [Tooltip("Speed reduction multiplier when turning sharply (0 = full penalty, 1 = no penalty)")]
    [Range(0f, 1f)]
    public float turnSpeedRetention = 0.85f;

    [Tooltip("Angle threshold (degrees) above which turn penalty applies")]
    [Range(0f, 90f)]
    public float turnPenaltyAngle = 45f;

    [Tooltip("How quickly rotation follows input (lower = more ice-like drift)")]
    [Range(0f, 1f)]
    public float rotationSnappiness = 0.15f;

    [Header("Acceleration Curves")]
    [Tooltip("Acceleration curve: X = current speed ratio (0-1), Y = acceleration multiplier")]
    public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f);

    [Tooltip("How much current speed affects acceleration (higher = harder to speed up when fast)")]
    [Range(0f, 1f)]
    public float speedResistance = 0.6f;

    [Header("Sprint/Boost")]
    [Tooltip("Speed multiplier when sprinting")]
    [Range(1f, 2f)]
    public float sprintSpeedMultiplier = 1.4f;

    [Tooltip("Acceleration multiplier when sprinting")]
    [Range(1f, 3f)]
    public float sprintAccelerationMultiplier = 1.6f;

    [Header("Dash/Check")]
    [Tooltip("Impulse force multiplier for dash")]
    [Range(0f, 200f)]
    public float dashImpulse = 80f;

    [Tooltip("Duration of dash boost (seconds)")]
    [Range(0.1f, 0.5f)]
    public float dashDuration = 0.25f;

    [Tooltip("Dash cooldown (seconds)")]
    [Range(0.5f, 5f)]
    public float dashCooldown = 1.5f;

    [Header("Mass & Physics")]
    [Tooltip("Player mass in kg (affects momentum and collisions)")]
    [Range(50f, 120f)]
    public float playerMass = 80f;

    [Tooltip("Angular drag to prevent spinning")]
    [Range(0f, 10f)]
    public float angularDrag = 5f;

    [Header("Ground Detection")]
    [Tooltip("Distance to check for ground below player")]
    [Range(0.1f, 1f)]
    public float groundCheckDistance = 0.3f;

    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer = ~0;

    /// <summary>
    /// Convert skating speed attribute (0-100) to actual max speed in units/sec.
    /// </summary>
    public float GetMaxSpeed(float skatingSpeedAttribute)
    {
        return skatingSpeedAttribute * speedAttributeScale;
    }

    /// <summary>
    /// Convert acceleration attribute (0-100) to actual force in Newtons.
    /// </summary>
    public float GetAccelerationForce(float accelerationAttribute)
    {
        return accelerationAttribute * accelerationAttributeScale;
    }

    /// <summary>
    /// Convert agility attribute (0-100) to turn rate in degrees/sec.
    /// </summary>
    public float GetTurnRate(float agilityAttribute)
    {
        return agilityAttribute * agilityAttributeScale;
    }

    /// <summary>
    /// Get knockback resistance multiplier from balance attribute (0-100).
    /// Higher balance = less knockback received.
    /// </summary>
    public float GetKnockbackResistance(float balanceAttribute)
    {
        return 1f - (balanceAttribute * balanceAttributeScale);
    }

    /// <summary>
    /// Calculate acceleration multiplier based on current speed ratio.
    /// </summary>
    public float GetAccelerationMultiplier(float currentSpeedRatio)
    {
        return accelerationCurve.Evaluate(currentSpeedRatio);
    }

    /// <summary>
    /// Calculate turn speed retention based on turn angle.
    /// </summary>
    public float GetTurnSpeedRetention(float turnAngle)
    {
        if (turnAngle <= turnPenaltyAngle)
        {
            return 1f; // No penalty for gentle turns
        }

        // Linear interpolation from 1.0 to turnSpeedRetention as angle goes from threshold to 180
        float t = (turnAngle - turnPenaltyAngle) / (180f - turnPenaltyAngle);
        return Mathf.Lerp(1f, turnSpeedRetention, t);
    }
}
