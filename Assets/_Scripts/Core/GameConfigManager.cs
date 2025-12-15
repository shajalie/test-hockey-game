using UnityEngine;
using System;

namespace HockeyGame.Core
{
    /// <summary>
    /// Singleton manager that loads and applies game configuration at runtime.
    /// Provides global access to game settings and notifies listeners when config changes.
    /// </summary>
    public class GameConfigManager : MonoBehaviour
    {
        private static GameConfigManager instance;

        [Header("Configuration")]
        [Tooltip("The active game configuration")]
        [SerializeField] private GameConfig activeConfig;

        [Header("Default Configuration Path")]
        [Tooltip("Path to default config in Resources folder")]
        [SerializeField] private string defaultConfigPath = "Configs/DefaultGameConfig";

        [Header("Runtime Settings")]
        [Tooltip("Load config on awake")]
        [SerializeField] private bool loadOnAwake = true;

        [Tooltip("Apply physics settings on load")]
        [SerializeField] private bool applyPhysicsSettings = true;

        // Events for config changes
        public event Action<GameConfig> OnConfigLoaded;
        public event Action<GameConfig> OnConfigChanged;
        public event Action<Difficulty> OnDifficultyChanged;
        public event Action<GameMode> OnGameModeChanged;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static GameConfigManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GameConfigManager>();

                    if (instance == null)
                    {
                        GameObject managerObj = new GameObject("GameConfigManager");
                        instance = managerObj.AddComponent<GameConfigManager>();
                        DontDestroyOnLoad(managerObj);
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Current active configuration
        /// </summary>
        public GameConfig Config
        {
            get
            {
                if (activeConfig == null)
                {
                    LoadDefaultConfig();
                }
                return activeConfig;
            }
        }

        private void Awake()
        {
            // Implement singleton pattern
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

                if (loadOnAwake)
                {
                    LoadDefaultConfig();
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (activeConfig != null && applyPhysicsSettings)
            {
                ApplyPhysicsSettings();
            }
        }

        /// <summary>
        /// Load the default configuration from Resources
        /// </summary>
        public void LoadDefaultConfig()
        {
            GameConfig config = Resources.Load<GameConfig>(defaultConfigPath);

            if (config != null)
            {
                LoadConfig(config);
                Debug.Log($"[GameConfigManager] Loaded default config from: {defaultConfigPath}");
            }
            else
            {
                Debug.LogWarning($"[GameConfigManager] Could not load config from: {defaultConfigPath}. Creating runtime config.");
                CreateRuntimeConfig();
            }
        }

        /// <summary>
        /// Load a specific configuration
        /// </summary>
        /// <param name="config">The config to load</param>
        public void LoadConfig(GameConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[GameConfigManager] Cannot load null config!");
                return;
            }

            activeConfig = config;

            if (applyPhysicsSettings)
            {
                ApplyPhysicsSettings();
            }

            OnConfigLoaded?.Invoke(activeConfig);
            OnConfigChanged?.Invoke(activeConfig);

            Debug.Log($"[GameConfigManager] Config loaded: {config.name}");
        }

        /// <summary>
        /// Create a runtime configuration with default values
        /// </summary>
        private void CreateRuntimeConfig()
        {
            activeConfig = ScriptableObject.CreateInstance<GameConfig>();
            activeConfig.name = "RuntimeGameConfig";
            activeConfig.ResetToDefaults();

            Debug.Log("[GameConfigManager] Created runtime config with default values");
        }

        /// <summary>
        /// Apply physics settings from config
        /// </summary>
        private void ApplyPhysicsSettings()
        {
            if (activeConfig == null) return;

            // Set fixed timestep based on physics update rate
            Time.fixedDeltaTime = 1f / activeConfig.physicsUpdateRate;

            Debug.Log($"[GameConfigManager] Applied physics settings - Fixed Timestep: {Time.fixedDeltaTime:F4}");
        }

        /// <summary>
        /// Change difficulty and apply preset
        /// </summary>
        /// <param name="newDifficulty">New difficulty level</param>
        public void SetDifficulty(Difficulty newDifficulty)
        {
            if (activeConfig == null)
            {
                Debug.LogWarning("[GameConfigManager] No active config to set difficulty!");
                return;
            }

            Difficulty oldDifficulty = activeConfig.difficulty;
            activeConfig.ApplyDifficultyPreset(newDifficulty);

            if (oldDifficulty != newDifficulty)
            {
                OnDifficultyChanged?.Invoke(newDifficulty);
                OnConfigChanged?.Invoke(activeConfig);
                Debug.Log($"[GameConfigManager] Difficulty changed: {oldDifficulty} -> {newDifficulty}");
            }
        }

        /// <summary>
        /// Change game mode
        /// </summary>
        /// <param name="newGameMode">New game mode</param>
        public void SetGameMode(GameMode newGameMode)
        {
            if (activeConfig == null)
            {
                Debug.LogWarning("[GameConfigManager] No active config to set game mode!");
                return;
            }

            GameMode oldMode = activeConfig.gameMode;
            activeConfig.gameMode = newGameMode;

            if (oldMode != newGameMode)
            {
                OnGameModeChanged?.Invoke(newGameMode);
                OnConfigChanged?.Invoke(activeConfig);
                Debug.Log($"[GameConfigManager] Game mode changed: {oldMode} -> {newGameMode}");
            }
        }

        /// <summary>
        /// Update player speed multiplier
        /// </summary>
        /// <param name="multiplier">New speed multiplier</param>
        public void SetPlayerSpeedMultiplier(float multiplier)
        {
            if (activeConfig == null)
            {
                Debug.LogWarning("[GameConfigManager] No active config!");
                return;
            }

            multiplier = Mathf.Clamp(multiplier, 0.1f, 3.0f);
            activeConfig.playerSpeedMultiplier = multiplier;

            OnConfigChanged?.Invoke(activeConfig);
            Debug.Log($"[GameConfigManager] Player speed multiplier set to: {multiplier:F2}");
        }

        /// <summary>
        /// Update AI reaction time
        /// </summary>
        /// <param name="reactionTime">New reaction time in seconds</param>
        public void SetAIReactionTime(float reactionTime)
        {
            if (activeConfig == null)
            {
                Debug.LogWarning("[GameConfigManager] No active config!");
                return;
            }

            reactionTime = Mathf.Clamp(reactionTime, 0.1f, 2.0f);
            activeConfig.aiReactionTime = reactionTime;

            OnConfigChanged?.Invoke(activeConfig);
            Debug.Log($"[GameConfigManager] AI reaction time set to: {reactionTime:F2}s");
        }

        /// <summary>
        /// Toggle a rule setting
        /// </summary>
        public void SetRuleEnabled(string ruleName, bool enabled)
        {
            if (activeConfig == null)
            {
                Debug.LogWarning("[GameConfigManager] No active config!");
                return;
            }

            switch (ruleName.ToLower())
            {
                case "offsides":
                    activeConfig.enableOffsides = enabled;
                    break;
                case "icing":
                    activeConfig.enableIcing = enabled;
                    break;
                case "penalties":
                    activeConfig.enablePenalties = enabled;
                    break;
                case "checking":
                    activeConfig.enableChecking = enabled;
                    break;
                case "fighting":
                    activeConfig.enableFighting = enabled;
                    break;
                case "twolinepass":
                    activeConfig.enableTwoLinePass = enabled;
                    break;
                default:
                    Debug.LogWarning($"[GameConfigManager] Unknown rule: {ruleName}");
                    return;
            }

            OnConfigChanged?.Invoke(activeConfig);
            Debug.Log($"[GameConfigManager] Rule '{ruleName}' set to: {enabled}");
        }

        /// <summary>
        /// Get current effective player speed
        /// </summary>
        public float GetEffectivePlayerSpeed()
        {
            return Config.GetEffectivePlayerSpeed();
        }

        /// <summary>
        /// Get current sprint speed
        /// </summary>
        public float GetSprintSpeed()
        {
            return Config.GetSprintSpeed();
        }

        /// <summary>
        /// Reset configuration to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            if (activeConfig == null)
            {
                Debug.LogWarning("[GameConfigManager] No active config to reset!");
                return;
            }

            activeConfig.ResetToDefaults();
            OnConfigChanged?.Invoke(activeConfig);

            Debug.Log("[GameConfigManager] Config reset to defaults");
        }

        /// <summary>
        /// Get a formatted summary of current settings
        /// </summary>
        public string GetConfigSummary()
        {
            if (activeConfig == null)
                return "No active configuration";

            return $"Game Config Summary:\n" +
                   $"- Game Mode: {activeConfig.gameMode}\n" +
                   $"- Difficulty: {activeConfig.difficulty}\n" +
                   $"- Period Length: {activeConfig.periodLengthSeconds}s ({activeConfig.periodLengthSeconds / 60}m)\n" +
                   $"- Period Count: {activeConfig.periodCount}\n" +
                   $"- Players Per Team: {activeConfig.playersPerTeam}\n" +
                   $"- Player Speed Multiplier: {activeConfig.playerSpeedMultiplier:F2}x\n" +
                   $"- Effective Speed: {GetEffectivePlayerSpeed():F2} m/s\n" +
                   $"- Sprint Speed: {GetSprintSpeed():F2} m/s\n" +
                   $"- AI Reaction Time: {activeConfig.aiReactionTime:F2}s\n" +
                   $"- Offsides: {activeConfig.enableOffsides}\n" +
                   $"- Icing: {activeConfig.enableIcing}\n" +
                   $"- Penalties: {activeConfig.enablePenalties}";
        }

        /// <summary>
        /// Print current config to console
        /// </summary>
        [ContextMenu("Print Config Summary")]
        public void PrintConfigSummary()
        {
            Debug.Log(GetConfigSummary());
        }

        /// <summary>
        /// Validate that a config is loaded
        /// </summary>
        public bool IsConfigLoaded()
        {
            return activeConfig != null;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper to create a default config asset
        /// </summary>
        [ContextMenu("Create Default Config Asset")]
        private void CreateDefaultConfigAsset()
        {
            Debug.Log("[GameConfigManager] Use Assets > Create > Hockey > Game Config to create a config asset, " +
                      "then place it in Resources/Configs/ folder and set the path in this manager.");
        }
#endif
    }
}
