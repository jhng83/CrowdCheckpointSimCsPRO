using Xunit;
using CrowdCheckpointSim.Recording;

namespace CrowdCheckpointSim.Tests;

public sealed class RecordingPathPolicyTests
{
    [Theory]
    [InlineData(1500, 1500)]
    [InlineData(1501, 1500)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    public void MakeEvenDimension_ReturnsPositiveEvenValue(int input, int expected)
    {
        Assert.Equal(expected, RecordingPathPolicy.MakeEvenDimension(input));
    }

    [Fact]
    public void BuildFileName_UsesStableTimestampAndExtension()
    {
        DateTime timestamp = new(2026, 8, 16, 18, 4, 5, 123);

        string name = RecordingPathPolicy.BuildFileName(timestamp, "mp4");

        Assert.Equal("CrowdSim_2026-08-16_180405_123.mp4", name);
    }
}
