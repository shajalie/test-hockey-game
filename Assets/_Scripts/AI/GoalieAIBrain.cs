using UnityEngine;

/// <summary>
/// Specialized AI brain for goalies.
/// Handles positioning, save attempts, and puck clearing.
/// </summary>
public class GoalieAIBrain : MonoBehaviour
{
    #region Enums

    public enum GoalieState
    {
        Ready,          // Set position, tracking puck
        Positioning,    // Moving to cut angle
        Challenging,    // Moving out to shooter
        SaveAttempt,    // Making a save
        Recovery,       // Getting back after save
        PlayingPuck,    // Handling loose puck
        Passing         // Making a pass
    }

    public enum SaveType
    {
        Glove,
        Blocker,
        Butterfly,
        StackPads,
        Poke
    }

    #endregion

    #region Serialized Fields

    [Header("Crease Positioning")]
    [SerializeField] private float creaseDepth = 1.5f;
    [SerializeField] private float maxChallengeDepth = 4f;
    [SerializeField] private float lateralSpeed = 15f;

    [Header("Save Settings")]
    [SerializeField] private float saveReactionTime = 0.1f;
    [SerializeField] private float saveRange = 2f;
    [SerializeField] private float butterflyThreshold = 0.8f; // Height below which to butterfly

    [Header("Puck Handling")]
    [SerializeField] private float trapezoidSize = 5f;
    [SerializeField] private float clearDistance = 20f;

    [Header("AI Tuning")]
    [SerializeField] private float positionUpdateRate = 0.05f;
    [SerializeField] private float threatAssessmentRate = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    #endregion

    #region Private Fields

    private PlayerInputController inputController;
    private IcePhysicsController physics;
    private HockeyPlayer hockeyPlayer;

    private GoalieState currentState = GoalieState.Ready;
    private Vector3 goalLineCenter;
    private Vector3 targetPosition;

    private float positionTimer;
    private float threatTimer;

    // Puck tracking
    private PuckController puck;
    private Vector3 lastPuckPos;
    private Vector3 puckVelocity;
    private float threatLevel; // 0-1, how dangerous is current situation

    // Save state
    private bool isMakingSave;
    private SaveType currentSaveType;
    private float saveTimer;

    #endregion

    #region Properties

    public GoalieState State => currentState;
    public float ThreatLevel => threatLevel;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        inputController = GetComponent<PlayerInputController>();
        physics = GetComponent<IcePhysicsController>();
        hockeyPlayer = GetComponent<HockeyPlayer>();
    }

    private void Start()
    {
        puck = FindObjectOfType<PuckController>();

        // Set goal line position based on team
        int teamId = hockeyPlayer?.TeamId ?? 0;
        goalLineCenter = new Vector3(0, 0, teamId == 0 ? -26f : 26f);

        PuckController.OnPuckShot += OnPuckShot;
    }

    private void OnDestroy()
    {
        PuckController.OnPuckShot -= OnPuckShot;
    }

    private void Update()
    {
        if (inputController == null || !inputController.IsAIControlled) return;

        UpdateTimers();
        TrackPuck();

        if (threatTimer <= 0)
        {
            AssessThreat();
            threatTimer = threatAssessmentRate;
        }

        if (positionTimer <= 0)
        {
            UpdateState();
            positionTimer = positionUpdateRate;
        }

        ExecuteState();
    }

    #endregion

    #region State Management

    private void UpdateState()
    {
        if (isMakingSave)
        {
            // Stay in save state until recovery
            return;
        }

        GoalieState newState = DetermineState();

        if (newState != currentState)
        {
            currentState = newState;
            if (showDebugInfo) Debug.Log($"[GoalieAI] State: {currentState}");
        }
    }

    private GoalieState DetermineState()
    {
        if (puck == null) return GoalieState.Ready;

        // Am I in possession?
        if (puck.Owner == gameObject)
        {
            return GoalieState.PlayingPuck;
        }

        // Is puck loose near me?
        float distToPuck = Vector3.Distance(transform.position, puck.transform.position);
        if (!puck.IsPossessed && distToPuck < trapezoidSize)
        {
            return GoalieState.PlayingPuck;
        }

        // High threat - need to save
        if (threatLevel > 0.8f && puck.IsShot)
        {
            return GoalieState.SaveAttempt;
        }

        // Medium threat - challenge shooter
        if (threatLevel > 0.5f)
        {
            return GoalieState.Challenging;
        }

        // Low threat - maintain position
        return GoalieState.Positioning;
    }

    private void ExecuteState()
    {
        switch (currentState)
        {
            case GoalieState.Ready:
            case GoalieState.Positioning:
                ExecutePositioning();
                break;
            case GoalieState.Challenging:
                ExecuteChallenging();
                break;
            case GoalieState.SaveAttempt:
                ExecuteSaveAttempt();
                break;
            case GoalieState.Recovery:
                ExecuteRecovery();
                break;
            case GoalieState.PlayingPuck:
                ExecutePlayingPuck();
                break;
            case GoalieState.Passing:
                ExecutePassing();
                break;
        }
    }

    #endregion

    #region State Execution

    private void ExecutePositioning()
    {
        // Calculate angle bisector position
        targetPosition = CalculateAngleBisectorPosition();
        MoveToPosition(targetPosition);

        // Face the puck
        FacePuck();
    }

    private void ExecuteChallenging()
    {
        // Move out to cut angle
        Vector3 challengePos = CalculateChallengePosition();
        MoveToPosition(challengePos);
        FacePuck();
    }

    private void ExecuteSaveAttempt()
    {
        if (!isMakingSave)
        {
            // Determine save type
            currentSaveType = DetermineSaveType();
            isMakingSave = true;
            saveTimer = 0.5f;

            if (showDebugInfo) Debug.Log($"[GoalieAI] Save attempt: {currentSaveType}");
        }

        // Execute save movement
        switch (currentSaveType)
        {
            case SaveType.Butterfly:
                // Drop down, spread legs
                inputController.SetAIMoveInput(Vector2.zero);
                break;

            case SaveType.Glove:
            case SaveType.Blocker:
                // Move laterally toward puck
                Vector3 toPuck = puck.transform.position - transform.position;
                Vector2 lateralMove = new Vector2(Mathf.Sign(toPuck.x), 0);
                inputController.SetAIMoveInput(lateralMove);
                break;

            case SaveType.StackPads:
                // Dive laterally
                inputController.AITriggerDash();
                break;

            case SaveType.Poke:
                // Lunge forward
                inputController.SetAIMoveInput(new Vector2(0, 1));
                inputController.AIPokeCheck();
                break;
        }

        // Timer for save
        saveTimer -= Time.deltaTime;
        if (saveTimer <= 0)
        {
            isMakingSave = false;
            currentState = GoalieState.Recovery;
        }
    }

    private void ExecuteRecovery()
    {
        // Return to crease
        targetPosition = goalLineCenter + GetCreaseOffset();
        MoveToPosition(targetPosition);

        // If back in position, switch to ready
        if (Vector3.Distance(transform.position, targetPosition) < 1f)
        {
            currentState = GoalieState.Ready;
        }
    }

    private void ExecutePlayingPuck()
    {
        if (puck == null) return;

        float distToPuck = Vector3.Distance(transform.position, puck.transform.position);

        // If we have possession, pass it
        if (puck.Owner == gameObject)
        {
            currentState = GoalieState.Passing;
            return;
        }

        // Move toward puck if it's loose and nearby
        if (!puck.IsPossessed && distToPuck < trapezoidSize)
        {
            MoveToPosition(puck.transform.position);
        }
        else
        {
            // Return to crease
            currentState = GoalieState.Recovery;
        }
    }

    private void ExecutePassing()
    {
        // Find best clear target
        GameObject target = FindClearTarget();

        if (target != null)
        {
            Vector3 clearDir = (target.transform.position - transform.position).normalized;
            inputController.AIPass(target);
        }
        else
        {
            // Just clear it
            int teamId = hockeyPlayer?.TeamId ?? 0;
            Vector3 clearDir = new Vector3(Random.Range(-0.5f, 0.5f), 0, teamId == 0 ? 1f : -1f).normalized;
            inputController.AIShoot(clearDir, 20f);
        }

        currentState = GoalieState.Recovery;
    }

    #endregion

    #region Positioning Calculations

    private Vector3 CalculateAngleBisectorPosition()
    {
        if (puck == null) return goalLineCenter;

        Vector3 puckPos = puck.transform.position;

        // Vector from goal center to puck
        Vector3 toPost = puckPos - goalLineCenter;
        toPost.y = 0;

        // Position along this line, at crease depth
        float distance = Mathf.Min(toPost.magnitude * 0.3f, creaseDepth);
        Vector3 pos = goalLineCenter + toPost.normalized * distance;

        // Clamp to crease width
        float maxWidth = 2f;
        pos.x = Mathf.Clamp(pos.x, goalLineCenter.x - maxWidth, goalLineCenter.x + maxWidth);

        return pos;
    }

    private Vector3 CalculateChallengePosition()
    {
        if (puck == null) return goalLineCenter;

        Vector3 puckPos = puck.transform.position;
        Vector3 toPost = puckPos - goalLineCenter;
        toPost.y = 0;

        // Challenge depth based on threat
        float challengeDepth = Mathf.Lerp(creaseDepth, maxChallengeDepth, threatLevel);
        challengeDepth = Mathf.Min(challengeDepth, toPost.magnitude * 0.5f);

        return goalLineCenter + toPost.normalized * challengeDepth;
    }

    private Vector3 GetCreaseOffset()
    {
        int teamId = hockeyPlayer?.TeamId ?? 0;
        return new Vector3(0, 0, teamId == 0 ? creaseDepth : -creaseDepth);
    }

    #endregion

    #region Save Logic

    private SaveType DetermineSaveType()
    {
        if (puck == null) return SaveType.Butterfly;

        Vector3 puckPos = puck.transform.position;
        Vector3 puckVel = puck.Velocity;

        // Predict where puck will cross goal line
        float timeToGoal = Mathf.Abs((goalLineCenter.z - puckPos.z) / puckVel.z);
        Vector3 crossingPoint = puckPos + puckVel * timeToGoal;

        float heightAtCrossing = crossingPoint.y;
        float lateralDistance = Mathf.Abs(crossingPoint.x - transform.position.x);

        // Low shot
        if (heightAtCrossing < butterflyThreshold)
        {
            if (lateralDistance > saveRange)
            {
                return SaveType.StackPads; // Diving save
            }
            return SaveType.Butterfly;
        }

        // High shot
        if (crossingPoint.x > transform.position.x)
        {
            return SaveType.Blocker;
        }
        else
        {
            return SaveType.Glove;
        }
    }

    #endregion

    #region Puck Tracking

    private void TrackPuck()
    {
        if (puck == null) return;

        puckVelocity = (puck.transform.position - lastPuckPos) / Time.deltaTime;
        lastPuckPos = puck.transform.position;
    }

    private void AssessThreat()
    {
        if (puck == null)
        {
            threatLevel = 0;
            return;
        }

        float threat = 0f;

        // Distance to goal
        float distToGoal = Vector3.Distance(puck.transform.position, goalLineCenter);
        threat += Mathf.Clamp01(1f - distToGoal / 30f) * 0.3f;

        // Puck moving toward goal
        int teamId = hockeyPlayer?.TeamId ?? 0;
        float towardGoal = teamId == 0 ? -puck.Velocity.z : puck.Velocity.z;
        if (towardGoal > 0)
        {
            threat += Mathf.Clamp01(towardGoal / 30f) * 0.4f;
        }

        // Puck is a shot
        if (puck.IsShot)
        {
            threat += 0.3f;
        }

        // Opponent has puck in zone
        if (puck.IsPossessed)
        {
            HockeyPlayer owner = puck.Owner?.GetComponent<HockeyPlayer>();
            if (owner != null && owner.TeamId != teamId && distToGoal < 20f)
            {
                threat += 0.2f;
            }
        }

        threatLevel = Mathf.Clamp01(threat);
    }

    #endregion

    #region Movement

    private void MoveToPosition(Vector3 pos)
    {
        Vector3 direction = pos - transform.position;
        direction.y = 0;

        float distance = direction.magnitude;
        if (distance < 0.3f)
        {
            inputController.SetAIMoveInput(Vector2.zero);
            return;
        }

        direction.Normalize();

        // Lateral movement is faster
        float xWeight = Mathf.Abs(direction.x) > Mathf.Abs(direction.z) ? lateralSpeed : 1f;
        Vector2 move = new Vector2(direction.x * xWeight, direction.z);
        move = Vector2.ClampMagnitude(move, 1f);

        inputController.SetAIMoveInput(move);
    }

    private void FacePuck()
    {
        if (puck == null) return;

        Vector3 toPuck = (puck.transform.position - transform.position).normalized;
        inputController.SetAIAimInput(new Vector2(toPuck.x, toPuck.z));
    }

    #endregion

    #region Helpers

    private void UpdateTimers()
    {
        positionTimer -= Time.deltaTime;
        threatTimer -= Time.deltaTime;
    }

    private GameObject FindClearTarget()
    {
        HockeyPlayer[] teammates = FindObjectsOfType<HockeyPlayer>();
        GameObject bestTarget = null;
        float bestScore = 0;

        int myTeamId = hockeyPlayer?.TeamId ?? 0;

        foreach (var player in teammates)
        {
            if (player.TeamId != myTeamId) continue;
            if (player.gameObject == gameObject) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < 5f || dist > clearDistance) continue;

            // Prefer players up ice
            float zDir = myTeamId == 0 ? player.transform.position.z : -player.transform.position.z;
            float score = zDir + (30f - dist);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = player.gameObject;
            }
        }

        return bestTarget;
    }

    private void OnPuckShot(PuckController puck, Vector3 direction, float power)
    {
        // Check if shot is at our goal
        int teamId = hockeyPlayer?.TeamId ?? 0;
        bool shotAtUs = teamId == 0 ? direction.z < -0.5f : direction.z > 0.5f;

        if (shotAtUs && Vector3.Distance(puck.transform.position, goalLineCenter) < 25f)
        {
            // Immediate save attempt
            threatLevel = 1f;
            currentState = GoalieState.SaveAttempt;
        }
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Goal line
        Gizmos.color = Color.red;
        Gizmos.DrawLine(goalLineCenter + Vector3.left * 3f, goalLineCenter + Vector3.right * 3f);

        // Target position
        if (targetPosition != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }

        // Threat level indicator
        Gizmos.color = Color.Lerp(Color.green, Color.red, threatLevel);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.5f);

        // Crease area
        Gizmos.color = new Color(0, 0, 1, 0.2f);
        Gizmos.DrawWireCube(goalLineCenter + GetCreaseOffset() * 0.5f, new Vector3(4f, 0.1f, creaseDepth));
    }

    #endregion
}
