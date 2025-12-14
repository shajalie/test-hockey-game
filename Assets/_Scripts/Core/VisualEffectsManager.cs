using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HockeyGame.Core
{
    /// <summary>
    /// Manages all visual effects in the game including particle systems, screen shake, and flash effects.
    /// Uses object pooling for performance optimization.
    /// </summary>
    public class VisualEffectsManager : MonoBehaviour
    {
        #region Singleton

        private static VisualEffectsManager instance;
        public static VisualEffectsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<VisualEffectsManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VisualEffectsManager");
                        instance = go.AddComponent<VisualEffectsManager>();
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePools();
        }

        #endregion

        #region Particle System References

        [Header("Particle System Prefabs")]
        [SerializeField] private ParticleSystem puckTrailPrefab;
        [SerializeField] private ParticleSystem iceSprayPrefab;
        [SerializeField] private ParticleSystem goalCelebrationPrefab;
        [SerializeField] private ParticleSystem bodyCheckImpactPrefab;
        [SerializeField] private ParticleSystem puckHitSparksPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int maxPoolSize = 50;

        #endregion

        #region Screen Effects

        [Header("Screen Effects")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CanvasGroup flashCanvasGroup;
        [SerializeField] private UnityEngine.UI.Image flashImage;

        [Header("Screen Shake Settings")]
        [SerializeField] private float screenShakeMultiplier = 1f;
        [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private Vector3 originalCameraPosition;
        private Coroutine currentShakeCoroutine;
        private Coroutine currentFlashCoroutine;

        #endregion

        #region Speed Lines

        [Header("Speed Lines")]
        [SerializeField] private ParticleSystem speedLinesPrefab;

        private Dictionary<Transform, ParticleSystem> activeSpeedLines = new Dictionary<Transform, ParticleSystem>();
        private Queue<ParticleSystem> speedLinesPool = new Queue<ParticleSystem>();

        #endregion

        #region Object Pools

        private Dictionary<ParticleSystem, Queue<ParticleSystem>> particlePools = new Dictionary<ParticleSystem, Queue<ParticleSystem>>();
        private Dictionary<ParticleSystem, HashSet<ParticleSystem>> activeParticles = new Dictionary<ParticleSystem, HashSet<ParticleSystem>>();

        // Active puck trail tracking
        private ParticleSystem activePuckTrail;
        private Transform currentPuckTransform;

        #endregion

        #region Initialization

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                originalCameraPosition = mainCamera.transform.localPosition;
            }

            // Setup flash canvas if not assigned
            if (flashCanvasGroup == null && flashImage != null)
            {
                flashCanvasGroup = flashImage.GetComponent<CanvasGroup>();
            }

            if (flashCanvasGroup != null)
            {
                flashCanvasGroup.alpha = 0f;
                flashCanvasGroup.blocksRaycasts = false;
            }
        }

        private void InitializePools()
        {
            // Initialize pools for each particle system type
            if (puckTrailPrefab != null)
                CreatePool(puckTrailPrefab, initialPoolSize);

            if (iceSprayPrefab != null)
                CreatePool(iceSprayPrefab, initialPoolSize);

            if (goalCelebrationPrefab != null)
                CreatePool(goalCelebrationPrefab, 5);

            if (bodyCheckImpactPrefab != null)
                CreatePool(bodyCheckImpactPrefab, initialPoolSize);

            if (puckHitSparksPrefab != null)
                CreatePool(puckHitSparksPrefab, initialPoolSize);

            // Initialize speed lines pool
            if (speedLinesPrefab != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    ParticleSystem speedLine = Instantiate(speedLinesPrefab, transform);
                    speedLine.gameObject.SetActive(false);
                    speedLinesPool.Enqueue(speedLine);
                }
            }
        }

        private void CreatePool(ParticleSystem prefab, int size)
        {
            Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
            HashSet<ParticleSystem> active = new HashSet<ParticleSystem>();

            for (int i = 0; i < size; i++)
            {
                ParticleSystem ps = Instantiate(prefab, transform);
                ps.gameObject.SetActive(false);
                pool.Enqueue(ps);
            }

            particlePools[prefab] = pool;
            activeParticles[prefab] = active;
        }

        private ParticleSystem GetFromPool(ParticleSystem prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning("VisualEffectsManager: Trying to get null prefab from pool");
                return null;
            }

            if (!particlePools.ContainsKey(prefab))
            {
                CreatePool(prefab, initialPoolSize);
            }

            Queue<ParticleSystem> pool = particlePools[prefab];
            ParticleSystem ps;

            if (pool.Count > 0)
            {
                ps = pool.Dequeue();
            }
            else
            {
                // Expand pool if needed
                if (activeParticles[prefab].Count < maxPoolSize)
                {
                    ps = Instantiate(prefab, transform);
                }
                else
                {
                    Debug.LogWarning($"VisualEffectsManager: Max pool size reached for {prefab.name}");
                    return null;
                }
            }

            ps.gameObject.SetActive(true);
            activeParticles[prefab].Add(ps);
            return ps;
        }

        private void ReturnToPool(ParticleSystem prefab, ParticleSystem ps)
        {
            if (prefab == null || ps == null) return;

            if (!particlePools.ContainsKey(prefab)) return;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
            ps.transform.SetParent(transform);

            activeParticles[prefab].Remove(ps);
            particlePools[prefab].Enqueue(ps);
        }

        #endregion

        #region Particle Effect Methods

        /// <summary>
        /// Spawns ice spray particles at the specified position and direction.
        /// </summary>
        public void SpawnIceSpray(Vector3 position, Vector3 direction)
        {
            if (iceSprayPrefab == null) return;

            ParticleSystem ps = GetFromPool(iceSprayPrefab);
            if (ps == null) return;

            ps.transform.position = position;
            ps.transform.rotation = Quaternion.LookRotation(direction);
            ps.Play();

            StartCoroutine(ReturnToPoolAfterDuration(iceSprayPrefab, ps, ps.main.duration + ps.main.startLifetime.constantMax));
        }

        /// <summary>
        /// Spawns and attaches a puck trail to the specified puck transform.
        /// </summary>
        public void SpawnPuckTrail(Transform puck)
        {
            if (puckTrailPrefab == null || puck == null) return;

            // Stop existing trail if any
            if (activePuckTrail != null)
            {
                StopPuckTrail();
            }

            activePuckTrail = GetFromPool(puckTrailPrefab);
            if (activePuckTrail == null) return;

            currentPuckTransform = puck;
            activePuckTrail.transform.SetParent(puck);
            activePuckTrail.transform.localPosition = Vector3.zero;
            activePuckTrail.Play();
        }

        /// <summary>
        /// Stops the current puck trail effect.
        /// </summary>
        public void StopPuckTrail()
        {
            if (activePuckTrail != null)
            {
                activePuckTrail.transform.SetParent(transform);
                StartCoroutine(ReturnToPoolAfterDuration(puckTrailPrefab, activePuckTrail, activePuckTrail.main.startLifetime.constantMax));
                activePuckTrail = null;
                currentPuckTransform = null;
            }
        }

        /// <summary>
        /// Spawns a goal celebration effect at the specified goal position.
        /// </summary>
        public void SpawnGoalCelebration(Vector3 goalPosition)
        {
            if (goalCelebrationPrefab == null) return;

            ParticleSystem ps = GetFromPool(goalCelebrationPrefab);
            if (ps == null) return;

            ps.transform.position = goalPosition;
            ps.transform.rotation = Quaternion.identity;
            ps.Play();

            StartCoroutine(ReturnToPoolAfterDuration(goalCelebrationPrefab, ps, ps.main.duration + ps.main.startLifetime.constantMax));

            // Trigger screen shake and flash for goal
            ScreenShake(0.5f, 0.8f);
            FlashColor(Color.white, 0.3f);
        }

        /// <summary>
        /// Spawns a body check impact effect at the specified position.
        /// </summary>
        public void SpawnHitEffect(Vector3 position)
        {
            if (bodyCheckImpactPrefab == null) return;

            ParticleSystem ps = GetFromPool(bodyCheckImpactPrefab);
            if (ps == null) return;

            ps.transform.position = position;
            ps.transform.rotation = Quaternion.identity;
            ps.Play();

            StartCoroutine(ReturnToPoolAfterDuration(bodyCheckImpactPrefab, ps, ps.main.duration + ps.main.startLifetime.constantMax));

            // Small screen shake for hits
            ScreenShake(0.2f, 0.2f);
        }

        /// <summary>
        /// Spawns puck hit sparks at the specified position.
        /// </summary>
        public void SpawnPuckHitSparks(Vector3 position, Vector3 normal)
        {
            if (puckHitSparksPrefab == null) return;

            ParticleSystem ps = GetFromPool(puckHitSparksPrefab);
            if (ps == null) return;

            ps.transform.position = position;
            ps.transform.rotation = Quaternion.LookRotation(normal);
            ps.Play();

            StartCoroutine(ReturnToPoolAfterDuration(puckHitSparksPrefab, ps, ps.main.duration + ps.main.startLifetime.constantMax));
        }

        private IEnumerator ReturnToPoolAfterDuration(ParticleSystem prefab, ParticleSystem ps, float duration)
        {
            yield return new WaitForSeconds(duration);
            ReturnToPool(prefab, ps);
        }

        #endregion

        #region Screen Effects Methods

        /// <summary>
        /// Shakes the camera with the specified intensity and duration.
        /// </summary>
        public void ScreenShake(float intensity, float duration)
        {
            if (mainCamera == null) return;

            if (currentShakeCoroutine != null)
            {
                StopCoroutine(currentShakeCoroutine);
            }

            currentShakeCoroutine = StartCoroutine(ScreenShakeCoroutine(intensity, duration));
        }

        private IEnumerator ScreenShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;
            Vector3 originalPos = originalCameraPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float magnitude = intensity * screenShakeMultiplier * shakeCurve.Evaluate(progress);

                float offsetX = Random.Range(-1f, 1f) * magnitude;
                float offsetY = Random.Range(-1f, 1f) * magnitude;

                mainCamera.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);

                yield return null;
            }

            mainCamera.transform.localPosition = originalPos;
            currentShakeCoroutine = null;
        }

        /// <summary>
        /// Flashes the screen with the specified color and duration.
        /// </summary>
        public void FlashColor(Color color, float duration)
        {
            if (flashImage == null || flashCanvasGroup == null) return;

            if (currentFlashCoroutine != null)
            {
                StopCoroutine(currentFlashCoroutine);
            }

            currentFlashCoroutine = StartCoroutine(FlashColorCoroutine(color, duration));
        }

        private IEnumerator FlashColorCoroutine(Color color, float duration)
        {
            flashImage.color = color;
            flashCanvasGroup.alpha = 0.6f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                flashCanvasGroup.alpha = Mathf.Lerp(0.6f, 0f, elapsed / duration);
                yield return null;
            }

            flashCanvasGroup.alpha = 0f;
            currentFlashCoroutine = null;
        }

        #endregion

        #region Speed Lines Methods

        /// <summary>
        /// Starts the dash effect (speed lines) for the specified player.
        /// </summary>
        public void StartDashEffect(Transform player)
        {
            if (speedLinesPrefab == null || player == null) return;

            // Check if player already has speed lines
            if (activeSpeedLines.ContainsKey(player))
            {
                return;
            }

            ParticleSystem speedLines;

            if (speedLinesPool.Count > 0)
            {
                speedLines = speedLinesPool.Dequeue();
            }
            else
            {
                speedLines = Instantiate(speedLinesPrefab, transform);
            }

            speedLines.gameObject.SetActive(true);
            speedLines.transform.SetParent(player);
            speedLines.transform.localPosition = Vector3.zero;
            speedLines.transform.localRotation = Quaternion.identity;
            speedLines.Play();

            activeSpeedLines[player] = speedLines;
        }

        /// <summary>
        /// Stops the dash effect (speed lines) for the specified player.
        /// </summary>
        public void StopDashEffect(Transform player)
        {
            if (player == null || !activeSpeedLines.ContainsKey(player))
            {
                return;
            }

            ParticleSystem speedLines = activeSpeedLines[player];
            speedLines.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            speedLines.transform.SetParent(transform);

            StartCoroutine(ReturnSpeedLinesToPool(speedLines, speedLines.main.startLifetime.constantMax));

            activeSpeedLines.Remove(player);
        }

        private IEnumerator ReturnSpeedLinesToPool(ParticleSystem speedLines, float delay)
        {
            yield return new WaitForSeconds(delay);

            speedLines.gameObject.SetActive(false);
            speedLinesPool.Enqueue(speedLines);
        }

        #endregion

        #region Update

        private void Update()
        {
            // Update puck trail position if following puck
            if (activePuckTrail != null && currentPuckTransform != null)
            {
                // Trail is already parented to puck, but we could add velocity-based effects here
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Stop all coroutines
            StopAllCoroutines();

            // Clean up active effects
            if (activePuckTrail != null)
            {
                StopPuckTrail();
            }

            // Clean up speed lines
            foreach (var kvp in activeSpeedLines)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            activeSpeedLines.Clear();

            // Reset camera position
            if (mainCamera != null)
            {
                mainCamera.transform.localPosition = originalCameraPosition;
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Stops all active visual effects.
        /// </summary>
        public void StopAllEffects()
        {
            StopPuckTrail();

            foreach (var kvp in activeParticles)
            {
                foreach (var ps in kvp.Value.ToArray())
                {
                    ReturnToPool(kvp.Key, ps);
                }
            }

            foreach (var player in activeSpeedLines.Keys.ToArray())
            {
                StopDashEffect(player);
            }

            if (currentShakeCoroutine != null)
            {
                StopCoroutine(currentShakeCoroutine);
                currentShakeCoroutine = null;
                if (mainCamera != null)
                {
                    mainCamera.transform.localPosition = originalCameraPosition;
                }
            }

            if (currentFlashCoroutine != null)
            {
                StopCoroutine(currentFlashCoroutine);
                currentFlashCoroutine = null;
                if (flashCanvasGroup != null)
                {
                    flashCanvasGroup.alpha = 0f;
                }
            }
        }

        /// <summary>
        /// Checks if a player has an active dash effect.
        /// </summary>
        public bool HasActiveDashEffect(Transform player)
        {
            return activeSpeedLines.ContainsKey(player);
        }

        /// <summary>
        /// Gets the current number of active particles in a specific pool.
        /// </summary>
        public int GetActiveParticleCount(ParticleSystem prefab)
        {
            if (prefab == null || !activeParticles.ContainsKey(prefab))
            {
                return 0;
            }
            return activeParticles[prefab].Count;
        }

        #endregion
    }
}
