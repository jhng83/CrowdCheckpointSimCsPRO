using System.Numerics;

namespace CrowdCheckpointSim.UI;

/// <summary>Calculates proportional UI scaling from the application's reference layout.</summary>
public static class ResponsiveUiScale
{
    public const float ReferenceWidth = 1500f;
    public const float ReferenceHeight = 900f;
    public const float MinimumScale = 0.85f;
    public const float MaximumScale = 4.0f;

    /// <summary>
    /// Scale uniformly by the limiting window dimension so the reference aspect ratio is
    /// preserved and ultrawide/tall displays do not stretch controls independently.
    /// </summary>
    public static float Calculate(Vector2 windowSize)
    {
        if (!float.IsFinite(windowSize.X) ||
            !float.IsFinite(windowSize.Y) ||
            windowSize.X <= 0f ||
            windowSize.Y <= 0f)
        {
            return 1f;
        }

        float widthScale = windowSize.X / ReferenceWidth;
        float heightScale = windowSize.Y / ReferenceHeight;
        return Math.Clamp(Math.Min(widthScale, heightScale), MinimumScale, MaximumScale);
    }
}
