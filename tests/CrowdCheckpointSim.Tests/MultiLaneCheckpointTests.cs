using System.Numerics;
using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class MultiLaneCheckpointTests
{
    [Theory]
    [InlineData(15.0f, 15)]
    [InlineData(17.0f, 17)]
    [InlineData(20.0f, 20)]
    public void HallWidthDirectlyDeterminesOneMetreLaneCount(float hallWidth, int expectedLanes)
    {
        SimulationConfig config = CreateMultiLaneConfig(hallWidth);

        config.Validate();

        Assert.Equal(expectedLanes, config.MultiLaneCount);
        Assert.Equal(expectedLanes, config.QueueColumns);
        Assert.Equal(hallWidth, config.FunnelWidth, 3);
    }

    [Fact]
    public void MultiLaneModeRejectsFractionalHallWidth()
    {
        SimulationConfig config = CreateMultiLaneConfig(17.0f);
        config.HallWidth = 17.5f;
        config.FunnelWidth = 17.5f;

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void MaximumDensityFillsEveryLaneToCapacity()
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.TargetFunnelDensity = SimulationConfig.MaxSetupDensity;
        CrowdSimulation simulation = new(config);

        Assert.Equal(config.MultiLaneTotalCapacity, simulation.FunnelPopulation);
        Assert.Equal(config.MultiLaneCount, simulation.QueueLaneCount);

        int[] counts = simulation.Pedestrians
            .Where(p => p.Phase == PedestrianPhase.Funnel)
            .GroupBy(p => p.QueueLane)
            .OrderBy(group => group.Key)
            .Select(group => group.Count())
            .ToArray();

        Assert.Equal(config.MultiLaneCount, counts.Length);
        Assert.All(counts, count => Assert.Equal(config.MultiLaneCapacityPerLane, count));
    }

    [Fact]
    public void LowerDensityProducesUnevenSingleFileQueueLengths()
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.TargetFunnelDensity = 4.0f;
        CrowdSimulation simulation = new(config);

        int[] counts = simulation.Pedestrians
            .Where(p => p.Phase == PedestrianPhase.Funnel)
            .GroupBy(p => p.QueueLane)
            .OrderBy(group => group.Key)
            .Select(group => group.Count())
            .ToArray();

        Assert.Equal(config.MultiLaneCount, counts.Length);
        Assert.True(counts.Distinct().Count() > 1);
        Assert.All(counts, count => Assert.InRange(count, 1, config.MultiLaneCapacityPerLane));
    }

    [Fact]
    public void GateProcessesThenOpensBeforeFirstPassTravellerCrosses()
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.TargetFunnelDensity = 0.5f;
        config.LaneClearanceSeconds = 3.0f;
        CrowdSimulation simulation = new(config);

        AdvanceSeconds(simulation, 0.20f);
        Assert.All(simulation.CheckpointGates, gate =>
        {
            Assert.Equal(CheckpointGateState.Processing, gate.State);
            Assert.NotNull(gate.ActivePedestrianId);
            Assert.Equal(1, gate.AttemptNumber);
        });

        AdvanceSeconds(simulation, 3.05f);
        Assert.Contains(simulation.CheckpointGates, gate => gate.State == CheckpointGateState.Open);

        AdvanceSeconds(simulation, 2.0f);
        Assert.True(simulation.Metrics.TotalCrossings > 0);
    }

    [Theory]
    [InlineData(3.0f)]
    [InlineData(10.0f)]
    [InlineData(20.0f)]
    public void LaneClearanceAcceptsThreeToTwentySeconds(float seconds)
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.LaneClearanceSeconds = seconds;

        config.Validate();

        Assert.Equal(seconds, config.LaneClearanceSeconds, 3);
    }

    [Theory]
    [InlineData(2.9f)]
    [InlineData(20.1f)]
    public void LaneClearanceOutsideThreeToTwentySecondsIsRejected(float seconds)
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.LaneClearanceSeconds = seconds;

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Theory]
    [InlineData(0.0f, 10.0f)]
    [InlineData(4.0f, 6.0f)]
    [InlineData(10.0f, 0.0f)]
    public void Mode2YellowAndRedShareTheRemainingTenPercent(float yellow, float expectedRed)
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.MultiLaneYellowPercent = yellow;

        config.Validate();

        Assert.Equal(90.0f, config.MultiLaneGreenPercent, 3);
        Assert.Equal(yellow, config.MultiLaneYellowPercent, 3);
        Assert.Equal(expectedRed, config.MultiLaneRedPercent, 3);
        Assert.Equal(100.0f, config.MultiLaneGreenPercent + config.MultiLaneYellowPercent + config.MultiLaneRedPercent, 3);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(10.1f)]
    public void Mode2YellowOutsideZeroToTenPercentIsRejected(float yellow)
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.MultiLaneYellowPercent = yellow;

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Theory]
    [InlineData(10.0f)]
    [InlineData(20.0f)]
    [InlineData(30.0f)]
    [InlineData(120.0f)]
    [InlineData(180.0f)]
    public void Mode2ManualClearanceAcceptsTenToOneHundredEightySeconds(float seconds)
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.MultiLaneManualClearanceSeconds = seconds;

        config.Validate();

        Assert.Equal(seconds, config.MultiLaneManualClearanceSeconds, 3);
    }

    [Theory]
    [InlineData(9.9f)]
    [InlineData(180.1f)]
    public void Mode2ManualClearanceOutsideTenToOneHundredEightySecondsIsRejected(float seconds)
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.MultiLaneManualClearanceSeconds = seconds;

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void Mode2RedBarricadeFailureDispatchesPurpleEvenOutsideAcquisitionRadius()
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.OperatorAcquisitionRadius = SimulationConfig.MinOperatorAcquisitionRadius;
        CrowdSimulation simulation = new(config);
        Assert.True(simulation.AddOperatorPost(new Vector2(1.0f, 1.0f)));

        Pedestrian red = simulation.Pedestrians[0];
        red.Phase = PedestrianPhase.Room;
        red.Position = new Vector2(9.0f, 8.0f);
        red.DetectionStatus = DetectionStatus.NotCountedNotDetected;
        red.RequiresManualClearance = true;
        red.CooperativeStop = true;
        red.Velocity = Vector2.Zero;

        Assert.True(Vector2.Distance(simulation.Operators[0].Position, red.Position) > config.OperatorAcquisitionRadius);

        simulation.Advance(1f / 60f);

        CheckpointOperator op = simulation.Operators[0];
        Assert.Equal(red.Id, op.TargetPedestrianId);
        Assert.Equal(OperatorState.Pursuing, op.State);
    }

    [Fact]
    public void Mode2OperatorsDoNotAccumulateVerificationFatigue()
    {
        SimulationConfig config = CreateMultiLaneConfig(15.0f);
        config.MultiLaneManualClearanceSeconds = 18.0f;
        CrowdSimulation simulation = new(config);
        Assert.True(simulation.AddOperatorPost(new Vector2(5.0f, 5.0f)));

        CheckpointOperator op = Assert.Single(simulation.Operators);
        op.CompletedVerificationCount = 5;

        Assert.False(op.UseVerificationFatigue);
        Assert.Equal(18.0f, op.CurrentVerificationDuration, 3);
        Assert.Equal(1.0f, op.HealthFraction, 3);
    }

    private static SimulationConfig CreateMultiLaneConfig(float hallWidth) => new()
    {
        Mode = CheckpointMode.MultiLaneBarricade,
        HallWidth = hallWidth,
        FunnelWidth = hallWidth,
        LaneClearanceSeconds = SimulationConfig.DefaultLaneClearanceSeconds,
    };

    private static void AdvanceSeconds(CrowdSimulation simulation, float seconds)
    {
        int steps = (int)MathF.Ceiling(seconds * 60f);
        for (int i = 0; i < steps; i++)
        {
            simulation.Advance(1f / 60f);
        }
    }
}
