using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages hockey rules including offsides, icing, and zone tracking.
/// </summary>
public class HockeyRules : MonoBehaviour
{
    [Header("Zone Configuration")]
    [SerializeField] private RinkBuilder rinkBuilder;
    [SerializeField, Range(0.2f, 0.4f)] private float blueLinePosition = 0.333f; // Fraction of rink length (1/3)

    [Header("Puck Tracking")]
    [SerializeField] private Transform puckTransform;

    [Header("Rule Settings")]
    [SerializeField] private bool enableOffsides = true;
    [SerializeField] private bool enableIcing = true;
    [SerializeField] private float icingDistanceThreshold = 0.1f; // Distance from goal line to trigger icing

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Zone tracking
    public enum Zone
    {
        HomeDefensiveZone,   // Team 0 defends here (left side, -X)
        NeutralZone,
        AwayDefensiveZone    // Team 1 defends here (right side, +X)
    }

    // Events
    public event Action<Vector3> OnOffsides;  // Faceoff position
    public event Action<Vector3, int> OnIcing; // Faceoff position, offending team
    public event Action<Zone, Zone> OnZoneChange; // Old zone, new zone

    // Zone boundaries (in world X coordinates)
    private float homeBlueLineX;     // Left blue line
    private float awayBlueLineX;     // Right blue line
    private float centerLineX;       // Center red line

    // State tracking
    private Zone currentPuckZone;
    private Dictionary<int, Zone> playerZones = new Dictionary<int, Zone>();
    private Dictionary<int, List<Transform>> teamPlayers = new Dictionary<int, List<Transform>>();

    // Icing tracking
    private bool puckCrossedCenterLine = false;
    private int lastTeamToTouch = -1;
    private Vector3 lastPuckTouchPosition;
    private bool icingInProgress = false;

    // Offsides tracking
    private Dictionary<int, bool> teamInOffensiveZone = new Dictionary<int, bool>();

    // Properties
    public Zone CurrentPuckZone => currentPuckZone;
    public float HomeBlueLineX => homeBlueLineX;
    public float AwayBlueLineX => awayBlueLineX;
    public float CenterLineX => centerLineX;

    private void Start()
    {
        InitializeZones();

        if (puckTransform != null)
        {
            currentPuckZone = GetZoneForPosition(puckTransform.position);
        }

        teamInOffensiveZone[0] = false;
        teamInOffensiveZone[1] = false;
    }

    private void InitializeZones()
    {
        if (rinkBuilder == null)
        {
            rinkBuilder = FindObjectOfType<RinkBuilder>();
            if (rinkBuilder == null)
            {
                Debug.LogError("[HockeyRules] RinkBuilder not found!");
                return;
            }
        }

        float rinkLength = rinkBuilder.Length;
        float halfLength = rinkLength / 2f;

        // Calculate zone boundaries
        // Home zone: from -halfLength to homeBlueLineX
        // Neutral zone: from homeBlueLineX to awayBlueLineX
        // Away zone: from awayBlueLineX to +halfLength
        homeBlueLineX = -halfLength + (rinkLength * blueLinePosition);
        awayBlueLineX = halfLength - (rinkLength * blueLinePosition);
        centerLineX = 0f;

        Debug.Log($"[HockeyRules] Zone boundaries initialized:");
        Debug.Log($"  Home Blue Line: {homeBlueLineX:F2}");
        Debug.Log($"  Center Line: {centerLineX:F2}");
        Debug.Log($"  Away Blue Line: {awayBlueLineX:F2}");
    }

    private void Update()
    {
        if (puckTransform == null) return;

        UpdatePuckZone();
        CheckIcing();
    }

    /// <summary>
    /// Register a player for tracking.
    /// </summary>
    public void RegisterPlayer(Transform player, int teamIndex)
    {
        if (!teamPlayers.ContainsKey(teamIndex))
        {
            teamPlayers[teamIndex] = new List<Transform>();
        }

        if (!teamPlayers[teamIndex].Contains(player))
        {
            teamPlayers[teamIndex].Add(player);
            playerZones[player.GetInstanceID()] = GetZoneForPosition(player.position);
        }
    }

    /// <summary>
    /// Unregister a player from tracking.
    /// </summary>
    public void UnregisterPlayer(Transform player, int teamIndex)
    {
        if (teamPlayers.ContainsKey(teamIndex))
        {
            teamPlayers[teamIndex].Remove(player);
            playerZones.Remove(player.GetInstanceID());
        }
    }

    /// <summary>
    /// Update player zone and check for offsides.
    /// Call this when a player moves significantly.
    /// </summary>
    public void UpdatePlayerZone(Transform player, int teamIndex)
    {
        Zone newZone = GetZoneForPosition(player.position);
        int playerId = player.GetInstanceID();

        if (!playerZones.ContainsKey(playerId))
        {
            playerZones[playerId] = newZone;
            return;
        }

        Zone oldZone = playerZones[playerId];
        if (oldZone != newZone)
        {
            playerZones[playerId] = newZone;
            CheckOffsides(player, teamIndex, newZone);
        }
    }

    /// <summary>
    /// Notify the rules system that a team touched the puck.
    /// </summary>
    public void OnPuckTouched(int teamIndex, Vector3 touchPosition)
    {
        lastTeamToTouch = teamIndex;
        lastPuckTouchPosition = touchPosition;

        // Reset icing if puck is touched
        if (icingInProgress)
        {
            icingInProgress = false;
            Debug.Log("[HockeyRules] Icing negated - puck was touched");
        }

        // Check if puck crossed center line
        Zone touchZone = GetZoneForPosition(touchPosition);
        if (teamIndex == 0 && touchZone == Zone.HomeDefensiveZone)
        {
            // Team 0 touched in their defensive zone, start tracking for icing
            puckCrossedCenterLine = false;
        }
        else if (teamIndex == 1 && touchZone == Zone.AwayDefensiveZone)
        {
            // Team 1 touched in their defensive zone, start tracking for icing
            puckCrossedCenterLine = false;
        }
    }

    private void UpdatePuckZone()
    {
        Zone newZone = GetZoneForPosition(puckTransform.position);

        if (newZone != currentPuckZone)
        {
            Zone oldZone = currentPuckZone;
            currentPuckZone = newZone;
            OnZoneChange?.Invoke(oldZone, newZone);

            // Track center line crossing for icing
            if (oldZone != Zone.NeutralZone && newZone == Zone.NeutralZone)
            {
                puckCrossedCenterLine = true;
            }
        }
    }

    private void CheckOffsides(Transform player, int teamIndex, Zone playerZone)
    {
        if (!enableOffsides) return;

        // Determine if player entered offensive zone
        bool enteredOffensiveZone = false;

        if (teamIndex == 0 && playerZone == Zone.AwayDefensiveZone)
        {
            // Team 0 player entered away zone (their offensive zone)
            enteredOffensiveZone = true;
        }
        else if (teamIndex == 1 && playerZone == Zone.HomeDefensiveZone)
        {
            // Team 1 player entered home zone (their offensive zone)
            enteredOffensiveZone = true;
        }

        if (enteredOffensiveZone)
        {
            // Check if puck is also in that zone
            bool puckInZone = (teamIndex == 0 && currentPuckZone == Zone.AwayDefensiveZone) ||
                              (teamIndex == 1 && currentPuckZone == Zone.HomeDefensiveZone);

            if (!puckInZone)
            {
                // OFFSIDES! Player entered before puck
                CallOffsides(teamIndex);
            }
        }
    }

    private void CheckIcing()
    {
        if (!enableIcing || icingInProgress || lastTeamToTouch < 0) return;

        float puckX = puckTransform.position.x;

        // Check if puck crossed goal line
        // Team 0 icing: shot from home zone, crosses away goal line
        if (lastTeamToTouch == 0)
        {
            Zone touchZone = GetZoneForPosition(lastPuckTouchPosition);
            if (touchZone == Zone.HomeDefensiveZone && puckX > awayBlueLineX)
            {
                // Check if puck is near the away goal line
                float awayGoalLineX = rinkBuilder.Length / 2f;
                if (puckX >= awayGoalLineX - icingDistanceThreshold)
                {
                    // TODO: Check for penalty kill exception
                    CallIcing(0);
                }
            }
        }
        // Team 1 icing: shot from away zone, crosses home goal line
        else if (lastTeamToTouch == 1)
        {
            Zone touchZone = GetZoneForPosition(lastPuckTouchPosition);
            if (touchZone == Zone.AwayDefensiveZone && puckX < homeBlueLineX)
            {
                // Check if puck is near the home goal line
                float homeGoalLineX = -rinkBuilder.Length / 2f;
                if (puckX <= homeGoalLineX + icingDistanceThreshold)
                {
                    // TODO: Check for penalty kill exception
                    CallIcing(1);
                }
            }
        }
    }

    private void CallOffsides(int offendingTeam)
    {
        if (teamInOffensiveZone.ContainsKey(offendingTeam) && teamInOffensiveZone[offendingTeam])
        {
            // Already called offsides for this zone entry
            return;
        }

        teamInOffensiveZone[offendingTeam] = true;

        Debug.Log($"[HockeyRules] OFFSIDES! Team {offendingTeam}");

        // Faceoff at neutral zone dot
        Vector3 faceoffPosition = GetNeutralZoneFaceoffPosition(offendingTeam);
        OnOffsides?.Invoke(faceoffPosition);
    }

    private void CallIcing(int offendingTeam)
    {
        icingInProgress = true;

        Debug.Log($"[HockeyRules] ICING! Team {offendingTeam}");

        // Faceoff in offending team's defensive zone
        Vector3 faceoffPosition = GetDefensiveZoneFaceoffPosition(offendingTeam);
        OnIcing?.Invoke(faceoffPosition, offendingTeam);
    }

    /// <summary>
    /// Reset offsides tracking (call after faceoff or zone clear).
    /// </summary>
    public void ResetOffsides()
    {
        teamInOffensiveZone[0] = false;
        teamInOffensiveZone[1] = false;
    }

    /// <summary>
    /// Reset icing tracking (call after faceoff).
    /// </summary>
    public void ResetIcing()
    {
        icingInProgress = false;
        puckCrossedCenterLine = false;
        lastTeamToTouch = -1;
    }

    /// <summary>
    /// Get the zone for a given world position.
    /// </summary>
    public Zone GetZoneForPosition(Vector3 position)
    {
        float x = position.x;

        if (x < homeBlueLineX)
        {
            return Zone.HomeDefensiveZone;
        }
        else if (x > awayBlueLineX)
        {
            return Zone.AwayDefensiveZone;
        }
        else
        {
            return Zone.NeutralZone;
        }
    }

    /// <summary>
    /// Get the offensive zone for a team.
    /// </summary>
    public Zone GetOffensiveZone(int teamIndex)
    {
        return teamIndex == 0 ? Zone.AwayDefensiveZone : Zone.HomeDefensiveZone;
    }

    /// <summary>
    /// Get the defensive zone for a team.
    /// </summary>
    public Zone GetDefensiveZone(int teamIndex)
    {
        return teamIndex == 0 ? Zone.HomeDefensiveZone : Zone.AwayDefensiveZone;
    }

    /// <summary>
    /// Check if a player is in their offensive zone.
    /// </summary>
    public bool IsPlayerInOffensiveZone(Transform player, int teamIndex)
    {
        int playerId = player.GetInstanceID();
        if (!playerZones.ContainsKey(playerId)) return false;

        Zone playerZone = playerZones[playerId];
        Zone offensiveZone = GetOffensiveZone(teamIndex);

        return playerZone == offensiveZone;
    }

    private Vector3 GetNeutralZoneFaceoffPosition(int offendingTeam)
    {
        // Faceoff at neutral zone dot (closer to offending team's zone)
        float dotX = offendingTeam == 0 ? homeBlueLineX + 5f : awayBlueLineX - 5f;
        return new Vector3(dotX, 0.1f, 0);
    }

    private Vector3 GetDefensiveZoneFaceoffPosition(int offendingTeam)
    {
        // Faceoff in offending team's defensive zone
        float rinkLength = rinkBuilder.Length;
        float dotX = offendingTeam == 0 ? -rinkLength / 2f + 10f : rinkLength / 2f - 10f;
        float dotZ = 7f; // Standard faceoff dot offset

        return new Vector3(dotX, 0.1f, dotZ);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || rinkBuilder == null) return;

        // Draw blue lines
        Gizmos.color = Color.blue;
        float rinkWidth = rinkBuilder.Width;

        Vector3 homeBlueStart = new Vector3(homeBlueLineX, 0, -rinkWidth / 2f);
        Vector3 homeBlueEnd = new Vector3(homeBlueLineX, 0, rinkWidth / 2f);
        Gizmos.DrawLine(homeBlueStart, homeBlueEnd);

        Vector3 awayBlueStart = new Vector3(awayBlueLineX, 0, -rinkWidth / 2f);
        Vector3 awayBlueEnd = new Vector3(awayBlueLineX, 0, rinkWidth / 2f);
        Gizmos.DrawLine(awayBlueStart, awayBlueEnd);

        // Draw center line
        Gizmos.color = Color.red;
        Vector3 centerStart = new Vector3(centerLineX, 0, -rinkWidth / 2f);
        Vector3 centerEnd = new Vector3(centerLineX, 0, rinkWidth / 2f);
        Gizmos.DrawLine(centerStart, centerEnd);

        // Draw zone labels
        if (puckTransform != null)
        {
            Zone puckZone = GetZoneForPosition(puckTransform.position);
            Gizmos.color = Color.yellow;

            Vector3 labelPos = puckTransform.position + Vector3.up * 2f;
            // Note: Gizmos don't support text, but you can see the zone in inspector
        }
    }
}
