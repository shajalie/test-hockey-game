using UnityEngine;

namespace HockeyGame.Core
{
    /// <summary>
    /// Example script demonstrating how to use the GameConfig system.
    /// This shows common patterns for accessing and modifying game settings.
    /// </summary>
    public class GameConfigExample : MonoBehaviour
    {
        private void Start()
        {
            // Subscribe to config events
            GameConfigManager.Instance.OnConfigLoaded += OnConfigLoaded;
            GameConfigManager.Instance.OnConfigChanged += OnConfigChanged;
            GameConfigManager.Instance.OnDifficultyChanged += OnDifficultyChanged;
            GameConfigManager.Instance.OnGameModeChanged += OnGameModeChanged;

            // Access the current config
            GameConfig config = GameConfigManager.Instance.Config;

            // Example: Reading settings
            Debug.Log($"Period Length: {config.periodLengthSeconds} seconds");
            Debug.Log($"Players Per Team: {config.playersPerTeam}");
            Debug.Log($"Player Speed: {config.GetEffectivePlayerSpeed()} m/s");

            // Example: Print full summary
            GameConfigManager.Instance.PrintConfigSummary();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (GameConfigManager.Instance != null)
            {
                GameConfigManager.Instance.OnConfigLoaded -= OnConfigLoaded;
                GameConfigManager.Instance.OnConfigChanged -= OnConfigChanged;
                GameConfigManager.Instance.OnDifficultyChanged -= OnDifficultyChanged;
                GameConfigManager.Instance.OnGameModeChanged -= OnGameModeChanged;
            }
        }

        // Event handlers
        private void OnConfigLoaded(GameConfig config)
        {
            Debug.Log($"Config loaded: {config.name}");
        }

        private void OnConfigChanged(GameConfig config)
        {
            Debug.Log("Config changed - updating game systems...");
            // Update your game systems here when config changes
        }

        private void OnDifficultyChanged(Difficulty newDifficulty)
        {
            Debug.Log($"Difficulty changed to: {newDifficulty}");
            // Update AI behavior, goalie stats, etc.
        }

        private void OnGameModeChanged(GameMode newMode)
        {
            Debug.Log($"Game mode changed to: {newMode}");
            // Update UI, rules, etc.
        }

        // Example methods showing how to modify settings at runtime
        [ContextMenu("Example: Change to Hard Difficulty")]
        private void ExampleChangeToHard()
        {
            GameConfigManager.Instance.SetDifficulty(Difficulty.Hard);
        }

        [ContextMenu("Example: Increase Player Speed by 50%")]
        private void ExampleIncreaseSpeed()
        {
            GameConfigManager.Instance.SetPlayerSpeedMultiplier(1.5f);
        }

        [ContextMenu("Example: Disable Offsides")]
        private void ExampleDisableOffsides()
        {
            GameConfigManager.Instance.SetRuleEnabled("offsides", false);
        }

        [ContextMenu("Example: Set Tournament Mode")]
        private void ExampleSetTournamentMode()
        {
            GameConfigManager.Instance.SetGameMode(GameMode.Tournament);
        }

        [ContextMenu("Example: Reset All Settings")]
        private void ExampleResetSettings()
        {
            GameConfigManager.Instance.ResetToDefaults();
        }
    }
}
