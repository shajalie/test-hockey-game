using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Controls a hockey team - manages players, switching, and team-level AI.
/// Singleton pattern for easy access from anywhere.
/// </summary>
public class TeamController : MonoBehaviour
{
    #region Singleton

    public static TeamController Instance { get; private set; }

    #endregion

    #region Events

    /// <summary>Fired when controlled player changes.</summary>
    public static event Action<PlayerInputController, PlayerInputController> OnPlayerSwitched;

    /// <summary>Fired when team formation changes.</summary>
    public static event Action<FormationType> OnFormationChanged;

    #endregion

    #region Enums

    public enum FormationType
    {
        Neutral,        // 2-1-2
        Offensive,      // 1-2-2 (push up)
        Defensive,      // 2-2-1 (fall back)
        Forecheck,      // Aggressive pressure
        TrapDefense     // Neutral zone trap
    }

    #endregion

    #region Serialized Fields

    [Header("Team Setup")]
    [SerializeField] private int teamId = 0;
    [SerializeField] private Color teamColor = Color.blue;
    [SerializeField] private string teamName = "Home Team";

    [Header("Players")]
    [SerializeField] private List<PlayerInputController> players = new List<PlayerInputController>();
    [SerializeField] private PlayerInputController goalie;

    [Header("Switching")]
    [SerializeField] private float switchCooldown = 0.3f;
    [SerializeField] private float autoSwitchDelay = 0.1f;
    [SerializeField] private bool autoSwitchOnPossession = true;
    [SerializeField] private bool smartDefenseSwitching = true;

    [Header("Formation")]
    [SerializeField] private FormationType currentFormation = FormationType.Neutral;

    [Header("Defense Analyzer")]
    [SerializeField] private float defenseAnalysisInterval = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    #endregion

    #region Private Fields

    private PlayerInputController controlledPlayer;
    private float switchCooldownTimer;
    private float defenseAnalysisTimer;

    // Defense analysis cache
    private PlayerInputController bestDefender;
    private List<PlayerInputController> sortedByDefenseValue = new List<PlayerInputController>();

    #endregion

    #region Properties

    /// <summary>Team ID (0 = home, 1 = away).</summary>
    public int TeamId => teamId;

    /// <summary>Team color for visuals.</summary>
    public Color TeamColor => teamColor;

    /// <summary>Team name.</summary>
    public string TeamName => teamName;

    /// <summary>Currently controlled player.</summary>
    public PlayerInputController ControlledPlayer => controlledPlayer;

    /// <summary>All players on this team (excluding goalie).</summary>
    public IReadOnlyList<PlayerInputController> Players => players;

    /// <summary>The goalie.</summary>
    public PlayerInputController Goalie => goalie;

    /// <summary>Current team formation.</summary>
    public FormationType Formation => currentFormation;

    /// <summary>Best defender to switch to.</summary>
    public PlayerInputController BestDefender => bestDefender;

    /// <summary>Whether switch is on cooldown.</summary>
    public bool CanSwitch => switchCooldownTimer <= 0;

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
        InitializePlayers();
        SubscribeToEvents();

        // Start with first player controlled
        if (players.Count > 0)
        {
            SwitchToPlayer(players[0]);
        }
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
        UpdateCooldowns();
        UpdateDefenseAnalysis();
    }

    #endregion

    #region Initialization

    private void InitializePlayers()
    {
        // Find all players if not assigned
        if (players.Count == 0)
        {
            PlayerInputController[] allPlayers = FindObjectsOfType<PlayerInputController>();
            foreach (var player in allPlayers)
            {
                // Check team ID through physics controller or other means
                HockeyPlayer hp = player.GetComponent<HockeyPlayer>();
                if (hp != null && hp.TeamId == teamId)
                {
                    if (hp.Position == PlayerPosition.Goalie)
                    {
                        goalie = player;
                    }
                    else
                    {
                        players.Add(player);
                    }
                }
            }
        }

        // Set all players to AI control initially
        foreach (var player in players)
        {
            player.EnableAIControl();
        }

        if (goalie != null)
        {
            goalie.EnableAIControl();
        }

        Debug.Log($"[TeamController] Initialized with {players.Count} players + goalie");
    }

    private void SubscribeToEvents()
    {
        PuckController.OnPossessionChanged += OnPuckPossessionChanged;
    }

    private void UnsubscribeFromEvents()
    {
        PuckController.OnPossessionChanged -= OnPuckPossessionChanged;
    }

    #endregion

    #region Player Switching

    /// <summary>
    /// Request a player switch (cycles to next best player).
    /// </summary>
    public void RequestPlayerSwitch()
    {
        if (!CanSwitch) return;

        // If we have a best defender and smart switching is on, use them
        if (smartDefenseSwitching && bestDefender != null && bestDefender != controlledPlayer)
        {
            SwitchToPlayer(bestDefender);
        }
        else
        {
            // Cycle to next player
            CycleToNextPlayer();
        }
    }

    /// <summary>
    /// Switch control to a specific player.
    /// </summary>
    public void SwitchToPlayer(PlayerInputController newPlayer)
    {
        if (newPlayer == null) return;
        if (newPlayer == controlledPlayer) return;
        if (!CanSwitch) return;

        PlayerInputController oldPlayer = controlledPlayer;

        // Disable old player's human control
        if (oldPlayer != null)
        {
            oldPlayer.EnableAIControl();
        }

        // Enable new player's human control
        newPlayer.EnableHumanControl();
        controlledPlayer = newPlayer;

        // Start cooldown
        switchCooldownTimer = switchCooldown;

        // Fire event
        OnPlayerSwitched?.Invoke(oldPlayer, newPlayer);

        Debug.Log($"[TeamController] Switched: {oldPlayer?.name ?? "None"} -> {newPlayer.name}");
    }

    /// <summary>
    /// Switch to the player nearest to the puck.
    /// </summary>
    public void SwitchToNearestToPuck()
    {
        PuckController puck = FindObjectOfType<PuckController>();
        if (puck == null) return;

        PlayerInputController nearest = GetNearestPlayerTo(puck.transform.position);
        if (nearest != null)
        {
            SwitchToPlayer(nearest);
        }
    }

    /// <summary>
    /// Cycle to the next player in the list.
    /// </summary>
    public void CycleToNextPlayer()
    {
        if (players.Count == 0) return;

        int currentIndex = players.IndexOf(controlledPlayer);
        int nextIndex = (currentIndex + 1) % players.Count;

        SwitchToPlayer(players[nextIndex]);
    }

    /// <summary>
    /// Cycle to the previous player in the list.
    /// </summary>
    public void CycleToPreviousPlayer()
    {
        if (players.Count == 0) return;

        int currentIndex = players.IndexOf(controlledPlayer);
        int prevIndex = (currentIndex - 1 + players.Count) % players.Count;

        SwitchToPlayer(players[prevIndex]);
    }

    /// <summary>
    /// Get the player nearest to a position.
    /// </summary>
    public PlayerInputController GetNearestPlayerTo(Vector3 position)
    {
        PlayerInputController nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var player in players)
        {
            float dist = Vector3.Distance(player.transform.position, position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = player;
            }
        }

        return nearest;
    }

    #endregion

    #region Defense Analysis

    private void UpdateDefenseAnalysis()
    {
        defenseAnalysisTimer -= Time.deltaTime;
        if (defenseAnalysisTimer > 0) return;

        defenseAnalysisTimer = defenseAnalysisInterval;
        AnalyzeDefensivePositions();
    }

    private void AnalyzeDefensivePositions()
    {
        PuckController puck = FindObjectOfType<PuckController>();
        if (puck == null) return;

        // Don't analyze if we have the puck
        if (puck.IsPossessed)
        {
            HockeyPlayer owner = puck.Owner?.GetComponent<HockeyPlayer>();
            if (owner != null && owner.TeamId == teamId)
            {
                bestDefender = null;
                return;
            }
        }

        Vector3 puckPos = puck.transform.position;
        Vector3 puckVel = puck.Velocity;

        // Score each player's defensive value
        sortedByDefenseValue.Clear();
        sortedByDefenseValue.AddRange(players);

        sortedByDefenseValue.Sort((a, b) =>
        {
            float scoreA = CalculateDefensiveValue(a, puckPos, puckVel);
            float scoreB = CalculateDefensiveValue(b, puckPos, puckVel);
            return scoreB.CompareTo(scoreA);
        });

        // Best defender is highest scoring player that isn't already controlled
        bestDefender = null;
        foreach (var player in sortedByDefenseValue)
        {
            if (player != controlledPlayer)
            {
                bestDefender = player;
                break;
            }
        }
    }

    private float CalculateDefensiveValue(PlayerInputController player, Vector3 puckPos, Vector3 puckVel)
    {
        float score = 0f;

        Vector3 playerPos = player.transform.position;
        float distToPuck = Vector3.Distance(playerPos, puckPos);

        // Proximity to puck (closer = better)
        score += Mathf.Max(0, 30f - distToPuck);

        // Interception potential (if puck is moving toward player)
        if (puckVel.magnitude > 1f)
        {
            Vector3 toPuck = (puckPos - playerPos).normalized;
            float interceptAngle = Vector3.Dot(puckVel.normalized, -toPuck);
            score += interceptAngle * 20f;
        }

        // Position relative to our goal (closer to goal when defending = better)
        Vector3 ourGoal = new Vector3(0, 0, teamId == 0 ? -26f : 26f);
        float distToGoal = Vector3.Distance(playerPos, ourGoal);
        float puckDistToGoal = Vector3.Distance(puckPos, ourGoal);

        // Bonus if between puck and goal
        if (distToGoal < puckDistToGoal)
        {
            score += 15f;
        }

        // Role bonus (defensemen should be preferred for defense)
        HockeyPlayer hp = player.GetComponent<HockeyPlayer>();
        if (hp != null)
        {
            if (hp.Position == PlayerPosition.LeftDefense || hp.Position == PlayerPosition.RightDefense)
            {
                score += 10f;
            }
        }

        return score;
    }

    #endregion

    #region Formation

    /// <summary>
    /// Set the team formation.
    /// </summary>
    public void SetFormation(FormationType formation)
    {
        if (currentFormation == formation) return;

        currentFormation = formation;
        OnFormationChanged?.Invoke(formation);

        Debug.Log($"[TeamController] Formation changed to: {formation}");
    }

    /// <summary>
    /// Get the target position for a player based on formation.
    /// </summary>
    public Vector3 GetFormationPosition(PlayerInputController player, Vector3 puckPos)
    {
        HockeyPlayer hp = player.GetComponent<HockeyPlayer>();
        if (hp == null) return player.transform.position;

        // Base positions depend on formation and player role
        float zOffset = teamId == 0 ? -1f : 1f;
        float baseZ = puckPos.z + (10f * zOffset);

        switch (hp.Position)
        {
            case PlayerPosition.Center:
                return new Vector3(puckPos.x * 0.5f, 0, baseZ);

            case PlayerPosition.LeftWing:
                return new Vector3(-8f, 0, baseZ + 5f * -zOffset);

            case PlayerPosition.RightWing:
                return new Vector3(8f, 0, baseZ + 5f * -zOffset);

            case PlayerPosition.LeftDefense:
                return new Vector3(-6f, 0, baseZ + 15f * zOffset);

            case PlayerPosition.RightDefense:
                return new Vector3(6f, 0, baseZ + 15f * zOffset);

            default:
                return player.transform.position;
        }
    }

    #endregion

    #region Event Handlers

    private void OnPuckPossessionChanged(PuckController puck, GameObject newOwner)
    {
        if (!autoSwitchOnPossession) return;

        if (newOwner != null)
        {
            // Check if our team got the puck
            HockeyPlayer hp = newOwner.GetComponent<HockeyPlayer>();
            if (hp != null && hp.TeamId == teamId)
            {
                // Switch to the player with the puck
                PlayerInputController ownerInput = newOwner.GetComponent<PlayerInputController>();
                if (ownerInput != null && ownerInput != controlledPlayer)
                {
                    // Delayed switch to allow for one-timers
                    Invoke(nameof(SwitchToPuckCarrier), autoSwitchDelay);
                }
            }
        }
    }

    private void SwitchToPuckCarrier()
    {
        PuckController puck = FindObjectOfType<PuckController>();
        if (puck == null || !puck.IsPossessed) return;

        PlayerInputController carrier = puck.Owner?.GetComponent<PlayerInputController>();
        if (carrier != null)
        {
            HockeyPlayer hp = carrier.GetComponent<HockeyPlayer>();
            if (hp != null && hp.TeamId == teamId)
            {
                SwitchToPlayer(carrier);
            }
        }
    }

    #endregion

    #region Cooldowns

    private void UpdateCooldowns()
    {
        if (switchCooldownTimer > 0)
        {
            switchCooldownTimer -= Time.deltaTime;
        }
    }

    #endregion

    #region Debug

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 12;
        style.alignment = TextAnchor.UpperLeft;

        string text = $"[{teamName}]\n";
        text += $"Controlled: {controlledPlayer?.name ?? "None"}\n";
        text += $"Best Defender: {bestDefender?.name ?? "None"}\n";
        text += $"Formation: {currentFormation}\n";
        text += $"Can Switch: {CanSwitch}\n";

        GUI.Box(new Rect(10, 10, 200, 110), text, style);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Draw controlled player indicator
        if (controlledPlayer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(controlledPlayer.transform.position + Vector3.up * 2.5f, 0.5f);
        }

        // Draw best defender indicator
        if (bestDefender != null && bestDefender != controlledPlayer)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bestDefender.transform.position + Vector3.up * 2.5f, 0.4f);
        }
    }

    #endregion
}
