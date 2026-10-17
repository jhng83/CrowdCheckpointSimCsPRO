using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class OperatorTimingTests
{
    [Theory]
    [InlineData(10.0f, 0, 10.0f)]
    [InlineData(10.0f, 1, 12.5f)]
    [InlineData(10.0f, 4, 20.0f)]
    [InlineData(30.0f, 0, 30.0f)]
    [InlineData(30.0f, 1, 32.5f)]
    [InlineData(30.0f, 2, 35.0f)]
    [InlineData(30.0f, 4, 40.0f)]
    public void VerificationDurationStartsFromConfiguredInitialTimeAndKeepsAddingFatigue(
        float initialSeconds,
        int completed,
        float expected)
    {
        Assert.Equal(
            expected,
            OperatorTiming.VerificationDuration(initialSeconds, completed),
            3);
    }

    [Theory]
    [InlineData(0, 1.00f)]
    [InlineData(1, 0.90f)]
    [InlineData(2, 0.80f)]
    [InlineData(3, 0.70f)]
    [InlineData(4, 0.60f)]
    [InlineData(5, 0.50f)]
    [InlineData(9, 0.50f)]
    public void HealthBarStartsFullDropsTenPercentAndStopsAtFifty(int completed, float expected)
    {
        Assert.Equal(expected, OperatorTiming.HealthFraction(completed), 3);
    }

    [Fact]
    public void EscortRightOfWayEnvelopeIsWiderThanSingleOperatorPursuit()
    {
        Assert.True(OperatorTiming.EscortYieldInfluenceDistance > OperatorTiming.YieldInfluenceDistance);
        Assert.True(OperatorTiming.EscortYieldCorridorHalfWidth > OperatorTiming.YieldCorridorHalfWidth);
        Assert.True(OperatorTiming.EscortYieldSideStepDistance > OperatorTiming.YieldSideStepDistance);
    }
    [Fact]
    public void SimulationOperatorsUseConfiguredInitialManualClearanceTime()
    {
        SimulationConfig config = new()
        {
            InitialManualClearanceSeconds = 30.0f,
        };
        CrowdSimulation simulation = new(config);

        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(5.0f, 5.0f)));
        CheckpointOperator op = Assert.Single(simulation.Operators);

        Assert.Equal(30.0f, op.CurrentVerificationDuration, 3);
        op.CompletedVerificationCount = 1;
        Assert.Equal(37.5f, op.CurrentVerificationDuration, 3);
        op.CompletedVerificationCount = 2;
        Assert.Equal(45.0f, op.CurrentVerificationDuration, 3);
    }

}
