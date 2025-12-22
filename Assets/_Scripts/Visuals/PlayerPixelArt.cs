using UnityEngine;

/// <summary>
/// Applies procedurally generated 64x64 isometric pixel art to hockey players.
/// Replaces 3D capsule mesh with 2D sprite that billboards toward camera.
/// </summary>
[RequireComponent(typeof(HockeyPlayer))]
public class PlayerPixelArt : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float spriteScale = 2f;
    [SerializeField] private float spriteYOffset = 0.5f;
    [SerializeField] private bool billboardToCamera = true;
    [SerializeField] private bool hideMeshRenderer = true;
    [SerializeField] private bool createStickSprite = true;

    [Header("Stick Settings")]
    [SerializeField] private Vector3 stickOffset = new Vector3(0.4f, 0.2f, 0);
    [SerializeField] private float stickScale = 1.2f;

    [Header("Team Colors")]
    [SerializeField] private Color32 homeTeamColor = new Color32(51, 102, 230, 255); // Blue
    [SerializeField] private Color32 awayTeamColor = new Color32(230, 51, 51, 255);  // Red

    // Runtime references
    private HockeyPlayer player;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer stickRenderer;
    private GameObject spriteObject;
    private GameObject stickObject;
    private Camera mainCamera;
    private MeshRenderer meshRenderer;

    // Cached sprites for direction changes
    private Sprite spriteRight;
    private Sprite spriteLeft;
    private Sprite stickSprite;
    private bool lastFacingRight = true;

    private void Awake()
    {
        player = GetComponent<HockeyPlayer>();
        mainCamera = Camera.main;
        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void Start()
    {
        CreateSpriteObject();
        GenerateSprites();
        ApplySprite();

        // Create stick sprite
        if (createStickSprite && player.Position != PlayerPosition.Goalie)
        {
            CreateStickSprite();
        }

        // Hide 3D mesh if requested
        if (hideMeshRenderer && meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (spriteObject == null) return;

        // Billboard toward camera
        if (billboardToCamera && mainCamera != null)
        {
            // Face camera but stay upright
            Vector3 lookDir = mainCamera.transform.forward;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                spriteObject.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // Update facing direction based on movement
        UpdateFacingDirection();
    }

    /// <summary>
    /// Creates the child GameObject with SpriteRenderer.
    /// </summary>
    private void CreateSpriteObject()
    {
        spriteObject = new GameObject("PlayerSprite");
        spriteObject.transform.SetParent(transform);
        spriteObject.transform.localPosition = new Vector3(0, spriteYOffset, 0);
        spriteObject.transform.localScale = Vector3.one * spriteScale;

        spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 10;
    }

    /// <summary>
    /// Creates the hockey stick sprite as a child of the player sprite.
    /// </summary>
    private void CreateStickSprite()
    {
        stickObject = new GameObject("StickSprite");
        stickObject.transform.SetParent(spriteObject.transform);
        stickObject.transform.localPosition = stickOffset;
        stickObject.transform.localScale = Vector3.one * stickScale;
        stickObject.transform.localRotation = Quaternion.Euler(0, 0, -30f);

        stickRenderer = stickObject.AddComponent<SpriteRenderer>();
        stickRenderer.sortingOrder = 11; // Above player sprite

        // Generate stick texture
        Texture2D stickTex = PixelArtGenerator.GenerateStickSprite();
        stickSprite = Sprite.Create(stickTex, new Rect(0, 0, 32, 48), new Vector2(0.5f, 0), 32);
        stickRenderer.sprite = stickSprite;
    }

    /// <summary>
    /// Generates both left and right facing sprites.
    /// </summary>
    private void GenerateSprites()
    {
        Color32 teamColor = player.TeamId == 0 ? homeTeamColor : awayTeamColor;
        bool isGoalie = player.Position == PlayerPosition.Goalie;

        Texture2D texRight;
        Texture2D texLeft;

        if (isGoalie)
        {
            texRight = PixelArtGenerator.GenerateGoalieSprite(teamColor, true);
            texLeft = PixelArtGenerator.GenerateGoalieSprite(teamColor, false);
        }
        else
        {
            texRight = PixelArtGenerator.GeneratePlayerSprite(teamColor, player.Position, true);
            texLeft = PixelArtGenerator.GeneratePlayerSprite(teamColor, player.Position, false);
        }

        // Create sprites with pivot at bottom center
        spriteRight = Sprite.Create(texRight, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0), 64);
        spriteLeft = Sprite.Create(texLeft, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0), 64);
    }

    /// <summary>
    /// Applies the current sprite based on facing direction.
    /// </summary>
    private void ApplySprite()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.sprite = lastFacingRight ? spriteRight : spriteLeft;
    }

    /// <summary>
    /// Updates sprite based on movement direction.
    /// </summary>
    private void UpdateFacingDirection()
    {
        // Get velocity from Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude < 0.1f) return;

        // Determine facing based on X velocity relative to camera
        bool facingRight = velocity.x > 0.1f;
        bool facingLeft = velocity.x < -0.1f;

        if (facingRight && !lastFacingRight)
        {
            lastFacingRight = true;
            ApplySprite();
        }
        else if (facingLeft && lastFacingRight)
        {
            lastFacingRight = false;
            ApplySprite();
        }
    }

    /// <summary>
    /// Force refresh the sprite (e.g., after team change).
    /// </summary>
    public void RefreshSprite()
    {
        GenerateSprites();
        ApplySprite();
    }

    /// <summary>
    /// Set custom team color and regenerate sprite.
    /// </summary>
    public void SetTeamColor(Color32 color)
    {
        if (player.TeamId == 0)
            homeTeamColor = color;
        else
            awayTeamColor = color;

        RefreshSprite();
    }

    private void OnDestroy()
    {
        // Clean up generated textures
        if (spriteRight != null && spriteRight.texture != null)
            Destroy(spriteRight.texture);
        if (spriteLeft != null && spriteLeft.texture != null)
            Destroy(spriteLeft.texture);
        if (stickSprite != null && stickSprite.texture != null)
            Destroy(stickSprite.texture);
    }
}
