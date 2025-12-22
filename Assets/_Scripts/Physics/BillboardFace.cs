using UnityEngine;

/// <summary>
/// Makes a sprite always face the camera (billboarding) for HD-2D/2.5D visual style.
/// Also handles X-axis flipping based on movement direction.
/// Attach to the sprite object (child of the physics object).
/// </summary>
public class BillboardFace : MonoBehaviour
{
    #region Serialized Fields

    [Header("Billboard Settings")]
    [Tooltip("Lock Y rotation (sprite only rotates to face camera horizontally)")]
    [SerializeField] private bool lockYRotation = true;

    [Tooltip("Offset angle from directly facing camera")]
    [SerializeField] private float angleOffset = 0f;

    [Header("Flip Settings")]
    [Tooltip("Flip sprite X based on movement direction")]
    [SerializeField] private bool flipBasedOnMovement = true;

    [Tooltip("Minimum velocity to trigger flip (prevents jitter)")]
    [SerializeField] private float flipVelocityThreshold = 0.5f;

    [Tooltip("Time before flip changes (debounce)")]
    [SerializeField] private float flipDebounceTime = 0.1f;

    [Tooltip("Reference to the physics controller (auto-detected if not set)")]
    [SerializeField] private IcePhysicsController physicsController;

    [Tooltip("Alternative: Use rigidbody directly if no IcePhysicsController")]
    [SerializeField] private Rigidbody targetRigidbody;

    [Header("Advanced")]
    [Tooltip("Custom camera (uses main camera if not set)")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Update in LateUpdate (recommended) vs Update")]
    [SerializeField] private bool useLatUpdate = true;

    #endregion

    #region Private Fields

    private Transform cameraTransform;
    private SpriteRenderer spriteRenderer;
    private bool isFacingRight = true;
    private float flipTimer = 0f;
    private bool pendingFlip = false;
    private bool pendingFlipDirection = true;

    #endregion

    #region Properties

    /// <summary>
    /// Whether the sprite is currently facing right.
    /// </summary>
    public bool IsFacingRight => isFacingRight;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Auto-detect physics controller on parent
        if (physicsController == null)
        {
            physicsController = GetComponentInParent<IcePhysicsController>();
        }

        // Fall back to rigidbody
        if (physicsController == null && targetRigidbody == null)
        {
            targetRigidbody = GetComponentInParent<Rigidbody>();
        }
    }

    private void Start()
    {
        // Get camera reference
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
        }
        else
        {
            Debug.LogWarning($"[BillboardFace] No camera found for {gameObject.name}");
        }
    }

    private void Update()
    {
        if (!useLatUpdate)
        {
            UpdateBillboard();
        }

        UpdateFlip();
    }

    private void LateUpdate()
    {
        if (useLatUpdate)
        {
            UpdateBillboard();
        }
    }

    #endregion

    #region Billboard

    private void UpdateBillboard()
    {
        if (cameraTransform == null)
        {
            // Try to get camera again
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            return;
        }

        if (lockYRotation)
        {
            // Only rotate around Y axis (stay upright)
            Vector3 directionToCamera = cameraTransform.position - transform.position;
            directionToCamera.y = 0f;

            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);

                // Apply offset
                if (angleOffset != 0f)
                {
                    targetRotation *= Quaternion.Euler(0f, angleOffset, 0f);
                }

                transform.rotation = targetRotation;
            }
        }
        else
        {
            // Full billboard - always face camera exactly
            transform.LookAt(cameraTransform);
            transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y + 180f + angleOffset, 0f);
        }
    }

    #endregion

    #region Flip

    private void UpdateFlip()
    {
        if (!flipBasedOnMovement || spriteRenderer == null) return;

        // Get velocity
        Vector3 velocity = GetVelocity();
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

        // Check if moving fast enough to determine direction
        if (horizontalSpeed > flipVelocityThreshold)
        {
            // Determine direction based on velocity relative to camera
            // We want to flip based on screen-space direction
            Vector3 velocityInCameraSpace = cameraTransform != null
                ? cameraTransform.InverseTransformDirection(velocity)
                : velocity;

            bool shouldFaceRight = velocityInCameraSpace.x > 0;

            // Debounce flip to prevent jitter
            if (shouldFaceRight != isFacingRight)
            {
                if (!pendingFlip || pendingFlipDirection != shouldFaceRight)
                {
                    pendingFlip = true;
                    pendingFlipDirection = shouldFaceRight;
                    flipTimer = flipDebounceTime;
                }
            }
            else
            {
                pendingFlip = false;
            }
        }

        // Process pending flip
        if (pendingFlip)
        {
            flipTimer -= Time.deltaTime;
            if (flipTimer <= 0f)
            {
                ApplyFlip(pendingFlipDirection);
                pendingFlip = false;
            }
        }
    }

    private Vector3 GetVelocity()
    {
        if (physicsController != null)
        {
            return physicsController.Velocity;
        }

        if (targetRigidbody != null)
        {
            return targetRigidbody.linearVelocity;
        }

        return Vector3.zero;
    }

    private void ApplyFlip(bool faceRight)
    {
        if (isFacingRight == faceRight) return;

        isFacingRight = faceRight;
        spriteRenderer.flipX = !faceRight;

        Debug.Log($"[BillboardFace] Flipped to face {(faceRight ? "right" : "left")}");
    }

    /// <summary>
    /// Force flip to a specific direction.
    /// </summary>
    public void SetFacing(bool faceRight)
    {
        pendingFlip = false;
        ApplyFlip(faceRight);
    }

    /// <summary>
    /// Force flip to face a world position.
    /// </summary>
    public void FacePosition(Vector3 worldPosition)
    {
        if (cameraTransform == null) return;

        Vector3 direction = worldPosition - transform.position;
        Vector3 directionInCameraSpace = cameraTransform.InverseTransformDirection(direction);

        SetFacing(directionInCameraSpace.x > 0);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Set a custom camera for billboarding.
    /// </summary>
    public void SetCamera(Camera camera)
    {
        targetCamera = camera;
        cameraTransform = camera?.transform;
    }

    /// <summary>
    /// Set a custom rigidbody for velocity tracking.
    /// </summary>
    public void SetRigidbody(Rigidbody rb)
    {
        targetRigidbody = rb;
    }

    /// <summary>
    /// Set a custom physics controller for velocity tracking.
    /// </summary>
    public void SetPhysicsController(IcePhysicsController controller)
    {
        physicsController = controller;
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, cameraTransform.position);
        }

        // Draw facing indicator
        Gizmos.color = isFacingRight ? Color.green : Color.red;
        Vector3 facingDir = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawRay(transform.position, facingDir * 0.5f);
    }

    #endregion
}
