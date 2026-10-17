namespace CrowdCheckpointSim.Core;

/// <summary>
/// Stochastic Mode 2 checkpoint outcome policy. RandomSeed makes runs repeatable: 90% clear on
/// attempt one; the configured 0-10% clear on attempt two; the remainder of the 10% requires
/// Purple manual clearance.
/// </summary>
public static class BarricadeOutcomePolicy
{
    public static BarricadeOutcome Sample(Random random, float yellowPercent)
    {
        ArgumentNullException.ThrowIfNull(random);
        return FromUnitRoll(random.NextDouble(), yellowPercent);
    }

    /// <summary>Resolve an outcome from a unit-interval roll; public for deterministic tests.</summary>
    public static BarricadeOutcome FromUnitRoll(double roll, float yellowPercent)
    {
        if (roll < 0.0 || roll >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(roll), "Roll must be in [0, 1).");
        }

        if (yellowPercent < SimulationConfig.MinMultiLaneYellowPercent ||
            yellowPercent > SimulationConfig.MaxMultiLaneYellowPercent)
        {
            throw new ArgumentOutOfRangeException(nameof(yellowPercent));
        }

        double yellowFraction = yellowPercent / 100.0;
        if (roll < SimulationConfig.FixedMultiLaneGreenPercent / 100.0)
        {
            return BarricadeOutcome.FirstPassGreen;
        }

        if (roll < SimulationConfig.FixedMultiLaneGreenPercent / 100.0 + yellowFraction)
        {
            return BarricadeOutcome.SecondPassYellow;
        }

        return BarricadeOutcome.ManualRed;
    }
}
