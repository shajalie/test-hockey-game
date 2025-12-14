using System;
using UnityEngine;

/// <summary>
/// Central event hub for game-wide communication.
/// Allows decoupled systems to react to game state changes.
/// </summary>
public static class GameEvents
{
    // === STATS & MODIFIERS ===
    /// <summary>Fired when player stats are recalculated (after artifact pickup)</summary>
    public static event Action OnStatsUpdated;

    /// <summary>Fired when a new artifact is acquired</summary>
    public static event Action<RunModifier> OnArtifactAcquired;

    // === PUCK EVENTS ===
    /// <summary>Fired when puck possession changes. Parameter is new owner (null if loose)</summary>
    public static event Action<GameObject> OnPuckPossessionChanged;

    /// <summary>Fired when a shot is taken</summary>
    public static event Action<Vector3, float> OnShotTaken; // direction, power

    // === GAME STATE ===
    /// <summary>Fired when a goal is scored. Parameter is scoring team index</summary>
    public static event Action<int> OnGoalScored;

    /// <summary>Fired when match starts</summary>
    public static event Action OnMatchStart;

    /// <summary>Fired when match ends</summary>
    public static event Action OnMatchEnd;

    /// <summary>Fired when entering draft/artifact selection</summary>
    public static event Action OnDraftStarted;

    // === PERIOD EVENTS ===
    /// <summary>Fired when a period ends</summary>
    public static event Action<int> OnPeriodEnd; // period number

    /// <summary>Fired when intermission starts</summary>
    public static event Action OnIntermissionStart;

    /// <summary>Fired when overtime starts</summary>
    public static event Action OnOvertimeStart;

    // === PENALTY EVENTS ===
    /// <summary>Fired when a penalty is called</summary>
    public static event Action<int, float> OnPenaltyCalled; // team index, duration

    /// <summary>Fired when a penalty expires</summary>
    public static event Action<int> OnPenaltyExpired; // team index

    /// <summary>Fired when a power play starts</summary>
    public static event Action<int> OnPowerPlayStart; // team on power play

    /// <summary>Fired when a power play ends</summary>
    public static event Action OnPowerPlayEnd;

    // === RULES EVENTS ===
    /// <summary>Fired when offsides is called</summary>
    public static event Action<int> OnOffsides; // offending team index

    /// <summary>Fired when icing is called</summary>
    public static event Action<int> OnIcing; // offending team index

    // === FACEOFF EVENTS ===
    /// <summary>Fired when a faceoff starts</summary>
    public static event Action<Vector3> OnFaceoffStart; // faceoff position

    /// <summary>Fired when a faceoff is won</summary>
    public static event Action<int> OnFaceoffWon; // winning team index

    // === SOUND EVENTS ===
    /// <summary>Fired when goal horn should play</summary>
    public static event Action<int> OnGoalHorn; // scoring team index

    /// <summary>Fired when whistle should play</summary>
    public static event Action OnWhistle;

    // === VISUAL EVENTS ===
    /// <summary>Fired when screen shake should occur</summary>
    public static event Action<float> OnScreenShake; // intensity

    /// <summary>Fired when goal celebration should trigger</summary>
    public static event Action<int> OnGoalCelebration; // scoring team index

    // === INVOKERS ===
    public static void TriggerStatsUpdated() => OnStatsUpdated?.Invoke();

    public static void TriggerArtifactAcquired(RunModifier modifier)
        => OnArtifactAcquired?.Invoke(modifier);

    public static void TriggerPuckPossessionChanged(GameObject newOwner)
        => OnPuckPossessionChanged?.Invoke(newOwner);

    public static void TriggerShotTaken(Vector3 direction, float power)
        => OnShotTaken?.Invoke(direction, power);

    public static void TriggerGoalScored(int teamIndex)
        => OnGoalScored?.Invoke(teamIndex);

    public static void TriggerMatchStart() => OnMatchStart?.Invoke();

    public static void TriggerMatchEnd() => OnMatchEnd?.Invoke();

    public static void TriggerDraftStarted() => OnDraftStarted?.Invoke();

    public static void TriggerPeriodEnd(int periodNumber)
        => OnPeriodEnd?.Invoke(periodNumber);

    public static void TriggerIntermissionStart() => OnIntermissionStart?.Invoke();

    public static void TriggerOvertimeStart() => OnOvertimeStart?.Invoke();

    public static void TriggerPenaltyCalled(int teamIndex, float duration)
        => OnPenaltyCalled?.Invoke(teamIndex, duration);

    public static void TriggerPenaltyExpired(int teamIndex)
        => OnPenaltyExpired?.Invoke(teamIndex);

    public static void TriggerPowerPlayStart(int teamIndex)
        => OnPowerPlayStart?.Invoke(teamIndex);

    public static void TriggerPowerPlayEnd() => OnPowerPlayEnd?.Invoke();

    public static void TriggerOffsides(int teamIndex)
        => OnOffsides?.Invoke(teamIndex);

    public static void TriggerIcing(int teamIndex)
        => OnIcing?.Invoke(teamIndex);

    public static void TriggerFaceoffStart(Vector3 position)
        => OnFaceoffStart?.Invoke(position);

    public static void TriggerFaceoffWon(int teamIndex)
        => OnFaceoffWon?.Invoke(teamIndex);

    public static void TriggerGoalHorn(int teamIndex)
        => OnGoalHorn?.Invoke(teamIndex);

    public static void TriggerWhistle() => OnWhistle?.Invoke();

    public static void TriggerScreenShake(float intensity)
        => OnScreenShake?.Invoke(intensity);

    public static void TriggerGoalCelebration(int teamIndex)
        => OnGoalCelebration?.Invoke(teamIndex);

    /// <summary>
    /// Clears all event subscribers. Call when returning to main menu
    /// to prevent memory leaks from destroyed objects.
    /// </summary>
    public static void ClearAllListeners()
    {
        OnStatsUpdated = null;
        OnArtifactAcquired = null;
        OnPuckPossessionChanged = null;
        OnShotTaken = null;
        OnGoalScored = null;
        OnMatchStart = null;
        OnMatchEnd = null;
        OnDraftStarted = null;
        OnPeriodEnd = null;
        OnIntermissionStart = null;
        OnOvertimeStart = null;
        OnPenaltyCalled = null;
        OnPenaltyExpired = null;
        OnPowerPlayStart = null;
        OnPowerPlayEnd = null;
        OnOffsides = null;
        OnIcing = null;
        OnFaceoffStart = null;
        OnFaceoffWon = null;
        OnGoalHorn = null;
        OnWhistle = null;
        OnScreenShake = null;
        OnGoalCelebration = null;
    }
}
