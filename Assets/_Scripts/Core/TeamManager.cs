using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Complete team management system for hockey game.
/// Manages 2 teams (home/away), player composition, stats tracking,
/// player switching, formations, and line changes.
/// </summary>
public class TeamManager : MonoBehaviour
{
    [Header("Team Configuration")]
    [SerializeField] private TeamData homeTeamData;
    [SerializeField] private TeamData awayTeamData;

    [Header("Spawn Settings")]
    [SerializeField] private Transform homeSpawnRoot;
    [SerializeField] private Transform awaySpawnRoot;
    [SerializeField] private Transform homeGoalTransform;
    [SerializeField] private Transform awayGoalTransform;

    [Header("Player Switching")]
    [SerializeField] private bool allowPlayerSwitching = true;
    [SerializeField] private float switchCooldown = 0.5f;
    [SerializeField] private float autoSwitchDistance = 5f; // Auto-switch to nearest player to puck

    [Header("Line Changes")]
    [SerializeField] private bool enableLineChanges = true;
    [SerializeField] private float staminaDrainRate = 1f; // Stamina per second
    [SerializeField] private float staminaRegenRate = 2f; // Stamina per second when benched
    [SerializeField] private float lineChangeTime = 2f; // Time for substitution

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showTeamStats = true;

    // Team instances
    private Team homeTeam;
    private Team awayTeam;

    // Player control
    private HockeyPlayer controlledPlayer;
    private float lastSwitchTime;

    // Events
    public event Action<HockeyPlayer> OnPlayerSwitched;
    public event Action<Team> OnGoalScored;
    public event Action<TeamPlayerInfo, TeamPlayerInfo> OnLineChange;

    // Properties
    public Team HomeTeam => homeTeam;
    public Team AwayTeam => awayTeam;
    public HockeyPlayer ControlledPlayer => controlledPlayer;

    private void Awake()
    {
        InitializeTeams();
    }

    private void Start()
    {
        SpawnTeams();
        SetupInitialControl();
        SubscribeToEvents();
    }

    private void Update()
    {
        if (enableLineChanges)
        {
            UpdateStamina();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #region Initialization

    /// <summary>
    /// Initialize team data structures.
    /// </summary>
    private void InitializeTeams()
    {
        homeTeam = new Team(homeTeamData, 0, true);
        awayTeam = new Team(awayTeamData, 1, false);

        Debug.Log($"[TeamManager] Teams initialized: {homeTeam.Name} vs {awayTeam.Name}");
    }

    /// <summary>
    /// Spawn all players for both teams.
    /// </summary>
    private void SpawnTeams()
    {
        SpawnTeam(homeTeam, homeSpawnRoot, true);
        SpawnTeam(awayTeam, awaySpawnRoot, false);

        Debug.Log($"[TeamManager] Teams spawned: {homeTeam.Players.Count} home, {awayTeam.Players.Count} away");
    }

    /// <summary>
    /// Spawn individual team with proper positioning.
    /// </summary>
    private void SpawnTeam(Team team, Transform spawnRoot, bool defendingLeftSide)
    {
        if (team.Data == null)
        {
            Debug.LogError($"[TeamManager] No team data for {team.Name}!");
            return;
        }

        Vector3 rootPosition = spawnRoot != null ? spawnRoot.position : Vector3.zero;

        // Spawn goalie
        SpawnPlayer(team, PlayerPosition.Goalie, rootPosition, defendingLeftSide);

        // Spawn skaters based on team composition
        int skatersCount = team.Data.skatersOnIce;

        // Always spawn center
        SpawnPlayer(team, PlayerPosition.Center, rootPosition, defendingLeftSide);

        // Spawn wings if we have enough skaters (4-5 skaters)
        if (skatersCount >= 4)
        {
            SpawnPlayer(team, PlayerPosition.LeftWing, rootPosition, defendingLeftSide);
            SpawnPlayer(team, PlayerPosition.RightWing, rootPosition, defendingLeftSide);
        }

        // Spawn defense (for 3v3: 2 defenders, for 4v4+: both defenders)
        if (skatersCount >= 3)
        {
            SpawnPlayer(team, PlayerPosition.LeftDefense, rootPosition, defendingLeftSide);
        }

        if (skatersCount >= 5)
        {
            SpawnPlayer(team, PlayerPosition.RightDefense, rootPosition, defendingLeftSide);
        }
        else if (skatersCount == 4)
        {
            // 4v4: spawn right defense instead of left for variety
            SpawnPlayer(team, PlayerPosition.RightDefense, rootPosition, defendingLeftSide);
        }
    }

    /// <summary>
    /// Spawn individual player at position.
    /// </summary>
    private void SpawnPlayer(Team team, PlayerPosition position, Vector3 rootPosition, bool defendingLeftSide)
    {
        // Select appropriate prefab and stats
        GameObject prefab = position == PlayerPosition.Goalie
            ? team.Data.goaliePrefab
            : team.Data.skaterPrefab;

        PlayerStats stats = position == PlayerPosition.Goalie
            ? team.Data.goalieStats
            : team.Data.skaterStats;

        if (prefab == null)
        {
            Debug.LogError($"[TeamManager] Missing prefab for {position} on {team.Name}!");
            return;
        }

        // Calculate spawn position
        Vector3 positionOffset = team.Data.GetPositionOffset(position, defendingLeftSide);
        Vector3 spawnPosition = rootPosition + positionOffset;

        // Spawn player
        GameObject playerObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        playerObj.name = $"{team.Name}_{position}";

        // Configure player component
        HockeyPlayer player = playerObj.GetComponent<HockeyPlayer>();
        if (player == null)
        {
            Debug.LogError($"[TeamManager] Spawned prefab missing HockeyPlayer component!");
            Destroy(playerObj);
            return;
        }

        // Set player stats (if stats reference exists on prefab, update it)
        // Note: This assumes HockeyPlayer exposes a way to set stats or we use reflection
        // For now, we'll rely on the prefab having the correct stats pre-assigned

        // Configure AI if not goalie and not player-controlled team
        AIController ai = playerObj.GetComponent<AIController>();
        if (ai != null && !team.IsPlayerControlled)
        {
            ai.SetDifficulty(team.Data.aiDifficulty);
            ai.SetGoals(
                defendingLeftSide ? awayGoalTransform : homeGoalTransform,
                defendingLeftSide ? homeGoalTransform : awayGoalTransform
            );
        }
        else if (ai != null && team.IsPlayerControlled)
        {
            // Disable AI for player-controlled team (except when not controlling this player)
            ai.enabled = false;
        }

        // Create team player info
        TeamPlayerInfo playerInfo = new TeamPlayerInfo
        {
            player = player,
            position = position,
            homePosition = spawnPosition,
            isOnIce = true,
            stamina = 100f,
            aiController = ai
        };

        team.AddPlayer(playerInfo);

        // Parent to team root for organization
        if (spawnPosition != null)
        {
            playerObj.transform.SetParent(team.IsHome ? homeSpawnRoot : awaySpawnRoot);
        }
    }

    /// <summary>
    /// Setup initial player control for home team.
    /// </summary>
    private void SetupInitialControl()
    {
        if (homeTeam.Players.Count > 0)
        {
            // Start controlling the center or first available skater
            TeamPlayerInfo centerPlayer = homeTeam.Players
                .FirstOrDefault(p => p.position == PlayerPosition.Center);

            if (centerPlayer != null)
            {
                SwitchToPlayer(centerPlayer.player);
            }
            else
            {
                SwitchToPlayer(homeTeam.Players[0].player);
            }
        }
    }

    private void SubscribeToEvents()
    {
        GameEvents.OnGoalScored += OnGoalScoredHandler;
        GameEvents.OnPuckPossessionChanged += OnPuckPossessionChanged;
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnGoalScored -= OnGoalScoredHandler;
        GameEvents.OnPuckPossessionChanged -= OnPuckPossessionChanged;
    }

    #endregion

    #region Player Switching

    /// <summary>
    /// Switch control to a specific player.
    /// </summary>
    public void SwitchToPlayer(HockeyPlayer newPlayer)
    {
        if (newPlayer == null || newPlayer == controlledPlayer)
            return;

        if (Time.time - lastSwitchTime < switchCooldown)
            return;

        // Disable previous player control
        if (controlledPlayer != null)
        {
            TeamPlayerInfo oldInfo = homeTeam.GetPlayerInfo(controlledPlayer);
            if (oldInfo != null && oldInfo.aiController != null)
            {
                oldInfo.aiController.enabled = true; // Re-enable AI
            }
        }

        // Enable new player control
        controlledPlayer = newPlayer;
        lastSwitchTime = Time.time;

        TeamPlayerInfo newInfo = homeTeam.GetPlayerInfo(newPlayer);
        if (newInfo != null && newInfo.aiController != null)
        {
            newInfo.aiController.enabled = false; // Disable AI
        }

        OnPlayerSwitched?.Invoke(controlledPlayer);
        Debug.Log($"[TeamManager] Switched to {newInfo?.position ?? PlayerPosition.Center}");
    }

    /// <summary>
    /// Switch to nearest player to puck.
    /// </summary>
    public void SwitchToNearestPlayer()
    {
        if (!allowPlayerSwitching) return;

        Puck puck = FindObjectOfType<Puck>();
        if (puck == null) return;

        TeamPlayerInfo nearest = homeTeam.GetNearestPlayerToPuck(puck.transform.position);
        if (nearest != null && nearest.isOnIce)
        {
            SwitchToPlayer(nearest.player);
        }
    }

    /// <summary>
    /// Switch to next player in rotation.
    /// </summary>
    public void SwitchToNextPlayer()
    {
        if (!allowPlayerSwitching) return;

        List<TeamPlayerInfo> activePlayers = homeTeam.Players
            .Where(p => p.isOnIce && p.position != PlayerPosition.Goalie)
            .ToList();

        if (activePlayers.Count == 0) return;

        int currentIndex = activePlayers.FindIndex(p => p.player == controlledPlayer);
        int nextIndex = (currentIndex + 1) % activePlayers.Count;

        SwitchToPlayer(activePlayers[nextIndex].player);
    }

    /// <summary>
    /// Switch to previous player in rotation.
    /// </summary>
    public void SwitchToPreviousPlayer()
    {
        if (!allowPlayerSwitching) return;

        List<TeamPlayerInfo> activePlayers = homeTeam.Players
            .Where(p => p.isOnIce && p.position != PlayerPosition.Goalie)
            .ToList();

        if (activePlayers.Count == 0) return;

        int currentIndex = activePlayers.FindIndex(p => p.player == controlledPlayer);
        int prevIndex = (currentIndex - 1 + activePlayers.Count) % activePlayers.Count;

        SwitchToPlayer(activePlayers[prevIndex].player);
    }

    #endregion

    #region Team Stats

    /// <summary>
    /// Record a goal for a team.
    /// </summary>
    public void RecordGoal(int teamIndex, HockeyPlayer scorer = null)
    {
        Team team = teamIndex == 0 ? homeTeam : awayTeam;
        team.Stats.goals++;

        if (scorer != null)
        {
            TeamPlayerInfo scorerInfo = team.GetPlayerInfo(scorer);
            if (scorerInfo != null)
            {
                scorerInfo.goals++;
            }
        }

        OnGoalScored?.Invoke(team);
        Debug.Log($"[TeamManager] GOAL! {team.Name}: {team.Stats.goals}");
    }

    /// <summary>
    /// Record a shot for a team.
    /// </summary>
    public void RecordShot(Team team, HockeyPlayer shooter = null)
    {
        team.Stats.shots++;

        if (shooter != null)
        {
            TeamPlayerInfo shooterInfo = team.GetPlayerInfo(shooter);
            if (shooterInfo != null)
            {
                shooterInfo.shots++;
            }
        }
    }

    /// <summary>
    /// Record a hit for a team.
    /// </summary>
    public void RecordHit(Team team, HockeyPlayer hitter = null)
    {
        team.Stats.hits++;

        if (hitter != null)
        {
            TeamPlayerInfo hitterInfo = team.GetPlayerInfo(hitter);
            if (hitterInfo != null)
            {
                hitterInfo.hits++;
            }
        }
    }

    /// <summary>
    /// Get team by index.
    /// </summary>
    public Team GetTeam(int teamIndex)
    {
        return teamIndex == 0 ? homeTeam : awayTeam;
    }

    #endregion

    #region Formation System

    /// <summary>
    /// Set formation for a team.
    /// </summary>
    public void SetFormation(Team team, FormationType formation)
    {
        team.CurrentFormation = formation;
        UpdatePlayerPositions(team);
        Debug.Log($"[TeamManager] {team.Name} formation set to {formation}");
    }

    /// <summary>
    /// Update all player positions based on current formation.
    /// </summary>
    private void UpdatePlayerPositions(Team team)
    {
        bool defendingLeftSide = team.IsHome;
        Vector3 rootPosition = team.IsHome ? homeSpawnRoot.position : awaySpawnRoot.position;

        foreach (var playerInfo in team.Players)
        {
            if (!playerInfo.isOnIce) continue;

            Vector3 formationOffset = GetFormationOffset(playerInfo.position, team.CurrentFormation, defendingLeftSide);
            playerInfo.homePosition = rootPosition + formationOffset;

            // Update AI home position if applicable
            if (playerInfo.aiController != null)
            {
                // AI will use homePosition for ReturnToPosition state
                // This would require extending AIController to support dynamic home positions
            }
        }
    }

    /// <summary>
    /// Get position offset based on formation type.
    /// </summary>
    private Vector3 GetFormationOffset(PlayerPosition position, FormationType formation, bool defendingLeftSide)
    {
        float direction = defendingLeftSide ? -1f : 1f;

        // Base offsets (modify based on formation)
        Vector3 baseOffset = homeTeamData.GetPositionOffset(position, defendingLeftSide);

        switch (formation)
        {
            case FormationType.Defensive:
                // Pull everyone back 5 units
                if (position != PlayerPosition.Goalie)
                {
                    baseOffset.x -= direction * 5f;
                }
                break;

            case FormationType.Offensive:
                // Push everyone forward 5 units
                if (position != PlayerPosition.Goalie)
                {
                    baseOffset.x += direction * 5f;
                }
                break;

            case FormationType.TrapDefense:
                // Compress to neutral zone
                if (position == PlayerPosition.Center || position == PlayerPosition.LeftWing || position == PlayerPosition.RightWing)
                {
                    baseOffset.x = direction * 5f; // Neutral zone
                }
                break;

            case FormationType.Forecheck:
                // Aggressive forward positioning
                if (position != PlayerPosition.Goalie && position != PlayerPosition.LeftDefense && position != PlayerPosition.RightDefense)
                {
                    baseOffset.x += direction * 8f;
                }
                break;

            case FormationType.Balanced:
            default:
                // Use base offset
                break;
        }

        return baseOffset;
    }

    #endregion

    #region Line Changes / Substitutions

    /// <summary>
    /// Update stamina for all players.
    /// </summary>
    private void UpdateStamina()
    {
        UpdateTeamStamina(homeTeam);
        UpdateTeamStamina(awayTeam);
    }

    private void UpdateTeamStamina(Team team)
    {
        foreach (var playerInfo in team.Players)
        {
            if (playerInfo.isOnIce)
            {
                // Drain stamina
                playerInfo.stamina -= staminaDrainRate * Time.deltaTime;
                playerInfo.stamina = Mathf.Max(0f, playerInfo.stamina);

                // Auto line change if stamina depleted
                if (playerInfo.stamina <= 0f && playerInfo.position != PlayerPosition.Goalie)
                {
                    TryLineChange(team, playerInfo);
                }
            }
            else
            {
                // Regen stamina on bench
                playerInfo.stamina += staminaRegenRate * Time.deltaTime;
                playerInfo.stamina = Mathf.Min(100f, playerInfo.stamina);
            }
        }
    }

    /// <summary>
    /// Attempt to substitute a player.
    /// </summary>
    private void TryLineChange(Team team, TeamPlayerInfo tiredPlayer)
    {
        // Find rested bench player at same position
        TeamPlayerInfo benchPlayer = team.BenchPlayers
            .FirstOrDefault(p => p.position == tiredPlayer.position && p.stamina > 50f);

        if (benchPlayer != null)
        {
            PerformLineChange(team, tiredPlayer, benchPlayer);
        }
    }

    /// <summary>
    /// Execute line change between two players.
    /// </summary>
    public void PerformLineChange(Team team, TeamPlayerInfo playerOut, TeamPlayerInfo playerIn)
    {
        if (playerOut == null || playerIn == null) return;

        // Swap on-ice status
        playerOut.isOnIce = false;
        playerIn.isOnIce = true;

        // Move playerOut to bench (disable)
        if (playerOut.player != null)
        {
            playerOut.player.gameObject.SetActive(false);
        }

        // Move playerIn to ice (enable at home position)
        if (playerIn.player != null)
        {
            playerIn.player.gameObject.SetActive(true);
            playerIn.player.transform.position = playerIn.homePosition;
        }

        OnLineChange?.Invoke(playerOut, playerIn);
        Debug.Log($"[TeamManager] Line change: {playerOut.position} out, fresh player in");
    }

    #endregion

    #region Event Handlers

    private void OnGoalScoredHandler(int teamIndex)
    {
        RecordGoal(teamIndex);
    }

    private void OnPuckPossessionChanged(GameObject newOwner)
    {
        // Auto-switch to player with puck if enabled
        if (autoSwitchDistance > 0f && newOwner != null)
        {
            HockeyPlayer player = newOwner.GetComponent<HockeyPlayer>();
            if (player != null && homeTeam.HasPlayer(player))
            {
                SwitchToPlayer(player);
            }
        }
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        if (homeTeam != null)
        {
            DrawTeamGizmos(homeTeam, Color.blue);
        }

        if (awayTeam != null)
        {
            DrawTeamGizmos(awayTeam, Color.red);
        }
    }

    private void DrawTeamGizmos(Team team, Color color)
    {
        Gizmos.color = color;

        foreach (var playerInfo in team.Players)
        {
            if (playerInfo.player == null) continue;

            // Draw line from player to home position
            Gizmos.color = color * 0.5f;
            Gizmos.DrawLine(playerInfo.player.transform.position, playerInfo.homePosition);

            // Draw home position marker
            Gizmos.color = color;
            Gizmos.DrawWireSphere(playerInfo.homePosition, 0.5f);

            // Draw stamina bar above player
            if (playerInfo.isOnIce)
            {
                Vector3 barPos = playerInfo.player.transform.position + Vector3.up * 2.5f;
                float staminaPercent = playerInfo.stamina / 100f;

                Gizmos.color = Color.Lerp(Color.red, Color.green, staminaPercent);
                Gizmos.DrawCube(barPos, new Vector3(staminaPercent * 2f, 0.1f, 0.1f));
            }
        }

        // Draw controlled player indicator
        if (controlledPlayer != null && team.HasPlayer(controlledPlayer))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(controlledPlayer.transform.position + Vector3.up * 3f, 0.4f);
        }
    }

    private void OnGUI()
    {
        if (!showTeamStats) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 12;

        // Home team stats
        string homeStats = $"{homeTeam.Name}: {homeTeam.Stats.goals}G {homeTeam.Stats.shots}S {homeTeam.Stats.hits}H";
        GUI.Box(new Rect(10, 10, 300, 30), homeStats, style);

        // Away team stats
        string awayStats = $"{awayTeam.Name}: {awayTeam.Stats.goals}G {awayTeam.Stats.shots}S {awayTeam.Stats.hits}H";
        GUI.Box(new Rect(10, 50, 300, 30), awayStats, style);

        // Controlled player info
        if (controlledPlayer != null)
        {
            TeamPlayerInfo info = homeTeam.GetPlayerInfo(controlledPlayer);
            if (info != null)
            {
                string playerInfo = $"Controlling: {info.position} | Stamina: {info.stamina:F0}%";
                GUI.Box(new Rect(10, 90, 300, 30), playerInfo, style);
            }
        }
    }

    #endregion
}

#region Team Data Structures

/// <summary>
/// Represents a complete hockey team with all players and stats.
/// </summary>
[System.Serializable]
public class Team
{
    public TeamData Data { get; private set; }
    public string Name => Data != null ? Data.teamName : "Unknown";
    public int TeamIndex { get; private set; }
    public bool IsHome { get; private set; }
    public bool IsPlayerControlled { get; set; }
    public FormationType CurrentFormation { get; set; }
    public TeamStats Stats { get; private set; }

    private List<TeamPlayerInfo> players = new List<TeamPlayerInfo>();
    private List<TeamPlayerInfo> benchPlayers = new List<TeamPlayerInfo>();

    public List<TeamPlayerInfo> Players => players;
    public List<TeamPlayerInfo> BenchPlayers => benchPlayers;

    public Team(TeamData data, int teamIndex, bool isHome)
    {
        Data = data;
        TeamIndex = teamIndex;
        IsHome = isHome;
        IsPlayerControlled = isHome; // By default, home team is player-controlled
        CurrentFormation = data != null ? data.defaultFormation : FormationType.Balanced;
        Stats = new TeamStats();
    }

    public void AddPlayer(TeamPlayerInfo playerInfo)
    {
        players.Add(playerInfo);
    }

    public TeamPlayerInfo GetPlayerInfo(HockeyPlayer player)
    {
        return players.FirstOrDefault(p => p.player == player);
    }

    public bool HasPlayer(HockeyPlayer player)
    {
        return players.Any(p => p.player == player);
    }

    public TeamPlayerInfo GetNearestPlayerToPuck(Vector3 puckPosition)
    {
        TeamPlayerInfo nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var playerInfo in players)
        {
            if (!playerInfo.isOnIce || playerInfo.player == null)
                continue;

            float distance = Vector3.Distance(playerInfo.player.transform.position, puckPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = playerInfo;
            }
        }

        return nearest;
    }
}

/// <summary>
/// Information about a single player on a team.
/// </summary>
[System.Serializable]
public class TeamPlayerInfo
{
    public HockeyPlayer player;
    public AIController aiController;
    public PlayerPosition position;
    public Vector3 homePosition;
    public bool isOnIce;
    public float stamina;

    // Individual stats
    public int goals;
    public int assists;
    public int shots;
    public int hits;
    public int saves; // For goalies

    public void ResetStats()
    {
        goals = 0;
        assists = 0;
        shots = 0;
        hits = 0;
        saves = 0;
    }
}

/// <summary>
/// Team-wide statistics.
/// </summary>
[System.Serializable]
public class TeamStats
{
    public int goals;
    public int shots;
    public int hits;
    public int saves;
    public int penalties;
    public int faceoffsWon;
    public float possessionTime;
    public float powerPlayTime;

    public void Reset()
    {
        goals = 0;
        shots = 0;
        hits = 0;
        saves = 0;
        penalties = 0;
        faceoffsWon = 0;
        possessionTime = 0f;
        powerPlayTime = 0f;
    }

    public float ShotPercentage => shots > 0 ? (float)goals / shots * 100f : 0f;
}

#endregion
