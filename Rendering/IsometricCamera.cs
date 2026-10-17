using System.Numerics;
using CrowdCheckpointSim.Core;

namespace CrowdCheckpointSim.Rendering;

/// <summary>
/// Fixed orthographic 2.5D camera matching the viewing direction of the Python OpenGL reference.
/// It projects 3D world points into the ImGui/OpenGL canvas and can unproject mouse clicks onto
/// the z=0 floor plane for operator placement.
/// </summary>
public sealed class IsometricCamera
{
    public const float MinZoom = 0.60f;
    public const float MaxZoom = 1.80f;
    private const float ZoomFactorPerWheelStep = 1.12f;
    private static readonly Vector3 ReferenceEye = new(24.0f, -13.5f, 16.0f);
    private static readonly Vector3 ReferenceTarget = new(8.55f, 5.0f, 0.5f);
    private static readonly Vector3 WorldUp = Vector3.UnitZ;

    private Vector3 _forward;
    private Vector3 _right;
    private Vector3 _trueUp;
    private Vector2 _canvasCenter;
    private float _cameraCenterX;
    private float _cameraCenterY;
    private float _scale;
    private float _zoom = 1.0f;

    /// <summary>Current view zoom multiplier. This affects rendering only, never simulation physics.</summary>
    public float Zoom => _zoom;

    /// <summary>Set the view zoom directly while keeping it within safe limits.</summary>
    public void SetZoom(float zoom)
    {
        if (!float.IsFinite(zoom))
        {
            return;
        }

        _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
    }

    /// <summary>Adjust view zoom from mouse-wheel input while keeping it within safe limits.</summary>
    public void AdjustZoom(float wheelDelta)
    {
        if (!float.IsFinite(wheelDelta) || Math.Abs(wheelDelta) <= 0.0001f)
        {
            return;
        }

        float factor = MathF.Pow(ZoomFactorPerWheelStep, wheelDelta);
        SetZoom(_zoom * factor);
    }

    /// <summary>Configure camera framing for the current canvas and scenario dimensions.</summary>
    public void Configure(Vector2 canvasMin, Vector2 canvasMax, SimulationConfig config)
    {
        Vector2 size = Vector2.Max(canvasMax - canvasMin, new Vector2(1.0f));
        _canvasCenter = (canvasMin + canvasMax) * 0.5f;

        _forward = Vector3.Normalize(ReferenceTarget - ReferenceEye);
        _right = Vector3.Normalize(Vector3.Cross(_forward, WorldUp));
        _trueUp = Vector3.Normalize(Vector3.Cross(_right, _forward));

        // Reserve framing for the maximum supported 30 m hall length and 20 m width, plus
        // the fixed 10 m orderly-queue funnel. Smaller hall settings intentionally leave some
        // spare screen space so changing geometry does not unexpectedly change camera scale.
        float minX = -0.85f;
        float maxX = SimulationConfig.MaxHallLength + SimulationConfig.BaselineFunnelLength + 0.85f;
        float minY = -0.85f;
        float maxY = SimulationConfig.MaxHallWidth + 0.85f;
        const float minZ = 0.0f;
        const float maxZ = 2.15f;

        float minCameraX = float.PositiveInfinity;
        float maxCameraX = float.NegativeInfinity;
        float minCameraY = float.PositiveInfinity;
        float maxCameraY = float.NegativeInfinity;

        foreach (float x in new[] { minX, maxX })
        {
            foreach (float y in new[] { minY, maxY })
            {
                foreach (float z in new[] { minZ, maxZ })
                {
                    Vector3 relative = new Vector3(x, y, z) - ReferenceEye;
                    float cameraX = Vector3.Dot(relative, _right);
                    float cameraY = Vector3.Dot(relative, _trueUp);
                    minCameraX = Math.Min(minCameraX, cameraX);
                    maxCameraX = Math.Max(maxCameraX, cameraX);
                    minCameraY = Math.Min(minCameraY, cameraY);
                    maxCameraY = Math.Max(maxCameraY, cameraY);
                }
            }
        }

        _cameraCenterX = (minCameraX + maxCameraX) * 0.5f;
        _cameraCenterY = (minCameraY + maxCameraY) * 0.5f;

        float projectedWidth = Math.Max(0.001f, maxCameraX - minCameraX);
        float projectedHeight = Math.Max(0.001f, maxCameraY - minCameraY);
        const float paddingFraction = 0.84f;
        _scale = paddingFraction * Math.Min(size.X / projectedWidth, size.Y / projectedHeight) * _zoom;
    }

    /// <summary>Project one 3D world point to the current canvas.</summary>
    public Vector2 Project(Vector3 world)
    {
        Vector3 relative = world - ReferenceEye;
        float cameraX = Vector3.Dot(relative, _right);
        float cameraY = Vector3.Dot(relative, _trueUp);

        return new Vector2(
            _canvasCenter.X + (cameraX - _cameraCenterX) * _scale,
            _canvasCenter.Y - (cameraY - _cameraCenterY) * _scale);
    }

    /// <summary>
    /// Return camera-space depth. Larger values are farther from the eye and should be painted first.
    /// </summary>
    public float Depth(Vector3 world) => Vector3.Dot(world - ReferenceEye, _forward);

    /// <summary>Project a screen position onto the z=0 floor plane.</summary>
    public bool TryScreenToGround(Vector2 screen, out Vector2 ground)
    {
        ground = default;
        if (_scale <= 0.0001f || Math.Abs(_forward.Z) <= 0.0001f)
        {
            return false;
        }

        float cameraX = _cameraCenterX + (screen.X - _canvasCenter.X) / _scale;
        float cameraY = _cameraCenterY - (screen.Y - _canvasCenter.Y) / _scale;

        Vector3 cameraPlanePoint = ReferenceEye + _right * cameraX + _trueUp * cameraY;
        float t = -cameraPlanePoint.Z / _forward.Z;
        Vector3 worldPoint = cameraPlanePoint + _forward * t;

        if (!float.IsFinite(worldPoint.X) || !float.IsFinite(worldPoint.Y))
        {
            return false;
        }

        ground = new Vector2(worldPoint.X, worldPoint.Y);
        return true;
    }
}
