using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles the artifact draft UI where players choose 1 of 3 random artifacts.
/// This is the "Tape to Tape" style roguelite progression.
/// </summary>
public class DraftSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject draftPanel;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject artifactChoicePrefab;
    [SerializeField] private Button skipButton;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Settings")]
    [SerializeField] private float showDelay = 0.5f;

    private List<RunModifier> currentChoices = new List<RunModifier>();
    private List<GameObject> choiceObjects = new List<GameObject>();

    private void OnEnable()
    {
        GameEvents.OnDraftStarted += ShowDraft;
    }

    private void OnDisable()
    {
        GameEvents.OnDraftStarted -= ShowDraft;
    }

    private void Start()
    {
        // Hide draft panel initially
        if (draftPanel != null)
        {
            draftPanel.SetActive(false);
        }

        // Setup skip button
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipClicked);
        }
    }

    /// <summary>
    /// Show the draft selection UI.
    /// </summary>
    public void ShowDraft()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[DraftSystem] GameManager not found!");
            return;
        }

        // Get random choices
        currentChoices = GameManager.Instance.GetDraftChoices();

        if (currentChoices.Count == 0)
        {
            Debug.Log("[DraftSystem] No artifacts available for draft, skipping...");
            GameManager.Instance.SkipDraft();
            return;
        }

        // Clear previous choices
        ClearChoices();

        // Create choice UI elements
        foreach (var artifact in currentChoices)
        {
            CreateChoiceUI(artifact);
        }

        // Update title
        if (titleText != null)
        {
            int matchNum = GameManager.Instance.CurrentMatch;
            titleText.text = matchNum == 0 ? "Choose Your Starting Artifact" : "Choose an Artifact";
        }

        // Show panel
        if (draftPanel != null)
        {
            draftPanel.SetActive(true);
        }

        Debug.Log($"[DraftSystem] Showing {currentChoices.Count} artifact choices");
    }

    /// <summary>
    /// Hide the draft UI.
    /// </summary>
    public void HideDraft()
    {
        if (draftPanel != null)
        {
            draftPanel.SetActive(false);
        }

        ClearChoices();
    }

    private void CreateChoiceUI(RunModifier artifact)
    {
        if (choicesContainer == null || artifactChoicePrefab == null)
        {
            Debug.LogWarning("[DraftSystem] Missing UI references, using fallback");
            return;
        }

        GameObject choiceObj = Instantiate(artifactChoicePrefab, choicesContainer);
        choiceObjects.Add(choiceObj);

        // Setup the choice card
        var choiceCard = choiceObj.GetComponent<ArtifactChoiceCard>();
        if (choiceCard != null)
        {
            choiceCard.Setup(artifact, OnArtifactSelected);
        }
        else
        {
            // Fallback: Try to find basic UI components
            SetupBasicChoiceUI(choiceObj, artifact);
        }
    }

    private void SetupBasicChoiceUI(GameObject choiceObj, RunModifier artifact)
    {
        // Find and set name
        var nameText = choiceObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = artifact.artifactName;
        }

        // Find and set icon
        var iconImage = choiceObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && artifact.icon != null)
        {
            iconImage.sprite = artifact.icon;
        }

        // Setup button
        var button = choiceObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnArtifactSelected(artifact));
        }
    }

    private void ClearChoices()
    {
        foreach (var obj in choiceObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        choiceObjects.Clear();
        currentChoices.Clear();
    }

    private void OnArtifactSelected(RunModifier artifact)
    {
        Debug.Log($"[DraftSystem] Selected: {artifact.artifactName}");

        HideDraft();
        GameManager.Instance?.SelectArtifact(artifact);
    }

    private void OnSkipClicked()
    {
        Debug.Log("[DraftSystem] Skipped draft");

        HideDraft();
        GameManager.Instance?.SkipDraft();
    }
}

/// <summary>
/// Individual artifact choice card in the draft UI.
/// </summary>
public class ArtifactChoiceCard : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Image rarityBackground;
    [SerializeField] private Button selectButton;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color uncommonColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color rareColor = new Color(0.3f, 0.5f, 1f);
    [SerializeField] private Color legendaryColor = new Color(1f, 0.8f, 0.2f);

    private RunModifier artifact;
    private System.Action<RunModifier> onSelected;

    public void Setup(RunModifier artifact, System.Action<RunModifier> onSelectedCallback)
    {
        this.artifact = artifact;
        this.onSelected = onSelectedCallback;

        // Set UI elements
        if (nameText != null)
        {
            nameText.text = artifact.artifactName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = artifact.description;
        }

        if (iconImage != null && artifact.icon != null)
        {
            iconImage.sprite = artifact.icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        // Set rarity
        if (rarityText != null)
        {
            rarityText.text = artifact.rarity.ToString();
        }

        Color rarityColor = GetRarityColor(artifact.rarity);
        if (rarityBackground != null)
        {
            rarityBackground.color = rarityColor;
        }
        if (rarityText != null)
        {
            rarityText.color = rarityColor;
        }

        // Setup button
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectClicked);
        }

        // Build stat modifier text
        string modifiers = BuildModifierText(artifact);
        if (!string.IsNullOrEmpty(modifiers) && descriptionText != null)
        {
            descriptionText.text += "\n\n" + modifiers;
        }
    }

    private void OnSelectClicked()
    {
        onSelected?.Invoke(artifact);
    }

    private Color GetRarityColor(ArtifactRarity rarity)
    {
        return rarity switch
        {
            ArtifactRarity.Common => commonColor,
            ArtifactRarity.Uncommon => uncommonColor,
            ArtifactRarity.Rare => rareColor,
            ArtifactRarity.Legendary => legendaryColor,
            _ => commonColor
        };
    }

    private string BuildModifierText(RunModifier artifact)
    {
        List<string> mods = new List<string>();

        if (artifact.skatingSpeedMultiplier != 0)
            mods.Add($"Speed: {artifact.skatingSpeedMultiplier:+0%;-0%}");
        if (artifact.accelerationMultiplier != 0)
            mods.Add($"Acceleration: {artifact.accelerationMultiplier:+0%;-0%}");
        if (artifact.shotPowerMultiplier != 0)
            mods.Add($"Shot Power: {artifact.shotPowerMultiplier:+0%;-0%}");
        if (artifact.checkForceMultiplier != 0)
            mods.Add($"Check Force: {artifact.checkForceMultiplier:+0%;-0%}");
        if (artifact.puckControlMultiplier != 0)
            mods.Add($"Puck Control: {artifact.puckControlMultiplier:+0%;-0%}");

        if (artifact.explosivePucks)
            mods.Add("<color=orange>Explosive Pucks</color>");
        if (artifact.slipperyIce)
            mods.Add("<color=cyan>Slippery Ice</color>");
        if (artifact.magneticStick)
            mods.Add("<color=yellow>Magnetic Stick</color>");
        if (artifact.heavyHitter)
            mods.Add("<color=red>Heavy Hitter</color>");

        return string.Join("\n", mods);
    }
}
