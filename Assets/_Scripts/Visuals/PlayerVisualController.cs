using UnityEngine;

/// <summary>
/// Controls player visuals for HD-2D style rendering.
/// Manages sprite display, billboarding, and animation states.
/// Works alongside 3D physics for the 2.5D look.
/// </summary>
public class PlayerVisualController : MonoBehaviour
{
    #region Enums

    public enum AnimationState
    {
        Idle,
        Skating,
        SprintSkating,
        Shooting,
        Passing,
        Checking,
        Stunned,
        Celebrating
    }

    #endregion

    #region Serialized Fields

    [Header("Sprite Setup")]
    [SerializeField] private SpriteRenderer bodySprite;
    [SerializeField] private SpriteRenderer shadowSprite;
    [SerializeField] private Transform visualRoot;

    [Header("Sprites - Idle")]
    [SerializeField] private Sprite idleSprite;

    [Header("Sprites - Skating")]
    [SerializeField] private Sprite[] skatingSprites;
    [SerializeField] private float skatingFrameRate = 12f;

    [Header("Sprites - Actions")]
    [SerializeField] private Sprite[] shootingSprites;
    [SerializeField] private Sprite[] passingSprites;
    [SerializeField] private Sprite[] checkingSprites;

    [Header("Billboard Settings")]
    [SerializeField] private bool billboardEnabled = true;
    [SerializeField] private bool flipBasedOnVelocity = true;
    [SerializeField] private float flipThreshold = 0.5f;

    [Header("Shadow Settings")]
    [SerializeField] private Vector3 shadowOffset = new Vector3(0.2f, 0.01f, 0f);
    [SerializeField] private float shadowScale = 0.8f;
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.3f);

    [Header("Team Colors")]
    [SerializeField] private Color jerseyPrimaryColor = Color.blue;
    [SerializeField] private Color jerseySecondaryColor = Color.white;

    [Header("Effects")]
    [SerializeField] private ParticleSystem iceSprayEffect;
    [SerializeField] private float sprayThreshold = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    #endregion

    #region Private Fields

    private IcePhysicsController physics;
    private Camera mainCamera;
    private Transform cameraTransform;

    private AnimationState currentState = AnimationState.Idle;
    private int currentFrame;
    private float frameTimer;

    private bool isFacingRight = true;
    private MaterialPropertyBlock propertyBlock;

    #endregion

    #region Properties

    /// <summary>Current animation state.</summary>
    public AnimationState CurrentState => currentState;

    /// <summary>Whether sprite is facing right.</summary>
    public bool IsFacingRight => isFacingRight;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        physics = GetComponentInParent<IcePhysicsController>();
        propertyBlock = new MaterialPropertyBlock();

        // Create visual hierarchy if not set up
        if (visualRoot == null)
        {
            SetupVisualHierarchy();
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }

        // Apply team colors
        ApplyTeamColors();
    }

    private void LateUpdate()
    {
        if (billboardEnabled)
        {
            UpdateBillboard();
        }

        if (flipBasedOnVelocity)
        {
            UpdateFlip();
        }

        UpdateAnimation();
        UpdateShadow();
        UpdateEffects();
    }

    #endregion

    #region Setup

    private void SetupVisualHierarchy()
    {
        // Create visual root
        GameObject rootObj = new GameObject("Visual");
        rootObj.transform.SetParent(transform);
        rootObj.transform.localPosition = new Vector3(0, 1f, 0);
        visualRoot = rootObj.transform;

        // Create body sprite
        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(visualRoot);
        bodyObj.transform.localPosition = Vector3.zero;
        bodySprite = bodyObj.AddComponent<SpriteRenderer>();
        bodySprite.sortingLayerName = "Players";
        bodySprite.sortingOrder = 0;

        // Create shadow sprite
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(transform);
        shadowObj.transform.localPosition = shadowOffset;
        shadowSprite = shadowObj.AddComponent<SpriteRenderer>();
        shadowSprite.sortingLayerName = "Shadows";
        shadowSprite.sortingOrder = 0;
        shadowSprite.color = shadowColor;

        Debug.Log("[PlayerVisualController] Visual hierarchy created");
    }

    #endregion

    #region Billboard

    private void UpdateBillboard()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            return;
        }

        // Face camera (Y-axis only)
        Vector3 dirToCamera = cameraTransform.position - visualRoot.position;
        dirToCamera.y = 0;

        if (dirToCamera.sqrMagnitude > 0.001f)
        {
            visualRoot.rotation = Quaternion.LookRotation(-dirToCamera);
        }
    }

    private void UpdateFlip()
    {
        if (physics == null) return;

        Vector3 velocity = physics.Velocity;
        velocity.y = 0;

        if (velocity.magnitude < flipThreshold) return;

        // Get velocity relative to camera
        Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
        float rightDot = Vector3.Dot(velocity.normalized, cameraRight);

        bool shouldFaceRight = rightDot > 0;

        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            bodySprite.flipX = !isFacingRight;
        }
    }

    #endregion

    #region Animation

    private void UpdateAnimation()
    {
        // Determine state from physics
        AnimationState targetState = DetermineAnimationState();

        if (targetState != currentState)
        {
            currentState = targetState;
            currentFrame = 0;
            frameTimer = 0;
        }

        // Update frame
        Sprite[] sprites = GetSpritesForState(currentState);
        if (sprites != null && sprites.Length > 0)
        {
            frameTimer += Time.deltaTime;
            float frameTime = 1f / GetFrameRate(currentState);

            if (frameTimer >= frameTime)
            {
                frameTimer -= frameTime;
                currentFrame = (currentFrame + 1) % sprites.Length;
            }

            bodySprite.sprite = sprites[currentFrame];
        }
        else if (idleSprite != null)
        {
            bodySprite.sprite = idleSprite;
        }

        // Update sorting order based on Z position
        UpdateSortingOrder();
    }

    private AnimationState DetermineAnimationState()
    {
        if (physics == null) return AnimationState.Idle;

        float speed = physics.CurrentSpeed;
        float maxSpeed = physics.EffectiveMaxSpeed;

        if (physics.IsDashing)
        {
            return AnimationState.Checking;
        }

        if (speed < 0.5f)
        {
            return AnimationState.Idle;
        }

        if (physics.IsSprinting || speed > maxSpeed * 0.8f)
        {
            return AnimationState.SprintSkating;
        }

        return AnimationState.Skating;
    }

    private Sprite[] GetSpritesForState(AnimationState state)
    {
        switch (state)
        {
            case AnimationState.Skating:
            case AnimationState.SprintSkating:
                return skatingSprites;
            case AnimationState.Shooting:
                return shootingSprites;
            case AnimationState.Passing:
                return passingSprites;
            case AnimationState.Checking:
                return checkingSprites;
            default:
                return null;
        }
    }

    private float GetFrameRate(AnimationState state)
    {
        float baseRate = skatingFrameRate;

        switch (state)
        {
            case AnimationState.SprintSkating:
                return baseRate * 1.5f;
            case AnimationState.Shooting:
            case AnimationState.Passing:
                return baseRate * 0.8f;
            default:
                return baseRate;
        }
    }

    private void UpdateSortingOrder()
    {
        // Sort by Z position for proper depth
        float z = transform.position.z;
        int order = Mathf.RoundToInt(-z * 10f); // Negative because higher Z = further back
        bodySprite.sortingOrder = order;
    }

    #endregion

    #region Shadow

    private void UpdateShadow()
    {
        if (shadowSprite == null) return;

        // Shadow follows player position
        shadowSprite.transform.position = transform.position + shadowOffset;
        shadowSprite.transform.localScale = Vector3.one * shadowScale;

        // Match body sprite
        shadowSprite.sprite = bodySprite.sprite;
        shadowSprite.flipX = bodySprite.flipX;
    }

    #endregion

    #region Effects

    private void UpdateEffects()
    {
        if (iceSprayEffect == null || physics == null) return;

        float speed = physics.CurrentSpeed;
        bool shouldSpray = speed > sprayThreshold && physics.IsGrounded;

        if (shouldSpray && !iceSprayEffect.isPlaying)
        {
            iceSprayEffect.Play();
        }
        else if (!shouldSpray && iceSprayEffect.isPlaying)
        {
            iceSprayEffect.Stop();
        }

        // Scale effect with speed
        if (shouldSpray)
        {
            var emission = iceSprayEffect.emission;
            emission.rateOverTime = speed * 2f;
        }
    }

    #endregion

    #region Customization

    /// <summary>
    /// Apply team colors to the player sprite.
    /// </summary>
    public void ApplyTeamColors()
    {
        if (bodySprite == null) return;

        bodySprite.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_PrimaryColor", jerseyPrimaryColor);
        propertyBlock.SetColor("_SecondaryColor", jerseySecondaryColor);
        bodySprite.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Set team colors.
    /// </summary>
    public void SetTeamColors(Color primary, Color secondary)
    {
        jerseyPrimaryColor = primary;
        jerseySecondaryColor = secondary;
        ApplyTeamColors();
    }

    /// <summary>
    /// Trigger a specific animation state.
    /// </summary>
    public void TriggerAnimation(AnimationState state, float duration = 0.5f)
    {
        currentState = state;
        currentFrame = 0;
        frameTimer = 0;

        // Auto-return to normal after duration
        CancelInvoke(nameof(ResetAnimation));
        Invoke(nameof(ResetAnimation), duration);
    }

    private void ResetAnimation()
    {
        // Will be overwritten by next Update
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Draw visual root position
        if (visualRoot != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(visualRoot.position, 0.2f);
        }

        // Draw facing direction
        Gizmos.color = isFacingRight ? Color.green : Color.red;
        Vector3 facingDir = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawRay(transform.position + Vector3.up, facingDir);
    }

    #endregion
}
