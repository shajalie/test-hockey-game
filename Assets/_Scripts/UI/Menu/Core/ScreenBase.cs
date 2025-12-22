using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Abstract base class for all menu screens.
/// Provides common functionality for lifecycle, transitions, and navigation.
/// </summary>
public abstract class ScreenBase : MonoBehaviour
{
    #region Fields

    protected MenuManager menuManager;
    protected Canvas canvas;
    protected CanvasGroup canvasGroup;
    protected GameObject screenRoot;
    protected Image background;

    private bool isInitialized = false;
    private bool isVisible = false;
    private Coroutine transitionCoroutine;

    #endregion

    #region Properties

    /// <summary>
    /// Whether this screen is currently visible.
    /// </summary>
    public bool IsVisible => isVisible;

    /// <summary>
    /// Screen name for debugging and identification.
    /// </summary>
    public abstract string ScreenName { get; }

    /// <summary>
    /// Background color for this screen (override in derived classes).
    /// </summary>
    protected virtual Color BackgroundColor => ArcadeTheme.Primary;

    /// <summary>
    /// Whether to show back button by default.
    /// </summary>
    protected virtual bool ShowBackButton => true;

    /// <summary>
    /// Transition duration for this screen.
    /// </summary>
    protected virtual float TransitionDuration => ArcadeTheme.TransitionDuration;

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the screen with menu manager reference.
    /// Called once when the screen is first created.
    /// </summary>
    public void Initialize(MenuManager manager, Canvas parentCanvas)
    {
        if (isInitialized) return;

        menuManager = manager;
        canvas = parentCanvas;

        // Create screen container
        CreateScreenContainer();

        // Build UI (implemented by derived classes)
        BuildUI();

        // Create back button if needed
        if (ShowBackButton)
        {
            UIFactory.CreateBackButton(screenRoot.transform, OnBackPressed);
        }

        isInitialized = true;
        Debug.Log($"[{ScreenName}] Initialized");
    }

    /// <summary>
    /// Create the screen's root container.
    /// </summary>
    private void CreateScreenContainer()
    {
        screenRoot = UIFactory.CreateFullScreenContainer(ScreenName, canvas.transform);

        // Add canvas group for fading
        canvasGroup = screenRoot.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Add background
        background = UIFactory.CreateBackground(screenRoot.transform, BackgroundColor);
        background.transform.SetAsFirstSibling();

        // Start hidden
        screenRoot.SetActive(false);
    }

    /// <summary>
    /// Build the screen's UI elements.
    /// Override in derived classes.
    /// </summary>
    protected abstract void BuildUI();

    #endregion

    #region Show / Hide

    /// <summary>
    /// Show this screen with optional transition.
    /// </summary>
    public virtual void Show(bool animated = true)
    {
        if (isVisible) return;

        Debug.Log($"[{ScreenName}] Showing");

        // Cancel any ongoing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        screenRoot.SetActive(true);
        isVisible = true;

        // Refresh data before showing
        OnBeforeShow();

        if (animated)
        {
            transitionCoroutine = StartCoroutine(ShowTransition());
        }
        else
        {
            canvasGroup.alpha = 1f;
            OnAfterShow();
        }
    }

    /// <summary>
    /// Hide this screen with optional transition.
    /// </summary>
    public virtual void Hide(bool animated = true)
    {
        if (!isVisible) return;

        Debug.Log($"[{ScreenName}] Hiding");

        // Cancel any ongoing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        OnBeforeHide();

        if (animated)
        {
            transitionCoroutine = StartCoroutine(HideTransition());
        }
        else
        {
            canvasGroup.alpha = 0f;
            screenRoot.SetActive(false);
            isVisible = false;
            OnAfterHide();
        }
    }

    /// <summary>
    /// Default show transition (fade in).
    /// Override for custom transitions.
    /// </summary>
    protected virtual IEnumerator ShowTransition()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < TransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / TransitionDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, EaseOutQuad(t));
            yield return null;
        }

        canvasGroup.alpha = 1f;
        transitionCoroutine = null;
        OnAfterShow();
    }

    /// <summary>
    /// Default hide transition (fade out).
    /// Override for custom transitions.
    /// </summary>
    protected virtual IEnumerator HideTransition()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < TransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / TransitionDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        screenRoot.SetActive(false);
        isVisible = false;
        transitionCoroutine = null;
        OnAfterHide();
    }

    #endregion

    #region Lifecycle Hooks

    /// <summary>
    /// Called before the screen is shown.
    /// Use for refreshing data.
    /// </summary>
    protected virtual void OnBeforeShow() { }

    /// <summary>
    /// Called after the show transition completes.
    /// </summary>
    protected virtual void OnAfterShow() { }

    /// <summary>
    /// Called before the screen is hidden.
    /// </summary>
    protected virtual void OnBeforeHide() { }

    /// <summary>
    /// Called after the hide transition completes.
    /// </summary>
    protected virtual void OnAfterHide() { }

    #endregion

    #region Navigation

    /// <summary>
    /// Handle back button press.
    /// Default behavior: navigate to previous screen.
    /// </summary>
    protected virtual void OnBackPressed()
    {
        Debug.Log($"[{ScreenName}] Back pressed");
        menuManager?.NavigateBack();
    }

    /// <summary>
    /// Navigate to another screen.
    /// </summary>
    protected void NavigateTo<T>() where T : ScreenBase
    {
        menuManager?.NavigateTo<T>();
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Handle input while this screen is active.
    /// Called from MenuManager each frame.
    /// </summary>
    public virtual void HandleInput()
    {
        // Check for escape/back key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackPressed();
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// Get the screen root transform for adding elements.
    /// </summary>
    protected Transform GetRoot()
    {
        return screenRoot.transform;
    }

    /// <summary>
    /// EaseOutQuad for smooth transitions.
    /// </summary>
    protected float EaseOutQuad(float t)
    {
        return 1 - (1 - t) * (1 - t);
    }

    #endregion
}
