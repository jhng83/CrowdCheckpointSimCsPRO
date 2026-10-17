using System.Numerics;
using CrowdCheckpointSim.Configuration;
using CrowdCheckpointSim.Core;
using CrowdCheckpointSim.Rendering;
using Xunit;

namespace CrowdCheckpointSim.Tests;

/// <summary>Verify the extended hall range through geometry, queue flow, framing and persistence.</summary>
public sealed class HallLengthRangeTests
{
    [Theory]
    [InlineData(CheckpointMode.OpenFunnel, 20f)]
    [InlineData(CheckpointMode.OpenFunnel, 25f)]
    [InlineData(CheckpointMode.OpenFunnel, 30f)]
    [InlineData(CheckpointMode.MultiLaneBarricade, 20f)]
    [InlineData(CheckpointMode.MultiLaneBarricade, 25f)]
    [InlineData(CheckpointMode.MultiLaneBarricade, 30f)]
    public void ExtendedHallPreservesFunnelPopulationAndReleasesThroughCheckpoint(CheckpointMode mode, float length)
    {
        SimulationConfig config = new()
        {
            Mode = mode,
            HallLength = length,
            FunnelWidth = mode == CheckpointMode.MultiLaneBarricade ? 15f : 5f,
            TargetFunnelDensity = 0.5f,
            LaneClearanceSeconds = 3f,
        };
        CrowdSimulation simulation = new(config);
        SimulationConfig minimum = config.Clone();
        minimum.HallLength = SimulationConfig.MinHallLength;
        Assert.Equal(length, config.RoomWidth);
        Assert.Equal(length, config.FunnelXMin);
        Assert.Equal(length + 10f, config.FunnelXMax);
        Assert.Equal(minimum.TargetFunnelPopulation, simulation.FunnelPopulation);
        Assert.Equal(minimum.QueueRows, config.QueueRows);
        Assert.All(simulation.Pedestrians, p => Assert.InRange(p.Position.X, length, length + 10f));

        Vector2 farPost = new(length - 1f, config.HallWidth * 0.5f);
        Assert.True(simulation.AddOperatorPost(farPost));
        Assert.Equal(farPost, Assert.Single(simulation.Operators).HomePosition);
        for (int step = 0; step < 8 * 60; step++) simulation.Advance(1f / 60f);
        Assert.True(simulation.Metrics.TotalCrossings > 0);
        Assert.True(simulation.RoomPopulation > 0);
    }

    [Theory]
    [InlineData(20f)]
    [InlineData(25f)]
    [InlineData(30f)]
    public void CameraFitsFullHallAndFunnelAndUnprojectsFarEndPlacement(float length)
    {
        SimulationConfig config = new() { HallLength = length, HallWidth = 20f };
        Vector2 canvasMin = new(100f, 80f);
        Vector2 canvasMax = new(1700f, 980f);
        IsometricCamera camera = new();
        camera.Configure(canvasMin, canvasMax, config);
        foreach (float x in new[] { 0f, config.FunnelXMax })
        foreach (float y in new[] { 0f, config.HallWidth })
        foreach (float z in new[] { 0f, 2.15f })
        {
            Vector2 screen = camera.Project(new Vector3(x, y, z));
            Assert.InRange(screen.X, canvasMin.X, canvasMax.X);
            Assert.InRange(screen.Y, canvasMin.Y, canvasMax.Y);
        }
        Vector2 expected = new(length - 1f, config.HallWidth * 0.5f);
        Assert.True(camera.TryScreenToGround(camera.Project(new Vector3(expected, 0f)), out Vector2 actual));
        Assert.True(Vector2.Distance(expected, actual) < 0.001f);
    }

    [Theory]
    [InlineData(20f)]
    [InlineData(25f)]
    [InlineData(30f)]
    public void SaveLoadAndCloneRetainExtendedHallLength(float length)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"crowd-hall-range-{Guid.NewGuid():N}");
        try
        {
            ScenarioConfigStore store = new(directory);
            SimulationConfig config = new() { HallLength = length };
            Vector2[] posts = { new(length - 1f, 5f) };
            string path = store.Save("Extended hall", config, posts);
            LoadedScenarioConfig loaded = store.Load(Path.GetFileName(path));
            Assert.Equal(length, loaded.Config.HallLength);
            Assert.Equal(length, loaded.Config.Clone().HallLength);
            Assert.Equal(posts, loaded.OperatorPosts.ToArray());
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
