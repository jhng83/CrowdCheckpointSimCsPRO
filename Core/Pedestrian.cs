using System.Numerics;

namespace CrowdCheckpointSim.Core;

/// <summary>Mutable state for one unique pedestrian.</summary>
public sealed class Pedestrian
{
    public required int Id { get; init; }

    /// <summary>
    /// Orderly funnel lane assigned at spawn. It remains attached to the pedestrian after the
    /// checkpoint only so the next person in that lane can preserve queue spacing during release.
    /// </summary>
    public required int QueueLane { get; init; }

    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }

    /// <summary>Simulation time at queue entry, used to calculate completed queue waiting time.</summary>
    public float SpawnTimeSeconds { get; init; }
    public float Urgency { get; init; }
    public float PreferredSpeed { get; init; }
    public float ExitTargetY { get; init; }

    /// <summary>Forward speed used only while the pedestrian is in the orderly funnel queue.</summary>
    public float QueueForwardSpeed { get; set; }

    /// <summary>Deterministic individual phase used for small natural queue timing variation.</summary>
    public float QueuePhase { get; init; }

    public PedestrianPhase Phase { get; set; } = PedestrianPhase.Funnel;
    public DetectionStatus DetectionStatus { get; set; } = DetectionStatus.InFunnel;

    /// <summary>Planned Mode 2 automated-processing result. It remains hidden until the relevant attempt completes.</summary>
    public BarricadeOutcome BarricadeOutcome { get; set; } = BarricadeOutcome.Pending;

    /// <summary>True after a Mode 2 traveller fails both automated attempts and still requires Purple manual clearance.</summary>
    public bool RequiresManualClearance { get; set; }

    /// <summary>Prevents Mode 2 checkpoint outcome metrics from being recorded more than once.</summary>
    public bool CheckpointOutcomeRecorded { get; set; }

    /// <summary>Prevents queue and lane performance metrics from being recorded more than once.</summary>
    public bool QueueMetricsRecorded { get; set; }

    /// <summary>
    /// True after this Undetected traveller has been physically caught at least once. This keeps
    /// caught/escaped outcome statistics mutually exclusive if a Dasher pre-empts the operator.
    /// </summary>
    public bool UndetectedCatchRecorded { get; set; }

    /// <summary>
    /// Simulation time at which this traveller became eligible for manual intervention. A null
    /// value means no manual-clearance completion timer is currently active.
    /// </summary>
    public float? ManualClearanceQueuedAtSeconds { get; set; }

    public bool CooperativeStop { get; set; }

    /// <summary>
    /// True when a manual enrolment was already in progress but its Purple operator was
    /// pre-empted by a higher-priority Dasher incident. The passenger remains stopped and
    /// completes only the remaining enrolment time autonomously before becoming Cleared.
    /// </summary>
    public bool AutonomousVerificationActive { get; set; }

    /// <summary>Remaining seconds for an autonomous completion after Dasher pre-emption.</summary>
    public float AutonomousVerificationRemainingSeconds { get; set; }

    /// <summary>Optional Dasher incident state. Normal pedestrians remain <see cref="DasherState.None"/>.</summary>
    public DasherState DasherState { get; set; } = DasherState.None;

    /// <summary>True while this pedestrian is the marked or active Dasher.</summary>
    public bool IsDasher => DasherState != DasherState.None;
}
