using System.Numerics;
using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class OperatorAcquisitionTests
{
    [Theory]
    [InlineData(4.0f, 4.0f, true)]
    [InlineData(4.001f, 4.0f, false)]
    [InlineData(5.0f, 5.0f, true)]
    [InlineData(5.001f, 5.0f, false)]
    [InlineData(6.0f, 6.0f, true)]
    [InlineData(6.001f, 6.0f, false)]
    [InlineData(10.0f, 10.0f, true)]
    [InlineData(10.001f, 10.0f, false)]
    public void AcquisitionUsesConfiguredCurrentDistanceGuard(
        float distance,
        float configuredRadius,
        bool expected)
    {
        Vector2 operatorPosition = new(2.0f, 3.0f);
        Vector2 targetPosition = new(2.0f + distance, 3.0f);

        Assert.Equal(
            expected,
            OperatorAcquisition.IsWithinRadius(
                operatorPosition,
                targetPosition,
                configuredRadius));
    }

    [Fact]
    public void InvalidNegativeRadiusCannotAcquire()
    {
        Assert.False(OperatorAcquisition.IsWithinRadius(Vector2.Zero, Vector2.Zero, -1.0f));
    }
}
