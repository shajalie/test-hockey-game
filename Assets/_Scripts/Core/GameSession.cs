using UnityEngine;
using System;

/// <summary>
/// Central game session controller.
/// Manages game state, teams, and core gameplay flow.
/// This is the main entry point for a hockey match.
/// </summary>
public class GameSession : MonoBehaviour
{
    #region Singleton

    public static GameSession Instance { get; private set; }

    #endregion

    #region Events

    /// <summary>Fired when match starts.</summary>
    public static event Action OnMatchStarted;

    /// <summary>Fired when period ends.</summary>
    public static event Action<int> OnPeriodEnded;

    /// <summary>Fired when goal is scored.</summary>
    public static event Action<int, int> OnGoalScored; // teamId, newScore

    /// <summary>Fired when match ends.</summary>
    public static event Action<int, int> OnMatchEnded; // homeScore, awayScore

    #endregion

    #region Enums

    public enum GameState
    {
        PreGame,
        Warmup,
        Faceoff,
        InPlay,
        GoalScored,
        PeriodEnd,
        Intermission,
        Overtime,
        PostGame
    }

    #endregion

    #region Serialized Fields

    [Header("Match Settings")]
    [SerializeField] private float periodLength = 180f; // 3 minutes
    [SerializeField] private int periodCount = 3;
    [SerializeField] private float faceoffDelay = 2f;
    [SerializeField] private float goalCelebrationTime = 3f;

    [Header("References")]
    [SerializeField] private TeamController homeTeam;
    [SerializeField] private TeamController awayTeam;
    [SerializeField] private PuckController puck;
    [SerializeField] private LongitudinalCamera gameCamera;

    [Header("Faceoff Spots")]
    [SerializeField] private Transform centerIce;
    [SerializeField] private Transform[] faceoffSpots;

    [Header("Faceoff")]
    [SerializeField] private FaceoffSystem faceoffSystem;

    [Header("Debug")]
    [SerializeField] private bool showDebugUI = false;

    #endregion

    #region Private Fields

    private GameState currentState = GameState.PreGame;
    private int currentPeriod = 1;
    private float periodTimer;
    private float stateTimer;

    private int homeScore;
    private int awayScore;

    private Transform nextFaceoffSpot;

    #endregion

    #region Properties

    /// <summary>Current game state.</summary>
    public GameState State => currentState;

    /// <summary>Current period (1-3+).</summary>
    public int CurrentPeriod => currentPeriod;

    /// <summary>Time remaining in period.</summary>
    public float PeriodTimeRemaining => periodTimer;

    /// <summary>Home team score.</summary>
    public int HomeScore => homeScore;

    /// <summary>Away team score.</summary>
    public int AwayScore => awayScore;

    /// <summary>Whether game is currently in play.</summary>
    public bool IsInPlay => currentState == GameState.InPlay;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        FindReferences();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        UpdateState();
    }

    #endregion

    #region Initialization

    private void FindReferences()
    {
        if (puck == null)
        {
            puck = FindObjectOfType<PuckController>();
        }

        if (gameCamera == null)
        {
            gameCamera = FindObjectOfType<LongitudinalCamera>();
        }

        if (homeTeam == null)
        {
            TeamController[] teams = FindObjectsOfType<TeamController>();
            foreach (var team in teams)
            {
                if (team.TeamId == 0) homeTeam = team;
                else if (team.TeamId == 1) awayTeam = team;
            }
        }

        if (centerIce == null)
        {
            GameObject center = new GameObject("CenterIce");
            center.transform.position = Vector3.zero;
            centerIce = center.transform;
        }

        // Auto-find FaceoffSystem
        if (faceoffSystem == null)
        {
            faceoffSystem = FindObjectOfType<FaceoffSystem>();
        }
    }

    private void SubscribeToEvents()
    {
        PuckController.OnGoalScored += HandleGoalScored;
    }

    private void UnsubscribeFromEvents()
    {
        PuckController.OnGoalScored -= HandleGoalScored;
    }

    #endregion

    #region Game Flow

    /// <summary>
    /// Start a new match.
    /// </summary>
    public void StartMatch()
    {
        homeScore = 0;
        awayScore = 0;
        currentPeriod = 1;
        periodTimer = periodLength;

        SetState(GameState.Faceoff);
        nextFaceoffSpot = centerIce;

        OnMatchStarted?.Invoke();

        Debug.Log("[GameSession] Match started!");
    }

    /// <summary>
    /// End the current match.
    /// </summary>
    public void EndMatch()
    {
        SetState(GameState.PostGame);
        OnMatchEnded?.Invoke(homeScore, awayScore);

        Debug.Log($"[GameSession] Match ended! Final: {homeScore} - {awayScore}");
    }

    private void SetState(GameState newState)
    {
        if (currentState == newState) return;

        OnStateExit(currentState);
        currentState = newState;
        stateTimer = 0f;
        OnStateEnter(newState);

        Debug.Log($"[GameSession] State: {newState}");
    }

    private void OnStateEnter(GameState state)
    {
        switch (state)
        {
            case GameState.Faceoff:
                SetupFaceoff();
                break;

            case GameState.InPlay:
                Time.timeScale = 1f;
                break;

            case GameState.GoalScored:
                Time.timeScale = 0.5f; // Slow-mo for celebration
                gameCamera?.Shake(0.5f, 0.3f);
                gameCamera?.ZoomPulse(45f, 0.5f);
                break;

            case GameState.PeriodEnd:
                Time.timeScale = 1f;
                OnPeriodEnded?.Invoke(currentPeriod);
                break;
        }
    }

    private void OnStateExit(GameState state)
    {
        switch (state)
        {
            case GameState.GoalScored:
                Time.timeScale = 1f;
                break;
        }
    }

    private void UpdateState()
    {
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case GameState.PreGame:
                // Waiting for start
                break;

            case GameState.Faceoff:
                if (stateTimer >= faceoffDelay)
                {
                    DropPuck();
                    SetState(GameState.InPlay);
                }
                break;

            case GameState.InPlay:
                UpdatePeriodTimer();
                break;

            case GameState.GoalScored:
                if (stateTimer >= goalCelebrationTime)
                {
                    nextFaceoffSpot = centerIce;
                    SetState(GameState.Faceoff);
                }
                break;

            case GameState.PeriodEnd:
                if (stateTimer >= 2f)
                {
                    AdvancePeriod();
                }
                break;
        }
    }

    private void UpdatePeriodTimer()
    {
        periodTimer -= Time.deltaTime;

        if (periodTimer <= 0)
        {
            periodTimer = 0;
            SetState(GameState.PeriodEnd);
        }
    }

    private void AdvancePeriod()
    {
        currentPeriod++;

        if (currentPeriod > periodCount)
        {
            // Check for tie
            if (homeScore == awayScore)
            {
                SetState(GameState.Overtime);
            }
            else
            {
                EndMatch();
            }
        }
        else
        {
            periodTimer = periodLength;
            nextFaceoffSpot = centerIce;
            SetState(GameState.Faceoff);
        }
    }

    #endregion

    #region Faceoff

    private void SetupFaceoff()
    {
        Vector3 faceoffPos = nextFaceoffSpot != null ? nextFaceoffSpot.position : Vector3.zero;

        // Use FaceoffSystem if available for full player positioning
        if (faceoffSystem != null)
        {
            faceoffSystem.StartFaceoff(faceoffPos);
            Debug.Log($"[GameSession] FaceoffSystem handling faceoff at {faceoffPos}");
            return;
        }

        // Fallback: just position the puck
        if (puck == null) return;

        puck.ResetToPosition(faceoffPos + Vector3.up * 0.5f);

        Debug.Log($"[GameSession] Faceoff at {faceoffPos} (no FaceoffSystem)");
    }

    private void DropPuck()
    {
        if (puck == null) return;

        // Drop puck to ice
        Vector3 dropPos = nextFaceoffSpot != null ? nextFaceoffSpot.position : Vector3.zero;
        dropPos.y = 0.1f;
        puck.ResetToPosition(dropPos);

        Debug.Log("[GameSession] Puck dropped!");
    }

    #endregion

    #region Scoring

    private void HandleGoalScored(PuckController puck, int goalTeamId)
    {
        // goalTeamId is the team whose goal was scored ON (they conceded)
        // So the OTHER team scored

        if (goalTeamId == 0)
        {
            // Scored on home team's goal = away team scores
            awayScore++;
            OnGoalScored?.Invoke(1, awayScore);
            Debug.Log($"[GameSession] GOAL! Away team scores! {homeScore} - {awayScore}");
        }
        else
        {
            // Scored on away team's goal = home team scores
            homeScore++;
            OnGoalScored?.Invoke(0, homeScore);
            Debug.Log($"[GameSession] GOAL! Home team scores! {homeScore} - {awayScore}");
        }

        SetState(GameState.GoalScored);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Pause the game.
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Resume the game.
    /// </summary>
    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Get formatted time string (M:SS).
    /// </summary>
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(periodTimer / 60f);
        int seconds = Mathf.FloorToInt(periodTimer % 60f);
        return $"{minutes}:{seconds:D2}";
    }

    #endregion

    #region Debug

    private void OnGUI()
    {
        if (!showDebugUI) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.alignment = TextAnchor.MiddleCenter;

        // Score display
        string scoreText = $"{homeScore} - {awayScore}";
        GUI.Box(new Rect(Screen.width / 2 - 50, 10, 100, 40), scoreText, style);

        // Time display
        string timeText = $"P{currentPeriod} {GetFormattedTime()}";
        GUI.Box(new Rect(Screen.width / 2 - 50, 55, 100, 30), timeText, style);

        // State display
        style.fontSize = 12;
        GUI.Box(new Rect(Screen.width / 2 - 50, 90, 100, 25), currentState.ToString(), style);
    }

    #endregion
}
