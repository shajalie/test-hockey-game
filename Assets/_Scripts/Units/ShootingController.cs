using UnityEngine;
using System;

/// <summary>
/// Handles shot charging and execution for hockey players.
/// Supports both shooting and passing with charge mechanics.
/// </summary>
public class ShootingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HockeyPlayer player;
    [SerializeField] private Transform aimIndicator; // Optional visual
    private HockeyStick stick; // Stick animation integration

    [Header("Settings")]
    [SerializeField] private float minChargeTime = 0.1f;
    [SerializeField] private float passForce = 15f;

    // State
    private bool isCharging;
    private float chargeStartTime;
    private float currentCharge; // 0-1 normalized
    private Vector2 aimDirection;
    private Puck targetPuck;

    // Events for UI feedback
    public event Action<float> OnChargeChanged; // 0-1 charge level
    public event Action OnShotReleased;

    // Properties
    public float CurrentCharge => currentCharge;
    public bool IsCharging => isCharging;
    public Vector3 AimDirection3D => new Vector3(aimDirection.x, 0f, aimDirection.y).normalized;

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<HockeyPlayer>();
        }

        // Get stick component for animations
        stick = GetComponent<HockeyStick>();
    }

    private void Update()
    {
        if (isCharging)
        {
            UpdateCharge();
        }

        UpdateAimIndicator();
    }

    /// <summary>
    /// Set the aim direction (from right stick or touch).
    /// </summary>
    public void SetAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.1f)
        {
            aimDirection = direction.normalized;
        }
        else if (player != null)
        {
            // Default to player facing direction
            aimDirection = new Vector2(player.transform.forward.x, player.transform.forward.z);
        }
    }

    /// <summary>
    /// Start charging a shot.
    /// </summary>
    public void StartCharge()
    {
        if (!player.HasPuck) return;

        isCharging = true;
        chargeStartTime = Time.time;
        currentCharge = 0f;

        // Find the puck
        targetPuck = FindObjectOfType<Puck>();

        // Trigger stick shooting animation (wind-up)
        if (stick != null)
        {
            stick.SetAnimationState(StickAnimationState.Shooting);
        }

        Debug.Log("[ShootingController] Charging shot...");
    }

    /// <summary>
    /// Release the shot with current charge level.
    /// </summary>
    public void ReleaseShot()
    {
        if (!isCharging) return;

        isCharging = false;

        if (!player.HasPuck || targetPuck == null)
        {
            currentCharge = 0f;
            OnChargeChanged?.Invoke(0f);
            return;
        }

        // Only shoot if charged enough
        if (currentCharge < 0.05f)
        {
            currentCharge = 0f;
            OnChargeChanged?.Invoke(0f);
            return;
        }

        // Calculate shot power based on charge
        float power = Mathf.Lerp(
            player.Stats.MinShotPower,
            player.Stats.MaxShotPower,
            currentCharge
        );

        // Get aim direction (3D)
        Vector3 shotDirection = AimDirection3D;

        // If no aim input, shoot forward
        if (shotDirection.sqrMagnitude < 0.1f)
        {
            shotDirection = player.transform.forward;
        }

        // Execute shot
        targetPuck.Shoot(shotDirection, power);

        // Trigger stick release animation
        if (stick != null)
        {
            stick.TriggerShootAnimation();
        }

        OnShotReleased?.Invoke();
        Debug.Log($"[ShootingController] Shot released! Charge: {currentCharge:F2}, Power: {power:F1}");

        currentCharge = 0f;
        OnChargeChanged?.Invoke(0f);
    }

    /// <summary>
    /// Execute a pass toward a target position or direction.
    /// </summary>
    public void Pass(Vector3? targetPosition = null)
    {
        if (!player.HasPuck) return;

        targetPuck = FindObjectOfType<Puck>();
        if (targetPuck == null) return;

        if (targetPosition.HasValue)
        {
            targetPuck.Pass(targetPosition.Value, passForce);
        }
        else
        {
            // Pass in aim direction
            Vector3 passTarget = transform.position + AimDirection3D * 10f;
            targetPuck.Pass(passTarget, passForce);
        }

        // Trigger stick pass animation
        if (stick != null)
        {
            stick.TriggerPassAnimation();
        }

        Debug.Log("[ShootingController] Pass executed");
    }

    /// <summary>
    /// Cancel charging without shooting.
    /// </summary>
    public void CancelCharge()
    {
        isCharging = false;
        currentCharge = 0f;
        OnChargeChanged?.Invoke(0f);

        // Return stick to skating position
        if (stick != null)
        {
            stick.SetAnimationState(StickAnimationState.Skating);
        }
    }

    private void UpdateCharge()
    {
        if (player.Stats == null) return;

        float chargeTime = Time.time - chargeStartTime;
        currentCharge = Mathf.Clamp01(chargeTime / player.Stats.ChargeTime);

        OnChargeChanged?.Invoke(currentCharge);

        // Lose puck if charging too long (optional balance mechanic)
        // if (chargeTime > player.Stats.ChargeTime * 1.5f)
        // {
        //     CancelCharge();
        // }
    }

    private void UpdateAimIndicator()
    {
        if (aimIndicator == null) return;

        // Show indicator only when player has puck
        aimIndicator.gameObject.SetActive(player.HasPuck);

        if (player.HasPuck)
        {
            // Point indicator in aim direction
            if (AimDirection3D.sqrMagnitude > 0.1f)
            {
                aimIndicator.rotation = Quaternion.LookRotation(AimDirection3D);
            }

            // Scale based on charge
            float scale = isCharging ? (1f + currentCharge * 0.5f) : 1f;
            aimIndicator.localScale = Vector3.one * scale;
        }
    }

    // === DEBUG ===

    private void OnDrawGizmos()
    {
        if (player == null || !player.HasPuck) return;

        // Draw aim direction
        Gizmos.color = isCharging ? Color.Lerp(Color.yellow, Color.red, currentCharge) : Color.white;
        Vector3 start = transform.position + Vector3.up * 0.5f;
        float length = isCharging ? (2f + currentCharge * 3f) : 2f;
        Gizmos.DrawRay(start, AimDirection3D * length);

        // Draw charge arc
        if (isCharging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(start + AimDirection3D * length, 0.2f + currentCharge * 0.3f);
        }
    }
}
