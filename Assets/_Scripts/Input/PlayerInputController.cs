using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Unified input controller for a hockey player.
/// Routes input to all player systems (physics, stick, shooting, passing).
/// Can be controlled by human input or AI.
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private IcePhysicsController physicsController;
    [SerializeField] private PlayerStickHandler stickHandler;

    [Header("Input Settings")]
    [SerializeField] private bool acceptInput = true;
    [SerializeField] private float aimDeadzone = 0.2f;

    [Header("Shot Settings")]
    [SerializeField] private float slapShotHoldTime = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showInputDebug = false;

    #endregion

    #region Private Fields

    // Input state
    private Vector2 moveInput;
    private Vector2 aimInput;
    private bool sprintHeld;
    private bool shootHeld;
    private float shootHoldTimer;
    private bool passPressed;

    // AI control
    private bool isAIControlled;
    private Vector2 aiMoveInput;
    private Vector2 aiAimInput;

    #endregion

    #region Properties

    /// <summary>Whether this player accepts human input.</summary>
    public bool AcceptInput
    {
        get => acceptInput;
        set => acceptInput = value;
    }

    /// <summary>Whether AI is controlling this player.</summary>
    public bool IsAIControlled
    {
        get => isAIControlled;
        set => isAIControlled = value;
    }

    /// <summary>Current move input (human or AI).</summary>
    public Vector2 CurrentMoveInput => isAIControlled ? aiMoveInput : moveInput;

    /// <summary>Current aim input (human or AI).</summary>
    public Vector2 CurrentAimInput => isAIControlled ? aiAimInput : aimInput;

    /// <summary>The physics controller.</summary>
    public IcePhysicsController Physics => physicsController;

    /// <summary>The stick handler.</summary>
    public PlayerStickHandler Stick => stickHandler;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (physicsController == null)
        {
            physicsController = GetComponent<IcePhysicsController>();
        }

        if (stickHandler == null)
        {
            stickHandler = GetComponent<PlayerStickHandler>();
        }
    }

    private void Update()
    {
        if (!acceptInput && !isAIControlled) return;

        // Apply movement
        Vector2 move = CurrentMoveInput;
        physicsController?.SetMoveInput(move);

        // Apply aim
        Vector2 aim = CurrentAimInput;
        if (aim.magnitude > aimDeadzone)
        {
            stickHandler?.SetAimDirection(aim);
        }

        // Apply sprint
        physicsController?.SetSprintInput(sprintHeld);

        // Update shot charging
        if (shootHeld)
        {
            shootHoldTimer += Time.deltaTime;
        }

        // Update passing system with aim
        if (stickHandler?.Passing != null)
        {
            stickHandler.Passing.SetAimDirection(aim);
        }

        if (showInputDebug)
        {
            DrawInputDebug();
        }
    }

    #endregion

    #region Human Input (New Input System)

    /// <summary>Called by PlayerInput component - Move action.</summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>Called by PlayerInput component - Aim action.</summary>
    public void OnAim(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;
        aimInput = context.ReadValue<Vector2>();
    }

    /// <summary>Called by PlayerInput component - Sprint action.</summary>
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;

        if (context.started || context.performed)
        {
            sprintHeld = true;
        }
        else if (context.canceled)
        {
            sprintHeld = false;
        }
    }

    /// <summary>Called by PlayerInput component - Dash action.</summary>
    public void OnDash(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;

        if (context.started)
        {
            physicsController?.TriggerDash();
        }
    }

    /// <summary>Called by PlayerInput component - Shoot action.</summary>
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;

        if (context.started)
        {
            shootHeld = true;
            shootHoldTimer = 0f;

            // Start charging wrist shot
            stickHandler?.StartShotCharge(ShootingSystem.ShotType.Wrist);
        }
        else if (context.canceled)
        {
            shootHeld = false;

            // Determine shot type based on hold time
            if (shootHoldTimer >= slapShotHoldTime)
            {
                // Switch to slap shot before release
                stickHandler?.StartShotCharge(ShootingSystem.ShotType.Slap);
            }

            stickHandler?.ReleaseShotCharge();
        }
    }

    /// <summary>Called by PlayerInput component - Pass action.</summary>
    public void OnPass(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;

        if (context.started)
        {
            // Execute smart pass
            bool passed = stickHandler?.SmartPass() ?? false;

            if (!passed && stickHandler?.HasPuck == true)
            {
                // No valid target - dump puck forward
                Vector3 dumpDir = transform.forward;
                stickHandler?.Shoot(dumpDir, 15f);
            }
        }
    }

    /// <summary>Called by PlayerInput component - Poke Check action.</summary>
    public void OnPokeCheck(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;

        if (context.started)
        {
            stickHandler?.PokeCheck();
        }
    }

    /// <summary>Called by PlayerInput component - Deke Left action.</summary>
    public void OnDekeLeft(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;

        if (context.started)
        {
            stickHandler?.StartDeke(-1);
        }
        else if (context.canceled)
        {
            stickHandler?.CancelDeke();
        }
    }

    /// <summary>Called by PlayerInput component - Deke Right action.</summary>
    public void OnDekeRight(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;

        if (context.started)
        {
            stickHandler?.StartDeke(1);
        }
        else if (context.canceled)
        {
            stickHandler?.CancelDeke();
        }
    }

    /// <summary>Called by PlayerInput component - Switch Player action.</summary>
    public void OnSwitchPlayer(InputAction.CallbackContext context)
    {
        if (!acceptInput) return;

        if (context.started)
        {
            // Request player switch through team manager
            TeamController.Instance?.RequestPlayerSwitch();
        }
    }

    #endregion

    #region AI Input

    /// <summary>
    /// Set movement input from AI.
    /// </summary>
    public void SetAIMoveInput(Vector2 input)
    {
        aiMoveInput = input;
    }

    /// <summary>
    /// Set aim input from AI.
    /// </summary>
    public void SetAIAimInput(Vector2 input)
    {
        aiAimInput = input;
    }

    /// <summary>
    /// Trigger dash from AI.
    /// </summary>
    public void AITriggerDash()
    {
        physicsController?.TriggerDash();
    }

    /// <summary>
    /// Execute pass from AI.
    /// </summary>
    public void AIPass(GameObject target = null)
    {
        if (target != null)
        {
            stickHandler?.Pass(target, 15f);
        }
        else
        {
            stickHandler?.SmartPass();
        }
    }

    /// <summary>
    /// Execute shot from AI.
    /// </summary>
    public void AIShoot(Vector3 direction, float power)
    {
        stickHandler?.Shoot(direction, power);
    }

    /// <summary>
    /// Execute poke check from AI.
    /// </summary>
    public void AIPokeCheck()
    {
        stickHandler?.PokeCheck();
    }

    #endregion

    #region Control Transfer

    /// <summary>
    /// Enable human control.
    /// </summary>
    public void EnableHumanControl()
    {
        acceptInput = true;
        isAIControlled = false;

        Debug.Log($"[PlayerInputController] {gameObject.name} - Human control enabled");
    }

    /// <summary>
    /// Enable AI control.
    /// </summary>
    public void EnableAIControl()
    {
        acceptInput = false;
        isAIControlled = true;

        // Clear human input state
        moveInput = Vector2.zero;
        aimInput = Vector2.zero;
        sprintHeld = false;
        shootHeld = false;

        Debug.Log($"[PlayerInputController] {gameObject.name} - AI control enabled");
    }

    /// <summary>
    /// Disable all control.
    /// </summary>
    public void DisableControl()
    {
        acceptInput = false;
        isAIControlled = false;

        // Stop movement
        physicsController?.SetMoveInput(Vector2.zero);
        physicsController?.SetSprintInput(false);

        Debug.Log($"[PlayerInputController] {gameObject.name} - Control disabled");
    }

    #endregion

    #region Debug

    private void DrawInputDebug()
    {
        Vector3 pos = transform.position + Vector3.up * 2.5f;

        // Move input
        Vector3 moveDir = new Vector3(CurrentMoveInput.x, 0, CurrentMoveInput.y);
        Debug.DrawRay(pos, moveDir * 2f, Color.green);

        // Aim input
        Vector3 aimDir = new Vector3(CurrentAimInput.x, 0, CurrentAimInput.y);
        Debug.DrawRay(pos, aimDir * 2f, Color.yellow);
    }

    private void OnGUI()
    {
        if (!showInputDebug) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
        if (screenPos.z > 0)
        {
            float x = screenPos.x - 50;
            float y = Screen.height - screenPos.y;

            string text = $"Move: {CurrentMoveInput}\nAim: {CurrentAimInput}\n";
            text += isAIControlled ? "[AI]" : (acceptInput ? "[HUMAN]" : "[DISABLED]");

            GUI.Label(new Rect(x, y, 120, 60), text, style);
        }
    }

    #endregion
}
