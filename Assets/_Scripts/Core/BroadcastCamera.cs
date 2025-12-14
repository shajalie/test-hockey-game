using UnityEngine;

/// <summary>
/// Broadcast-style camera that follows the puck and active player.
/// Provides a TV-like viewing angle for hockey games.
/// </summary>
public class BroadcastCamera : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform puck;
    [SerializeField] private Transform activePlayer;

    [Header("Camera Settings")]
    [SerializeField] private float height = 15f;
    [SerializeField] private float distance = 20f;
    [SerializeField] private float angle = 45f; // Degrees from vertical
    [SerializeField] private float fov = 60f;

    [Header("Follow Settings")]
    [SerializeField] private float followSmoothTime = 0.3f;
    [SerializeField] private float rotationSmoothTime = 0.5f;
    [SerializeField] private float puckWeight = 0.7f; // 0 = follow player, 1 = follow puck

    [Header("Boundaries")]
    [SerializeField] private bool useBoundaries = true;
    [SerializeField] private Vector2 rinkMinBounds = new Vector2(-30f, -15f);
    [SerializeField] private Vector2 rinkMaxBounds = new Vector2(30f, 15f);

    [Header("Lead Settings")]
    [SerializeField] private float leadAmount = 2f; // How much to lead the puck
    [SerializeField] private float leadSmoothTime = 0.5f;

    private Camera cam;
    private Vector3 currentVelocity;
    private Vector3 targetPosition;
    private Vector3 leadVelocity;
    private Vector3 smoothedLead;
    private Rigidbody puckRb;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fov;
        }
    }

    private void Start()
    {
        // Find puck if not assigned
        if (puck == null)
        {
            var puckObj = FindObjectOfType<Puck>();
            if (puckObj != null)
            {
                puck = puckObj.transform;
                puckRb = puckObj.GetComponent<Rigidbody>();
            }
        }
        else
        {
            puckRb = puck.GetComponent<Rigidbody>();
        }

        // Find player if not assigned
        if (activePlayer == null)
        {
            var players = FindObjectsOfType<HockeyPlayer>();
            foreach (var p in players)
            {
                if (p.GetComponent<AIController>() == null)
                {
                    activePlayer = p.transform;
                    break;
                }
            }
        }

        // Initialize position
        if (puck != null)
        {
            targetPosition = CalculateTargetPosition();
            transform.position = targetPosition;
            LookAtTarget();
        }
    }

    private void LateUpdate()
    {
        if (puck == null) return;

        // Calculate target focus point
        targetPosition = CalculateTargetPosition();

        // Smooth follow
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            followSmoothTime
        );

        // Smooth look at
        LookAtTarget();
    }

    /// <summary>
    /// Calculate where the camera should be positioned.
    /// </summary>
    private Vector3 CalculateTargetPosition()
    {
        // Get focus point (weighted between puck and player)
        Vector3 focusPoint = GetFocusPoint();

        // Add lead based on puck velocity
        if (puckRb != null)
        {
            Vector3 lead = puckRb.linearVelocity * leadAmount * 0.1f;
            lead.y = 0;
            smoothedLead = Vector3.SmoothDamp(smoothedLead, lead, ref leadVelocity, leadSmoothTime);
            focusPoint += smoothedLead;
        }

        // Clamp to rink boundaries
        if (useBoundaries)
        {
            focusPoint.x = Mathf.Clamp(focusPoint.x, rinkMinBounds.x, rinkMaxBounds.x);
            focusPoint.z = Mathf.Clamp(focusPoint.z, rinkMinBounds.y, rinkMaxBounds.y);
        }

        // Calculate camera position (broadcast angle from the side)
        float radAngle = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            0f,
            height,
            -distance
        );

        // Rotate offset around Y axis based on which side of rink we're on
        // This keeps camera on the "broadcast" side
        float sideAngle = focusPoint.x > 0 ? -15f : 15f; // Slight angle toward center
        offset = Quaternion.Euler(0, sideAngle, 0) * offset;

        return focusPoint + offset;
    }

    /// <summary>
    /// Get the weighted focus point between puck and player.
    /// </summary>
    private Vector3 GetFocusPoint()
    {
        Vector3 puckPos = puck.position;

        if (activePlayer != null)
        {
            Vector3 playerPos = activePlayer.position;
            return Vector3.Lerp(playerPos, puckPos, puckWeight);
        }

        return puckPos;
    }

    /// <summary>
    /// Smoothly rotate camera to look at the focus point.
    /// </summary>
    private void LookAtTarget()
    {
        Vector3 focusPoint = GetFocusPoint();

        // Add lead to look-at as well
        focusPoint += smoothedLead * 0.5f;

        Vector3 lookDirection = focusPoint - transform.position;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime / rotationSmoothTime
            );
        }
    }

    // === PUBLIC METHODS ===

    /// <summary>
    /// Set the puck to follow.
    /// </summary>
    public void SetPuck(Transform newPuck)
    {
        puck = newPuck;
        puckRb = newPuck?.GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Set the active player to consider.
    /// </summary>
    public void SetActivePlayer(Transform player)
    {
        activePlayer = player;
    }

    /// <summary>
    /// Set the weight between puck and player focus.
    /// </summary>
    public void SetPuckWeight(float weight)
    {
        puckWeight = Mathf.Clamp01(weight);
    }

    /// <summary>
    /// Shake the camera (for goals, big hits, etc.).
    /// </summary>
    public void Shake(float intensity = 0.5f, float duration = 0.3f)
    {
        StartCoroutine(DoShake(intensity, duration));
    }

    private System.Collections.IEnumerator DoShake(float intensity, float duration)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            intensity *= 0.95f; // Decay

            yield return null;
        }

        transform.localPosition = originalPos;
    }

    /// <summary>
    /// Set rink boundaries for camera clamping.
    /// </summary>
    public void SetBoundaries(Vector2 min, Vector2 max)
    {
        rinkMinBounds = min;
        rinkMaxBounds = max;
    }

    // === DEBUG ===

    private void OnDrawGizmosSelected()
    {
        // Draw focus point
        Vector3 focus = puck != null ? GetFocusPoint() : transform.position + transform.forward * 10f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(focus, 1f);

        // Draw boundaries
        if (useBoundaries)
        {
            Gizmos.color = Color.cyan;
            Vector3 min = new Vector3(rinkMinBounds.x, 0, rinkMinBounds.y);
            Vector3 max = new Vector3(rinkMaxBounds.x, 0, rinkMaxBounds.y);
            Vector3 size = max - min;
            Gizmos.DrawWireCube((min + max) / 2f, new Vector3(size.x, 1f, size.z));
        }
    }
}
