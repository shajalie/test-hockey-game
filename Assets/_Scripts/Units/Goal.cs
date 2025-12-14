using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Goal trigger detection for scoring with assist tracking.
/// Attach to a trigger collider inside the goal net.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Goal : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int teamIndex = 0; // 0 = Team A's goal (Team B scores here)

    [Header("Effects")]
    [SerializeField] private ParticleSystem goalEffect;
    [SerializeField] private AudioSource goalSound;
    [SerializeField] private float goalCelebrationTime = 3f;

    private bool goalScoredThisPlay;

    // Last goal info
    private HockeyPlayer lastScorer;
    private HockeyPlayer lastAssist;

    public int TeamIndex => teamIndex;
    public HockeyPlayer LastScorer => lastScorer;
    public HockeyPlayer LastAssist => lastAssist;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (goalScoredThisPlay) return;

        var puck = other.GetComponent<Puck>();
        if (puck != null)
        {
            // Get scorer and assist from puck's touch history
            OnPuckEntered(puck.LastOwner, puck.TouchHistory);
        }
    }

    /// <summary>
    /// Called when puck enters the goal. Supports assist tracking.
    /// </summary>
    public void OnPuckEntered(HockeyPlayer scorer, List<HockeyPlayer> touchHistory = null)
    {
        if (goalScoredThisPlay) return;

        goalScoredThisPlay = true;

        // Team that scores is opposite of goal owner
        int scoringTeam = teamIndex == 0 ? 1 : 0;

        // Track scorer
        lastScorer = scorer;

        // Track assist (second-to-last touch, same team as scorer)
        lastAssist = null;
        if (touchHistory != null && touchHistory.Count >= 2)
        {
            HockeyPlayer potentialAssist = touchHistory[1];
            if (potentialAssist != null && scorer != null &&
                potentialAssist.TeamId == scorer.TeamId && potentialAssist != scorer)
            {
                lastAssist = potentialAssist;
            }
        }

        // Log with details
        string scorerName = scorer != null ? scorer.name : "Unknown";
        string assistInfo = lastAssist != null ? $" (Assist: {lastAssist.name})" : "";
        Debug.Log($"[Goal] GOAL! Team {scoringTeam} scores! Scorer: {scorerName}{assistInfo}");

        // Play effects
        if (goalEffect != null)
        {
            goalEffect.Play();
        }

        if (goalSound != null)
        {
            goalSound.Play();
        }

        // Trigger event with scorer info
        GameEvents.TriggerGoalScored(scoringTeam);

        // Reset puck touch history
        var puck = FindObjectOfType<Puck>();
        if (puck != null)
        {
            puck.ClearTouchHistory();
        }

        // Reset after delay
        Invoke(nameof(ResetGoal), goalCelebrationTime);
    }

    private void ResetGoal()
    {
        goalScoredThisPlay = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = teamIndex == 0 ? Color.blue : Color.red;
        var col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }
}
