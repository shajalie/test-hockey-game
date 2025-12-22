using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Advanced team-based AI controller for hockey players.
/// Features intelligent positioning, passing, role-based behavior, and formation awareness.
/// AI players make smart decisions like real hockey players - not just chasing the puck.
/// </summary>
[RequireComponent(typeof(HockeyPlayer))]
public class AIController : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        ChasePuck,
        AttackWithPuck,
        DefendGoal,
        ReturnToPosition,
        SupportTeammate,
        PositionForPass,
        ClearZone,
        SetupPlay
    }

    [Header("References")]
    [SerializeField] private Transform opponentGoal;
    [SerializeField] private Transform ownGoal;
    [SerializeField] private Transform homePositionTransform; // Optional - use Transform reference
    [SerializeField] private Puck puck;
    [SerializeField] private RinkBuilder rinkBuilder;
    [SerializeField] private TeamManager teamManagerRef;

    // Home position can be set as Vector3 directly (preferred by TeamManager)
    private Vector3 homePositionPoint;
    private bool hasHomePosition = false;

    [Header("AI Settings")]
    [SerializeField] private float reactionTime = 0.2f;
    [SerializeField] private float shootingRange = 10f;
    [SerializeField] private float defendDistance = 15f;
    [SerializeField] private float chaseSpeed = 0.9f;
    [SerializeField] private float shootAccuracy = 0.8f;

    [Header("Team Play Settings")]
    [SerializeField] private float passRange = 20f;
    [SerializeField] private float passConsiderationChance = 0.7f;
    [SerializeField] private float supportDistance = 8f;
    [SerializeField] private float positionTolerance = 3f;
    [SerializeField] private float teammateSearchRadius = 30f;

    [Header("Position-Based Behavior")]
    [SerializeField] private float forwardAggressionRange = 20f;
    [SerializeField] private float defensemanMaxForwardPosition = 10f;
    [SerializeField] private float defensemanFallbackDistance = 15f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private AIState currentState = AIState.Idle;

    // Components
    private HockeyPlayer player;
    private ShootingController shootingController;
    private TeamManager teamManager;

    // State
    private Puck targetPuck;
    private float lastDecisionTime;
    private float shootChargeTimer;
    private bool isChargingShot;
    private HockeyPlayer targetTeammate;
    private Vector3 targetPosition;

    // Team awareness
    private List<HockeyPlayer> teammates;
    private List<HockeyPlayer> opponents;
    private HockeyPlayer puckCarrier;

    // Position role flags
    private bool isForward;
    private bool isDefenseman;
    private bool isGoalie;

    // Properties
    public AIState CurrentState => currentState;

    private void Awake()
    {
        player = GetComponent<HockeyPlayer>();
        shootingController = GetComponent<ShootingController>();
    }

    private void Start()
    {
        // Use serialized puck reference if set, otherwise find in scene
        if (puck != null)
        {
            targetPuck = puck;
        }
        else
        {
            targetPuck = FindObjectOfType<Puck>();
        }

        if (targetPuck == null)
        {
            Debug.LogWarning("[AIController] No puck found in scene!");
        }

        // Use serialized team manager reference if set, otherwise find in scene
        if (teamManagerRef != null)
        {
            teamManager = teamManagerRef;
        }
        else
        {
            teamManager = FindObjectOfType<TeamManager>();
        }

        // Auto-find goals based on team (only if not manually set)
        if (opponentGoal == null || ownGoal == null)
        {
            FindGoals();
        }

        // Initialize team lists
        teammates = new List<HockeyPlayer>();
        opponents = new List<HockeyPlayer>();

        // Determine position role
        DeterminePositionRole();

        // Update team awareness
        UpdateTeamAwareness();
    }

    private void Update()
    {
        if (targetPuck == null) return;

        // Periodically update team awareness
        if (Time.frameCount % 30 == 0)
        {
            UpdateTeamAwareness();
        }

        // Rate-limit decisions for more natural behavior
        if (Time.time - lastDecisionTime > reactionTime)
        {
            lastDecisionTime = Time.time;
            DecideState();
        }

        ExecuteState();
    }

    #region Initialization & Team Awareness

    /// <summary>
    /// Auto-find goals in the scene based on team assignment.
    /// </summary>
    private void FindGoals()
    {
        Goal[] allGoals = FindObjectsOfType<Goal>();

        if (allGoals.Length == 0)
        {
            Debug.LogWarning($"[AIController] No goals found in scene for {player.name}!");
            return;
        }

        foreach (Goal goal in allGoals)
        {
            // If goal's teamIndex matches our team, it's our goal (we defend it)
            // If goal's teamIndex is different, it's the opponent's goal (we attack it)
            if (goal.TeamIndex == player.TeamId)
            {
                ownGoal = goal.transform;
            }
            else
            {
                opponentGoal = goal.transform;
            }
        }

        // Log results
        if (opponentGoal == null)
        {
            Debug.LogWarning($"[AIController] Could not find opponent goal for {player.name} (Team {player.TeamId})");
        }

        if (ownGoal == null)
        {
            Debug.LogWarning($"[AIController] Could not find own goal for {player.name} (Team {player.TeamId})");
        }

        if (opponentGoal != null && ownGoal != null)
        {
            Debug.Log($"[AIController] {player.name} (Team {player.TeamId}) - Attacking: {opponentGoal.name}, Defending: {ownGoal.name}");
        }
    }

    /// <summary>
    /// Determine this player's role based on their position.
    /// </summary>
    private void DeterminePositionRole()
    {
        isGoalie = player.Position == PlayerPosition.Goalie;
        isDefenseman = player.Position == PlayerPosition.LeftDefense ||
                       player.Position == PlayerPosition.RightDefense;
        isForward = player.Position == PlayerPosition.Center ||
                    player.Position == PlayerPosition.LeftWing ||
                    player.Position == PlayerPosition.RightWing;
    }

    /// <summary>
    /// Update awareness of teammates and opponents.
    /// </summary>
    private void UpdateTeamAwareness()
    {
        teammates.Clear();
        opponents.Clear();

        HockeyPlayer[] allPlayers = FindObjectsOfType<HockeyPlayer>();

        foreach (var p in allPlayers)
        {
            if (p == player) continue;

            if (p.TeamId == player.TeamId)
            {
                teammates.Add(p);
            }
            else
            {
                opponents.Add(p);
            }
        }

        // Track who has the puck
        if (targetPuck != null)
        {
            puckCarrier = targetPuck.CurrentOwner;
        }
    }

    #endregion

    #region State Decision Logic

    /// <summary>
    /// Intelligent state decision based on game context, team position, and player role.
    /// </summary>
    private void DecideState()
    {
        // Goalie has special logic
        if (isGoalie)
        {
            DecideGoalieState();
            return;
        }

        // If we have the puck, decide between shooting and passing
        if (player.HasPuck)
        {
            DecideAttackState();
            return;
        }

        // Check if a teammate has the puck
        bool teammateHasPuck = puckCarrier != null && puckCarrier.TeamId == player.TeamId;

        if (teammateHasPuck)
        {
            DecideSupportState();
        }
        else
        {
            DecideDefenseState();
        }
    }

    /// <summary>
    /// Special goalie AI - stay in net, track puck.
    /// </summary>
    private void DecideGoalieState()
    {
        // Goalies mostly defend, rarely chase loose pucks near the net
        float distanceToPuck = Vector3.Distance(transform.position, targetPuck.transform.position);

        if (targetPuck.IsLoose && distanceToPuck < 3f)
        {
            currentState = AIState.ChasePuck;
        }
        else
        {
            currentState = AIState.DefendGoal;
        }
    }

    /// <summary>
    /// Decide what to do when we have the puck - shoot, pass, or carry.
    /// </summary>
    private void DecideAttackState()
    {
        float distanceToGoal = Vector3.Distance(transform.position, opponentGoal.position);

        // In shooting range - take the shot
        if (distanceToGoal < shootingRange)
        {
            currentState = AIState.AttackWithPuck;
            return;
        }

        // Consider passing to open teammate
        if (Random.value < passConsiderationChance)
        {
            HockeyPlayer openTeammate = FindBestPassTarget();
            if (openTeammate != null)
            {
                targetTeammate = openTeammate;
                currentState = AIState.SetupPlay;
                return;
            }
        }

        // No good pass option - carry the puck forward
        currentState = AIState.AttackWithPuck;
    }

    /// <summary>
    /// Decide what to do when teammate has puck - support or position for pass.
    /// </summary>
    private void DecideSupportState()
    {
        if (puckCarrier == null)
        {
            currentState = AIState.ReturnToPosition;
            return;
        }

        float distanceToPuckCarrier = Vector3.Distance(transform.position, puckCarrier.transform.position);

        // Forwards: move to open ice for pass
        if (isForward)
        {
            // If close to puck carrier, support them
            if (distanceToPuckCarrier < supportDistance)
            {
                currentState = AIState.SupportTeammate;
            }
            else
            {
                // Position for potential pass
                currentState = AIState.PositionForPass;
            }
        }
        // Defensemen: stay back, don't get pulled forward
        else if (isDefenseman)
        {
            float myOffensivePosition = GetOffensivePosition(transform.position);

            // If too far forward, fall back
            if (myOffensivePosition > defensemanMaxForwardPosition)
            {
                currentState = AIState.ReturnToPosition;
            }
            else
            {
                // Stay in defensive zone, support from behind
                currentState = AIState.SupportTeammate;
            }
        }
        else
        {
            currentState = AIState.SupportTeammate;
        }
    }

    /// <summary>
    /// Decide what to do when opponent has puck or it's loose.
    /// </summary>
    private void DecideDefenseState()
    {
        bool puckIsLoose = targetPuck.IsLoose;
        Vector3 puckPosition = targetPuck.transform.position;
        float distanceToPuck = Vector3.Distance(transform.position, puckPosition);
        float puckDistanceToOurGoal = ownGoal != null
            ? Vector3.Distance(puckPosition, ownGoal.position)
            : float.MaxValue;

        // Puck is loose - everyone tries to get it
        if (puckIsLoose)
        {
            // Defensemen only chase if puck is in defensive zone or close by
            if (isDefenseman)
            {
                if (puckDistanceToOurGoal < defendDistance || distanceToPuck < 5f)
                {
                    currentState = AIState.ChasePuck;
                }
                else
                {
                    currentState = AIState.ReturnToPosition;
                }
            }
            else
            {
                // Forwards chase loose pucks more aggressively
                currentState = AIState.ChasePuck;
            }
            return;
        }

        // Opponent has puck
        float myOffensivePosition = GetOffensivePosition(transform.position);
        float puckOffensivePosition = GetOffensivePosition(puckPosition);

        // Defensemen: protect the net, clear the zone
        if (isDefenseman)
        {
            if (puckDistanceToOurGoal < defendDistance)
            {
                // Puck is dangerous - defend hard or clear zone
                if (distanceToPuck < 4f)
                {
                    currentState = AIState.ClearZone;
                }
                else
                {
                    currentState = AIState.DefendGoal;
                }
            }
            else
            {
                // Return to defensive position
                currentState = AIState.ReturnToPosition;
            }
        }
        // Forwards: pressure the puck carrier
        else if (isForward)
        {
            // Forecheck if puck is in offensive zone
            if (puckOffensivePosition > 0)
            {
                currentState = AIState.ChasePuck;
            }
            // If puck is in our zone, help defend
            else if (puckDistanceToOurGoal < defendDistance)
            {
                currentState = AIState.DefendGoal;
            }
            else
            {
                // Return to position
                currentState = AIState.ReturnToPosition;
            }
        }
        else
        {
            // Default behavior
            currentState = AIState.ChasePuck;
        }
    }

    #endregion

    #region State Execution

    /// <summary>
    /// Execute behavior for current state.
    /// </summary>
    private void ExecuteState()
    {
        switch (currentState)
        {
            case AIState.Idle:
                ExecuteIdle();
                break;
            case AIState.ChasePuck:
                ExecuteChasePuck();
                break;
            case AIState.AttackWithPuck:
                ExecuteAttack();
                break;
            case AIState.DefendGoal:
                ExecuteDefend();
                break;
            case AIState.ReturnToPosition:
                ExecuteReturn();
                break;
            case AIState.SupportTeammate:
                ExecuteSupport();
                break;
            case AIState.PositionForPass:
                ExecutePositionForPass();
                break;
            case AIState.ClearZone:
                ExecuteClearZone();
                break;
            case AIState.SetupPlay:
                ExecuteSetupPlay();
                break;
        }
    }

    private void ExecuteIdle()
    {
        player.SetMoveInput(Vector2.zero);
    }

    private void ExecuteChasePuck()
    {
        if (targetPuck == null) return;

        // Move toward puck
        Vector3 toPuck = targetPuck.transform.position - transform.position;
        toPuck.y = 0;

        Vector2 moveInput = new Vector2(toPuck.x, toPuck.z).normalized * chaseSpeed;
        player.SetMoveInput(moveInput);

        // Dash if puck is far and we're a forward
        if (toPuck.magnitude > 5f && isForward)
        {
            player.TriggerDash();
        }
    }

    private void ExecuteAttack()
    {
        if (opponentGoal == null)
        {
            player.SetMoveInput(Vector2.zero);
            return;
        }

        Vector3 toGoal = opponentGoal.position - transform.position;
        toGoal.y = 0;
        float distanceToGoal = toGoal.magnitude;

        // If in shooting range, take a shot
        if (distanceToGoal < shootingRange && shootingController != null)
        {
            // Aim at goal with some inaccuracy
            Vector3 aimPoint = opponentGoal.position;
            aimPoint += Random.insideUnitSphere * (1f - shootAccuracy) * 3f;
            aimPoint.y = opponentGoal.position.y;

            Vector3 aimDirection = (aimPoint - transform.position).normalized;
            shootingController.SetAimDirection(new Vector2(aimDirection.x, aimDirection.z));

            // Charge and shoot
            if (!isChargingShot)
            {
                isChargingShot = true;
                shootChargeTimer = 0f;
                shootingController.StartCharge();
            }
            else
            {
                shootChargeTimer += Time.deltaTime;

                // Release shot after charging
                float targetChargeTime = Random.Range(0.3f, 0.8f);
                if (shootChargeTimer >= targetChargeTime)
                {
                    shootingController.ReleaseShot();
                    isChargingShot = false;
                }
            }

            // Slow down while shooting
            player.SetMoveInput(Vector2.zero);
        }
        else
        {
            // Skate toward goal
            Vector2 moveInput = new Vector2(toGoal.x, toGoal.z).normalized * chaseSpeed;
            player.SetMoveInput(moveInput);
            isChargingShot = false;
        }
    }

    private void ExecuteDefend()
    {
        if (ownGoal == null || targetPuck == null) return;

        Vector3 puckPos = targetPuck.transform.position;
        Vector3 goalPos = ownGoal.position;

        // Position between puck and goal
        float interpolation = isDefenseman ? 0.4f : 0.3f;
        Vector3 defendPoint = Vector3.Lerp(goalPos, puckPos, interpolation);
        defendPoint.y = transform.position.y;

        // Add positional offset based on player position
        if (player.Position == PlayerPosition.LeftDefense)
        {
            defendPoint += Vector3.forward * -3f;
        }
        else if (player.Position == PlayerPosition.RightDefense)
        {
            defendPoint += Vector3.forward * 3f;
        }

        Vector3 toDefendPoint = defendPoint - transform.position;
        toDefendPoint.y = 0;

        if (toDefendPoint.magnitude > 1f)
        {
            Vector2 moveInput = new Vector2(toDefendPoint.x, toDefendPoint.z).normalized * chaseSpeed;
            player.SetMoveInput(moveInput);
        }
        else
        {
            player.SetMoveInput(Vector2.zero);
        }
    }

    private void ExecuteReturn()
    {
        Vector3 targetPos = GetHomePosition();
        Vector3 toHome = targetPos - transform.position;
        toHome.y = 0;

        if (toHome.magnitude > positionTolerance)
        {
            Vector2 moveInput = new Vector2(toHome.x, toHome.z).normalized * (chaseSpeed * 0.8f);
            player.SetMoveInput(moveInput);
        }
        else
        {
            player.SetMoveInput(Vector2.zero);
        }
    }

    /// <summary>
    /// Get the home position for this AI. Uses Vector3 if set, Transform if available, or calculates default.
    /// </summary>
    private Vector3 GetHomePosition()
    {
        // Priority 1: Directly set Vector3 position (from TeamManager)
        if (hasHomePosition)
        {
            return homePositionPoint;
        }

        // Priority 2: Transform reference (from Inspector)
        if (homePositionTransform != null)
        {
            return homePositionTransform.position;
        }

        // Priority 3: Calculate default position based on player role and goal position
        if (ownGoal != null)
        {
            Vector3 basePos = ownGoal.position;
            float forwardOffset = 10f; // Distance from goal

            switch (player.Position)
            {
                case PlayerPosition.Goalie:
                    return basePos; // Goalie stays at goal
                case PlayerPosition.LeftDefense:
                    return basePos + Vector3.right * forwardOffset + Vector3.forward * -4f;
                case PlayerPosition.RightDefense:
                    return basePos + Vector3.right * forwardOffset + Vector3.forward * 4f;
                case PlayerPosition.Center:
                    return basePos + Vector3.right * (forwardOffset * 2f);
                case PlayerPosition.LeftWing:
                    return basePos + Vector3.right * (forwardOffset * 2f) + Vector3.forward * -8f;
                case PlayerPosition.RightWing:
                    return basePos + Vector3.right * (forwardOffset * 2f) + Vector3.forward * 8f;
            }
        }

        // Last resort: center ice
        return Vector3.zero;
    }

    private void ExecuteSupport()
    {
        if (puckCarrier == null)
        {
            ExecuteReturn();
            return;
        }

        // Position to support puck carrier
        Vector3 puckCarrierPos = puckCarrier.transform.position;
        Vector3 supportOffset = GetSupportPosition(puckCarrierPos);
        Vector3 toSupport = supportOffset - transform.position;
        toSupport.y = 0;

        if (toSupport.magnitude > 2f)
        {
            Vector2 moveInput = new Vector2(toSupport.x, toSupport.z).normalized * chaseSpeed;
            player.SetMoveInput(moveInput);
        }
        else
        {
            player.SetMoveInput(Vector2.zero);
        }
    }

    private void ExecutePositionForPass()
    {
        if (puckCarrier == null)
        {
            ExecuteReturn();
            return;
        }

        // Find open ice position ahead of puck carrier
        Vector3 puckCarrierPos = puckCarrier.transform.position;
        Vector3 directionToGoal = (opponentGoal.position - puckCarrierPos).normalized;

        // Position ahead and to the side
        float sideOffset = (player.Position == PlayerPosition.LeftWing) ? -6f :
                           (player.Position == PlayerPosition.RightWing) ? 6f : 0f;

        Vector3 openIceTarget = puckCarrierPos + directionToGoal * 8f + Vector3.forward * sideOffset;
        openIceTarget.y = transform.position.y;

        Vector3 toTarget = openIceTarget - transform.position;
        toTarget.y = 0;

        if (toTarget.magnitude > 2f)
        {
            Vector2 moveInput = new Vector2(toTarget.x, toTarget.z).normalized * chaseSpeed;
            player.SetMoveInput(moveInput);
        }
        else
        {
            player.SetMoveInput(Vector2.zero);
        }
    }

    private void ExecuteClearZone()
    {
        if (targetPuck == null) return;

        // Chase puck aggressively to clear it
        Vector3 toPuck = targetPuck.transform.position - transform.position;
        toPuck.y = 0;

        Vector2 moveInput = new Vector2(toPuck.x, toPuck.z).normalized * chaseSpeed;
        player.SetMoveInput(moveInput);

        // Use body checks if close
        if (toPuck.magnitude < 3f)
        {
            player.TriggerDash();
        }
    }

    private void ExecuteSetupPlay()
    {
        if (targetTeammate == null || !player.HasPuck)
        {
            ExecuteAttack();
            return;
        }

        // Pass to the open teammate
        if (targetPuck != null)
        {
            float passSpeed = Vector3.Distance(transform.position, targetTeammate.transform.position) * 2f;
            passSpeed = Mathf.Clamp(passSpeed, 10f, 30f);

            targetPuck.PassToPlayer(targetTeammate, passSpeed);
            targetTeammate = null;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Find the best teammate to pass to - open and in good position.
    /// </summary>
    private HockeyPlayer FindBestPassTarget()
    {
        if (teammates.Count == 0) return null;

        HockeyPlayer bestTarget = null;
        float bestScore = float.MinValue;

        Vector3 toGoal = opponentGoal.position - transform.position;
        toGoal.y = 0;
        toGoal.Normalize();

        foreach (var teammate in teammates)
        {
            if (teammate == null || !teammate.gameObject.activeInHierarchy) continue;
            if (teammate.Position == PlayerPosition.Goalie) continue;

            float distance = Vector3.Distance(transform.position, teammate.transform.position);

            // Skip if too close or too far
            if (distance < 3f || distance > passRange) continue;

            // Check if teammate is ahead of us
            Vector3 toTeammate = teammate.transform.position - transform.position;
            toTeammate.y = 0;
            float forwardDot = Vector3.Dot(toTeammate.normalized, toGoal);

            // Prefer teammates ahead of us
            if (forwardDot < 0.3f) continue;

            // Check if path is clear
            if (!IsPassLaneClear(teammate.transform.position)) continue;

            // Score this teammate
            float score = 0f;
            score += forwardDot * 10f; // Prefer forward passes
            score += (passRange - distance) * 0.5f; // Prefer moderate distance

            // Bonus for teammates closer to goal
            float teammateDistToGoal = Vector3.Distance(teammate.transform.position, opponentGoal.position);
            float myDistToGoal = Vector3.Distance(transform.position, opponentGoal.position);
            if (teammateDistToGoal < myDistToGoal)
            {
                score += 5f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = teammate;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Check if there's a clear lane to pass to target position.
    /// </summary>
    private bool IsPassLaneClear(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        float distance = direction.magnitude;
        direction.Normalize();

        // Raycast to check for blocking opponents
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out hit, distance))
        {
            // Check if it's an opponent
            HockeyPlayer hitPlayer = hit.collider.GetComponent<HockeyPlayer>();
            if (hitPlayer != null && hitPlayer.TeamId != player.TeamId)
            {
                return false; // Opponent in the way
            }
        }

        return true;
    }

    /// <summary>
    /// Get a support position relative to the puck carrier.
    /// </summary>
    private Vector3 GetSupportPosition(Vector3 puckCarrierPosition)
    {
        Vector3 supportPos = puckCarrierPosition;

        // Defensemen support from behind
        if (isDefenseman)
        {
            Vector3 toGoal = ownGoal.position - puckCarrierPosition;
            toGoal.y = 0;
            supportPos += toGoal.normalized * defensemanFallbackDistance;

            // Add side offset
            if (player.Position == PlayerPosition.LeftDefense)
            {
                supportPos += Vector3.forward * -3f;
            }
            else if (player.Position == PlayerPosition.RightDefense)
            {
                supportPos += Vector3.forward * 3f;
            }
        }
        // Forwards support from sides
        else if (isForward)
        {
            Vector3 toOpponentGoal = opponentGoal.position - puckCarrierPosition;
            toOpponentGoal.y = 0;

            if (player.Position == PlayerPosition.LeftWing)
            {
                supportPos += Vector3.forward * -5f;
            }
            else if (player.Position == PlayerPosition.RightWing)
            {
                supportPos += Vector3.forward * 5f;
            }
            else
            {
                supportPos += toOpponentGoal.normalized * 3f;
            }
        }

        return supportPos;
    }

    /// <summary>
    /// Get offensive position (positive = offensive zone, negative = defensive zone).
    /// </summary>
    private float GetOffensivePosition(Vector3 position)
    {
        if (ownGoal == null || opponentGoal == null) return 0f;

        Vector3 ownGoalPos = ownGoal.position;
        Vector3 oppGoalPos = opponentGoal.position;

        // Center ice is 0, own goal is negative, opponent goal is positive
        Vector3 toOppGoal = oppGoalPos - ownGoalPos;
        Vector3 toPosition = position - ownGoalPos;

        float projection = Vector3.Dot(toPosition, toOppGoal.normalized);
        float rinkLength = toOppGoal.magnitude;

        // Normalize to -1 to 1, then scale
        return (projection / rinkLength - 0.5f) * rinkLength;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Set difficulty (0-1 scale).
    /// </summary>
    public void SetDifficulty(float difficulty)
    {
        difficulty = Mathf.Clamp01(difficulty);

        reactionTime = Mathf.Lerp(0.5f, 0.1f, difficulty);
        chaseSpeed = Mathf.Lerp(0.6f, 1f, difficulty);
        shootAccuracy = Mathf.Lerp(0.5f, 0.95f, difficulty);
        passConsiderationChance = Mathf.Lerp(0.3f, 0.85f, difficulty);
    }

    /// <summary>
    /// Set goal references.
    /// </summary>
    public void SetGoals(Transform attackGoal, Transform defendGoal)
    {
        opponentGoal = attackGoal;
        ownGoal = defendGoal;
    }

    /// <summary>
    /// Set home position for this AI using a Transform reference.
    /// </summary>
    public void SetHomePosition(Transform position)
    {
        homePositionTransform = position;
        if (position != null)
        {
            homePositionPoint = position.position;
            hasHomePosition = true;
        }
    }

    /// <summary>
    /// Set home position for this AI using a Vector3 (called by TeamManager).
    /// </summary>
    public void SetHomePosition(Vector3 position)
    {
        homePositionPoint = position;
        hasHomePosition = true;
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw state indicator
        Gizmos.color = currentState switch
        {
            AIState.Idle => Color.gray,
            AIState.ChasePuck => Color.yellow,
            AIState.AttackWithPuck => Color.red,
            AIState.DefendGoal => Color.blue,
            AIState.ReturnToPosition => Color.green,
            AIState.SupportTeammate => Color.cyan,
            AIState.PositionForPass => Color.magenta,
            AIState.ClearZone => new Color(1f, 0.5f, 0f),
            AIState.SetupPlay => new Color(0.5f, 0f, 1f),
            _ => Color.white
        };

        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);

        // Draw position role indicator
        if (player != null)
        {
            Gizmos.color = isForward ? Color.red : isDefenseman ? Color.blue : Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.2f);
        }

        // Draw shooting range when attacking
        if (currentState == AIState.AttackWithPuck)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, shootingRange);
        }

        // Draw line to target
        if (opponentGoal != null && currentState == AIState.AttackWithPuck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, opponentGoal.position);
        }

        if (targetPuck != null && currentState == AIState.ChasePuck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPuck.transform.position);
        }

        // Draw home position
        if (hasHomePosition || homePositionTransform != null)
        {
            Vector3 homePos = GetHomePosition();
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawLine(transform.position, homePos);
            Gizmos.DrawWireSphere(homePos, 0.5f);
        }

        // Draw pass target
        if (targetTeammate != null && currentState == AIState.SetupPlay)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, targetTeammate.transform.position);
            Gizmos.DrawWireSphere(targetTeammate.transform.position, 0.8f);
        }

        // Draw pass range when considering pass
        if (player != null && player.HasPuck && currentState == AIState.SetupPlay)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, passRange);
        }

        // Draw support position
        if (currentState == AIState.SupportTeammate && puckCarrier != null)
        {
            Vector3 supportPos = GetSupportPosition(puckCarrier.transform.position);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, supportPos);
            Gizmos.DrawWireSphere(supportPos, 0.6f);
        }
    }

    #endregion
}
