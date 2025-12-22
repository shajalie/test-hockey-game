using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Arcade-styled artifact selection screen.
/// Shows 3 artifacts to choose from after winning a match.
/// </summary>
public class DraftScreen : ScreenBase
{
    #region Properties

    public override string ScreenName => "Draft";
    protected override bool ShowBackButton => false; // Must choose or skip
    protected override Color BackgroundColor => new Color32(15, 25, 50, 255);

    #endregion

    #region UI References

    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private List<ArtifactCard> artifactCards = new List<ArtifactCard>();
    private Button skipButton;
    private List<RunModifier> currentChoices = new List<RunModifier>();

    #endregion

    #region Constants

    private const int ARTIFACT_COUNT = 3;

    #endregion

    #region Build UI

    protected override void BuildUI()
    {
        Transform root = GetRoot();

        // Title
        titleText = UIFactory.CreateTitle(root, "CHOOSE YOUR ARTIFACT");
        titleText.color = ArcadeTheme.Warning;

        // Subtitle (victory message)
        subtitleText = UIFactory.CreateText("Subtitle", root, "MATCH VICTORY!", ArcadeTheme.HeaderSizeSmall);
        subtitleText.color = ArcadeTheme.Success;
        RectTransform subRect = subtitleText.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0, -130);
        subRect.sizeDelta = new Vector2(800, 50);

        // Artifact cards container
        CreateArtifactCards(root);

        // Skip button
        skipButton = UIFactory.CreateSecondaryButton(root, "SKIP (NO ARTIFACT)",
            OnSkipClicked, new Vector2(0, -450));
    }

    private void CreateArtifactCards(Transform parent)
    {
        float cardWidth = 280f;
        float cardHeight = 380f;
        float spacing = 40f;
        float startX = -((cardWidth + spacing) * (ARTIFACT_COUNT - 1)) / 2f;

        for (int i = 0; i < ARTIFACT_COUNT; i++)
        {
            float xPos = startX + i * (cardWidth + spacing);
            ArtifactCard card = CreateArtifactCard(parent, i, new Vector2(xPos, -50), new Vector2(cardWidth, cardHeight));
            artifactCards.Add(card);
        }
    }

    private ArtifactCard CreateArtifactCard(Transform parent, int index, Vector2 position, Vector2 size)
    {
        // Card container
        GameObject cardObj = UIFactory.CreateElement($"Artifact Card {index}", parent);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchoredPosition = position;
        cardRect.sizeDelta = size;

        // Background
        Image bg = cardObj.AddComponent<Image>();
        bg.color = ArcadeTheme.PanelSolid;

        // Button component
        Button button = cardObj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.colors = ArcadeTheme.GetSecondaryButtonColors();

        int cardIndex = index;
        button.onClick.AddListener(() => OnArtifactSelected(cardIndex));

        // Rarity border (will be colored based on artifact)
        GameObject border = UIFactory.CreateElement("Border", cardObj.transform);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = new Vector2(8, 8);
        border.transform.SetAsFirstSibling();

        Image borderImage = border.AddComponent<Image>();
        borderImage.color = ArcadeTheme.RarityCommon;
        borderImage.raycastTarget = false;

        // Icon area
        GameObject iconObj = UIFactory.CreateElement("Icon", cardObj.transform);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchoredPosition = new Vector2(0, 100);
        iconRect.sizeDelta = new Vector2(120, 120);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = ArcadeTheme.TextSecondary;
        iconImage.raycastTarget = false;

        // Name text
        TextMeshProUGUI nameText = UIFactory.CreateText("Name", cardObj.transform, "ARTIFACT", ArcadeTheme.BodySizeLarge);
        nameText.fontStyle = FontStyles.Bold;
        RectTransform nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchoredPosition = new Vector2(0, 10);
        nameRect.sizeDelta = new Vector2(size.x - 30, 50);

        // Description text
        TextMeshProUGUI descText = UIFactory.CreateText("Description", cardObj.transform, "Description here", ArcadeTheme.CaptionSize);
        descText.fontStyle = FontStyles.Normal;
        descText.color = ArcadeTheme.TextSecondary;
        RectTransform descRect = descText.GetComponent<RectTransform>();
        descRect.anchoredPosition = new Vector2(0, -50);
        descRect.sizeDelta = new Vector2(size.x - 30, 80);

        // Stats text
        TextMeshProUGUI statsText = UIFactory.CreateText("Stats", cardObj.transform, "", ArcadeTheme.LabelSize);
        statsText.color = ArcadeTheme.Success;
        RectTransform statsRect = statsText.GetComponent<RectTransform>();
        statsRect.anchoredPosition = new Vector2(0, -120);
        statsRect.sizeDelta = new Vector2(size.x - 30, 60);

        // Rarity badge
        GameObject badge = UIFactory.CreateElement("Rarity Badge", cardObj.transform);
        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        badgeRect.anchoredPosition = new Vector2(0, -165);
        badgeRect.sizeDelta = new Vector2(120, 30);

        Image badgeBg = badge.AddComponent<Image>();
        badgeBg.color = ArcadeTheme.RarityCommon;
        badgeBg.raycastTarget = false;

        TextMeshProUGUI rarityText = UIFactory.CreateText("Rarity Text", badge.transform, "COMMON", 18);
        rarityText.fontStyle = FontStyles.Bold;
        rarityText.color = Color.white;
        RectTransform rarityTextRect = rarityText.GetComponent<RectTransform>();
        rarityTextRect.anchorMin = Vector2.zero;
        rarityTextRect.anchorMax = Vector2.one;
        rarityTextRect.sizeDelta = Vector2.zero;

        return new ArtifactCard
        {
            Root = cardObj,
            Button = button,
            Border = borderImage,
            Icon = iconImage,
            NameText = nameText,
            DescriptionText = descText,
            StatsText = statsText,
            RarityBadge = badgeBg,
            RarityText = rarityText
        };
    }

    #endregion

    #region Data Refresh

    protected override void OnBeforeShow()
    {
        RefreshArtifactChoices();
    }

    private void RefreshArtifactChoices()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[DraftScreen] No GameManager found");
            return;
        }

        // Get draft choices
        currentChoices = gm.GetDraftChoices();

        // Update cards
        for (int i = 0; i < ARTIFACT_COUNT; i++)
        {
            if (i < currentChoices.Count && currentChoices[i] != null)
            {
                UpdateArtifactCard(artifactCards[i], currentChoices[i]);
                artifactCards[i].Root.SetActive(true);
            }
            else
            {
                artifactCards[i].Root.SetActive(false);
            }
        }

        // Update subtitle with match number
        subtitleText.text = $"MATCH #{gm.CurrentMatch} VICTORY!";
    }

    private void UpdateArtifactCard(ArtifactCard card, RunModifier artifact)
    {
        // Name
        card.NameText.text = artifact.artifactName.ToUpper();

        // Description
        card.DescriptionText.text = artifact.description;

        // Stats
        string stats = GetArtifactStats(artifact);
        card.StatsText.text = stats;

        // Rarity styling
        Color rarityColor = ArcadeTheme.GetRarityColor(artifact.rarity);
        card.Border.color = rarityColor;
        card.RarityBadge.color = rarityColor;
        card.RarityText.text = artifact.rarity.ToString().ToUpper();

        // Icon (use sprite if available, otherwise show letter)
        if (artifact.icon != null)
        {
            card.Icon.sprite = artifact.icon;
            card.Icon.color = Color.white;
        }
        else
        {
            card.Icon.sprite = null;
            card.Icon.color = rarityColor;
        }

        // Glow animation for rare+ artifacts
        if (artifact.rarity >= ArtifactRarity.Rare)
        {
            UIAnimator.PulseGlow(card.Border, 0.4f, 1f, 1.5f);
        }
    }

    private string GetArtifactStats(RunModifier artifact)
    {
        List<string> stats = new List<string>();

        if (artifact.skatingSpeedMultiplier != 0)
            stats.Add($"Speed +{artifact.skatingSpeedMultiplier * 100:F0}%");
        if (artifact.accelerationMultiplier != 0)
            stats.Add($"Accel +{artifact.accelerationMultiplier * 100:F0}%");
        if (artifact.shotPowerMultiplier != 0)
            stats.Add($"Shot +{artifact.shotPowerMultiplier * 100:F0}%");
        if (artifact.checkForceMultiplier != 0)
            stats.Add($"Check +{artifact.checkForceMultiplier * 100:F0}%");
        if (artifact.puckControlMultiplier != 0)
            stats.Add($"Control +{artifact.puckControlMultiplier * 100:F0}%");

        if (artifact.explosivePucks) stats.Add("Explosive Pucks!");
        if (artifact.magneticStick) stats.Add("Magnetic Stick!");
        if (artifact.heavyHitter) stats.Add("Heavy Hitter!");

        return string.Join("\n", stats);
    }

    #endregion

    #region Button Handlers

    private void OnArtifactSelected(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;

        RunModifier selected = currentChoices[index];
        Debug.Log($"[DraftScreen] Selected artifact: {selected.artifactName}");

        // Animate selection
        UIAnimator.PressButton(artifactCards[index].Button.transform, () =>
        {
            // Apply artifact
            GameManager.Instance?.SelectArtifact(selected);

            // Return to game or hub
            menuManager?.ReturnToMainMenu();
        });

        // Flash the selected card
        UIAnimator.FlashColor(artifactCards[index].Border, ArcadeTheme.Success, 0.3f);
    }

    private void OnSkipClicked()
    {
        Debug.Log("[DraftScreen] Skipped artifact selection");
        UIAnimator.PressButton(skipButton.transform, () =>
        {
            GameManager.Instance?.SkipDraft();
            menuManager?.ReturnToMainMenu();
        });
    }

    #endregion

    #region Helper Classes

    private class ArtifactCard
    {
        public GameObject Root;
        public Button Button;
        public Image Border;
        public Image Icon;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI DescriptionText;
        public TextMeshProUGUI StatsText;
        public Image RarityBadge;
        public TextMeshProUGUI RarityText;
    }

    #endregion
}
