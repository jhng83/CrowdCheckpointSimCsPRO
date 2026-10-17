using System.Numerics;
using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

/// <summary>Tests the shared validity rule used by both ghost preview and actual placement.</summary>
public sealed class OperatorPlacementTests
{
    [Fact]
    public void ValidRoomPosition_IsAccepted()
    {
        SimulationConfig config = new();
        Vector2 candidate = new(5f, 5f);

        bool valid = ScenarioGeometry.IsValidOperatorPost(
            config,
            candidate,
            Array.Empty<Vector2>());

        Assert.True(valid);
    }

    [Fact]
    public void CandidateTooCloseToExistingPost_IsRejected()
    {
        SimulationConfig config = new();
        Vector2 existing = new(5f, 5f);
        Vector2 candidate = new(5.3f, 5f);

        bool valid = ScenarioGeometry.IsValidOperatorPost(
            config,
            candidate,
            new[] { existing });

        Assert.False(valid);
    }

    [Fact]
    public void ReconfigureToNarrowerHallRemovesPostsOutsideNewBoundary()
    {
        SimulationConfig wide = new() { HallWidth = 20.0f };
        CrowdSimulation simulation = new(wide);

        Assert.True(simulation.AddOperatorPost(new Vector2(5.0f, 19.0f)));
        Assert.Single(simulation.OperatorPosts);

        SimulationConfig narrower = wide.Clone();
        narrower.HallWidth = 15.0f;
        simulation.Reconfigure(narrower);

        Assert.Empty(simulation.OperatorPosts);
    }
}
