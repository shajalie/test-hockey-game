using UnityEngine;

namespace HockeyGame.Core
{
    /// <summary>
    /// Debug UI for testing GameConfig system in the editor and runtime.
    /// Attach to a GameObject to get a runtime debug panel for config testing.
    /// </summary>
    public class GameConfigDebugUI : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private bool isWindowVisible = false;
        private Rect windowRect = new Rect(20, 20, 400, 600);
        private Vector2 scrollPosition;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isWindowVisible = !isWindowVisible;
            }
        }

        private void OnGUI()
        {
            if (!showDebugUI || !isWindowVisible) return;

            windowRect = GUILayout.Window(0, windowRect, DrawDebugWindow, "Game Config Debug Panel");
        }

        private void DrawDebugWindow(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("=== GAME CONFIG DEBUG ===", GUI.skin.box);
            GUILayout.Space(10);

            if (!GameConfigManager.Instance.IsConfigLoaded())
            {
                GUILayout.Label("No config loaded!", GUI.skin.box);
                if (GUILayout.Button("Load Default Config"))
                {
                    GameConfigManager.Instance.LoadDefaultConfig();
                }
                GUILayout.EndScrollView();
                GUI.DragWindow();
                return;
            }

            GameConfig config = GameConfigManager.Instance.Config;

            // Game Mode Section
            GUILayout.Label("GAME MODE", GUI.skin.box);
            DrawEnumButtons("Mode", config.gameMode, (GameMode mode) =>
            {
                GameConfigManager.Instance.SetGameMode(mode);
            });
            GUILayout.Space(5);

            // Difficulty Section
            GUILayout.Label("DIFFICULTY", GUI.skin.box);
            DrawEnumButtons("Difficulty", config.difficulty, (Difficulty diff) =>
            {
                GameConfigManager.Instance.SetDifficulty(diff);
            });
            GUILayout.Space(5);

            // Player Speed Section
            GUILayout.Label("PLAYER SPEED", GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Multiplier: {config.playerSpeedMultiplier:F2}x");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0.5x")) GameConfigManager.Instance.SetPlayerSpeedMultiplier(0.5f);
            if (GUILayout.Button("1.0x")) GameConfigManager.Instance.SetPlayerSpeedMultiplier(1.0f);
            if (GUILayout.Button("1.5x")) GameConfigManager.Instance.SetPlayerSpeedMultiplier(1.5f);
            if (GUILayout.Button("2.0x")) GameConfigManager.Instance.SetPlayerSpeedMultiplier(2.0f);
            GUILayout.EndHorizontal();

            GUILayout.Label($"Effective Speed: {config.GetEffectivePlayerSpeed():F2} m/s");
            GUILayout.Label($"Sprint Speed: {config.GetSprintSpeed():F2} m/s");
            GUILayout.Space(5);

            // Rules Section
            GUILayout.Label("RULES", GUI.skin.box);
            DrawRuleToggle("Offsides", config.enableOffsides, "offsides");
            DrawRuleToggle("Icing", config.enableIcing, "icing");
            DrawRuleToggle("Penalties", config.enablePenalties, "penalties");
            DrawRuleToggle("Checking", config.enableChecking, "checking");
            DrawRuleToggle("Fighting", config.enableFighting, "fighting");
            DrawRuleToggle("Two-Line Pass", config.enableTwoLinePass, "twolinepass");
            GUILayout.Space(5);

            // Match Settings Section
            GUILayout.Label("MATCH SETTINGS", GUI.skin.box);
            GUILayout.Label($"Period Length: {config.periodLengthSeconds}s ({config.periodLengthSeconds / 60}m)");
            GUILayout.Label($"Period Count: {config.periodCount}");
            GUILayout.Label($"Players Per Team: {config.playersPerTeam}");
            GUILayout.Label($"Include Goalie: {config.includeGoalie}");
            GUILayout.Label($"Overtime: {config.enableOvertime}");
            GUILayout.Label($"Shootout: {config.enableShootout}");
            GUILayout.Space(5);

            // AI Settings Section
            GUILayout.Label("AI SETTINGS", GUI.skin.box);
            GUILayout.Label($"Reaction Time: {config.aiReactionTime:F2}s");
            GUILayout.Label($"Accuracy: {config.aiAccuracyMultiplier:F2}x");
            GUILayout.Label($"Aggression: {config.aiAggressionLevel:F2}x");
            GUILayout.Space(5);

            // Goalie Settings Section
            GUILayout.Label("GOALIE SETTINGS", GUI.skin.box);
            GUILayout.Label($"Reaction Time: {config.goalieReactionTime:F2}s");
            GUILayout.Label($"Save %: {config.goalieSavePercentage * 100:F0}%");
            GUILayout.Label($"Speed Multiplier: {config.goalieSpeedMultiplier:F2}x");
            GUILayout.Space(5);

            // Stamina Section
            GUILayout.Label("STAMINA", GUI.skin.box);
            GUILayout.Label($"Enabled: {config.enableStamina}");
            if (config.enableStamina)
            {
                GUILayout.Label($"Max: {config.maxStamina:F0}");
                GUILayout.Label($"Drain Rate: {config.staminaDrainRate:F1}/s");
                GUILayout.Label($"Recovery Rate: {config.staminaRecoveryRate:F1}/s");
            }
            GUILayout.Space(5);

            // Physics Section
            GUILayout.Label("PHYSICS", GUI.skin.box);
            GUILayout.Label($"Update Rate: {config.physicsUpdateRate} Hz");
            GUILayout.Label($"Fixed Timestep: {Time.fixedDeltaTime:F4}s");
            GUILayout.Label($"Puck Mass: {config.puckMass:F3} kg");
            GUILayout.Label($"Max Puck Speed: {config.maxPuckSpeed:F1} m/s");
            GUILayout.Space(5);

            // Actions Section
            GUILayout.Label("ACTIONS", GUI.skin.box);
            if (GUILayout.Button("Print Config Summary"))
            {
                GameConfigManager.Instance.PrintConfigSummary();
            }
            if (GUILayout.Button("Reset to Defaults"))
            {
                GameConfigManager.Instance.ResetToDefaults();
            }
            if (GUILayout.Button("Reload Config"))
            {
                GameConfigManager.Instance.LoadDefaultConfig();
            }

            GUILayout.Space(10);
            GUILayout.Label($"Press {toggleKey} to toggle this panel", GUI.skin.box);

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawEnumButtons<T>(string label, T currentValue, System.Action<T> onChanged) where T : System.Enum
        {
            GUILayout.BeginHorizontal();

            foreach (T value in System.Enum.GetValues(typeof(T)))
            {
                bool isActive = value.Equals(currentValue);
                GUI.color = isActive ? Color.green : Color.white;

                if (GUILayout.Button(value.ToString()))
                {
                    onChanged?.Invoke(value);
                }
            }

            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        private void DrawRuleToggle(string label, bool currentValue, string ruleName)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150));

            GUI.color = currentValue ? Color.green : Color.red;
            string buttonText = currentValue ? "ENABLED" : "DISABLED";

            if (GUILayout.Button(buttonText))
            {
                GameConfigManager.Instance.SetRuleEnabled(ruleName, !currentValue);
            }

            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        [ContextMenu("Show Debug UI")]
        private void ShowUI()
        {
            showDebugUI = true;
            isWindowVisible = true;
        }

        [ContextMenu("Hide Debug UI")]
        private void HideUI()
        {
            isWindowVisible = false;
        }
    }
}
