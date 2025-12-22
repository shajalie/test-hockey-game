using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Factory for creating arcade-styled UI elements programmatically.
/// All elements follow the ArcadeTheme styling.
/// </summary>
public static class UIFactory
{
    #region Base Elements

    /// <summary>
    /// Create a basic UI GameObject with RectTransform.
    /// </summary>
    public static GameObject CreateElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    /// <summary>
    /// Create a full-screen container.
    /// </summary>
    public static GameObject CreateFullScreenContainer(string name, Transform parent)
    {
        GameObject container = CreateElement(name, parent);
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return container;
    }

    #endregion

    #region Text Elements

    /// <summary>
    /// Create a TextMeshProUGUI element with arcade styling.
    /// </summary>
    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize = 0)
    {
        GameObject obj = CreateElement(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize > 0 ? fontSize : ArcadeTheme.BodySize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = ArcadeTheme.TextPrimary;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>
    /// Create a large title text with glow effect styling.
    /// </summary>
    public static TextMeshProUGUI CreateTitle(Transform parent, string text)
    {
        TextMeshProUGUI title = CreateText("Title", parent, text, ArcadeTheme.TitleSize);
        title.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        title.alignment = TextAlignmentOptions.Center;

        RectTransform rect = title.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -ArcadeTheme.ScreenPadding);
        rect.sizeDelta = new Vector2(1200, 120);

        return title;
    }

    /// <summary>
    /// Create a section header text.
    /// </summary>
    public static TextMeshProUGUI CreateHeader(Transform parent, string text, Vector2 position)
    {
        TextMeshProUGUI header = CreateText("Header", parent, text, ArcadeTheme.HeaderSize);
        header.fontStyle = FontStyles.Bold;
        header.color = ArcadeTheme.TextAccent;

        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(800, 60);

        return header;
    }

    /// <summary>
    /// Create body/description text.
    /// </summary>
    public static TextMeshProUGUI CreateBody(Transform parent, string text, Vector2 position, Vector2 size)
    {
        TextMeshProUGUI body = CreateText("Body", parent, text, ArcadeTheme.BodySize);
        body.fontStyle = FontStyles.Normal;
        body.color = ArcadeTheme.TextSecondary;
        body.alignment = TextAlignmentOptions.Left;

        RectTransform rect = body.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return body;
    }

    /// <summary>
    /// Create a small label text.
    /// </summary>
    public static TextMeshProUGUI CreateLabel(Transform parent, string text)
    {
        TextMeshProUGUI label = CreateText("Label", parent, text, ArcadeTheme.LabelSize);
        label.fontStyle = FontStyles.Bold;
        label.color = ArcadeTheme.TextSecondary;
        return label;
    }

    #endregion

    #region Buttons

    /// <summary>
    /// Create a primary arcade-style button (chunky, vibrant).
    /// </summary>
    public static Button CreatePrimaryButton(Transform parent, string text, UnityAction onClick, Vector2 position)
    {
        return CreateButton(parent, text, onClick, position, ArcadeTheme.ButtonPrimarySize,
            ArcadeTheme.GetPrimaryButtonColors(), true);
    }

    /// <summary>
    /// Create a secondary arcade-style button.
    /// </summary>
    public static Button CreateSecondaryButton(Transform parent, string text, UnityAction onClick, Vector2 position)
    {
        return CreateButton(parent, text, onClick, position, ArcadeTheme.ButtonSecondarySize,
            ArcadeTheme.GetSecondaryButtonColors(), false);
    }

    /// <summary>
    /// Create a small button.
    /// </summary>
    public static Button CreateSmallButton(Transform parent, string text, UnityAction onClick, Vector2 position)
    {
        return CreateButton(parent, text, onClick, position, ArcadeTheme.ButtonSmallSize,
            ArcadeTheme.GetSecondaryButtonColors(), false);
    }

    /// <summary>
    /// Create a back button (positioned bottom-left by default).
    /// </summary>
    public static Button CreateBackButton(Transform parent, UnityAction onClick)
    {
        Vector2 position = new Vector2(-600, -450);
        Button btn = CreateButton(parent, "BACK", onClick, position, new Vector2(200, 70),
            ArcadeTheme.GetSecondaryButtonColors(), false);
        btn.name = "Back Button";
        return btn;
    }

    /// <summary>
    /// Internal button creation with all styling options.
    /// </summary>
    private static Button CreateButton(Transform parent, string text, UnityAction onClick,
        Vector2 position, Vector2 size, ColorBlock colors, bool isPrimary)
    {
        // Container with border
        GameObject buttonObj = CreateElement($"{text} Button", parent);
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        // Background image (the button body)
        Image bgImage = buttonObj.AddComponent<Image>();
        bgImage.color = colors.normalColor;

        // Button component
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = bgImage;
        button.colors = colors;

        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        // Add border child
        CreateButtonBorder(buttonObj.transform, size, isPrimary);

        // Button text
        float textSize = isPrimary ? ArcadeTheme.ButtonTextSize : ArcadeTheme.ButtonTextSizeSmall;
        TextMeshProUGUI btnText = CreateText("Text", buttonObj.transform, text, textSize);
        btnText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        btnText.color = Color.white;
        btnText.raycastTarget = false;

        RectTransform textRect = btnText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return button;
    }

    /// <summary>
    /// Create the chunky border effect for arcade buttons.
    /// </summary>
    private static void CreateButtonBorder(Transform parent, Vector2 size, bool glowBorder)
    {
        // Bottom shadow (gives 3D depth)
        GameObject shadow = CreateElement("Shadow", parent);
        RectTransform shadowRect = shadow.GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.sizeDelta = new Vector2(0, -4);
        shadowRect.anchoredPosition = new Vector2(0, -2);
        shadow.transform.SetAsFirstSibling();

        Image shadowImage = shadow.AddComponent<Image>();
        shadowImage.color = new Color(0, 0, 0, 0.4f);
        shadowImage.raycastTarget = false;

        // Optional glow outline for primary buttons
        if (glowBorder)
        {
            GameObject glow = CreateElement("Glow", parent);
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.sizeDelta = new Vector2(8, 8);
            glow.transform.SetAsFirstSibling();

            Image glowImage = glow.AddComponent<Image>();
            glowImage.color = ArcadeTheme.WithAlpha(ArcadeTheme.Ice, 0.3f);
            glowImage.raycastTarget = false;
        }
    }

    #endregion

    #region Panels

    /// <summary>
    /// Create an arcade-styled panel with optional border.
    /// </summary>
    public static Image CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, bool hasBorder = true)
    {
        GameObject panelObj = CreateElement(name, parent);
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = ArcadeTheme.Panel;

        if (hasBorder)
        {
            CreatePanelBorder(panelObj.transform, size);
        }

        return panelImage;
    }

    /// <summary>
    /// Create a solid panel (no transparency).
    /// </summary>
    public static Image CreateSolidPanel(Transform parent, string name, Vector2 position, Vector2 size)
    {
        Image panel = CreatePanel(parent, name, position, size, true);
        panel.color = ArcadeTheme.PanelSolid;
        return panel;
    }

    /// <summary>
    /// Create a full-screen background.
    /// </summary>
    public static Image CreateBackground(Transform parent, Color? color = null)
    {
        GameObject bgObj = CreateFullScreenContainer("Background", parent);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = color ?? ArcadeTheme.Primary;
        bgImage.raycastTarget = true;
        return bgImage;
    }

    /// <summary>
    /// Create the glowing border effect for panels.
    /// </summary>
    private static void CreatePanelBorder(Transform parent, Vector2 size)
    {
        float borderWidth = ArcadeTheme.PanelBorderWidth;

        // Top border
        CreateBorderEdge(parent, "Top Border",
            new Vector2(0, size.y / 2 - borderWidth / 2),
            new Vector2(size.x, borderWidth));

        // Bottom border
        CreateBorderEdge(parent, "Bottom Border",
            new Vector2(0, -size.y / 2 + borderWidth / 2),
            new Vector2(size.x, borderWidth));

        // Left border
        CreateBorderEdge(parent, "Left Border",
            new Vector2(-size.x / 2 + borderWidth / 2, 0),
            new Vector2(borderWidth, size.y));

        // Right border
        CreateBorderEdge(parent, "Right Border",
            new Vector2(size.x / 2 - borderWidth / 2, 0),
            new Vector2(borderWidth, size.y));
    }

    private static void CreateBorderEdge(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject edge = CreateElement(name, parent);
        RectTransform rect = edge.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image edgeImage = edge.AddComponent<Image>();
        edgeImage.color = ArcadeTheme.PanelBorder;
        edgeImage.raycastTarget = false;
    }

    #endregion

    #region Specialized Components

    /// <summary>
    /// Create a heart icon for life display.
    /// </summary>
    public static Image CreateHeartIcon(Transform parent, bool isFilled, int index)
    {
        GameObject heartObj = CreateElement($"Heart_{index}", parent);
        RectTransform rect = heartObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50, 50);

        Image heartImage = heartObj.AddComponent<Image>();

        // Use color to indicate filled/empty (actual heart sprite would be added via Resources or passed in)
        heartImage.color = isFilled ? ArcadeTheme.Danger : ArcadeTheme.WithAlpha(ArcadeTheme.TextSecondary, 0.3f);

        return heartImage;
    }

    /// <summary>
    /// Create an artifact slot display.
    /// </summary>
    public static GameObject CreateArtifactSlot(Transform parent, RunModifier artifact, int index)
    {
        GameObject slotObj = CreateElement($"Artifact_{index}", parent);
        RectTransform rect = slotObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 80);

        // Background with rarity border
        Image bg = slotObj.AddComponent<Image>();

        if (artifact != null)
        {
            bg.color = ArcadeTheme.PanelSolid;

            // Rarity border
            GameObject border = CreateElement("Border", slotObj.transform);
            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(6, 6);
            border.transform.SetAsFirstSibling();

            Image borderImg = border.AddComponent<Image>();
            borderImg.color = ArcadeTheme.GetRarityColor(artifact.rarity);
            borderImg.raycastTarget = false;

            // Icon (if sprite available)
            if (artifact.icon != null)
            {
                GameObject iconObj = CreateElement("Icon", slotObj.transform);
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.1f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.sizeDelta = Vector2.zero;

                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = artifact.icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }
            else
            {
                // Placeholder text with first letter
                TextMeshProUGUI letter = CreateText("Letter", slotObj.transform,
                    artifact.artifactName.Substring(0, 1).ToUpper(), 32);
                letter.color = ArcadeTheme.GetRarityColor(artifact.rarity);
                RectTransform letterRect = letter.GetComponent<RectTransform>();
                letterRect.anchorMin = Vector2.zero;
                letterRect.anchorMax = Vector2.one;
                letterRect.sizeDelta = Vector2.zero;
            }
        }
        else
        {
            // Empty slot
            bg.color = ArcadeTheme.WithAlpha(ArcadeTheme.PanelSolid, 0.3f);

            // Question mark
            TextMeshProUGUI question = CreateText("Empty", slotObj.transform, "?", 36);
            question.color = ArcadeTheme.WithAlpha(ArcadeTheme.TextSecondary, 0.5f);
            RectTransform qRect = question.GetComponent<RectTransform>();
            qRect.anchorMin = Vector2.zero;
            qRect.anchorMax = Vector2.one;
            qRect.sizeDelta = Vector2.zero;
        }

        return slotObj;
    }

    /// <summary>
    /// Create a horizontal slider.
    /// </summary>
    public static Slider CreateSlider(Transform parent, string name, float value, Vector2 position, Vector2 size)
    {
        GameObject sliderObj = CreateElement(name, parent);
        RectTransform rect = sliderObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;

        // Background track
        GameObject bg = CreateElement("Background", sliderObj.transform);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = ArcadeTheme.PanelSolid;

        // Fill area
        GameObject fillArea = CreateElement("Fill Area", sliderObj.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fill = CreateElement("Fill", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = ArcadeTheme.Accent;
        slider.fillRect = fillRect;

        // Handle
        GameObject handleArea = CreateElement("Handle Slide Area", sliderObj.transform);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;

        GameObject handle = CreateElement("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(30, 50);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    /// <summary>
    /// Create a toggle switch.
    /// </summary>
    public static Toggle CreateToggle(Transform parent, string label, bool isOn, Vector2 position)
    {
        GameObject toggleRow = CreateElement($"{label} Toggle", parent);
        RectTransform rowRect = toggleRow.GetComponent<RectTransform>();
        rowRect.anchoredPosition = position;
        rowRect.sizeDelta = new Vector2(400, 60);

        // Label
        TextMeshProUGUI labelText = CreateText("Label", toggleRow.transform, label, ArcadeTheme.BodySize);
        labelText.alignment = TextAlignmentOptions.Left;
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.7f, 1);
        labelRect.sizeDelta = Vector2.zero;

        // Toggle box
        GameObject toggleObj = CreateElement("Toggle", toggleRow.transform);
        RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.75f, 0.1f);
        toggleRect.anchorMax = new Vector2(1f, 0.9f);
        toggleRect.sizeDelta = Vector2.zero;

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = isOn;

        // Background
        Image toggleBg = toggleObj.AddComponent<Image>();
        toggleBg.color = ArcadeTheme.PanelSolid;
        toggle.targetGraphic = toggleBg;

        // Checkmark
        GameObject checkmark = CreateElement("Checkmark", toggleObj.transform);
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.15f, 0.15f);
        checkRect.anchorMax = new Vector2(0.85f, 0.85f);
        checkRect.sizeDelta = Vector2.zero;
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = ArcadeTheme.Success;
        toggle.graphic = checkImage;

        return toggle;
    }

    /// <summary>
    /// Create a progress bar (horizontal fill).
    /// </summary>
    public static Image CreateProgressBar(Transform parent, string name, float value, Vector2 position, Vector2 size, Color fillColor)
    {
        GameObject barObj = CreateElement(name, parent);
        RectTransform rect = barObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        // Background
        Image bg = barObj.AddComponent<Image>();
        bg.color = ArcadeTheme.PanelSolid;

        // Fill
        GameObject fillObj = CreateElement("Fill", barObj.transform);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        fillRect.sizeDelta = Vector2.zero;

        Image fill = fillObj.AddComponent<Image>();
        fill.color = fillColor;

        return fill;
    }

    #endregion

    #region Layout Helpers

    /// <summary>
    /// Create a horizontal layout group container.
    /// </summary>
    public static HorizontalLayoutGroup CreateHorizontalGroup(Transform parent, string name, float spacing = 0)
    {
        GameObject groupObj = CreateElement(name, parent);
        HorizontalLayoutGroup group = groupObj.AddComponent<HorizontalLayoutGroup>();
        group.spacing = spacing > 0 ? spacing : ArcadeTheme.ElementSpacing;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = false;
        group.childControlHeight = false;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        return group;
    }

    /// <summary>
    /// Create a vertical layout group container.
    /// </summary>
    public static VerticalLayoutGroup CreateVerticalGroup(Transform parent, string name, float spacing = 0)
    {
        GameObject groupObj = CreateElement(name, parent);
        VerticalLayoutGroup group = groupObj.AddComponent<VerticalLayoutGroup>();
        group.spacing = spacing > 0 ? spacing : ArcadeTheme.ElementSpacing;
        group.childAlignment = TextAnchor.UpperCenter;
        group.childControlWidth = false;
        group.childControlHeight = false;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        return group;
    }

    /// <summary>
    /// Create a grid layout group container.
    /// </summary>
    public static GridLayoutGroup CreateGridGroup(Transform parent, string name, int columns, Vector2 cellSize)
    {
        GameObject groupObj = CreateElement(name, parent);
        GridLayoutGroup group = groupObj.AddComponent<GridLayoutGroup>();
        group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        group.constraintCount = columns;
        group.cellSize = cellSize;
        group.spacing = new Vector2(ArcadeTheme.ElementSpacing, ArcadeTheme.ElementSpacing);
        group.childAlignment = TextAnchor.MiddleCenter;
        return group;
    }

    #endregion
}
