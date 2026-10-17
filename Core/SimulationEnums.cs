namespace CrowdCheckpointSim.Core;

/// <summary>High-level checkpoint layout/operating mode selected before scenario setup.</summary>
public enum CheckpointMode
{
    /// <summary>Existing open orderly-funnel behaviour.</summary>
    OpenFunnel = 0,

    /// <summary>One-metre single-file lanes with independent timed barricade gates.</summary>
    MultiLaneBarricade = 1,
}

/// <summary>Runtime state of one timed barricade gate in Multi-Lane mode.</summary>
public enum CheckpointGateState
{
    ClosedWaiting = 0,
    Processing = 1,
    Open = 2,

    /// <summary>Traveller failed both automated attempts and is waiting for Purple manual intervention.</summary>
    ManualIntervention = 3,
}

/// <summary>Final automated-processing result planned for a Mode 2 barricade traveller.</summary>
public enum BarricadeOutcome
{
    Pending = 0,
    FirstPassGreen = 1,
    SecondPassYellow = 2,
    ManualRed = 3,
}

/// <summary>
/// Physical movement phase of a pedestrian. Funnel uses orderly queue logic; Room uses SFM.
/// </summary>
public enum PedestrianPhase
{
    Funnel = 0,
    Room = 1,
}

/// <summary>Checkpoint outcome applied once a pedestrian fully crosses the threshold.</summary>
public enum DetectionStatus
{
    InFunnel = 0,
    DetectedAndCounted = 1,
    CountedNotDetected = 2,
    NotCountedNotDetected = 3,

    /// <summary>Special user-selected incident; excluded from normal checkpoint outcome quotas.</summary>
    DasherIncident = 4,
}

/// <summary>Lifecycle of an authorised checkpoint operator.</summary>
public enum OperatorState
{
    IdleAtPost = 0,
    Pursuing = 1,
    Stopping = 2,
    Escorting = 3,
    Verifying = 4,
    Returning = 5,

    /// <summary>Running response toward the single active Dasher.</summary>
    DasherPursuing = 6,

    /// <summary>First catcher is holding the Dasher stationary while awaiting another responder.</summary>
    DasherControlling = 7,

    /// <summary>Exactly two catchers and the Dasher are completing the mandatory one-second stop.</summary>
    DasherGroupStop = 8,

    /// <summary>Catcher is moving with the Dasher toward the lower-left holding point.</summary>
    DasherEscorting = 9,

    /// <summary>Yellow target acquired; Purple is visually searching/confirming before pursuit.</summary>
    ConfirmingYellow = 10,
}

/// <summary>Lifecycle of the optional, user-selected Dasher incident.</summary>
public enum DasherState
{
    None = 0,
    MarkedInFunnel = 1,
    Dashing = 2,
    /// <summary>One Purple operator has caught the Dasher; both remain stopped until reinforcement arrives.</summary>
    SingleOperatorControl = 3,
    GroupStop = 4,
    EscortingToHolding = 5,
    HeldAtHoldingPoint = 6,
}
