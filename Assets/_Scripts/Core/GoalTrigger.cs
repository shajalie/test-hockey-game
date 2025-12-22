using UnityEngine;

/// <summary>
/// Trigger zone for detecting goals.
/// Place on a trigger collider inside each goal.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalTrigger : MonoBehaviour
{
    [Header("Goal Settings")]
    [SerializeField] private int teamId = 0; // Team this goal belongs to (0 = home, 1 = away)

    [Header("Visual Feedback")]
    [SerializeField] private Light goalLight;
    [SerializeField] private Color goalLightColor = Color.red;
    [SerializeField] private float lightDuration = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip goalHorn;
    [SerializeField] private AudioSource audioSource;

    private Collider triggerCollider;

    /// <summary>Team ID this goal belongs to.</summary>
    public int TeamId => teamId;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the puck
        PuckController puck = other.GetComponent<PuckController>();
        if (puck != null)
        {
            // Goal scored!
            OnGoalScored();
        }
    }

    private void OnGoalScored()
    {
        Debug.Log($"[GoalTrigger] GOAL on team {teamId}!");

        // Play goal horn
        if (goalHorn != null && audioSource != null)
        {
            audioSource.PlayOneShot(goalHorn);
        }

        // Flash goal light
        if (goalLight != null)
        {
            goalLight.color = goalLightColor;
            goalLight.enabled = true;
            Invoke(nameof(DisableLight), lightDuration);
        }
    }

    private void DisableLight()
    {
        if (goalLight != null)
        {
            goalLight.enabled = false;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw goal zone
        Gizmos.color = teamId == 0 ? Color.blue : Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else
        {
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 2f);
        }
    }
}
