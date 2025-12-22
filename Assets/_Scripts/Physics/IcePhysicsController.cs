using UnityEngine;

/// <summary>
/// Physics-based ice skating controller.
/// Provides "slippery but responsive" feel through:
/// - Low drag for The Glide
/// - AddForce for smooth acceleration
/// - Edge Bite (sideways force) for realistic turning
/// - Per-frame velocity reduction for fine friction control
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class IcePhysicsController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Settings")]
    [SerializeField] private IcePhysicsSettings settings;

    [Header("Player Attributes")]
    [SerializeField] private PlayerAttributes attributes = PlayerAttributes.Default;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugUI = false;

    #endregion

    #region Private Fields

    private Rigidbody rb;
    private CapsuleCollider capsule;

    // Input state
    private Vector2 moveInput;
    private bool sprintInput;
    private bool brakeInput;

    // Dash state
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    // Calculated physics values
    private float maxSpeed;
    private float accelerationForce;
    private float turnRate;
    private float knockbackResistance;

    // State tracking
    private Vector3 lastVelocity;
    private float currentSpeedRatio;
    private bool isGrounded;

    #endregion

    #region Properties

    /// <summary>
    /// Current horizontal velocity magnitude.
    /// </summary>
    public float CurrentSpeed => GetHorizontalVelocity().magnitude;

    /// <summary>
    /// Current velocity as ratio of max speed (0-1).
    /// </summary>
    public float SpeedRatio => currentSpeedRatio;

    /// <summary>
    /// Whether the player is currently sprinting.
    /// </summary>
    public bool IsSprinting => sprintInput && moveInput.sqrMagnitude > 0.1f;

    /// <summary>
    /// Whether the player is currently dashing.
    /// </summary>
    public bool IsDashing => isDashing;

    /// <summary>
    /// Whether the player is grounded.
    /// </summary>
    public bool IsGrounded => isGrounded;

    /// <summary>
    /// The rigidbody velocity.
    /// </summary>
    public Vector3 Velocity => rb.linearVelocity;

    /// <summary>
    /// Current player attributes.
    /// </summary>
    public PlayerAttributes Attributes => attributes;

    /// <summary>
    /// Current effective max speed (including sprint).
    /// </summary>
    public float EffectiveMaxSpeed => maxSpeed * (IsSprinting ? settings.sprintSpeedMultiplier : 1f);

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        ValidateSettings();
        ConfigureRigidbody();
        RecalculateStats();
    }

    private void Update()
    {
        UpdateDashTimers();
    }

    private void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        if (!isGrounded)
        {
            // Apply gravity and skip ice physics when airborne
            return;
        }

        // Core physics update
        ApplyAcceleration();
        ApplyEdgeBite();
        ApplyIceFriction();
        ClampVelocity();
        UpdateRotation();

        // Track state
        lastVelocity = rb.linearVelocity;
        currentSpeedRatio = CurrentSpeed / EffectiveMaxSpeed;
    }

    #endregion

    #region Initialization

    private void ValidateSettings()
    {
        if (settings == null)
        {
            Debug.LogWarning($"[IcePhysicsController] No settings assigned on {gameObject.name}. Creating default settings.");
            settings = ScriptableObject.CreateInstance<IcePhysicsSettings>();
        }
    }

    private void ConfigureRigidbody()
    {
        rb.mass = settings.playerMass;
        rb.linearDamping = settings.baseDrag;
        rb.angularDamping = settings.angularDrag;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    /// <summary>
    /// Recalculate physics values from attributes.
    /// Call this when attributes change.
    /// </summary>
    public void RecalculateStats()
    {
        maxSpeed = settings.GetMaxSpeed(attributes.SkatingSpeed);
        accelerationForce = settings.GetAccelerationForce(attributes.Acceleration);
        turnRate = settings.GetTurnRate(attributes.Agility);
        knockbackResistance = settings.GetKnockbackResistance(attributes.Balance);

        Debug.Log($"[IcePhysicsController] Stats: MaxSpeed={maxSpeed:F1} AccelForce={accelerationForce:F1} TurnRate={turnRate:F1}");
    }

    #endregion

    #region Input

    /// <summary>
    /// Set movement input (normalized -1 to 1 on each axis).
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        moveInput = Vector2.ClampMagnitude(input, 1f);
    }

    /// <summary>
    /// Set sprint input state.
    /// </summary>
    public void SetSprintInput(bool sprinting)
    {
        sprintInput = sprinting;
    }

    /// <summary>
    /// Set brake/stop input state.
    /// </summary>
    public void SetBrakeInput(bool braking)
    {
        brakeInput = braking;
    }

    /// <summary>
    /// Trigger a dash in the current movement direction.
    /// </summary>
    public void TriggerDash()
    {
        if (dashCooldownTimer > 0f) return;

        Vector3 dashDir = GetDashDirection();

        // Apply impulse
        rb.AddForce(dashDir * settings.dashImpulse, ForceMode.Impulse);

        // Set dash state
        isDashing = true;
        dashTimer = settings.dashDuration;
        dashCooldownTimer = settings.dashCooldown;

        Debug.Log($"[IcePhysicsController] DASH! Direction: {dashDir}");
    }

    /// <summary>
    /// Update player attributes.
    /// </summary>
    public void SetAttributes(PlayerAttributes newAttributes)
    {
        attributes = newAttributes;
        RecalculateStats();
    }

    #endregion

    #region Core Physics

    /// <summary>
    /// Apply acceleration force in the input direction.
    /// This creates the responsive feel.
    /// </summary>
    private void ApplyAcceleration()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        // Convert 2D input to 3D direction
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        // Calculate effective acceleration
        float effectiveAccel = accelerationForce;

        // Sprint boost
        if (IsSprinting)
        {
            effectiveAccel *= settings.sprintAccelerationMultiplier;
        }

        // Dash boost (sustained during dash)
        if (isDashing)
        {
            effectiveAccel *= 1.5f;
        }

        // Reduce acceleration at higher speeds (realistic skating)
        float speedMultiplier = settings.GetAccelerationMultiplier(currentSpeedRatio);
        effectiveAccel *= Mathf.Lerp(1f, speedMultiplier, settings.speedResistance);

        // Apply turn penalty
        Vector3 currentDir = GetHorizontalVelocity().normalized;
        if (currentDir.sqrMagnitude > 0.01f)
        {
            float turnAngle = Vector3.Angle(currentDir, inputDirection);
            float turnRetention = settings.GetTurnSpeedRetention(turnAngle);
            effectiveAccel *= turnRetention;
        }

        // Apply force in input direction
        Vector3 force = inputDirection * effectiveAccel;
        rb.AddForce(force, ForceMode.Force);
    }

    /// <summary>
    /// Apply sideways force to simulate skate edge "bite" during turns.
    /// This makes turning feel more realistic - you carve, not spin.
    /// </summary>
    private void ApplyEdgeBite()
    {
        if (moveInput.sqrMagnitude < 0.1f) return;

        Vector3 velocity = GetHorizontalVelocity();
        if (velocity.magnitude < 1f) return;

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        Vector3 velocityDirection = velocity.normalized;

        // Calculate sideways component of desired direction change
        Vector3 desiredChange = inputDirection - velocityDirection;

        // Get the sideways (perpendicular) component
        Vector3 sideways = Vector3.Cross(Vector3.up, velocityDirection);
        float sidewaysAmount = Vector3.Dot(desiredChange, sideways);

        // Apply edge bite force proportional to turn sharpness and speed
        float speedFactor = Mathf.Clamp01(velocity.magnitude / maxSpeed);
        float edgeForce = sidewaysAmount * settings.edgeBiteForce * speedFactor;

        // Scale by agility attribute
        edgeForce *= (attributes.Agility / 50f); // 50 = baseline

        rb.AddForce(sideways * edgeForce, ForceMode.Force);
    }

    /// <summary>
    /// Apply ice friction using per-frame velocity reduction.
    /// This gives us fine control over "The Glide" feel.
    /// </summary>
    private void ApplyIceFriction()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);

        // Different friction based on input state
        if (brakeInput)
        {
            // Hockey stop - high friction
            rb.linearDamping = settings.baseDrag * settings.brakingDragMultiplier;
        }
        else if (moveInput.sqrMagnitude < 0.1f)
        {
            // Coasting - apply per-frame friction for smooth glide
            horizontalVel *= settings.coastingFriction;
            rb.linearVelocity = new Vector3(horizontalVel.x, velocity.y, horizontalVel.z);
            rb.linearDamping = settings.baseDrag;
        }
        else
        {
            // Active skating - minimal drag
            rb.linearDamping = settings.baseDrag * 0.5f;
        }
    }

    /// <summary>
    /// Clamp velocity to max speed, preserving direction.
    /// </summary>
    private void ClampVelocity()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);

        float effectiveMax = EffectiveMaxSpeed;
        if (isDashing)
        {
            effectiveMax *= 1.5f; // Allow exceeding max during dash
        }

        if (horizontalVel.magnitude > effectiveMax)
        {
            horizontalVel = horizontalVel.normalized * effectiveMax;
            rb.linearVelocity = new Vector3(horizontalVel.x, velocity.y, horizontalVel.z);
        }

        // Clamp Y velocity to prevent bouncing
        if (Mathf.Abs(velocity.y) > 5f)
        {
            velocity.y = Mathf.Sign(velocity.y) * 5f;
            rb.linearVelocity = velocity;
        }
    }

    /// <summary>
    /// Update rotation to face input direction.
    /// Uses interpolation for smooth, ice-like turning.
    /// </summary>
    private void UpdateRotation()
    {
        Vector3 targetDirection = Vector3.zero;

        // Determine target direction
        if (moveInput.sqrMagnitude > 0.1f)
        {
            // Face input direction (responsive feel)
            targetDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }
        else if (GetHorizontalVelocity().magnitude > 1f)
        {
            // Face velocity when coasting
            targetDirection = GetHorizontalVelocity().normalized;
        }

        if (targetDirection.sqrMagnitude < 0.1f) return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        // Interpolate rotation based on agility and snappiness setting
        float rotationSpeed = turnRate * settings.rotationSnappiness;

        // Faster rotation at low speeds (can pivot easily when slow)
        float speedFactor = Mathf.Lerp(2f, 1f, currentSpeedRatio);
        rotationSpeed *= speedFactor;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );
    }

    #endregion

    #region Dash

    private void UpdateDashTimers()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    private Vector3 GetDashDirection()
    {
        if (moveInput.sqrMagnitude > 0.1f)
        {
            return new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }
        return transform.forward;
    }

    /// <summary>
    /// Check if dash is available (off cooldown).
    /// </summary>
    public bool CanDash => dashCooldownTimer <= 0f;

    /// <summary>
    /// Get dash cooldown remaining (0-1 ratio).
    /// </summary>
    public float DashCooldownRatio => dashCooldownTimer / settings.dashCooldown;

    #endregion

    #region Utility

    private Vector3 GetHorizontalVelocity()
    {
        return new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    private bool CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, settings.groundCheckDistance + 0.1f, settings.groundLayer);
    }

    /// <summary>
    /// Apply external knockback force (from body checks, etc).
    /// Knockback is reduced by Balance attribute.
    /// </summary>
    public void ApplyKnockback(Vector3 force)
    {
        // Reduce knockback based on balance
        Vector3 reducedForce = force * knockbackResistance;
        rb.AddForce(reducedForce, ForceMode.Impulse);

        Debug.Log($"[IcePhysicsController] Knockback: Original={force.magnitude:F1} Reduced={reducedForce.magnitude:F1}");
    }

    /// <summary>
    /// Stop all momentum immediately.
    /// </summary>
    public void StopMovement()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Add velocity directly (for special moves, spawning, etc).
    /// </summary>
    public void AddVelocity(Vector3 velocity)
    {
        rb.linearVelocity += velocity;
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || rb == null) return;

        Vector3 pos = transform.position + Vector3.up * 0.5f;

        // Velocity (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(pos, rb.linearVelocity);

        // Input direction (green)
        Gizmos.color = Color.green;
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y) * 3f;
        Gizmos.DrawRay(pos, inputDir);

        // Forward (red)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(pos, transform.forward * 2f);

        // Dash indicator (cyan sphere)
        if (isDashing)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pos + Vector3.up, 0.3f);
        }

        // Sprint indicator (yellow sphere)
        if (IsSprinting)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos + Vector3.up * 1.5f, 0.2f);
        }

        // Ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f,
                        transform.position + Vector3.up * 0.1f + Vector3.down * (settings?.groundCheckDistance ?? 0.3f + 0.1f));
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.alignment = TextAnchor.UpperLeft;

        string text = $"[Ice Physics]\n";
        text += $"Speed: {CurrentSpeed:F1} / {EffectiveMaxSpeed:F1}\n";
        text += $"Speed Ratio: {currentSpeedRatio:F2}\n";
        text += $"Sprinting: {IsSprinting}\n";
        text += $"Dashing: {isDashing}\n";
        text += $"Grounded: {isGrounded}\n";
        text += $"Input: {moveInput}\n";
        text += $"\nAttributes:\n{attributes}";

        GUI.Box(new Rect(10, 150, 250, 200), text, style);
    }

    #endregion
}
