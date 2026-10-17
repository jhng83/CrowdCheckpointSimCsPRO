using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class ResultsMetricsTests
{
    [Fact]
    public void QueueMetricsProduceOverallAndPerLaneResults()
    {
        SimulationMetrics metrics = new();
        metrics.Reset(laneCount: 2);
        metrics.TimeSeconds = 120f;

        metrics.RecordQueueCompletion(laneIndex: 0, queueTimeSeconds: 10f);
        metrics.RecordQueueCompletion(laneIndex: 0, queueTimeSeconds: 20f);
        metrics.RecordQueueCompletion(laneIndex: 1, queueTimeSeconds: 30f);

        Assert.Equal(3, metrics.QueueTimeSampleCount);
        Assert.Equal(20f, metrics.AverageQueueTimeSeconds, 3);
        Assert.Equal(30f, metrics.MaximumQueueTimeSeconds, 3);
        Assert.Equal(2, metrics.GetLaneProcessedCount(0));
        Assert.Equal(15f, metrics.GetLaneAverageQueueTimeSeconds(0), 3);
        Assert.Equal(20f, metrics.GetLaneMaximumQueueTimeSeconds(0), 3);
        Assert.Equal(1f, metrics.GetLaneThroughputPerMinute(0), 3);
        Assert.Equal(0.5f, metrics.GetLaneThroughputPerMinute(1), 3);
    }

    [Fact]
    public void OperatorAndManualClearanceMetricsUseTimeWeightedCalculations()
    {
        SimulationMetrics metrics = new();
        metrics.Reset(laneCount: 1);

        metrics.RecordOperatorActivity(activeOperatorCount: 1, operatorCount: 2, elapsedSeconds: 10f);
        metrics.RecordOperatorActivity(activeOperatorCount: 2, operatorCount: 2, elapsedSeconds: 10f);
        metrics.RecordManualClearanceCompletion(waitSeconds: 12f);
        metrics.RecordManualClearanceCompletion(waitSeconds: 18f);

        Assert.Equal(75f, metrics.OperatorUtilisationPercent, 3);
        Assert.Equal(2, metrics.ManualClearanceWaitSampleCount);
        Assert.Equal(15f, metrics.AverageManualClearanceWaitSeconds, 3);
        Assert.Equal(18f, metrics.MaximumManualClearanceWaitSeconds, 3);
    }

    [Fact]
    public void UndetectedCatchRatesUseOnlyResolvedCases()
    {
        SimulationMetrics metrics = new();
        metrics.Reset(laneCount: 1);
        metrics.TotalCaughtYellow = 3;
        metrics.TotalEscapedYellow = 1;
        metrics.CurrentCaughtYellow = 1;
        metrics.CurrentEscapedYellow = 1;

        Assert.Equal(4, metrics.TotalResolvedUndetectedCount);
        Assert.Equal(75f, metrics.TotalUndetectedCatchRatePercent, 3);
        Assert.Equal(2, metrics.CurrentResolvedUndetectedCount);
        Assert.Equal(50f, metrics.CurrentUndetectedCatchRatePercent, 3);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void PreviouslyCaughtUndetectedTravellerIsNotAlsoCountedAsEscaped(
        bool caughtPreviously,
        int expectedEscaped)
    {
        CrowdSimulation simulation = new(new SimulationConfig());
        Pedestrian traveller = simulation.Pedestrians[0];
        traveller.Phase = PedestrianPhase.Room;
        traveller.DetectionStatus = DetectionStatus.CountedNotDetected;
        traveller.UndetectedCatchRecorded = caughtPreviously;
        traveller.Position = new System.Numerics.Vector2(-2f, simulation.Config.RoomHeight * 0.5f);
        traveller.Velocity = System.Numerics.Vector2.Zero;

        simulation.Advance(1f / 60f);

        Assert.Equal(expectedEscaped, simulation.Metrics.TotalEscapedYellow);
    }

    [Fact]
    public void ResetClearsResultMetricsAndReconfiguresLaneCount()
    {
        SimulationMetrics metrics = new();
        metrics.Reset(laneCount: 2);
        metrics.TimeSeconds = 60f;
        metrics.RecordQueueCompletion(laneIndex: 1, queueTimeSeconds: 25f);
        metrics.RecordOperatorActivity(activeOperatorCount: 1, operatorCount: 1, elapsedSeconds: 5f);
        metrics.RecordManualClearanceCompletion(waitSeconds: 20f);

        metrics.Reset(laneCount: 3);

        Assert.Equal(3, metrics.LaneCount);
        Assert.Equal(0, metrics.QueueTimeSampleCount);
        Assert.Equal(0, metrics.ManualClearanceWaitSampleCount);
        Assert.Equal(0f, metrics.OperatorUtilisationPercent, 3);
        Assert.All(Enumerable.Range(0, 3), lane => Assert.Equal(0, metrics.GetLaneProcessedCount(lane)));
    }
}
