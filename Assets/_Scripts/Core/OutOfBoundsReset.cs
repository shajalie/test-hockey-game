using UnityEngine;

/// <summary>
/// Monitors all game objects and resets them if they fall out of bounds.
/// Attached to a manager object, it watches players and puck.
/// </summary>
public class OutOfBoundsReset : MonoBehaviour
{
    [Header("Bounds")]
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 20f;
    [SerializeField] private float maxDistance = 50f;

    [Header("Reset Settings")]
    [SerializeField] private float resetHeight = 0.5f;
    [SerializeField] private float checkInterval = 0.5f;

    private RinkBuilder rinkBuilder;
    private float lastCheckTime;

    private void Start()
    {
        rinkBuilder = FindObjectOfType<RinkBuilder>();
    }

    private void Update()
    {
        if (Time.time - lastCheckTime < checkInterval) return;
        lastCheckTime = Time.time;

        CheckAndResetOutOfBounds();
    }

    private void CheckAndResetOutOfBounds()
    {
        // Check all players
        HockeyPlayer[] players = FindObjectsOfType<HockeyPlayer>();
        foreach (var player in players)
        {
            if (IsOutOfBounds(player.transform.position))
            {
                ResetPlayer(player);
            }
        }

        // Check puck
        Puck puck = FindObjectOfType<Puck>();
        if (puck != null && IsOutOfBounds(puck.transform.position))
        {
            ResetPuck(puck);
        }

        // Also check PuckController
        PuckController puckController = FindObjectOfType<PuckController>();
        if (puckController != null && IsOutOfBounds(puckController.transform.position))
        {
            ResetPuckController(puckController);
        }
    }

    private bool IsOutOfBounds(Vector3 position)
    {
        // Below floor or above ceiling
        if (position.y < minY || position.y > maxY)
        {
            return true;
        }

        // Too far from center
        Vector3 center = rinkBuilder != null ? rinkBuilder.CenterIce : Vector3.zero;
        float distance = Vector3.Distance(new Vector3(position.x, 0, position.z),
                                          new Vector3(center.x, 0, center.z));
        if (distance > maxDistance)
        {
            return true;
        }

        return false;
    }

    private void ResetPlayer(HockeyPlayer player)
    {
        Debug.Log($"[OutOfBoundsReset] Resetting player {player.name} - was at {player.transform.position}");

        // Determine reset position based on team
        Vector3 resetPos;
        if (rinkBuilder != null)
        {
            // Reset to spawn point
            resetPos = player.TeamId == 0
                ? rinkBuilder.GetSpawnPoint("PlayerSpawn")?.position ?? new Vector3(-15, resetHeight, 0)
                : rinkBuilder.GetSpawnPoint("AISpawn")?.position ?? new Vector3(15, resetHeight, 0);
        }
        else
        {
            resetPos = player.TeamId == 0 ? new Vector3(-15, resetHeight, 0) : new Vector3(15, resetHeight, 0);
        }

        // Add some random offset to avoid stacking
        resetPos += new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        resetPos.y = resetHeight;

        player.transform.position = resetPos;

        // Reset velocity
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ResetPuck(Puck puck)
    {
        Debug.Log($"[OutOfBoundsReset] Resetting puck - was at {puck.transform.position}");

        Vector3 resetPos = rinkBuilder != null
            ? rinkBuilder.GetSpawnPoint("PuckSpawn")?.position ?? new Vector3(0, 0.1f, 0)
            : new Vector3(0, 0.1f, 0);

        puck.transform.position = resetPos;

        Rigidbody rb = puck.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ResetPuckController(PuckController puck)
    {
        Debug.Log($"[OutOfBoundsReset] Resetting puck controller - was at {puck.transform.position}");

        Vector3 resetPos = rinkBuilder != null
            ? rinkBuilder.GetSpawnPoint("PuckSpawn")?.position ?? new Vector3(0, 0.1f, 0)
            : new Vector3(0, 0.1f, 0);

        puck.transform.position = resetPos;

        Rigidbody rb = puck.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
