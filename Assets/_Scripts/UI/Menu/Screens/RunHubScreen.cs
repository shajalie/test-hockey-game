using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main menu screen with roguelite run progress hub.
/// Shows current run status, collected artifacts, and navigation options.
/// </summary>
public class RunHubScreen : ScreenBase
{
    #region Properties

    public override string ScreenName => "Run Hub";
    protected override bool ShowBackButton => false; // Main menu has no back
    protected override Color BackgroundColor => ArcadeTheme.Primary;

    #endregion

    #region UI References

    private TextMeshProUGUI titleText;
    private GameObject runStatusPanel;
    private TextMeshProUGUI winsText;
    private TextMeshProUGUI lossesText;
    private TextMeshProUGUI matchText;
    private TextMeshProUGUI streakText;
    private List<Image> heartIcons = new List<Image>();
    private GameObject artifactGrid;
    private List<GameObject> artifactSlots = new List<GameObject>();
    private Button continueRunButton;
    private Button newRunButton;
    private Button quickMatchButton;
    private Button practiceButton;
    private Button settingsButton;
    private Image puckIcon;

    #endregion

    #region Constants

    private const int MAX_HEARTS = 3;
    private const int ARTIFACT_COLUMNS = 4;
    private const int MAX_DISPLAYED_ARTIFACTS = 8;

    #endregion

    #region Build UI

    protected override void BuildUI()
    {
        Transform root = GetRoot();

        // Title with animated puck
        CreateTitleSection(root);

        // Run status panel (wins, losses, hearts)
        CreateRunStatusPanel(root);

        // Artifact collection grid
        CreateArtifactSection(root);

        // Main action buttons
        CreateActionButtons(root);

        // Quick access buttons row
        CreateQuickAccessRow(root);
    }

    private void CreateTitleSection(Transform parent)
    {
        // Title text
        titleText = UIFactory.CreateTitle(parent, "HOCKEY LEGENDS");
        titleText.color = ArcadeTheme.TextPrimary;

        // Subtitle
        TextMeshProUGUI subtitle = UIFactory.CreateText("Subtitle", parent, "ROGUELITE HOCKEY", ArcadeTheme.HeaderSizeSmall);
        subtitle.color = ArcadeTheme.Ice;
        RectTransform subRect = subtitle.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0, -130);
        subRect.sizeDelta = new Vector2(800, 50);

        // Animated puck icon (placeholder - would use sprite in production)
        GameObject puckObj = UIFactory.CreateElement("Puck Icon", parent);
        RectTransform puckRect = puckObj.GetComponent<RectTransform>();
        puckRect.anchorMin = new Vector2(0.5f, 1f);
        puckRect.anchorMax = new Vector2(0.5f, 1f);
        puckRect.pivot = new Vector2(0.5f, 1f);
        puckRect.anchoredPosition = new Vector2(0, -190);
        puckRect.sizeDelta = new Vector2(60, 60);

        puckIcon = puckObj.AddComponent<Image>();
        puckIcon.color = ArcadeTheme.TextPrimary;

        // Start floating animation
        UIAnimator.FloatUpDown(puckObj.transform, 5f, 0.8f);
        UIAnimator.RotateContinuous(puckObj.transform, 20f);
    }

    private void CreateRunStatusPanel(Transform parent)
    {
        // Panel background
        Image panelBg = UIFactory.CreatePanel(parent, "Run Status Panel",
            new Vector2(0, 150), new Vector2(800, 200), true);
        runStatusPanel = panelBg.gameObject;

        Transform panelRoot = runStatusPanel.transform;

        // Panel header
        TextMeshProUGUI header = UIFactory.CreateText("Header", panelRoot, "CURRENT RUN", ArcadeTheme.HeaderSizeSmall);
        header.color = ArcadeTheme.TextAccent;
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchoredPosition = new Vector2(0, 70);
        headerRect.sizeDelta = new Vector2(700, 40);

        // Stats row (Wins | Losses | Hearts)
        GameObject statsRow = UIFactory.CreateElement("Stats Row", panelRoot);
        RectTransform statsRect = statsRow.GetComponent<RectTransform>();
        statsRect.anchoredPosition = new Vector2(0, 10);
        statsRect.sizeDelta = new Vector2(700, 60);

        // Wins
        winsText = UIFactory.CreateText("Wins", statsRow.transform, "W: 0", ArcadeTheme.BodySizeLarge);
        winsText.color = ArcadeTheme.Success;
        RectTransform winsRect = winsText.GetComponent<RectTransform>();
        winsRect.anchoredPosition = new Vector2(-200, 0);
        winsRect.sizeDelta = new Vector2(150, 60);

        // Losses
        lossesText = UIFactory.CreateText("Losses", statsRow.transform, "L: 0", ArcadeTheme.BodySizeLarge);
        lossesText.color = ArcadeTheme.Danger;
        RectTransform lossesRect = lossesText.GetComponent<RectTransform>();
        lossesRect.anchoredPosition = new Vector2(-50, 0);
        lossesRect.sizeDelta = new Vector2(150, 60);

        // Hearts container
        GameObject heartsContainer = UIFactory.CreateElement("Hearts", statsRow.transform);
        RectTransform heartsRect = heartsContainer.GetComponent<RectTransform>();
        heartsRect.anchoredPosition = new Vector2(150, 0);
        heartsRect.sizeDelta = new Vector2(200, 60);

        HorizontalLayoutGroup heartsLayout = heartsContainer.AddComponent<HorizontalLayoutGroup>();
        heartsLayout.spacing = 10f;
        heartsLayout.childAlignment = TextAnchor.MiddleCenter;
        heartsLayout.childControlWidth = false;
        heartsLayout.childControlHeight = false;

        // Create heart icons
        for (int i = 0; i < MAX_HEARTS; i++)
        {
            Image heart = UIFactory.CreateHeartIcon(heartsContainer.transform, true, i);
            heartIcons.Add(heart);
        }

        // Match and streak row
        GameObject infoRow = UIFactory.CreateElement("Info Row", panelRoot);
        RectTransform infoRect = infoRow.GetComponent<RectTransform>();
        infoRect.anchoredPosition = new Vector2(0, -50);
        infoRect.sizeDelta = new Vector2(700, 40);

        matchText = UIFactory.CreateText("Match", infoRow.transform, "MATCH #1", ArcadeTheme.BodySize);
        RectTransform matchRect = matchText.GetComponent<RectTransform>();
        matchRect.anchoredPosition = new Vector2(-150, 0);
        matchRect.sizeDelta = new Vector2(250, 40);

        streakText = UIFactory.CreateText("Streak", infoRow.transform, "NO STREAK", ArcadeTheme.BodySize);
        streakText.color = ArcadeTheme.TextSecondary;
        RectTransform streakRect = streakText.GetComponent<RectTransform>();
        streakRect.anchoredPosition = new Vector2(150, 0);
        streakRect.sizeDelta = new Vector2(250, 40);
    }

    private void CreateArtifactSection(Transform parent)
    {
        // Artifact panel
        Image panelBg = UIFactory.CreatePanel(parent, "Artifact Panel",
            new Vector2(0, -100), new Vector2(500, 220), true);

        Transform panelRoot = panelBg.gameObject.transform;

        // Header
        TextMeshProUGUI header = UIFactory.CreateText("Header", panelRoot, "ARTIFACTS", ArcadeTheme.BodySize);
        header.color = ArcadeTheme.TextAccent;
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchoredPosition = new Vector2(0, 80);
        headerRect.sizeDelta = new Vector2(400, 40);

        // Grid container
        artifactGrid = UIFactory.CreateElement("Artifact Grid", panelRoot);
        RectTransform gridRect = artifactGrid.GetComponent<RectTransform>();
        gridRect.anchoredPosition = new Vector2(0, -20);
        gridRect.sizeDelta = new Vector2(400, 150);

        GridLayoutGroup grid = artifactGrid.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = ARTIFACT_COLUMNS;
        grid.cellSize = new Vector2(80, 80);
        grid.spacing = new Vector2(15, 15);
        grid.childAlignment = TextAnchor.MiddleCenter;

        // Create empty slots initially
        for (int i = 0; i < MAX_DISPLAYED_ARTIFACTS; i++)
        {
            GameObject slot = UIFactory.CreateArtifactSlot(artifactGrid.transform, null, i);
            artifactSlots.Add(slot);
        }
    }

    private void CreateActionButtons(Transform parent)
    {
        // Continue Run button (primary - only shown when run is active)
        continueRunButton = UIFactory.CreatePrimaryButton(parent, "CONTINUE RUN",
            OnContinueRunClicked, new Vector2(0, -320));

        // New Run button (secondary)
        newRunButton = UIFactory.CreateSecondaryButton(parent, "NEW RUN",
            OnNewRunClicked, new Vector2(0, -420));
    }

    private void CreateQuickAccessRow(Transform parent)
    {
        // Bottom row of smaller buttons
        GameObject buttonRow = UIFactory.CreateElement("Quick Access Row", parent);
        RectTransform rowRect = buttonRow.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0, 50);
        rowRect.sizeDelta = new Vector2(700, 80);

        HorizontalLayoutGroup layout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Quick Match
        quickMatchButton = UIFactory.CreateSmallButton(parent, "QUICK MATCH",
            OnQuickMatchClicked, new Vector2(-230, -510));

        // Practice
        practiceButton = UIFactory.CreateSmallButton(parent, "PRACTICE",
            OnPracticeClicked, new Vector2(0, -510));

        // Settings
        settingsButton = UIFactory.CreateSmallButton(parent, "SETTINGS",
            OnSettingsClicked, new Vector2(230, -510));
    }

    #endregion

    #region Data Refresh

    protected override void OnBeforeShow()
    {
        RefreshRunStatus();
        RefreshArtifacts();
        UpdateButtonStates();
    }

    private void RefreshRunStatus()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null || !gm.IsRunActive)
        {
            // No active run - show placeholder
            runStatusPanel.SetActive(false);
            return;
        }

        runStatusPanel.SetActive(true);

        // Update stats
        winsText.text = $"W: {gm.Wins}";
        lossesText.text = $"L: {gm.Losses}";
        matchText.text = $"MATCH #{gm.CurrentMatch + 1}";

        // Update hearts (3 lives - losses remaining)
        int livesRemaining = MAX_HEARTS - gm.Losses;
        for (int i = 0; i < MAX_HEARTS; i++)
        {
            bool isFilled = i < livesRemaining;
            heartIcons[i].color = isFilled ? ArcadeTheme.Danger : ArcadeTheme.WithAlpha(ArcadeTheme.TextSecondary, 0.3f);

            // Pulse animation on last heart
            if (isFilled && i == livesRemaining - 1 && livesRemaining == 1)
            {
                UIAnimator.PulseScale(heartIcons[i].transform, 0.15f, 0.5f);
            }
        }

        // Streak text
        if (gm.Wins >= 3)
        {
            streakText.text = $"{gm.Wins} WIN STREAK!";
            streakText.color = ArcadeTheme.Success;
        }
        else if (gm.Losses >= 2)
        {
            streakText.text = "DANGER ZONE!";
            streakText.color = ArcadeTheme.Danger;
        }
        else
        {
            streakText.text = "";
        }
    }

    private void RefreshArtifacts()
    {
        GameManager gm = GameManager.Instance;

        // Clear old slots
        foreach (var slot in artifactSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        artifactSlots.Clear();

        // Get artifacts from game manager
        IReadOnlyList<RunModifier> artifacts = gm?.CurrentArtifacts;
        int artifactCount = artifacts?.Count ?? 0;

        // Create slots
        for (int i = 0; i < MAX_DISPLAYED_ARTIFACTS; i++)
        {
            RunModifier artifact = (i < artifactCount) ? artifacts[i] : null;
            GameObject slot = UIFactory.CreateArtifactSlot(artifactGrid.transform, artifact, i);
            artifactSlots.Add(slot);
        }
    }

    private void UpdateButtonStates()
    {
        GameManager gm = GameManager.Instance;
        bool hasActiveRun = gm != null && gm.IsRunActive;

        // Show/hide continue button based on active run
        continueRunButton.gameObject.SetActive(hasActiveRun);

        // Adjust new run button position
        RectTransform newRunRect = newRunButton.GetComponent<RectTransform>();
        if (hasActiveRun)
        {
            newRunRect.anchoredPosition = new Vector2(0, -420);
        }
        else
        {
            newRunRect.anchoredPosition = new Vector2(0, -320);
        }
    }

    #endregion

    #region Button Handlers

    private void OnContinueRunClicked()
    {
        Debug.Log("[RunHubScreen] Continue Run clicked");
        UIAnimator.PressButton(continueRunButton.transform, () =>
        {
            // Continue the current run - start next match
            GameManager.Instance?.StartMatch();
            LoadGameScene();
        });
    }

    private void OnNewRunClicked()
    {
        Debug.Log("[RunHubScreen] New Run clicked");
        UIAnimator.PressButton(newRunButton.transform, () =>
        {
            // Start a new roguelite run
            GameManager.Instance?.StartNewRun();
            LoadGameScene();
        });
    }

    private void OnQuickMatchClicked()
    {
        Debug.Log("[RunHubScreen] Quick Match clicked");
        UIAnimator.PressButton(quickMatchButton.transform, () =>
        {
            NavigateTo<ModeSelectScreen>();
        });
    }

    private void OnPracticeClicked()
    {
        Debug.Log("[RunHubScreen] Practice clicked");
        UIAnimator.PressButton(practiceButton.transform, () =>
        {
            // Direct to practice mode
            LoadGameScene(isPractice: true);
        });
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[RunHubScreen] Settings clicked");
        UIAnimator.PressButton(settingsButton.transform, () =>
        {
            NavigateTo<SettingsScreen>();
        });
    }

    private void LoadGameScene(bool isPractice = false)
    {
        // In a full implementation, this would trigger scene loading
        // For now, just log
        Debug.Log($"[RunHubScreen] Loading game scene (practice: {isPractice})");

        // Could use: UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    #endregion

    #region Input

    public override void HandleInput()
    {
        // No back button on main menu - could show quit confirmation instead
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Show quit confirmation
            Debug.Log("[RunHubScreen] Escape pressed - could show quit dialog");
        }
    }

    #endregion
}
