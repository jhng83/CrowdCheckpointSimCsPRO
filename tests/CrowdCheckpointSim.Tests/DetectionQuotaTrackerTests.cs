using Xunit;
using CrowdCheckpointSim.Core;

namespace CrowdCheckpointSim.Tests;

public sealed class DetectionQuotaTrackerTests
{
    [Fact]
    public void OneHundredCrossingsMatchDefaultThreeOutcomeQuota()
    {
        Dictionary<DetectionStatus, int> counts = CountOutcomes(new DetectionQuotaTracker(), 100);

        Assert.Equal(95, counts.GetValueOrDefault(DetectionStatus.DetectedAndCounted));
        Assert.Equal(4, counts.GetValueOrDefault(DetectionStatus.CountedNotDetected));
        Assert.Equal(1, counts.GetValueOrDefault(DetectionStatus.NotCountedNotDetected));
    }

    [Fact]
    public void ZeroPercentYellowProducesNinetyNineGreenAndOneRed()
    {
        DetectionQuotaTracker tracker = new(0.00f);
        Dictionary<DetectionStatus, int> counts = CountOutcomes(tracker, 100);

        Assert.Equal(99, counts.GetValueOrDefault(DetectionStatus.DetectedAndCounted));
        Assert.Equal(0, counts.GetValueOrDefault(DetectionStatus.CountedNotDetected));
        Assert.Equal(1, counts.GetValueOrDefault(DetectionStatus.NotCountedNotDetected));
    }

    [Fact]
    public void FourPercentYellowProducesNinetyFiveGreenFourYellowAndOneRed()
    {
        DetectionQuotaTracker tracker = new(0.04f);
        Dictionary<DetectionStatus, int> counts = CountOutcomes(tracker, 100);

        Assert.Equal(95, counts.GetValueOrDefault(DetectionStatus.DetectedAndCounted));
        Assert.Equal(4, counts.GetValueOrDefault(DetectionStatus.CountedNotDetected));
        Assert.Equal(1, counts.GetValueOrDefault(DetectionStatus.NotCountedNotDetected));
    }

    [Theory]
    [InlineData(-0.001f)]
    [InlineData(0.041f)]
    public void YellowFractionOutsideZeroToFourPercentIsRejected(float yellowFraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionQuotaTracker(yellowFraction));
    }

    [Theory]
    [InlineData(0.00f)]
    [InlineData(0.02f)]
    public void RedFractionOtherThanOnePercentIsRejected(float redFraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionQuotaTracker(0.04f, redFraction));
    }

    private static Dictionary<DetectionStatus, int> CountOutcomes(
        DetectionQuotaTracker tracker,
        int crossingCount)
    {
        Dictionary<DetectionStatus, int> counts = new();
        for (int i = 0; i < crossingCount; i++)
        {
            DetectionStatus status = tracker.AssignNext();
            counts[status] = counts.GetValueOrDefault(status) + 1;
        }

        return counts;
    }
}
