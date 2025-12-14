using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the flow of a hockey match: faceoffs, periods, scoring, etc.
/// </summary>
public class MatchManager : MonoBehaviour
{
    [Header("Match Settings")]
    [SerializeField] private int goalsToWin = 3;
    [SerializeField] private float periodLength = 300f; // 5 minutes per period
    [SerializeField] private float intermissionLength = 30f; // 30 seconds between periods
    [SerializeField] private float overtimeLength = 300f; // 5 minute OT (sudden death)
    [SerializeField] private float faceoffDelay = 2f;

    [Header("References")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform opponentSpawnPoint;
    [SerializeField] private Transform puckSpawnPoint;
    [SerializeField] private HockeyPlayer playerPrefab;
    [SerializeField] private HockeyPlayer aiPrefab;
    [SerializeField] private Puck puckPrefab;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject pausePanel;

    [Header("Goals")]
    [SerializeField] private Goal playerGoal; // AI scores here
    [SerializeField] private Goal opponentGoal; // Player scores here

    // Runtime references
    private HockeyPlayer playerInstance;
    private HockeyPlayer aiInstance;
    private Puck puckInstance;

    // Match state
    private int[] scores = new int[2]; // [0] = player, [1] = AI
    private float periodTimer;
    private int currentPeriod = 1; // 1-3 for regulation, 4+ for overtime
    private bool isIntermission;
    private float intermissionTimer;
    private bool isOvertime;
    private bool isMatchRunning;
    private bool isPaused;

    // Public properties for UI
    public bool IsMatchRunning => isMatchRunning;
    public bool IsPaused => isPaused;
    public int CurrentPeriod => currentPeriod;
    public bool IsOvertime => isOvertime;
    public bool IsIntermission => isIntermission;
    public float PeriodTimer => periodTimer;
    public float IntermissionTimer => intermissionTimer;

    // Events
    public static event System.Action<int> OnPeriodEnd; // Period number that ended
    public static event System.Action OnIntermissionStart;
    public static event System.Action OnOvertimeStart;

    private void OnEnable()
    {
        GameEvents.OnGoalScored += OnGoalScored;
        GameEvents.OnMatchStart += OnMatchStart;
    }

    private void OnDisable()
    {
        GameEvents.OnGoalScored -= OnGoalScored;
        GameEvents.OnMatchStart -= OnMatchStart;
    }

    private void Start()
    {
        // Initialize match when scene loads
        // In a full game, this would be triggered by GameManager
        if (GameManager.Instance != null && GameManager.Instance.IsRunActive)
        {
            SetupMatch();
        }
        else
        {
            // Standalone testing - start a match directly
            Debug.Log("[MatchManager] No active run, starting standalone match");
            SetupMatch();
            StartMatch();
        }
    }

    private void Update()
    {
        if (!isMatchRunning || isPaused) return;

        // Handle intermission
        if (isIntermission)
        {
            intermissionTimer -= Time.deltaTime;
            UpdateTimerUI();

            if (intermissionTimer <= 0)
            {
                EndIntermission();
            }
        }
        else
        {
            // Update period timer
            periodTimer -= Time.deltaTime;
            UpdateTimerUI();

            // Check for period end
            if (periodTimer <= 0)
            {
                HandlePeriodEnd();
            }
        }

        // Pause input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // === MATCH SETUP ===

    private void SetupMatch()
    {
        Debug.Log("[MatchManager] Setting up match...");

        // Spawn player
        if (playerPrefab != null && playerSpawnPoint != null)
        {
            playerInstance = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
            playerInstance.name = "Player";

            // Connect to InputManager
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetControlledPlayer(playerInstance);
            }
        }

        // Spawn AI
        if (aiPrefab != null && opponentSpawnPoint != null)
        {
            aiInstance = Instantiate(aiPrefab, opponentSpawnPoint.position, opponentSpawnPoint.rotation);
            aiInstance.name = "AI_Opponent";

            // Setup AI controller
            var aiController = aiInstance.GetComponent<AIController>();
            if (aiController != null)
            {
                aiController.SetGoals(playerGoal?.transform, opponentGoal?.transform);
            }
        }

        // Spawn puck
        if (puckPrefab != null && puckSpawnPoint != null)
        {
            puckInstance = Instantiate(puckPrefab, puckSpawnPoint.position, Quaternion.identity);
            puckInstance.name = "Puck";
        }

        // Reset scores
        scores[0] = 0;
        scores[1] = 0;
        UpdateScoreUI();

        // Reset period tracking
        currentPeriod = 1;
        isOvertime = false;
        isIntermission = false;
        periodTimer = periodLength;
        UpdateTimerUI();
    }

    // === MATCH FLOW ===

    public void StartMatch()
    {
        Debug.Log("[MatchManager] Starting match!");
        isMatchRunning = true;
        isPaused = false;

        ShowMessage("FACE OFF!", 2f);

        // Do initial faceoff
        StartCoroutine(DoFaceoff());
    }

    private void OnMatchStart()
    {
        // Called by GameManager
        StartMatch();
    }

    private void HandlePeriodEnd()
    {
        Debug.Log($"[MatchManager] Period {currentPeriod} ended!");

        // Trigger period end event
        OnPeriodEnd?.Invoke(currentPeriod);

        // Check if this was regulation (periods 1-3)
        if (currentPeriod < 3)
        {
            // Start intermission
            StartIntermission();
        }
        else if (!isOvertime)
        {
            // End of regulation - check for overtime
            if (scores[0] == scores[1])
            {
                // Tied - go to overtime
                StartOvertime();
            }
            else
            {
                // Not tied - end match
                EndMatch();
            }
        }
        else
        {
            // Overtime ended (should not happen in sudden death, but handle it)
            EndMatch();
        }
    }

    private void StartIntermission()
    {
        isIntermission = true;
        intermissionTimer = intermissionLength;

        // Trigger intermission event
        OnIntermissionStart?.Invoke();

        string message = $"END OF PERIOD {currentPeriod}\nINTERMISSION";
        ShowMessage(message, 3f);

        Debug.Log($"[MatchManager] Intermission started after period {currentPeriod}");
    }

    private void EndIntermission()
    {
        isIntermission = false;
        currentPeriod++;
        periodTimer = periodLength;

        Debug.Log($"[MatchManager] Intermission ended. Starting period {currentPeriod}");

        ShowMessage($"PERIOD {currentPeriod}", 2f);

        // Do faceoff to start new period
        StartCoroutine(DoFaceoff());
    }

    private void StartOvertime()
    {
        isOvertime = true;
        currentPeriod = 4; // OT is period 4
        periodTimer = overtimeLength;

        // Trigger overtime event
        OnOvertimeStart?.Invoke();

        ShowMessage("OVERTIME\nSUDDEN DEATH!", 3f);

        Debug.Log("[MatchManager] Overtime started!");

        // Do faceoff to start overtime
        StartCoroutine(DoFaceoff());
    }

    public void EndMatch()
    {
        Debug.Log("[MatchManager] Match ended!");
        isMatchRunning = false;

        // Determine winner
        bool playerWon = scores[0] > scores[1];
        string resultMessage = playerWon ? "YOU WIN!" : (scores[0] == scores[1] ? "DRAW!" : "YOU LOSE!");

        ShowMessage(resultMessage, 3f);

        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndMatch();
        }
    }

    private void OnGoalScored(int teamIndex)
    {
        scores[teamIndex]++;
        UpdateScoreUI();

        string message = teamIndex == 0 ? "GOAL!" : "OPPONENT SCORES!";
        ShowMessage(message, 2f);

        Debug.Log($"[MatchManager] Goal! Score: {scores[0]} - {scores[1]}");

        // In overtime (sudden death), end match immediately
        if (isOvertime)
        {
            EndMatch();
            return;
        }

        // Check for match end (if goalsToWin is still enabled)
        if (goalsToWin > 0 && (scores[0] >= goalsToWin || scores[1] >= goalsToWin))
        {
            EndMatch();
        }
        else
        {
            // Reset for faceoff
            StartCoroutine(DoFaceoff());
        }
    }

    private IEnumerator DoFaceoff()
    {
        Debug.Log("[MatchManager] Faceoff...");

        // Reset positions
        if (playerInstance != null && playerSpawnPoint != null)
        {
            playerInstance.transform.position = playerSpawnPoint.position;
            playerInstance.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }

        if (aiInstance != null && opponentSpawnPoint != null)
        {
            aiInstance.transform.position = opponentSpawnPoint.position;
            aiInstance.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }

        if (puckInstance != null && puckSpawnPoint != null)
        {
            puckInstance.transform.position = puckSpawnPoint.position;
            puckInstance.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            puckInstance.LosePossession();
        }

        yield return new WaitForSeconds(faceoffDelay);

        ShowMessage("GO!", 1f);
    }

    // === PAUSE ===

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }

        Debug.Log($"[MatchManager] Paused: {isPaused}");
    }

    public void ResumeMatch()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }

    public void QuitMatch()
    {
        Time.timeScale = 1f;

        // Return to menu or end run
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndRun(false);
        }
    }

    // === UI ===

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{scores[0]} - {scores[1]}";
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            if (isIntermission)
            {
                // Show intermission timer
                int seconds = Mathf.FloorToInt(intermissionTimer);
                timerText.text = $"INTERMISSION: {seconds}s";
            }
            else
            {
                // Show period timer
                int minutes = Mathf.FloorToInt(periodTimer / 60f);
                int seconds = Mathf.FloorToInt(periodTimer % 60f);

                string periodLabel = isOvertime ? "OT" : $"P{currentPeriod}";
                timerText.text = $"{periodLabel} {minutes}:{seconds:00}";
            }
        }
    }

    private void ShowMessage(string message, float duration)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);

            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), duration);
        }
    }

    private void HideMessage()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    // === DEBUG ===

    [ContextMenu("Force Player Goal")]
    private void DebugPlayerGoal()
    {
        OnGoalScored(0);
    }

    [ContextMenu("Force AI Goal")]
    private void DebugAIGoal()
    {
        OnGoalScored(1);
    }
}
