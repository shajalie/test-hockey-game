using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Complete Hockey Game HUD system with professional sports-broadcast style.
/// Auto-creates UI elements if they don't exist.
/// Shows scores, timer, period, stamina, shot power, puck possession, player info, and celebrations.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("References (Auto-Created if Null)")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private TeamManager teamManager;

    [Header("HUD Settings")]
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private int canvasSortOrder = 100;

    [Header("Display Options")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private float shotPowerDisplayTime = 2f;
    [SerializeField] private float goalCelebrationTime = 3f;
    [SerializeField] private float faceoffIndicatorTime = 2f;

    // UI Element References (created at runtime)
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI periodText;
    private Image shotPowerBar;
    private GameObject shotPowerContainer;
    private Image staminaBar;
    private TextMeshProUGUI staminaText;
    private TextMeshProUGUI possessionIndicator;
    private TextMeshProUGUI controlledPlayerText;
    private GameObject playerSwitchIndicator;
    private GameObject goalCelebrationPopup;
    private TextMeshProUGUI goalCelebrationText;
    private GameObject faceoffIndicator;
    private TextMeshProUGUI faceoffText;

    // Runtime state
    private HockeyPlayer currentPlayer;
    private float currentShotPower = 0f;
    private bool isChargingShot = false;
    private int currentPeriod = 1;
    private GameObject lastPuckOwner;

    private void Awake()
    {
        FindOrCreateReferences();

        if (autoCreateUI)
        {
            CreateHUDElements();
        }
    }

    private void OnEnable()
    {
        // Subscribe to game events
        GameEvents.OnGoalScored += OnGoalScored;
        GameEvents.OnMatchStart += OnMatchStart;
        GameEvents.OnMatchEnd += OnMatchEnd;
        GameEvents.OnPuckPossessionChanged += OnPuckPossessionChanged;
        GameEvents.OnShotTaken += OnShotTaken;

        if (teamManager != null)
        {
            teamManager.OnPlayerSwitched += OnPlayerSwitched;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        GameEvents.OnGoalScored -= OnGoalScored;
        GameEvents.OnMatchStart -= OnMatchStart;
        GameEvents.OnMatchEnd -= OnMatchEnd;
        GameEvents.OnPuckPossessionChanged -= OnPuckPossessionChanged;
        GameEvents.OnShotTaken -= OnShotTaken;

        if (teamManager != null)
        {
            teamManager.OnPlayerSwitched -= OnPlayerSwitched;
        }
    }

    private void Update()
    {
        UpdateTimer();
        UpdateStamina();
        UpdateShotPower();
        UpdateControlledPlayer();
    }

    #region Initialization

    /// <summary>
    /// Find or create manager references.
    /// </summary>
    private void FindOrCreateReferences()
    {
        // Find GameManager
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        // Find MatchManager
        if (matchManager == null)
        {
            matchManager = FindObjectOfType<MatchManager>();
        }

        // Find TeamManager
        if (teamManager == null)
        {
            teamManager = FindObjectOfType<TeamManager>();
        }

        // Create or find Canvas
        if (hudCanvas == null)
        {
            hudCanvas = GetComponentInChildren<Canvas>();

            if (hudCanvas == null && autoCreateUI)
            {
                GameObject canvasObj = new GameObject("HUD Canvas");
                canvasObj.transform.SetParent(transform);
                hudCanvas = canvasObj.AddComponent<Canvas>();
                hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hudCanvas.sortingOrder = canvasSortOrder;

                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }
    }

    /// <summary>
    /// Create all HUD UI elements programmatically.
    /// </summary>
    private void CreateHUDElements()
    {
        if (hudCanvas == null)
        {
            Debug.LogError("[GameHUD] Cannot create UI elements - no canvas!");
            return;
        }

        // Create main container
        GameObject hudRoot = new GameObject("HUD Root");
        hudRoot.transform.SetParent(hudCanvas.transform, false);
        RectTransform rootRect = hudRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        // Create all UI elements
        CreateScoreDisplay(hudRoot.transform);
        CreateTimerDisplay(hudRoot.transform);
        CreatePeriodDisplay(hudRoot.transform);
        CreatePossessionIndicator(hudRoot.transform);
        CreateStaminaBar(hudRoot.transform);
        CreateShotPowerBar(hudRoot.transform);
        CreateControlledPlayerDisplay(hudRoot.transform);
        CreatePlayerSwitchIndicator(hudRoot.transform);
        CreateGoalCelebrationPopup(hudRoot.transform);
        CreateFaceoffIndicator(hudRoot.transform);

        Debug.Log("[GameHUD] All UI elements created successfully!");
    }

    /// <summary>
    /// Create score display (big, centered at top).
    /// </summary>
    private void CreateScoreDisplay(Transform parent)
    {
        GameObject scoreObj = CreateUIElement("Score Display", parent);
        RectTransform rect = scoreObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(400, 120);

        // Background panel
        Image bg = scoreObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        // Score text
        scoreText = CreateTextElement("Score Text", scoreObj.transform);
        scoreText.fontSize = 80;
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.text = "0 - 0";
        scoreText.color = Color.white;

        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Create timer display.
    /// </summary>
    private void CreateTimerDisplay(Transform parent)
    {
        GameObject timerObj = CreateUIElement("Timer Display", parent);
        RectTransform rect = timerObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -150);
        rect.sizeDelta = new Vector2(200, 60);

        // Background
        Image bg = timerObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Timer text
        timerText = CreateTextElement("Timer Text", timerObj.transform);
        timerText.fontSize = 48;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.text = "3:00";
        timerText.color = new Color(1f, 0.9f, 0.3f);

        RectTransform timerRect = timerText.GetComponent<RectTransform>();
        timerRect.anchorMin = Vector2.zero;
        timerRect.anchorMax = Vector2.one;
        timerRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Create period indicator.
    /// </summary>
    private void CreatePeriodDisplay(Transform parent)
    {
        GameObject periodObj = CreateUIElement("Period Display", parent);
        RectTransform rect = periodObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -220);
        rect.sizeDelta = new Vector2(150, 40);

        // Background
        Image bg = periodObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        // Period text
        periodText = CreateTextElement("Period Text", periodObj.transform);
        periodText.fontSize = 28;
        periodText.fontStyle = FontStyles.Bold;
        periodText.alignment = TextAlignmentOptions.Center;
        periodText.text = "1st PERIOD";
        periodText.color = Color.white;

        RectTransform periodRect = periodText.GetComponent<RectTransform>();
        periodRect.anchorMin = Vector2.zero;
        periodRect.anchorMax = Vector2.one;
        periodRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Create stamina bar for controlled player.
    /// </summary>
    private void CreateStaminaBar(Transform parent)
    {
        GameObject staminaObj = CreateUIElement("Stamina Bar", parent);
        RectTransform rect = staminaObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(20, 20);
        rect.sizeDelta = new Vector2(300, 40);

        // Background
        Image bg = staminaObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Stamina fill
        GameObject fillObj = CreateUIElement("Stamina Fill", staminaObj.transform);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.sizeDelta = new Vector2(-10, -10);
        fillRect.anchoredPosition = Vector2.zero;

        staminaBar = fillObj.AddComponent<Image>();
        staminaBar.color = new Color(0.2f, 1f, 0.3f, 0.9f);
        staminaBar.type = Image.Type.Filled;
        staminaBar.fillMethod = Image.FillMethod.Horizontal;
        staminaBar.fillAmount = 1f;

        // Stamina text
        staminaText = CreateTextElement("Stamina Text", staminaObj.transform);
        staminaText.fontSize = 24;
        staminaText.fontStyle = FontStyles.Bold;
        staminaText.alignment = TextAlignmentOptions.Center;
        staminaText.text = "STAMINA";
        staminaText.color = Color.white;

        RectTransform staminaTextRect = staminaText.GetComponent<RectTransform>();
        staminaTextRect.anchorMin = Vector2.zero;
        staminaTextRect.anchorMax = Vector2.one;
        staminaTextRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Create shot power charge bar.
    /// </summary>
    private void CreateShotPowerBar(Transform parent)
    {
        shotPowerContainer = CreateUIElement("Shot Power Container", parent);
        RectTransform rect = shotPowerContainer.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 100);
        rect.sizeDelta = new Vector2(400, 50);
        shotPowerContainer.SetActive(false);

        // Background
        Image bg = shotPowerContainer.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Power fill
        GameObject fillObj = CreateUIElement("Power Fill", shotPowerContainer.transform);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.sizeDelta = new Vector2(-10, -10);
        fillRect.anchoredPosition = Vector2.zero;

        shotPowerBar = fillObj.AddComponent<Image>();
        shotPowerBar.color = new Color(1f, 0.3f, 0.1f, 0.9f);
        shotPowerBar.type = Image.Type.Filled;
        shotPowerBar.fillMethod = Image.FillMethod.Horizontal;
        shotPowerBar.fillAmount = 0f;

        // Label
        TextMeshProUGUI label = CreateTextElement("Power Label", shotPowerContainer.transform);
        label.fontSize = 28;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.text = "SHOT POWER";
        label.color = Color.white;

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Create puck possession indicator.
    /// </summary>
    private void CreatePossessionIndicator(Transform parent)
    {
        GameObject possessionObj = CreateUIElement("Possession Indicator", parent);
        RectTransform rect = possessionObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(250, 50);

        // Background
        Image bg = possessionObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Possession text
        possessionIndicator = CreateTextElement("Possession Text", possessionObj.transform);
        possessionIndicator.fontSize = 32;
        possessionIndicator.fontStyle = FontStyles.Bold;
        possessionIndicator.alignment = TextAlignmentOptions.Center;
        possessionIndicator.text = "LOOSE PUCK";
        possessionIndicator.color = Color.yellow;

        RectTransform possessionRect = possessionIndicator.GetComponent<RectTransform>();
        possessionRect.anchorMin = Vector2.zero;
        possessionRect.anchorMax = Vector2.one;
        possessionRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Create controlled player name display.
    /// </summary>
    private void CreateControlledPlayerDisplay(Transform parent)
    {
        GameObject playerObj = CreateUIElement("Controlled Player", parent);
        RectTransform rect = playerObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(20, 70);
        rect.sizeDelta = new Vector2(250, 50);

        // Background
        Image bg = playerObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.3f, 0.6f, 0.8f);

        // Player text
        controlledPlayerText = CreateTextElement("Player Text", playerObj.transform);
        controlledPlayerText.fontSize = 28;
        controlledPlayerText.fontStyle = FontStyles.Bold;
        controlledPlayerText.alignment = TextAlignmentOptions.Center;
        controlledPlayerText.text = "CENTER";
        controlledPlayerText.color = Color.white;

        RectTransform playerTextRect = controlledPlayerText.GetComponent<RectTransform>();
        playerTextRect.anchorMin = Vector2.zero;
        playerTextRect.anchorMax = Vector2.one;
        playerTextRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Create player switch indicator (mini arrows or icons).
    /// </summary>
    private void CreatePlayerSwitchIndicator(Transform parent)
    {
        playerSwitchIndicator = CreateUIElement("Player Switch Indicator", parent);
        RectTransform rect = playerSwitchIndicator.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(280, 70);
        rect.sizeDelta = new Vector2(150, 50);
        playerSwitchIndicator.SetActive(false);

        // Background
        Image bg = playerSwitchIndicator.AddComponent<Image>();
        bg.color = new Color(0.9f, 0.6f, 0.1f, 0.8f);

        // Switch text
        TextMeshProUGUI switchText = CreateTextElement("Switch Text", playerSwitchIndicator.transform);
        switchText.fontSize = 24;
        switchText.fontStyle = FontStyles.Bold;
        switchText.alignment = TextAlignmentOptions.Center;
        switchText.text = "SWITCH: Q/E";
        switchText.color = Color.white;

        RectTransform switchRect = switchText.GetComponent<RectTransform>();
        switchRect.anchorMin = Vector2.zero;
        switchRect.anchorMax = Vector2.one;
        switchRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Create goal celebration popup (big, flashy).
    /// </summary>
    private void CreateGoalCelebrationPopup(Transform parent)
    {
        goalCelebrationPopup = CreateUIElement("Goal Celebration", parent);
        RectTransform rect = goalCelebrationPopup.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800, 300);
        goalCelebrationPopup.SetActive(false);

        // Background (semi-transparent)
        Image bg = goalCelebrationPopup.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        // Goal text
        goalCelebrationText = CreateTextElement("Goal Text", goalCelebrationPopup.transform);
        goalCelebrationText.fontSize = 120;
        goalCelebrationText.fontStyle = FontStyles.Bold;
        goalCelebrationText.alignment = TextAlignmentOptions.Center;
        goalCelebrationText.text = "GOAL!";
        goalCelebrationText.color = new Color(1f, 0.8f, 0.1f);

        RectTransform goalRect = goalCelebrationText.GetComponent<RectTransform>();
        goalRect.anchorMin = Vector2.zero;
        goalRect.anchorMax = Vector2.one;
        goalRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Create face-off indicator.
    /// </summary>
    private void CreateFaceoffIndicator(Transform parent)
    {
        faceoffIndicator = CreateUIElement("Faceoff Indicator", parent);
        RectTransform rect = faceoffIndicator.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600, 200);
        faceoffIndicator.SetActive(false);

        // Background
        Image bg = faceoffIndicator.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Faceoff text
        faceoffText = CreateTextElement("Faceoff Text", faceoffIndicator.transform);
        faceoffText.fontSize = 80;
        faceoffText.fontStyle = FontStyles.Bold;
        faceoffText.alignment = TextAlignmentOptions.Center;
        faceoffText.text = "FACE OFF";
        faceoffText.color = Color.cyan;

        RectTransform faceoffRect = faceoffText.GetComponent<RectTransform>();
        faceoffRect.anchorMin = Vector2.zero;
        faceoffRect.anchorMax = Vector2.one;
        faceoffRect.sizeDelta = Vector2.zero;
    }

    #endregion

    #region UI Update Methods

    /// <summary>
    /// Update score display.
    /// </summary>
    private void UpdateScore()
    {
        if (scoreText == null || gameManager == null) return;

        int playerScore = gameManager.PlayerScore;
        int opponentScore = gameManager.OpponentScore;

        scoreText.text = $"{playerScore} - {opponentScore}";

        // Color based on who's winning
        if (playerScore > opponentScore)
        {
            scoreText.color = new Color(0.3f, 1f, 0.3f); // Green
        }
        else if (playerScore < opponentScore)
        {
            scoreText.color = new Color(1f, 0.3f, 0.3f); // Red
        }
        else
        {
            scoreText.color = Color.white;
        }
    }

    /// <summary>
    /// Update timer display.
    /// </summary>
    private void UpdateTimer()
    {
        if (timerText == null) return;

        // Get match time from MatchManager if available
        if (matchManager != null)
        {
            // MatchManager doesn't expose timer directly, so we'll use a generic countdown
            // In a full implementation, you'd add a public property to MatchManager
            timerText.text = "3:00";
        }
        else
        {
            timerText.text = "0:00";
        }
    }

    /// <summary>
    /// Update period indicator.
    /// </summary>
    private void UpdatePeriod(int period)
    {
        if (periodText == null) return;

        currentPeriod = period;

        string suffix;
        switch (period)
        {
            case 1: suffix = "st"; break;
            case 2: suffix = "nd"; break;
            case 3: suffix = "rd"; break;
            default: suffix = "th"; break;
        }

        periodText.text = $"{period}{suffix} PERIOD";
    }

    /// <summary>
    /// Update stamina bar for controlled player.
    /// </summary>
    private void UpdateStamina()
    {
        if (staminaBar == null || staminaText == null) return;

        if (currentPlayer != null)
        {
            float stamina = currentPlayer.CurrentStamina;
            float maxStamina = currentPlayer.MaxStamina;
            float staminaPercent = stamina / maxStamina;

            staminaBar.fillAmount = staminaPercent;

            // Color based on stamina level
            if (staminaPercent > 0.6f)
            {
                staminaBar.color = new Color(0.2f, 1f, 0.3f, 0.9f); // Green
            }
            else if (staminaPercent > 0.3f)
            {
                staminaBar.color = new Color(1f, 0.8f, 0.2f, 0.9f); // Yellow
            }
            else
            {
                staminaBar.color = new Color(1f, 0.2f, 0.2f, 0.9f); // Red
            }

            staminaText.text = $"STAMINA: {Mathf.RoundToInt(stamina)}";
        }
        else
        {
            staminaBar.fillAmount = 0f;
            staminaText.text = "STAMINA";
        }
    }

    /// <summary>
    /// Update shot power charge bar.
    /// </summary>
    private void UpdateShotPower()
    {
        if (shotPowerBar == null || shotPowerContainer == null) return;

        // Check if player is charging a shot
        // This would need to be implemented in your shooting system
        // For now, we'll show/hide based on a simple flag

        if (isChargingShot)
        {
            shotPowerContainer.SetActive(true);
            shotPowerBar.fillAmount = currentShotPower;

            // Color changes as power increases
            float t = currentShotPower;
            shotPowerBar.color = Color.Lerp(
                new Color(1f, 0.8f, 0.2f, 0.9f), // Yellow
                new Color(1f, 0.2f, 0.1f, 0.9f), // Red
                t
            );
        }
        else
        {
            shotPowerContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Update controlled player display.
    /// </summary>
    private void UpdateControlledPlayer()
    {
        if (controlledPlayerText == null) return;

        if (currentPlayer != null)
        {
            PlayerPosition pos = currentPlayer.Position;
            controlledPlayerText.text = GetPositionDisplayName(pos);

            // Show player switch indicator if there are other players
            if (playerSwitchIndicator != null && teamManager != null)
            {
                playerSwitchIndicator.SetActive(teamManager.HomeTeam.Players.Count > 1);
            }
        }
        else
        {
            controlledPlayerText.text = "NO PLAYER";
            if (playerSwitchIndicator != null)
            {
                playerSwitchIndicator.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Update puck possession indicator.
    /// </summary>
    private void UpdatePossession(GameObject puckOwner)
    {
        if (possessionIndicator == null) return;

        if (puckOwner == null)
        {
            possessionIndicator.text = "LOOSE PUCK";
            possessionIndicator.color = Color.yellow;
        }
        else
        {
            HockeyPlayer player = puckOwner.GetComponent<HockeyPlayer>();
            if (player != null)
            {
                int teamId = player.TeamId;

                if (teamId == 0) // Player's team
                {
                    possessionIndicator.text = "YOUR PUCK";
                    possessionIndicator.color = new Color(0.3f, 1f, 0.3f); // Green
                }
                else // Opponent's team
                {
                    possessionIndicator.text = "OPPONENT PUCK";
                    possessionIndicator.color = new Color(1f, 0.3f, 0.3f); // Red
                }
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnMatchStart()
    {
        UpdateScore();
        UpdatePeriod(1);

        // Show face-off indicator
        ShowFaceoff("FACE OFF!");
    }

    private void OnMatchEnd()
    {
        // Could show match result here
    }

    private void OnGoalScored(int teamIndex)
    {
        UpdateScore();

        if (teamIndex == 0)
        {
            ShowGoalCelebration("GOAL!", new Color(0.3f, 1f, 0.3f));
        }
        else
        {
            ShowGoalCelebration("OPPONENT SCORES!", new Color(1f, 0.3f, 0.3f));
        }
    }

    private void OnPuckPossessionChanged(GameObject newOwner)
    {
        lastPuckOwner = newOwner;
        UpdatePossession(newOwner);
    }

    private void OnShotTaken(Vector3 direction, float power)
    {
        // Reset shot charging
        isChargingShot = false;
        currentShotPower = 0f;
    }

    private void OnPlayerSwitched(HockeyPlayer newPlayer)
    {
        currentPlayer = newPlayer;
        UpdateControlledPlayer();

        // Flash the player switch indicator
        if (playerSwitchIndicator != null)
        {
            StartCoroutine(FlashPlayerSwitchIndicator());
        }
    }

    #endregion

    #region Display Effects

    /// <summary>
    /// Show goal celebration popup.
    /// </summary>
    private void ShowGoalCelebration(string message, Color color)
    {
        if (goalCelebrationPopup == null || goalCelebrationText == null) return;

        goalCelebrationText.text = message;
        goalCelebrationText.color = color;
        goalCelebrationPopup.SetActive(true);

        StartCoroutine(HideAfterDelay(goalCelebrationPopup, goalCelebrationTime));
        StartCoroutine(PulseScale(goalCelebrationPopup.transform, goalCelebrationTime));
    }

    /// <summary>
    /// Show face-off indicator.
    /// </summary>
    private void ShowFaceoff(string message)
    {
        if (faceoffIndicator == null || faceoffText == null) return;

        faceoffText.text = message;
        faceoffIndicator.SetActive(true);

        StartCoroutine(HideAfterDelay(faceoffIndicator, faceoffIndicatorTime));
    }

    /// <summary>
    /// Flash player switch indicator.
    /// </summary>
    private IEnumerator FlashPlayerSwitchIndicator()
    {
        if (playerSwitchIndicator == null) yield break;

        Image bg = playerSwitchIndicator.GetComponent<Image>();
        if (bg == null) yield break;

        Color originalColor = bg.color;
        Color flashColor = new Color(1f, 1f, 0.3f, 0.9f);

        for (int i = 0; i < 3; i++)
        {
            bg.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            bg.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Hide UI element after delay.
    /// </summary>
    private IEnumerator HideAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    /// <summary>
    /// Pulse scale animation for celebration.
    /// </summary>
    private IEnumerator PulseScale(Transform target, float duration)
    {
        if (target == null) yield break;

        Vector3 originalScale = target.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 2f, 1f);
            float scale = Mathf.Lerp(0.9f, 1.1f, t);
            target.localScale = originalScale * scale;
            yield return null;
        }

        target.localScale = originalScale;
    }

    #endregion

    #region Public API (for external systems)

    /// <summary>
    /// Set shot power charge (0-1). Call from shooting system.
    /// </summary>
    public void SetShotPowerCharge(float power, bool isCharging)
    {
        currentShotPower = Mathf.Clamp01(power);
        isChargingShot = isCharging;
    }

    /// <summary>
    /// Update the period display externally.
    /// </summary>
    public void SetPeriod(int period)
    {
        UpdatePeriod(period);
    }

    /// <summary>
    /// Show custom message on face-off indicator.
    /// </summary>
    public void ShowFaceoffMessage(string message, float duration = 2f)
    {
        if (faceoffIndicator == null || faceoffText == null) return;

        faceoffText.text = message;
        faceoffIndicator.SetActive(true);
        StartCoroutine(HideAfterDelay(faceoffIndicator, duration));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create basic UI GameObject with RectTransform.
    /// </summary>
    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    /// <summary>
    /// Create TextMeshProUGUI element.
    /// </summary>
    private TextMeshProUGUI CreateTextElement(string name, Transform parent)
    {
        GameObject obj = CreateUIElement(name, parent);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false; // Performance optimization
        return text;
    }

    /// <summary>
    /// Get display name for player position.
    /// </summary>
    private string GetPositionDisplayName(PlayerPosition position)
    {
        switch (position)
        {
            case PlayerPosition.Goalie: return "GOALIE";
            case PlayerPosition.LeftDefense: return "LEFT DEFENSE";
            case PlayerPosition.RightDefense: return "RIGHT DEFENSE";
            case PlayerPosition.Center: return "CENTER";
            case PlayerPosition.LeftWing: return "LEFT WING";
            case PlayerPosition.RightWing: return "RIGHT WING";
            default: return "UNKNOWN";
        }
    }

    #endregion

    #region Debug

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        string debugInfo = $"[GameHUD Debug]\n";
        debugInfo += $"Canvas: {(hudCanvas != null ? "OK" : "NULL")}\n";
        debugInfo += $"GameManager: {(gameManager != null ? "OK" : "NULL")}\n";
        debugInfo += $"MatchManager: {(matchManager != null ? "OK" : "NULL")}\n";
        debugInfo += $"TeamManager: {(teamManager != null ? "OK" : "NULL")}\n";
        debugInfo += $"Current Player: {(currentPlayer != null ? currentPlayer.name : "NULL")}\n";
        debugInfo += $"Controlled Position: {(currentPlayer != null ? currentPlayer.Position.ToString() : "N/A")}\n";

        GUI.Box(new Rect(10, Screen.height - 160, 300, 150), debugInfo, style);
    }

    #endregion
}
