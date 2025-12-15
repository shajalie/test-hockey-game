using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace HockeyGame.Core
{
    /// <summary>
    /// Game Polish Manager - Adds professional polish and feel to the hockey game.
    /// Handles camera transitions, slow-motion effects, goal replays, dynamic crowd reactions,
    /// momentum indicators, hit effects, puck possession highlighting, player labels, team colors,
    /// and match intro sequences.
    /// </summary>
    public class GamePolish : MonoBehaviour
    {
        #region Singleton

        private static GamePolish instance;
        public static GamePolish Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GamePolish>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GamePolish");
                        instance = go.AddComponent<GamePolish>();
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

            InitializePolishSystems();
        }

        #endregion

        #region Camera System

        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float cameraMoveSpeed = 2f;
        [SerializeField] private float cameraRotateSpeed = 1.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Transform defaultCameraParent;
        private Vector3 defaultCameraLocalPosition;
        private Quaternion defaultCameraLocalRotation;
        private Coroutine cameraTransitionCoroutine;

        #endregion

        #region Goal Replay System

        [Header("Goal Replay Settings")]
        [SerializeField] private Transform replayCameraPosition;
        [SerializeField] private float slowMotionTimeScale = 0.3f;
        [SerializeField] private float slowMotionDuration = 2f;
        [SerializeField] private float replayDuration = 3f;
        [SerializeField] private Vector3 replayCameraOffset = new Vector3(0f, 3f, -5f);

        private bool isInSlowMotion = false;
        private bool isInReplay = false;

        #endregion

        #region Crowd System

        [Header("Crowd Noise Settings")]
        [SerializeField] private AudioSource crowdAudioSource;
        [SerializeField] private float crowdIntensityChangeSpeed = 1f;
        [SerializeField] private float baseIntensity = 0.3f;
        [SerializeField] private float maxIntensity = 1f;

        private float targetCrowdIntensity;
        private float currentCrowdIntensity;

        // Crowd reaction factors
        private float proximityToGoal = 0f;
        private float puckSpeedFactor = 0f;
        private int recentShotCount = 0;
        private float shotCountDecayTimer = 0f;

        #endregion

        #region Momentum Indicators (Speed Lines)

        [Header("Momentum Settings")]
        [SerializeField] private ParticleSystem speedLinesPrefab;
        [SerializeField] private float speedThresholdForLines = 8f;
        [SerializeField] private float maxSpeedForLines = 15f;

        private Dictionary<Transform, ParticleSystem> activeSpeedLines = new Dictionary<Transform, ParticleSystem>();
        private Queue<ParticleSystem> speedLinesPool = new Queue<ParticleSystem>();

        #endregion

        #region Hit Freeze Frames

        [Header("Hit Impact Settings")]
        [SerializeField] private float freezeFrameDuration = 0.1f;
        [SerializeField] private float hitMagnitudeThreshold = 5f;
        [SerializeField] private AnimationCurve freezeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private bool isInFreezeFrame = false;

        #endregion

        #region Puck Possession Highlighting

        [Header("Possession Highlight Settings")]
        [SerializeField] private GameObject possessionIndicatorPrefab;
        [SerializeField] private Vector3 indicatorOffset = new Vector3(0f, 3f, 0f);
        [SerializeField] private float indicatorRotationSpeed = 90f;

        private GameObject currentPossessionIndicator;
        private Transform currentPossessionPlayer;

        #endregion

        #region Player Name Labels

        [Header("Player Name Labels")]
        [SerializeField] private GameObject playerLabelPrefab;
        [SerializeField] private Vector3 labelOffset = new Vector3(0f, 2.5f, 0f);
        [SerializeField] private bool showPlayerLabels = true;
        [SerializeField] private float labelFadeDistance = 20f;

        private Dictionary<Transform, GameObject> playerLabels = new Dictionary<Transform, GameObject>();

        #endregion

        #region Team Color Management

        [Header("Team Colors")]
        [SerializeField] private Material team0Material;
        [SerializeField] private Material team1Material;
        [SerializeField] private Color team0Color = Color.blue;
        [SerializeField] private Color team1Color = Color.red;

        private Dictionary<int, Material> teamMaterials = new Dictionary<int, Material>();
        private Dictionary<int, Color> teamColors = new Dictionary<int, Color>();

        #endregion

        #region Match Intro Sequence

        [Header("Match Intro Settings")]
        [SerializeField] private bool playIntroOnStart = true;
        [SerializeField] private float introFlyoverDuration = 5f;
        [SerializeField] private Vector3 introStartPosition = new Vector3(0f, 20f, -30f);
        [SerializeField] private Vector3 introEndPosition = new Vector3(0f, 15f, 0f);
        [SerializeField] private string team0Name = "Home Team";
        [SerializeField] private string team1Name = "Away Team";
        [SerializeField] private GameObject introUIPanel;
        [SerializeField] private TextMeshProUGUI introTeamText;

        private bool hasPlayedIntro = false;

        #endregion

        #region Initialization

        private void InitializePolishSystems()
        {
            // Find main camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                defaultCameraParent = mainCamera.transform.parent;
                defaultCameraLocalPosition = mainCamera.transform.localPosition;
                defaultCameraLocalRotation = mainCamera.transform.localRotation;
            }

            // Initialize crowd system
            currentCrowdIntensity = baseIntensity;
            targetCrowdIntensity = baseIntensity;

            // Initialize team materials
            teamMaterials[0] = team0Material;
            teamMaterials[1] = team1Material;
            teamColors[0] = team0Color;
            teamColors[1] = team1Color;

            // Initialize speed lines pool
            if (speedLinesPrefab != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    ParticleSystem speedLine = Instantiate(speedLinesPrefab, transform);
                    speedLine.gameObject.SetActive(false);
                    speedLinesPool.Enqueue(speedLine);
                }
            }
        }

        private void Start()
        {
            // Subscribe to game events
            SubscribeToEvents();

            // Play match intro if enabled
            if (playIntroOnStart && !hasPlayedIntro)
            {
                StartCoroutine(PlayMatchIntro());
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            // Clean up
            CleanupAllLabels();
            CleanupPossessionIndicator();
            CleanupSpeedLines();
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnGoalScored += OnGoalScored;
            GameEvents.OnPuckPossessionChanged += OnPuckPossessionChanged;
            GameEvents.OnShotTaken += OnShotTaken;
            GameEvents.OnMatchStart += OnMatchStart;
            GameEvents.OnMatchEnd += OnMatchEnd;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnGoalScored -= OnGoalScored;
            GameEvents.OnPuckPossessionChanged -= OnPuckPossessionChanged;
            GameEvents.OnShotTaken -= OnShotTaken;
            GameEvents.OnMatchStart -= OnMatchStart;
            GameEvents.OnMatchEnd -= OnMatchEnd;
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            UpdateCrowdIntensity();
            UpdatePossessionIndicator();
            UpdatePlayerLabels();
            UpdateMomentumIndicators();

            // Decay shot count
            if (shotCountDecayTimer > 0f)
            {
                shotCountDecayTimer -= Time.deltaTime;
            }
            else if (recentShotCount > 0)
            {
                recentShotCount = 0;
            }
        }

        #endregion

        #region Camera Transitions

        /// <summary>
        /// Smoothly transitions camera to a target transform.
        /// </summary>
        public void TransitionCameraToTarget(Transform target, float duration)
        {
            if (mainCamera == null || target == null) return;

            if (cameraTransitionCoroutine != null)
            {
                StopCoroutine(cameraTransitionCoroutine);
            }

            cameraTransitionCoroutine = StartCoroutine(CameraTransitionCoroutine(target, duration));
        }

        /// <summary>
        /// Smoothly transitions camera to default position.
        /// </summary>
        public void TransitionCameraToDefault(float duration)
        {
            if (mainCamera == null) return;

            if (cameraTransitionCoroutine != null)
            {
                StopCoroutine(cameraTransitionCoroutine);
            }

            cameraTransitionCoroutine = StartCoroutine(ReturnCameraToDefaultCoroutine(duration));
        }

        private IEnumerator CameraTransitionCoroutine(Transform target, float duration)
        {
            Vector3 startPosition = mainCamera.transform.position;
            Quaternion startRotation = mainCamera.transform.rotation;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / duration);

                mainCamera.transform.position = Vector3.Lerp(startPosition, target.position, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRotation, target.rotation, t);

                yield return null;
            }

            mainCamera.transform.position = target.position;
            mainCamera.transform.rotation = target.rotation;

            cameraTransitionCoroutine = null;
        }

        private IEnumerator ReturnCameraToDefaultCoroutine(float duration)
        {
            Vector3 startPosition = mainCamera.transform.localPosition;
            Quaternion startRotation = mainCamera.transform.localRotation;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / duration);

                if (defaultCameraParent != null)
                {
                    mainCamera.transform.SetParent(defaultCameraParent);
                }

                mainCamera.transform.localPosition = Vector3.Lerp(startPosition, defaultCameraLocalPosition, t);
                mainCamera.transform.localRotation = Quaternion.Slerp(startRotation, defaultCameraLocalRotation, t);

                yield return null;
            }

            mainCamera.transform.localPosition = defaultCameraLocalPosition;
            mainCamera.transform.localRotation = defaultCameraLocalRotation;

            cameraTransitionCoroutine = null;
        }

        #endregion

        #region Slow-Motion & Goal Replay

        /// <summary>
        /// Triggers slow-motion effect for a brief period.
        /// </summary>
        public void TriggerSlowMotion(float duration = 0f)
        {
            if (isInSlowMotion) return;

            float actualDuration = duration > 0f ? duration : slowMotionDuration;
            StartCoroutine(SlowMotionCoroutine(actualDuration));
        }

        private IEnumerator SlowMotionCoroutine(float duration)
        {
            isInSlowMotion = true;
            // SAFETY: Only apply slow-mo if not already in slow-mo/freeze
            if (!isInFreezeFrame && Time.timeScale >= 0.9f)
            {
                Time.timeScale = slowMotionTimeScale;
                Time.fixedDeltaTime = 0.02f * slowMotionTimeScale;
            }

            yield return new WaitForSecondsRealtime(duration);

            // SAFETY: Only restore if we're still in slow-mo (not paused or frozen)
            if (isInSlowMotion && !isInFreezeFrame && Time.timeScale < 0.9f)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
            isInSlowMotion = false;
        }

        /// <summary>
        /// Plays goal replay sequence with camera angle and slow-motion.
        /// </summary>
        public void PlayGoalReplay(Vector3 goalPosition)
        {
            if (isInReplay) return;

            StartCoroutine(GoalReplaySequence(goalPosition));
        }

        private IEnumerator GoalReplaySequence(Vector3 goalPosition)
        {
            isInReplay = true;

            // Create temporary camera position for replay
            GameObject tempCameraTarget = new GameObject("ReplayCameraTarget");
            tempCameraTarget.transform.position = goalPosition + replayCameraOffset;
            tempCameraTarget.transform.LookAt(goalPosition);

            // Trigger slow-motion
            TriggerSlowMotion(slowMotionDuration);

            // Transition camera to replay angle
            TransitionCameraToTarget(tempCameraTarget.transform, 0.5f);

            // Wait for replay duration
            yield return new WaitForSecondsRealtime(replayDuration);

            // Return camera to default
            TransitionCameraToDefault(0.5f);

            // Clean up
            Destroy(tempCameraTarget, 1f);

            isInReplay = false;
        }

        #endregion

        #region Dynamic Crowd Noise

        private void UpdateCrowdIntensity()
        {
            // Calculate target intensity based on gameplay factors
            float intensityFromProximity = proximityToGoal * 0.3f;
            float intensityFromSpeed = puckSpeedFactor * 0.2f;
            float intensityFromShots = Mathf.Min(recentShotCount * 0.15f, 0.3f);

            targetCrowdIntensity = Mathf.Clamp(
                baseIntensity + intensityFromProximity + intensityFromSpeed + intensityFromShots,
                baseIntensity,
                maxIntensity
            );

            // Smoothly interpolate to target
            currentCrowdIntensity = Mathf.Lerp(
                currentCrowdIntensity,
                targetCrowdIntensity,
                crowdIntensityChangeSpeed * Time.deltaTime
            );

            // Apply to sound manager
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetCrowdIntensity(currentCrowdIntensity);
            }
        }

        /// <summary>
        /// Updates crowd reaction based on puck proximity to goal.
        /// Call this from game systems that track puck position.
        /// </summary>
        public void UpdatePuckProximityToGoal(float normalizedDistance)
        {
            proximityToGoal = 1f - Mathf.Clamp01(normalizedDistance);
        }

        /// <summary>
        /// Updates crowd reaction based on puck speed.
        /// Call this when puck speed changes significantly.
        /// </summary>
        public void UpdatePuckSpeed(float speed)
        {
            puckSpeedFactor = Mathf.Clamp01(speed / 30f);
        }

        #endregion

        #region Momentum Indicators (Speed Lines)

        private void UpdateMomentumIndicators()
        {
            // Find all hockey players and update their speed lines
            HockeyPlayer[] players = FindObjectsOfType<HockeyPlayer>();

            foreach (HockeyPlayer player in players)
            {
                float speed = player.Velocity.magnitude;

                // Show speed lines if moving fast enough
                if (speed >= speedThresholdForLines)
                {
                    ShowSpeedLines(player.transform, speed);
                }
                else
                {
                    HideSpeedLines(player.transform);
                }
            }
        }

        private void ShowSpeedLines(Transform player, float speed)
        {
            if (activeSpeedLines.ContainsKey(player))
            {
                // Update existing speed lines intensity
                ParticleSystem ps = activeSpeedLines[player];
                var emission = ps.emission;
                float intensity = Mathf.InverseLerp(speedThresholdForLines, maxSpeedForLines, speed);
                emission.rateOverTime = 20f + (intensity * 80f);
                return;
            }

            // Create new speed lines
            ParticleSystem speedLines;

            if (speedLinesPool.Count > 0)
            {
                speedLines = speedLinesPool.Dequeue();
            }
            else if (speedLinesPrefab != null)
            {
                speedLines = Instantiate(speedLinesPrefab, transform);
            }
            else
            {
                return;
            }

            speedLines.gameObject.SetActive(true);
            speedLines.transform.SetParent(player);
            speedLines.transform.localPosition = Vector3.zero;
            speedLines.transform.localRotation = Quaternion.identity;
            speedLines.Play();

            activeSpeedLines[player] = speedLines;
        }

        private void HideSpeedLines(Transform player)
        {
            if (!activeSpeedLines.ContainsKey(player)) return;

            ParticleSystem speedLines = activeSpeedLines[player];
            speedLines.Stop();
            speedLines.transform.SetParent(transform);
            speedLines.gameObject.SetActive(false);

            activeSpeedLines.Remove(player);
            speedLinesPool.Enqueue(speedLines);
        }

        private void CleanupSpeedLines()
        {
            foreach (var kvp in activeSpeedLines)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            activeSpeedLines.Clear();

            while (speedLinesPool.Count > 0)
            {
                ParticleSystem ps = speedLinesPool.Dequeue();
                if (ps != null)
                {
                    Destroy(ps.gameObject);
                }
            }
        }

        #endregion

        #region Hit Impact Freeze Frames

        /// <summary>
        /// Triggers a brief freeze frame effect on big hits.
        /// </summary>
        public void TriggerHitFreezeFrame(float hitMagnitude)
        {
            if (hitMagnitude < hitMagnitudeThreshold || isInFreezeFrame) return;

            StartCoroutine(FreezeFrameCoroutine());
        }

        private IEnumerator FreezeFrameCoroutine()
        {
            isInFreezeFrame = true;

            float originalTimeScale = Time.timeScale;
            float elapsed = 0f;

            while (elapsed < freezeFrameDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = freezeCurve.Evaluate(elapsed / freezeFrameDuration);
                Time.timeScale = Mathf.Lerp(0.1f, originalTimeScale, t);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;

                yield return null;
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = 0.02f;
            isInFreezeFrame = false;
        }

        #endregion

        #region Puck Possession Highlighting

        private void UpdatePossessionIndicator()
        {
            if (currentPossessionIndicator == null || currentPossessionPlayer == null) return;

            // Update position
            currentPossessionIndicator.transform.position = currentPossessionPlayer.position + indicatorOffset;

            // Rotate indicator
            currentPossessionIndicator.transform.Rotate(Vector3.up, indicatorRotationSpeed * Time.deltaTime);
        }

        private void ShowPossessionIndicator(Transform player)
        {
            if (player == null) return;

            // Hide existing indicator
            if (currentPossessionIndicator != null)
            {
                Destroy(currentPossessionIndicator);
            }

            // Create new indicator
            if (possessionIndicatorPrefab != null)
            {
                currentPossessionIndicator = Instantiate(possessionIndicatorPrefab);
                currentPossessionIndicator.transform.position = player.position + indicatorOffset;
                currentPossessionPlayer = player;
            }
        }

        private void HidePossessionIndicator()
        {
            if (currentPossessionIndicator != null)
            {
                Destroy(currentPossessionIndicator);
                currentPossessionIndicator = null;
            }
            currentPossessionPlayer = null;
        }

        private void CleanupPossessionIndicator()
        {
            if (currentPossessionIndicator != null)
            {
                Destroy(currentPossessionIndicator);
            }
        }

        #endregion

        #region Player Name Labels

        /// <summary>
        /// Creates a name label for a player.
        /// </summary>
        public void CreatePlayerLabel(Transform player, string playerName)
        {
            if (!showPlayerLabels || player == null || playerLabelPrefab == null) return;

            // Don't create duplicate labels
            if (playerLabels.ContainsKey(player)) return;

            GameObject label = Instantiate(playerLabelPrefab);
            label.transform.SetParent(player);
            label.transform.localPosition = labelOffset;

            // Set player name text
            TextMeshProUGUI textComponent = label.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = playerName;
            }

            playerLabels[player] = label;
        }

        /// <summary>
        /// Removes a player's name label.
        /// </summary>
        public void RemovePlayerLabel(Transform player)
        {
            if (!playerLabels.ContainsKey(player)) return;

            GameObject label = playerLabels[player];
            if (label != null)
            {
                Destroy(label);
            }

            playerLabels.Remove(player);
        }

        private void UpdatePlayerLabels()
        {
            if (!showPlayerLabels || mainCamera == null) return;

            foreach (var kvp in playerLabels)
            {
                Transform player = kvp.Key;
                GameObject label = kvp.Value;

                if (player == null || label == null) continue;

                // Make label face camera
                label.transform.LookAt(mainCamera.transform);
                label.transform.Rotate(0f, 180f, 0f);

                // Fade based on distance
                float distance = Vector3.Distance(mainCamera.transform.position, player.position);
                float alpha = 1f - Mathf.Clamp01((distance - 5f) / labelFadeDistance);

                CanvasGroup canvasGroup = label.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = alpha;
                }
            }
        }

        private void CleanupAllLabels()
        {
            foreach (var label in playerLabels.Values)
            {
                if (label != null)
                {
                    Destroy(label);
                }
            }
            playerLabels.Clear();
        }

        /// <summary>
        /// Toggle player name labels on/off.
        /// </summary>
        public void TogglePlayerLabels(bool show)
        {
            showPlayerLabels = show;

            foreach (var label in playerLabels.Values)
            {
                if (label != null)
                {
                    label.SetActive(show);
                }
            }
        }

        #endregion

        #region Team Color Management

        /// <summary>
        /// Gets the material for a specific team.
        /// </summary>
        public Material GetTeamMaterial(int teamId)
        {
            if (teamMaterials.ContainsKey(teamId))
            {
                return teamMaterials[teamId];
            }
            return null;
        }

        /// <summary>
        /// Gets the color for a specific team.
        /// </summary>
        public Color GetTeamColor(int teamId)
        {
            if (teamColors.ContainsKey(teamId))
            {
                return teamColors[teamId];
            }
            return Color.white;
        }

        /// <summary>
        /// Sets team colors and materials.
        /// </summary>
        public void SetTeamColors(int teamId, Color color, Material material = null)
        {
            teamColors[teamId] = color;
            if (material != null)
            {
                teamMaterials[teamId] = material;
            }
        }

        /// <summary>
        /// Applies team color to a renderer.
        /// </summary>
        public void ApplyTeamColor(Renderer renderer, int teamId)
        {
            if (renderer == null) return;

            Material teamMaterial = GetTeamMaterial(teamId);
            if (teamMaterial != null)
            {
                renderer.material = teamMaterial;
            }
            else
            {
                renderer.material.color = GetTeamColor(teamId);
            }
        }

        #endregion

        #region Match Intro Sequence

        /// <summary>
        /// Plays the match intro sequence with camera flyover and team names.
        /// </summary>
        public IEnumerator PlayMatchIntro()
        {
            hasPlayedIntro = true;

            if (mainCamera == null) yield break;

            // Show intro UI
            if (introUIPanel != null)
            {
                introUIPanel.SetActive(true);
            }

            if (introTeamText != null)
            {
                introTeamText.text = $"{team0Name} vs {team1Name}";
            }

            // Save original camera position
            Vector3 originalPosition = mainCamera.transform.position;
            Quaternion originalRotation = mainCamera.transform.rotation;

            // Set camera to intro start position
            mainCamera.transform.position = introStartPosition;
            mainCamera.transform.LookAt(Vector3.zero);

            float elapsed = 0f;

            // Animate camera flyover
            while (elapsed < introFlyoverDuration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / introFlyoverDuration);

                mainCamera.transform.position = Vector3.Lerp(introStartPosition, introEndPosition, t);
                mainCamera.transform.LookAt(Vector3.zero);

                yield return null;
            }

            // Return camera to original position
            elapsed = 0f;
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / 1f);

                mainCamera.transform.position = Vector3.Lerp(startPos, originalPosition, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, originalRotation, t);

                yield return null;
            }

            // Hide intro UI
            if (introUIPanel != null)
            {
                introUIPanel.SetActive(false);
            }

            // Trigger match start
            GameEvents.TriggerMatchStart();
        }

        /// <summary>
        /// Manually trigger match intro.
        /// </summary>
        public void TriggerMatchIntro()
        {
            StartCoroutine(PlayMatchIntro());
        }

        #endregion

        #region Event Handlers

        private void OnGoalScored(int teamIndex)
        {
            // Find goal position (you may need to get this from your goal detection system)
            Vector3 goalPosition = Vector3.zero; // Replace with actual goal position

            // Trigger slow-motion
            TriggerSlowMotion();

            // Play goal replay
            PlayGoalReplay(goalPosition);

            // Trigger crowd cheer
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.TriggerCrowdCheer();
            }

            // Spike crowd intensity
            targetCrowdIntensity = maxIntensity;
        }

        private void OnPuckPossessionChanged(GameObject newOwner)
        {
            if (newOwner == null)
            {
                HidePossessionIndicator();
            }
            else
            {
                ShowPossessionIndicator(newOwner.transform);
            }
        }

        private void OnShotTaken(Vector3 direction, float power)
        {
            recentShotCount++;
            shotCountDecayTimer = 2f;

            // Increase crowd intensity
            targetCrowdIntensity = Mathf.Min(targetCrowdIntensity + 0.2f, maxIntensity);
        }

        private void OnMatchStart()
        {
            // Reset crowd to base intensity
            currentCrowdIntensity = baseIntensity;
            targetCrowdIntensity = baseIntensity;

            // Initialize all player labels
            HockeyPlayer[] players = FindObjectsOfType<HockeyPlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                CreatePlayerLabel(players[i].transform, $"Player {i + 1}");
            }
        }

        private void OnMatchEnd()
        {
            // Clean up
            CleanupAllLabels();
            HidePossessionIndicator();
            CleanupSpeedLines();

            // Reset time scale
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Enable or disable game polish effects globally.
        /// </summary>
        public void SetPolishEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        /// <summary>
        /// Reset all polish systems to default state.
        /// </summary>
        public void ResetAllSystems()
        {
            // Stop all coroutines
            StopAllCoroutines();

            // Reset time scale
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            // Reset flags
            isInSlowMotion = false;
            isInReplay = false;
            isInFreezeFrame = false;

            // Clean up
            CleanupAllLabels();
            HidePossessionIndicator();
            CleanupSpeedLines();

            // Reset camera
            if (mainCamera != null)
            {
                TransitionCameraToDefault(0.5f);
            }

            // Reset crowd
            currentCrowdIntensity = baseIntensity;
            targetCrowdIntensity = baseIntensity;
        }

        #endregion
    }
}
