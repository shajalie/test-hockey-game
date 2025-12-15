using UnityEngine;

namespace HockeyGame.Core
{
    /// <summary>
    /// Comprehensive game configuration system using ScriptableObject.
    /// Create instances via Assets > Create > Hockey > Game Config
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Hockey/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Match Settings")]
        [Tooltip("Duration of each period in seconds")]
        public int periodLengthSeconds = 300; // 5 minutes

        [Tooltip("Number of periods in a game")]
        [Range(1, 5)]
        public int periodCount = 3;

        [Tooltip("Enable overtime if game is tied")]
        public bool enableOvertime = true;

        [Tooltip("Duration of overtime period in seconds")]
        public int overtimeLengthSeconds = 300;

        [Tooltip("Enable shootout if still tied after overtime")]
        public bool enableShootout = true;

        [Tooltip("Number of shooters per team in shootout")]
        [Range(3, 10)]
        public int shootoutRounds = 3;

        [Header("Team Settings")]
        [Tooltip("Number of players per team (excluding goalie)")]
        [Range(3, 6)]
        public int playersPerTeam = 5; // 3v3, 4v4, 5v5, 6v6

        [Tooltip("Include goalies in the match")]
        public bool includeGoalie = true;

        [Tooltip("Allow line changes during play")]
        public bool allowLineChanges = true;

        [Tooltip("Maximum number of players on bench")]
        [Range(0, 20)]
        public int benchSize = 12;

        [Header("Game Mode")]
        [Tooltip("Current game mode")]
        public GameMode gameMode = GameMode.QuickMatch;

        [Header("Difficulty Settings")]
        [Tooltip("AI difficulty level")]
        public Difficulty difficulty = Difficulty.Normal;

        [Tooltip("AI reaction time in seconds (lower = harder)")]
        [Range(0.1f, 2.0f)]
        public float aiReactionTime = 0.3f;

        [Tooltip("AI accuracy multiplier")]
        [Range(0.1f, 2.0f)]
        public float aiAccuracyMultiplier = 1.0f;

        [Tooltip("AI aggression level")]
        [Range(0.1f, 2.0f)]
        public float aiAggressionLevel = 1.0f;

        [Header("Rules")]
        [Tooltip("Enable offsides rule")]
        public bool enableOffsides = true;

        [Tooltip("Enable icing rule")]
        public bool enableIcing = true;

        [Tooltip("Enable penalty system")]
        public bool enablePenalties = true;

        [Tooltip("Enable two-line pass rule")]
        public bool enableTwoLinePass = false;

        [Tooltip("Enable checking/body contact")]
        public bool enableChecking = true;

        [Tooltip("Enable fighting (minor penalties)")]
        public bool enableFighting = false;

        [Header("Penalty Settings")]
        [Tooltip("Duration of minor penalty in seconds")]
        public int minorPenaltyDuration = 120; // 2 minutes

        [Tooltip("Duration of major penalty in seconds")]
        public int majorPenaltyDuration = 300; // 5 minutes

        [Tooltip("Duration of misconduct penalty in seconds")]
        public int misconductPenaltyDuration = 600; // 10 minutes

        [Header("Player Movement - IMPORTANT")]
        [Tooltip("Global speed multiplier for all players")]
        [Range(0.1f, 3.0f)]
        public float playerSpeedMultiplier = 1.0f;

        [Tooltip("Base movement speed (m/s)")]
        [Range(1f, 20f)]
        public float basePlayerSpeed = 8.0f;

        [Tooltip("Sprint speed multiplier")]
        [Range(1.0f, 2.0f)]
        public float sprintMultiplier = 1.5f;

        [Tooltip("Acceleration rate")]
        [Range(1f, 50f)]
        public float playerAcceleration = 20.0f;

        [Tooltip("Deceleration/braking rate")]
        [Range(1f, 50f)]
        public float playerDeceleration = 25.0f;

        [Tooltip("Turn speed (degrees per second)")]
        [Range(90f, 720f)]
        public float playerTurnSpeed = 360.0f;

        [Header("Stamina Settings")]
        [Tooltip("Enable stamina system")]
        public bool enableStamina = true;

        [Tooltip("Maximum stamina value")]
        [Range(50f, 200f)]
        public float maxStamina = 100.0f;

        [Tooltip("Stamina drain rate while sprinting")]
        [Range(1f, 50f)]
        public float staminaDrainRate = 10.0f;

        [Tooltip("Stamina recovery rate while not sprinting")]
        [Range(1f, 50f)]
        public float staminaRecoveryRate = 15.0f;

        [Tooltip("Speed penalty when stamina is depleted (0-1)")]
        [Range(0f, 1f)]
        public float lowStaminaSpeedPenalty = 0.5f;

        [Header("Puck Physics")]
        [Tooltip("Puck mass in kg")]
        [Range(0.05f, 0.5f)]
        public float puckMass = 0.17f; // Official puck mass

        [Tooltip("Puck friction coefficient")]
        [Range(0f, 1f)]
        public float puckFriction = 0.1f;

        [Tooltip("Puck bounciness")]
        [Range(0f, 1f)]
        public float puckBounciness = 0.3f;

        [Tooltip("Maximum puck speed (m/s)")]
        [Range(10f, 100f)]
        public float maxPuckSpeed = 50.0f;

        [Header("Shooting Settings")]
        [Tooltip("Base shot power")]
        [Range(1f, 100f)]
        public float baseShotPower = 30.0f;

        [Tooltip("Shot accuracy spread (degrees)")]
        [Range(0f, 45f)]
        public float shotAccuracySpread = 5.0f;

        [Tooltip("Time to charge shot (seconds)")]
        [Range(0.1f, 3f)]
        public float shotChargeTime = 0.5f;

        [Tooltip("Maximum shot power multiplier when charged")]
        [Range(1f, 3f)]
        public float maxShotPowerMultiplier = 2.0f;

        [Header("Passing Settings")]
        [Tooltip("Base pass power")]
        [Range(1f, 50f)]
        public float basePassPower = 15.0f;

        [Tooltip("Pass accuracy spread (degrees)")]
        [Range(0f, 30f)]
        public float passAccuracySpread = 3.0f;

        [Header("Goalie Settings")]
        [Tooltip("Goalie reaction time (seconds)")]
        [Range(0.05f, 1f)]
        public float goalieReactionTime = 0.2f;

        [Tooltip("Goalie save percentage (0-1)")]
        [Range(0f, 1f)]
        public float goalieSavePercentage = 0.85f;

        [Tooltip("Goalie movement speed multiplier")]
        [Range(0.1f, 2f)]
        public float goalieSpeedMultiplier = 0.7f;

        [Header("Camera Settings")]
        [Tooltip("Camera follow smoothing")]
        [Range(1f, 20f)]
        public float cameraFollowSpeed = 10.0f;

        [Tooltip("Camera distance from action")]
        [Range(5f, 30f)]
        public float cameraDistance = 15.0f;

        [Tooltip("Camera height above ice")]
        [Range(5f, 30f)]
        public float cameraHeight = 12.0f;

        [Header("Audio Settings")]
        [Tooltip("Master volume (0-1)")]
        [Range(0f, 1f)]
        public float masterVolume = 1.0f;

        [Tooltip("SFX volume (0-1)")]
        [Range(0f, 1f)]
        public float sfxVolume = 0.8f;

        [Tooltip("Music volume (0-1)")]
        [Range(0f, 1f)]
        public float musicVolume = 0.6f;

        [Tooltip("Crowd/ambience volume (0-1)")]
        [Range(0f, 1f)]
        public float ambienceVolume = 0.7f;

        [Header("UI Settings")]
        [Tooltip("Show HUD elements")]
        public bool showHUD = true;

        [Tooltip("Show player names above heads")]
        public bool showPlayerNames = true;

        [Tooltip("Show stamina bars")]
        public bool showStaminaBars = true;

        [Tooltip("Show shot power indicator")]
        public bool showShotPowerIndicator = true;

        [Header("Advanced Settings")]
        [Tooltip("Physics update rate (fixed timestep)")]
        [Range(30, 120)]
        public int physicsUpdateRate = 60;

        [Tooltip("Enable replays")]
        public bool enableReplays = true;

        [Tooltip("Replay duration in seconds")]
        [Range(3f, 15f)]
        public float replayDuration = 5.0f;

        [Tooltip("Enable goal celebrations")]
        public bool enableGoalCelebrations = true;

        [Tooltip("Goal celebration duration")]
        [Range(1f, 10f)]
        public float goalCelebrationDuration = 3.0f;

        /// <summary>
        /// Get the actual player speed after applying multiplier
        /// </summary>
        public float GetEffectivePlayerSpeed()
        {
            return basePlayerSpeed * playerSpeedMultiplier;
        }

        /// <summary>
        /// Get the actual sprint speed
        /// </summary>
        public float GetSprintSpeed()
        {
            return GetEffectivePlayerSpeed() * sprintMultiplier;
        }

        /// <summary>
        /// Get AI difficulty settings based on difficulty level
        /// </summary>
        public void ApplyDifficultyPreset(Difficulty newDifficulty)
        {
            difficulty = newDifficulty;

            switch (difficulty)
            {
                case Difficulty.Easy:
                    aiReactionTime = 0.8f;
                    aiAccuracyMultiplier = 0.5f;
                    aiAggressionLevel = 0.6f;
                    goalieSavePercentage = 0.65f;
                    break;

                case Difficulty.Normal:
                    aiReactionTime = 0.4f;
                    aiAccuracyMultiplier = 1.0f;
                    aiAggressionLevel = 1.0f;
                    goalieSavePercentage = 0.80f;
                    break;

                case Difficulty.Hard:
                    aiReactionTime = 0.2f;
                    aiAccuracyMultiplier = 1.5f;
                    aiAggressionLevel = 1.4f;
                    goalieSavePercentage = 0.90f;
                    break;

                case Difficulty.Legendary:
                    aiReactionTime = 0.1f;
                    aiAccuracyMultiplier = 2.0f;
                    aiAggressionLevel = 1.8f;
                    goalieSavePercentage = 0.95f;
                    break;
            }
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        public void ResetToDefaults()
        {
            periodLengthSeconds = 300;
            periodCount = 3;
            enableOvertime = true;
            overtimeLengthSeconds = 300;
            enableShootout = true;
            shootoutRounds = 3;

            playersPerTeam = 5;
            includeGoalie = true;
            allowLineChanges = true;
            benchSize = 12;

            gameMode = GameMode.QuickMatch;
            difficulty = Difficulty.Normal;

            enableOffsides = true;
            enableIcing = true;
            enablePenalties = true;
            enableTwoLinePass = false;
            enableChecking = true;
            enableFighting = false;

            playerSpeedMultiplier = 1.0f;
            basePlayerSpeed = 8.0f;
            sprintMultiplier = 1.5f;
            playerAcceleration = 20.0f;
            playerDeceleration = 25.0f;
            playerTurnSpeed = 360.0f;

            enableStamina = true;
            maxStamina = 100.0f;
            staminaDrainRate = 10.0f;
            staminaRecoveryRate = 15.0f;

            ApplyDifficultyPreset(Difficulty.Normal);
        }

        /// <summary>
        /// Validate configuration values
        /// </summary>
        private void OnValidate()
        {
            // Ensure period count is at least 1
            periodCount = Mathf.Max(1, periodCount);

            // Ensure at least 3 players per team for hockey
            playersPerTeam = Mathf.Max(3, playersPerTeam);

            // Ensure positive values for time settings
            periodLengthSeconds = Mathf.Max(60, periodLengthSeconds);
            overtimeLengthSeconds = Mathf.Max(60, overtimeLengthSeconds);

            // Ensure shootout rounds is at least 3
            shootoutRounds = Mathf.Max(3, shootoutRounds);
        }
    }

    /// <summary>
    /// Game mode enumeration
    /// </summary>
    public enum GameMode
    {
        QuickMatch,    // Single match
        Practice,      // Practice mode with no score
        Season,        // Season/campaign mode
        Tournament,    // Tournament bracket
        Shootout,      // Shootout only mode
        Challenge      // Special challenge mode
    }

    /// <summary>
    /// Difficulty level enumeration
    /// </summary>
    public enum Difficulty
    {
        Easy,          // Beginner friendly
        Normal,        // Balanced gameplay
        Hard,          // Challenging
        Legendary      // Expert level
    }
}
