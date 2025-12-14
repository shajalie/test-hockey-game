using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Complete faceoff system for hockey game with proper positioning, countdown, and win detection.
/// Features 5 faceoff circles (center ice + 4 zone dots), visual indicators, and integration with MatchManager.
/// </summary>
public class FaceoffSystem : MonoBehaviour
{
    [Header("Faceoff Circle Positions")]
    [SerializeField] private Vector3 centerIcePosition = Vector3.zero;
    [SerializeField] private Vector3 homeLeftCircle = new Vector3(-15f, 0f, 7f);
    [SerializeField] private Vector3 homeRightCircle = new Vector3(-15f, 0f, -7f);
    [SerializeField] private Vector3 awayLeftCircle = new Vector3(15f, 0f, 7f);
    [SerializeField] private Vector3 awayRightCircle = new Vector3(15f, 0f, -7f);

    [Header("Faceoff Settings")]
    [SerializeField] private float faceoffCircleRadius = 4.5f; // NHL standard ~4.5m
    [SerializeField] private float playerSpacing = 1.0f; // Distance between centers
    [SerializeField] private float countdownDuration = 3f; // 3 seconds
    [SerializeField] private float dropDelay = 0.5f; // Delay after "DROP!" before puck is truly loose
    [SerializeField] private float puckDropHeight = 0.5f;
    [SerializeField] private float puckDropRandomness = 0.3f; // Random direction variance

    [Header("Player Detection")]
    [SerializeField] private float playerDetectionRadius = 10f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Visual Indicators")]
    [SerializeField] private bool showCircleHighlight = true;
    [SerializeField] private Color circleHighlightColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private float circlePulseSpeed = 2f;
    [SerializeField] private GameObject faceoffCirclePrefab; // Optional visual prefab

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Canvas faceoffCanvas;

    [Header("References")]
    [SerializeField] private RinkBuilder rinkBuilder;
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private Puck puck;

    // Faceoff state
    private FaceoffState currentState = FaceoffState.InPlay;
    private Vector3 currentFaceoffPosition;
    private GameObject activeCircleIndicator;

    // Player references for faceoff
    private HockeyPlayer homeCenter;
    private HockeyPlayer awayCenter;
    private List<HockeyPlayer> allPlayers = new List<HockeyPlayer>();

    // Countdown tracking
    private float countdownTimer;
    private int lastDisplayedCount = -1;

    // Puck tracking for win detection
    private bool trackingFaceoffWin = false;
    private float faceoffStartTime;

    // Properties
    public FaceoffState CurrentState => currentState;
    public Vector3 CurrentFaceoffPosition => currentFaceoffPosition;
    public bool IsFaceoffActive => currentState != FaceoffState.InPlay;

    // Events
    public event System.Action<Vector3> OnFaceoffStarted;
    public event System.Action<int> OnFaceoffWon; // Team index

    #region Initialization

    private void Awake()
    {
        // Auto-find references if not assigned
        if (rinkBuilder == null)
            rinkBuilder = FindObjectOfType<RinkBuilder>();

        if (matchManager == null)
            matchManager = FindObjectOfType<MatchManager>();

        if (puck == null)
            puck = FindObjectOfType<Puck>();

        // Initialize faceoff positions relative to rink if RinkBuilder exists
        if (rinkBuilder != null)
        {
            InitializeFaceoffPositionsFromRink();
        }
    }

    private void OnEnable()
    {
        // Subscribe to game events
        GameEvents.OnGoalScored += OnGoalScored;
        GameEvents.OnPeriodEnd += OnPeriodEnd;
        GameEvents.OnIntermissionStart += OnIntermissionStart;
        GameEvents.OnIcing += OnIcing;
        GameEvents.OnOffsides += OnOffsides;

        // Subscribe to puck events for win detection
        GameEvents.OnPuckPossessionChanged += OnPuckPossessionChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGoalScored -= OnGoalScored;
        GameEvents.OnPeriodEnd -= OnPeriodEnd;
        GameEvents.OnIntermissionStart -= OnIntermissionStart;
        GameEvents.OnIcing -= OnIcing;
        GameEvents.OnOffsides -= OnOffsides;
        GameEvents.OnPuckPossessionChanged -= OnPuckPossessionChanged;
    }

    /// <summary>
    /// Initialize faceoff positions relative to RinkBuilder dimensions.
    /// </summary>
    private void InitializeFaceoffPositionsFromRink()
    {
        float rinkLength = rinkBuilder.Length;
        float rinkWidth = rinkBuilder.Width;

        // Center ice
        centerIcePosition = rinkBuilder.CenterIce;

        // Zone faceoff dots (NHL standard: ~20ft from goal line, ~22ft apart)
        float dotDistanceFromGoal = rinkLength * 0.25f; // 25% from each end
        float dotLateralOffset = rinkWidth * 0.23f; // 23% from center

        // Home zone (left side)
        homeLeftCircle = new Vector3(-dotDistanceFromGoal, 0f, dotLateralOffset);
        homeRightCircle = new Vector3(-dotDistanceFromGoal, 0f, -dotLateralOffset);

        // Away zone (right side)
        awayLeftCircle = new Vector3(dotDistanceFromGoal, 0f, dotLateralOffset);
        awayRightCircle = new Vector3(dotDistanceFromGoal, 0f, -dotLateralOffset);

        Debug.Log($"[FaceoffSystem] Positions initialized relative to rink ({rinkLength}x{rinkWidth})");
    }

    #endregion

    #region Faceoff Execution

    /// <summary>
    /// Starts a faceoff at the specified position.
    /// </summary>
    public void StartFaceoff(Vector3 position)
    {
        // Find nearest faceoff circle
        currentFaceoffPosition = GetNearestFaceoffCircle(position);

        Debug.Log($"[FaceoffSystem] Starting faceoff at {currentFaceoffPosition}");

        // Trigger event
        OnFaceoffStarted?.Invoke(currentFaceoffPosition);
        GameEvents.TriggerFaceoffStart(currentFaceoffPosition);
        GameEvents.TriggerWhistle(); // Whistle blows

        // Start sequence
        StartCoroutine(FaceoffSequence());
    }

    /// <summary>
    /// Starts faceoff at center ice (default).
    /// </summary>
    public void StartFaceoff()
    {
        StartFaceoff(centerIcePosition);
    }

    /// <summary>
    /// Main faceoff sequence coroutine.
    /// </summary>
    private IEnumerator FaceoffSequence()
    {
        // State: Waiting for players
        currentState = FaceoffState.WaitingForPlayers;
        FindPlayers();
        PositionPlayersForFaceoff();

        // Show circle indicator
        if (showCircleHighlight)
        {
            CreateCircleIndicator();
        }

        yield return new WaitForSeconds(0.5f);

        // State: Ready
        currentState = FaceoffState.Ready;
        yield return new WaitForSeconds(0.5f);

        // State: Countdown
        currentState = FaceoffState.Countdown;
        countdownTimer = countdownDuration;
        lastDisplayedCount = -1;

        if (faceoffCanvas != null)
            faceoffCanvas.gameObject.SetActive(true);

        // Countdown loop
        while (countdownTimer > 0f)
        {
            countdownTimer -= Time.deltaTime;
            UpdateCountdownDisplay();
            yield return null;
        }

        // State: Drop
        currentState = FaceoffState.Drop;
        ShowCountdownText("DROP!");

        yield return new WaitForSeconds(dropDelay);

        DropPuck();

        // State: In Play
        yield return new WaitForSeconds(0.3f);
        currentState = FaceoffState.InPlay;

        // Hide UI
        if (faceoffCanvas != null)
            faceoffCanvas.gameObject.SetActive(false);

        // Destroy circle indicator
        if (activeCircleIndicator != null)
        {
            Destroy(activeCircleIndicator);
            activeCircleIndicator = null;
        }

        // Start tracking for faceoff win
        trackingFaceoffWin = true;
        faceoffStartTime = Time.time;
    }

    /// <summary>
    /// Finds all players on the ice for faceoff positioning.
    /// </summary>
    private void FindPlayers()
    {
        allPlayers.Clear();
        HockeyPlayer[] players = FindObjectsOfType<HockeyPlayer>();

        foreach (var player in players)
        {
            allPlayers.Add(player);

            // Find centers for faceoff
            if (player.TeamId == 0 && (player.Position == PlayerPosition.Center || homeCenter == null))
            {
                homeCenter = player;
            }
            else if (player.TeamId == 1 && (player.Position == PlayerPosition.Center || awayCenter == null))
            {
                awayCenter = player;
            }
        }

        Debug.Log($"[FaceoffSystem] Found {allPlayers.Count} players. Home center: {homeCenter?.name}, Away center: {awayCenter?.name}");
    }

    /// <summary>
    /// Positions players for faceoff - centers at dot, others spread out.
    /// </summary>
    public void PositionPlayersForFaceoff()
    {
        if (puck != null)
        {
            // Position puck above faceoff spot (will drop later)
            puck.transform.position = currentFaceoffPosition + Vector3.up * puckDropHeight;
            puck.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            puck.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            puck.LosePossession();
        }

        // Position home center (left side of dot, facing right)
        if (homeCenter != null)
        {
            Vector3 homeCenterPos = currentFaceoffPosition + Vector3.left * playerSpacing;
            homeCenterPos.y = 0f;
            homeCenter.transform.position = homeCenterPos;
            homeCenter.transform.rotation = Quaternion.LookRotation(Vector3.right);
            homeCenter.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            homeCenter.SetMoveInput(Vector2.zero);
        }

        // Position away center (right side of dot, facing left)
        if (awayCenter != null)
        {
            Vector3 awayCenterPos = currentFaceoffPosition + Vector3.right * playerSpacing;
            awayCenterPos.y = 0f;
            awayCenter.transform.position = awayCenterPos;
            awayCenter.transform.rotation = Quaternion.LookRotation(Vector3.left);
            awayCenter.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            awayCenter.SetMoveInput(Vector2.zero);
        }

        // Position other players around the circle
        PositionNonCenters();
    }

    /// <summary>
    /// Position wingers and defensemen around faceoff circle.
    /// </summary>
    private void PositionNonCenters()
    {
        // Simple formation: spread players around the circle
        List<HockeyPlayer> homePlayers = new List<HockeyPlayer>();
        List<HockeyPlayer> awayPlayers = new List<HockeyPlayer>();

        foreach (var player in allPlayers)
        {
            if (player == homeCenter || player == awayCenter)
                continue;

            if (player.Position == PlayerPosition.Goalie)
                continue; // Goalies stay in net

            if (player.TeamId == 0)
                homePlayers.Add(player);
            else
                awayPlayers.Add(player);
        }

        // Position home team players
        for (int i = 0; i < homePlayers.Count; i++)
        {
            float angle = (180f + 45f + (i * 30f)) * Mathf.Deg2Rad; // Spread behind center
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * faceoffCircleRadius;
            Vector3 position = currentFaceoffPosition + offset;
            position.y = 0f;

            homePlayers[i].transform.position = position;
            homePlayers[i].transform.rotation = Quaternion.LookRotation((currentFaceoffPosition - position).normalized);
            homePlayers[i].GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }

        // Position away team players
        for (int i = 0; i < awayPlayers.Count; i++)
        {
            float angle = (0f + 45f + (i * 30f)) * Mathf.Deg2Rad; // Spread behind center
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * faceoffCircleRadius;
            Vector3 position = currentFaceoffPosition + offset;
            position.y = 0f;

            awayPlayers[i].transform.position = position;
            awayPlayers[i].transform.rotation = Quaternion.LookRotation((currentFaceoffPosition - position).normalized);
            awayPlayers[i].GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Drops the puck with slight random direction.
    /// </summary>
    private void DropPuck()
    {
        if (puck == null) return;

        // Position puck at faceoff spot
        puck.transform.position = currentFaceoffPosition + Vector3.up * 0.1f;

        Rigidbody puckRb = puck.GetComponent<Rigidbody>();
        puckRb.linearVelocity = Vector3.zero;
        puckRb.angularVelocity = Vector3.zero;

        // Add slight random horizontal impulse for unpredictability
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;

        puckRb.AddForce(randomDirection * puckDropRandomness, ForceMode.Impulse);

        // Ensure puck is loose
        puck.LosePossession();

        Debug.Log($"[FaceoffSystem] Puck dropped with random impulse: {randomDirection * puckDropRandomness}");
    }

    #endregion

    #region Faceoff Win Detection

    /// <summary>
    /// Tracks which player touches puck first after faceoff.
    /// </summary>
    private void OnPuckPossessionChanged(GameObject newOwner)
    {
        if (!trackingFaceoffWin) return;
        if (newOwner == null) return;

        HockeyPlayer player = newOwner.GetComponent<HockeyPlayer>();
        if (player == null) return;

        // Determine winning team
        int winningTeam = player.TeamId;

        // Only count if touched within 3 seconds of faceoff
        if (Time.time - faceoffStartTime < 3f)
        {
            OnFaceoffWon?.Invoke(winningTeam);
            GameEvents.TriggerFaceoffWon(winningTeam);

            // Update team stats
            Team team = FindObjectOfType<TeamManager>()?.GetTeam(winningTeam);
            if (team != null)
            {
                team.Stats.faceoffsWon++;
            }

            Debug.Log($"[FaceoffSystem] Faceoff won by Team {winningTeam} ({player.name})!");
        }

        trackingFaceoffWin = false;
    }

    #endregion

    #region Visual Indicators

    /// <summary>
    /// Creates visual circle indicator at faceoff position.
    /// </summary>
    private void CreateCircleIndicator()
    {
        if (faceoffCirclePrefab != null)
        {
            // Use prefab if provided
            activeCircleIndicator = Instantiate(faceoffCirclePrefab, currentFaceoffPosition, Quaternion.identity);
        }
        else
        {
            // Create simple circle using line renderer
            activeCircleIndicator = new GameObject("FaceoffCircleIndicator");
            activeCircleIndicator.transform.position = currentFaceoffPosition;

            LineRenderer lineRenderer = activeCircleIndicator.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 50;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;

            // Create circle points
            for (int i = 0; i < 50; i++)
            {
                float angle = (i / 49f) * 2f * Mathf.PI;
                float x = Mathf.Cos(angle) * faceoffCircleRadius;
                float z = Mathf.Sin(angle) * faceoffCircleRadius;
                lineRenderer.SetPosition(i, new Vector3(x, 0.05f, z));
            }

            // Set material color
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = circleHighlightColor;
            lineRenderer.material = mat;

            // Add pulsing effect
            FaceoffCirclePulse pulse = activeCircleIndicator.AddComponent<FaceoffCirclePulse>();
            pulse.pulseSpeed = circlePulseSpeed;
            pulse.lineRenderer = lineRenderer;
        }
    }

    /// <summary>
    /// Updates countdown display text.
    /// </summary>
    private void UpdateCountdownDisplay()
    {
        int currentCount = Mathf.CeilToInt(countdownTimer);

        if (currentCount != lastDisplayedCount && currentCount > 0)
        {
            lastDisplayedCount = currentCount;
            ShowCountdownText(currentCount.ToString());
        }
    }

    /// <summary>
    /// Shows text in countdown UI.
    /// </summary>
    private void ShowCountdownText(string text)
    {
        if (countdownText != null)
        {
            countdownText.text = text;
            countdownText.fontSize = 120;

            // Pulse effect
            StartCoroutine(PulseText());
        }

        Debug.Log($"[FaceoffSystem] {text}");
    }

    /// <summary>
    /// Text pulse animation.
    /// </summary>
    private IEnumerator PulseText()
    {
        if (countdownText == null) yield break;

        float duration = 0.5f;
        float elapsed = 0f;
        float startSize = 120f;
        float endSize = 80f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            countdownText.fontSize = Mathf.Lerp(startSize, endSize, t);
            yield return null;
        }
    }

    #endregion

    #region Faceoff Position Selection

    /// <summary>
    /// Gets the nearest official faceoff circle to a given position.
    /// </summary>
    public Vector3 GetNearestFaceoffCircle(Vector3 position)
    {
        Vector3[] faceoffCircles = new Vector3[]
        {
            centerIcePosition,
            homeLeftCircle,
            homeRightCircle,
            awayLeftCircle,
            awayRightCircle
        };

        Vector3 nearest = centerIcePosition;
        float nearestDistance = float.MaxValue;

        foreach (var circle in faceoffCircles)
        {
            float distance = Vector3.Distance(position, circle);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = circle;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Gets faceoff position after goal (attacking zone of team that was scored on).
    /// </summary>
    public Vector3 GetFaceoffPositionAfterGoal(int scoringTeam)
    {
        // Faceoff in the zone of the team that was scored on
        if (scoringTeam == 0)
        {
            // Home team scored - faceoff in away zone
            return Random.value > 0.5f ? awayLeftCircle : awayRightCircle;
        }
        else
        {
            // Away team scored - faceoff in home zone
            return Random.value > 0.5f ? homeLeftCircle : homeRightCircle;
        }
    }

    /// <summary>
    /// Gets faceoff position after icing (defensive zone of team that iced).
    /// </summary>
    public Vector3 GetFaceoffPositionAfterIcing(int icingTeam)
    {
        if (icingTeam == 0)
        {
            // Home team iced - faceoff in home zone
            return Random.value > 0.5f ? homeLeftCircle : homeRightCircle;
        }
        else
        {
            // Away team iced - faceoff in away zone
            return Random.value > 0.5f ? awayLeftCircle : awayRightCircle;
        }
    }

    #endregion

    #region Event Handlers

    private void OnGoalScored(int teamIndex)
    {
        // Start faceoff at center ice after goal
        Invoke(nameof(StartCenterIceFaceoff), 3f);
    }

    private void OnPeriodEnd(int periodNumber)
    {
        // Faceoff at center ice when new period starts (handled by MatchManager)
    }

    private void OnIntermissionStart()
    {
        // Reset state during intermission
        currentState = FaceoffState.InPlay;
    }

    private void OnIcing(int teamIndex)
    {
        // Faceoff in defensive zone of team that iced
        Vector3 position = GetFaceoffPositionAfterIcing(teamIndex);
        Invoke(nameof(StartDelayedFaceoff), 2f);
    }

    private void OnOffsides(int teamIndex)
    {
        // Faceoff at nearest circle
        Invoke(nameof(StartDelayedFaceoff), 2f);
    }

    private void StartCenterIceFaceoff()
    {
        StartFaceoff(centerIcePosition);
    }

    private void StartDelayedFaceoff()
    {
        StartFaceoff(currentFaceoffPosition);
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        // Draw all faceoff circles
        DrawFaceoffCircle(centerIcePosition, Color.red);
        DrawFaceoffCircle(homeLeftCircle, Color.blue);
        DrawFaceoffCircle(homeRightCircle, Color.blue);
        DrawFaceoffCircle(awayLeftCircle, Color.green);
        DrawFaceoffCircle(awayRightCircle, Color.green);

        // Highlight active faceoff position
        if (IsFaceoffActive)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentFaceoffPosition, faceoffCircleRadius * 1.1f);
        }
    }

    private void DrawFaceoffCircle(Vector3 position, Color color)
    {
        Gizmos.color = color;

        // Draw circle outline
        int segments = 32;
        Vector3 previousPoint = position + new Vector3(faceoffCircleRadius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * 2f * Mathf.PI;
            Vector3 newPoint = position + new Vector3(
                Mathf.Cos(angle) * faceoffCircleRadius,
                0f,
                Mathf.Sin(angle) * faceoffCircleRadius
            );

            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }

        // Draw center dot
        Gizmos.DrawWireSphere(position, 0.3f);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Manually trigger faceoff at specific circle.
    /// </summary>
    public void TriggerFaceoffAtCircle(FaceoffCircle circle)
    {
        Vector3 position = circle switch
        {
            FaceoffCircle.CenterIce => centerIcePosition,
            FaceoffCircle.HomeLeft => homeLeftCircle,
            FaceoffCircle.HomeRight => homeRightCircle,
            FaceoffCircle.AwayLeft => awayLeftCircle,
            FaceoffCircle.AwayRight => awayRightCircle,
            _ => centerIcePosition
        };

        StartFaceoff(position);
    }

    /// <summary>
    /// Gets the current faceoff circle enum based on position.
    /// </summary>
    public FaceoffCircle GetCurrentCircle()
    {
        if (currentFaceoffPosition == centerIcePosition)
            return FaceoffCircle.CenterIce;
        else if (currentFaceoffPosition == homeLeftCircle)
            return FaceoffCircle.HomeLeft;
        else if (currentFaceoffPosition == homeRightCircle)
            return FaceoffCircle.HomeRight;
        else if (currentFaceoffPosition == awayLeftCircle)
            return FaceoffCircle.AwayLeft;
        else if (currentFaceoffPosition == awayRightCircle)
            return FaceoffCircle.AwayRight;

        return FaceoffCircle.CenterIce;
    }

    #endregion
}

#region Enums and Helper Classes

/// <summary>
/// Faceoff state machine states.
/// </summary>
public enum FaceoffState
{
    WaitingForPlayers,  // Positioning players
    Ready,               // Players in position, about to start countdown
    Countdown,           // 3, 2, 1...
    Drop,                // DROP! - puck is being dropped
    InPlay               // Puck has been dropped, play continues
}

/// <summary>
/// Faceoff circle locations.
/// </summary>
public enum FaceoffCircle
{
    CenterIce,
    HomeLeft,
    HomeRight,
    AwayLeft,
    AwayRight
}

/// <summary>
/// Simple component for pulsing faceoff circle visual.
/// </summary>
public class FaceoffCirclePulse : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float pulseSpeed = 2f;

    private Color baseColor;

    private void Start()
    {
        if (lineRenderer != null)
        {
            baseColor = lineRenderer.material.color;
        }
    }

    private void Update()
    {
        if (lineRenderer == null) return;

        float alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; // 0 to 1
        alpha = Mathf.Lerp(0.3f, 0.8f, alpha);

        Color pulseColor = baseColor;
        pulseColor.a = alpha;
        lineRenderer.material.color = pulseColor;
    }
}

#endregion
