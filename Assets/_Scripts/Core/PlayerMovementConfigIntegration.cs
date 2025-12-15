using UnityEngine;

namespace HockeyGame.Core
{
    /// <summary>
    /// Example integration of GameConfig with player movement system.
    /// Shows how to make existing player controllers read from centralized config.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovementConfigIntegration : MonoBehaviour
    {
        private Rigidbody rb;
        private GameConfig config;

        [Header("Movement State")]
        [SerializeField] private bool isSprinting = false;
        [SerializeField] private float currentStamina = 100f;

        [Header("Input (Example)")]
        [SerializeField] private Vector2 movementInput;

        [Header("Debug Info")]
        [SerializeField] private float currentSpeed;
        [SerializeField] private float targetSpeed;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();

            // Get initial config
            config = GameConfigManager.Instance.Config;

            // Initialize stamina
            currentStamina = config.maxStamina;

            // Subscribe to config changes
            GameConfigManager.Instance.OnConfigChanged += OnConfigChanged;

            Debug.Log($"[PlayerMovement] Initialized with speed: {config.GetEffectivePlayerSpeed()} m/s");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (GameConfigManager.Instance != null)
            {
                GameConfigManager.Instance.OnConfigChanged -= OnConfigChanged;
            }
        }

        private void OnConfigChanged(GameConfig newConfig)
        {
            config = newConfig;
            Debug.Log($"[PlayerMovement] Config updated - New speed: {config.GetEffectivePlayerSpeed()} m/s");
        }

        private void Update()
        {
            // Example input (replace with your input system)
            HandleExampleInput();

            // Update stamina
            if (config.enableStamina)
            {
                UpdateStamina();
            }
        }

        private void FixedUpdate()
        {
            // Apply movement using config values
            ApplyMovementFromConfig();
        }

        /// <summary>
        /// Apply movement based on GameConfig settings
        /// </summary>
        private void ApplyMovementFromConfig()
        {
            // Get speed from config based on sprint state
            float baseSpeed = isSprinting ? config.GetSprintSpeed() : config.GetEffectivePlayerSpeed();

            // Apply stamina penalty if enabled
            if (config.enableStamina && currentStamina <= 0)
            {
                baseSpeed *= (1f - config.lowStaminaSpeedPenalty);
            }

            targetSpeed = baseSpeed;

            // Calculate desired velocity
            Vector3 targetVelocity = new Vector3(
                movementInput.x * targetSpeed,
                rb.velocity.y, // Preserve Y velocity for physics
                movementInput.y * targetSpeed
            );

            // Apply acceleration/deceleration from config
            float acceleration = movementInput.magnitude > 0.1f
                ? config.playerAcceleration
                : config.playerDeceleration;

            // Smoothly interpolate to target velocity
            Vector3 newVelocity = Vector3.MoveTowards(
                rb.velocity,
                targetVelocity,
                acceleration * Time.fixedDeltaTime
            );

            // Apply velocity
            rb.velocity = newVelocity;

            // Apply rotation if moving
            if (movementInput.magnitude > 0.1f)
            {
                ApplyRotationFromConfig();
            }

            // Track current speed for debug
            currentSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        }

        /// <summary>
        /// Apply rotation based on config turn speed
        /// </summary>
        private void ApplyRotationFromConfig()
        {
            Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y);
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                float turnSpeed = config.playerTurnSpeed;

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.fixedDeltaTime
                );
            }
        }

        /// <summary>
        /// Update stamina system from config
        /// </summary>
        private void UpdateStamina()
        {
            if (isSprinting && movementInput.magnitude > 0.1f)
            {
                // Drain stamina while sprinting
                currentStamina -= config.staminaDrainRate * Time.deltaTime;
                currentStamina = Mathf.Max(0, currentStamina);

                // Stop sprinting if out of stamina
                if (currentStamina <= 0)
                {
                    isSprinting = false;
                }
            }
            else
            {
                // Recover stamina when not sprinting
                currentStamina += config.staminaRecoveryRate * Time.deltaTime;
                currentStamina = Mathf.Min(config.maxStamina, currentStamina);
            }
        }

        /// <summary>
        /// Example input handling (replace with your input system)
        /// </summary>
        private void HandleExampleInput()
        {
            // WASD movement
            movementInput = new Vector2(
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Vertical")
            );

            // Sprint with Shift
            if (Input.GetKey(KeyCode.LeftShift) && currentStamina > 0)
            {
                isSprinting = true;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                isSprinting = false;
            }
        }

        /// <summary>
        /// Get current stamina percentage (0-1)
        /// </summary>
        public float GetStaminaPercentage()
        {
            if (!config.enableStamina) return 1f;
            return currentStamina / config.maxStamina;
        }

        /// <summary>
        /// Get current speed as percentage of max (useful for animations)
        /// </summary>
        public float GetSpeedPercentage()
        {
            float maxSpeed = config.GetSprintSpeed();
            return Mathf.Clamp01(currentSpeed / maxSpeed);
        }

        /// <summary>
        /// Check if player can sprint
        /// </summary>
        public bool CanSprint()
        {
            return !config.enableStamina || currentStamina > 0;
        }

        // Gizmos for debugging
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw velocity vector
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, rb.velocity);

            // Draw target direction
            Gizmos.color = Color.yellow;
            Vector3 targetDir = new Vector3(movementInput.x, 0, movementInput.y).normalized;
            Gizmos.DrawRay(transform.position, targetDir * 2f);
        }

        // Debug display
        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 100, 300, 200));
            GUILayout.Box("Player Movement Debug");
            GUILayout.Label($"Speed: {currentSpeed:F2} / {targetSpeed:F2} m/s");
            GUILayout.Label($"Sprinting: {isSprinting}");

            if (config.enableStamina)
            {
                GUILayout.Label($"Stamina: {currentStamina:F0} / {config.maxStamina:F0}");

                // Simple stamina bar
                Rect staminaBarBg = GUILayout.GetLastRect();
                staminaBarBg.y += 20;
                staminaBarBg.width = 200;
                staminaBarBg.height = 20;
                GUI.Box(staminaBarBg, "");

                Rect staminaBarFill = staminaBarBg;
                staminaBarFill.width *= GetStaminaPercentage();
                GUI.color = GetStaminaPercentage() > 0.3f ? Color.green : Color.red;
                GUI.Box(staminaBarFill, "");
                GUI.color = Color.white;
            }

            GUILayout.Label($"Config Speed Multiplier: {config.playerSpeedMultiplier:F2}x");
            GUILayout.Label($"Base Speed: {config.basePlayerSpeed:F2} m/s");
            GUILayout.Label($"Effective Speed: {config.GetEffectivePlayerSpeed():F2} m/s");
            GUILayout.EndArea();
        }
    }
}
