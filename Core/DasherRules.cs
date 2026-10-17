using System.Numerics;

namespace CrowdCheckpointSim.Core;

/// <summary>
/// Central guardrails for the single optional Dasher incident. These values are intentionally
/// isolated from the normal Undetected intervention state machine so the baseline behaviour stays
/// unchanged.
/// </summary>
public static class DasherRules
{
    /// <summary>Running speed used by an active Dasher in the main hall.</summary>
    public const float DashSpeed = 2.50f;

    /// <summary>Running speed used by Purple operators while pursuing an active Dasher.</summary>
    public const float OperatorPursuitSpeed = 3.50f;

    /// <summary>Dasher speed after exactly one operator has caught it; the pair must hold position.</summary>
    public const float SingleCatchHoldSpeed = 0.00f;

    /// <summary>Walking speed for the two-operator escort to the lower-left holding area.</summary>
    public const float GroupEscortSpeed = 1.10f;

    /// <summary>Physical distance at which a pursuing operator counts as having caught the Dasher.</summary>
    public const float CatchDistance = 0.50f;

    /// <summary>Exactly two physical catchers form the secured group that diverts to holding.</summary>
    public const int RequiredCatchers = 2;

    /// <summary>Mandatory full stop after exactly two operators have caught the Dasher.</summary>
    public const float GroupStopSeconds = 1.00f;

    // The holding-point clearance duration is scenario-configurable through
    // SimulationConfig.DasherClearanceSeconds (10-30 seconds).

    /// <summary>Preferred holding-point distance from the exit/left wall.</summary>
    public const float HoldingPointX = 6.00f;

    /// <summary>Preferred holding-point distance from the bottom wall.</summary>
    public const float HoldingPointY = 0.65f;

    /// <summary>How far ahead ordinary passengers begin yielding to a moving Dasher.</summary>
    public const float YieldInfluenceDistance = 2.60f;

    /// <summary>Half width of the Dasher's right-of-way corridor.</summary>
    public const float YieldCorridorHalfWidth = 0.80f;

    /// <summary>Lateral sidestep request applied to ordinary passengers in the Dasher corridor.</summary>
    public const float YieldSideStepDistance = 0.62f;

    /// <summary>
    /// Returns the lower-left interior holding point indicated by the approved UI reference.
    /// The target is approximately 4.0 m from the exit/left wall and 0.65 m from the bottom wall,
    /// while still respecting the pedestrian wall-clearance guardrail.
    /// </summary>
    public static Vector2 HoldingPoint(SimulationConfig config)
    {
        float safeMargin = config.AgentRadius + 0.45f;
        float x = Math.Clamp(
            HoldingPointX,
            safeMargin,
            config.RoomWidth - safeMargin);
        float y = Math.Clamp(
            HoldingPointY,
            safeMargin,
            config.RoomHeight - safeMargin);
        return new Vector2(x, y);
    }
}
