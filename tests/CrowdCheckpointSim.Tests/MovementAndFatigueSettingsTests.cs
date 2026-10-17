using System.Numerics;
using CrowdCheckpointSim.Configuration;
using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

/// <summary>Behaviour and persistence checks for ordinary movement and operator-fatigue controls.</summary>
public sealed class MovementAndFatigueSettingsTests
{
    [Theory]
    [InlineData(10f, 25f, 2, 15f)]
    [InlineData(120f, 10f, 3, 156f)]
    [InlineData(180f, 0f, 9, 180f)]
    [InlineData(180f, 25f, 1, 225f)]
    public void ClearanceFatigueAddsPercentageOfInitialTime(
        float initial, float percent, int completed, float expected)
    {
        Assert.Equal(expected, OperatorTiming.VerificationDuration(initial, completed, percent), 3);
    }

    [Theory]
    [InlineData(10f, 0, 2f)]
    [InlineData(10f, 1, 1.8f)]
    [InlineData(10f, 5, 1f)]
    [InlineData(10f, 20, 1f)]
    [InlineData(17f, 3, 1f)]
    [InlineData(0f, 100, 2f)]
    public void InterceptionLossStopsAtHalfIncludingOvershoot(float percent, int catches, float expected)
    {
        Assert.Equal(expected, OperatorTiming.InterceptionSpeed(2f, catches, percent), 3);
    }

    [Fact]
    public void PassengerSpeedBoundsRescaleSameSeedWithoutChangingOtherRandomTraits()
    {
        CrowdSimulation baseline = new(new SimulationConfig());
        CrowdSimulation adjusted = new(new SimulationConfig
        {
            PassengerMinimumSpeed = 0.5f,
            PassengerMaximumSpeed = 1.2f,
        });
        Assert.Equal(baseline.Pedestrians.Count, adjusted.Pedestrians.Count);
        for (int i = 0; i < baseline.Pedestrians.Count; i++)
        {
            Pedestrian original = baseline.Pedestrians[i];
            Pedestrian changed = adjusted.Pedestrians[i];
            Assert.InRange(changed.PreferredSpeed, 0.5f, 1.2f);
            Assert.Equal(original.Id, changed.Id);
            Assert.Equal(original.Position, changed.Position);
            Assert.Equal(original.Urgency, changed.Urgency);
            Assert.Equal(original.ExitTargetY, changed.ExitTargetY);
            Assert.Equal(original.QueuePhase, changed.QueuePhase);
            float fraction = (original.PreferredSpeed - 0.85f) / 0.7f;
            Assert.Equal(0.5f + fraction * 0.7f, changed.PreferredSpeed, 4);
        }
    }

    [Fact]
    public void EqualPassengerBoundsGiveOnePreferredSpeedAndPersistAfterReset()
    {
        CrowdSimulation simulation = new(new SimulationConfig
        {
            PassengerMinimumSpeed = 1.2f,
            PassengerMaximumSpeed = 1.2f,
        });
        Assert.All(simulation.Pedestrians, person => Assert.Equal(1.2f, person.PreferredSpeed, 3));
        simulation.Reset();
        Assert.All(simulation.Pedestrians, person => Assert.Equal(1.2f, person.PreferredSpeed, 3));
    }

    [Fact]
    public void PhysicalCatchAppliesSpeedLossOnceAndRestartClearsIt()
    {
        CrowdSimulation simulation = new(new SimulationConfig
        {
            OperatorInterceptionSpeed = 2f,
            OperatorInterceptionSpeedLossPercent = 20f,
        });
        Assert.True(simulation.AddOperatorPost(new Vector2(5f, 5f)));
        CheckpointOperator op = Assert.Single(simulation.Operators);
        Pedestrian target = simulation.Pedestrians[0];
        target.Phase = PedestrianPhase.Room;
        target.DetectionStatus = DetectionStatus.CountedNotDetected;
        target.Position = new Vector2(5.3f, 5f);
        op.State = OperatorState.Pursuing;
        op.TargetPedestrianId = target.Id;

        simulation.Advance(1f / 60f);
        Assert.Equal(1, op.CatchCount);
        Assert.Equal(1.6f, op.CurrentInterceptionSpeed, 3);
        Assert.Equal(OperatorState.Stopping, op.State);
        simulation.Advance(0.25f);
        Assert.Equal(1, op.CatchCount);

        simulation.Reset();
        op = Assert.Single(simulation.Operators);
        Assert.Equal(0, op.CatchCount);
        Assert.Equal(2f, op.CurrentInterceptionSpeed, 3);
    }

    [Fact]
    public void DasherAndPurpleDasherTrajectoriesIgnoreNormalSpeedAndFatigueSettings()
    {
        CrowdSimulation baseline = new(new SimulationConfig());
        CrowdSimulation changed = new(new SimulationConfig
        {
            PassengerMinimumSpeed = 0.3f,
            PassengerMaximumSpeed = 0.5f,
            OperatorInterceptionSpeed = 0.5f,
            OperatorInterceptionSpeedLossPercent = 50f,
            ManualClearanceFatigueIncreasePercent = 100f,
        });
        Pedestrian originalDasher = PrepareDasher(baseline);
        Pedestrian changedDasher = PrepareDasher(changed);
        CheckpointOperator originalOperator = Assert.Single(baseline.Operators);
        CheckpointOperator changedOperator = Assert.Single(changed.Operators);
        originalOperator.CatchCount = changedOperator.CatchCount = 10;

        for (int i = 0; i < 10; i++)
        {
            baseline.Advance(1f / 60f);
            changed.Advance(1f / 60f);
            Assert.Equal(originalDasher.Position, changedDasher.Position);
            Assert.Equal(originalDasher.Velocity, changedDasher.Velocity);
            Assert.Equal(originalOperator.Position, changedOperator.Position);
            Assert.Equal(originalOperator.Velocity, changedOperator.Velocity);
            Assert.Equal(OperatorState.DasherPursuing, changedOperator.State);
        }
    }

    [Fact]
    public void Mode2RetainsFixedNormalSpeedAndNoFatigue()
    {
        CrowdSimulation simulation = new(new SimulationConfig
        {
            Mode = CheckpointMode.MultiLaneBarricade,
            FunnelWidth = 15f,
            MultiLaneManualClearanceSeconds = 180f,
            OperatorInterceptionSpeed = 3f,
            OperatorInterceptionSpeedLossPercent = 50f,
            ManualClearanceFatigueIncreasePercent = 100f,
        });
        Assert.True(simulation.AddOperatorPost(new Vector2(5f, 5f)));
        CheckpointOperator op = Assert.Single(simulation.Operators);
        op.CatchCount = op.CompletedVerificationCount = 10;
        Assert.Equal(180f, op.CurrentVerificationDuration);
        Assert.Equal(1f, op.HealthFraction);
        Assert.Equal(OperatorTiming.MaxWalkingSpeed, op.CurrentInterceptionSpeed);
    }

    [Fact]
    public void LegacySavedScenarioKeepsFixedTwoPointFiveSecondIncrement()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"crowd-legacy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "legacy.json"),
                """
                {"FormatVersion":1,"DisplayName":"Old trial","Scenario":{"InitialManualClearanceSeconds":30},"OperatorPosts":[]}
                """);
            SimulationConfig config = new ScenarioConfigStore(directory).Load("legacy.json").Config;
            Assert.Equal(SimulationConfig.DefaultPassengerMinimumSpeed, config.PassengerMinimumSpeed);
            Assert.Equal(0f, config.OperatorInterceptionSpeedLossPercent);
            Assert.Equal(32.5f,
                OperatorTiming.VerificationDuration(30f, 1, config.ManualClearanceFatigueIncreasePercent), 3);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void InvalidOrNonFiniteMovementSettingsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationConfig { PassengerMinimumSpeed = 1.4f, PassengerMaximumSpeed = 1.2f }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationConfig { PassengerMaximumSpeed = 1.66f }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationConfig { OperatorInterceptionSpeed = float.NaN }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationConfig { PassengerMinimumSpeed = float.PositiveInfinity }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationConfig { ManualClearanceFatigueIncreasePercent = -1f }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationConfig { OperatorInterceptionSpeedLossPercent = 51f }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationConfig { InitialManualClearanceSeconds = float.NaN }.Validate());
    }

    private static Pedestrian PrepareDasher(CrowdSimulation simulation)
    {
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 5f)));
        Pedestrian dasher = simulation.Pedestrians[0];
        Assert.True(simulation.TrySelectDasher(dasher.Id));
        dasher.Phase = PedestrianPhase.Room;
        dasher.Position = new Vector2(5f, 5f);
        dasher.DetectionStatus = DetectionStatus.DasherIncident;
        dasher.DasherState = DasherState.Dashing;
        return dasher;
    }
}
