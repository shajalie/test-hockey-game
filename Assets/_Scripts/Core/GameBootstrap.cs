using UnityEngine;

/// <summary>
/// Initializes the game scene with all required components.
/// Add this to an empty GameObject in your main scene.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject inputManagerPrefab;
    [SerializeField] private GameObject matchManagerPrefab;
    [SerializeField] private PlayerStats defaultPlayerStats;
    [SerializeField] private ArtifactDatabase artifactDatabase;

    [Header("Scene Setup")]
    [SerializeField] private bool autoStartRun = true;
    [SerializeField] private bool buildRinkOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool skipDraft = false;

    private void Awake()
    {
        Debug.Log("[GameBootstrap] Initializing game...");

        // Ensure required managers exist
        EnsureGameManager();
        EnsureInputManager();

        // Setup rink
        if (buildRinkOnStart)
        {
            SetupRink();
        }

        // Setup camera
        SetupCamera();
    }

    private void Start()
    {
        // Start a run if configured
        if (autoStartRun && GameManager.Instance != null)
        {
            if (skipDraft)
            {
                // Skip draft for quick testing
                GameManager.Instance.StartNewRun();
                GameManager.Instance.SkipDraft();
            }
            else
            {
                GameManager.Instance.StartNewRun();
            }
        }
    }

    private void EnsureGameManager()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("[GameBootstrap] GameManager already exists");
            return;
        }

        GameObject gmObj;

        if (gameManagerPrefab != null)
        {
            gmObj = Instantiate(gameManagerPrefab);
        }
        else
        {
            gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        // Configure GameManager
        var gm = gmObj.GetComponent<GameManager>();

        // Use reflection or serialization to set private fields if needed
        // For now, we assume the prefab is properly configured

        Debug.Log("[GameBootstrap] GameManager created");
    }

    private void EnsureInputManager()
    {
        if (InputManager.Instance != null)
        {
            Debug.Log("[GameBootstrap] InputManager already exists");
            return;
        }

        GameObject imObj;

        if (inputManagerPrefab != null)
        {
            imObj = Instantiate(inputManagerPrefab);
        }
        else
        {
            imObj = new GameObject("InputManager");
            imObj.AddComponent<InputManager>();
        }

        Debug.Log("[GameBootstrap] InputManager created");
    }

    private void SetupRink()
    {
        // Check if rink already exists
        var existingRink = FindObjectOfType<RinkBuilder>();
        if (existingRink != null)
        {
            Debug.Log("[GameBootstrap] Rink already exists");
            return;
        }

        // Create rink builder
        GameObject rinkObj = new GameObject("RinkBuilder");
        var rinkBuilder = rinkObj.AddComponent<RinkBuilder>();
        rinkBuilder.BuildRink();

        Debug.Log("[GameBootstrap] Rink created");
    }

    private void SetupCamera()
    {
        // Find main camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[GameBootstrap] No main camera found!");
            return;
        }

        // Add broadcast camera if not present
        var broadcastCam = mainCam.GetComponent<BroadcastCamera>();
        if (broadcastCam == null)
        {
            broadcastCam = mainCam.gameObject.AddComponent<BroadcastCamera>();
            Debug.Log("[GameBootstrap] BroadcastCamera added to main camera");
        }
    }

    /// <summary>
    /// Quick setup for testing - creates minimal playable scene.
    /// </summary>
    [ContextMenu("Quick Test Setup")]
    public void QuickTestSetup()
    {
        EnsureGameManager();
        EnsureInputManager();
        SetupRink();
        SetupCamera();

        // Create player
        var rink = FindObjectOfType<RinkBuilder>();
        if (rink != null)
        {
            // Player
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = rink.GetSpawnPoint("PlayerSpawn")?.position ?? new Vector3(-15, 0, 0);
            var player = playerObj.AddComponent<HockeyPlayer>();
            playerObj.AddComponent<ShootingController>();

            // Add collider and rigidbody (HockeyPlayer requires Rigidbody)
            var playerCol = playerObj.AddComponent<CapsuleCollider>();
            playerCol.height = 2f;
            playerCol.center = Vector3.up;

            playerObj.layer = LayerMask.NameToLayer("Player");
            playerObj.tag = "Player";

            // Visual placeholder
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(playerObj.transform);
            visual.transform.localPosition = Vector3.up;
            Destroy(visual.GetComponent<Collider>());

            // Stick tip (for puck magnet)
            GameObject stickTip = new GameObject("StickTip");
            stickTip.transform.SetParent(playerObj.transform);
            stickTip.transform.localPosition = new Vector3(0.5f, 0.3f, 0.8f);

            // AI Opponent
            GameObject aiObj = new GameObject("AI_Opponent");
            aiObj.transform.position = rink.GetSpawnPoint("AISpawn")?.position ?? new Vector3(15, 0, 0);
            var aiPlayer = aiObj.AddComponent<HockeyPlayer>();
            aiObj.AddComponent<ShootingController>();
            var aiController = aiObj.AddComponent<AIController>();

            var aiCol = aiObj.AddComponent<CapsuleCollider>();
            aiCol.height = 2f;
            aiCol.center = Vector3.up;

            aiObj.layer = LayerMask.NameToLayer("Player");

            var aiVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            aiVisual.transform.SetParent(aiObj.transform);
            aiVisual.transform.localPosition = Vector3.up;
            aiVisual.GetComponent<MeshRenderer>().material.color = Color.red;
            Destroy(aiVisual.GetComponent<Collider>());

            GameObject aiStickTip = new GameObject("StickTip");
            aiStickTip.transform.SetParent(aiObj.transform);
            aiStickTip.transform.localPosition = new Vector3(0.5f, 0.3f, 0.8f);

            // Setup AI goals
            aiController.SetGoals(rink.HomeGoal?.transform, rink.AwayGoal?.transform);

            // Puck
            GameObject puckObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puckObj.name = "Puck";
            puckObj.transform.position = rink.GetSpawnPoint("PuckSpawn")?.position ?? Vector3.up * 0.1f;
            puckObj.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);
            puckObj.GetComponent<MeshRenderer>().material.color = Color.black;

            var puck = puckObj.AddComponent<Puck>();
            puckObj.tag = "Puck";

            // Connect InputManager to player
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetControlledPlayer(player);
            }

            Debug.Log("[GameBootstrap] Quick test setup complete!");
        }
    }
}
