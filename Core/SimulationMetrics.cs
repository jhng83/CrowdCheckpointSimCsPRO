namespace CrowdCheckpointSim.Core;

/// <summary>Accumulated and current-60-second metrics exposed to the UI.</summary>
public sealed class SimulationMetrics
{
    private double _totalQueueTimeSeconds;
    private int _queueTimeSampleCount;
    private float _maximumQueueTimeSeconds;
    private double _activeOperatorSeconds;
    private double _availableOperatorSeconds;
    private double _totalManualClearanceWaitSeconds;
    private int _manualClearanceWaitSampleCount;
    private float _maximumManualClearanceWaitSeconds;
    private int[] _laneProcessedCounts = Array.Empty<int>();
    private double[] _laneQueueTimeSeconds = Array.Empty<double>();
    private float[] _laneMaximumQueueTimeSeconds = Array.Empty<float>();

    public float TimeSeconds { get; internal set; }
    public int TotalCrossings { get; internal set; }
    public int TotalGreen { get; internal set; }
    public int TotalYellow { get; internal set; }
    public int TotalRed { get; internal set; }
    public int TotalCaughtYellow { get; internal set; }
    public int TotalEscapedYellow { get; internal set; }
    public int TotalManualVerified { get; internal set; }

    public int CurrentCrossings { get; internal set; }
    public int CurrentGreen { get; internal set; }
    public int CurrentYellow { get; internal set; }
    public int CurrentRed { get; internal set; }
    public int CurrentCaughtYellow { get; internal set; }
    public int CurrentEscapedYellow { get; internal set; }

    public float AverageCrossingsPerMinute =>
        TimeSeconds <= 0.001f ? 0f : TotalCrossings / (TimeSeconds / 60f);

    /// <summary>Checkpoint outcomes resolved per simulation minute.</summary>
    public float AverageOutcomesPerMinute =>
        TimeSeconds <= 0.001f ? 0f : TotalOutcomeCount / (TimeSeconds / 60f);

    /// <summary>Average time from queue entry/spawn until checkpoint processing completes.</summary>
    public float AverageQueueTimeSeconds =>
        _queueTimeSampleCount == 0 ? 0f : (float)(_totalQueueTimeSeconds / _queueTimeSampleCount);

    public float MaximumQueueTimeSeconds => _maximumQueueTimeSeconds;
    public int QueueTimeSampleCount => _queueTimeSampleCount;

    /// <summary>Share of available operator-seconds spent in a non-idle state.</summary>
    public float OperatorUtilisationPercent =>
        _availableOperatorSeconds <= 0.0001
            ? 0f
            : (float)(_activeOperatorSeconds * 100.0 / _availableOperatorSeconds);

    /// <summary>
    /// Average time from an intervention outcome becoming eligible for Purple response until its
    /// manual clearance completes. Escaped or still-pending cases are intentionally excluded.
    /// </summary>
    public float AverageManualClearanceWaitSeconds =>
        _manualClearanceWaitSampleCount == 0
            ? 0f
            : (float)(_totalManualClearanceWaitSeconds / _manualClearanceWaitSampleCount);

    public float MaximumManualClearanceWaitSeconds => _maximumManualClearanceWaitSeconds;
    public int ManualClearanceWaitSampleCount => _manualClearanceWaitSampleCount;
    public int LaneCount => _laneProcessedCounts.Length;

    public int CurrentOutcomeCount => CurrentGreen + CurrentYellow + CurrentRed;
    public int TotalOutcomeCount => TotalGreen + TotalYellow + TotalRed;

    public float CurrentGreenPercent => Percent(CurrentGreen, CurrentOutcomeCount);
    public float CurrentYellowPercent => Percent(CurrentYellow, CurrentOutcomeCount);
    public float CurrentRedPercent => Percent(CurrentRed, CurrentOutcomeCount);
    public float TotalGreenPercent => Percent(TotalGreen, TotalOutcomeCount);
    public float TotalYellowPercent => Percent(TotalYellow, TotalOutcomeCount);
    public float TotalRedPercent => Percent(TotalRed, TotalOutcomeCount);
    public int CurrentResolvedUndetectedCount => CurrentCaughtYellow + CurrentEscapedYellow;
    public int TotalResolvedUndetectedCount => TotalCaughtYellow + TotalEscapedYellow;
    public float CurrentUndetectedCatchRatePercent =>
        Percent(CurrentCaughtYellow, CurrentResolvedUndetectedCount);
    public float TotalUndetectedCatchRatePercent =>
        Percent(TotalCaughtYellow, TotalResolvedUndetectedCount);

    internal void Reset(int laneCount)
    {
        if (laneCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(laneCount));
        }

        TimeSeconds = 0;
        TotalCrossings = 0;
        TotalGreen = 0;
        TotalYellow = 0;
        TotalRed = 0;
        TotalCaughtYellow = 0;
        TotalEscapedYellow = 0;
        TotalManualVerified = 0;
        _totalQueueTimeSeconds = 0;
        _queueTimeSampleCount = 0;
        _maximumQueueTimeSeconds = 0;
        _activeOperatorSeconds = 0;
        _availableOperatorSeconds = 0;
        _totalManualClearanceWaitSeconds = 0;
        _manualClearanceWaitSampleCount = 0;
        _maximumManualClearanceWaitSeconds = 0;
        _laneProcessedCounts = new int[laneCount];
        _laneQueueTimeSeconds = new double[laneCount];
        _laneMaximumQueueTimeSeconds = new float[laneCount];
        ResetCurrentInterval();
    }

    internal void ResetCurrentInterval()
    {
        CurrentCrossings = 0;
        CurrentGreen = 0;
        CurrentYellow = 0;
        CurrentRed = 0;
        CurrentCaughtYellow = 0;
        CurrentEscapedYellow = 0;
    }

    /// <summary>Number of completed checkpoint outcomes recorded for a zero-based lane.</summary>
    public int GetLaneProcessedCount(int laneIndex)
    {
        ValidateLaneIndex(laneIndex);
        return _laneProcessedCounts[laneIndex];
    }

    /// <summary>Average queue time of completed travellers in a zero-based lane.</summary>
    public float GetLaneAverageQueueTimeSeconds(int laneIndex)
    {
        ValidateLaneIndex(laneIndex);
        int count = _laneProcessedCounts[laneIndex];
        return count == 0 ? 0f : (float)(_laneQueueTimeSeconds[laneIndex] / count);
    }

    /// <summary>Maximum queue time observed in a zero-based lane.</summary>
    public float GetLaneMaximumQueueTimeSeconds(int laneIndex)
    {
        ValidateLaneIndex(laneIndex);
        return _laneMaximumQueueTimeSeconds[laneIndex];
    }

    /// <summary>Completed checkpoint outcomes per minute for a zero-based lane.</summary>
    public float GetLaneThroughputPerMinute(int laneIndex)
    {
        ValidateLaneIndex(laneIndex);
        return TimeSeconds <= 0.001f
            ? 0f
            : _laneProcessedCounts[laneIndex] / (TimeSeconds / 60f);
    }

    internal void RecordQueueCompletion(int laneIndex, float queueTimeSeconds)
    {
        ValidateLaneIndex(laneIndex);
        if (!float.IsFinite(queueTimeSeconds) || queueTimeSeconds < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(queueTimeSeconds));
        }

        _totalQueueTimeSeconds += queueTimeSeconds;
        _queueTimeSampleCount++;
        _maximumQueueTimeSeconds = Math.Max(_maximumQueueTimeSeconds, queueTimeSeconds);
        _laneProcessedCounts[laneIndex]++;
        _laneQueueTimeSeconds[laneIndex] += queueTimeSeconds;
        _laneMaximumQueueTimeSeconds[laneIndex] = Math.Max(
            _laneMaximumQueueTimeSeconds[laneIndex],
            queueTimeSeconds);
    }

    internal void RecordOperatorActivity(int activeOperatorCount, int operatorCount, float elapsedSeconds)
    {
        if (operatorCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(operatorCount));
        }

        if (activeOperatorCount < 0 || activeOperatorCount > operatorCount)
        {
            throw new ArgumentOutOfRangeException(nameof(activeOperatorCount));
        }

        if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        }

        _activeOperatorSeconds += activeOperatorCount * elapsedSeconds;
        _availableOperatorSeconds += operatorCount * elapsedSeconds;
    }

    internal void RecordManualClearanceCompletion(float waitSeconds)
    {
        if (!float.IsFinite(waitSeconds) || waitSeconds < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(waitSeconds));
        }

        _totalManualClearanceWaitSeconds += waitSeconds;
        _manualClearanceWaitSampleCount++;
        _maximumManualClearanceWaitSeconds = Math.Max(
            _maximumManualClearanceWaitSeconds,
            waitSeconds);
    }

    private void ValidateLaneIndex(int laneIndex)
    {
        if ((uint)laneIndex >= (uint)_laneProcessedCounts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(laneIndex));
        }
    }

    private static float Percent(int value, int total) => total <= 0 ? 0f : value * 100f / total;
}
