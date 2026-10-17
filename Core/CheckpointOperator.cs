using System.Numerics;

namespace CrowdCheckpointSim.Core;

/// <summary>Mutable state for a human Purple operator or a wheeled robot responder.</summary>
public sealed class CheckpointOperator
{
    public required int Id { get; init; }
    public ResponderKind Kind { get; init; } = ResponderKind.PurpleOperator;

    /// <summary>Robots share operator rules but skip normal visual confirmation.</summary>
    public bool IsRobot => Kind == ResponderKind.WheeledRobot;

    /// <summary>Captured robot position during fixed on-site manual clearance.</summary>
    public Vector2? OnSiteRobotPosition { get; set; }

    /// <summary>Captured passenger position; on-site clearance never teleports them to a dock.</summary>
    public Vector2? OnSitePassengerPosition { get; set; }

    /// <summary>One acquisition check at the clearance location before starting the return trip.</summary>
    public bool CheckForNextTargetAfterClearance { get; set; }

    /// <summary>Unique label for world and dashboard identification.</summary>
    public string DisplayName => $"{(IsRobot ? "ROBOT" : "OPERATOR")} {Id:00}";
    public Vector2 Position { get; set; }
    public Vector2 HomePosition { get; init; }
    public Vector2 Velocity { get; set; }
    public OperatorState State { get; set; } = OperatorState.IdleAtPost;
    public int? TargetPedestrianId { get; set; }
    public bool TargetHasStopped { get; set; }
    public float StopElapsedSeconds { get; set; }
    public float VerificationElapsedSeconds { get; set; }
    public float TargetConfirmationElapsedSeconds { get; set; }
    public int CatchCount { get; set; }
    public int CompletedVerificationCount { get; set; }

    /// <summary>
    /// Retained as an incident-provenance flag for a return that followed Dasher response.
    /// Right-of-way itself is now general: every non-idle Purple away from its own post has it.
    /// </summary>
    public bool DasherReturnRightOfWay { get; set; }

    /// <summary>
    /// Purple operators have right-of-way whenever they are actively away from their own post.
    /// Ordinary passengers are the yielding party until the operator returns within post radius.
    /// </summary>
    public bool HasRightOfWay
    {
        get
        {
            if (State == OperatorState.IdleAtPost)
            {
                return false;
            }

            float radiusSquared = OperatorTiming.PostRadius * OperatorTiming.PostRadius;
            return Vector2.DistanceSquared(Position, HomePosition) > radiusSquared;
        }
    }

    /// <summary>Configured duration for this operator's first/manual clearance.</summary>
    public float InitialVerificationSeconds { get; init; } = SimulationConfig.MinInitialManualClearanceSeconds;

    /// <summary>Human Mode 1 clearance uses fatigue; robot clearance and Mode 2 remain constant.</summary>
    public bool UseVerificationFatigue { get; init; } = true;

    /// <summary>Percentage of initial clearance time added per completed manual case.</summary>
    public float VerificationFatigueIncreasePercent { get; init; } = SimulationConfig.DefaultManualClearanceFatigueIncreasePercent;

    /// <summary>Initial pursuit speed for normal Undetected intervention, in metres/second.</summary>
    public float InitialInterceptionSpeed { get; init; } = SimulationConfig.DefaultOperatorInterceptionSpeed;

    /// <summary>Percent of initial pursuit speed lost per normal catch; zero preserves baseline speed.</summary>
    public float InterceptionSpeedLossPercent { get; init; }

    /// <summary>Speed used only for normal pursuit; Dasher, escort and return states keep separate speeds.</summary>
    public float CurrentInterceptionSpeed => OperatorTiming.InterceptionSpeed(
        InitialInterceptionSpeed, CatchCount, InterceptionSpeedLossPercent);

    /// <summary>Current verification duration, with fatigue only when enabled for the selected mode.</summary>
    public float CurrentVerificationDuration => UseVerificationFatigue
        ? OperatorTiming.VerificationDuration(InitialVerificationSeconds, CompletedVerificationCount, VerificationFatigueIncreasePercent)
        : InitialVerificationSeconds;

    /// <summary>Human workload bar. Robot clearance and Mode 2 do not use timing fatigue.</summary>
    public float HealthFraction => UseVerificationFatigue
        ? OperatorTiming.HealthFraction(CompletedVerificationCount)
        : 1.0f;

    /// <summary>Convenience percentage for UI labels.</summary>
    public int HealthPercent => (int)MathF.Round(HealthFraction * 100f);
}
