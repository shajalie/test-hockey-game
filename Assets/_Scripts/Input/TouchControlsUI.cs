using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Mobile touch controls UI with virtual joysticks and action buttons.
/// Integrates with InputManager for seamless touch/controller switching.
/// </summary>
public class TouchControlsUI : MonoBehaviour
{
    [Header("Joysticks")]
    [SerializeField] private VirtualJoystick moveJoystick;
    [SerializeField] private VirtualJoystick aimJoystick;

    [Header("Action Buttons")]
    [SerializeField] private TouchButton shootButton;
    [SerializeField] private TouchButton passButton;
    [SerializeField] private TouchButton dashButton;

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeSpeed = 5f;

    private bool isVisible = true;
    private float targetAlpha = 1f;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Update()
    {
        // Smooth fade
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        // Route joystick input to InputManager
        if (isVisible && InputManager.Instance != null)
        {
            if (moveJoystick != null)
            {
                InputManager.Instance.SetMoveInputDirect(moveJoystick.Direction);
            }

            if (aimJoystick != null)
            {
                InputManager.Instance.SetAimInputDirect(aimJoystick.Direction);
            }
        }
    }

    /// <summary>
    /// Show or hide the touch controls.
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        targetAlpha = visible ? 1f : 0f;

        Debug.Log($"[TouchControlsUI] Visibility set to: {visible}");
    }

    /// <summary>
    /// Toggle visibility.
    /// </summary>
    public void ToggleVisibility()
    {
        SetVisible(!isVisible);
    }

    // === BUTTON CALLBACKS (Assign in Inspector) ===

    public void OnShootButtonDown()
    {
        InputManager.Instance?.TriggerShootStart();
    }

    public void OnShootButtonUp()
    {
        InputManager.Instance?.TriggerShootRelease();
    }

    public void OnPassButtonPressed()
    {
        InputManager.Instance?.TriggerPass();
    }

    public void OnDashButtonPressed()
    {
        InputManager.Instance?.TriggerDash();
    }
}

/// <summary>
/// Virtual joystick component for touch input.
/// Attach to a UI Image that acts as the joystick base.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Components")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [SerializeField] private float handleRange = 50f;
    [SerializeField] private float deadzone = 0.1f;
    [SerializeField] private bool snapToFinger = true;

    private Vector2 inputVector = Vector2.zero;
    private Vector2 startPosition;
    private Canvas canvas;
    private Camera canvasCamera;

    public Vector2 Direction => inputVector;
    public float Magnitude => inputVector.magnitude;

    private void Awake()
    {
        startPosition = background.anchoredPosition;
        canvas = GetComponentInParent<Canvas>();

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvasCamera = canvas.worldCamera;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (snapToFinger)
        {
            // Move joystick to touch position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvasCamera,
                out Vector2 localPoint
            );
            background.anchoredPosition = localPoint;
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            canvasCamera,
            out Vector2 localPoint
        );

        // Normalize to -1 to 1 range
        Vector2 normalizedInput = localPoint / (background.sizeDelta / 2f);

        // Clamp to circle
        inputVector = Vector2.ClampMagnitude(normalizedInput, 1f);

        // Apply deadzone
        if (inputVector.magnitude < deadzone)
        {
            inputVector = Vector2.zero;
        }
        else
        {
            // Remap to remove deadzone gap
            inputVector = inputVector.normalized * ((inputVector.magnitude - deadzone) / (1f - deadzone));
        }

        // Move handle visual
        if (handle != null)
        {
            handle.anchoredPosition = inputVector * handleRange;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }

        if (snapToFinger)
        {
            // Return to original position
            background.anchoredPosition = startPosition;
        }
    }
}

/// <summary>
/// Touch button component with press and release events.
/// </summary>
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Events")]
    public UnityEngine.Events.UnityEvent onButtonDown;
    public UnityEngine.Events.UnityEvent onButtonUp;

    [Header("Visual Feedback")]
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Visual feedback
        transform.localScale = originalScale * pressedScale;
        if (buttonImage != null)
        {
            buttonImage.color = pressedColor;
        }

        // Invoke event
        onButtonDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset visual
        transform.localScale = originalScale;
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }

        // Invoke event
        onButtonUp?.Invoke();
    }
}
