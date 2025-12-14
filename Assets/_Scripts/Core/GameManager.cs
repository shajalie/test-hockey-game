using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Singleton manager for roguelite run state.
/// Persists between scenes and manages artifacts, score, and progression.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Run Configuration")]
    [SerializeField] private PlayerStats defaultPlayerStats;
    [SerializeField] private List<RunModifier> allArtifacts = new List<RunModifier>();
    [SerializeField] private int artifactChoicesPerDraft = 3;

    [Header("Current Run State")]
    [SerializeField] private List<RunModifier> currentRunArtifacts = new List<RunModifier>();
    [SerializeField] private int currentMatchNumber = 0;
    [SerializeField] private int totalWins = 0;
    [SerializeField] private int totalLosses = 0;

    // Team scores for current match
    private int[] teamScores = new int[2];

    // Runtime stats for the player (with artifacts applied)
    private RuntimeStats playerRuntimeStats;

    // Properties
    public PlayerStats DefaultStats => defaultPlayerStats;
    public RuntimeStats PlayerStats => playerRuntimeStats;
    public IReadOnlyList<RunModifier> CurrentArtifacts => currentRunArtifacts.AsReadOnly();
    public int CurrentMatch => currentMatchNumber;
    public int Wins => totalWins;
    public int Losses => totalLosses;
    public int PlayerScore => teamScores[0];
    public int OpponentScore => teamScores[1];

    // Run state
    public bool IsRunActive { get; private set; }
    public bool IsMatchActive { get; private set; }

    private void Awake()
    {
        // Singleton pattern with persistence
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize runtime stats
        if (defaultPlayerStats != null)
        {
            playerRuntimeStats = new RuntimeStats(defaultPlayerStats);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnGoalScored += OnGoalScored;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnGoalScored -= OnGoalScored;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // === RUN MANAGEMENT ===

    /// <summary>
    /// Start a new roguelite run.
    /// </summary>
    public void StartNewRun()
    {
        Debug.Log("[GameManager] Starting new run!");

        // Reset run state
        currentRunArtifacts.Clear();
        currentMatchNumber = 0;
        totalWins = 0;
        totalLosses = 0;
        IsRunActive = true;

        // Reset player stats
        playerRuntimeStats?.ClearModifiers();

        // Start with draft
        StartDraft();
    }

    /// <summary>
    /// End the current run (win or lose).
    /// </summary>
    public void EndRun(bool victory)
    {
        Debug.Log($"[GameManager] Run ended. Victory: {victory}");

        IsRunActive = false;
        IsMatchActive = false;

        // Could trigger end-of-run UI, save stats, etc.
        GameEvents.TriggerMatchEnd();
    }

    // === MATCH MANAGEMENT ===

    /// <summary>
    /// Start a new match within the run.
    /// </summary>
    public void StartMatch()
    {
        if (!IsRunActive)
        {
            Debug.LogWarning("[GameManager] Cannot start match - no active run!");
            return;
        }

        currentMatchNumber++;
        teamScores[0] = 0;
        teamScores[1] = 0;
        IsMatchActive = true;

        Debug.Log($"[GameManager] Starting match #{currentMatchNumber}");
        GameEvents.TriggerMatchStart();
    }

    /// <summary>
    /// End the current match.
    /// </summary>
    public void EndMatch()
    {
        if (!IsMatchActive) return;

        IsMatchActive = false;

        bool playerWon = teamScores[0] > teamScores[1];

        if (playerWon)
        {
            totalWins++;
            Debug.Log($"[GameManager] Match won! Total wins: {totalWins}");

            // Continue run - draft new artifact
            StartDraft();
        }
        else
        {
            totalLosses++;
            Debug.Log($"[GameManager] Match lost! Total losses: {totalLosses}");

            // Check if run is over (e.g., 3 losses = game over)
            if (totalLosses >= 3)
            {
                EndRun(false);
            }
            else
            {
                // Continue with next match
                StartMatch();
            }
        }

        GameEvents.TriggerMatchEnd();
    }

    private void OnGoalScored(int teamIndex)
    {
        if (!IsMatchActive) return;

        teamScores[teamIndex]++;
        Debug.Log($"[GameManager] Goal! Team {teamIndex}. Score: {teamScores[0]} - {teamScores[1]}");

        // Check for match end (first to 3 goals, for example)
        if (teamScores[0] >= 3 || teamScores[1] >= 3)
        {
            EndMatch();
        }
    }

    // === DRAFT SYSTEM ===

    /// <summary>
    /// Start the artifact draft phase.
    /// </summary>
    public void StartDraft()
    {
        Debug.Log("[GameManager] Starting artifact draft...");
        GameEvents.TriggerDraftStarted();

        // DraftSystem will handle the UI and selection
    }

    /// <summary>
    /// Get random artifact choices for draft.
    /// </summary>
    public List<RunModifier> GetDraftChoices()
    {
        List<RunModifier> choices = new List<RunModifier>();
        List<RunModifier> available = new List<RunModifier>(allArtifacts);

        // Remove already owned artifacts
        foreach (var owned in currentRunArtifacts)
        {
            available.Remove(owned);
        }

        // Pick random choices
        for (int i = 0; i < artifactChoicesPerDraft && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            choices.Add(available[index]);
            available.RemoveAt(index);
        }

        return choices;
    }

    /// <summary>
    /// Player selects an artifact from draft.
    /// </summary>
    public void SelectArtifact(RunModifier artifact)
    {
        if (artifact == null) return;

        currentRunArtifacts.Add(artifact);
        playerRuntimeStats?.AddModifier(artifact);

        Debug.Log($"[GameManager] Artifact acquired: {artifact.artifactName}");

        // After draft, start next match
        StartMatch();
    }

    /// <summary>
    /// Skip the draft (take no artifact).
    /// </summary>
    public void SkipDraft()
    {
        Debug.Log("[GameManager] Draft skipped");
        StartMatch();
    }

    // === SCENE MANAGEMENT ===

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: {scene.name}");

        // Could auto-start match when game scene loads
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // === UTILITY ===

    /// <summary>
    /// Get current run summary for UI.
    /// </summary>
    public string GetRunSummary()
    {
        return $"Match {currentMatchNumber} | W:{totalWins} L:{totalLosses} | Artifacts: {currentRunArtifacts.Count}";
    }

    /// <summary>
    /// Reset everything (for testing).
    /// </summary>
    [ContextMenu("Reset All")]
    public void ResetAll()
    {
        currentRunArtifacts.Clear();
        playerRuntimeStats?.ClearModifiers();
        currentMatchNumber = 0;
        totalWins = 0;
        totalLosses = 0;
        IsRunActive = false;
        IsMatchActive = false;
        teamScores[0] = 0;
        teamScores[1] = 0;
    }
}
