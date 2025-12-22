using UnityEngine;
using System;

/// <summary>
/// Core puck physics and state controller.
/// Handles puck movement, possession state, and physics interactions.
/// This is the central hub - other systems (passing, shooting) interact through this.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PuckController : MonoBehaviour
{
    #region Events

    /// <summary>Fired when possession changes. Null = loose puck.</summary>
    public static event Action<PuckController, GameObject> OnPossessionChanged;

    /// <summary>Fired when puck is shot.</summary>
    public static event Action<PuckController, Vector3, float> OnPuckShot;

    /// <summary>Fired when puck is passed.</summary>
    public static event Action<PuckController, GameObject, GameObject> OnPuckPassed;

    /// <summary>Fired when puck enters goal.</summary>
    public static event Action<PuckController, int> OnGoalScored;

    #endregion

    #region Serialized Fields

    [Header("Physics")]
    [SerializeField] private float mass = 0.17f; // NHL puck is 170g
    [SerializeField] private float icefriction = 0.985f; // Per-frame multiplier
    [SerializeField] private float bounciness = 0.6f;
    [SerializeField] private float maxSpeed = 50f; // ~180 km/h max shot speed

    [Header("Possession")]
    [SerializeField] private float possessionRange = 1.2f;
    [SerializeField] private float stickMagnetForce = 25f;
    [SerializeField] private float stickDamping = 8f;
    [SerializeField] private float loseControlSpeed = 15f; // Speed difference to lose puck

    [Header("Loose Puck")]
    [SerializeField] private float loosePuckSlowdown = 0.98f;
    [SerializeField] private float minSpeedThreshold = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private AudioClip passSound;
    [SerializeField] private AudioClip boardHitSound;
    [SerializeField] private AudioClip stickHitSound;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    #endregion

    #region Private Fields

    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private AudioSource audioSource;

    // Possession state
    private GameObject currentOwner;
    private Transform currentStickTarget;
    private float possessionTimer;
    private bool isBeingPassed;
    private GameObject passTarget;

    // Shot state
    private bool wasJustShot;
    private float shotTimer;
    private const float SHOT_IMMUNITY_TIME = 0.15f;

    // Physics state
    private Vector3 lastPosition;
    private Vector3 velocity;

    #endregion

    #region Properties

    /// <summary>Current puck owner (null if loose).</summary>
    public GameObject Owner => currentOwner;

    /// <summary>Whether puck is currently possessed.</summary>
    public bool IsPossessed => currentOwner != null;

    /// <summary>Whether puck is in flight from a shot.</summary>
    public bool IsShot => wasJustShot;

    /// <summary>Whether puck is being passed.</summary>
    public bool IsPassed => isBeingPassed;

    /// <summary>Current puck velocity.</summary>
    public Vector3 Velocity => rb.linearVelocity;

    /// <summary>Current puck speed.</summary>
    public float Speed => rb.linearVelocity.magnitude;

    /// <summary>The rigidbody for external physics interactions.</summary>
    public Rigidbody Rigidbody => rb;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }

        ConfigureRigidbody();
    }

    private void FixedUpdate()
    {
        UpdatePossession();
        ApplyIceFriction();
        ClampSpeed();
        UpdateTimers();

        lastPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision);
    }

    #endregion

    #region Initialization

    private void ConfigureRigidbody()
    {
        rb.mass = mass;
        rb.linearDamping = 0f; // We handle friction manually
        rb.angularDamping = 2f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Freeze Y rotation to keep puck flat
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    #endregion

    #region Possession System

    private void UpdatePossession()
    {
        if (currentOwner == null || currentStickTarget == null) return;

        // Check if owner is still valid
        if (!currentOwner.activeInHierarchy)
        {
            LosePossession();
            return;
        }

        // Spring-like force toward stick
        Vector3 toStick = currentStickTarget.position - transform.position;
        float distance = toStick.magnitude;

        if (distance > possessionRange * 1.5f)
        {
            // Too far - lose possession
            LosePossession();
            return;
        }

        if (distance > 0.1f)
        {
            // Apply spring force
            Vector3 springForce = toStick.normalized * stickMagnetForce * distance;

            // Apply damping
            Vector3 relativeVelocity = rb.linearVelocity - GetOwnerVelocity();
            Vector3 dampingForce = -relativeVelocity * stickDamping;

            rb.AddForce(springForce + dampingForce, ForceMode.Force);
        }

        // Match owner's velocity for smooth carrying
        Vector3 ownerVel = GetOwnerVelocity();
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, ownerVel, Time.fixedDeltaTime * 10f);

        possessionTimer += Time.fixedDeltaTime;
    }

    private Vector3 GetOwnerVelocity()
    {
        if (currentOwner == null) return Vector3.zero;

        Rigidbody ownerRb = currentOwner.GetComponent<Rigidbody>();
        if (ownerRb != null)
        {
            return ownerRb.linearVelocity;
        }

        IcePhysicsController physics = currentOwner.GetComponent<IcePhysicsController>();
        if (physics != null)
        {
            return physics.Velocity;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Attempt to gain possession of the puck.
    /// </summary>
    public bool TryGainPossession(GameObject player, Transform stickTarget)
    {
        if (player == null || stickTarget == null) return false;

        // Can't gain possession of a just-shot puck
        if (wasJustShot) return false;

        // Check distance
        float distance = Vector3.Distance(transform.position, stickTarget.position);
        if (distance > possessionRange) return false;

        // If already owned by same player, just update stick target
        if (currentOwner == player)
        {
            currentStickTarget = stickTarget;
            return true;
        }

        // Contested puck - check if we can steal
        if (currentOwner != null)
        {
            if (!CanStealFrom(currentOwner, player))
            {
                return false;
            }
        }

        // Gain possession
        SetPossession(player, stickTarget);
        return true;
    }

    private bool CanStealFrom(GameObject currentOwner, GameObject challenger)
    {
        // TODO: Implement steal mechanics based on:
        // - Relative positions
        // - Player attributes (puck control vs checking)
        // - Whether a check is happening

        // For now, simple distance check
        float ownerDist = Vector3.Distance(currentOwner.transform.position, transform.position);
        float challengerDist = Vector3.Distance(challenger.transform.position, transform.position);

        return challengerDist < ownerDist * 0.7f;
    }

    private void SetPossession(GameObject newOwner, Transform stickTarget)
    {
        GameObject previousOwner = currentOwner;
        currentOwner = newOwner;
        currentStickTarget = stickTarget;
        possessionTimer = 0f;
        isBeingPassed = false;
        passTarget = null;

        // Cancel shot state
        wasJustShot = false;
        shotTimer = 0f;

        OnPossessionChanged?.Invoke(this, newOwner);

        Debug.Log($"[PuckController] Possession: {(previousOwner?.name ?? "None")} -> {newOwner.name}");
    }

    /// <summary>
    /// Force the puck to become loose.
    /// </summary>
    public void LosePossession()
    {
        if (currentOwner == null) return;

        GameObject previousOwner = currentOwner;
        currentOwner = null;
        currentStickTarget = null;
        isBeingPassed = false;
        passTarget = null;

        OnPossessionChanged?.Invoke(this, null);

        Debug.Log($"[PuckController] {previousOwner.name} lost possession");
    }

    /// <summary>
    /// Forcefully knock the puck loose (from a check, poke, etc).
    /// </summary>
    public void KnockLoose(Vector3 direction, float force)
    {
        LosePossession();
        rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        PlaySound(stickHitSound);
    }

    #endregion

    #region Shooting

    /// <summary>
    /// Shoot the puck.
    /// </summary>
    public void Shoot(Vector3 direction, float power)
    {
        if (currentOwner == null)
        {
            Debug.LogWarning("[PuckController] Cannot shoot - no owner");
            return;
        }

        GameObject shooter = currentOwner;

        // Release possession
        currentOwner = null;
        currentStickTarget = null;

        // Apply shot force
        direction.y = 0;
        direction.Normalize();

        // Add slight upward angle based on power
        float upAngle = Mathf.Lerp(0.02f, 0.1f, power / maxSpeed);
        direction += Vector3.up * upAngle;
        direction.Normalize();

        rb.linearVelocity = direction * power;

        // Set shot state
        wasJustShot = true;
        shotTimer = SHOT_IMMUNITY_TIME;
        isBeingPassed = false;

        OnPuckShot?.Invoke(this, direction, power);
        OnPossessionChanged?.Invoke(this, null);
        PlaySound(shotSound);

        Debug.Log($"[PuckController] Shot by {shooter.name} at {power:F1} units/s");
    }

    /// <summary>
    /// One-timer shot (hit a moving puck without gaining possession).
    /// </summary>
    public void OneTimer(GameObject shooter, Vector3 direction, float power)
    {
        // Don't change possession state - just redirect
        direction.y = 0;
        direction.Normalize();

        // Blend current velocity with new direction
        Vector3 currentDir = rb.linearVelocity.normalized;
        float currentSpeed = rb.linearVelocity.magnitude;

        // One-timer adds power
        float totalPower = Mathf.Min(power + currentSpeed * 0.5f, maxSpeed);

        rb.linearVelocity = direction * totalPower;

        wasJustShot = true;
        shotTimer = SHOT_IMMUNITY_TIME;
        isBeingPassed = false;

        OnPuckShot?.Invoke(this, direction, totalPower);
        PlaySound(shotSound);

        Debug.Log($"[PuckController] One-timer by {shooter.name} at {totalPower:F1} units/s");
    }

    #endregion

    #region Passing

    /// <summary>
    /// Pass the puck to a target.
    /// </summary>
    public void Pass(GameObject target, float power)
    {
        if (currentOwner == null)
        {
            Debug.LogWarning("[PuckController] Cannot pass - no owner");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[PuckController] Cannot pass - no target");
            return;
        }

        GameObject passer = currentOwner;
        passTarget = target;
        isBeingPassed = true;

        // Calculate lead position
        Vector3 targetPos = target.transform.position;
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            // Lead the target based on their velocity
            float travelTime = Vector3.Distance(transform.position, targetPos) / power;
            targetPos += targetRb.linearVelocity * travelTime * 0.7f;
        }

        // Direction to target
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0;
        direction.Normalize();

        // Release and pass
        currentOwner = null;
        currentStickTarget = null;

        rb.linearVelocity = direction * power;

        OnPuckPassed?.Invoke(this, passer, target);
        OnPossessionChanged?.Invoke(this, null);
        PlaySound(passSound);

        Debug.Log($"[PuckController] Pass from {passer.name} to {target.name}");
    }

    /// <summary>
    /// Saucer pass (elevated pass over obstacles).
    /// </summary>
    public void SaucerPass(GameObject target, float power, float height = 0.5f)
    {
        if (currentOwner == null || target == null) return;

        GameObject passer = currentOwner;
        passTarget = target;
        isBeingPassed = true;

        Vector3 targetPos = target.transform.position;
        Vector3 direction = (targetPos - transform.position);
        float distance = direction.magnitude;
        direction.y = 0;
        direction.Normalize();

        // Release
        currentOwner = null;
        currentStickTarget = null;

        // Calculate arc
        float travelTime = distance / power;
        float verticalVelocity = (height / (travelTime * 0.5f)) + (Physics.gravity.magnitude * travelTime * 0.25f);

        rb.linearVelocity = direction * power + Vector3.up * verticalVelocity;

        OnPuckPassed?.Invoke(this, passer, target);
        OnPossessionChanged?.Invoke(this, null);
        PlaySound(passSound);

        Debug.Log($"[PuckController] Saucer pass from {passer.name} to {target.name}");
    }

    #endregion

    #region Physics

    private void ApplyIceFriction()
    {
        if (IsPossessed) return; // Owner controls puck movement

        Vector3 vel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(vel.x, 0, vel.z);

        // Apply friction
        float friction = wasJustShot ? 0.995f : icefriction;
        horizontalVel *= friction;

        // Stop if very slow
        if (horizontalVel.magnitude < minSpeedThreshold)
        {
            horizontalVel = Vector3.zero;
        }

        rb.linearVelocity = new Vector3(horizontalVel.x, vel.y, horizontalVel.z);
    }

    private void ClampSpeed()
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(vel.x, 0, vel.z);

        if (horizontalVel.magnitude > maxSpeed)
        {
            horizontalVel = horizontalVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVel.x, vel.y, horizontalVel.z);
        }
    }

    private void UpdateTimers()
    {
        if (wasJustShot)
        {
            shotTimer -= Time.fixedDeltaTime;
            if (shotTimer <= 0f)
            {
                wasJustShot = false;
            }
        }
    }

    private void HandleCollision(Collision collision)
    {
        // Check for goal
        if (collision.gameObject.CompareTag("Goal"))
        {
            GoalTrigger goal = collision.gameObject.GetComponent<GoalTrigger>();
            int teamId = goal != null ? goal.TeamId : 0;
            OnGoalScored?.Invoke(this, teamId);
            return;
        }

        // Board collision
        if (collision.gameObject.CompareTag("Boards"))
        {
            PlaySound(boardHitSound, collision.relativeVelocity.magnitude / 20f);
            return;
        }

        // Player collision (potential steal)
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!IsPossessed && !wasJustShot)
            {
                // Could be picked up - let the player's pickup logic handle it
            }
        }
    }

    #endregion

    #region Audio

    private void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volumeScale);
        }
    }

    #endregion

    #region Public Utility

    /// <summary>
    /// Teleport puck to position (for faceoffs, resets).
    /// </summary>
    public void ResetToPosition(Vector3 position)
    {
        transform.position = position;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        LosePossession();
        wasJustShot = false;
        shotTimer = 0f;
    }

    /// <summary>
    /// Check if a player is in range to pick up the puck.
    /// </summary>
    public bool IsInPickupRange(Transform stickPosition)
    {
        if (stickPosition == null) return false;
        return Vector3.Distance(transform.position, stickPosition.position) <= possessionRange;
    }

    /// <summary>
    /// Get direction from puck to a position.
    /// </summary>
    public Vector3 GetDirectionTo(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0;
        return dir.normalized;
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Possession range
        Gizmos.color = IsPossessed ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, possessionRange);

        // Velocity
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, rb.linearVelocity * 0.2f);
        }

        // Connection to owner
        if (currentStickTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentStickTarget.position);
        }

        // Pass target
        if (passTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, passTarget.transform.position);
        }
    }

    #endregion
}
