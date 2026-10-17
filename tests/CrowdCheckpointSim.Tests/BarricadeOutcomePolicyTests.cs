using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class BarricadeOutcomePolicyTests
{
    [Fact]
    public void FirstNinetyPercentAlwaysClearsOnFirstAttempt()
    {
        Assert.Equal(BarricadeOutcome.FirstPassGreen, BarricadeOutcomePolicy.FromUnitRoll(0.000, 5.0f));
        Assert.Equal(BarricadeOutcome.FirstPassGreen, BarricadeOutcomePolicy.FromUnitRoll(0.899, 5.0f));
    }

    [Fact]
    public void ConfiguredYellowBandClearsOnSecondAttempt()
    {
        Assert.Equal(BarricadeOutcome.SecondPassYellow, BarricadeOutcomePolicy.FromUnitRoll(0.901, 5.0f));
        Assert.Equal(BarricadeOutcome.SecondPassYellow, BarricadeOutcomePolicy.FromUnitRoll(0.949, 5.0f));
    }

    [Fact]
    public void RemainingTenPercentBecomesManualRed()
    {
        Assert.Equal(BarricadeOutcome.ManualRed, BarricadeOutcomePolicy.FromUnitRoll(0.951, 5.0f));
        Assert.Equal(BarricadeOutcome.ManualRed, BarricadeOutcomePolicy.FromUnitRoll(0.999, 5.0f));
    }

    [Fact]
    public void TenPercentYellowLeavesNoRedBand()
    {
        Assert.Equal(BarricadeOutcome.SecondPassYellow, BarricadeOutcomePolicy.FromUnitRoll(0.999, 10.0f));
    }

    [Fact]
    public void ZeroPercentYellowMakesAllFirstAttemptFailuresRed()
    {
        Assert.Equal(BarricadeOutcome.ManualRed, BarricadeOutcomePolicy.FromUnitRoll(0.901, 0.0f));
    }
}
