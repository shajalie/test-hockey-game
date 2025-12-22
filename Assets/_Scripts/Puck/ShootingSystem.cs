using UnityEngine;
using System;

/// <summary>
/// Shooting system handling wrist shots, slap shots, and one-timers.
/// Integrates with player attributes for power/accuracy.
/// </summary>
public class ShootingSystem : MonoBehaviour
{
    #region Enums

    public enum ShotType
    {
        Wrist,      // Quick, accurate
        Slap,       // Powerful, wind-up
        Snap,       // Quick power shot
        Backhand,   // Deceptive
        OneTimer    // Redirected pass
    }

    #endregion

    #region Events

    /// <summary>Fired when charging begins.</summary>
    public event Action<ShotType> OnChargeStarted;

    /// <summary>Fired during charging (0-1 progress).</summary>
    public event Action<float> OnCharging;

    /// <summary>Fired when shot is released.</summary>
    public event Action<ShotType, float, Vector3> OnShotReleased;

    #endregion

    #region Serialized Fields

    [Header("Wrist Shot")]
    [SerializeField] private float wristMinPower = 15f;
    [SerializeField] private float wristMaxPower = 30f;
    [SerializeField] private float wristChargeTime = 0.4f;
    [SerializeField] private float wristAccuracyBonus = 0.2f;

    [Header("Slap Shot")]
    [SerializeField] private float slapMinPower = 25f;
    [SerializeField] private float slapMaxPower = 50f;
    [SerializeField] private float slapChargeTime = 0.8f;
    [SerializeField] private float slapWindUpTime = 0.3f;
    [SerializeField] private float slapAccuracyPenalty = -0.15f;

    [Header("Snap Shot")]
    [SerializeField] private float snapMinPower = 20f;
    [SerializeField] private float snapMaxPower = 40f;
    [SerializeField] private float snapChargeTime = 0.25f;

    [Header("Backhand")]
    [SerializeField] private float backhandMinPower = 12f;
    [SerializeField] private float backhandMaxPower = 25f;
    [SerializeField] private float backhandChargeTime = 0.35f;
    [SerializeField] private float backhandDeceptionBonus = 0.3f;

    [Header("One-Timer")]
    [SerializeField] private float oneTimerPowerBonus = 1.3f;
    [SerializeField] private float oneTimerWindow = 0.5f; // Time window to execute

    [Header("Accuracy")]
    [SerializeField] private float baseAccuracySpread = 5f; // Degrees
    [SerializeField] private float maxAccuracySpread = 15f;
    [SerializeField] private float movingAccuracyPenalty = 0.15f; // Per unit of speed

    [Header("Aiming")]
    [SerializeField] private float aimAssistStrength = 0.3f;
    [SerializeField] private float aimAssistRange = 20f;
    [SerializeField] private LayerMask goalLayer;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    #endregion

    #region Private Fields

    private PuckController puckController;
    private IcePhysicsController playerPhysics;
    private PlayerAttributes playerAttributes;

    // Shot state
    private bool isCharging;
    private ShotType currentShotType;
    private float chargeTimer;
    private float chargeProgress;
    private Vector2 aimInput;
    private Vector3 targetPosition;

    // One-timer state
    private bool oneTimerReady;
    private float oneTimerTimer;
    private Vector3 incomingPuckDirection;

    #endregion

    #region Properties

    /// <summary>Whether currently charging a shot.</summary>
    public bool IsCharging => isCharging;

    /// <summary>Current charge progress (0-1).</summary>
    public float ChargeProgress => chargeProgress;

    /// <summary>Current shot type being charged.</summary>
    public ShotType CurrentShotType => currentShotType;

    /// <summary>Whether one-timer is available.</summary>
    public bool CanOneTimer => oneTimerReady && puckController != null && !puckController.IsPossessed;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        puckController = FindObjectOfType<PuckController>();
    }

    private void Update()
    {
        UpdateCharging();
        UpdateOneTimerWindow();
    }

    #endregion

    #region Setup

    /// <summary>
    /// Set the player components for this shooting system.
    /// </summary>
    public void SetPlayer(IcePhysicsController physics, PlayerAttributes attributes)
    {
        playerPhysics = physics;
        playerAttributes = attributes;
    }

    /// <summary>
    /// Set the puck controller reference.
    /// </summary>
    public void SetPuckController(PuckController puck)
    {
        puckController = puck;
    }

    #endregion

    #region Aiming

    /// <summary>
    /// Set aim input (normalized direction).
    /// </summary>
    public void SetAimInput(Vector2 aim)
    {
        aimInput = aim;
    }

    /// <summary>
    /// Set a specific target position to aim at.
    /// </summary>
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }

    /// <summary>
    /// Get the current aim direction in world space.
    /// </summary>
    public Vector3 GetAimDirection()
    {
        Vector3 aimDir;

        if (aimInput.sqrMagnitude > 0.1f)
        {
            aimDir = new Vector3(aimInput.x, 0, aimInput.y).normalized;
        }
        else if (targetPosition != Vector3.zero && puckController != null)
        {
            aimDir = (targetPosition - puckController.transform.position).normalized;
            aimDir.y = 0;
        }
        else if (playerPhysics != null)
        {
            aimDir = playerPhysics.transform.forward;
        }
        else
        {
            aimDir = Vector3.forward;
        }

        // Apply aim assist toward goal
        aimDir = ApplyAimAssist(aimDir);

        return aimDir;
    }

    private Vector3 ApplyAimAssist(Vector3 aimDir)
    {
        if (puckController == null || aimAssistStrength <= 0) return aimDir;

        // Find nearest goal opening
        Vector3 puckPos = puckController.transform.position;
        Vector3 toGoal = FindNearestGoalTarget(puckPos) - puckPos;
        toGoal.y = 0;

        if (toGoal.magnitude > aimAssistRange) return aimDir;

        toGoal.Normalize();

        // Blend toward goal based on assist strength and distance
        float distanceFactor = 1f - (toGoal.magnitude / aimAssistRange);
        float assistAmount = aimAssistStrength * distanceFactor;

        return Vector3.Slerp(aimDir, toGoal, assistAmount).normalized;
    }

    private Vector3 FindNearestGoalTarget(Vector3 fromPos)
    {
        // Find goal colliders
        Collider[] goals = Physics.OverlapSphere(fromPos, 60f, goalLayer);

        Vector3 bestTarget = fromPos + Vector3.forward * 30f; // Default
        float bestScore = float.MinValue;

        foreach (var goal in goals)
        {
            // Get goal bounds
            Bounds bounds = goal.bounds;

            // Score based on angle (prefer shots we're facing)
            Vector3 toGoal = bounds.center - fromPos;
            toGoal.y = 0;

            float score = Vector3.Dot(GetAimDirection(), toGoal.normalized);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = bounds.center;
            }
        }

        return bestTarget;
    }

    #endregion

    #region Charging

    /// <summary>
    /// Start charging a shot.
    /// </summary>
    public void StartCharge(ShotType shotType)
    {
        if (puckController == null || !puckController.IsPossessed) return;
        if (puckController.Owner != playerPhysics?.gameObject) return;

        isCharging = true;
        currentShotType = shotType;
        chargeTimer = 0f;
        chargeProgress = 0f;

        OnChargeStarted?.Invoke(shotType);

        Debug.Log($"[ShootingSystem] Started charging {shotType}");
    }

    private void UpdateCharging()
    {
        if (!isCharging) return;

        float maxChargeTime = GetChargeTime(currentShotType);
        chargeTimer += Time.deltaTime;
        chargeProgress = Mathf.Clamp01(chargeTimer / maxChargeTime);

        OnCharging?.Invoke(chargeProgress);
    }

    /// <summary>
    /// Release the shot.
    /// </summary>
    public void ReleaseShot()
    {
        if (!isCharging) return;

        // Check minimum charge for slap shot wind-up
        if (currentShotType == ShotType.Slap && chargeTimer < slapWindUpTime)
        {
            CancelShot();
            return;
        }

        ExecuteShot(currentShotType, chargeProgress);

        isCharging = false;
        chargeTimer = 0f;
        chargeProgress = 0f;
    }

    /// <summary>
    /// Cancel the current shot.
    /// </summary>
    public void CancelShot()
    {
        isCharging = false;
        chargeTimer = 0f;
        chargeProgress = 0f;
    }

    private float GetChargeTime(ShotType type)
    {
        switch (type)
        {
            case ShotType.Wrist: return wristChargeTime;
            case ShotType.Slap: return slapChargeTime;
            case ShotType.Snap: return snapChargeTime;
            case ShotType.Backhand: return backhandChargeTime;
            default: return wristChargeTime;
        }
    }

    #endregion

    #region Shot Execution

    private void ExecuteShot(ShotType type, float charge)
    {
        if (puckController == null) return;

        // Calculate power
        float power = CalculateShotPower(type, charge);

        // Calculate direction with accuracy
        Vector3 direction = CalculateShotDirection(type);

        // Execute on puck
        puckController.Shoot(direction, power);

        OnShotReleased?.Invoke(type, power, direction);

        Debug.Log($"[ShootingSystem] {type} shot released - Power: {power:F1}, Charge: {charge:F2}");
    }

    private float CalculateShotPower(ShotType type, float charge)
    {
        float minPower, maxPower;

        switch (type)
        {
            case ShotType.Wrist:
                minPower = wristMinPower;
                maxPower = wristMaxPower;
                break;
            case ShotType.Slap:
                minPower = slapMinPower;
                maxPower = slapMaxPower;
                break;
            case ShotType.Snap:
                minPower = snapMinPower;
                maxPower = snapMaxPower;
                break;
            case ShotType.Backhand:
                minPower = backhandMinPower;
                maxPower = backhandMaxPower;
                break;
            default:
                minPower = wristMinPower;
                maxPower = wristMaxPower;
                break;
        }

        // Base power from charge
        float basePower = Mathf.Lerp(minPower, maxPower, charge);

        // Apply player attribute bonus
        float attributeBonus = 1f + (playerAttributes.ShotPower / 100f) * 0.5f;
        basePower *= attributeBonus;

        return basePower;
    }

    private Vector3 CalculateShotDirection(ShotType type)
    {
        Vector3 baseDirection = GetAimDirection();

        // Calculate accuracy spread
        float spread = CalculateAccuracySpread(type);

        // Apply random spread
        float spreadAngle = UnityEngine.Random.Range(-spread, spread);
        baseDirection = Quaternion.Euler(0, spreadAngle, 0) * baseDirection;

        return baseDirection.normalized;
    }

    private float CalculateAccuracySpread(ShotType type)
    {
        float baseSpread = baseAccuracySpread;

        // Shot type modifier
        switch (type)
        {
            case ShotType.Wrist:
                baseSpread *= (1f - wristAccuracyBonus);
                break;
            case ShotType.Slap:
                baseSpread *= (1f - slapAccuracyPenalty); // Negative = penalty
                break;
            case ShotType.Backhand:
                baseSpread *= 1.1f; // Slightly less accurate
                break;
        }

        // Moving penalty
        if (playerPhysics != null)
        {
            float speed = playerPhysics.CurrentSpeed;
            baseSpread += speed * movingAccuracyPenalty;
        }

        // Charge bonus (more charged = more accurate)
        baseSpread *= Mathf.Lerp(1.5f, 1f, chargeProgress);

        // Player attribute bonus
        float accuracyBonus = playerAttributes.ShotAccuracy / 100f;
        baseSpread *= (1f - accuracyBonus * 0.5f);

        return Mathf.Clamp(baseSpread, 0f, maxAccuracySpread);
    }

    #endregion

    #region One-Timer

    private void UpdateOneTimerWindow()
    {
        if (oneTimerReady)
        {
            oneTimerTimer -= Time.deltaTime;
            if (oneTimerTimer <= 0f)
            {
                oneTimerReady = false;
            }
        }
    }

    /// <summary>
    /// Called when a pass is incoming to this player.
    /// </summary>
    public void NotifyIncomingPass(Vector3 direction)
    {
        oneTimerReady = true;
        oneTimerTimer = oneTimerWindow;
        incomingPuckDirection = direction;
    }

    /// <summary>
    /// Execute a one-timer on an incoming puck.
    /// </summary>
    public bool ExecuteOneTimer()
    {
        if (!CanOneTimer || puckController == null) return false;

        // Calculate power (boosted by puck velocity)
        float incomingSpeed = puckController.Speed;
        float basePower = Mathf.Lerp(snapMinPower, snapMaxPower, 0.8f);
        float power = basePower * oneTimerPowerBonus + (incomingSpeed * 0.3f);

        // Direction
        Vector3 direction = GetAimDirection();

        // Execute
        puckController.OneTimer(playerPhysics?.gameObject, direction, power);

        oneTimerReady = false;
        OnShotReleased?.Invoke(ShotType.OneTimer, power, direction);

        Debug.Log($"[ShootingSystem] One-timer executed - Power: {power:F1}");

        return true;
    }

    #endregion

    #region Quick Shots

    /// <summary>
    /// Execute an instant wrist shot (no charging).
    /// </summary>
    public void QuickWristShot()
    {
        if (puckController == null || !puckController.IsPossessed) return;

        ExecuteShot(ShotType.Wrist, 0.5f);
    }

    /// <summary>
    /// Execute an instant snap shot.
    /// </summary>
    public void QuickSnapShot()
    {
        if (puckController == null || !puckController.IsPossessed) return;

        ExecuteShot(ShotType.Snap, 0.7f);
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;
        if (puckController == null) return;

        Vector3 puckPos = puckController.transform.position;

        // Draw aim direction
        Vector3 aimDir = GetAimDirection();
        Gizmos.color = isCharging ? Color.red : Color.yellow;
        Gizmos.DrawRay(puckPos, aimDir * 10f);

        // Draw accuracy cone when charging
        if (isCharging)
        {
            float spread = CalculateAccuracySpread(currentShotType);
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            Vector3 left = Quaternion.Euler(0, -spread, 0) * aimDir * 10f;
            Vector3 right = Quaternion.Euler(0, spread, 0) * aimDir * 10f;

            Gizmos.DrawLine(puckPos, puckPos + left);
            Gizmos.DrawLine(puckPos, puckPos + right);
        }

        // Draw charge progress
        if (isCharging)
        {
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, chargeProgress);
            Gizmos.DrawWireSphere(puckPos + Vector3.up * 2f, chargeProgress * 0.5f);
        }
    }

    #endregion
}
