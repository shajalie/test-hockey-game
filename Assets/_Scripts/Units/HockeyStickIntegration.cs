using UnityEngine;

/// <summary>
/// Example integration script showing how to use the HockeyStick component
/// with player actions like shooting, passing, and poke checking.
///
/// This script demonstrates the recommended patterns for triggering
/// stick animations and accessing stick data.
///
/// USAGE:
/// - Attach this to the same GameObject as HockeyPlayer and HockeyStick
/// - Call these methods from your InputManager or AIController
/// - Customize timing and conditions as needed for your game
/// </summary>
[RequireComponent(typeof(HockeyPlayer))]
[RequireComponent(typeof(HockeyStick))]
public class HockeyStickIntegration : MonoBehaviour
{
    [Header("Action Settings")]
    [Tooltip("Key to trigger poke check")]
    [SerializeField] private KeyCode pokeCheckKey = KeyCode.E;

    [Tooltip("Cooldown between poke checks (seconds)")]
    [SerializeField] private float pokeCheckCooldown = 1.5f;

    [Tooltip("Range to detect puck for poke check")]
    [SerializeField] private float pokeCheckRange = 2f;

    // Component references
    private HockeyPlayer player;
    private HockeyStick stick;
    private ShootingController shootingController;

    // State tracking
    private float lastPokeCheckTime = -999f;

    private void Awake()
    {
        player = GetComponent<HockeyPlayer>();
        stick = GetComponent<HockeyStick>();
        shootingController = GetComponent<ShootingController>();

        if (stick == null)
        {
            Debug.LogError("[HockeyStickIntegration] HockeyStick component not found!");
            enabled = false;
        }
    }

    private void Update()
    {
        // Example: Manual poke check trigger (for testing)
        if (Input.GetKeyDown(pokeCheckKey))
        {
            TryPokeCheck();
        }

        // Example: Auto-trigger pass animation when passing
        // (You would hook this into your actual pass input system)
        if (Input.GetKeyDown(KeyCode.Q) && player.HasPuck)
        {
            PerformPass();
        }
    }

    // === PUBLIC METHODS (Call these from InputManager or AI) ===

    /// <summary>
    /// Attempts to perform a poke check.
    /// Returns true if successful, false if on cooldown.
    /// </summary>
    public bool TryPokeCheck()
    {
        // Check cooldown
        if (Time.time - lastPokeCheckTime < pokeCheckCooldown)
        {
            Debug.Log("[HockeyStickIntegration] Poke check on cooldown");
            return false;
        }

        // Trigger stick animation
        stick.TriggerPokeCheck();
        lastPokeCheckTime = Time.time;

        // Optional: Check if puck is in range and knock it loose
        CheckPokeCheckHit();

        Debug.Log("[HockeyStickIntegration] Poke check executed!");
        return true;
    }

    /// <summary>
    /// Performs a pass action with stick animation.
    /// Call this when player makes a pass.
    /// </summary>
    public void PerformPass()
    {
        if (!player.HasPuck)
        {
            Debug.LogWarning("[HockeyStickIntegration] Cannot pass without puck!");
            return;
        }

        // Trigger stick animation
        stick.TriggerPassAnimation();

        // TODO: Implement actual pass physics here
        // Example:
        // - Calculate pass direction
        // - Apply force to puck
        // - Transfer possession

        Debug.Log("[HockeyStickIntegration] Pass executed!");
    }

    /// <summary>
    /// Triggers shooting animation.
    /// Call this when player releases a shot.
    ///
    /// NOTE: This should be called by ShootingController when shot is taken.
    /// Included here as an example of the integration pattern.
    /// </summary>
    public void OnShotTaken()
    {
        stick.TriggerShootAnimation();
        Debug.Log("[HockeyStickIntegration] Shot animation triggered");
    }

    /// <summary>
    /// Gets the current blade position for puck pickup range checks.
    /// </summary>
    public Vector3 GetBladePosition()
    {
        return stick.BladePosition;
    }

    /// <summary>
    /// Checks if puck is within reach of the blade.
    /// Useful for puck pickup and poke check logic.
    /// </summary>
    public bool IsPuckInReach(Vector3 puckPosition, float range)
    {
        return stick.IsPointInRange(puckPosition, range);
    }

    // === HELPER METHODS ===

    /// <summary>
    /// Checks if poke check hit anything (puck or opponent).
    /// This is a simple example - expand based on your game logic.
    /// </summary>
    private void CheckPokeCheckHit()
    {
        // Cast a sphere from blade position forward
        Vector3 bladePos = stick.BladePosition;
        Vector3 bladeForward = stick.BladeForward;

        RaycastHit[] hits = Physics.SphereCastAll(
            bladePos,
            0.3f,
            bladeForward,
            pokeCheckRange
        );

        foreach (RaycastHit hit in hits)
        {
            // Check for puck
            if (hit.collider.CompareTag("Puck"))
            {
                Debug.Log("[HockeyStickIntegration] Poke check hit puck!");

                // Get puck component
                Puck puck = hit.collider.GetComponent<Puck>();
                if (puck != null)
                {
                    // Apply small force to puck
                    Rigidbody puckRb = hit.collider.GetComponent<Rigidbody>();
                    if (puckRb != null)
                    {
                        Vector3 pokeForce = bladeForward * 5f;
                        puckRb.AddForce(pokeForce, ForceMode.Impulse);
                    }
                }
            }

            // Check for opponent player
            else if (hit.collider.CompareTag("Player"))
            {
                HockeyPlayer opponent = hit.collider.GetComponent<HockeyPlayer>();
                if (opponent != null && opponent.TeamId != player.TeamId)
                {
                    Debug.Log("[HockeyStickIntegration] Poke check hit opponent!");

                    // If opponent has puck, try to knock it loose
                    if (opponent.HasPuck)
                    {
                        Debug.Log("[HockeyStickIntegration] Attempting to knock puck loose!");
                        // Puck controller would handle this based on check force
                    }
                }
            }
        }
    }

    // === INTEGRATION WITH SHOOTING CONTROLLER ===

    /// <summary>
    /// Subscribe to shooting events to trigger stick animations.
    /// This shows how to integrate with ShootingController if you have one.
    /// </summary>
    private void OnEnable()
    {
        GameEvents.OnShotTaken += OnShotTakenEvent;
    }

    private void OnDisable()
    {
        GameEvents.OnShotTaken -= OnShotTakenEvent;
    }

    private void OnShotTakenEvent(Vector3 direction, float power)
    {
        // Only trigger animation if this is our shot
        // (Check if we're close to the event source)
        OnShotTaken();
    }

    // === DEBUG VISUALIZATION ===

    private void OnDrawGizmos()
    {
        if (stick == null) return;

        // Draw poke check range
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 bladePos = stick.BladePosition;
        Vector3 bladeForward = stick.BladeForward;
        Gizmos.DrawRay(bladePos, bladeForward * pokeCheckRange);
        Gizmos.DrawWireSphere(bladePos + bladeForward * pokeCheckRange, 0.3f);

        // Draw cooldown indicator
        if (Time.time - lastPokeCheckTime < pokeCheckCooldown)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.2f);
        }
    }
}

// === EXAMPLE USAGE PATTERNS ===

/*
 * EXAMPLE 1: Integrating with InputManager
 * -----------------------------------------
 *
 * In your InputManager.cs:
 *
 * private HockeyStickIntegration stickIntegration;
 *
 * void Awake()
 * {
 *     stickIntegration = player.GetComponent<HockeyStickIntegration>();
 * }
 *
 * void Update()
 * {
 *     // Poke check input
 *     if (Input.GetButtonDown("PokeCheck"))
 *     {
 *         stickIntegration.TryPokeCheck();
 *     }
 *
 *     // Pass input
 *     if (Input.GetButtonDown("Pass"))
 *     {
 *         stickIntegration.PerformPass();
 *     }
 * }
 *
 *
 * EXAMPLE 2: Integrating with ShootingController
 * -----------------------------------------------
 *
 * In your ShootingController.cs, when releasing a shot:
 *
 * private HockeyStick stick;
 *
 * void ReleaseShotCharge()
 * {
 *     // ... existing shot logic ...
 *
 *     // Trigger stick animation
 *     if (stick != null)
 *     {
 *         stick.TriggerShootAnimation();
 *     }
 * }
 *
 *
 * EXAMPLE 3: AI Controller Usage
 * -------------------------------
 *
 * In your AIController.cs:
 *
 * private HockeyStickIntegration stickIntegration;
 *
 * void DecideAction()
 * {
 *     if (ShouldPokeCheck())
 *     {
 *         stickIntegration.TryPokeCheck();
 *     }
 *
 *     if (ShouldPass())
 *     {
 *         stickIntegration.PerformPass();
 *     }
 * }
 *
 *
 * EXAMPLE 4: Puck Pickup Detection
 * ---------------------------------
 *
 * In your PuckController.cs:
 *
 * void CheckForNearbyPlayers()
 * {
 *     foreach (HockeyPlayer player in nearbyPlayers)
 *     {
 *         HockeyStickIntegration stick = player.GetComponent<HockeyStickIntegration>();
 *         if (stick != null && stick.IsPuckInReach(puck.position, pickupRange))
 *         {
 *             AttachPuckToPlayer(player);
 *         }
 *     }
 * }
 */
