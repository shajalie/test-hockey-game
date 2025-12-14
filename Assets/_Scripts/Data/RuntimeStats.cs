using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calculates final player stats by applying all active RunModifier artifacts
/// to the base PlayerStats. This is the "live" stats used during gameplay.
/// </summary>
[System.Serializable]
public class RuntimeStats
{
    // Cached calculated values
    public float MaxSpeed { get; private set; }
    public float AccelerationForce { get; private set; }
    public float BrakingDrag { get; private set; }
    public float IceDrag { get; private set; }
    public float CarvingSpeedPenalty { get; private set; }
    public float TurnSpeed { get; private set; }

    public float PuckMagnetRange { get; private set; }
    public float PuckMagnetStrength { get; private set; }
    public float PuckKnockLooseThreshold { get; private set; }

    public float MinShotPower { get; private set; }
    public float MaxShotPower { get; private set; }
    public float ChargeTime { get; private set; }

    public float CheckForce { get; private set; }
    public float DashMultiplier { get; private set; }
    public float DashDuration { get; private set; }
    public float DashCooldown { get; private set; }

    // Special effect flags (any artifact enables it)
    public bool HasExplosivePucks { get; private set; }
    public bool HasSlipperyIce { get; private set; }
    public bool HasMagneticStick { get; private set; }
    public bool IsHeavyHitter { get; private set; }

    // Bonus accumulators
    public float BonusDamage { get; private set; }
    public float BonusHealth { get; private set; }
    public float CooldownReduction { get; private set; }

    private PlayerStats baseStats;
    private List<RunModifier> activeModifiers = new List<RunModifier>();

    public RuntimeStats(PlayerStats baseStats)
    {
        this.baseStats = baseStats;
        Recalculate();
    }

    /// <summary>
    /// Add an artifact modifier and recalculate stats.
    /// </summary>
    public void AddModifier(RunModifier modifier)
    {
        if (modifier != null && !activeModifiers.Contains(modifier))
        {
            activeModifiers.Add(modifier);
            Recalculate();
            GameEvents.TriggerArtifactAcquired(modifier);
            GameEvents.TriggerStatsUpdated();
        }
    }

    /// <summary>
    /// Remove an artifact modifier and recalculate stats.
    /// </summary>
    public void RemoveModifier(RunModifier modifier)
    {
        if (activeModifiers.Remove(modifier))
        {
            Recalculate();
            GameEvents.TriggerStatsUpdated();
        }
    }

    /// <summary>
    /// Clear all modifiers (e.g., at start of new run).
    /// </summary>
    public void ClearModifiers()
    {
        activeModifiers.Clear();
        Recalculate();
        GameEvents.TriggerStatsUpdated();
    }

    /// <summary>
    /// Recalculate all stats from base + modifiers.
    /// Multipliers are additive (1.0 = no change, 0.2 = +20%, -0.1 = -10%).
    /// </summary>
    public void Recalculate()
    {
        if (baseStats == null)
        {
            Debug.LogError("RuntimeStats: No base stats assigned!");
            return;
        }

        // Sum all multipliers (they're additive, so 0.2 + 0.1 = +30% total)
        float speedMult = 1f;
        float accelMult = 1f;
        float shotMult = 1f;
        float checkMult = 1f;
        float puckControlMult = 1f;

        // Reset special effects
        HasExplosivePucks = false;
        HasSlipperyIce = false;
        HasMagneticStick = false;
        IsHeavyHitter = false;

        // Reset bonus accumulators
        BonusDamage = 0f;
        BonusHealth = 0f;
        CooldownReduction = 0f;

        // Aggregate all modifier effects
        foreach (var mod in activeModifiers)
        {
            speedMult += mod.skatingSpeedMultiplier;
            accelMult += mod.accelerationMultiplier;
            shotMult += mod.shotPowerMultiplier;
            checkMult += mod.checkForceMultiplier;
            puckControlMult += mod.puckControlMultiplier;

            // Special effects (OR'd together)
            HasExplosivePucks |= mod.explosivePucks;
            HasSlipperyIce |= mod.slipperyIce;
            HasMagneticStick |= mod.magneticStick;
            IsHeavyHitter |= mod.heavyHitter;

            // Bonus accumulators
            BonusDamage += mod.bonusDamage;
            BonusHealth += mod.bonusHealth;
            CooldownReduction += mod.cooldownReduction;
        }

        // Clamp multipliers to prevent negative values
        speedMult = Mathf.Max(0.1f, speedMult);
        accelMult = Mathf.Max(0.1f, accelMult);
        shotMult = Mathf.Max(0.1f, shotMult);
        checkMult = Mathf.Max(0.1f, checkMult);
        puckControlMult = Mathf.Max(0.1f, puckControlMult);

        // Apply multipliers to base stats
        MaxSpeed = baseStats.maxSpeed * speedMult;
        AccelerationForce = baseStats.accelerationForce * accelMult;
        BrakingDrag = baseStats.brakingDrag;
        IceDrag = baseStats.iceDrag * (HasSlipperyIce ? 0.3f : 1f); // Slippery ice reduces drag
        CarvingSpeedPenalty = baseStats.carvingSpeedPenalty;
        TurnSpeed = baseStats.turnSpeed;

        PuckMagnetRange = baseStats.puckMagnetRange * puckControlMult;
        PuckMagnetStrength = baseStats.puckMagnetStrength * puckControlMult;
        PuckKnockLooseThreshold = baseStats.puckKnockLooseThreshold * puckControlMult;

        MinShotPower = baseStats.minShotPower * shotMult;
        MaxShotPower = baseStats.maxShotPower * shotMult;
        ChargeTime = baseStats.chargeTime;

        CheckForce = baseStats.checkForce * checkMult * (IsHeavyHitter ? 1.2f : 1f);
        DashMultiplier = baseStats.dashMultiplier;
        DashDuration = baseStats.dashDuration;
        DashCooldown = Mathf.Max(0.1f, baseStats.dashCooldown * (1f - CooldownReduction));
    }

    /// <summary>
    /// Get a list of all currently active modifiers.
    /// </summary>
    public IReadOnlyList<RunModifier> GetActiveModifiers() => activeModifiers.AsReadOnly();
}
