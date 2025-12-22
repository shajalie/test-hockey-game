using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Game over / run complete screen.
/// Shows final stats and collected artifacts.
/// </summary>
public class GameOverScreen : ScreenBase
{
    #region Properties

    public override string ScreenName => "Game Over";
    protected override bool ShowBackButton => false;
    protected override Color BackgroundColor => new Color32(10, 10, 20, 255);

    #endregion

    #region State

    private bool wasVictory = false;

    #endregion

    #region UI References

    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private Image iconImage;
    private TextMeshProUGUI matchesText;
    private TextMeshProUGUI winsText;
    private TextMeshProUGUI lossesText;
    private TextMeshProUGUI artifactsText;
    private GameObject artifactGrid;
    private Button tryAgainButton;
    private Button mainMenuButton;

    #endregion

    #region Build UI

    protected override void BuildUI()
    {
        Transform root = GetRoot();

        // Title (RUN OVER or VICTORY)
        titleText = UIFactory.CreateTitle(root, "RUN OVER");
        titleText.fontSize = ArcadeTheme.TitleSizeLarge;

        // Subtitle
        subtitleText = UIFactory.CreateText("Subtitle", root, "Better luck next time!", ArcadeTheme.HeaderSizeSmall);
        RectTransform subRect = subtitleText.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0, -140);
        subRect.sizeDelta = new Vector2(800, 50);

        // Icon (broken puck for loss, trophy for win)
        GameObject iconObj = UIFactory.CreateElement("Icon", root);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0, -200);
        iconRect.sizeDelta = new Vector2(80, 80);

        iconImage = iconObj.AddComponent<Image>();
        iconImage.color = ArcadeTheme.Danger;

        // Stats panel
        CreateStatsPanel(root);

        // Artifacts collected
        CreateArtifactsSection(root);

        // Buttons
        tryAgainButton = UIFactory.CreatePrimaryButton(root, "TRY AGAIN",
            OnTryAgainClicked, new Vector2(0, -420));

        mainMenuButton = UIFactory.CreateSecondaryButton(root, "MAIN MENU",
            OnMainMenuClicked, new Vector2(0, -510));
    }

    private void CreateStatsPanel(Transform parent)
    {
        Image panel = UIFactory.CreatePanel(parent, "Stats Panel",
            new Vector2(0, 50), new Vector2(600, 200), true);

        Transform panelRoot = panel.transform;

        // Header
        TextMeshProUGUI header = UIFactory.CreateText("Header", panelRoot, "FINAL STATS", ArcadeTheme.BodySize);
        header.color = ArcadeTheme.TextAccent;
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchoredPosition = new Vector2(0, 70);
        headerRect.sizeDelta = new Vector2(500, 40);

        // Stats grid
        float yPos = 20f;
        float spacing = 40f;

        // Matches played
        matchesText = CreateStatRow(panelRoot, "Matches Played:", "0", new Vector2(0, yPos));
        yPos -= spacing;

        // Total wins
        winsText = CreateStatRow(panelRoot, "Total Wins:", "0", new Vector2(0, yPos));
        winsText.color = ArcadeTheme.Success;
        yPos -= spacing;

        // Total losses
        lossesText = CreateStatRow(panelRoot, "Total Losses:", "0", new Vector2(0, yPos));
        lossesText.color = ArcadeTheme.Danger;
        yPos -= spacing;

        // Artifacts collected
        artifactsText = CreateStatRow(panelRoot, "Artifacts:", "0", new Vector2(0, yPos));
        artifactsText.color = ArcadeTheme.Warning;
    }

    private TextMeshProUGUI CreateStatRow(Transform parent, string label, string value, Vector2 position)
    {
        GameObject row = UIFactory.CreateElement("Stat Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchoredPosition = position;
        rowRect.sizeDelta = new Vector2(500, 40);

        // Label
        TextMeshProUGUI labelText = UIFactory.CreateText("Label", row.transform, label, ArcadeTheme.BodySizeSmall);
        labelText.alignment = TextAlignmentOptions.Left;
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.6f, 1);
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = new Vector2(20, 0);

        // Value
        TextMeshProUGUI valueText = UIFactory.CreateText("Value", row.transform, value, ArcadeTheme.BodySizeLarge);
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.fontStyle = FontStyles.Bold;
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.6f, 0);
        valueRect.anchorMax = new Vector2(1, 1);
        valueRect.sizeDelta = Vector2.zero;
        valueRect.anchoredPosition = new Vector2(-20, 0);

        return valueText;
    }

    private void CreateArtifactsSection(Transform parent)
    {
        // Header
        TextMeshProUGUI header = UIFactory.CreateText("Artifacts Header", parent, "ARTIFACTS THIS RUN", ArcadeTheme.BodySizeSmall);
        header.color = ArcadeTheme.TextSecondary;
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchoredPosition = new Vector2(0, -130);
        headerRect.sizeDelta = new Vector2(600, 30);

        // Grid container
        artifactGrid = UIFactory.CreateElement("Artifact Grid", parent);
        RectTransform gridRect = artifactGrid.GetComponent<RectTransform>();
        gridRect.anchoredPosition = new Vector2(0, -200);
        gridRect.sizeDelta = new Vector2(500, 100);

        HorizontalLayoutGroup layout = artifactGrid.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Set whether this was a victory or defeat.
    /// </summary>
    public void SetVictory(bool victory)
    {
        wasVictory = victory;
    }

    #endregion

    #region Data Refresh

    protected override void OnBeforeShow()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        GameManager gm = GameManager.Instance;

        // Determine victory state (could check if player reached some goal)
        wasVictory = gm != null && gm.Wins >= 10; // Example: 10 wins = victory

        // Update title and styling based on outcome
        if (wasVictory)
        {
            titleText.text = "VICTORY!";
            titleText.color = ArcadeTheme.Success;
            subtitleText.text = "Congratulations, Champion!";
            subtitleText.color = ArcadeTheme.Success;
            iconImage.color = ArcadeTheme.Warning; // Gold trophy color
            tryAgainButton.GetComponentInChildren<TextMeshProUGUI>().text = "PLAY AGAIN";
        }
        else
        {
            titleText.text = "RUN OVER";
            titleText.color = ArcadeTheme.Danger;
            subtitleText.text = "Better luck next time!";
            subtitleText.color = ArcadeTheme.TextSecondary;
            iconImage.color = ArcadeTheme.Danger;
            tryAgainButton.GetComponentInChildren<TextMeshProUGUI>().text = "TRY AGAIN";
        }

        // Update stats
        if (gm != null)
        {
            matchesText.text = gm.CurrentMatch.ToString();
            winsText.text = gm.Wins.ToString();
            lossesText.text = gm.Losses.ToString();
            artifactsText.text = gm.CurrentArtifacts.Count.ToString();

            // Populate artifact grid
            RefreshArtifactGrid(gm.CurrentArtifacts);
        }

        // Animate title
        UIAnimator.PulseScale(titleText.transform, 0.05f, 1f);
    }

    private void RefreshArtifactGrid(IReadOnlyList<RunModifier> artifacts)
    {
        // Clear existing
        foreach (Transform child in artifactGrid.transform)
        {
            Destroy(child.gameObject);
        }

        // Add artifact icons
        int count = Mathf.Min(artifacts.Count, 8); // Max 8 displayed
        for (int i = 0; i < count; i++)
        {
            GameObject slot = UIFactory.CreateArtifactSlot(artifactGrid.transform, artifacts[i], i);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(60, 60);
        }

        // Add "..." if more
        if (artifacts.Count > 8)
        {
            TextMeshProUGUI moreText = UIFactory.CreateText("More", artifactGrid.transform, $"+{artifacts.Count - 8}", 24);
            moreText.color = ArcadeTheme.TextSecondary;
        }
    }

    #endregion

    #region Button Handlers

    private void OnTryAgainClicked()
    {
        Debug.Log("[GameOverScreen] Try Again clicked");
        UIAnimator.PressButton(tryAgainButton.transform, () =>
        {
            GameManager.Instance?.StartNewRun();
            menuManager?.ReturnToMainMenu();
        });
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[GameOverScreen] Main Menu clicked");
        UIAnimator.PressButton(mainMenuButton.transform, () =>
        {
            menuManager?.ReturnToMainMenu();
        });
    }

    #endregion
}
