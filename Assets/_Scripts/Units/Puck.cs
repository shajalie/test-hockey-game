using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Realistic hockey puck physics with responsive stick-handling and proper collision mechanics.
/// Features ice friction, board bouncing, passing, shooting, and possession tracking for assists.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Puck : MonoBehaviour
{
    [Header("Ice Physics")]
    [SerializeField] private float iceFriction = 0.98f; // Per-frame velocity multiplier (very low friction)
    [SerializeField] private float maxSpeed = 60f;
    [SerializeField] private float boardBounciness = 0.65f; // Energy retention on board collisions
    [SerializeField] private float minBounceSpeed = 2f; // Below this, puck won't bounce

    [Header("Stick Handling (Magnetic)")]
    [SerializeField] private float magnetRange = 1.8f;
    [SerializeField] private float magnetForce = 12f; // Responsive pull force
    [SerializeField] private float velocityMatchStrength = 0.5f; // How much puck matches player velocity
    [SerializeField] private float maxPossessionDistance = 2.5f; // Auto-lose if too far

    [Header("Shooting")]
    [SerializeField] private float shotPowerMultiplier = 1.2f;
    [SerializeField] private float maxShotSpeed = 55f;
    [SerializeField] private float shotReleaseDelay = 0.15f; // Time before puck can be picked up after shot

    [Header("Passing")]
    [SerializeField] private float passPowerMultiplier = 0.8f;
    [SerializeField] private float passArcHeight = 0.1f; // Slight lift for passes
    [SerializeField] private float passAccuracy = 0.95f; // 1.0 = perfect, lower = more variance

    [Header("Possession")]
    [SerializeField] private float possessionPickupCooldown = 0.25f;
    [SerializeField] private float knockLooseThreshold = 6f;
    [SerializeField] private int maxAssistTracking = 3; // Track last N players who touched puck

    [Header("Visual Feedback")]
    [SerializeField] private bool enablePossessionEffects = true;
    [SerializeField] private Color possessedColor = Color.yellow;
    [SerializeField] private Color looseColor = Color.white;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool logPossessionChanges = true;

    // Cached Components (no FindObjectOfType during gameplay)
    private Rigidbody rb;
    private SphereCollider col;
    private MeshRenderer meshRenderer;
    private Material puckMaterial;

    // Possession State
    private HockeyPlayer currentOwner;
    private HockeyPlayer lastOwner;
    private List<HockeyPlayer> touchHistory; // For assist tracking
    private float lastPossessionChangeTime;
    private float shotCooldownTimer;

    // Physics State
    private bool isInShot;
    private bool isInPass;
    private Vector3 lastFrameVelocity;
    private PhysicsMaterial iceMaterial;
    private PhysicsMaterial boardMaterial;

    // Properties
    public HockeyPlayer CurrentOwner => currentOwner;
    public HockeyPlayer LastOwner => lastOwner;
    public bool IsLoose => currentOwner == null;
    public Vector3 Velocity => rb.linearVelocity;
    public float Speed => rb.linearVelocity.magnitude;
    public List<HockeyPlayer> TouchHistory => new List<HockeyPlayer>(touchHistory); // Return copy for safety

    #region Initialization

    private void Awake()
    {
        CacheComponents();
        ConfigurePhysics();
        InitializeState();
    }

    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<SphereCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        // Clone material for individual color changes
        if (meshRenderer != null && enablePossessionEffects)
        {
            puckMaterial = new Material(meshRenderer.sharedMaterial);
            meshRenderer.material = puckMaterial;
        }
    }

    private void ConfigurePhysics()
    {
        // Real hockey puck physics
        rb.mass = 0.17f; // 170 grams
        rb.useGravity = true;
        rb.linearDamping = 0f; // We handle friction manually for better control
        rb.angularDamping = 0.3f; // Slight spin dampening
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // Puck spins only on Y-axis

        // Create physics materials for different surfaces
        iceMaterial = new PhysicsMaterial("Ice")
        {
            dynamicFriction = 0.02f,
            staticFriction = 0.02f,
            bounciness = 0.1f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        boardMaterial = new PhysicsMaterial("Boards")
        {
            dynamicFriction = 0.3f,
            staticFriction = 0.3f,
            bounciness = boardBounciness,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Average
        };

        col.material = iceMaterial;
    }

    private void InitializeState()
    {
        touchHistory = new List<HockeyPlayer>();
        lastPossessionChangeTime = -999f;
        shotCooldownTimer = 0f;
        isInShot = false;
        isInPass = false;

        UpdateVisualFeedback();
    }

    #endregion

    #region Update Loop

    private void FixedUpdate()
    {
        // Store velocity for collision detection
        lastFrameVelocity = rb.linearVelocity;

        // Update cooldown timers
        if (shotCooldownTimer > 0f)
        {
            shotCooldownTimer -= Time.fixedDeltaTime;
        }

        // Handle possession mechanics
        if (currentOwner != null)
        {
            HandlePossession();
        }
        else if (shotCooldownTimer <= 0f)
        {
            CheckForNearbyPlayers();
        }

        // Apply ice physics
        ApplyIceFriction();
        ClampVelocity();

        // Keep puck on ice (prevent jumping unless hit hard)
        KeepPuckOnIce();
    }

    #endregion

    #region Possession Mechanics

    /// <summary>
    /// Handles magnetic stick-handling when player has possession.
    /// </summary>
    private void HandlePossession()
    {
        if (currentOwner == null || currentOwner.StickTip == null)
        {
            LosePossession();
            return;
        }

        Vector3 stickPosition = currentOwner.StickTip.position;
        Vector3 toPuck = transform.position - stickPosition;
        float distance = toPuck.magnitude;

        // Auto-lose if too far from stick
        if (distance > maxPossessionDistance)
        {
            if (logPossessionChanges)
                Debug.Log($"[Puck] {currentOwner.name} lost possession - too far from stick ({distance:F2}m)");
            LosePossession();
            return;
        }

        // Don't apply magnet force during shot/pass release
        if (isInShot || isInPass)
            return;

        // Apply magnetic pull toward stick tip
        Vector3 pullDirection = (stickPosition - transform.position).normalized;
        float pullStrength = magnetForce * Mathf.Clamp01(distance / magnetRange);
        rb.AddForce(pullDirection * pullStrength, ForceMode.Force);

        // Match player velocity for responsive stick-handling (no sluggish lerp!)
        Vector3 playerVelocity = currentOwner.Velocity;
        Vector3 velocityDifference = playerVelocity - rb.linearVelocity;
        rb.AddForce(velocityDifference * velocityMatchStrength, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Checks for nearby players who can pick up the loose puck.
    /// </summary>
    private void CheckForNearbyPlayers()
    {
        // Enforce cooldown after possession changes
        if (Time.time < lastPossessionChangeTime + possessionPickupCooldown)
            return;

        // Find nearest player within magnet range
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, magnetRange, LayerMask.GetMask("Player"));

        HockeyPlayer closestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in nearbyColliders)
        {
            HockeyPlayer player = col.GetComponent<HockeyPlayer>();
            if (player == null) continue;

            // Skip if this is the player who just lost it (prevent instant re-grab)
            if (player == lastOwner && Time.time < lastPossessionChangeTime + possessionPickupCooldown * 1.5f)
                continue;

            Vector3 stickPos = player.StickTip != null ? player.StickTip.position : player.transform.position;
            float distance = Vector3.Distance(transform.position, stickPos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        // Auto-pickup if player is close enough
        if (closestPlayer != null && closestDistance <= magnetRange)
        {
            GainPossession(closestPlayer);
        }
    }

    /// <summary>
    /// Player gains possession of the puck.
    /// </summary>
    public void GainPossession(HockeyPlayer player)
    {
        if (player == null) return;

        // Release from previous owner
        if (currentOwner != null && currentOwner != player)
        {
            currentOwner.SetHasPuck(false);
        }

        // Set new owner
        lastOwner = currentOwner;
        currentOwner = player;
        currentOwner.SetHasPuck(true);
        lastPossessionChangeTime = Time.time;
        isInShot = false;
        isInPass = false;

        // Track touch for assists
        AddToTouchHistory(player);

        // Update magnet settings from player stats
        if (player.Stats != null)
        {
            magnetRange = player.Stats.PuckMagnetRange;
            magnetForce = player.Stats.PuckMagnetStrength;
            knockLooseThreshold = player.Stats.PuckKnockLooseThreshold;
        }

        // Visual feedback
        UpdateVisualFeedback();

        // Events
        if (logPossessionChanges)
            Debug.Log($"[Puck] Possession: {player.name} (Team: {player.TeamId})");

        GameEvents.TriggerPuckPossessionChanged(player.gameObject);
    }

    /// <summary>
    /// Current owner loses possession - puck becomes loose.
    /// </summary>
    public void LosePossession()
    {
        if (currentOwner == null) return;

        lastOwner = currentOwner;
        currentOwner.SetHasPuck(false);
        currentOwner = null;
        lastPossessionChangeTime = Time.time;

        // Visual feedback
        UpdateVisualFeedback();

        // Events
        if (logPossessionChanges)
            Debug.Log("[Puck] Puck is now loose!");

        GameEvents.TriggerPuckPossessionChanged(null);
    }

    /// <summary>
    /// Tracks players who touched the puck for assist attribution.
    /// </summary>
    private void AddToTouchHistory(HockeyPlayer player)
    {
        // Remove if already in list (move to front)
        touchHistory.Remove(player);

        // Add to front
        touchHistory.Insert(0, player);

        // Trim to max tracking
        if (touchHistory.Count > maxAssistTracking)
        {
            touchHistory.RemoveRange(maxAssistTracking, touchHistory.Count - maxAssistTracking);
        }
    }

    #endregion

    #region Shooting and Passing

    /// <summary>
    /// Shoots the puck with power and direction. Feels powerful and responsive.
    /// </summary>
    public void Shoot(Vector3 direction, float power)
    {
        if (currentOwner == null) return;

        isInShot = true;

        // Clear current velocity for clean shot
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Apply shot force
        Vector3 shotDirection = direction.normalized;
        float shotSpeed = Mathf.Min(power * shotPowerMultiplier, maxShotSpeed);
        rb.AddForce(shotDirection * shotSpeed, ForceMode.Impulse);

        // Add slight upward component for realistic shot trajectory
        rb.AddForce(Vector3.up * (shotSpeed * 0.05f), ForceMode.Impulse);

        // Spin the puck
        rb.AddTorque(Vector3.up * shotSpeed * 2f, ForceMode.Impulse);

        if (logPossessionChanges)
            Debug.Log($"[Puck] SHOT by {currentOwner.name}! Power: {shotSpeed:F1} m/s, Direction: {shotDirection}");

        // Events
        GameEvents.TriggerShotTaken(shotDirection, shotSpeed);

        // Release possession
        LosePossession();
        shotCooldownTimer = shotReleaseDelay;
    }

    /// <summary>
    /// Passes the puck toward a target position with accuracy variance.
    /// </summary>
    public void Pass(Vector3 targetPosition, float power)
    {
        if (currentOwner == null) return;

        isInPass = true;

        // Calculate direction to target
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0; // Keep pass horizontal
        Vector3 baseDirection = toTarget.normalized;

        // Add slight accuracy variance (human element)
        if (passAccuracy < 1f)
        {
            float variance = (1f - passAccuracy) * 2f; // Convert to degrees
            float randomAngle = Random.Range(-variance, variance);
            baseDirection = Quaternion.Euler(0, randomAngle, 0) * baseDirection;
        }

        // Clear velocity
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Apply pass force
        float passSpeed = power * passPowerMultiplier;
        rb.AddForce(baseDirection * passSpeed, ForceMode.Impulse);

        // Add slight lift for saucer pass feel
        rb.AddForce(Vector3.up * passArcHeight, ForceMode.Impulse);

        if (logPossessionChanges)
            Debug.Log($"[Puck] PASS by {currentOwner.name} toward {targetPosition}, Power: {passSpeed:F1}");

        // Release possession
        LosePossession();
        shotCooldownTimer = shotReleaseDelay * 0.5f; // Shorter cooldown for passes
    }

    /// <summary>
    /// Passes to a specific player (auto-targeting).
    /// </summary>
    public void PassToPlayer(HockeyPlayer targetPlayer, float power)
    {
        if (targetPlayer == null || targetPlayer.StickTip == null)
        {
            Debug.LogWarning("[Puck] Cannot pass to null player or player without stick!");
            return;
        }

        // Lead the pass slightly based on target's velocity
        Vector3 leadPosition = targetPlayer.transform.position + (targetPlayer.Velocity * 0.3f);
        Pass(leadPosition, power);
    }

    #endregion

    #region Physics

    /// <summary>
    /// Applies ice friction - puck slides smoothly but gradually slows down.
    /// </summary>
    private void ApplyIceFriction()
    {
        // Manual friction application for ice-like sliding
        Vector3 velocity = rb.linearVelocity;
        velocity.x *= iceFriction;
        velocity.z *= iceFriction;
        rb.linearVelocity = velocity;
    }

    /// <summary>
    /// Clamps puck velocity to maximum speed.
    /// </summary>
    private void ClampVelocity()
    {
        float speed = rb.linearVelocity.magnitude;
        if (speed > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    /// <summary>
    /// Keeps puck from bouncing too high - should slide on ice.
    /// </summary>
    private void KeepPuckOnIce()
    {
        // If puck is above ice and moving up slowly, push it down
        if (transform.position.y > 0.3f && rb.linearVelocity.y > -1f && rb.linearVelocity.y < 2f)
        {
            rb.AddForce(Vector3.down * 5f, ForceMode.Force);
        }

        // Clamp vertical velocity to prevent bouncing
        if (rb.linearVelocity.y > 3f)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = 3f;
            rb.linearVelocity = vel;
        }
    }

    #endregion

    #region Collision Handling

    private void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        Vector3 impactPoint = collision.contacts[0].point;
        Vector3 impactNormal = collision.contacts[0].normal;

        // Check for knock loose from current owner
        if (currentOwner != null && impactSpeed > knockLooseThreshold)
        {
            if (logPossessionChanges)
                Debug.Log($"[Puck] Knocked loose by {collision.gameObject.name}! Impact: {impactSpeed:F1}");

            LosePossession();

            // Add deflection
            Vector3 deflection = impactNormal * impactSpeed * 0.4f;
            rb.AddForce(deflection, ForceMode.Impulse);
        }

        // Board collisions - realistic bounce
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            HandleBoardCollision(collision);
        }

        // Goal detection
        Goal goal = collision.gameObject.GetComponent<Goal>();
        if (goal != null)
        {
            // Pass touch history for assist tracking
            goal.OnPuckEntered(lastOwner, new List<HockeyPlayer>(touchHistory));
        }

        // Player collision - potential possession change or puck battle
        HockeyPlayer hitPlayer = collision.gameObject.GetComponent<HockeyPlayer>();
        if (hitPlayer != null && hitPlayer != currentOwner)
        {
            HandlePlayerCollision(hitPlayer, impactSpeed);
        }
    }

    /// <summary>
    /// Handles realistic board bouncing physics.
    /// </summary>
    private void HandleBoardCollision(Collision collision)
    {
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        Vector3 contactNormal = collision.contacts[0].normal;

        // Only bounce if moving fast enough
        if (speed < minBounceSpeed)
        {
            // Kill velocity along normal for dead stop
            rb.linearVelocity = velocity - Vector3.Dot(velocity, contactNormal) * contactNormal * 0.5f;
            return;
        }

        // Realistic board bounce - reduce energy
        Vector3 reflection = Vector3.Reflect(velocity, contactNormal);
        rb.linearVelocity = reflection * boardBounciness;

        // Add spin based on angle of impact
        float impactAngle = Vector3.Angle(velocity, -contactNormal);
        rb.AddTorque(Vector3.up * impactAngle * 0.5f, ForceMode.Impulse);
    }

    /// <summary>
    /// Handles puck hitting a player - might change possession.
    /// </summary>
    private void HandlePlayerCollision(HockeyPlayer player, float impactSpeed)
    {
        // If puck is loose and hits player's stick area, they might gain possession
        if (IsLoose && shotCooldownTimer <= 0f && player.StickTip != null)
        {
            float distanceToStick = Vector3.Distance(transform.position, player.StickTip.position);
            if (distanceToStick < magnetRange * 0.8f)
            {
                GainPossession(player);
            }
        }
    }

    #endregion

    #region Visual Feedback

    /// <summary>
    /// Updates visual feedback based on possession state.
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (!enablePossessionEffects || puckMaterial == null) return;

        Color targetColor = IsLoose ? looseColor : possessedColor;
        puckMaterial.color = targetColor;

        // Could add emission for possessed puck
        if (puckMaterial.HasProperty("_EmissionColor"))
        {
            puckMaterial.SetColor("_EmissionColor", targetColor * (IsLoose ? 0f : 0.3f));
        }
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Magnet range sphere
        Gizmos.color = IsLoose ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, magnetRange);

        // Max possession distance
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxPossessionDistance);

        // Velocity vector
        if (rb != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, rb.linearVelocity * 0.3f);
        }

        // Line to owner's stick
        if (currentOwner?.StickTip != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentOwner.StickTip.position);

            // Distance label would go here (requires Handles in editor)
        }

        // Touch history visualization
        if (touchHistory != null && touchHistory.Count > 0)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < touchHistory.Count; i++)
            {
                if (touchHistory[i] != null)
                {
                    Vector3 offset = Vector3.up * (i * 0.2f + 0.5f);
                    Gizmos.DrawWireSphere(touchHistory[i].transform.position + offset, 0.3f);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Draw detailed info when selected
        Gizmos.color = Color.white;

        // Show current stats in Scene view (would use Handles.Label in editor extension)
        // For now just draw helpful spheres
    }

    #endregion

    #region Public API

    /// <summary>
    /// Forces the puck to a specific position (for resets, faceoffs, etc.)
    /// </summary>
    public void ResetPosition(Vector3 position)
    {
        transform.position = position;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (currentOwner != null)
        {
            LosePossession();
        }

        shotCooldownTimer = 0.5f; // Prevent immediate pickup
    }

    /// <summary>
    /// Gets the player who should receive an assist (second-to-last touch).
    /// </summary>
    public HockeyPlayer GetAssistPlayer()
    {
        if (touchHistory.Count >= 2)
        {
            return touchHistory[1]; // Second most recent toucher
        }
        return null;
    }

    /// <summary>
    /// Clears touch history (for new possessions after goals, etc.)
    /// </summary>
    public void ClearTouchHistory()
    {
        touchHistory.Clear();
    }

    #endregion
}
