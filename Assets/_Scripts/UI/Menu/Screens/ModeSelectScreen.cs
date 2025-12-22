using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Game mode selection screen.
/// Allows choosing between Quick Match, Roguelite Run, Practice, etc.
/// </summary>
public class ModeSelectScreen : ScreenBase
{
    #region Properties

    public override string ScreenName => "Mode Select";
    protected override Color BackgroundColor => ArcadeTheme.PrimaryLight;

    #endregion

    #region UI References

    private Button rogueliteButton;
    private Button quickMatchButton;
    private Button practiceButton;
    private Button shootoutButton;

    #endregion

    #region Build UI

    protected override void BuildUI()
    {
        Transform root = GetRoot();

        // Title
        TextMeshProUGUI title = UIFactory.CreateTitle(root, "SELECT MODE");

        // Mode buttons grid (2x2)
        CreateModeButtons(root);
    }

    private void CreateModeButtons(Transform parent)
    {
        float buttonWidth = 450f;
        float buttonHeight = 140f;
        float spacing = 30f;
        float startY = 100f;

        // Roguelite Run (top left)
        rogueliteButton = CreateModeButton(parent, "ROGUELITE RUN",
            "Win matches, collect artifacts.\nSurvive 3 losses!",
            new Vector2(-buttonWidth / 2 - spacing / 2, startY),
            new Vector2(buttonWidth, buttonHeight),
            OnRogueliteClicked,
            true);

        // Quick Match (top right)
        quickMatchButton = CreateModeButton(parent, "QUICK MATCH",
            "Jump straight into action.\nNo progression.",
            new Vector2(buttonWidth / 2 + spacing / 2, startY),
            new Vector2(buttonWidth, buttonHeight),
            OnQuickMatchClicked,
            false);

        // Practice (bottom left)
        practiceButton = CreateModeButton(parent, "PRACTICE",
            "Train your skills.\nNo opponents.",
            new Vector2(-buttonWidth / 2 - spacing / 2, startY - buttonHeight - spacing),
            new Vector2(buttonWidth, buttonHeight),
            OnPracticeClicked,
            false);

        // Shootout (bottom right)
        shootoutButton = CreateModeButton(parent, "SHOOTOUT",
            "Penalty shots only.\n1v1 Goalie!",
            new Vector2(buttonWidth / 2 + spacing / 2, startY - buttonHeight - spacing),
            new Vector2(buttonWidth, buttonHeight),
            OnShootoutClicked,
            false);
    }

    private Button CreateModeButton(Transform parent, string title, string description,
        Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick, bool isPrimary)
    {
        // Container
        GameObject container = UIFactory.CreateElement(title + " Container", parent);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchoredPosition = position;
        containerRect.sizeDelta = size;

        // Background panel
        Image bg = container.AddComponent<Image>();
        bg.color = isPrimary ? ArcadeTheme.Accent : ArcadeTheme.ButtonSecondary;

        // Button component
        Button button = container.AddComponent<Button>();
        button.targetGraphic = bg;
        button.colors = isPrimary ? ArcadeTheme.GetPrimaryButtonColors() : ArcadeTheme.GetSecondaryButtonColors();
        button.onClick.AddListener(onClick);

        // Add chunky border
        CreateModeButtonBorder(container.transform, size, isPrimary);

        // Title text
        TextMeshProUGUI titleText = UIFactory.CreateText("Title", container.transform, title, ArcadeTheme.ButtonTextSize);
        titleText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 25);
        titleRect.sizeDelta = new Vector2(size.x - 40, 50);

        // Description text
        TextMeshProUGUI descText = UIFactory.CreateText("Description", container.transform, description, ArcadeTheme.CaptionSize);
        descText.fontStyle = FontStyles.Normal;
        descText.color = ArcadeTheme.WithAlpha(Color.white, 0.8f);
        RectTransform descRect = descText.GetComponent<RectTransform>();
        descRect.anchoredPosition = new Vector2(0, -25);
        descRect.sizeDelta = new Vector2(size.x - 40, 60);

        // Recommended badge for roguelite
        if (isPrimary)
        {
            CreateRecommendedBadge(container.transform, size);
        }

        return button;
    }

    private void CreateModeButtonBorder(Transform parent, Vector2 size, bool glowBorder)
    {
        // Shadow for depth
        GameObject shadow = UIFactory.CreateElement("Shadow", parent);
        RectTransform shadowRect = shadow.GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.sizeDelta = new Vector2(0, -6);
        shadowRect.anchoredPosition = new Vector2(0, -3);
        shadow.transform.SetAsFirstSibling();

        Image shadowImage = shadow.AddComponent<Image>();
        shadowImage.color = new Color(0, 0, 0, 0.5f);
        shadowImage.raycastTarget = false;

        if (glowBorder)
        {
            GameObject glow = UIFactory.CreateElement("Glow", parent);
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.sizeDelta = new Vector2(10, 10);
            glow.transform.SetAsFirstSibling();

            Image glowImage = glow.AddComponent<Image>();
            glowImage.color = ArcadeTheme.WithAlpha(ArcadeTheme.Ice, 0.4f);
            glowImage.raycastTarget = false;

            // Pulse the glow
            UIAnimator.PulseGlow(glowImage, 0.2f, 0.5f, 1.5f);
        }
    }

    private void CreateRecommendedBadge(Transform parent, Vector2 parentSize)
    {
        GameObject badge = UIFactory.CreateElement("Badge", parent);
        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1, 1);
        badgeRect.anchorMax = new Vector2(1, 1);
        badgeRect.pivot = new Vector2(1, 1);
        badgeRect.anchoredPosition = new Vector2(-10, -10);
        badgeRect.sizeDelta = new Vector2(140, 30);

        Image badgeBg = badge.AddComponent<Image>();
        badgeBg.color = ArcadeTheme.Success;

        TextMeshProUGUI badgeText = UIFactory.CreateText("Text", badge.transform, "RECOMMENDED", 16);
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.color = ArcadeTheme.PrimaryDark;
        RectTransform textRect = badgeText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
    }

    #endregion

    #region Button Handlers

    private void OnRogueliteClicked()
    {
        Debug.Log("[ModeSelectScreen] Roguelite Run selected");
        UIAnimator.PressButton(rogueliteButton.transform, () =>
        {
            GameManager.Instance?.StartNewRun();
            // Would load game scene here
        });
    }

    private void OnQuickMatchClicked()
    {
        Debug.Log("[ModeSelectScreen] Quick Match selected");
        UIAnimator.PressButton(quickMatchButton.transform, () =>
        {
            // Hide menu and start the match
            if (MenuManager.Instance != null)
            {
                MenuManager.Instance.Canvas.gameObject.SetActive(false);
            }

            // Start the game session
            if (GameSession.Instance != null)
            {
                GameSession.Instance.StartMatch();
            }
            else
            {
                Debug.LogWarning("[ModeSelectScreen] GameSession not found - creating one");
                GameObject sessionObj = new GameObject("GameSession");
                GameSession session = sessionObj.AddComponent<GameSession>();
                session.StartMatch();
            }
        });
    }

    private void OnPracticeClicked()
    {
        Debug.Log("[ModeSelectScreen] Practice selected");
        UIAnimator.PressButton(practiceButton.transform, () =>
        {
            // Hide menu and start practice mode (no opponents, just skating and shooting)
            if (MenuManager.Instance != null)
            {
                MenuManager.Instance.Canvas.gameObject.SetActive(false);
            }

            // In practice mode, we just let the player skate around
            // No GameSession needed - just disable the menu
            Debug.Log("[ModeSelectScreen] Practice mode started - skate freely!");
        });
    }

    private void OnShootoutClicked()
    {
        Debug.Log("[ModeSelectScreen] Shootout selected");
        UIAnimator.PressButton(shootoutButton.transform, () =>
        {
            // Load shootout mode
        });
    }

    #endregion
}
