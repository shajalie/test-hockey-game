using UnityEngine;

/// <summary>
/// Applies team colors to player visuals.
/// Works with capsule primitives (temporary) and sprite renderers (final art).
/// </summary>
public class TeamColorApplier : MonoBehaviour
{
    [Header("Team Colors")]
    [SerializeField] private Color homeTeamColor = new Color(0.2f, 0.4f, 0.9f, 1f); // Blue
    [SerializeField] private Color awayTeamColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red
    [SerializeField] private Color goalieColorBoost = new Color(0.1f, 0.1f, 0.1f, 0f); // Darker for goalies

    [Header("References")]
    [SerializeField] private Renderer targetRenderer;

    private int teamId = -1;
    private bool isGoalie = false;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private void Start()
    {
        // Try to get team ID from parent components
        HockeyPlayer player = GetComponent<HockeyPlayer>();
        if (player != null)
        {
            SetTeam(player.TeamId, player.Position == PlayerPosition.Goalie);
            return;
        }

        // Try TeamController on parent
        TeamController team = GetComponentInParent<TeamController>();
        if (team != null)
        {
            SetTeam(team.TeamId, false);
        }
    }

    /// <summary>
    /// Set the team ID and apply the appropriate color.
    /// </summary>
    public void SetTeam(int newTeamId, bool goalie = false)
    {
        teamId = newTeamId;
        isGoalie = goalie;
        ApplyColor();
    }

    /// <summary>
    /// Apply the team color to the renderer.
    /// </summary>
    public void ApplyColor()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer == null) return;
        }

        Color baseColor = teamId == 0 ? homeTeamColor : awayTeamColor;

        // Make goalies slightly darker/different
        if (isGoalie)
        {
            baseColor -= goalieColorBoost;
            baseColor.a = 1f;
        }

        // Use MaterialPropertyBlock for performance (doesn't create material instances)
        targetRenderer.GetPropertyBlock(propertyBlock);

        // Try URP shader property first, then standard
        propertyBlock.SetColor("_BaseColor", baseColor);
        propertyBlock.SetColor("_Color", baseColor);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Force refresh the color (call after team assignment changes).
    /// </summary>
    public void RefreshColor()
    {
        HockeyPlayer player = GetComponent<HockeyPlayer>();
        if (player != null)
        {
            SetTeam(player.TeamId, player.Position == PlayerPosition.Goalie);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && teamId >= 0)
        {
            ApplyColor();
        }
    }
#endif
}
