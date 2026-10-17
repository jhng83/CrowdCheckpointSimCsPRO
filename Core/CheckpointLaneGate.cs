namespace CrowdCheckpointSim.Core;

/// <summary>
/// Runtime state for one independently operated one-metre checkpoint lane in Multi-Lane
/// Barricade mode. Gate state is simulation state; rendering only visualises these values.
/// </summary>
public sealed class CheckpointLaneGate
{
    public required int LaneIndex { get; init; }
    public required float CentreY { get; init; }
    public CheckpointGateState State { get; internal set; } = CheckpointGateState.ClosedWaiting;
    public int? ActivePedestrianId { get; internal set; }
    public float ProcessingElapsedSeconds { get; internal set; }

    /// <summary>Current automated attempt number. Zero means no traveller is active.</summary>
    public int AttemptNumber { get; internal set; }

    internal void Reset()
    {
        State = CheckpointGateState.ClosedWaiting;
        ActivePedestrianId = null;
        ProcessingElapsedSeconds = 0f;
        AttemptNumber = 0;
    }
}
