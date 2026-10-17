using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class QueueMovementTests
{
    [Fact]
    public void DefaultConfigurationCreatesThirtyRowsAcrossElevenQueueLanes()
    {
        SimulationConfig config = new();
        CrowdSimulation simulation = new(config);

        Assert.Equal(30, config.QueueRows);
        Assert.Equal(11, config.QueueColumns);
        Assert.Equal(11, simulation.QueueLaneCount);
        Assert.Equal(330, simulation.FunnelPopulation);

        foreach (IGrouping<int, Pedestrian> lane in simulation.Pedestrians.GroupBy(p => p.QueueLane))
        {
            Assert.Equal(30, lane.Count());
        }
    }

    [Fact]
    public void FunnelPedestriansRemainCloseToTheirAssignedQueueLane()
    {
        SimulationConfig config = new();
        CrowdSimulation simulation = new(config);

        for (int i = 0; i < 120; i++)
        {
            simulation.Advance(1f / 60f);
        }

        float centreMargin = config.AgentRadius + config.QueueWallClearance;
        float lowerY = config.FunnelYMin + centreMargin;
        float upperY = config.FunnelYMax - centreMargin;

        foreach (Pedestrian pedestrian in simulation.Pedestrians.Where(p => p.Phase == PedestrianPhase.Funnel))
        {
            float laneY = config.QueueColumns == 1
                ? (config.FunnelYMin + config.FunnelYMax) * 0.5f
                : lowerY + (upperY - lowerY) * pedestrian.QueueLane / (config.QueueColumns - 1f);

            Assert.InRange(MathF.Abs(pedestrian.Position.Y - laneY), 0f, 0.02f);
            Assert.Equal(DetectionStatus.InFunnel, pedestrian.DetectionStatus);
        }
    }

    [Fact]
    public void QueueReleasesPedestriansIntoTheRoom()
    {
        CrowdSimulation simulation = new(new SimulationConfig());

        for (int i = 0; i < 240; i++)
        {
            simulation.Advance(1f / 60f);
        }

        Assert.True(simulation.RoomPopulation > 0);
        Assert.True(simulation.Metrics.TotalCrossings > 0);
    }
}
