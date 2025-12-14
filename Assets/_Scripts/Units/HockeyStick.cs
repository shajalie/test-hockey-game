using UnityEngine;

public enum StickAnimationState { Skating, Shooting, Passing, PokeCheck }

/// <summary>
/// Visual hockey stick that follows player rotation with animation states.
/// </summary>
public class HockeyStick : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform stickRoot;
    [SerializeField] private Transform bladeContactPoint;
    [SerializeField] private MeshRenderer bladeRenderer;

    [Header("Positioning")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0.3f, 0.5f, 0.4f);
    [SerializeField] private Vector3 rotationOffset = new Vector3(-45f, 0f, 0f);
    [SerializeField] private float followSmoothness = 0.15f;

    [Header("Animation")]
    [SerializeField] private float skatingAngle = -10f;
    [SerializeField] private float shootingAngle = 30f;
    [SerializeField] private float passingAngle = 15f;
    [SerializeField] private float pokeCheckAngle = -15f;
    [SerializeField] private float pokeCheckExtension = 0.5f;
    [SerializeField] private float animationSpeed = 8f;

    [Header("Puck Glow")]
    [SerializeField] private bool enablePuckGlow = true;
    [SerializeField] private Color glowColor = new Color(1f, 0.7f, 0f);

    private StickAnimationState currentState = StickAnimationState.Skating;
    private float currentBlendAngle;
    private float currentExtension;
    private float stateTimer;
    private Material bladeMaterial;
    private HockeyPlayer player;

    public Vector3 BladePosition => bladeContactPoint != null ? bladeContactPoint.position : transform.position + transform.forward;
    public Vector3 BladeForward => bladeContactPoint != null ? bladeContactPoint.forward : transform.forward;
    public Transform BladeContactPoint => bladeContactPoint;
    public StickAnimationState CurrentState => currentState;

    private void Awake()
    {
        player = GetComponent<HockeyPlayer>();
        if (bladeRenderer != null && enablePuckGlow)
        {
            bladeMaterial = new Material(bladeRenderer.sharedMaterial);
            bladeRenderer.material = bladeMaterial;
        }
        currentBlendAngle = skatingAngle;
    }

    private void LateUpdate()
    {
        UpdateStickPosition();
        UpdateAnimationState();
        UpdatePuckGlow();
    }

    private void UpdateStickPosition()
    {
        if (stickRoot == null) return;

        // Position relative to player
        Vector3 targetPos = transform.position + transform.TransformDirection(positionOffset);
        targetPos.z += currentExtension;
        stickRoot.position = Vector3.Lerp(stickRoot.position, targetPos, 1f - followSmoothness);

        // Rotation follows player with blend angle
        Vector3 targetRot = rotationOffset + new Vector3(currentBlendAngle, 0f, 0f);
        Quaternion targetQuat = transform.rotation * Quaternion.Euler(targetRot);
        stickRoot.rotation = Quaternion.Slerp(stickRoot.rotation, targetQuat, 1f - followSmoothness);
    }

    private void UpdateAnimationState()
    {
        float targetAngle = currentState switch
        {
            StickAnimationState.Skating => skatingAngle,
            StickAnimationState.Shooting => shootingAngle,
            StickAnimationState.Passing => passingAngle,
            StickAnimationState.PokeCheck => pokeCheckAngle,
            _ => skatingAngle
        };

        float targetExtension = currentState == StickAnimationState.PokeCheck ? pokeCheckExtension : 0f;

        currentBlendAngle = Mathf.Lerp(currentBlendAngle, targetAngle, animationSpeed * Time.deltaTime);
        currentExtension = Mathf.Lerp(currentExtension, targetExtension, animationSpeed * Time.deltaTime);

        // Auto-return from temporary states
        if (stateTimer > 0f)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                currentState = StickAnimationState.Skating;
            }
        }
    }

    private void UpdatePuckGlow()
    {
        if (!enablePuckGlow || bladeMaterial == null || player == null) return;

        float targetEmission = player.HasPuck ? 1f : 0f;
        Color currentEmission = bladeMaterial.GetColor("_EmissionColor");
        Color targetColor = glowColor * targetEmission * 2f;
        bladeMaterial.SetColor("_EmissionColor", Color.Lerp(currentEmission, targetColor, 5f * Time.deltaTime));
    }

    public void SetAnimationState(StickAnimationState state)
    {
        currentState = state;
        stateTimer = 0f;
    }

    public void TriggerShootAnimation()
    {
        currentState = StickAnimationState.Shooting;
        stateTimer = 0.5f;
    }

    public void TriggerPassAnimation()
    {
        currentState = StickAnimationState.Passing;
        stateTimer = 0.3f;
    }

    public void TriggerPokeCheck()
    {
        currentState = StickAnimationState.PokeCheck;
        stateTimer = 0.3f;
    }

    public bool IsPointInRange(Vector3 point, float range)
    {
        return Vector3.Distance(BladePosition, point) <= range;
    }

    private void OnDrawGizmosSelected()
    {
        if (bladeContactPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(bladeContactPoint.position, 0.15f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(bladeContactPoint.position, bladeContactPoint.forward * 0.5f);
        }
        if (stickRoot != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(stickRoot.position, 0.1f);
        }
    }
}
