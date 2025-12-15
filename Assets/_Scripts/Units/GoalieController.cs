using UnityEngine;

/// <summary>
/// AI-controlled goalie system with realistic save mechanics.
/// Features multiple save animations, difficulty levels, and intelligent positioning.
/// Integrates with Puck and Goal systems for complete goalie behavior.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GoalieController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Goal defendingGoal;
    [SerializeField] private Transform puck;
    [SerializeField] private Transform creaseCenter;
    [SerializeField] private Animator animator;

    [Header("Crease Settings")]
    [SerializeField] private float creaseRadius = 2.5f;
    [SerializeField] private float creaseDepth = 1.5f;
    [SerializeField] private float optimalDistanceFromGoal = 1.2f;

    [Header("Movement")]
    [SerializeField] private float lateralSpeed = 8f;
    [SerializeField] private float forwardSpeed = 4f;
    [SerializeField] private float positioningSmoothing = 0.15f;

    [Header("Difficulty Settings")]
    [SerializeField] private DifficultyLevel difficulty = DifficultyLevel.Medium;
    [SerializeField] private float easyReactionTime = 0.4f;
    [SerializeField] private float mediumReactionTime = 0.25f;
    [SerializeField] private float hardReactionTime = 0.15f;
    [SerializeField] private float easyPositionError = 0.8f;
    [SerializeField] private float mediumPositionError = 0.4f;
    [SerializeField] private float hardPositionError = 0.15f;

    [Header("Save Settings")]
    [SerializeField] private float saveRange = 2.0f;
    [SerializeField] private float saveSpeed = 12f;
    [SerializeField] private float gloveReachDistance = 1.5f;
    [SerializeField] private float blockerReachDistance = 1.3f;
    [SerializeField] private float pokeCheckRange = 2.5f;
    [SerializeField] private float pokeCheckCooldown = 1.5f;

    [Header("Shot Anticipation")]
    [SerializeField] private float anticipationDistance = 15f;
    [SerializeField] private float shotAngleThreshold = 45f;
    [SerializeField] private float dangerZoneDistance = 8f;

    [Header("Puck Cover")]
    [SerializeField] private float coverPuckDistance = 0.5f;
    [SerializeField] private float coverPuckDelay = 0.3f;
    [SerializeField] private float whistleDelay = 1.5f;

    [Header("Team")]
    [SerializeField] private int teamId = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLogs = false;

    /// <summary>
    /// The team this goalie belongs to (0 or 1).
    /// </summary>
    public int TeamId => teamId;

    // Components
    private Rigidbody rb;
    private Puck puckScript;

    // State
    private GoalieState currentState = GoalieState.Standing;
    private SaveType lastSaveType = SaveType.None;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private float lastPokeCheckTime = -999f;
    private float reactionDelayTimer = 0f;
    private bool isReacting = false;
    private Vector3 anticipatedShotPosition;
    private bool hasCoveredPuck = false;
    private float coverPuckTimer = 0f;

    // Cached calculations
    private float currentReactionTime;
    private float currentPositionError;
    private float currentSavePercentage;

    public enum GoalieState
    {
        Standing,
        Tracking,
        Butterfly,
        GloveSave,
        BlockerSave,
        PokeCheck,
        CoveringPuck,
        PostSave
    }

    public enum SaveType
    {
        None,
        Glove,
        Blocker,
        Butterfly,
        Pad,
        PokeCheck
    }

    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Configure Rigidbody for goalie physics
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.mass = 90f; // Goalie with equipment is heavy

        // Auto-find references if not set
        if (creaseCenter == null)
        {
            creaseCenter = transform;
        }

        if (puck == null)
        {
            puck = GameObject.FindGameObjectWithTag("Puck")?.transform;
        }

        if (puck != null)
        {
            puckScript = puck.GetComponent<Puck>();
        }

        InitializeDifficultySettings();
    }

    private void OnEnable()
    {
        GameEvents.OnShotTaken += OnShotTaken;
        GameEvents.OnGoalScored += OnGoalScored;
    }

    private void OnDisable()
    {
        GameEvents.OnShotTaken -= OnShotTaken;
        GameEvents.OnGoalScored -= OnGoalScored;
    }

    private void Start()
    {
        targetPosition = creaseCenter.position;
    }

    private void Update()
    {
        if (puck == null) return;

        UpdateState();
        UpdateAnimations();

        // Handle reaction delay
        if (reactionDelayTimer > 0f)
        {
            reactionDelayTimer -= Time.deltaTime;
            if (reactionDelayTimer <= 0f)
            {
                isReacting = true;
            }
        }

        // Check for puck cover opportunity
        if (currentState != GoalieState.CoveringPuck && ShouldCoverPuck())
        {
            coverPuckTimer += Time.deltaTime;
            if (coverPuckTimer >= coverPuckDelay)
            {
                CoverPuck();
            }
        }
        else
        {
            coverPuckTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (puck == null) return;

        switch (currentState)
        {
            case GoalieState.Standing:
            case GoalieState.Tracking:
                UpdatePositioning();
                CheckForPokeCheck();
                break;

            case GoalieState.CoveringPuck:
                // Stay still on puck
                rb.linearVelocity = Vector3.zero;
                break;

            case GoalieState.PostSave:
                // Recover from save
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                break;
        }
    }

    /// <summary>
    /// Initialize difficulty-based parameters
    /// </summary>
    private void InitializeDifficultySettings()
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                currentReactionTime = easyReactionTime;
                currentPositionError = easyPositionError;
                currentSavePercentage = 0.65f;
                break;

            case DifficultyLevel.Medium:
                currentReactionTime = mediumReactionTime;
                currentPositionError = mediumPositionError;
                currentSavePercentage = 0.80f;
                break;

            case DifficultyLevel.Hard:
                currentReactionTime = hardReactionTime;
                currentPositionError = hardPositionError;
                currentSavePercentage = 0.92f;
                break;
        }
    }

    /// <summary>
    /// Update goalie state based on puck and game situation
    /// </summary>
    private void UpdateState()
    {
        if (hasCoveredPuck)
        {
            currentState = GoalieState.CoveringPuck;
            return;
        }

        float distanceToPuck = Vector3.Distance(transform.position, puck.position);
        bool puckInDangerZone = distanceToPuck < dangerZoneDistance && IsPuckThreatening();

        // Transition logic
        switch (currentState)
        {
            case GoalieState.Standing:
                if (puckInDangerZone)
                {
                    currentState = GoalieState.Tracking;
                }
                break;

            case GoalieState.Tracking:
                if (!puckInDangerZone)
                {
                    currentState = GoalieState.Standing;
                }
                break;

            case GoalieState.Butterfly:
            case GoalieState.GloveSave:
            case GoalieState.BlockerSave:
            case GoalieState.PokeCheck:
                // Transition to PostSave after animation
                Invoke(nameof(ReturnToStanding), 0.8f);
                currentState = GoalieState.PostSave;
                break;

            case GoalieState.PostSave:
                // Will transition back to Standing via Invoke
                break;
        }
    }

    /// <summary>
    /// Calculate optimal positioning to cut off shooting angles
    /// </summary>
    private void UpdatePositioning()
    {
        if (defendingGoal == null || puck == null)
        {
            targetPosition = creaseCenter.position;
            return;
        }

        Vector3 goalPosition = defendingGoal.transform.position;
        Vector3 puckPosition = puck.position;

        // Calculate angle from goal to puck
        Vector3 goalToPuck = (puckPosition - goalPosition).normalized;

        // Position goalie between puck and goal center
        Vector3 optimalPosition = goalPosition + goalToPuck * optimalDistanceFromGoal;

        // Apply positioning error based on difficulty
        if (currentPositionError > 0f)
        {
            float errorX = Mathf.PerlinNoise(Time.time * 0.5f, 0f) * currentPositionError * 2f - currentPositionError;
            optimalPosition += transform.right * errorX;
        }

        // Constrain to crease area
        optimalPosition = ConstrainToCrease(optimalPosition);

        // Smooth movement to target position
        targetPosition = Vector3.SmoothDamp(targetPosition, optimalPosition, ref currentVelocity, positioningSmoothing);

        // Apply movement
        Vector3 moveDirection = (targetPosition - transform.position);
        moveDirection.y = 0f;

        if (moveDirection.magnitude > 0.1f)
        {
            float speed = Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.z) ? lateralSpeed : forwardSpeed;
            Vector3 desiredVelocity = moveDirection.normalized * speed;

            // Use force-based movement for smooth physics
            Vector3 velocityDelta = desiredVelocity - new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(velocityDelta * 10f, ForceMode.Force);
        }
        else
        {
            // Stop when close to target
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        // Always face the puck
        Vector3 lookDirection = (puckPosition - transform.position);
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
    }

    /// <summary>
    /// Constrain position to stay within crease area
    /// </summary>
    private Vector3 ConstrainToCrease(Vector3 position)
    {
        if (creaseCenter == null) return position;

        Vector3 fromCenter = position - creaseCenter.position;
        fromCenter.y = 0f;

        // Circular crease constraint (lateral)
        if (fromCenter.magnitude > creaseRadius)
        {
            fromCenter = fromCenter.normalized * creaseRadius;
        }

        // Depth constraint (forward/back)
        Vector3 goalForward = defendingGoal != null ? defendingGoal.transform.forward : -transform.forward;
        float depthOffset = Vector3.Dot(fromCenter, goalForward);
        depthOffset = Mathf.Clamp(depthOffset, -creaseDepth, creaseDepth * 0.5f);

        Vector3 lateralOffset = fromCenter - goalForward * depthOffset;
        fromCenter = lateralOffset + goalForward * depthOffset;

        return creaseCenter.position + fromCenter;
    }

    /// <summary>
    /// Check if puck is threatening (heading toward goal)
    /// </summary>
    private bool IsPuckThreatening()
    {
        if (puckScript == null || defendingGoal == null) return false;

        Vector3 puckVelocity = puckScript.Velocity;

        // Check if puck is moving
        if (puckVelocity.magnitude < 1f) return false;

        // Check if puck is moving toward goal
        Vector3 toGoal = (defendingGoal.transform.position - puck.position).normalized;
        float velocityAlignment = Vector3.Dot(puckVelocity.normalized, toGoal);

        return velocityAlignment > 0.3f; // Puck is heading generally toward goal
    }

    /// <summary>
    /// Check if goalie should attempt a poke check
    /// </summary>
    private void CheckForPokeCheck()
    {
        if (Time.time < lastPokeCheckTime + pokeCheckCooldown) return;
        if (puckScript == null) return;

        float distanceToPuck = Vector3.Distance(transform.position, puck.position);

        // Only poke check if:
        // 1. Puck is in range
        // 2. Puck has an owner (player has possession)
        // 3. Player is close to goal
        if (distanceToPuck < pokeCheckRange && puckScript.CurrentOwner != null)
        {
            // Calculate if player is in dangerous position
            Vector3 toPlayer = puckScript.CurrentOwner.transform.position - transform.position;
            float angleToPlayer = Vector3.Angle(transform.forward, toPlayer);

            if (angleToPlayer < 45f && distanceToPuck < pokeCheckRange * 0.7f)
            {
                // Higher difficulty = more aggressive poke checks
                float pokeCheckChance = difficulty == DifficultyLevel.Hard ? 0.7f :
                                       difficulty == DifficultyLevel.Medium ? 0.5f : 0.3f;

                if (Random.value < pokeCheckChance * Time.fixedDeltaTime)
                {
                    PerformPokeCheck();
                }
            }
        }
    }

    /// <summary>
    /// Perform poke check action
    /// </summary>
    private void PerformPokeCheck()
    {
        currentState = GoalieState.PokeCheck;
        lastPokeCheckTime = Time.time;

        if (showDebugLogs)
        {
            Debug.Log("[Goalie] Poke check!");
        }

        // Apply forward lunge
        rb.AddForce(transform.forward * saveSpeed, ForceMode.Impulse);

        // Check for successful poke check
        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, 0.8f);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<Puck>() != null)
            {
                puckScript.LosePossession();

                // Deflect puck to side
                Vector3 deflection = (transform.right * Random.Range(-1f, 1f) + transform.forward * -1f).normalized;
                puckScript.GetComponent<Rigidbody>().AddForce(deflection * 8f, ForceMode.Impulse);

                if (showDebugLogs)
                {
                    Debug.Log("[Goalie] Poke check successful!");
                }
                break;
            }
        }
    }

    /// <summary>
    /// Called when a shot is taken (via GameEvents)
    /// </summary>
    private void OnShotTaken(Vector3 direction, float power)
    {
        if (puck == null || defendingGoal == null) return;

        // Check if shot is toward our goal
        Vector3 shotDirection = direction.normalized;
        Vector3 toGoal = (defendingGoal.transform.position - puck.position).normalized;
        float shotAlignment = Vector3.Dot(shotDirection, toGoal);

        if (shotAlignment > 0.5f) // Shot is threatening our goal
        {
            // Start reaction delay
            reactionDelayTimer = currentReactionTime;
            isReacting = false;

            // Anticipate shot destination
            AnticipateShot(direction, power);

            // Schedule save attempt
            Invoke(nameof(AttemptSave), currentReactionTime);
        }
    }

    /// <summary>
    /// Anticipate where the shot will end up
    /// </summary>
    private void AnticipateShot(Vector3 direction, float power)
    {
        // Raycast to find where shot will hit goal plane
        Vector3 shotOrigin = puck.position;
        Vector3 shotDirection = direction.normalized;

        // Create a plane at the goal
        Plane goalPlane = new Plane(-defendingGoal.transform.forward, defendingGoal.transform.position);

        Ray shotRay = new Ray(shotOrigin, shotDirection);
        float enter;

        if (goalPlane.Raycast(shotRay, out enter))
        {
            anticipatedShotPosition = shotRay.GetPoint(enter);

            if (showDebugLogs)
            {
                Debug.Log($"[Goalie] Anticipated shot position: {anticipatedShotPosition}");
            }
        }
        else
        {
            anticipatedShotPosition = defendingGoal.transform.position;
        }
    }

    /// <summary>
    /// Attempt to save the shot
    /// </summary>
    private void AttemptSave()
    {
        if (puck == null) return;

        float distanceToPuck = Vector3.Distance(transform.position, puck.position);

        // Check if puck is in save range
        if (distanceToPuck > saveRange)
        {
            if (showDebugLogs)
            {
                Debug.Log("[Goalie] Shot out of range - no save attempt");
            }
            return;
        }

        // Determine save type based on puck position relative to goalie
        Vector3 puckLocalPosition = transform.InverseTransformPoint(anticipatedShotPosition);
        SaveType saveType = DetermineSaveType(puckLocalPosition);

        // Calculate save success based on difficulty and shot quality
        bool saveSuccessful = DetermineSaveSuccess(distanceToPuck);

        if (saveSuccessful)
        {
            PerformSave(saveType);
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log("[Goalie] Save attempt failed - goal!");
            }
            currentState = GoalieState.PostSave;
        }
    }

    /// <summary>
    /// Determine which save type to use based on puck position
    /// </summary>
    private SaveType DetermineSaveType(Vector3 localPuckPosition)
    {
        float absX = Mathf.Abs(localPuckPosition.x);
        float height = localPuckPosition.y;

        // High shots
        if (height > 1.5f)
        {
            return localPuckPosition.x > 0 ? SaveType.Glove : SaveType.Blocker;
        }
        // Mid shots
        else if (height > 0.5f)
        {
            if (absX > 0.8f)
            {
                return localPuckPosition.x > 0 ? SaveType.Glove : SaveType.Blocker;
            }
            else
            {
                return SaveType.Butterfly;
            }
        }
        // Low shots
        else
        {
            return SaveType.Butterfly;
        }
    }

    /// <summary>
    /// Calculate if save is successful based on various factors
    /// </summary>
    private bool DetermineSaveSuccess(float distanceToPuck)
    {
        // Base save chance from difficulty
        float saveChance = currentSavePercentage;

        // Adjust for distance - closer shots are harder to save
        float distanceFactor = Mathf.Clamp01(distanceToPuck / saveRange);
        saveChance *= Mathf.Lerp(0.7f, 1.0f, distanceFactor);

        // Adjust for positioning - better position = higher save chance
        float positionQuality = 1f - (Vector3.Distance(transform.position, targetPosition) / creaseRadius);
        saveChance *= Mathf.Lerp(0.8f, 1.0f, Mathf.Clamp01(positionQuality));

        // Random element
        bool successful = Random.value < saveChance;

        if (showDebugLogs)
        {
            Debug.Log($"[Goalie] Save chance: {saveChance:P0}, Result: {(successful ? "SAVE" : "GOAL")}");
        }

        return successful;
    }

    /// <summary>
    /// Perform the save animation and physics
    /// </summary>
    private void PerformSave(SaveType saveType)
    {
        lastSaveType = saveType;

        // Apply save movement toward puck
        Vector3 saveDelta = (puck.position - transform.position).normalized;
        saveDelta.y = 0f;
        rb.AddForce(saveDelta * saveSpeed, ForceMode.Impulse);

        // Set state
        switch (saveType)
        {
            case SaveType.Glove:
                currentState = GoalieState.GloveSave;
                break;
            case SaveType.Blocker:
                currentState = GoalieState.BlockerSave;
                break;
            case SaveType.Butterfly:
            case SaveType.Pad:
                currentState = GoalieState.Butterfly;
                break;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[Goalie] Save performed: {saveType}");
        }

        // Deflect puck
        DeflectPuck(saveType);
    }

    /// <summary>
    /// Deflect the puck after a save
    /// </summary>
    private void DeflectPuck(SaveType saveType)
    {
        if (puckScript == null) return;

        // Make puck loose
        if (puckScript.CurrentOwner != null)
        {
            puckScript.LosePossession();
        }

        Rigidbody puckRb = puckScript.GetComponent<Rigidbody>();
        if (puckRb == null) return;

        // Calculate deflection direction based on save type
        Vector3 deflection;

        switch (saveType)
        {
            case SaveType.Glove:
                // Glove catches and directs to corner
                deflection = (transform.right + transform.forward * 0.5f).normalized;
                puckRb.linearVelocity = Vector3.zero;
                puckRb.AddForce(deflection * 5f, ForceMode.Impulse);
                break;

            case SaveType.Blocker:
                // Blocker deflects strongly to side
                deflection = (-transform.right + transform.forward * 0.3f).normalized;
                puckRb.linearVelocity *= 0.3f;
                puckRb.AddForce(deflection * 8f, ForceMode.Impulse);
                break;

            case SaveType.Butterfly:
            case SaveType.Pad:
                // Pads create rebounds
                deflection = (transform.forward + transform.right * Random.Range(-0.5f, 0.5f)).normalized;
                puckRb.linearVelocity *= 0.5f;
                puckRb.AddForce(deflection * 6f, ForceMode.Impulse);
                break;

            default:
                deflection = transform.forward;
                puckRb.linearVelocity *= 0.5f;
                break;
        }
    }

    /// <summary>
    /// Check if goalie should cover the puck
    /// </summary>
    private bool ShouldCoverPuck()
    {
        if (hasCoveredPuck || puckScript == null) return false;

        // Cover if puck is loose, close, and slow
        bool puckIsLoose = puckScript.IsLoose;
        float distanceToPuck = Vector3.Distance(transform.position, puck.position);
        float puckSpeed = puckScript.Velocity.magnitude;

        return puckIsLoose && distanceToPuck < coverPuckDistance && puckSpeed < 1f;
    }

    /// <summary>
    /// Cover the puck and stop play
    /// </summary>
    private void CoverPuck()
    {
        hasCoveredPuck = true;
        currentState = GoalieState.CoveringPuck;

        if (showDebugLogs)
        {
            Debug.Log("[Goalie] Covering puck - whistle!");
        }

        // Stop puck movement
        if (puckScript != null)
        {
            Rigidbody puckRb = puckScript.GetComponent<Rigidbody>();
            if (puckRb != null)
            {
                puckRb.linearVelocity = Vector3.zero;
                puckRb.isKinematic = true;
            }
        }

        // Trigger whistle after delay
        Invoke(nameof(Whistle), whistleDelay);
    }

    /// <summary>
    /// Blow whistle and reset play
    /// </summary>
    private void Whistle()
    {
        if (showDebugLogs)
        {
            Debug.Log("[Goalie] Whistle blown - reset faceoff");
        }

        // Re-enable puck physics
        if (puckScript != null)
        {
            Rigidbody puckRb = puckScript.GetComponent<Rigidbody>();
            if (puckRb != null)
            {
                puckRb.isKinematic = false;
            }
        }

        hasCoveredPuck = false;
        ReturnToStanding();
    }

    /// <summary>
    /// Return to standing/ready state
    /// </summary>
    private void ReturnToStanding()
    {
        currentState = GoalieState.Standing;
        lastSaveType = SaveType.None;
    }

    /// <summary>
    /// Called when a goal is scored
    /// </summary>
    private void OnGoalScored(int scoringTeam)
    {
        // Reset state
        ReturnToStanding();
        hasCoveredPuck = false;
        reactionDelayTimer = 0f;
        isReacting = false;
    }

    /// <summary>
    /// Update animator parameters if animator is assigned
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Set animator parameters based on state
        animator.SetInteger("GoalieState", (int)currentState);
        animator.SetInteger("SaveType", (int)lastSaveType);
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
        animator.SetBool("HasPuck", hasCoveredPuck);
    }

    /// <summary>
    /// Handle puck collisions
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        var puckComponent = collision.gameObject.GetComponent<Puck>();
        if (puckComponent != null)
        {
            // Automatic save on contact if we're in position
            if (currentState == GoalieState.Tracking || currentState == GoalieState.Standing)
            {
                float saveRoll = Random.value;
                if (saveRoll < currentSavePercentage * 0.7f) // Slightly lower chance on collision
                {
                    Vector3 localHit = transform.InverseTransformPoint(collision.contacts[0].point);
                    SaveType saveType = DetermineSaveType(localHit);
                    PerformSave(saveType);
                }
            }
        }
    }

    /// <summary>
    /// Public method to change difficulty during runtime
    /// </summary>
    public void SetDifficulty(DifficultyLevel newDifficulty)
    {
        difficulty = newDifficulty;
        InitializeDifficultySettings();

        if (showDebugLogs)
        {
            Debug.Log($"[Goalie] Difficulty set to {difficulty}");
        }
    }

    /// <summary>
    /// Public getter for current state
    /// </summary>
    public GoalieState CurrentState => currentState;

    /// <summary>
    /// Debug visualization
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw crease area
        if (creaseCenter != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(creaseCenter.position, creaseRadius);

            Vector3 forward = defendingGoal != null ? defendingGoal.transform.forward : -transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(creaseCenter.position, creaseCenter.position + forward * creaseDepth);
        }

        // Draw target position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, 0.3f);

        // Draw save range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, saveRange);

        // Draw poke check range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pokeCheckRange);

        // Draw anticipated shot position
        if (Application.isPlaying && reactionDelayTimer > 0f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(anticipatedShotPosition, 0.5f);
            Gizmos.DrawLine(transform.position, anticipatedShotPosition);
        }

        // Draw line to puck
        if (puck != null)
        {
            Gizmos.color = IsPuckThreatening() ? Color.red : Color.gray;
            Gizmos.DrawLine(transform.position, puck.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Draw danger zone
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, dangerZoneDistance);

        // Draw anticipation distance
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, anticipationDistance);
    }
}
