using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Settings screen for audio, graphics, and control options.
/// </summary>
public class SettingsScreen : ScreenBase
{
    #region Properties

    public override string ScreenName => "Settings";
    protected override Color BackgroundColor => new Color32(20, 20, 30, 255);

    #endregion

    #region UI References

    private Slider masterVolumeSlider;
    private Slider sfxVolumeSlider;
    private Slider musicVolumeSlider;
    private TextMeshProUGUI masterVolumeText;
    private TextMeshProUGUI sfxVolumeText;
    private TextMeshProUGUI musicVolumeText;
    private Toggle fullscreenToggle;
    private Toggle vsyncToggle;
    private Toggle screenShakeToggle;

    #endregion

    #region PlayerPrefs Keys

    private const string PREF_MASTER_VOLUME = "MasterVolume";
    private const string PREF_SFX_VOLUME = "SFXVolume";
    private const string PREF_MUSIC_VOLUME = "MusicVolume";
    private const string PREF_FULLSCREEN = "Fullscreen";
    private const string PREF_VSYNC = "VSync";
    private const string PREF_SCREEN_SHAKE = "ScreenShake";

    #endregion

    #region Build UI

    protected override void BuildUI()
    {
        Transform root = GetRoot();

        // Title
        UIFactory.CreateTitle(root, "SETTINGS");

        // Settings panel
        Image panel = UIFactory.CreatePanel(root, "Settings Panel", Vector2.zero, new Vector2(900, 600), true);
        Transform panelRoot = panel.transform;

        float yPos = 220f;
        float rowSpacing = 90f;

        // Audio section header
        CreateSectionHeader(panelRoot, "AUDIO", new Vector2(0, yPos + 40));

        // Master Volume
        CreateVolumeRow(panelRoot, "Master Volume", "MASTER", ref masterVolumeSlider, ref masterVolumeText,
            new Vector2(0, yPos), PREF_MASTER_VOLUME);
        yPos -= rowSpacing;

        // SFX Volume
        CreateVolumeRow(panelRoot, "SFX Volume", "SFX", ref sfxVolumeSlider, ref sfxVolumeText,
            new Vector2(0, yPos), PREF_SFX_VOLUME);
        yPos -= rowSpacing;

        // Music Volume
        CreateVolumeRow(panelRoot, "Music Volume", "MUSIC", ref musicVolumeSlider, ref musicVolumeText,
            new Vector2(0, yPos), PREF_MUSIC_VOLUME);
        yPos -= rowSpacing + 20;

        // Display section header
        CreateSectionHeader(panelRoot, "DISPLAY", new Vector2(0, yPos + 30));

        // Fullscreen toggle
        fullscreenToggle = CreateToggleRow(panelRoot, "Fullscreen", new Vector2(0, yPos - 30), PREF_FULLSCREEN);
        yPos -= rowSpacing;

        // VSync toggle
        vsyncToggle = CreateToggleRow(panelRoot, "VSync", new Vector2(0, yPos - 30), PREF_VSYNC);
        yPos -= rowSpacing;

        // Screen shake toggle
        screenShakeToggle = CreateToggleRow(panelRoot, "Screen Shake", new Vector2(0, yPos - 30), PREF_SCREEN_SHAKE);
    }

    private void CreateSectionHeader(Transform parent, string text, Vector2 position)
    {
        TextMeshProUGUI header = UIFactory.CreateText("Header", parent, text, ArcadeTheme.BodySize);
        header.color = ArcadeTheme.TextAccent;
        header.fontStyle = FontStyles.Bold;
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(800, 40);
    }

    private void CreateVolumeRow(Transform parent, string name, string label,
        ref Slider sliderRef, ref TextMeshProUGUI valueTextRef, Vector2 position, string prefKey)
    {
        GameObject row = UIFactory.CreateElement(name + " Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchoredPosition = position;
        rowRect.sizeDelta = new Vector2(800, 60);

        // Label
        TextMeshProUGUI labelText = UIFactory.CreateText("Label", row.transform, label, ArcadeTheme.BodySize);
        labelText.alignment = TextAlignmentOptions.Left;
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.25f, 1);
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = new Vector2(50, 0);

        // Slider
        float savedValue = PlayerPrefs.GetFloat(prefKey, 1f);
        sliderRef = UIFactory.CreateSlider(row.transform, name + " Slider", savedValue,
            new Vector2(50, 0), new Vector2(400, 40));
        RectTransform sliderRect = sliderRef.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.25f, 0.2f);
        sliderRect.anchorMax = new Vector2(0.75f, 0.8f);
        sliderRect.anchoredPosition = Vector2.zero;
        sliderRect.sizeDelta = Vector2.zero;

        // Value text
        valueTextRef = UIFactory.CreateText("Value", row.transform, $"{Mathf.RoundToInt(savedValue * 100)}%", ArcadeTheme.BodySize);
        RectTransform valueRect = valueTextRef.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.75f, 0);
        valueRect.anchorMax = new Vector2(1, 1);
        valueRect.sizeDelta = Vector2.zero;

        // Slider callback
        string key = prefKey;
        TextMeshProUGUI valueText = valueTextRef;
        sliderRef.onValueChanged.AddListener((value) =>
        {
            valueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            PlayerPrefs.SetFloat(key, value);
            ApplyVolumeSetting(key, value);
        });
    }

    private Toggle CreateToggleRow(Transform parent, string label, Vector2 position, string prefKey)
    {
        bool savedValue = PlayerPrefs.GetInt(prefKey, 1) == 1;
        Toggle toggle = UIFactory.CreateToggle(parent, label, savedValue, position);

        RectTransform toggleRect = toggle.GetComponent<RectTransform>().parent.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(500, 60);

        string key = prefKey;
        toggle.onValueChanged.AddListener((isOn) =>
        {
            PlayerPrefs.SetInt(key, isOn ? 1 : 0);
            ApplyDisplaySetting(key, isOn);
        });

        return toggle;
    }

    #endregion

    #region Settings Application

    private void ApplyVolumeSetting(string key, float value)
    {
        switch (key)
        {
            case PREF_MASTER_VOLUME:
                AudioListener.volume = value;
                break;
            case PREF_SFX_VOLUME:
                // Would apply to SoundManager
                break;
            case PREF_MUSIC_VOLUME:
                // Would apply to MusicManager
                break;
        }
    }

    private void ApplyDisplaySetting(string key, bool value)
    {
        switch (key)
        {
            case PREF_FULLSCREEN:
                Screen.fullScreen = value;
                break;
            case PREF_VSYNC:
                QualitySettings.vSyncCount = value ? 1 : 0;
                break;
            case PREF_SCREEN_SHAKE:
                // Would store preference for screen shake effects
                break;
        }
    }

    #endregion

    #region Lifecycle

    protected override void OnBeforeHide()
    {
        // Save all settings
        PlayerPrefs.Save();
    }

    #endregion
}
