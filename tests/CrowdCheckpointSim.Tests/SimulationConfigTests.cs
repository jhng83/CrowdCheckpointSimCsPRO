using Xunit;
using CrowdCheckpointSim.Core;

namespace CrowdCheckpointSim.Tests;

public sealed class SimulationConfigTests
{
    [Fact]
    public void DefaultConfigurationProducesBaselinePopulation()
    {
        SimulationConfig config = new();

        Assert.Equal(20.0f, config.HallLength, 3);
        Assert.Equal(15.0f, config.HallWidth, 3);
        Assert.Equal(3.0f, config.ExitWidth, 3);
        Assert.Equal(10.0f, config.FunnelLength, 3);
        Assert.Equal(330, config.TargetFunnelPopulation);
        Assert.Equal(6.6f, config.InitialFunnelDensity, 3);
        Assert.Equal(6.0f, config.OperatorAcquisitionRadius, 3);
        Assert.Equal(2.0f, config.YellowVisualSearchSeconds, 3);
        Assert.Equal(10.0f, config.InitialManualClearanceSeconds, 3);
        Assert.Equal(10.0f, config.DasherClearanceSeconds, 3);
        Assert.Equal(4.0f, config.YellowPercent, 3);
        Assert.Equal(1.0f, config.RedPercent, 3);
        Assert.Equal(95.0f, config.GreenPercent, 3);
    }

    [Fact]
    public void PopulationUpdatesWithFunnelAreaAndDensity()
    {
        SimulationConfig config = new()
        {
            FunnelWidth = 5.0f,
            TargetFunnelDensity = 5.0f,
        };

        Assert.Equal(250, config.TargetFunnelPopulation);
    }

    [Fact]
    public void MainHallLengthCanBeConfiguredFromTwentyToThirtyMetres()
    {
        SimulationConfig minimum = new() { HallLength = SimulationConfig.MinHallLength };
        SimulationConfig maximum = new() { HallLength = SimulationConfig.MaxHallLength };

        minimum.Validate();
        maximum.Validate();

        Assert.Equal(20.0f, minimum.RoomWidth, 3);
        Assert.Equal(30.0f, maximum.RoomWidth, 3);
        Assert.Equal(10.0f, minimum.FunnelLength, 3);
        Assert.Equal(10.0f, maximum.FunnelLength, 3);

        // Hall length does not control queue population; funnel area/density does.
        Assert.Equal(330, minimum.TargetFunnelPopulation);
        Assert.Equal(330, maximum.TargetFunnelPopulation);
        Assert.Equal(30, minimum.QueueRows);
        Assert.Equal(30, maximum.QueueRows);
    }

    [Theory]
    [InlineData(19.9f)]
    [InlineData(30.1f)]
    public void MainHallLengthOutsideSupportedRangeIsRejected(float length)
    {
        SimulationConfig config = new() { HallLength = length };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Theory]
    [InlineData(15.0f)]
    [InlineData(17.5f)]
    [InlineData(20.0f)]
    public void MainHallWidthCanBeConfiguredFromFifteenToTwentyMetres(float width)
    {
        SimulationConfig config = new() { HallWidth = width };

        config.Validate();

        Assert.Equal(width, config.HallWidth, 3);
        Assert.Equal(width, config.RoomHeight, 3);
    }

    [Theory]
    [InlineData(14.9f)]
    [InlineData(20.1f)]
    public void MainHallWidthOutsideSupportedRangeIsRejected(float width)
    {
        SimulationConfig config = new() { HallWidth = width };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Theory]
    [InlineData(3.0f)]
    [InlineData(6.0f)]
    [InlineData(10.0f)]
    [InlineData(12.0f)]
    public void ExitWidthAcceptsThreeToTwelveMetresWhenHallIsWideEnough(float exitWidth)
    {
        SimulationConfig config = new()
        {
            HallWidth = 15.0f,
            ExitWidth = exitWidth,
        };

        config.Validate();

        Assert.Equal(exitWidth, config.ExitWidth, 3);
    }

    [Theory]
    [InlineData(2.9f)]
    [InlineData(12.1f)]
    public void ExitWidthOutsideThreeToTwelveMetresIsRejected(float exitWidth)
    {
        SimulationConfig config = new()
        {
            HallWidth = 15.0f,
            ExitWidth = exitWidth,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void MaximumExitWidthFitsMinimumSupportedHallWidth()
    {
        SimulationConfig config = new()
        {
            HallWidth = SimulationConfig.MinHallWidth,
            ExitWidth = SimulationConfig.MaxExitWidth,
        };

        config.Validate();
        Assert.True(config.ExitWidth <= config.HallWidth);
    }

    [Theory]
    [InlineData(0.0f, 99.0f)]
    [InlineData(2.0f, 97.0f)]
    [InlineData(4.0f, 95.0f)]
    public void YellowTargetControlsGreenWhileRedStaysAtOnePercent(
        float yellowPercent,
        float expectedGreenPercent)
    {
        SimulationConfig config = new() { YellowPercent = yellowPercent };

        config.Validate();

        Assert.Equal(yellowPercent, config.YellowPercent, 3);
        Assert.Equal(1.0f, config.RedPercent, 3);
        Assert.Equal(expectedGreenPercent, config.GreenPercent, 3);
        Assert.Equal(100.0f, config.GreenPercent + config.YellowPercent + config.RedPercent, 3);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(4.1f)]
    public void YellowTargetOutsideZeroToFourPercentIsRejected(float yellowPercent)
    {
        SimulationConfig config = new() { YellowPercent = yellowPercent };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void FunnelLengthRemainsFixedAtTenMetres()
    {
        SimulationConfig config = new() { FunnelLength = 11.0f };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }
    [Theory]
    [InlineData(4.0f)]
    [InlineData(5.0f)]
    [InlineData(6.0f)]
    [InlineData(10.0f)]
    public void OperatorAcquisitionRadiusAcceptsFourToTenMetres(float radius)
    {
        SimulationConfig config = new() { OperatorAcquisitionRadius = radius };

        config.Validate();

        Assert.Equal(radius, config.OperatorAcquisitionRadius, 3);
    }

    [Theory]
    [InlineData(3.9f)]
    [InlineData(10.1f)]
    public void OperatorAcquisitionRadiusOutsideFourToTenMetresIsRejected(float radius)
    {
        SimulationConfig config = new() { OperatorAcquisitionRadius = radius };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Theory]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    [InlineData(5.0f)]
    public void YellowVisualSearchAcceptsOneToFiveSeconds(float seconds)
    {
        SimulationConfig config = new() { YellowVisualSearchSeconds = seconds };

        config.Validate();

        Assert.Equal(seconds, config.YellowVisualSearchSeconds, 3);
    }

    [Theory]
    [InlineData(0.9f)]
    [InlineData(5.1f)]
    public void YellowVisualSearchOutsideOneToFiveSecondsIsRejected(float seconds)
    {
        SimulationConfig config = new() { YellowVisualSearchSeconds = seconds };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Theory]
    [InlineData(10.0f)]
    [InlineData(20.0f)]
    [InlineData(30.0f)]
    [InlineData(120.0f)]
    [InlineData(180.0f)]
    public void InitialManualClearanceAcceptsTenToOneHundredEightySeconds(float seconds)
    {
        SimulationConfig config = new() { InitialManualClearanceSeconds = seconds };

        config.Validate();

        Assert.Equal(seconds, config.InitialManualClearanceSeconds, 3);
    }

    [Theory]
    [InlineData(9.9f)]
    [InlineData(180.1f)]
    public void InitialManualClearanceOutsideTenToOneHundredEightySecondsIsRejected(float seconds)
    {
        SimulationConfig config = new() { InitialManualClearanceSeconds = seconds };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Theory]
    [InlineData(10.0f)]
    [InlineData(20.0f)]
    [InlineData(30.0f)]
    public void DasherClearanceAcceptsTenToThirtySeconds(float seconds)
    {
        SimulationConfig config = new() { DasherClearanceSeconds = seconds };

        config.Validate();

        Assert.Equal(seconds, config.DasherClearanceSeconds, 3);
    }

    [Theory]
    [InlineData(9.9f)]
    [InlineData(30.1f)]
    public void DasherClearanceOutsideTenToThirtySecondsIsRejected(float seconds)
    {
        SimulationConfig config = new() { DasherClearanceSeconds = seconds };

        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

}
