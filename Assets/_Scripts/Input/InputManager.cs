using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using System;

/// <summary>
/// Central input manager that handles input routing and control scheme switching.
/// Supports gamepad, keyboard, and touch controls with automatic switching.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private HockeyPlayer controlledPlayer;
    [SerializeField] private ShootingController shootingController;
    [SerializeField] private TouchControlsUI touchControlsUI;

    [Header("Settings")]
    [SerializeField] private bool preferControllerOverTouch = true;

    // Input Actions
    private HockeyInput inputActions;

    // State
    private InputControlScheme currentScheme;
    private bool isControllerConnected;
    private Vector2 currentMoveInput;
    private Vector2 currentAimInput;
    private bool isSprinting;

    // Events
    public event Action<bool> OnControllerConnectionChanged;
    public event Action<string> OnControlSchemeChanged;

    // Properties
    public bool IsControllerConnected => isControllerConnected;
    public string CurrentControlScheme => currentScheme.name;
    public Vector2 MoveInput => currentMoveInput;
    public Vector2 AimInput => currentAimInput;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize input actions
        inputActions = new HockeyInput();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        // Subscribe to input events
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Aim.performed += OnAim;
        inputActions.Player.Aim.canceled += OnAim;
        inputActions.Player.Shoot.started += OnShootStarted;
        inputActions.Player.Shoot.canceled += OnShootReleased;
        inputActions.Player.Pass.performed += OnPass;
        inputActions.Player.Dash.performed += OnDash;
        inputActions.Player.Dash.started += OnSprintStarted;
        inputActions.Player.Dash.canceled += OnSprintCanceled;

        // Subscribe to device changes
        InputSystem.onDeviceChange += OnDeviceChange;
        InputUser.onChange += OnInputUserChange;

        // Subscribe to TeamManager player switch events
        TeamManager tm = FindObjectOfType<TeamManager>();
        if (tm != null)
        {
            tm.OnPlayerSwitched += OnPlayerSwitched;
        }

        // Initial controller check
        CheckControllerConnection();
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Aim.performed -= OnAim;
        inputActions.Player.Aim.canceled -= OnAim;
        inputActions.Player.Shoot.started -= OnShootStarted;
        inputActions.Player.Shoot.canceled -= OnShootReleased;
        inputActions.Player.Pass.performed -= OnPass;
        inputActions.Player.Dash.performed -= OnDash;
        inputActions.Player.Dash.started -= OnSprintStarted;
        inputActions.Player.Dash.canceled -= OnSprintCanceled;

        InputSystem.onDeviceChange -= OnDeviceChange;
        InputUser.onChange -= OnInputUserChange;

        // Unsubscribe from TeamManager
        TeamManager tm = FindObjectOfType<TeamManager>();
        if (tm != null)
        {
            tm.OnPlayerSwitched -= OnPlayerSwitched;
        }

        inputActions.Disable();
    }

    private void Update()
    {
        // Continuously route input to player
        if (controlledPlayer != null)
        {
            controlledPlayer.SetMoveInput(currentMoveInput);
            controlledPlayer.SetAimInput(currentAimInput);
            controlledPlayer.SetSprintInput(isSprinting);
        }

        if (shootingController != null)
        {
            shootingController.SetAimDirection(currentAimInput);
        }
    }

    // === INPUT CALLBACKS ===

    private void OnMove(InputAction.CallbackContext context)
    {
        currentMoveInput = context.ReadValue<Vector2>();
    }

    private void OnAim(InputAction.CallbackContext context)
    {
        currentAimInput = context.ReadValue<Vector2>();
    }

    private void OnShootStarted(InputAction.CallbackContext context)
    {
        shootingController?.StartCharge();
    }

    private void OnShootReleased(InputAction.CallbackContext context)
    {
        shootingController?.ReleaseShot();
    }

    private void OnPass(InputAction.CallbackContext context)
    {
        shootingController?.Pass();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        controlledPlayer?.TriggerDash();
    }

    private void OnSprintStarted(InputAction.CallbackContext ctx)
    {
        isSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        isSprinting = false;
    }

    private void OnPlayerSwitched(HockeyPlayer newPlayer)
    {
        controlledPlayer = newPlayer;
    }

    // === DEVICE MANAGEMENT ===

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                    Debug.Log($"[InputManager] Gamepad connected: {device.displayName}");
                    isControllerConnected = true;
                    OnControllerConnectionChanged?.Invoke(true);
                    UpdateTouchControlsVisibility();
                    break;

                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                    Debug.Log($"[InputManager] Gamepad disconnected: {device.displayName}");
                    CheckControllerConnection(); // Check if any gamepads remain
                    break;
            }
        }
    }

    private void OnInputUserChange(InputUser user, InputUserChange change, InputDevice device)
    {
        if (change == InputUserChange.ControlSchemeChanged)
        {
            currentScheme = user.controlScheme ?? default;
            Debug.Log($"[InputManager] Control scheme changed to: {currentScheme.name}");
            OnControlSchemeChanged?.Invoke(currentScheme.name);
            UpdateTouchControlsVisibility();
        }
    }

    private void CheckControllerConnection()
    {
        isControllerConnected = Gamepad.current != null;
        OnControllerConnectionChanged?.Invoke(isControllerConnected);
        UpdateTouchControlsVisibility();

        Debug.Log($"[InputManager] Controller connected: {isControllerConnected}");
    }

    private void UpdateTouchControlsVisibility()
    {
        if (touchControlsUI == null) return;

        // Hide touch controls if controller is connected (and preference is set)
        bool showTouch = !isControllerConnected || !preferControllerOverTouch;

        // Also hide on non-touch platforms in editor
        #if UNITY_EDITOR
        showTouch = showTouch && (UnityEngine.Device.SystemInfo.deviceType == DeviceType.Handheld ||
                                   Input.touchSupported);
        #endif

        touchControlsUI.SetVisible(showTouch);
    }

    // === PUBLIC METHODS ===

    /// <summary>
    /// Set the player being controlled by this input manager.
    /// </summary>
    public void SetControlledPlayer(HockeyPlayer player)
    {
        controlledPlayer = player;
        shootingController = player?.GetComponent<ShootingController>();
    }

    /// <summary>
    /// Set move input directly (for touch controls).
    /// </summary>
    public void SetMoveInputDirect(Vector2 input)
    {
        currentMoveInput = input;
    }

    /// <summary>
    /// Set aim input directly (for touch controls).
    /// </summary>
    public void SetAimInputDirect(Vector2 input)
    {
        currentAimInput = input;
    }

    /// <summary>
    /// Trigger shoot start (for touch controls).
    /// </summary>
    public void TriggerShootStart()
    {
        shootingController?.StartCharge();
    }

    /// <summary>
    /// Trigger shoot release (for touch controls).
    /// </summary>
    public void TriggerShootRelease()
    {
        shootingController?.ReleaseShot();
    }

    /// <summary>
    /// Trigger pass (for touch controls).
    /// </summary>
    public void TriggerPass()
    {
        shootingController?.Pass();
    }

    /// <summary>
    /// Trigger dash (for touch controls).
    /// </summary>
    public void TriggerDash()
    {
        controlledPlayer?.TriggerDash();
    }

    /// <summary>
    /// Force show/hide touch controls.
    /// </summary>
    public void ForceShowTouchControls(bool show)
    {
        touchControlsUI?.SetVisible(show);
    }
}
