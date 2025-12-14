using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HockeyGame.Core
{
    /// <summary>
    /// Types of penalties in hockey
    /// </summary>
    public enum PenaltyType
    {
        Tripping,
        Hooking,
        HighSticking,
        Roughing,
        Interference,
        Boarding
    }

    /// <summary>
    /// Severity of the penalty
    /// </summary>
    public enum PenaltySeverity
    {
        Minor,  // 2 minutes
        Major   // 5 minutes
    }

    /// <summary>
    /// Represents a single penalty instance
    /// </summary>
    [System.Serializable]
    public class Penalty
    {
        public string playerName;
        public int playerNumber;
        public GameObject playerObject;
        public PenaltyType type;
        public PenaltySeverity severity;
        public float duration;
        public float timeRemaining;
        public int teamId;
        public bool canEndOnGoal; // Minor penalties end on power play goals
        public Vector3 penaltyBoxPosition;

        public Penalty(GameObject player, string name, int number, PenaltyType penaltyType, PenaltySeverity penaltySeverity, int team)
        {
            playerObject = player;
            playerName = name;
            playerNumber = number;
            type = penaltyType;
            severity = penaltySeverity;
            teamId = team;

            // Set duration based on severity
            duration = penaltySeverity == PenaltySeverity.Minor ? 120f : 300f; // 2 or 5 minutes in seconds
            timeRemaining = duration;

            // Minor penalties end on power play goals, major penalties do not
            canEndOnGoal = penaltySeverity == PenaltySeverity.Minor;
        }

        public bool IsExpired()
        {
            return timeRemaining <= 0f;
        }

        public void UpdateTime(float deltaTime)
        {
            timeRemaining = Mathf.Max(0f, timeRemaining - deltaTime);
        }

        public string GetDisplayTime()
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            return $"{minutes}:{seconds:D2}";
        }
    }

    /// <summary>
    /// Power play situation data
    /// </summary>
    public class PowerPlayInfo
    {
        public bool isActive;
        public int teamOnPowerPlay;
        public int teamShorthanded;
        public int powerPlayStrength;  // e.g., 5v4, 5v3, 4v3
        public int shorthandedStrength;
        public string situation; // "5v4", "5v3", "4v3", etc.

        public PowerPlayInfo(bool active, int ppTeam, int shTeam, int ppStrength, int shStrength)
        {
            isActive = active;
            teamOnPowerPlay = ppTeam;
            teamShorthanded = shTeam;
            powerPlayStrength = ppStrength;
            shorthandedStrength = shStrength;
            situation = $"{ppStrength}v{shStrength}";
        }
    }

    /// <summary>
    /// Manages all penalties and power play situations in the game
    /// </summary>
    public class PenaltySystem : MonoBehaviour
    {
        [Header("Penalty Box Configuration")]
        [SerializeField] private Vector3[] team1PenaltyBoxPositions = new Vector3[]
        {
            new Vector3(-15f, 0f, 5f),
            new Vector3(-15f, 0f, 3f),
            new Vector3(-15f, 0f, 1f)
        };

        [SerializeField] private Vector3[] team2PenaltyBoxPositions = new Vector3[]
        {
            new Vector3(15f, 0f, 5f),
            new Vector3(15f, 0f, 3f),
            new Vector3(15f, 0f, 1f)
        };

        [Header("Player Strength Settings")]
        [SerializeField] private int normalStrength = 5; // 5v5

        [Header("Collision Detection Thresholds")]
        [SerializeField] private float roughingSpeedThreshold = 10f; // High speed hit
        [SerializeField] private float boardingDistanceToWall = 2f; // Distance from boards for boarding
        [SerializeField] private float highStickingHeight = 1.5f; // Height for high sticking

        // Active penalties for each team
        private List<Penalty> team1Penalties = new List<Penalty>();
        private List<Penalty> team2Penalties = new List<Penalty>();

        // Current power play situation
        private PowerPlayInfo currentPowerPlay;
        private PowerPlayInfo previousPowerPlay;

        // Events
        public event Action<Penalty> OnPenaltyCalled;
        public event Action<Penalty> OnPenaltyExpired;
        public event Action<PowerPlayInfo> OnPowerPlayStart;
        public event Action<PowerPlayInfo> OnPowerPlayEnd;
        public event Action<Penalty, bool> OnPenaltyServing; // Penalty, isEntering

        // Singleton instance
        private static PenaltySystem instance;
        public static PenaltySystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<PenaltySystem>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdatePenalties(Time.deltaTime);
        }

        /// <summary>
        /// Call a penalty on a player
        /// </summary>
        public void CallPenalty(GameObject player, string playerName, int playerNumber, PenaltyType type, PenaltySeverity severity, int teamId)
        {
            if (player == null)
            {
                Debug.LogError("Cannot call penalty on null player");
                return;
            }

            // Create penalty
            Penalty penalty = new Penalty(player, playerName, playerNumber, type, severity, teamId);

            // Assign penalty box position
            Vector3 boxPosition = GetNextPenaltyBoxPosition(teamId);
            penalty.penaltyBoxPosition = boxPosition;

            // Add to appropriate team's penalty list
            if (teamId == 1)
            {
                team1Penalties.Add(penalty);
            }
            else if (teamId == 2)
            {
                team2Penalties.Add(penalty);
            }

            // Send player to penalty box
            ServePenalty(penalty, true);

            // Invoke penalty called event
            OnPenaltyCalled?.Invoke(penalty);

            Debug.Log($"Penalty called: {playerName} #{playerNumber} - {type} ({severity}) - {penalty.GetDisplayTime()}");

            // Check for power play changes
            UpdatePowerPlayStatus();
        }

        /// <summary>
        /// Convenient overload for calling minor penalties
        /// </summary>
        public void CallPenalty(GameObject player, string playerName, int playerNumber, PenaltyType type, int teamId)
        {
            CallPenalty(player, playerName, playerNumber, type, PenaltySeverity.Minor, teamId);
        }

        /// <summary>
        /// Send a player to serve their penalty
        /// </summary>
        public void ServePenalty(Penalty penalty, bool isEntering)
        {
            if (penalty == null || penalty.playerObject == null)
            {
                return;
            }

            if (isEntering)
            {
                // Move player to penalty box
                penalty.playerObject.transform.position = penalty.penaltyBoxPosition;

                // Disable player controls (you would implement this based on your player controller)
                DisablePlayerControls(penalty.playerObject);
            }
            else
            {
                // Return player to ice (you would implement proper positioning)
                EnablePlayerControls(penalty.playerObject);
            }

            OnPenaltyServing?.Invoke(penalty, isEntering);
        }

        /// <summary>
        /// Cancel penalties when a power play goal is scored
        /// </summary>
        public void CancelPenaltyOnGoal(int scoringTeam)
        {
            // Determine which team was shorthanded
            int shorthandedTeam = scoringTeam == 1 ? 2 : 1;

            List<Penalty> penalties = shorthandedTeam == 1 ? team1Penalties : team2Penalties;

            // Find the first minor penalty that can end on a goal
            Penalty penaltyToCancel = penalties.FirstOrDefault(p => p.canEndOnGoal);

            if (penaltyToCancel != null)
            {
                Debug.Log($"Power play goal! Cancelling {penaltyToCancel.playerName}'s {penaltyToCancel.type} penalty");
                RemovePenalty(penaltyToCancel, shorthandedTeam);
            }
        }

        /// <summary>
        /// Update all active penalties
        /// </summary>
        private void UpdatePenalties(float deltaTime)
        {
            // Update team 1 penalties
            for (int i = team1Penalties.Count - 1; i >= 0; i--)
            {
                team1Penalties[i].UpdateTime(deltaTime);

                if (team1Penalties[i].IsExpired())
                {
                    RemovePenalty(team1Penalties[i], 1);
                }
            }

            // Update team 2 penalties
            for (int i = team2Penalties.Count - 1; i >= 0; i--)
            {
                team2Penalties[i].UpdateTime(deltaTime);

                if (team2Penalties[i].IsExpired())
                {
                    RemovePenalty(team2Penalties[i], 2);
                }
            }
        }

        /// <summary>
        /// Remove a penalty and return player to ice
        /// </summary>
        private void RemovePenalty(Penalty penalty, int teamId)
        {
            // Return player to ice
            ServePenalty(penalty, false);

            // Remove from list
            if (teamId == 1)
            {
                team1Penalties.Remove(penalty);
            }
            else if (teamId == 2)
            {
                team2Penalties.Remove(penalty);
            }

            // Invoke penalty expired event
            OnPenaltyExpired?.Invoke(penalty);

            Debug.Log($"Penalty expired: {penalty.playerName} - {penalty.type}");

            // Check for power play changes
            UpdatePowerPlayStatus();
        }

        /// <summary>
        /// Update power play status based on active penalties
        /// </summary>
        private void UpdatePowerPlayStatus()
        {
            int team1ActiveCount = team1Penalties.Count;
            int team2ActiveCount = team2Penalties.Count;

            int team1Strength = normalStrength - team1ActiveCount;
            int team2Strength = normalStrength - team2ActiveCount;

            previousPowerPlay = currentPowerPlay;

            // Determine power play situation
            if (team1Strength != team2Strength)
            {
                int ppTeam = team1Strength > team2Strength ? 1 : 2;
                int shTeam = ppTeam == 1 ? 2 : 1;
                int ppStrength = Mathf.Max(team1Strength, team2Strength);
                int shStrength = Mathf.Min(team1Strength, team2Strength);

                currentPowerPlay = new PowerPlayInfo(true, ppTeam, shTeam, ppStrength, shStrength);

                // Check if power play just started
                if (previousPowerPlay == null || !previousPowerPlay.isActive)
                {
                    OnPowerPlayStart?.Invoke(currentPowerPlay);
                    Debug.Log($"Power play started: {currentPowerPlay.situation} (Team {ppTeam} advantage)");
                }
            }
            else
            {
                // Equal strength - check if power play just ended
                if (previousPowerPlay != null && previousPowerPlay.isActive)
                {
                    OnPowerPlayEnd?.Invoke(previousPowerPlay);
                    Debug.Log($"Power play ended - Back to {team1Strength}v{team2Strength}");
                }

                currentPowerPlay = new PowerPlayInfo(false, 0, 0, team1Strength, team2Strength);
            }
        }

        /// <summary>
        /// Get the next available penalty box position for a team
        /// </summary>
        private Vector3 GetNextPenaltyBoxPosition(int teamId)
        {
            Vector3[] positions = teamId == 1 ? team1PenaltyBoxPositions : team2PenaltyBoxPositions;
            List<Penalty> penalties = teamId == 1 ? team1Penalties : team2Penalties;

            int index = Mathf.Min(penalties.Count, positions.Length - 1);
            return positions[index];
        }

        /// <summary>
        /// Disable player controls (integrate with your player controller)
        /// </summary>
        private void DisablePlayerControls(GameObject player)
        {
            // Example implementation - adjust based on your player controller
            var playerController = player.GetComponent<MonoBehaviour>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // Disable physics if needed
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        /// <summary>
        /// Enable player controls (integrate with your player controller)
        /// </summary>
        private void EnablePlayerControls(GameObject player)
        {
            // Example implementation - adjust based on your player controller
            var playerController = player.GetComponent<MonoBehaviour>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // Enable physics if needed
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }

        #region Collision Detection Integration Hooks

        /// <summary>
        /// Check if a collision should result in a roughing penalty
        /// </summary>
        public bool CheckForRoughing(Collision collision, out GameObject offendingPlayer)
        {
            offendingPlayer = null;

            // Check collision speed
            float collisionSpeed = collision.relativeVelocity.magnitude;
            if (collisionSpeed >= roughingSpeedThreshold)
            {
                offendingPlayer = collision.gameObject;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a collision should result in a boarding penalty
        /// </summary>
        public bool CheckForBoarding(Collision collision, Vector3 wallPosition, out GameObject offendingPlayer)
        {
            offendingPlayer = null;

            // Check if hit occurred near the boards
            float distanceToWall = Vector3.Distance(collision.contacts[0].point, wallPosition);
            if (distanceToWall <= boardingDistanceToWall)
            {
                float collisionSpeed = collision.relativeVelocity.magnitude;
                if (collisionSpeed >= roughingSpeedThreshold * 0.75f) // Slightly lower threshold for boarding
                {
                    offendingPlayer = collision.gameObject;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a stick contact is high sticking
        /// </summary>
        public bool CheckForHighSticking(GameObject stickObject, Vector3 contactPoint, out GameObject offendingPlayer)
        {
            offendingPlayer = null;

            // Check contact height
            if (contactPoint.y >= highStickingHeight)
            {
                offendingPlayer = stickObject.transform.root.gameObject; // Get player from stick
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check for interference (hitting player without puck)
        /// </summary>
        public bool CheckForInterference(GameObject hittingPlayer, GameObject hitPlayer, GameObject playerWithPuck, out GameObject offendingPlayer)
        {
            offendingPlayer = null;

            // If hit player doesn't have the puck, it's interference
            if (hitPlayer != playerWithPuck)
            {
                offendingPlayer = hittingPlayer;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Auto-detect penalty type from collision data
        /// </summary>
        public void EvaluateCollisionForPenalty(Collision collision, GameObject playerWithPuck, Vector3 nearestWallPosition)
        {
            GameObject offendingPlayer;

            // Check for boarding first (most specific)
            if (CheckForBoarding(collision, nearestWallPosition, out offendingPlayer))
            {
                int teamId = GetPlayerTeam(offendingPlayer);
                string playerName = GetPlayerName(offendingPlayer);
                int playerNumber = GetPlayerNumber(offendingPlayer);

                CallPenalty(offendingPlayer, playerName, playerNumber, PenaltyType.Boarding, PenaltySeverity.Major, teamId);
                return;
            }

            // Check for roughing (high speed hit)
            if (CheckForRoughing(collision, out offendingPlayer))
            {
                int teamId = GetPlayerTeam(offendingPlayer);
                string playerName = GetPlayerName(offendingPlayer);
                int playerNumber = GetPlayerNumber(offendingPlayer);

                CallPenalty(offendingPlayer, playerName, playerNumber, PenaltyType.Roughing, teamId);
                return;
            }

            // Check for interference
            GameObject hitPlayer = collision.gameObject == offendingPlayer ? collision.contacts[0].otherCollider.gameObject : collision.gameObject;
            if (CheckForInterference(collision.gameObject, hitPlayer, playerWithPuck, out offendingPlayer))
            {
                int teamId = GetPlayerTeam(offendingPlayer);
                string playerName = GetPlayerName(offendingPlayer);
                int playerNumber = GetPlayerNumber(offendingPlayer);

                CallPenalty(offendingPlayer, playerName, playerNumber, PenaltyType.Interference, teamId);
                return;
            }
        }

        #endregion

        #region Player Information Helpers

        // These helper methods should be implemented based on your player data structure
        private int GetPlayerTeam(GameObject player)
        {
            // Implement based on your player component structure
            // Example: return player.GetComponent<PlayerData>().teamId;
            return 1; // Placeholder
        }

        private string GetPlayerName(GameObject player)
        {
            // Implement based on your player component structure
            return player.name;
        }

        private int GetPlayerNumber(GameObject player)
        {
            // Implement based on your player component structure
            return 0; // Placeholder
        }

        #endregion

        #region Public Getters

        public List<Penalty> GetTeam1Penalties() => team1Penalties;
        public List<Penalty> GetTeam2Penalties() => team2Penalties;
        public PowerPlayInfo GetCurrentPowerPlay() => currentPowerPlay;
        public bool IsTeamOnPowerPlay(int teamId) => currentPowerPlay?.isActive == true && currentPowerPlay.teamOnPowerPlay == teamId;
        public bool IsTeamShorthanded(int teamId) => currentPowerPlay?.isActive == true && currentPowerPlay.teamShorthanded == teamId;
        public int GetTeamStrength(int teamId)
        {
            int penaltyCount = teamId == 1 ? team1Penalties.Count : team2Penalties.Count;
            return normalStrength - penaltyCount;
        }

        #endregion

        #region Debug and Testing

        [ContextMenu("Test Minor Penalty Team 1")]
        private void TestMinorPenaltyTeam1()
        {
            GameObject testPlayer = new GameObject("Test Player 1");
            CallPenalty(testPlayer, "Test Player", 99, PenaltyType.Tripping, PenaltySeverity.Minor, 1);
        }

        [ContextMenu("Test Major Penalty Team 2")]
        private void TestMajorPenaltyTeam2()
        {
            GameObject testPlayer = new GameObject("Test Player 2");
            CallPenalty(testPlayer, "Test Player", 88, PenaltyType.Roughing, PenaltySeverity.Major, 2);
        }

        [ContextMenu("Clear All Penalties")]
        private void ClearAllPenalties()
        {
            team1Penalties.Clear();
            team2Penalties.Clear();
            UpdatePowerPlayStatus();
        }

        #endregion
    }
}
