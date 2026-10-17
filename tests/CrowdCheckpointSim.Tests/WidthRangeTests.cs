using System.Numerics;
using CrowdCheckpointSim.Configuration;
using CrowdCheckpointSim.Core;
using CrowdCheckpointSim.Rendering;
using Xunit;

namespace CrowdCheckpointSim.Tests;

/// <summary>Extended widths preserve funnel fit, single-file lane geometry and camera placement.</summary>
public sealed class WidthRangeTests
{
    [Theory]
    [InlineData(15f, 5f)]
    [InlineData(15f, 15f)]
    [InlineData(17.5f, 12.5f)]
    [InlineData(20f, 5f)]
    [InlineData(20f, 20f)]
    public void OpenFunnelUsesSelectedWidthAndPreservesAreaBasedPopulation(float hallWidth, float funnelWidth)
    {
        SimulationConfig config = new() { HallWidth = hallWidth, FunnelWidth = funnelWidth };
        CrowdSimulation simulation = new(config);
        Assert.Equal((int)MathF.Round(10f * funnelWidth * config.TargetFunnelDensity), simulation.FunnelPopulation);
        Assert.Equal(funnelWidth, config.FunnelYMax - config.FunnelYMin, 3);
        Assert.True(config.FunnelYMin >= 0f);
        Assert.True(config.FunnelYMax <= hallWidth);
        Assert.All(simulation.Pedestrians, p =>
        {
            Assert.InRange(p.Position.Y, config.FunnelYMin, config.FunnelYMax);
            Assert.True(float.IsFinite(p.Position.X));
        });
    }

    [Theory]
    [InlineData(4.9f, 20f)]
    [InlineData(20.1f, 20f)]
    [InlineData(16f, 15f)]
    [InlineData(float.NaN, 20f)]
    public void InvalidFunnelWidthIsRejected(float funnelWidth, float hallWidth) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationConfig { HallWidth = hallWidth, FunnelWidth = funnelWidth }.Validate());

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    public void BarricadeCreatesExactlySelectedNumberOfOneMetreGatesAndQueues(int lanes)
    {
        SimulationConfig config = new()
        {
            Mode = CheckpointMode.MultiLaneBarricade,
            HallWidth = lanes,
            FunnelWidth = lanes,
            TargetFunnelDensity = SimulationConfig.MaxSetupDensity,
        };
        CrowdSimulation simulation = new(config);
        Assert.Equal(lanes, config.MultiLaneCount);
        Assert.Equal(lanes, simulation.QueueLaneCount);
        Assert.Equal(lanes, simulation.CheckpointGates.Count);
        Assert.Equal(config.MultiLaneTotalCapacity, simulation.FunnelPopulation);
        for (int lane = 0; lane < lanes; lane++)
        {
            Assert.Equal(lane + 0.5f, simulation.CheckpointGates[lane].CentreY, 3);
            Assert.Equal(config.MultiLaneCapacityPerLane,
                simulation.Pedestrians.Count(p => p.QueueLane == lane));
        }
    }

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    public void BarricadeProcessesAndReleasesTravellersAtBothLaneBounds(int lanes)
    {
        CrowdSimulation simulation = new(new SimulationConfig
        {
            Mode = CheckpointMode.MultiLaneBarricade,
            HallWidth = lanes,
            FunnelWidth = lanes,
            TargetFunnelDensity = 0.5f,
            LaneClearanceSeconds = 3f,
        });
        for (int step = 0; step < 12; step++) simulation.Advance(1f / 60f);
        Assert.Equal(lanes, simulation.CheckpointGates.Count);
        Assert.All(simulation.CheckpointGates, gate => Assert.Equal(CheckpointGateState.Processing, gate.State));
        for (int step = 0; step < 7 * 60; step++) simulation.Advance(1f / 60f);
        Assert.True(simulation.Metrics.TotalCrossings > 0);
        Assert.True(simulation.RoomPopulation > 0);
    }

    [Theory]
    [InlineData(14f)]
    [InlineData(21f)]
    [InlineData(15.5f)]
    public void BarricadeRejectsOutOfRangeOrFractionalLaneWidth(float width) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationConfig
        {
            Mode = CheckpointMode.MultiLaneBarricade,
            HallWidth = width,
            FunnelWidth = width,
        }.Validate());

    [Theory]
    [InlineData(CheckpointMode.OpenFunnel, 15f, 5f)]
    [InlineData(CheckpointMode.OpenFunnel, 20f, 20f)]
    [InlineData(CheckpointMode.MultiLaneBarricade, 15f, 15f)]
    [InlineData(CheckpointMode.MultiLaneBarricade, 20f, 20f)]
    public void LargestHallFitsCameraAndAllowsFarCornerPostAndSaveLoad(
        CheckpointMode mode, float hallWidth, float funnelWidth)
    {
        SimulationConfig config = new()
        {
            Mode = mode, HallLength = 30f, HallWidth = hallWidth, FunnelWidth = funnelWidth,
            TargetFunnelDensity = 0.5f,
        };
        IsometricCamera camera = new();
        Vector2 min = new(80f, 60f), max = new(1680f, 960f);
        camera.Configure(min, max, config);
        foreach (float x in new[] { 0f, config.FunnelXMax })
        foreach (float y in new[] { 0f, hallWidth })
        foreach (float z in new[] { 0f, 2.15f })
        {
            Vector2 screen = camera.Project(new Vector3(x, y, z));
            Assert.InRange(screen.X, min.X, max.X);
            Assert.InRange(screen.Y, min.Y, max.Y);
        }
        Vector2 post = new(29f, hallWidth - 1f);
        Assert.True(camera.TryScreenToGround(camera.Project(new Vector3(post, 0f)), out Vector2 actual));
        Assert.True(Vector2.Distance(post, actual) < 0.001f);
        CrowdSimulation simulation = new(config);
        Assert.True(simulation.AddOperatorPost(post));

        string directory = Path.Combine(Path.GetTempPath(), $"crowd-width-{Guid.NewGuid():N}");
        try
        {
            ScenarioConfigStore store = new(directory);
            string path = store.Save("Wide hall", config, simulation.ResponderPosts);
            LoadedScenarioConfig loaded = store.Load(Path.GetFileName(path));
            Assert.Equal(hallWidth, loaded.Config.HallWidth);
            Assert.Equal(funnelWidth, loaded.Config.FunnelWidth);
            Assert.Equal(config.MultiLaneCount, loaded.Config.Clone().MultiLaneCount);
            Assert.Equal(post, Assert.Single(loaded.ResponderPosts).Position);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
