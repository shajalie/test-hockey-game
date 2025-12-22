using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Arcade-style animation utilities for UI elements.
/// Provides tweening, screen shake, pulse effects, and transitions.
/// </summary>
public class UIAnimator : MonoBehaviour
{
    private static UIAnimator _instance;
    public static UIAnimator Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("UIAnimator");
                _instance = obj.AddComponent<UIAnimator>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    #region Screen Transitions

    /// <summary>
    /// Fade a canvas group in.
    /// </summary>
    public static Coroutine FadeIn(CanvasGroup canvasGroup, float duration = -1, Action onComplete = null)
    {
        if (duration < 0) duration = ArcadeTheme.FadeInDuration;
        return Instance.StartCoroutine(FadeCoroutine(canvasGroup, 0f, 1f, duration, onComplete));
    }

    /// <summary>
    /// Fade a canvas group out.
    /// </summary>
    public static Coroutine FadeOut(CanvasGroup canvasGroup, float duration = -1, Action onComplete = null)
    {
        if (duration < 0) duration = ArcadeTheme.FadeOutDuration;
        return Instance.StartCoroutine(FadeCoroutine(canvasGroup, 1f, 0f, duration, onComplete));
    }

    private static IEnumerator FadeCoroutine(CanvasGroup canvasGroup, float from, float to, float duration, Action onComplete)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(from, to, EaseOutQuad(t));
            yield return null;
        }

        canvasGroup.alpha = to;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Slide a RectTransform in from a direction.
    /// </summary>
    public static Coroutine SlideIn(RectTransform rect, SlideDirection direction, float duration = -1, Action onComplete = null)
    {
        if (duration < 0) duration = ArcadeTheme.TransitionDuration;
        Vector2 startOffset = GetSlideOffset(direction, rect);
        return Instance.StartCoroutine(SlideCoroutine(rect, startOffset, Vector2.zero, duration, onComplete));
    }

    /// <summary>
    /// Slide a RectTransform out to a direction.
    /// </summary>
    public static Coroutine SlideOut(RectTransform rect, SlideDirection direction, float duration = -1, Action onComplete = null)
    {
        if (duration < 0) duration = ArcadeTheme.TransitionDuration;
        Vector2 endOffset = GetSlideOffset(direction, rect);
        return Instance.StartCoroutine(SlideCoroutine(rect, Vector2.zero, endOffset, duration, onComplete));
    }

    private static Vector2 GetSlideOffset(SlideDirection direction, RectTransform rect)
    {
        float offset = 1200f; // Slide distance
        switch (direction)
        {
            case SlideDirection.Left: return new Vector2(-offset, 0);
            case SlideDirection.Right: return new Vector2(offset, 0);
            case SlideDirection.Up: return new Vector2(0, offset);
            case SlideDirection.Down: return new Vector2(0, -offset);
            default: return Vector2.zero;
        }
    }

    private static IEnumerator SlideCoroutine(RectTransform rect, Vector2 from, Vector2 to, float duration, Action onComplete)
    {
        if (rect == null) yield break;

        float elapsed = 0f;
        Vector2 originalPos = rect.anchoredPosition;
        Vector2 startPos = originalPos + from;
        Vector2 endPos = originalPos + to;

        rect.anchoredPosition = startPos;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, EaseOutBack(t));
            yield return null;
        }

        rect.anchoredPosition = endPos;
        onComplete?.Invoke();
    }

    #endregion

    #region Button Effects

    /// <summary>
    /// Animate button press (scale down then up).
    /// </summary>
    public static Coroutine PressButton(Transform button, Action onComplete = null)
    {
        return Instance.StartCoroutine(PressButtonCoroutine(button, onComplete));
    }

    private static IEnumerator PressButtonCoroutine(Transform button, Action onComplete)
    {
        if (button == null) yield break;

        float duration = ArcadeTheme.ButtonPressDuration;
        Vector3 originalScale = button.localScale;
        Vector3 pressedScale = originalScale * ArcadeTheme.ButtonPressScale;
        Vector3 bounceScale = originalScale * ArcadeTheme.ButtonReleaseScale;

        // Press down
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            button.localScale = Vector3.Lerp(originalScale, pressedScale, t);
            yield return null;
        }

        // Bounce up
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            button.localScale = Vector3.Lerp(pressedScale, bounceScale, EaseOutQuad(t));
            yield return null;
        }

        // Return to normal
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (duration * 0.5f);
            button.localScale = Vector3.Lerp(bounceScale, originalScale, t);
            yield return null;
        }

        button.localScale = originalScale;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Pulse a button scale continuously.
    /// </summary>
    public static Coroutine PulseButton(Transform button, float scaleAmount = 0.05f, float duration = -1)
    {
        if (duration < 0) duration = ArcadeTheme.PulseDuration;
        return Instance.StartCoroutine(PulseScaleCoroutine(button, scaleAmount, duration, true));
    }

    /// <summary>
    /// Stop pulsing by setting scale back to 1.
    /// </summary>
    public static void StopPulse(Transform button)
    {
        if (button != null)
        {
            button.localScale = Vector3.one;
        }
    }

    #endregion

    #region Shake Effects

    /// <summary>
    /// Shake the screen (or a specific transform).
    /// </summary>
    public static Coroutine ScreenShake(float intensity = -1, float duration = -1)
    {
        if (intensity < 0) intensity = ArcadeTheme.ShakeIntensity;
        if (duration < 0) duration = ArcadeTheme.ShakeDuration;

        // Find main canvas or camera
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas != null)
        {
            return Shake(mainCanvas.transform, intensity, duration);
        }
        return null;
    }

    /// <summary>
    /// Shake a specific transform.
    /// </summary>
    public static Coroutine Shake(Transform target, float intensity, float duration)
    {
        return Instance.StartCoroutine(ShakeCoroutine(target, intensity, duration));
    }

    private static IEnumerator ShakeCoroutine(Transform target, float intensity, float duration)
    {
        if (target == null) yield break;

        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float remaining = 1f - (elapsed / duration);

            float x = UnityEngine.Random.Range(-1f, 1f) * intensity * remaining;
            float y = UnityEngine.Random.Range(-1f, 1f) * intensity * remaining;

            target.localPosition = originalPos + new Vector3(x, y, 0);
            yield return null;
        }

        target.localPosition = originalPos;
    }

    #endregion

    #region Continuous Animations

    /// <summary>
    /// Float up and down continuously.
    /// </summary>
    public static Coroutine FloatUpDown(Transform target, float amplitude = 10f, float frequency = 1f)
    {
        return Instance.StartCoroutine(FloatCoroutine(target, amplitude, frequency));
    }

    private static IEnumerator FloatCoroutine(Transform target, float amplitude, float frequency)
    {
        if (target == null) yield break;

        Vector3 startPos = target.localPosition;
        float time = 0f;

        while (target != null)
        {
            time += Time.deltaTime * frequency;
            float yOffset = Mathf.Sin(time * Mathf.PI * 2) * amplitude;
            target.localPosition = startPos + new Vector3(0, yOffset, 0);
            yield return null;
        }
    }

    /// <summary>
    /// Rotate continuously.
    /// </summary>
    public static Coroutine RotateContinuous(Transform target, float speed = 30f)
    {
        return Instance.StartCoroutine(RotateCoroutine(target, speed));
    }

    private static IEnumerator RotateCoroutine(Transform target, float speed)
    {
        while (target != null)
        {
            target.Rotate(0, 0, speed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// Pulse scale continuously.
    /// </summary>
    public static Coroutine PulseScale(Transform target, float amount = 0.1f, float duration = -1)
    {
        if (duration < 0) duration = ArcadeTheme.PulseDuration;
        return Instance.StartCoroutine(PulseScaleCoroutine(target, amount, duration, true));
    }

    private static IEnumerator PulseScaleCoroutine(Transform target, float amount, float duration, bool loop)
    {
        if (target == null) yield break;

        Vector3 baseScale = target.localScale;
        Vector3 maxScale = baseScale * (1f + amount);

        do
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed / duration * 2f, 1f);
                target.localScale = Vector3.Lerp(baseScale, maxScale, t);
                yield return null;
            }
        } while (loop && target != null);
    }

    /// <summary>
    /// Pulse image alpha (glow effect).
    /// </summary>
    public static Coroutine PulseGlow(Image image, float minAlpha = 0.3f, float maxAlpha = 1f, float speed = 1f)
    {
        return Instance.StartCoroutine(PulseGlowCoroutine(image, minAlpha, maxAlpha, speed));
    }

    private static IEnumerator PulseGlowCoroutine(Image image, float minAlpha, float maxAlpha, float speed)
    {
        if (image == null) yield break;

        Color baseColor = image.color;
        float time = 0f;

        while (image != null)
        {
            time += Time.deltaTime * speed;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(time * Mathf.PI * 2) + 1f) / 2f);
            image.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }
    }

    #endregion

    #region Text Effects

    /// <summary>
    /// Typewriter text reveal effect.
    /// </summary>
    public static Coroutine TypewriterText(TextMeshProUGUI text, string content, float charDelay = 0.03f, Action onComplete = null)
    {
        return Instance.StartCoroutine(TypewriterCoroutine(text, content, charDelay, onComplete));
    }

    private static IEnumerator TypewriterCoroutine(TextMeshProUGUI text, string content, float charDelay, Action onComplete)
    {
        if (text == null) yield break;

        text.text = "";
        for (int i = 0; i < content.Length; i++)
        {
            text.text += content[i];
            yield return new WaitForSecondsRealtime(charDelay);
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// Flash text color.
    /// </summary>
    public static Coroutine FlashText(TextMeshProUGUI text, Color flashColor, float duration = 0.3f)
    {
        return Instance.StartCoroutine(FlashTextCoroutine(text, flashColor, duration));
    }

    private static IEnumerator FlashTextCoroutine(TextMeshProUGUI text, Color flashColor, float duration)
    {
        if (text == null) yield break;

        Color originalColor = text.color;
        text.color = flashColor;
        yield return new WaitForSecondsRealtime(duration);
        text.color = originalColor;
    }

    #endregion

    #region Color Effects

    /// <summary>
    /// Flash an image color then return.
    /// </summary>
    public static Coroutine FlashColor(Image image, Color flashColor, float duration = 0.2f)
    {
        return Instance.StartCoroutine(FlashColorCoroutine(image, flashColor, duration));
    }

    private static IEnumerator FlashColorCoroutine(Image image, Color flashColor, float duration)
    {
        if (image == null) yield break;

        Color originalColor = image.color;

        // Flash to color
        float halfDuration = duration / 2f;
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            image.color = Color.Lerp(originalColor, flashColor, t);
            yield return null;
        }

        // Return to original
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            image.color = Color.Lerp(flashColor, originalColor, t);
            yield return null;
        }

        image.color = originalColor;
    }

    /// <summary>
    /// Lerp color over time.
    /// </summary>
    public static Coroutine LerpColor(Image image, Color targetColor, float duration, Action onComplete = null)
    {
        return Instance.StartCoroutine(LerpColorCoroutine(image, targetColor, duration, onComplete));
    }

    private static IEnumerator LerpColorCoroutine(Image image, Color targetColor, float duration, Action onComplete)
    {
        if (image == null) yield break;

        Color startColor = image.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        image.color = targetColor;
        onComplete?.Invoke();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Stop all coroutines (use carefully).
    /// </summary>
    public static void StopAllAnimations()
    {
        if (_instance != null)
        {
            _instance.StopAllCoroutines();
        }
    }

    /// <summary>
    /// Stop a specific animation coroutine.
    /// </summary>
    public static void StopAnimation(Coroutine coroutine)
    {
        if (_instance != null && coroutine != null)
        {
            _instance.StopCoroutine(coroutine);
        }
    }

    #endregion

    #region Easing Functions

    private static float EaseOutQuad(float t)
    {
        return 1 - (1 - t) * (1 - t);
    }

    private static float EaseInQuad(float t)
    {
        return t * t;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }

    private static float EaseOutElastic(float t)
    {
        const float c4 = (2 * Mathf.PI) / 3;
        if (t == 0) return 0;
        if (t == 1) return 1;
        return Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;
    }

    private static float EaseInOutSine(float t)
    {
        return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
    }

    #endregion
}

/// <summary>
/// Direction for slide animations.
/// </summary>
public enum SlideDirection
{
    Left,
    Right,
    Up,
    Down
}
