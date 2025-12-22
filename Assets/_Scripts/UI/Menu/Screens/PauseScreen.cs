using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// In-game pause menu overlay.
/// Shows current match status, artifacts, and options.
/// </summary>
public class PauseScreen : ScreenBase
{
    #region Properties

    public override string ScreenName => "Pause";
    protected override bool ShowBackButton => false; // Has custom resume button
    protected override Color BackgroundColor => ArcadeTheme.WithAlpha(Color.black, 0.85f);

    #endregion

    #region UI References

    private TextMeshProUGUI titleText;
    private TextMeshProUGUI matchInfoText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI timeText;
    private GameObject artifactGrid;
    private Button resumeButton;
    private Button settingsButton;
    private Button quitButton;

    #endregion

    #region Build UI

    protected override void BuildUI()
    {
        Transform root = GetRoot();

        // Darkened overlay is already the background

        // Title
        titleText = UIFactory.CreateTitle(root, "PAUSED");
        titleText.color = ArcadeTheme.Ice;
        titleText.fontSize = ArcadeTheme.TitleSize;

        // Match info panel
        CreateMatchInfoPanel(root);

        // Artifacts section
        CreateArtifactsSection(root);

        // Buttons
        CreateMenuButtons(root);
    }

    private void CreateMatchInfoPanel(Transform parent)
    {
        Image panel = UIFactory.CreatePanel(parent, "Match Info Panel",
            new Vector2(0, 150), new Vector2(600, 180), true);

        Transform panelRoot = panel.transform;

        // Match number
        matchInfoText = UIFactory.CreateText("Match Info", panelRoot, "MATCH #1", ArcadeTheme.BodySize);
        matchInfoText.color = ArcadeTheme.TextAccent;
        RectTransform matchRect = matchInfoText.GetComponent<RectTransform>();
        matchRect.anchoredPosition = new Vector2(0, 55);
        matchRect.sizeDelta = new Vector2(500, 40);

        // Score
        scoreText = UIFactory.CreateText("Score", panelRoot, "0 - 0", ArcadeTheme.TitleSize);
        scoreText.fontStyle = FontStyles.Bold;
        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchoredPosition = new Vector2(0, 0);
        scoreRect.sizeDelta = new Vector2(400, 80);

        // Time remaining
        timeText = UIFactory.CreateText("Time", panelRoot, "TIME: 3:00", ArcadeTheme.BodySize);
        timeText.color = ArcadeTheme.Warning;
        RectTransform timeRect = timeText.GetComponent<RectTransform>();
        timeRect.anchoredPosition = new Vector2(0, -55);
        timeRect.sizeDelta = new Vector2(300, 40);
    }

    private void CreateArtifactsSection(Transform parent)
    {
        // Header
        TextMeshProUGUI header = UIFactory.CreateText("Artifacts Header", parent, "YOUR ARTIFACTS", ArcadeTheme.BodySizeSmall);
        header.color = ArcadeTheme.TextSecondary;
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchoredPosition = new Vector2(0, -10);
        headerRect.sizeDelta = new Vector2(400, 30);

        // Grid
        artifactGrid = UIFactory.CreateElement("Artifact Grid", parent);
        RectTransform gridRect = artifactGrid.GetComponent<RectTransform>();
        gridRect.anchoredPosition = new Vector2(0, -80);
        gridRect.sizeDelta = new Vector2(400, 80);

        HorizontalLayoutGroup layout = artifactGrid.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
    }

    private void CreateMenuButtons(Transform parent)
    {
        // Resume (primary)
        resumeButton = UIFactory.CreatePrimaryButton(parent, "RESUME",
            OnResumeClicked, new Vector2(0, -200));

        // Settings
        settingsButton = UIFactory.CreateSecondaryButton(parent, "SETTINGS",
            OnSettingsClicked, new Vector2(0, -300));

        // Quit Run (danger)
        quitButton = UIFactory.CreateSecondaryButton(parent, "QUIT RUN",
            OnQuitClicked, new Vector2(0, -390));

        // Style quit button as danger
        Image quitBg = quitButton.GetComponent<Image>();
        quitBg.color = ArcadeTheme.Danger;
        quitButton.colors = ArcadeTheme.GetDangerButtonColors();
    }

    #endregion

    #region Lifecycle

    protected override void OnBeforeShow()
    {
        // Pause the game
        Time.timeScale = 0f;
        RefreshMatchInfo();
        RefreshArtifacts();
    }

    protected override void OnAfterHide()
    {
        // Resume the game (unless quitting)
        Time.timeScale = 1f;
    }

    private void RefreshMatchInfo()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        matchInfoText.text = $"MATCH #{gm.CurrentMatch}";
        scoreText.text = $"{gm.PlayerScore} - {gm.OpponentScore}";

        // Color score based on who's winning
        if (gm.PlayerScore > gm.OpponentScore)
        {
            scoreText.color = ArcadeTheme.Success;
        }
        else if (gm.PlayerScore < gm.OpponentScore)
        {
            scoreText.color = ArcadeTheme.Danger;
        }
        else
        {
            scoreText.color = ArcadeTheme.TextPrimary;
        }

        // Time would come from MatchManager
        timeText.text = "TIME: 3:00"; // Placeholder
    }

    private void RefreshArtifacts()
    {
        // Clear existing
        foreach (Transform child in artifactGrid.transform)
        {
            Destroy(child.gameObject);
        }

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        IReadOnlyList<RunModifier> artifacts = gm.CurrentArtifacts;
        int count = Mathf.Min(artifacts.Count, 6);

        for (int i = 0; i < count; i++)
        {
            GameObject slot = UIFactory.CreateArtifactSlot(artifactGrid.transform, artifacts[i], i);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(60, 60);
        }

        if (artifacts.Count == 0)
        {
            TextMeshProUGUI noArtifacts = UIFactory.CreateText("None", artifactGrid.transform, "None yet", 20);
            noArtifacts.color = ArcadeTheme.TextSecondary;
        }
    }

    #endregion

    #region Button Handlers

    private void OnResumeClicked()
    {
        Debug.Log("[PauseScreen] Resume clicked");
        UIAnimator.PressButton(resumeButton.transform, () =>
        {
            Hide();
        });
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[PauseScreen] Settings clicked");
        UIAnimator.PressButton(settingsButton.transform, () =>
        {
            NavigateTo<SettingsScreen>();
        });
    }

    private void OnQuitClicked()
    {
        Debug.Log("[PauseScreen] Quit clicked");
        UIAnimator.PressButton(quitButton.transform, () =>
        {
            // End the run
            GameManager.Instance?.EndRun(false);

            // Ensure time is resumed
            Time.timeScale = 1f;

            // Return to main menu
            menuManager?.ReturnToMainMenu();
        });
    }

    #endregion

    #region Input

    public override void HandleInput()
    {
        // Escape to resume
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnResumeClicked();
        }
    }

    #endregion
}
