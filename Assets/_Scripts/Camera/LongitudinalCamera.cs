using UnityEngine;

/// <summary>
/// Broadcast-style camera positioned behind the net, looking down the ice.
/// Provides a longitudinal view like NHL goal-cam or end-zone broadcast angles.
/// Features:
/// - Fixed position behind net (or slight tracking)
/// - FOV zoom based on puck distance
/// - Smooth puck/player tracking
/// - Optional end-switching when play crosses center
/// </summary>
public class LongitudinalCamera : MonoBehaviour
{
    #region Enums

    public enum CameraEnd
    {
        Home,   // Behind home net (negative Z typically)
        Away,   // Behind away net (positive Z typically)
        Auto    // Automatically switch based on puck position
    }

    public enum TrackingMode
    {
        Fixed,      // Camera stays fixed, only rotates to track
        SlightPan,  // Camera pans slightly left/right with action
        FullFollow  // Camera follows puck position more closely
    }

    #endregion

    #region Serialized Fields

    [Header("Camera End Position")]
    [SerializeField] private CameraEnd activeEnd = CameraEnd.Home;
    [SerializeField] private float switchHysteresis = 5f; // Distance past center before switching

    [Header("Position Settings")]
    [SerializeField] private float distanceBehindNet = 8f;
    [SerializeField] private float height = 6f;
    [SerializeField] private float horizontalOffset = 0f; // Slight offset from center

    [Header("Rink Dimensions")]
    [SerializeField] private float rinkLength = 60f; // Full length of rink (net to net)
    [SerializeField] private float rinkWidth = 30f;  // Width of rink
    [SerializeField] private float netZPosition = 26f; // Z position of the goal line

    [Header("Tracking")]
    [SerializeField] private TrackingMode trackingMode = TrackingMode.SlightPan;
    [SerializeField] private float panAmount = 0.3f; // How much to pan (0-1)
    [SerializeField] private float trackingSmoothTime = 0.2f;

    [Header("FOV Zoom")]
    [SerializeField] private bool useFovZoom = true;
    [SerializeField] private float minFov = 40f;  // When puck is far (other end)
    [SerializeField] private float maxFov = 70f;  // When puck is close
    [SerializeField] private float fovSmoothTime = 0.3f;

    [Header("Look Settings")]
    [SerializeField] private float lookAheadAmount = 2f; // Lead the puck slightly
    [SerializeField] private float verticalLookOffset = -1f; // Look slightly down at ice
    [SerializeField] private float rotationSmoothTime = 0.15f;

    [Header("Targets")]
    [SerializeField] private Transform puck;
    [SerializeField] private Transform activePlayer;
    [SerializeField] private float playerWeight = 0.3f; // Blend between puck and player

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    #endregion

    #region Private Fields

    private Camera cam;
    private Rigidbody puckRb;

    // Smoothing
    private Vector3 positionVelocity;
    private Vector3 lookVelocity;
    private float fovVelocity;
    private Vector3 currentLookTarget;

    // State
    private CameraEnd currentEnd;
    private bool isInitialized = false;

    #endregion

    #region Properties

    /// <summary>
    /// Current active end the camera is viewing from.
    /// </summary>
    public CameraEnd CurrentEnd => currentEnd;

    /// <summary>
    /// Current camera FOV.
    /// </summary>
    public float CurrentFov => cam != null ? cam.fieldOfView : 60f;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = gameObject.AddComponent<Camera>();
        }

        currentEnd = activeEnd == CameraEnd.Auto ? CameraEnd.Home : activeEnd;
    }

    private void Start()
    {
        FindTargets();
        InitializePosition();
        isInitialized = true;
    }

    private void LateUpdate()
    {
        if (!isInitialized || puck == null) return;

        // Check for end switching
        if (activeEnd == CameraEnd.Auto)
        {
            UpdateAutoEndSwitch();
        }

        // Update position
        UpdatePosition();

        // Update rotation (look at puck)
        UpdateRotation();

        // Update FOV
        if (useFovZoom)
        {
            UpdateFov();
        }
    }

    #endregion

    #region Initialization

    private void FindTargets()
    {
        // Find puck
        if (puck == null)
        {
            Puck puckObj = FindObjectOfType<Puck>();
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

        // Find player
        if (activePlayer == null)
        {
            HockeyPlayer[] players = FindObjectsOfType<HockeyPlayer>();
            foreach (var p in players)
            {
                if (p.GetComponent<AIController>() == null)
                {
                    activePlayer = p.transform;
                    break;
                }
            }
        }

        if (puck == null)
        {
            Debug.LogWarning("[LongitudinalCamera] No puck found in scene!");
        }
    }

    private void InitializePosition()
    {
        Vector3 targetPos = CalculateTargetPosition();
        transform.position = targetPos;

        if (puck != null)
        {
            currentLookTarget = GetFocusPoint();
            transform.LookAt(currentLookTarget);
        }

        if (cam != null)
        {
            cam.fieldOfView = CalculateTargetFov();
        }
    }

    #endregion

    #region Position

    private void UpdatePosition()
    {
        Vector3 targetPos = CalculateTargetPosition();

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref positionVelocity,
            trackingSmoothTime
        );
    }

    private Vector3 CalculateTargetPosition()
    {
        // Base position behind the net
        float zPos = currentEnd == CameraEnd.Home
            ? -netZPosition - distanceBehindNet
            : netZPosition + distanceBehindNet;

        float xPos = horizontalOffset;

        // Apply tracking mode
        if (trackingMode != TrackingMode.Fixed && puck != null)
        {
            Vector3 focusPoint = GetFocusPoint();

            if (trackingMode == TrackingMode.SlightPan)
            {
                // Slight horizontal pan to follow action
                float maxPan = rinkWidth * 0.3f;
                xPos += Mathf.Clamp(focusPoint.x * panAmount, -maxPan, maxPan);
            }
            else if (trackingMode == TrackingMode.FullFollow)
            {
                // More aggressive following
                float maxPan = rinkWidth * 0.4f;
                xPos += Mathf.Clamp(focusPoint.x * 0.6f, -maxPan, maxPan);

                // Also adjust Z slightly based on puck distance
                float puckDistanceRatio = GetPuckDistanceRatio();
                float zAdjust = puckDistanceRatio * 3f; // Move forward when puck is far

                if (currentEnd == CameraEnd.Home)
                {
                    zPos += zAdjust;
                }
                else
                {
                    zPos -= zAdjust;
                }
            }
        }

        return new Vector3(xPos, height, zPos);
    }

    #endregion

    #region Rotation

    private void UpdateRotation()
    {
        Vector3 targetLookPoint = GetLookTarget();

        // Smooth the look target
        currentLookTarget = Vector3.SmoothDamp(
            currentLookTarget,
            targetLookPoint,
            ref lookVelocity,
            rotationSmoothTime
        );

        // Look at the smoothed target
        Vector3 lookDirection = currentLookTarget - transform.position;
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

    private Vector3 GetLookTarget()
    {
        Vector3 focusPoint = GetFocusPoint();

        // Add look-ahead based on puck velocity
        if (puckRb != null && lookAheadAmount > 0)
        {
            Vector3 velocity = puckRb.linearVelocity;
            velocity.y = 0;
            focusPoint += velocity.normalized * lookAheadAmount * Mathf.Clamp01(velocity.magnitude / 20f);
        }

        // Apply vertical offset
        focusPoint.y += verticalLookOffset;

        return focusPoint;
    }

    private Vector3 GetFocusPoint()
    {
        if (puck == null) return Vector3.zero;

        Vector3 puckPos = puck.position;

        if (activePlayer != null && playerWeight > 0)
        {
            Vector3 playerPos = activePlayer.position;
            return Vector3.Lerp(puckPos, playerPos, playerWeight);
        }

        return puckPos;
    }

    #endregion

    #region FOV Zoom

    private void UpdateFov()
    {
        float targetFov = CalculateTargetFov();

        cam.fieldOfView = Mathf.SmoothDamp(
            cam.fieldOfView,
            targetFov,
            ref fovVelocity,
            fovSmoothTime
        );
    }

    private float CalculateTargetFov()
    {
        float distanceRatio = GetPuckDistanceRatio();

        // Closer puck = wider FOV, farther puck = narrower FOV (zoom in)
        // This keeps the action visible at both ends
        return Mathf.Lerp(maxFov, minFov, distanceRatio);
    }

    /// <summary>
    /// Get puck distance as ratio (0 = at camera end, 1 = at far end).
    /// </summary>
    private float GetPuckDistanceRatio()
    {
        if (puck == null) return 0.5f;

        float puckZ = puck.position.z;
        float cameraZ = currentEnd == CameraEnd.Home ? -netZPosition : netZPosition;
        float farZ = currentEnd == CameraEnd.Home ? netZPosition : -netZPosition;

        float distance = Mathf.Abs(puckZ - cameraZ);
        float maxDistance = Mathf.Abs(farZ - cameraZ);

        return Mathf.Clamp01(distance / maxDistance);
    }

    #endregion

    #region End Switching

    private void UpdateAutoEndSwitch()
    {
        if (puck == null) return;

        float puckZ = puck.position.z;

        // Switch ends when puck crosses center with hysteresis
        if (currentEnd == CameraEnd.Home && puckZ > switchHysteresis)
        {
            SwitchToEnd(CameraEnd.Away);
        }
        else if (currentEnd == CameraEnd.Away && puckZ < -switchHysteresis)
        {
            SwitchToEnd(CameraEnd.Home);
        }
    }

    private void SwitchToEnd(CameraEnd newEnd)
    {
        if (currentEnd == newEnd) return;

        Debug.Log($"[LongitudinalCamera] Switching from {currentEnd} to {newEnd}");
        currentEnd = newEnd;

        // Could trigger a transition animation here
    }

    #endregion

    #region Public API

    /// <summary>
    /// Set the camera to view from a specific end.
    /// </summary>
    public void SetEnd(CameraEnd end)
    {
        activeEnd = end;
        if (end != CameraEnd.Auto)
        {
            currentEnd = end;
        }
    }

    /// <summary>
    /// Set the puck to track.
    /// </summary>
    public void SetPuck(Transform newPuck)
    {
        puck = newPuck;
        puckRb = newPuck?.GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Set the active player to blend focus with.
    /// </summary>
    public void SetActivePlayer(Transform player)
    {
        activePlayer = player;
    }

    /// <summary>
    /// Set the blend weight between puck and player.
    /// </summary>
    public void SetPlayerWeight(float weight)
    {
        playerWeight = Mathf.Clamp01(weight);
    }

    /// <summary>
    /// Shake the camera (for goals, hits, etc).
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
            intensity *= 0.9f;

            yield return null;
        }

        transform.localPosition = originalPos;
    }

    /// <summary>
    /// Quick zoom effect (for goals, etc).
    /// </summary>
    public void ZoomPulse(float targetFov = 50f, float duration = 0.5f)
    {
        StartCoroutine(DoZoomPulse(targetFov, duration));
    }

    private System.Collections.IEnumerator DoZoomPulse(float targetFov, float duration)
    {
        float startFov = cam.fieldOfView;
        float elapsed = 0f;
        float halfDuration = duration / 2f;

        // Zoom in
        while (elapsed < halfDuration)
        {
            cam.fieldOfView = Mathf.Lerp(startFov, targetFov, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Zoom out
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            cam.fieldOfView = Mathf.Lerp(targetFov, startFov, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.fieldOfView = startFov;
    }

    /// <summary>
    /// Set rink dimensions for proper camera positioning.
    /// </summary>
    public void SetRinkDimensions(float length, float width, float netZ)
    {
        rinkLength = length;
        rinkWidth = width;
        netZPosition = netZ;
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw camera frustum
        Gizmos.color = Color.white;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, cam != null ? cam.fieldOfView : 60f, 50f, 0.3f, 1.78f);
        Gizmos.matrix = Matrix4x4.identity;

        // Draw look target
        if (Application.isPlaying && puck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentLookTarget, 0.5f);
            Gizmos.DrawLine(transform.position, currentLookTarget);
        }

        // Draw net positions
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(0, 1, -netZPosition), new Vector3(2, 1.2f, 0.5f));
        Gizmos.DrawWireCube(new Vector3(0, 1, netZPosition), new Vector3(2, 1.2f, 0.5f));

        // Draw camera positions for both ends
        Gizmos.color = Color.cyan;
        Vector3 homePos = new Vector3(0, height, -netZPosition - distanceBehindNet);
        Vector3 awayPos = new Vector3(0, height, netZPosition + distanceBehindNet);
        Gizmos.DrawWireSphere(homePos, 0.5f);
        Gizmos.DrawWireSphere(awayPos, 0.5f);

        // Draw rink outline
        Gizmos.color = Color.blue;
        float halfWidth = rinkWidth / 2f;
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-halfWidth, 0, -netZPosition - 5),
            new Vector3(halfWidth, 0, -netZPosition - 5),
            new Vector3(halfWidth, 0, netZPosition + 5),
            new Vector3(-halfWidth, 0, netZPosition + 5)
        };
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        // Draw center line
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-halfWidth, 0, 0), new Vector3(halfWidth, 0, 0));
    }

    private void OnDrawGizmosSelected()
    {
        // Draw FOV range indicator
        if (puck != null)
        {
            float ratio = GetPuckDistanceRatio();
            Gizmos.color = Color.Lerp(Color.green, Color.red, ratio);
            Gizmos.DrawLine(transform.position, puck.position);
        }
    }

    #endregion
}
