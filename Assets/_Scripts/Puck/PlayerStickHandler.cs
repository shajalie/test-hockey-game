using UnityEngine;

/// <summary>
/// Handles stick interactions with the puck for a player.
/// Bridges between IcePhysicsController and PuckController.
/// Manages possession pickup, carrying, and stick positioning.
/// </summary>
public class PlayerStickHandler : MonoBehaviour
{
    #region Serialized Fields

    [Header("Stick Configuration")]
    [SerializeField] private Transform stickTip;
    [SerializeField] private float stickLength = 1.5f;
    [SerializeField] private float stickAngle = 45f;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 1.2f;
    [SerializeField] private float pickupCooldown = 0.25f;
    [SerializeField] private bool autoPickup = true;

    [Header("Stick Handling")]
    [SerializeField] private float stickHandlingSpeed = 10f;
    [SerializeField] private float dekeMagnitude = 0.5f;

    [Header("References")]
    [SerializeField] private IcePhysicsController physicsController;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    #endregion

    #region Private Fields

    private PuckController puckController;
    private PassingSystem passingSystem;
    private ShootingSystem shootingSystem;

    private float pickupCooldownTimer;
    private Vector3 stickOffset;
    private bool hasPuck;

    // Deke state
    private float dekeTimer;
    private int dekeDirection; // -1 left, 0 none, 1 right

    #endregion

    #region Properties

    /// <summary>Whether this player has puck possession.</summary>
    public bool HasPuck => hasPuck;

    /// <summary>Current stick tip world position.</summary>
    public Vector3 StickTipPosition => stickTip != null ? stickTip.position : CalculateStickTipPosition();

    /// <summary>The puck controller (if any).</summary>
    public PuckController Puck => puckController;

    /// <summary>The passing system.</summary>
    public PassingSystem Passing => passingSystem;

    /// <summary>The shooting system.</summary>
    public ShootingSystem Shooting => shootingSystem;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (physicsController == null)
        {
            physicsController = GetComponent<IcePhysicsController>();
        }

        // Get or create systems
        passingSystem = GetComponent<PassingSystem>();
        if (passingSystem == null)
        {
            passingSystem = gameObject.AddComponent<PassingSystem>();
        }

        shootingSystem = GetComponent<ShootingSystem>();
        if (shootingSystem == null)
        {
            shootingSystem = gameObject.AddComponent<ShootingSystem>();
        }

        // Create stick tip if not assigned
        if (stickTip == null)
        {
            CreateStickTip();
        }

        // Calculate default stick offset
        stickOffset = new Vector3(stickLength * 0.7f, 0, stickLength * 0.7f);
    }

    private void Start()
    {
        // Find puck
        puckController = FindObjectOfType<PuckController>();

        // Setup shooting system
        if (shootingSystem != null && physicsController != null)
        {
            shootingSystem.SetPlayer(physicsController, physicsController.Attributes);
            shootingSystem.SetPuckController(puckController);
        }

        // Subscribe to puck events
        PuckController.OnPossessionChanged += OnPuckPossessionChanged;
    }

    private void OnDestroy()
    {
        PuckController.OnPossessionChanged -= OnPuckPossessionChanged;
    }

    private void Update()
    {
        UpdateCooldowns();
        UpdateStickPosition();

        if (autoPickup && !hasPuck)
        {
            TryPickupPuck();
        }
    }

    #endregion

    #region Initialization

    private void CreateStickTip()
    {
        GameObject stickObj = new GameObject("StickTip");
        stickObj.transform.SetParent(transform);
        stickObj.transform.localPosition = CalculateStickTipPosition() - transform.position;
        stickTip = stickObj.transform;
    }

    #endregion

    #region Stick Positioning

    private void UpdateStickPosition()
    {
        if (stickTip == null) return;

        // Base position
        Vector3 targetPos = CalculateStickTipPosition();

        // Apply deke offset
        if (dekeDirection != 0)
        {
            Vector3 right = transform.right;
            targetPos += right * dekeDirection * dekeMagnitude;
        }

        // Smooth movement
        stickTip.position = Vector3.Lerp(stickTip.position, targetPos, Time.deltaTime * stickHandlingSpeed);
    }

    private Vector3 CalculateStickTipPosition()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // Stick extends forward and slightly to the side
        float radAngle = stickAngle * Mathf.Deg2Rad;
        Vector3 stickDir = forward * Mathf.Cos(radAngle) + right * Mathf.Sin(radAngle);

        // Keep low to the ice
        Vector3 tipPos = transform.position + stickDir * stickLength;
        tipPos.y = 0.1f; // Just above ice

        return tipPos;
    }

    #endregion

    #region Puck Interaction

    private void TryPickupPuck()
    {
        if (puckController == null) return;
        if (pickupCooldownTimer > 0) return;
        if (puckController.IsPossessed && puckController.Owner != gameObject) return;

        // Check if puck is in range
        if (puckController.IsInPickupRange(stickTip))
        {
            bool success = puckController.TryGainPossession(gameObject, stickTip);
            if (success)
            {
                hasPuck = true;
                Debug.Log($"[PlayerStickHandler] {gameObject.name} picked up puck");
            }
        }
    }

    private void OnPuckPossessionChanged(PuckController puck, GameObject newOwner)
    {
        bool wasOwner = hasPuck;
        hasPuck = (newOwner == gameObject);

        if (wasOwner && !hasPuck)
        {
            // Lost possession
            pickupCooldownTimer = pickupCooldown;
            Debug.Log($"[PlayerStickHandler] {gameObject.name} lost puck");
        }
    }

    private void UpdateCooldowns()
    {
        if (pickupCooldownTimer > 0)
        {
            pickupCooldownTimer -= Time.deltaTime;
        }
    }

    #endregion

    #region Actions

    /// <summary>
    /// Attempt to shoot the puck.
    /// </summary>
    public void Shoot(Vector3 direction, float power)
    {
        if (!hasPuck || puckController == null) return;

        puckController.Shoot(direction, power);
        hasPuck = false;
    }

    /// <summary>
    /// Attempt to pass to target.
    /// </summary>
    public void Pass(GameObject target, float power)
    {
        if (!hasPuck || puckController == null) return;

        puckController.Pass(target, power);
        hasPuck = false;
    }

    /// <summary>
    /// Attempt smart pass (using passing system).
    /// </summary>
    public bool SmartPass()
    {
        if (!hasPuck || passingSystem == null) return false;

        return passingSystem.ExecutePass();
    }

    /// <summary>
    /// Start a deke move.
    /// </summary>
    public void StartDeke(int direction)
    {
        dekeDirection = Mathf.Clamp(direction, -1, 1);
        dekeTimer = 0.3f;
    }

    /// <summary>
    /// Cancel deke move.
    /// </summary>
    public void CancelDeke()
    {
        dekeDirection = 0;
    }

    /// <summary>
    /// Poke check attempt (for defense).
    /// </summary>
    public void PokeCheck()
    {
        if (hasPuck) return; // Can't poke check if you have the puck

        // Extend stick forward
        Vector3 pokeDirection = transform.forward;
        float pokeRange = stickLength * 1.5f;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, pokeDirection, out hit, pokeRange))
        {
            PuckController hitPuck = hit.collider.GetComponent<PuckController>();
            if (hitPuck != null && hitPuck.IsPossessed)
            {
                // Knock puck loose
                hitPuck.KnockLoose(pokeDirection, 5f);
                Debug.Log($"[PlayerStickHandler] {gameObject.name} poke check successful!");
            }
        }
    }

    /// <summary>
    /// Drop the puck (intentionally).
    /// </summary>
    public void DropPuck()
    {
        if (!hasPuck || puckController == null) return;

        puckController.LosePossession();
        hasPuck = false;
        pickupCooldownTimer = pickupCooldown;
    }

    #endregion

    #region Input Helpers

    /// <summary>
    /// Set aim direction for passing/shooting.
    /// </summary>
    public void SetAimDirection(Vector2 aim)
    {
        passingSystem?.SetAimDirection(aim);
        shootingSystem?.SetAimInput(aim);
    }

    /// <summary>
    /// Start charging a shot.
    /// </summary>
    public void StartShotCharge(ShootingSystem.ShotType type = ShootingSystem.ShotType.Wrist)
    {
        shootingSystem?.StartCharge(type);
    }

    /// <summary>
    /// Release the charged shot.
    /// </summary>
    public void ReleaseShotCharge()
    {
        shootingSystem?.ReleaseShot();
    }

    /// <summary>
    /// Cancel shot charge.
    /// </summary>
    public void CancelShotCharge()
    {
        shootingSystem?.CancelShot();
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Stick tip position
        Vector3 tipPos = stickTip != null ? stickTip.position : CalculateStickTipPosition();

        Gizmos.color = hasPuck ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(tipPos, 0.15f);

        // Stick line
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, tipPos);

        // Pickup range
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(tipPos, pickupRange);
    }

    #endregion
}
