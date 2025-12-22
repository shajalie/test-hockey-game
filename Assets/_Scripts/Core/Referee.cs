using UnityEngine;
using System.Collections;

/// <summary>
/// Hockey referee with striped jersey, whistle sounds, and game management behavior.
/// Moves to faceoff circles, blows whistle, and follows play.
/// </summary>
public class Referee : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip whistleClip;
    [SerializeField] private float whistleVolume = 0.8f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float followDistance = 15f;
    [SerializeField] private float faceoffDistance = 3f;

    [Header("Visual")]
    [SerializeField] private float spriteScale = 2f;
    [SerializeField] private float spriteYOffset = 0.5f;

    // Runtime references
    private SpriteRenderer spriteRenderer;
    private GameObject spriteObject;
    private AudioSource audioSource;
    private Puck puck;
    private Camera mainCamera;

    // State
    private Vector3 targetPosition;
    private bool isMovingToFaceoff;
    private bool isAtFaceoff;

    private void Awake()
    {
        mainCamera = Camera.main;
        puck = FindObjectOfType<Puck>();

        // Create audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f;
    }

    private void Start()
    {
        CreateRefereeSprite();
        targetPosition = transform.position;
    }

    private void OnEnable()
    {
        // Subscribe to game events
        GameEvents.OnGoalScored += OnGoalScored;
        GameEvents.OnFaceoffStart += OnFaceoffStart;
        GameEvents.OnWhistle += OnWhistle;
    }

    private void OnDisable()
    {
        GameEvents.OnGoalScored -= OnGoalScored;
        GameEvents.OnFaceoffStart -= OnFaceoffStart;
        GameEvents.OnWhistle -= OnWhistle;
    }

    private void Update()
    {
        UpdateMovement();
        UpdateBillboard();

        // Follow puck during play (loosely)
        if (!isMovingToFaceoff && !isAtFaceoff && puck != null)
        {
            FollowPlay();
        }
    }

    /// <summary>
    /// Creates the referee sprite using PixelArtGenerator.
    /// </summary>
    private void CreateRefereeSprite()
    {
        spriteObject = new GameObject("RefereeSprite");
        spriteObject.transform.SetParent(transform);
        spriteObject.transform.localPosition = new Vector3(0, spriteYOffset, 0);
        spriteObject.transform.localScale = Vector3.one * spriteScale;

        spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 10;

        // Generate referee texture
        Texture2D tex = PixelArtGenerator.GenerateRefereeSprite(true);
        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0), 64);
    }

    /// <summary>
    /// Billboard sprite toward camera.
    /// </summary>
    private void UpdateBillboard()
    {
        if (spriteObject == null || mainCamera == null) return;

        Vector3 lookDir = mainCamera.transform.forward;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            spriteObject.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    /// <summary>
    /// Move toward target position.
    /// </summary>
    private void UpdateMovement()
    {
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (isMovingToFaceoff)
            {
                isMovingToFaceoff = false;
                isAtFaceoff = true;
            }
            return;
        }

        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Follow play loosely during active gameplay.
    /// </summary>
    private void FollowPlay()
    {
        if (puck == null) return;

        Vector3 puckPos = puck.transform.position;
        Vector3 dirToPuck = puckPos - transform.position;
        dirToPuck.y = 0;

        // Stay at follow distance from puck
        if (dirToPuck.magnitude > followDistance)
        {
            targetPosition = puckPos - dirToPuck.normalized * followDistance * 0.7f;
            targetPosition.y = 0;
        }
    }

    /// <summary>
    /// Move to faceoff circle position.
    /// </summary>
    public void MoveToFaceoffCircle(Vector3 circlePosition)
    {
        // Position behind/beside the faceoff circle
        Vector3 offset = new Vector3(0, 0, -faceoffDistance);
        targetPosition = circlePosition + offset;
        targetPosition.y = 0;
        isMovingToFaceoff = true;
        isAtFaceoff = false;
    }

    /// <summary>
    /// Blow the whistle.
    /// </summary>
    public void BlowWhistle()
    {
        Debug.Log("[Referee] *TWEET* Whistle blown!");

        if (whistleClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(whistleClip, whistleVolume);
        }
        else
        {
            // Fallback: Generate procedural whistle sound
            StartCoroutine(ProceduralWhistle());
        }

        // Arm raise animation would go here
        StartCoroutine(ArmRaiseAnimation());
    }

    /// <summary>
    /// Procedural whistle sound fallback.
    /// </summary>
    private IEnumerator ProceduralWhistle()
    {
        // Create a simple beep sound
        int sampleRate = 44100;
        int samples = sampleRate / 4; // 0.25 seconds

        AudioClip clip = AudioClip.Create("Whistle", samples, 1, sampleRate, false);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            // High-pitched whistle frequency (around 3000-4000 Hz)
            float freq = 3500f + Mathf.Sin(t * 50f) * 200f;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;

            // Fade in/out
            float envelope = Mathf.Clamp01(i / 1000f) * Mathf.Clamp01((samples - i) / 1000f);
            data[i] *= envelope;
        }

        clip.SetData(data, 0);
        audioSource.PlayOneShot(clip, whistleVolume);

        yield return new WaitForSeconds(0.3f);
        Destroy(clip);
    }

    /// <summary>
    /// Simple arm raise animation (scale pulse for now).
    /// </summary>
    private IEnumerator ArmRaiseAnimation()
    {
        if (spriteObject == null) yield break;

        Vector3 originalScale = spriteObject.transform.localScale;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
            spriteObject.transform.localScale = originalScale * scale;
            yield return null;
        }

        spriteObject.transform.localScale = originalScale;
    }

    /// <summary>
    /// Signal goal scored.
    /// </summary>
    public void SignalGoal()
    {
        BlowWhistle();
        Debug.Log("[Referee] GOAL!");

        // Point to goal animation would go here
        StartCoroutine(GoalAnimation());
    }

    private IEnumerator GoalAnimation()
    {
        // Jump/celebration animation
        if (spriteObject == null) yield break;

        Vector3 originalPos = spriteObject.transform.localPosition;

        for (int i = 0; i < 3; i++)
        {
            spriteObject.transform.localPosition = originalPos + Vector3.up * 0.3f;
            yield return new WaitForSeconds(0.1f);
            spriteObject.transform.localPosition = originalPos;
            yield return new WaitForSeconds(0.1f);
        }
    }

    #region Event Handlers

    private void OnGoalScored(int teamIndex)
    {
        SignalGoal();
    }

    private void OnFaceoffStart(Vector3 position)
    {
        MoveToFaceoffCircle(position);
    }

    private void OnWhistle()
    {
        BlowWhistle();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Reset referee to default position.
    /// </summary>
    public void ResetToDefaultPosition()
    {
        targetPosition = Vector3.zero + Vector3.back * 10f;
        isAtFaceoff = false;
        isMovingToFaceoff = false;
    }

    /// <summary>
    /// Check if referee is at faceoff position.
    /// </summary>
    public bool IsAtFaceoff => isAtFaceoff;

    #endregion

    private void OnDestroy()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
        {
            Destroy(spriteRenderer.sprite.texture);
        }
    }
}
