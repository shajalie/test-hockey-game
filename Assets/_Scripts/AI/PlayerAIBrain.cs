using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI brain for a hockey player.
/// Handles decision-making, positioning, and actions.
/// Works with PlayerInputController to execute decisions.
/// </summary>
[RequireComponent(typeof(PlayerInputController))]
public class PlayerAIBrain : MonoBehaviour
{
    #region Enums

    public enum AIState
    {
        Idle,
        ChasePuck,
        AttackWithPuck,
        SupportAttack,
        DefendZone,
        ReturnToPosition,
        Forecheck,
        Backcheck
    }

    #endregion

    #region Serialized Fields

    [Header("AI Settings")]
    [SerializeField] private float decisionInterval = 0.15f;
    [SerializeField] private float reactionTime = 0.1f;

    [Header("Awareness")]
    [SerializeField] private float awarenessRadius = 30f;
    [SerializeField] private float passLaneCheckInterval = 0.3f;

    [Header("Positioning")]
    [SerializeField] private float positionTolerance = 1.5f;
    [SerializeField] private float supportDistance = 8f;
    [SerializeField] private float defenseDepth = 15f;

    [Header("Attack")]
    [SerializeField] private float shootingRange = 15f;
    [SerializeField] private float passPreference = 0.6f; // 0 = always shoot, 1 = always pass

    [Header("Defense")]
    [SerializeField] private float pokeCheckRange = 2.5f;
    [SerializeField] private float interceptDistance = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    #endregion

    #region Private Fields

    private PlayerInputController inputController;
    private HockeyPlayer hockeyPlayer;
    private IcePhysicsController physics;

    private AIState currentState = AIState.Idle;
    private float decisionTimer;
    private float passLaneTimer;

    // Cached references
    private PuckController puck;
    private TeamController myTeam;
    private List<HockeyPlayer> teammates = new List<HockeyPlayer>();
    private List<HockeyPlayer> opponents = new List<HockeyPlayer>();

    // Target tracking
    private Vector3 targetPosition;
    private GameObject targetPlayer;
    private bool hasValidPassLane;

    // Role-based behavior
    private bool isDefenseman;
    private bool isForward;
    private Vector3 homePosition;

    #endregion

    #region Properties

    /// <summary>Current AI state.</summary>
    public AIState State => currentState;

    /// <summary>Whether AI is active.</summary>
    public bool IsActive => inputController != null && inputController.IsAIControlled;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        inputController = GetComponent<PlayerInputController>();
        hockeyPlayer = GetComponent<HockeyPlayer>();
        physics = GetComponent<IcePhysicsController>();
    }

    private void Start()
    {
        // Cache references
        puck = FindObjectOfType<PuckController>();
        myTeam = TeamController.Instance;

        // Determine role
        if (hockeyPlayer != null)
        {
            isDefenseman = hockeyPlayer.Position == PlayerPosition.LeftDefense ||
                          hockeyPlayer.Position == PlayerPosition.RightDefense;
            isForward = hockeyPlayer.Position == PlayerPosition.Center ||
                       hockeyPlayer.Position == PlayerPosition.LeftWing ||
                       hockeyPlayer.Position == PlayerPosition.RightWing;

            // Set home position based on role
            homePosition = GetDefaultHomePosition();
        }

        // Subscribe to events
        PuckController.OnPossessionChanged += OnPuckPossessionChanged;
    }

    private void OnDestroy()
    {
        PuckController.OnPossessionChanged -= OnPuckPossessionChanged;
    }

    private void Update()
    {
        if (!IsActive) return;

        UpdateTimers();
        UpdateAwareness();

        if (decisionTimer <= 0)
        {
            MakeDecision();
            decisionTimer = decisionInterval;
        }

        ExecuteState();
    }

    #endregion

    #region Decision Making

    private void UpdateTimers()
    {
        decisionTimer -= Time.deltaTime;
        passLaneTimer -= Time.deltaTime;
    }

    private void MakeDecision()
    {
        if (puck == null) return;

        // Determine new state based on game situation
        AIState newState = DetermineState();

        if (newState != currentState)
        {
            OnStateExit(currentState);
            currentState = newState;
            OnStateEnter(currentState);
        }
    }

    private AIState DetermineState()
    {
        // Do I have the puck?
        if (HasPuck())
        {
            return AIState.AttackWithPuck;
        }

        // Does my team have the puck?
        if (TeamHasPuck())
        {
            return DetermineOffensiveState();
        }
        else
        {
            return DetermineDefensiveState();
        }
    }

    private AIState DetermineOffensiveState()
    {
        // Am I closest to puck carrier?
        HockeyPlayer carrier = GetPuckCarrier();
        if (carrier != null)
        {
            float myDist = Vector3.Distance(transform.position, carrier.transform.position);

            // Support the attack
            if (myDist < supportDistance * 2f)
            {
                return AIState.SupportAttack;
            }
        }

        // Default to positioning
        return AIState.ReturnToPosition;
    }

    private AIState DetermineDefensiveState()
    {
        float distToPuck = Vector3.Distance(transform.position, puck.transform.position);

        // Am I closest to the puck?
        if (AmClosestToPuck())
        {
            return AIState.ChasePuck;
        }

        // Puck is in our zone?
        if (IsPuckInDefensiveZone())
        {
            if (distToPuck < interceptDistance * 2f)
            {
                return AIState.DefendZone;
            }
        }

        // Puck moving toward our zone?
        if (IsPuckMovingTowardUs())
        {
            return AIState.Backcheck;
        }

        // Otherwise forecheck or return
        if (isForward && !IsPuckInDefensiveZone())
        {
            return AIState.Forecheck;
        }

        return AIState.ReturnToPosition;
    }

    #endregion

    #region State Execution

    private void OnStateEnter(AIState state)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAIBrain] {gameObject.name} entering state: {state}");
        }
    }

    private void OnStateExit(AIState state)
    {
        // Clean up state-specific data
    }

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
                ExecuteAttackWithPuck();
                break;
            case AIState.SupportAttack:
                ExecuteSupportAttack();
                break;
            case AIState.DefendZone:
                ExecuteDefendZone();
                break;
            case AIState.ReturnToPosition:
                ExecuteReturnToPosition();
                break;
            case AIState.Forecheck:
                ExecuteForecheck();
                break;
            case AIState.Backcheck:
                ExecuteBackcheck();
                break;
        }
    }

    private void ExecuteIdle()
    {
        // Stand still
        inputController.SetAIMoveInput(Vector2.zero);
    }

    private void ExecuteChasePuck()
    {
        if (puck == null) return;

        // Move toward puck with lead
        Vector3 puckPos = puck.transform.position;
        Vector3 puckVel = puck.Velocity;

        // Lead the puck
        Vector3 targetPos = puckPos + puckVel * 0.3f;
        MoveToward(targetPos);

        // Attempt poke check when close
        float distToPuck = Vector3.Distance(transform.position, puckPos);
        if (distToPuck < pokeCheckRange && puck.IsPossessed)
        {
            inputController.AIPokeCheck();
        }
    }

    private void ExecuteAttackWithPuck()
    {
        if (puck == null) return;

        // Get goal position
        Vector3 goalPos = GetOpponentGoalPosition();
        float distToGoal = Vector3.Distance(transform.position, goalPos);

        // Should I shoot?
        if (distToGoal < shootingRange && HasClearLaneToGoal())
        {
            // Shoot!
            Vector3 shootDir = (goalPos - transform.position).normalized;
            float power = Mathf.Lerp(25f, 40f, 1f - (distToGoal / shootingRange));

            inputController.AIShoot(shootDir, power);
            return;
        }

        // Should I pass?
        if (passLaneTimer <= 0)
        {
            passLaneTimer = passLaneCheckInterval;
            CheckPassLanes();
        }

        if (hasValidPassLane && Random.value < passPreference && targetPlayer != null)
        {
            inputController.AIPass(targetPlayer);
            return;
        }

        // Skate toward goal
        MoveToward(goalPos);

        // Set aim toward goal
        Vector3 aimDir = (goalPos - transform.position).normalized;
        inputController.SetAIAimInput(new Vector2(aimDir.x, aimDir.z));
    }

    private void ExecuteSupportAttack()
    {
        HockeyPlayer carrier = GetPuckCarrier();
        if (carrier == null)
        {
            ExecuteReturnToPosition();
            return;
        }

        // Position for a pass
        Vector3 supportPos = CalculateSupportPosition(carrier);
        MoveToward(supportPos);

        // Face the puck
        Vector3 toPuck = (puck.transform.position - transform.position).normalized;
        inputController.SetAIAimInput(new Vector2(toPuck.x, toPuck.z));
    }

    private void ExecuteDefendZone()
    {
        if (puck == null) return;

        // Position between puck and goal
        Vector3 goalPos = GetOwnGoalPosition();
        Vector3 puckPos = puck.transform.position;

        Vector3 defendPos = Vector3.Lerp(goalPos, puckPos, 0.3f);

        // Adjust based on role
        if (isDefenseman)
        {
            // Stay closer to goal
            defendPos = Vector3.Lerp(goalPos, puckPos, 0.2f);
        }

        MoveToward(defendPos);

        // Face the puck
        Vector3 toPuck = (puckPos - transform.position).normalized;
        inputController.SetAIAimInput(new Vector2(toPuck.x, toPuck.z));
    }

    private void ExecuteReturnToPosition()
    {
        // Get formation position
        Vector3 formationPos = myTeam != null
            ? myTeam.GetFormationPosition(inputController, puck?.transform.position ?? Vector3.zero)
            : homePosition;

        MoveToward(formationPos);
    }

    private void ExecuteForecheck()
    {
        if (puck == null) return;

        // Aggressive pursuit
        Vector3 puckPos = puck.transform.position;
        MoveToward(puckPos);

        // Dash when close
        float distToPuck = Vector3.Distance(transform.position, puckPos);
        if (distToPuck < 5f && physics != null && physics.CanDash)
        {
            inputController.AITriggerDash();
        }
    }

    private void ExecuteBackcheck()
    {
        // Skate back toward defensive zone
        Vector3 goalPos = GetOwnGoalPosition();
        Vector3 targetPos = Vector3.Lerp(transform.position, goalPos, 0.5f);

        MoveToward(targetPos);
    }

    #endregion

    #region Movement

    private void MoveToward(Vector3 targetPos)
    {
        targetPosition = targetPos;

        Vector3 direction = targetPos - transform.position;
        direction.y = 0;

        float distance = direction.magnitude;

        if (distance < positionTolerance)
        {
            inputController.SetAIMoveInput(Vector2.zero);
            return;
        }

        direction.Normalize();
        inputController.SetAIMoveInput(new Vector2(direction.x, direction.z));
    }

    #endregion

    #region Awareness

    private void UpdateAwareness()
    {
        // Update teammate/opponent lists periodically
        teammates.Clear();
        opponents.Clear();

        HockeyPlayer[] allPlayers = FindObjectsOfType<HockeyPlayer>();
        int myTeamId = hockeyPlayer?.TeamId ?? 0;

        foreach (var player in allPlayers)
        {
            if (player.gameObject == gameObject) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > awarenessRadius) continue;

            if (player.TeamId == myTeamId)
            {
                teammates.Add(player);
            }
            else
            {
                opponents.Add(player);
            }
        }
    }

    private bool HasPuck()
    {
        return puck != null && puck.Owner == gameObject;
    }

    private bool TeamHasPuck()
    {
        if (puck == null || !puck.IsPossessed) return false;

        HockeyPlayer owner = puck.Owner?.GetComponent<HockeyPlayer>();
        return owner != null && owner.TeamId == hockeyPlayer?.TeamId;
    }

    private HockeyPlayer GetPuckCarrier()
    {
        if (puck == null || !puck.IsPossessed) return null;
        return puck.Owner?.GetComponent<HockeyPlayer>();
    }

    private bool AmClosestToPuck()
    {
        if (puck == null) return false;

        float myDist = Vector3.Distance(transform.position, puck.transform.position);

        foreach (var teammate in teammates)
        {
            float theirDist = Vector3.Distance(teammate.transform.position, puck.transform.position);
            if (theirDist < myDist) return false;
        }

        return true;
    }

    private bool IsPuckInDefensiveZone()
    {
        if (puck == null) return false;

        int myTeamId = hockeyPlayer?.TeamId ?? 0;
        float puckZ = puck.transform.position.z;

        return myTeamId == 0 ? puckZ < -10f : puckZ > 10f;
    }

    private bool IsPuckMovingTowardUs()
    {
        if (puck == null) return false;

        int myTeamId = hockeyPlayer?.TeamId ?? 0;
        float puckVelZ = puck.Velocity.z;

        return myTeamId == 0 ? puckVelZ < -2f : puckVelZ > 2f;
    }

    private bool HasClearLaneToGoal()
    {
        Vector3 goalPos = GetOpponentGoalPosition();
        Vector3 direction = goalPos - transform.position;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction.normalized, out hit, direction.magnitude))
        {
            HockeyPlayer hitPlayer = hit.collider.GetComponent<HockeyPlayer>();
            if (hitPlayer != null && hitPlayer.TeamId != hockeyPlayer?.TeamId)
            {
                return false; // Blocked by opponent
            }
        }

        return true;
    }

    private void CheckPassLanes()
    {
        hasValidPassLane = false;
        targetPlayer = null;

        float bestScore = 0f;

        foreach (var teammate in teammates)
        {
            // Check if pass lane is clear
            Vector3 direction = teammate.transform.position - transform.position;
            float distance = direction.magnitude;

            if (distance < 3f || distance > 25f) continue;

            RaycastHit hit;
            bool blocked = false;

            if (Physics.Raycast(transform.position, direction.normalized, out hit, distance))
            {
                HockeyPlayer hitPlayer = hit.collider.GetComponent<HockeyPlayer>();
                if (hitPlayer != null && hitPlayer.TeamId != hockeyPlayer?.TeamId)
                {
                    blocked = true;
                }
            }

            if (blocked) continue;

            // Score this pass option
            float score = 100f - distance;

            // Bonus if teammate is closer to goal
            float theirDistToGoal = Vector3.Distance(teammate.transform.position, GetOpponentGoalPosition());
            float myDistToGoal = Vector3.Distance(transform.position, GetOpponentGoalPosition());
            if (theirDistToGoal < myDistToGoal)
            {
                score += 30f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                targetPlayer = teammate.gameObject;
                hasValidPassLane = true;
            }
        }
    }

    #endregion

    #region Positioning

    private Vector3 CalculateSupportPosition(HockeyPlayer carrier)
    {
        Vector3 carrierPos = carrier.transform.position;
        Vector3 goalDir = (GetOpponentGoalPosition() - carrierPos).normalized;

        // Position to the side and slightly ahead
        Vector3 sideOffset = Vector3.Cross(Vector3.up, goalDir) *
            (hockeyPlayer?.Position == PlayerPosition.LeftWing ? -1f : 1f) * supportDistance;

        return carrierPos + goalDir * 5f + sideOffset;
    }

    private Vector3 GetDefaultHomePosition()
    {
        if (hockeyPlayer == null) return Vector3.zero;

        int teamId = hockeyPlayer.TeamId;
        float zBase = teamId == 0 ? -15f : 15f;

        switch (hockeyPlayer.Position)
        {
            case PlayerPosition.Center:
                return new Vector3(0, 0, zBase * 0.5f);
            case PlayerPosition.LeftWing:
                return new Vector3(-8f, 0, zBase * 0.3f);
            case PlayerPosition.RightWing:
                return new Vector3(8f, 0, zBase * 0.3f);
            case PlayerPosition.LeftDefense:
                return new Vector3(-6f, 0, zBase);
            case PlayerPosition.RightDefense:
                return new Vector3(6f, 0, zBase);
            default:
                return Vector3.zero;
        }
    }

    private Vector3 GetOpponentGoalPosition()
    {
        int myTeamId = hockeyPlayer?.TeamId ?? 0;
        return new Vector3(0, 0, myTeamId == 0 ? 26f : -26f);
    }

    private Vector3 GetOwnGoalPosition()
    {
        int myTeamId = hockeyPlayer?.TeamId ?? 0;
        return new Vector3(0, 0, myTeamId == 0 ? -26f : 26f);
    }

    #endregion

    #region Event Handlers

    private void OnPuckPossessionChanged(PuckController puck, GameObject newOwner)
    {
        // Force immediate re-evaluation
        decisionTimer = 0;
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        // Current state
        Gizmos.color = GetStateColor();
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.2f, 0.3f);

        // Target position
        if (targetPosition != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
        }

        // Target player
        if (targetPlayer != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, targetPlayer.transform.position);
        }
    }

    private Color GetStateColor()
    {
        switch (currentState)
        {
            case AIState.ChasePuck: return Color.red;
            case AIState.AttackWithPuck: return Color.green;
            case AIState.SupportAttack: return Color.yellow;
            case AIState.DefendZone: return Color.blue;
            case AIState.Forecheck: return new Color(1f, 0.5f, 0f);
            case AIState.Backcheck: return Color.cyan;
            default: return Color.gray;
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        Vector3 screenPos = Camera.main?.WorldToScreenPoint(transform.position + Vector3.up * 2.5f) ?? Vector3.zero;
        if (screenPos.z > 0)
        {
            GUI.Label(new Rect(screenPos.x - 40, Screen.height - screenPos.y, 80, 20), currentState.ToString());
        }
    }

    #endregion
}
