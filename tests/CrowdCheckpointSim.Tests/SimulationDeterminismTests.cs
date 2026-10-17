using Xunit;
using CrowdCheckpointSim.Core;

namespace CrowdCheckpointSim.Tests;

public sealed class SimulationDeterminismTests
{
    [Fact]
    public void SameSeedProducesSameInitialCrowd()
    {
        CrowdSimulation first = new(new SimulationConfig { RandomSeed = 42 });
        CrowdSimulation second = new(new SimulationConfig { RandomSeed = 42 });

        Assert.Equal(first.Pedestrians.Count, second.Pedestrians.Count);
        for (int i = 0; i < Math.Min(25, first.Pedestrians.Count); i++)
        {
            Assert.Equal(first.Pedestrians[i].Position.X, second.Pedestrians[i].Position.X, 5);
            Assert.Equal(first.Pedestrians[i].Position.Y, second.Pedestrians[i].Position.Y, 5);
            Assert.Equal(first.Pedestrians[i].PreferredSpeed, second.Pedestrians[i].PreferredSpeed, 5);
            Assert.Equal(first.Pedestrians[i].ExitTargetY, second.Pedestrians[i].ExitTargetY, 5);
        }
    }
}
