using System.Numerics;

namespace CrowdCheckpointSim.Core;

/// <summary>A finite wall segment used by local SFM wall-repulsion calculations.</summary>
public readonly record struct WallSegment(Vector2 A, Vector2 B);

/// <summary>Builds physical boundaries and geometry predicates for the scenario.</summary>
public static class ScenarioGeometry
{
    /// <summary>Build walls while leaving the exit and funnel openings clear.</summary>
    public static IReadOnlyList<WallSegment> BuildWalls(SimulationConfig config)
    {
        float rw = config.RoomWidth;
        float rh = config.RoomHeight;
        float fx0 = config.FunnelXMin;
        float fx1 = config.FunnelXMax;
        float fy0 = config.FunnelYMin;
        float fy1 = config.FunnelYMax;
        float ey0 = config.ExitYMin;
        float ey1 = config.ExitYMax;

        return new[]
        {
            new WallSegment(new Vector2(0, 0), new Vector2(rw, 0)),
            new WallSegment(new Vector2(0, rh), new Vector2(rw, rh)),
            new WallSegment(new Vector2(0, 0), new Vector2(0, ey0)),
            new WallSegment(new Vector2(0, ey1), new Vector2(0, rh)),
            new WallSegment(new Vector2(rw, 0), new Vector2(rw, fy0)),
            new WallSegment(new Vector2(rw, fy1), new Vector2(rw, rh)),
            new WallSegment(new Vector2(fx0, fy0), new Vector2(fx1, fy0)),
            new WallSegment(new Vector2(fx0, fy1), new Vector2(fx1, fy1)),
        };
    }

    /// <summary>Whether a setup click is a safe operator-post location in the room.</summary>
    public static bool IsValidOperatorPost(
        SimulationConfig config,
        Vector2 point,
        IReadOnlyList<Vector2> existing,
        float postRadius = 0.40f)
    {
        float margin = postRadius + 0.08f;
        if (point.X < margin || point.X > config.RoomWidth - margin ||
            point.Y < margin || point.Y > config.RoomHeight - margin)
        {
            return false;
        }

        foreach (Vector2 other in existing)
        {
            if (Vector2.Distance(point, other) < postRadius * 2.2f)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Find the nearest point on a wall segment.</summary>
    public static Vector2 ClosestPoint(WallSegment segment, Vector2 point)
    {
        Vector2 ab = segment.B - segment.A;
        float lengthSquared = ab.LengthSquared();
        if (lengthSquared <= 1e-8f)
        {
            return segment.A;
        }

        float t = Math.Clamp(Vector2.Dot(point - segment.A, ab) / lengthSquared, 0f, 1f);
        return segment.A + ab * t;
    }
}
