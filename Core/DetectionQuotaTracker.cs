namespace CrowdCheckpointSim.Core;

/// <summary>
/// Applies a deterministic cumulative quota rule inside each 60-second simulation interval.
/// Uncounted (Red) is fixed at 1%, Undetected (Yellow) is configurable from 0% to 4%,
/// and Cleared (Green) is the corresponding remainder from 99% to 95%.
/// </summary>
public sealed class DetectionQuotaTracker
{
    private float _greenFraction;
    private float _yellowFraction;
    private float _redFraction;

    /// <summary>Create a tracker using the baseline 95% Cleared / 4% Undetected / 1% Uncounted distribution.</summary>
    public DetectionQuotaTracker()
        : this(0.04f, 0.01f)
    {
    }

    /// <summary>Create a tracker using an explicit Yellow fraction and the fixed 1% Red fraction.</summary>
    public DetectionQuotaTracker(float yellowFraction, float redFraction = 0.01f)
    {
        Configure(yellowFraction, redFraction);
    }

    public float GreenFraction => _greenFraction;
    public float YellowFraction => _yellowFraction;
    public float RedFraction => _redFraction;

    public int CrossingCount { get; private set; }
    public int GreenCount { get; private set; }
    public int YellowCount { get; private set; }
    public int RedCount { get; private set; }

    /// <summary>
    /// Update the target distribution and clear the current interval so no old quota debt carries
    /// across a pre-simulation configuration change.
    /// </summary>
    public void Configure(float yellowFraction, float redFraction = 0.01f)
    {
        if (yellowFraction < 0f || yellowFraction > 0.04f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(yellowFraction),
                "Undetected fraction must be between 0.00 and 0.04.");
        }

        if (Math.Abs(redFraction - 0.01f) > 0.00001f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(redFraction),
                "Uncounted fraction is fixed at 0.01 (1%).");
        }

        float greenFraction = 1.0f - yellowFraction - redFraction;
        if (greenFraction < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(yellowFraction), "Outcome fractions exceed 100%.");
        }

        _yellowFraction = yellowFraction;
        _redFraction = redFraction;
        _greenFraction = greenFraction;
        ResetInterval();
    }

    /// <summary>Clear the current 60-second interval counters.</summary>
    public void ResetInterval()
    {
        CrossingCount = 0;
        GreenCount = 0;
        YellowCount = 0;
        RedCount = 0;
    }

    /// <summary>Assign the next crossing to the category with the largest quota deficit.</summary>
    public DetectionStatus AssignNext()
    {
        int next = CrossingCount + 1;
        int targetYellow = (int)MathF.Floor(next * _yellowFraction + 0.5f);
        int targetRed = (int)MathF.Floor(next * _redFraction + 0.5f);
        int targetGreen = next - targetYellow - targetRed;

        int greenDeficit = targetGreen - GreenCount;
        int yellowDeficit = targetYellow - YellowCount;
        int redDeficit = targetRed - RedCount;

        CrossingCount = next;

        if (yellowDeficit > greenDeficit && yellowDeficit >= redDeficit)
        {
            YellowCount++;
            return DetectionStatus.CountedNotDetected;
        }

        if (redDeficit > greenDeficit && redDeficit > yellowDeficit)
        {
            RedCount++;
            return DetectionStatus.NotCountedNotDetected;
        }

        GreenCount++;
        return DetectionStatus.DetectedAndCounted;
    }
}
