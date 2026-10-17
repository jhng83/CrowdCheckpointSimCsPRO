using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class SimulationSpeedTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void RuntimeSpeedAdvancesCorrespondingAmountOfSimulationTime(int multiplier)
    {
        CrowdSimulation simulation = new(new SimulationConfig());

        simulation.Advance(1f / 60f, multiplier);

        Assert.Equal(multiplier / 60f, simulation.Metrics.TimeSeconds, 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void RuntimeSpeedOutsideOneToFourIsRejected(int multiplier)
    {
        CrowdSimulation simulation = new(new SimulationConfig());

        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.Advance(1f / 60f, multiplier));
    }
}
