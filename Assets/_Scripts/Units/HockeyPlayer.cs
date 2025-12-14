using UnityEngine;

/// <summary>
/// Physics-based hockey player controller with NHL-like skating feel.
/// Responsive movement with proper ice physics - smooth, weighty, and punchy.
/// Can be controlled by player input or AI.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HockeyPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats baseStats;
    [SerializeField] private Transform stickTip; // Where puck attaches (legacy - use HockeyStick component)
    private HockeyStick hockeyStick; // New stick system

    [Header("Stamina System")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f; // Per second while sprinting
    [SerializeField] private float staminaRegenRate = 25f; // Per second when not sprinting
    [SerializeField] private float minStaminaToSprint = 10f;

    [Header("Team")]
    [SerializeField] private int teamId = 0; // 0 = Home, 1 = Away
    [SerializeField] private PlayerPosition position = PlayerPosition.Center;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Components
    private Rigidbody rb;
    private RuntimeStats stats;

    // Input state (set by InputManager or AI)
    private Vector2 moveInput;
    private Vector2 aimInput;
    private bool sprintInput;
    private bool dashInput;

    // Stamina
    private float currentStamina;
    private bool canSprint = true;

    // Dash state
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private bool isDashing = false;

    // Movement state
    private Vector3 lastInputDirection;
    private float currentSpeed;

    // Properties
    public RuntimeStats Stats => stats;
    public Transform StickTip => GetStickTip(); // Smart getter - uses HockeyStick if available
    public HockeyStick Stick => hockeyStick; // Direct access to stick component
    public Vector3 Velocity => rb.linearVelocity;
    public bool HasPuck { get; private set; }
    public bool IsDashing => isDashing;
    public bool IsSprinting => sprintInput && canSprint && moveInput.sqrMagnitude > 0.1f;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public int TeamId => teamId;
    public PlayerPosition Position => position;

    // Team setters (called by TeamManager)
    public void SetTeam(int team) => teamId = team;
    public void SetPosition(PlayerPosition pos) => position = pos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Configure Rigidbody for ice physics
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.mass = 80f; // Hockey player weight in kg

        // Get hockey stick component (new system)
        hockeyStick = GetComponent<HockeyStick>();
        if (hockeyStick == null)
        {
            Debug.LogWarning("[HockeyPlayer] No HockeyStick component found. Using legacy stickTip reference.");
        }

        // Initialize runtime stats
        if (baseStats != null)
        {
            stats = new RuntimeStats(baseStats);
        }
        else
        {
            Debug.LogError("HockeyPlayer: No base stats assigned!");
        }

        // Initialize stamina
        currentStamina = maxStamina;
    }

    private void OnEnable()
    {
        GameEvents.OnStatsUpdated += OnStatsUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnStatsUpdated -= OnStatsUpdated;
    }

    private void Update()
    {
        // Update timers (non-physics)
        UpdateDashTimer();
        UpdateStamina();
    }

    private void FixedUpdate()
    {
        if (stats == null) return;

        ApplyMovement();
        ApplyDrag();
        ClampVelocityY(); // Prevent bouncing
        ClampSpeed();
        RotateTowardInput(); // Rotate toward input, not velocity!

        // Track current speed for debug
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        currentSpeed = horizontalVel.magnitude;
    }

    /// <summary>
    /// Update stamina based on sprint state.
    /// </summary>
    private void UpdateStamina()
    {
        if (IsSprinting)
        {
            // Drain stamina while sprinting
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);

            // Can't sprint if stamina too low
            if (currentStamina < minStaminaToSprint)
            {
                canSprint = false;
            }
        }
        else
        {
            // Regenerate stamina when not sprinting
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina);

            // Can sprint again once we have enough stamina
            if (currentStamina >= minStaminaToSprint)
            {
                canSprint = true;
            }
        }
    }

    /// <summary>
    /// Update dash timer manually instead of using Invoke().
    /// </summary>
    private void UpdateDashTimer()
    {
        // Dash duration timer
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }

        // Dash cooldown timer
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Core skating physics - responsive and smooth like NHL games.
    /// </summary>
    private void ApplyMovement()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        lastInputDirection = inputDirection;

        // Calculate effective acceleration
        float acceleration = stats.AccelerationForce;

        // Sprint multiplier (from stamina system)
        if (IsSprinting)
        {
            acceleration *= 1.6f; // Sprint boost
        }

        // Dash gives a bigger boost
        if (IsDashing)
        {
            acceleration *= stats.DashMultiplier;
        }

        // Speed loss on sharp turns (NHL-like feel)
        float turnPenalty = CalculateTurnSpeedPenalty(inputDirection);
        acceleration *= turnPenalty;

        // Apply force in input direction
        Vector3 force = inputDirection * acceleration;
        rb.AddForce(force, ForceMode.Force);
    }

    /// <summary>
    /// Simple turn penalty - lose speed on sharp turns but stay responsive.
    /// This replaces the complex carving system.
    /// </summary>
    private float CalculateTurnSpeedPenalty(Vector3 desiredDirection)
    {
        // Get current velocity direction
        Vector3 velocityDir = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // If moving slow, no penalty (can turn freely when slow)
        if (velocityDir.magnitude < 2f) return 1f;

        velocityDir.Normalize();

        // Calculate angle between current movement and desired direction
        float angle = Vector3.Angle(velocityDir, desiredDirection);

        // Simple penalty curve:
        // 0-45 degrees: no penalty (smooth turns)
        // 45-90 degrees: gradual penalty (medium turns)
        // 90-180 degrees: bigger penalty (sharp turns/reversals)
        if (angle < 45f)
        {
            return 1f; // No penalty for gentle turns
        }
        else if (angle < 90f)
        {
            // Gradual penalty for medium turns
            float t = (angle - 45f) / 45f; // 0 to 1
            return Mathf.Lerp(1f, 0.8f, t);
        }
        else
        {
            // Bigger penalty for sharp turns
            float t = (angle - 90f) / 90f; // 0 to 1
            return Mathf.Lerp(0.8f, 0.6f, t);
        }
    }

    /// <summary>
    /// Apply drag based on input state - creates the "ice" feel.
    /// </summary>
    private void ApplyDrag()
    {
        float targetDrag;

        if (moveInput.sqrMagnitude < 0.01f)
        {
            // No input = slide to a stop (hockey stop feel)
            targetDrag = stats.BrakingDrag;
        }
        else
        {
            // Normal ice drag when skating
            targetDrag = stats.IceDrag;
        }

        rb.linearDamping = targetDrag;
    }

    /// <summary>
    /// Clamp Y velocity to prevent bouncing on ice.
    /// </summary>
    private void ClampVelocityY()
    {
        Vector3 vel = rb.linearVelocity;

        // Clamp Y velocity to prevent bouncing
        vel.y = Mathf.Clamp(vel.y, -5f, 5f);

        // If moving slow vertically and close to ground, pin to ground
        if (Mathf.Abs(vel.y) < 1f && IsGrounded())
        {
            vel.y = Mathf.Max(vel.y, -0.5f);
        }

        rb.linearVelocity = vel;
    }

    /// <summary>
    /// Check if player is on the ground.
    /// </summary>
    private bool IsGrounded()
    {
        // Simple ground check using raycast
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);
    }

    /// <summary>
    /// Clamp horizontal speed to max (accounting for sprint/dash).
    /// </summary>
    private void ClampSpeed()
    {
        float maxSpeed = stats.MaxSpeed;

        // Sprint increases max speed
        if (IsSprinting)
        {
            maxSpeed *= 1.5f;
        }

        // Dash increases max speed even more
        if (IsDashing)
        {
            maxSpeed *= stats.DashMultiplier;
        }

        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (horizontalVel.magnitude > maxSpeed)
        {
            horizontalVel = horizontalVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);
        }
    }

    /// <summary>
    /// Rotate toward INPUT direction (not velocity) for responsive feel.
    /// </summary>
    private void RotateTowardInput()
    {
        // Use input direction for rotation, not velocity
        if (moveInput.sqrMagnitude > 0.1f)
        {
            Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(inputDir);

            // Fast rotation for responsive NHL-like feel
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                stats.TurnSpeed * Time.fixedDeltaTime
            );
        }
        // If no input but moving, rotate toward velocity (for AI or physics-driven movement)
        else if (rb.linearVelocity.sqrMagnitude > 1f)
        {
            Vector3 velocityDir = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(velocityDir);

            // Slower rotation when coasting
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                stats.TurnSpeed * 0.5f * Time.fixedDeltaTime
            );
        }
    }

    // === PUBLIC INPUT METHODS (Called by InputManager or AI) ===

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void SetAimInput(Vector2 input)
    {
        aimInput = input;
    }

    public void SetSprintInput(bool sprinting)
    {
        sprintInput = sprinting;
    }

    public void SetDashInput(bool dashing)
    {
        dashInput = dashing;
        if (dashing)
        {
            TriggerDash();
        }
    }

    /// <summary>
    /// Trigger dash - punchy and useful with immediate force.
    /// </summary>
    public void TriggerDash()
    {
        // Check cooldown
        if (dashCooldownTimer > 0f) return;

        // Determine dash direction
        Vector3 dashDir;
        if (moveInput.sqrMagnitude > 0.1f)
        {
            // Dash in input direction
            dashDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }
        else
        {
            // Dash forward if no input
            dashDir = transform.forward;
        }

        // Apply immediate impulse for punchy feel
        float dashImpulse = stats.AccelerationForce * 0.8f; // Punchy burst
        rb.AddForce(dashDir * dashImpulse, ForceMode.Impulse);

        // Set dash state
        isDashing = true;
        dashTimer = stats.DashDuration;
        dashCooldownTimer = stats.DashCooldown;

        Debug.Log($"[HockeyPlayer] DASH! Direction: {dashDir}, Cooldown: {stats.DashCooldown}s");
    }

    public void SetHasPuck(bool hasPuck)
    {
        if (HasPuck != hasPuck)
        {
            HasPuck = hasPuck;
            GameEvents.TriggerPuckPossessionChanged(hasPuck ? gameObject : null);
        }
    }

    // === EVENT HANDLERS ===

    private void OnStatsUpdated()
    {
        // Stats already recalculated by RuntimeStats
        Debug.Log($"[HockeyPlayer] Stats updated. New max speed: {stats.MaxSpeed}");
    }

    // === COLLISION HANDLING ===

    private void OnCollisionEnter(Collision collision)
    {
        // Body check - push other players
        if (collision.gameObject.CompareTag("Player"))
        {
            HockeyPlayer otherPlayer = collision.gameObject.GetComponent<HockeyPlayer>();
            if (otherPlayer != null)
            {
                // Calculate check force based on our speed and angle
                Vector3 ourVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                float ourSpeed = ourVelocity.magnitude;

                // Direction from us to them
                Vector3 checkDirection = (collision.transform.position - transform.position).normalized;
                checkDirection.y = 0f;

                // Calculate impact force
                float checkForce = stats.CheckForce;

                // Bonus force if we're moving fast
                if (ourSpeed > stats.MaxSpeed * 0.5f)
                {
                    checkForce *= 1.5f;
                }

                // Bonus force if dashing
                if (IsDashing)
                {
                    checkForce *= 1.5f;
                }

                // Apply force to other player
                otherPlayer.GetComponent<Rigidbody>().AddForce(
                    checkDirection * checkForce,
                    ForceMode.Impulse
                );

                Debug.Log($"[HockeyPlayer] Body check delivered! Force: {checkForce}");

                // Check if they lose the puck
                if (otherPlayer.HasPuck && checkForce > stats.CheckForce * 1.2f)
                {
                    // Strong check - they might lose the puck
                    // (PuckController would handle this)
                    Debug.Log($"[HockeyPlayer] Hard check - target may lose puck!");
                }
            }
        }
    }

    // === HELPER METHODS ===

    /// <summary>
    /// Smart getter for stick tip position.
    /// Uses HockeyStick component if available, falls back to legacy Transform.
    /// </summary>
    private Transform GetStickTip()
    {
        if (hockeyStick != null && hockeyStick.BladeContactPoint != null)
        {
            return hockeyStick.BladeContactPoint;
        }
        return stickTip;
    }

    // === DEBUG ===

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || rb == null) return;

        // Draw velocity (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, rb.linearVelocity);

        // Draw input direction (green)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, new Vector3(moveInput.x, 0, moveInput.y) * 3f);

        // Draw forward direction (red)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * 2f);

        // Draw stick tip (yellow)
        if (stickTip != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(stickTip.position, 0.2f);
        }

        // Draw dash state (cyan sphere when dashing)
        if (IsDashing)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
        }

        // Draw sprint state (magenta sphere when sprinting)
        if (IsSprinting)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);
        }
    }
}
