using System.Numerics;

namespace CrowdCheckpointSim.Core;

/// <summary>
/// Guardrails for deciding when a free checkpoint operator may acquire an Undetected pedestrian.
/// This does not alter pursuit, cooperative-stop, catch, escort, verification, or SFM behaviour.
/// </summary>
public static class OperatorAcquisition
{
    /// <summary>
    /// Returns true when the target is currently inside or exactly on the configured acquisition
    /// radius. The radius is a pre-simulation parameter; this helper does not own the setting.
    /// </summary>
    public static bool IsWithinRadius(
        Vector2 operatorPosition,
        Vector2 targetPosition,
        float radiusMeters)
    {
        if (!float.IsFinite(radiusMeters) || radiusMeters < 0.0f)
        {
            return false;
        }

        Vector2 delta = targetPosition - operatorPosition;
        return delta.LengthSquared() <= radiusMeters * radiusMeters;
    }
}
