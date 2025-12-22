using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Smart passing system that finds the best pass target based on aim direction.
/// Handles pass power calculation, lead targeting, and saucer passes.
/// </summary>
public class PassingSystem : MonoBehaviour
{
    #region Serialized Fields

    [Header("Pass Detection")]
    [SerializeField] private float maxPassDistance = 30f;
    [SerializeField] private float minPassDistance = 2f;
    [SerializeField] private float aimAssistAngle = 45f; // Degrees from aim direction
    [SerializeField] private LayerMask teammateLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Pass Power")]
    [SerializeField] private float minPassPower = 8f;
    [SerializeField] private float maxPassPower = 25f;
    [SerializeField] private float powerPerDistance = 0.8f; // Power added per unit distance

    [Header("Lead Targeting")]
    [SerializeField] private float leadFactor = 0.7f; // How much to lead moving targets
    [SerializeField] private float maxLeadDistance = 5f;

    [Header("Saucer Pass")]
    [SerializeField] private float saucerHeight = 0.4f;
    [SerializeField] private float saucerPowerMultiplier = 1.2f;

    [Header("Pass Types")]
    [SerializeField] private bool enableSnapPasses = true;
    [SerializeField] private float snapPassRange = 8f;
    [SerializeField] private float snapPassPowerBoost = 1.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    #endregion

    #region Private Fields

    private PuckController puckController;
    private List<PassCandidate> candidates = new List<PassCandidate>();
    private PassCandidate currentBestTarget;
    private Vector2 aimDirection;

    #endregion

    #region Structs

    [System.Serializable]
    public struct PassCandidate
    {
        public GameObject player;
        public Transform stickTarget;
        public float distance;
        public float angle;
        public float score;
        public bool isBlocked;
        public Vector3 leadPosition;

        public bool IsValid => player != null && !isBlocked;
    }

    #endregion

    #region Properties

    /// <summary>Current best pass target.</summary>
    public PassCandidate? BestTarget => currentBestTarget.player != null ? currentBestTarget : null;

    /// <summary>All valid pass candidates.</summary>
    public IReadOnlyList<PassCandidate> Candidates => candidates;

    /// <summary>Whether a valid pass target exists.</summary>
    public bool HasValidTarget => currentBestTarget.player != null && currentBestTarget.IsValid;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        puckController = GetComponent<PuckController>();
        if (puckController == null)
        {
            puckController = FindObjectOfType<PuckController>();
        }
    }

    private void Update()
    {
        // Only update when there's a puck owner
        if (puckController != null && puckController.Owner != null)
        {
            UpdatePassCandidates();
        }
    }

    #endregion

    #region Pass Target Detection

    /// <summary>
    /// Set the aim direction for pass targeting.
    /// </summary>
    public void SetAimDirection(Vector2 aim)
    {
        aimDirection = aim.normalized;
    }

    /// <summary>
    /// Update all pass candidates based on current aim.
    /// </summary>
    public void UpdatePassCandidates()
    {
        candidates.Clear();
        currentBestTarget = default;

        if (puckController == null || puckController.Owner == null) return;

        GameObject owner = puckController.Owner;
        int ownerTeam = GetTeamId(owner);

        // Convert aim to world direction
        Vector3 worldAimDir = new Vector3(aimDirection.x, 0, aimDirection.y);
        if (worldAimDir.sqrMagnitude < 0.1f)
        {
            worldAimDir = owner.transform.forward;
        }
        worldAimDir.Normalize();

        // Find all potential teammates
        HockeyPlayer[] allPlayers = FindObjectsOfType<HockeyPlayer>();

        foreach (var player in allPlayers)
        {
            // Skip self
            if (player.gameObject == owner) continue;

            // Skip enemies
            if (player.TeamId != ownerTeam) continue;

            // Calculate candidate
            PassCandidate candidate = EvaluateCandidate(owner, player, worldAimDir);

            if (candidate.distance >= minPassDistance && candidate.distance <= maxPassDistance)
            {
                candidates.Add(candidate);
            }
        }

        // Sort by score (highest first)
        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        // Best target is highest scoring unblocked candidate
        foreach (var candidate in candidates)
        {
            if (candidate.IsValid)
            {
                currentBestTarget = candidate;
                break;
            }
        }
    }

    private PassCandidate EvaluateCandidate(GameObject owner, HockeyPlayer target, Vector3 aimDir)
    {
        PassCandidate candidate = new PassCandidate
        {
            player = target.gameObject,
            stickTarget = target.StickTip
        };

        Vector3 ownerPos = owner.transform.position;
        Vector3 targetPos = target.transform.position;

        // Distance
        candidate.distance = Vector3.Distance(ownerPos, targetPos);

        // Angle from aim direction
        Vector3 toTarget = (targetPos - ownerPos).normalized;
        toTarget.y = 0;
        candidate.angle = Vector3.Angle(aimDir, toTarget);

        // Calculate lead position
        candidate.leadPosition = CalculateLeadPosition(target, ownerPos, candidate.distance);

        // Check if blocked
        candidate.isBlocked = IsPassBlocked(ownerPos, candidate.leadPosition, target.gameObject);

        // Calculate score
        candidate.score = CalculatePassScore(candidate, aimDir);

        return candidate;
    }

    private Vector3 CalculateLeadPosition(HockeyPlayer target, Vector3 fromPos, float distance)
    {
        Vector3 targetPos = target.transform.position;
        Vector3 targetVel = target.Velocity;

        if (targetVel.magnitude < 0.5f)
        {
            return targetPos; // Not moving much
        }

        // Estimate pass travel time
        float passPower = CalculatePassPower(distance);
        float travelTime = distance / passPower;

        // Lead position
        Vector3 lead = targetVel * travelTime * leadFactor;

        // Clamp lead distance
        if (lead.magnitude > maxLeadDistance)
        {
            lead = lead.normalized * maxLeadDistance;
        }

        return targetPos + lead;
    }

    private bool IsPassBlocked(Vector3 from, Vector3 to, GameObject ignoreTarget)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        direction.Normalize();

        // Raise ray slightly above ice
        from.y += 0.2f;

        RaycastHit[] hits = Physics.RaycastAll(from, direction, distance, obstacleLayer);

        foreach (var hit in hits)
        {
            // Ignore the target
            if (hit.collider.gameObject == ignoreTarget) continue;
            if (hit.collider.transform.IsChildOf(ignoreTarget.transform)) continue;

            // Check if it's an opponent
            HockeyPlayer player = hit.collider.GetComponent<HockeyPlayer>();
            if (player != null)
            {
                int ownerTeam = GetTeamId(puckController.Owner);
                if (player.TeamId != ownerTeam)
                {
                    return true; // Blocked by opponent
                }
                continue; // Skip teammates
            }

            // Other obstacles (boards shouldn't block normally)
            if (!hit.collider.CompareTag("Boards"))
            {
                return true;
            }
        }

        return false;
    }

    private float CalculatePassScore(PassCandidate candidate, Vector3 aimDir)
    {
        float score = 100f;

        // Angle penalty (passes in aim direction score higher)
        if (candidate.angle > aimAssistAngle)
        {
            return 0f; // Outside aim assist cone
        }
        float angleScore = 1f - (candidate.angle / aimAssistAngle);
        score *= angleScore;

        // Distance factor (prefer medium distance)
        float optimalDistance = maxPassDistance * 0.4f;
        float distanceFromOptimal = Mathf.Abs(candidate.distance - optimalDistance);
        float distanceScore = 1f - (distanceFromOptimal / maxPassDistance);
        score *= Mathf.Lerp(0.5f, 1f, distanceScore);

        // Blocked penalty
        if (candidate.isBlocked)
        {
            score *= 0.1f; // Heavily penalize blocked passes
        }

        // Bonus for player in good position (ahead of puck)
        Vector3 toTarget = (candidate.player.transform.position - puckController.transform.position).normalized;
        float forwardDot = Vector3.Dot(toTarget, aimDir);
        if (forwardDot > 0.5f)
        {
            score *= 1.2f; // Bonus for forward passes
        }

        return score;
    }

    private int GetTeamId(GameObject player)
    {
        HockeyPlayer hp = player.GetComponent<HockeyPlayer>();
        return hp != null ? hp.TeamId : 0;
    }

    #endregion

    #region Pass Execution

    /// <summary>
    /// Execute a pass to the best target.
    /// </summary>
    public bool ExecutePass()
    {
        if (!HasValidTarget || puckController == null) return false;

        PassCandidate target = currentBestTarget;
        float power = CalculatePassPower(target.distance);

        puckController.Pass(target.player, power);
        return true;
    }

    /// <summary>
    /// Execute a pass to a specific target.
    /// </summary>
    public bool ExecutePassTo(GameObject target)
    {
        if (target == null || puckController == null) return false;

        float distance = Vector3.Distance(puckController.transform.position, target.transform.position);
        float power = CalculatePassPower(distance);

        puckController.Pass(target, power);
        return true;
    }

    /// <summary>
    /// Execute a snap pass (quick, hard pass).
    /// </summary>
    public bool ExecuteSnapPass()
    {
        if (!HasValidTarget || puckController == null) return false;

        PassCandidate target = currentBestTarget;

        if (target.distance > snapPassRange)
        {
            return ExecutePass(); // Fall back to normal pass
        }

        float power = CalculatePassPower(target.distance) * snapPassPowerBoost;
        puckController.Pass(target.player, power);
        return true;
    }

    /// <summary>
    /// Execute a saucer pass (over obstacles).
    /// </summary>
    public bool ExecuteSaucerPass()
    {
        if (!HasValidTarget || puckController == null) return false;

        PassCandidate target = currentBestTarget;
        float power = CalculatePassPower(target.distance) * saucerPowerMultiplier;

        puckController.SaucerPass(target.player, power, saucerHeight);
        return true;
    }

    /// <summary>
    /// Calculate pass power based on distance.
    /// </summary>
    public float CalculatePassPower(float distance)
    {
        float basePower = minPassPower + (distance * powerPerDistance);
        return Mathf.Clamp(basePower, minPassPower, maxPassPower);
    }

    #endregion

    #region Manual Target Selection

    /// <summary>
    /// Cycle to next pass target.
    /// </summary>
    public void CycleTarget(bool forward = true)
    {
        if (candidates.Count < 2) return;

        int currentIndex = candidates.FindIndex(c => c.player == currentBestTarget.player);

        if (forward)
        {
            currentIndex = (currentIndex + 1) % candidates.Count;
        }
        else
        {
            currentIndex = (currentIndex - 1 + candidates.Count) % candidates.Count;
        }

        // Find next valid target
        for (int i = 0; i < candidates.Count; i++)
        {
            int checkIndex = (currentIndex + i) % candidates.Count;
            if (candidates[checkIndex].IsValid)
            {
                currentBestTarget = candidates[checkIndex];
                break;
            }
        }
    }

    /// <summary>
    /// Get candidate closest to a world position.
    /// </summary>
    public PassCandidate? GetCandidateNear(Vector3 worldPosition)
    {
        PassCandidate? closest = null;
        float closestDist = float.MaxValue;

        foreach (var candidate in candidates)
        {
            if (!candidate.IsValid) continue;

            float dist = Vector3.Distance(candidate.player.transform.position, worldPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = candidate;
            }
        }

        return closest;
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        // Draw aim direction
        if (puckController != null && puckController.Owner != null)
        {
            Vector3 ownerPos = puckController.Owner.transform.position + Vector3.up * 0.5f;
            Vector3 aimWorld = new Vector3(aimDirection.x, 0, aimDirection.y);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(ownerPos, aimWorld * 5f);

            // Draw aim cone
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            DrawAimCone(ownerPos, aimWorld, aimAssistAngle, maxPassDistance);
        }

        // Draw candidates
        foreach (var candidate in candidates)
        {
            if (candidate.player == null) continue;

            bool isBest = candidate.player == currentBestTarget.player;

            // Line to target
            Gizmos.color = candidate.isBlocked ? Color.red :
                           isBest ? Color.green : Color.white;
            Gizmos.DrawLine(puckController.transform.position, candidate.leadPosition);

            // Lead position marker
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(candidate.leadPosition, 0.3f);

            // Score label would go here in editor
        }
    }

    private void DrawAimCone(Vector3 origin, Vector3 direction, float angle, float length)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        int segments = 16;
        float halfAngle = angle * Mathf.Deg2Rad;

        Vector3 forward = direction.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 leftEdge = Quaternion.AngleAxis(-angle, Vector3.up) * forward * length;
        Vector3 rightEdge = Quaternion.AngleAxis(angle, Vector3.up) * forward * length;

        Gizmos.DrawLine(origin, origin + leftEdge);
        Gizmos.DrawLine(origin, origin + rightEdge);
    }

    #endregion
}
