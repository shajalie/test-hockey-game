using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Practice mode system for skill development and training.
/// Includes free skate, shooting practice, passing drills, goalie practice, and skating drills.
/// Tracks performance metrics and provides feedback without the pressure of a real match.
/// </summary>
public class PracticeMode : MonoBehaviour
{
    public enum PracticeType
    {
        FreeSkate,
        ShootingPractice,
        PassingDrill,
        GoaliePractice,
        SkatingDrill
    }

    [System.Serializable]
    public class DrillStats
    {
        public int totalShots;
        public int shotsOnTarget;
        public int targetsHit;
        public int totalPasses;
        public int successfulPasses;
        public int totalSaves;
        public int totalShots_Goalie;
        public float bestLapTime;
        public float currentLapTime;
        public int conesHit;

        public float ShotAccuracy => totalShots > 0 ? (float)shotsOnTarget / totalShots * 100f : 0f;
        public float PassAccuracy => totalPasses > 0 ? (float)successfulPasses / totalPasses * 100f : 0f;
        public float SavePercentage => totalShots_Goalie > 0 ? (float)totalSaves / totalShots_Goalie * 100f : 0f;
    }

    [Header("Practice Configuration")]
    [SerializeField] private PracticeType currentPracticeType = PracticeType.FreeSkate;
    [SerializeField] private bool practiceActive = false;

    [Header("Player References")]
    [SerializeField] private HockeyPlayer playerController;
    [SerializeField] private GoalieController goalieController;
    [SerializeField] private Puck puck;
    [SerializeField] private Transform puckSpawnPoint;

    [Header("Goal References")]
    [SerializeField] private Goal practiceGoal;
    [SerializeField] private Transform goalTransform;

    [Header("Shooting Practice Settings")]
    [SerializeField] private bool goalieEnabled = false;
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private int numberOfTargets = 5;
    [SerializeField] private float targetSize = 0.5f;
    [SerializeField] private float targetPoints = 10f;
    [SerializeField] private float puckRespawnDelay = 1.5f;

    [Header("Passing Drill Settings")]
    [SerializeField] private GameObject aiTeammatePrefab;
    [SerializeField] private int numberOfPassPartners = 2;
    [SerializeField] private float passSuccessDistance = 3f;
    [SerializeField] private float partnerSpacing = 10f;

    [Header("Goalie Practice Settings")]
    [SerializeField] private GameObject aiShooterPrefab;
    [SerializeField] private int numberOfShooters = 3;
    [SerializeField] private float shotInterval = 3f;
    [SerializeField] private float shooterDistance = 15f;
    [SerializeField] private float shooterAccuracy = 0.7f;

    [Header("Skating Drill Settings")]
    [SerializeField] private GameObject conePrefab;
    [SerializeField] private int numberOfCones = 10;
    [SerializeField] private float coneSpacing = 5f;
    [SerializeField] private bool timeTrialMode = false;
    [SerializeField] private float speedTestDuration = 10f;
    [SerializeField] private float coneHitPenalty = 2f; // Seconds added to lap time

    [Header("UI & Feedback")]
    [SerializeField] private bool showStats = true;
    [SerializeField] private float feedbackDuration = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Runtime state
    private DrillStats stats;
    private List<GameObject> spawnedObjects;
    private List<HockeyPlayer> aiPlayers;
    private List<Transform> targets;
    private List<Transform> cones;
    private float drillTimer;
    private float puckRespawnTimer;
    private float shotTimer;
    private bool lapInProgress;
    private Vector3 playerStartPosition;
    private int currentCheckpoint;
    private float speedTestDistance;

    // Properties
    public PracticeType CurrentPractice => currentPracticeType;
    public bool IsPracticeActive => practiceActive;
    public DrillStats CurrentStats => stats;
    public bool GoalieEnabled
    {
        get => goalieEnabled;
        set
        {
            goalieEnabled = value;
            if (practiceActive && currentPracticeType == PracticeType.ShootingPractice)
            {
                UpdateGoalieState();
            }
        }
    }

    private void Awake()
    {
        spawnedObjects = new List<GameObject>();
        aiPlayers = new List<HockeyPlayer>();
        targets = new List<Transform>();
        cones = new List<Transform>();
        stats = new DrillStats();

        // Auto-find references if not set
        if (puck == null) puck = FindObjectOfType<Puck>();
        if (playerController == null) playerController = FindObjectOfType<HockeyPlayer>();
        if (goalieController == null) goalieController = FindObjectOfType<GoalieController>();
        if (practiceGoal == null) practiceGoal = FindObjectOfType<Goal>();
        if (goalTransform == null && practiceGoal != null) goalTransform = practiceGoal.transform;
    }

    private void Update()
    {
        if (!practiceActive) return;

        drillTimer += Time.deltaTime;

        switch (currentPracticeType)
        {
            case PracticeType.ShootingPractice:
                UpdateShootingPractice();
                break;
            case PracticeType.PassingDrill:
                UpdatePassingDrill();
                break;
            case PracticeType.GoaliePractice:
                UpdateGoaliePractice();
                break;
            case PracticeType.SkatingDrill:
                UpdateSkatingDrill();
                break;
            case PracticeType.FreeSkate:
                UpdateFreeSkate();
                break;
        }
    }

    #region Public API

    /// <summary>
    /// Start a practice session of the specified type.
    /// </summary>
    public void StartPractice(PracticeType type)
    {
        if (practiceActive)
        {
            EndPractice();
        }

        currentPracticeType = type;
        practiceActive = true;
        stats = new DrillStats();
        drillTimer = 0f;

        Debug.Log($"[PracticeMode] Starting {type} practice");

        switch (type)
        {
            case PracticeType.FreeSkate:
                SetupFreeSkate();
                break;
            case PracticeType.ShootingPractice:
                SetupShootingPractice();
                break;
            case PracticeType.PassingDrill:
                SetupPassingDrill();
                break;
            case PracticeType.GoaliePractice:
                SetupGoaliePractice();
                break;
            case PracticeType.SkatingDrill:
                SetupSkatingDrill();
                break;
        }

        GameEvents.TriggerMatchStart(); // Use existing event system
    }

    /// <summary>
    /// End the current practice session.
    /// </summary>
    public void EndPractice()
    {
        if (!practiceActive) return;

        Debug.Log($"[PracticeMode] Ending {currentPracticeType} practice");
        Debug.Log($"[PracticeMode] Final Stats: {GetStatsString()}");

        practiceActive = false;
        CleanupPractice();

        GameEvents.TriggerMatchEnd();
    }

    /// <summary>
    /// Reset the current drill (keep same type, reset stats and objects).
    /// </summary>
    public void ResetDrill()
    {
        if (!practiceActive) return;

        PracticeType typeToReset = currentPracticeType;
        EndPractice();
        StartPractice(typeToReset);
    }

    /// <summary>
    /// Spawn shooting targets in the goal.
    /// </summary>
    public void SpawnTargets()
    {
        if (goalTransform == null)
        {
            Debug.LogWarning("[PracticeMode] Cannot spawn targets - no goal reference!");
            return;
        }

        ClearTargets();

        // Target positions within the goal (corners and center)
        Vector3[] targetPositions = new Vector3[]
        {
            new Vector3(-0.8f, 0.5f, 0f), // Top left
            new Vector3(0.8f, 0.5f, 0f),  // Top right
            new Vector3(-0.8f, -0.3f, 0f), // Bottom left
            new Vector3(0.8f, -0.3f, 0f),  // Bottom right
            new Vector3(0f, 0.3f, 0f)      // Center
        };

        for (int i = 0; i < Mathf.Min(numberOfTargets, targetPositions.Length); i++)
        {
            GameObject target = CreateTarget(targetPositions[i]);
            if (target != null)
            {
                targets.Add(target.transform);
                spawnedObjects.Add(target);
            }
        }

        Debug.Log($"[PracticeMode] Spawned {targets.Count} targets");
    }

    /// <summary>
    /// Spawn cones for skating drills.
    /// </summary>
    public void SpawnCones()
    {
        ClearCones();

        if (playerController == null)
        {
            Debug.LogWarning("[PracticeMode] Cannot spawn cones - no player reference!");
            return;
        }

        Vector3 startPos = playerController.transform.position;

        // Create a slalom pattern
        for (int i = 0; i < numberOfCones; i++)
        {
            float zOffset = i * coneSpacing;
            float xOffset = (i % 2 == 0) ? -2f : 2f; // Alternate sides

            Vector3 conePos = startPos + new Vector3(xOffset, 0f, zOffset);
            GameObject cone = CreateCone(conePos);

            if (cone != null)
            {
                cones.Add(cone.transform);
                spawnedObjects.Add(cone);
            }
        }

        Debug.Log($"[PracticeMode] Spawned {cones.Count} cones");
    }

    #endregion

    #region Setup Methods

    private void SetupFreeSkate()
    {
        // Simple setup - just player and puck, no opponents
        if (puck != null && puckSpawnPoint != null)
        {
            puck.ResetPosition(puckSpawnPoint.position);
        }
        else if (puck != null && playerController != null)
        {
            Vector3 puckPos = playerController.transform.position + playerController.transform.forward * 2f;
            puck.ResetPosition(puckPos);
        }

        // Disable any active AI
        DisableAllAI();

        Debug.Log("[PracticeMode] Free skate mode ready - just you and the puck!");
    }

    private void SetupShootingPractice()
    {
        // Spawn targets
        SpawnTargets();

        // Setup puck
        if (puck != null && puckSpawnPoint != null)
        {
            puck.ResetPosition(puckSpawnPoint.position);
        }

        // Setup goalie
        UpdateGoalieState();

        // Subscribe to goal events
        if (practiceGoal != null)
        {
            GameEvents.OnGoalScored += OnPracticeShotScored;
        }

        Debug.Log("[PracticeMode] Shooting practice ready - aim for the targets!");
    }

    private void SetupPassingDrill()
    {
        if (aiTeammatePrefab == null || playerController == null)
        {
            Debug.LogWarning("[PracticeMode] Cannot setup passing drill - missing prefabs!");
            return;
        }

        // Spawn AI teammates in a line
        Vector3 playerPos = playerController.transform.position;
        Vector3 playerForward = playerController.transform.forward;

        for (int i = 0; i < numberOfPassPartners; i++)
        {
            Vector3 partnerPos = playerPos + playerForward * ((i + 1) * partnerSpacing);
            GameObject partnerObj = Instantiate(aiTeammatePrefab, partnerPos, Quaternion.identity);

            HockeyPlayer partner = partnerObj.GetComponent<HockeyPlayer>();
            if (partner != null)
            {
                partner.SetTeam(playerController.TeamId); // Same team
                aiPlayers.Add(partner);

                // Setup simple AI to pass back
                AIController ai = partnerObj.GetComponent<AIController>();
                if (ai != null)
                {
                    ai.enabled = true; // They'll auto-pass back when they get the puck
                }
            }

            spawnedObjects.Add(partnerObj);
        }

        // Give player the puck to start
        if (puck != null && playerController != null)
        {
            Vector3 puckPos = playerController.transform.position + playerController.transform.forward * 1.5f;
            puck.ResetPosition(puckPos);
        }

        Debug.Log($"[PracticeMode] Passing drill ready - {numberOfPassPartners} partners spawned!");
    }

    private void SetupGoaliePractice()
    {
        if (goalieController == null)
        {
            Debug.LogWarning("[PracticeMode] Cannot setup goalie practice - no goalie controller!");
            return;
        }

        if (aiShooterPrefab == null)
        {
            Debug.LogWarning("[PracticeMode] Cannot setup goalie practice - no shooter prefab!");
            return;
        }

        // Enable goalie control for player
        goalieController.enabled = true;

        // Spawn AI shooters
        if (goalTransform != null)
        {
            for (int i = 0; i < numberOfShooters; i++)
            {
                float angle = (i - numberOfShooters / 2f) * 30f; // Spread shooters
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * -goalTransform.forward;
                Vector3 shooterPos = goalTransform.position + direction * shooterDistance;
                shooterPos.y = 0f;

                GameObject shooterObj = Instantiate(aiShooterPrefab, shooterPos, Quaternion.identity);
                HockeyPlayer shooter = shooterObj.GetComponent<HockeyPlayer>();

                if (shooter != null)
                {
                    // Opposite team from goalie
                    shooter.SetTeam(goalieController.TeamId == 0 ? 1 : 0);
                    aiPlayers.Add(shooter);
                }

                spawnedObjects.Add(shooterObj);
            }
        }

        shotTimer = shotInterval; // First shot after interval

        Debug.Log($"[PracticeMode] Goalie practice ready - {numberOfShooters} shooters prepared!");
    }

    private void SetupSkatingDrill()
    {
        if (playerController == null)
        {
            Debug.LogWarning("[PracticeMode] Cannot setup skating drill - no player!");
            return;
        }

        playerStartPosition = playerController.transform.position;
        SpawnCones();

        lapInProgress = false;
        currentCheckpoint = 0;
        speedTestDistance = 0f;
        stats.bestLapTime = float.MaxValue;

        Debug.Log("[PracticeMode] Skating drill ready - navigate the cones!");
        if (timeTrialMode)
        {
            Debug.Log("[PracticeMode] Time trial mode active - beat your best time!");
        }
    }

    #endregion

    #region Update Methods

    private void UpdateFreeSkate()
    {
        // Nothing to update - just free skating
        // Could add tricks tracking or distance traveled here
    }

    private void UpdateShootingPractice()
    {
        // Check if puck needs respawning
        if (puckRespawnTimer > 0f)
        {
            puckRespawnTimer -= Time.deltaTime;

            if (puckRespawnTimer <= 0f)
            {
                RespawnPuck();
            }
        }

        // Check for target hits
        CheckTargetHits();
    }

    private void UpdatePassingDrill()
    {
        // Track successful passes
        if (puck != null && puck.CurrentOwner != null)
        {
            // Check if puck changed hands
            CheckPassSuccess();
        }
    }

    private void UpdateGoaliePractice()
    {
        shotTimer -= Time.deltaTime;

        if (shotTimer <= 0f)
        {
            TriggerAIShot();
            shotTimer = shotInterval;
        }
    }

    private void UpdateSkatingDrill()
    {
        if (playerController == null) return;

        if (timeTrialMode && lapInProgress)
        {
            stats.currentLapTime += Time.deltaTime;

            // Check if lap completed (returned to start)
            float distanceToStart = Vector3.Distance(playerController.transform.position, playerStartPosition);
            if (distanceToStart < 3f && currentCheckpoint >= cones.Count)
            {
                CompleteLap();
            }
        }
        else if (!timeTrialMode)
        {
            // Speed test mode - track distance
            speedTestDistance += playerController.Velocity.magnitude * Time.deltaTime;
        }

        // Check cone proximity
        CheckConeProximity();
    }

    #endregion

    #region Helper Methods

    private void UpdateGoalieState()
    {
        if (goalieController != null && currentPracticeType == PracticeType.ShootingPractice)
        {
            goalieController.enabled = goalieEnabled;
            goalieController.gameObject.SetActive(goalieEnabled);
        }
    }

    private void DisableAllAI()
    {
        AIController[] allAI = FindObjectsOfType<AIController>();
        foreach (var ai in allAI)
        {
            ai.enabled = false;
        }
    }

    private void RespawnPuck()
    {
        if (puck == null) return;

        Vector3 spawnPos = puckSpawnPoint != null ? puckSpawnPoint.position : playerController.transform.position + Vector3.forward * 2f;
        puck.ResetPosition(spawnPos);

        Debug.Log("[PracticeMode] Puck respawned");
    }

    private void CheckTargetHits()
    {
        if (puck == null) return;

        foreach (Transform target in targets)
        {
            if (target == null) continue;

            float distance = Vector3.Distance(puck.transform.position, target.position);
            if (distance < targetSize)
            {
                OnTargetHit(target);
            }
        }
    }

    private void OnTargetHit(Transform target)
    {
        stats.targetsHit++;

        Debug.Log($"[PracticeMode] Target hit! Total: {stats.targetsHit}, Points: +{targetPoints}");

        // Visual feedback
        if (target.gameObject.activeSelf)
        {
            // Could play particle effect here
            StartCoroutine(FlashTarget(target));
        }
    }

    private System.Collections.IEnumerator FlashTarget(Transform target)
    {
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.green;
            yield return new WaitForSeconds(0.3f);
            renderer.material.color = originalColor;
        }
    }

    private void CheckPassSuccess()
    {
        // Simple pass tracking - if puck changed owners to an AI partner
        if (puck.LastOwner == playerController)
        {
            foreach (HockeyPlayer ai in aiPlayers)
            {
                if (puck.CurrentOwner == ai)
                {
                    stats.successfulPasses++;
                    Debug.Log($"[PracticeMode] Successful pass! Accuracy: {stats.PassAccuracy:F1}%");
                }
            }
        }
        // Track when AI passes back to player
        else if (aiPlayers.Contains(puck.LastOwner) && puck.CurrentOwner == playerController)
        {
            stats.totalPasses++;
        }
    }

    private void TriggerAIShot()
    {
        if (aiPlayers.Count == 0 || puck == null || goalTransform == null) return;

        // Pick random shooter
        HockeyPlayer shooter = aiPlayers[Random.Range(0, aiPlayers.Count)];

        // Give them the puck
        puck.ResetPosition(shooter.transform.position + shooter.transform.forward * 1.5f);
        puck.GainPossession(shooter);

        stats.totalShots_Goalie++;

        // Trigger shot after short delay
        StartCoroutine(DelayedAIShot(shooter));
    }

    private System.Collections.IEnumerator DelayedAIShot(HockeyPlayer shooter)
    {
        yield return new WaitForSeconds(0.5f);

        if (shooter == null || puck == null || goalTransform == null) yield break;

        // Calculate shot direction with accuracy variance
        Vector3 targetPoint = goalTransform.position;
        targetPoint += Random.insideUnitSphere * (1f - shooterAccuracy) * 2f;
        targetPoint.y = goalTransform.position.y;

        Vector3 shotDirection = (targetPoint - shooter.transform.position).normalized;
        float shotPower = Random.Range(20f, 35f);

        puck.Shoot(shotDirection, shotPower);
    }

    private void CheckConeProximity()
    {
        if (playerController == null) return;

        for (int i = 0; i < cones.Count; i++)
        {
            if (cones[i] == null) continue;

            float distance = Vector3.Distance(playerController.transform.position, cones[i].position);

            // Track checkpoints in order
            if (distance < 2f && i == currentCheckpoint && lapInProgress)
            {
                currentCheckpoint++;
                Debug.Log($"[PracticeMode] Checkpoint {currentCheckpoint}/{cones.Count}");
            }

            // Penalty for hitting cones
            if (distance < 0.5f)
            {
                OnConeHit(cones[i]);
            }
        }
    }

    private void OnConeHit(Transform cone)
    {
        stats.conesHit++;
        stats.currentLapTime += coneHitPenalty;

        Debug.Log($"[PracticeMode] Cone hit! +{coneHitPenalty}s penalty");

        // Visual feedback
        StartCoroutine(ShakeCone(cone));
    }

    private System.Collections.IEnumerator ShakeCone(Transform cone)
    {
        Vector3 originalPos = cone.position;
        float shakeDuration = 0.3f;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            cone.position = originalPos + Random.insideUnitSphere * 0.1f;
            elapsed += Time.deltaTime;
            yield return null;
        }

        cone.position = originalPos;
    }

    private void CompleteLap()
    {
        float lapTime = stats.currentLapTime;

        if (lapTime < stats.bestLapTime)
        {
            stats.bestLapTime = lapTime;
            Debug.Log($"[PracticeMode] NEW BEST LAP TIME: {lapTime:F2}s!");
        }
        else
        {
            Debug.Log($"[PracticeMode] Lap completed: {lapTime:F2}s (Best: {stats.bestLapTime:F2}s)");
        }

        // Reset for next lap
        lapInProgress = false;
        currentCheckpoint = 0;
        stats.currentLapTime = 0f;
    }

    private void OnPracticeShotScored(int teamIndex)
    {
        stats.totalShots++;
        stats.shotsOnTarget++;

        Debug.Log($"[PracticeMode] GOAL! Accuracy: {stats.ShotAccuracy:F1}%");

        // Respawn puck after goal
        puckRespawnTimer = puckRespawnDelay;
    }

    private void ClearTargets()
    {
        foreach (Transform target in targets)
        {
            if (target != null) Destroy(target.gameObject);
        }
        targets.Clear();
    }

    private void ClearCones()
    {
        foreach (Transform cone in cones)
        {
            if (cone != null) Destroy(cone.gameObject);
        }
        cones.Clear();
    }

    private void CleanupPractice()
    {
        // Clean up all spawned objects
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
        aiPlayers.Clear();
        targets.Clear();
        cones.Clear();

        // Unsubscribe from events
        GameEvents.OnGoalScored -= OnPracticeShotScored;

        // Re-enable normal AI
        AIController[] allAI = FindObjectsOfType<AIController>();
        foreach (var ai in allAI)
        {
            ai.enabled = true;
        }
    }

    private GameObject CreateTarget(Vector3 localPosition)
    {
        if (goalTransform == null) return null;

        GameObject target;

        if (targetPrefab != null)
        {
            target = Instantiate(targetPrefab, goalTransform);
        }
        else
        {
            // Create simple sphere target
            target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.transform.SetParent(goalTransform);
            target.GetComponent<MeshRenderer>().material.color = Color.red;

            // Remove collider so it doesn't block shots
            Destroy(target.GetComponent<Collider>());
        }

        target.transform.localPosition = localPosition;
        target.transform.localScale = Vector3.one * targetSize;
        target.name = $"Target_{targets.Count}";

        return target;
    }

    private GameObject CreateCone(Vector3 position)
    {
        GameObject cone;

        if (conePrefab != null)
        {
            cone = Instantiate(conePrefab, position, Quaternion.identity);
        }
        else
        {
            // Create simple cone
            cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.transform.position = position;
            cone.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            cone.GetComponent<MeshRenderer>().material.color = Color.yellow;
        }

        cone.name = $"Cone_{cones.Count}";
        return cone;
    }

    private string GetStatsString()
    {
        switch (currentPracticeType)
        {
            case PracticeType.ShootingPractice:
                return $"Shots: {stats.totalShots}, Accuracy: {stats.ShotAccuracy:F1}%, Targets Hit: {stats.targetsHit}";

            case PracticeType.PassingDrill:
                return $"Passes: {stats.totalPasses}, Successful: {stats.successfulPasses}, Accuracy: {stats.PassAccuracy:F1}%";

            case PracticeType.GoaliePractice:
                return $"Shots Faced: {stats.totalShots_Goalie}, Saves: {stats.totalSaves}, Save%: {stats.SavePercentage:F1}%";

            case PracticeType.SkatingDrill:
                return timeTrialMode
                    ? $"Best Lap: {stats.bestLapTime:F2}s, Cones Hit: {stats.conesHit}"
                    : $"Distance: {speedTestDistance:F1}m, Speed: {speedTestDistance / drillTimer:F1}m/s";

            default:
                return $"Practice time: {drillTimer:F1}s";
        }
    }

    #endregion

    #region Public Helpers

    /// <summary>
    /// Start a lap timer for skating drills.
    /// </summary>
    public void StartLap()
    {
        if (currentPracticeType != PracticeType.SkatingDrill) return;

        lapInProgress = true;
        currentCheckpoint = 0;
        stats.currentLapTime = 0f;

        Debug.Log("[PracticeMode] Lap started!");
    }

    /// <summary>
    /// Record a save for goalie practice.
    /// </summary>
    public void RecordSave()
    {
        if (currentPracticeType != PracticeType.GoaliePractice) return;

        stats.totalSaves++;
        Debug.Log($"[PracticeMode] SAVE! Save%: {stats.SavePercentage:F1}%");
    }

    /// <summary>
    /// Get current drill progress as a formatted string.
    /// </summary>
    public string GetProgressString()
    {
        return GetStatsString();
    }

    /// <summary>
    /// Toggle goalie for shooting practice.
    /// </summary>
    public void ToggleGoalie()
    {
        GoalieEnabled = !GoalieEnabled;
        Debug.Log($"[PracticeMode] Goalie {(GoalieEnabled ? "enabled" : "disabled")}");
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw practice area indicator
        if (practiceActive)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }

        // Draw cone path
        if (cones.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < cones.Count - 1; i++)
            {
                if (cones[i] != null && cones[i + 1] != null)
                {
                    Gizmos.DrawLine(cones[i].position, cones[i + 1].position);
                }
            }
        }

        // Draw shooter positions
        if (currentPracticeType == PracticeType.GoaliePractice && goalTransform != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < numberOfShooters; i++)
            {
                float angle = (i - numberOfShooters / 2f) * 30f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * -goalTransform.forward;
                Vector3 shooterPos = goalTransform.position + direction * shooterDistance;
                Gizmos.DrawWireSphere(shooterPos, 0.5f);
            }
        }

        // Draw target positions
        if (targets.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (Transform target in targets)
            {
                if (target != null)
                {
                    Gizmos.DrawWireSphere(target.position, targetSize);
                }
            }
        }
    }

    private void OnGUI()
    {
        if (!showStats || !practiceActive) return;

        // Display stats in top-left corner
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        string statsText = $"PRACTICE MODE: {currentPracticeType}\n";
        statsText += $"Time: {drillTimer:F1}s\n";
        statsText += GetStatsString();

        GUI.Label(new Rect(10, 10, 400, 200), statsText, style);
    }

    #endregion
}
