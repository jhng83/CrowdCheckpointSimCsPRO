namespace CrowdCheckpointSim.Core;

/// <summary>
/// Configurable physical and behavioural settings for the checkpoint simulation.
/// Distances are metres and times are seconds.
/// </summary>
public sealed class SimulationConfig
{
    /// <summary>Minimum configurable main-hall length from exit wall to checkpoint.</summary>
    public const float MinHallLength = 20.0f;

    /// <summary>Maximum configurable main-hall length from exit wall to checkpoint.</summary>
    public const float MaxHallLength = 30.0f;

    /// <summary>Minimum configurable main-hall width perpendicular to the exit/checkpoint axis.</summary>
    public const float MinHallWidth = 15.0f;

    /// <summary>Maximum configurable main-hall width perpendicular to the exit/checkpoint axis.</summary>
    public const float MaxHallWidth = 20.0f;

    /// <summary>Minimum selectable Open Funnel width, in metres.</summary>
    public const float MinFunnelWidth = 5.0f;

    /// <summary>Maximum selectable Open Funnel width, also capped by the current hall width.</summary>
    public const float MaxFunnelWidth = 20.0f;

    /// <summary>Minimum configurable exit width.</summary>
    public const float MinExitWidth = 3.0f;

    /// <summary>Maximum configurable exit width.</summary>
    public const float MaxExitWidth = 12.0f;

    /// <summary>Fixed orderly-queue funnel length retained from the baseline scenario.</summary>
    public const float BaselineFunnelLength = 10.0f;

    /// <summary>Fixed lane width used by Multi-Lane Barricade mode.</summary>
    public const float MultiLaneWidth = 1.0f;

    /// <summary>Minimum normal checkpoint processing time for each Multi-Lane gate.</summary>
    public const float MinLaneClearanceSeconds = 3.0f;

    /// <summary>Maximum normal checkpoint processing time for each Multi-Lane gate.</summary>
    public const float MaxLaneClearanceSeconds = 20.0f;

    /// <summary>Default normal checkpoint processing time for each Multi-Lane gate.</summary>
    public const float DefaultLaneClearanceSeconds = 5.0f;

    /// <summary>Mode 2 first-attempt clearance percentage is intentionally fixed.</summary>
    public const float FixedMultiLaneGreenPercent = 90.0f;

    /// <summary>Minimum percentage that clears on Mode 2's second automated attempt.</summary>
    public const float MinMultiLaneYellowPercent = 0.0f;

    /// <summary>Maximum percentage that clears on Mode 2's second automated attempt.</summary>
    public const float MaxMultiLaneYellowPercent = 10.0f;

    /// <summary>Default Mode 2 split: 90% Green, 5% Yellow, 5% Red.</summary>
    public const float DefaultMultiLaneYellowPercent = 5.0f;

    /// <summary>Minimum Mode 2 Purple manual-clearance duration for a Red traveller.</summary>
    public const float MinMultiLaneManualClearanceSeconds = 10.0f;

    /// <summary>Maximum Mode 2 Purple manual-clearance duration for a Red traveller.</summary>
    public const float MaxMultiLaneManualClearanceSeconds = 180.0f;

    /// <summary>Default Mode 2 Purple manual-clearance duration.</summary>
    public const float DefaultMultiLaneManualClearanceSeconds = 10.0f;

    /// <summary>Maximum setup density value. In Multi-Lane mode this means 100% queue occupancy.</summary>
    public const float MaxSetupDensity = 8.0f;

    /// <summary>Centre-to-centre spacing of a fully occupied single-file Multi-Lane queue.</summary>
    public const float MultiLaneFullQueueSpacing = 0.46f;

    /// <summary>Minimum configurable Yellow / undetected checkpoint target percentage.</summary>
    public const float MinYellowPercent = 0.0f;

    /// <summary>Maximum configurable Yellow / undetected checkpoint target percentage.</summary>
    public const float MaxYellowPercent = 4.0f;

    /// <summary>Red / uncounted checkpoint target percentage is intentionally fixed.</summary>
    public const float FixedRedPercent = 1.0f;

    /// <summary>Minimum selectable operator acquisition radius.</summary>
    public const float MinOperatorAcquisitionRadius = 4.0f;

    /// <summary>Maximum selectable operator acquisition radius.</summary>
    public const float MaxOperatorAcquisitionRadius = 10.0f;

    /// <summary>Baseline/default operator acquisition radius retained from the working build.</summary>
    public const float DefaultOperatorAcquisitionRadius = 6.0f;

    /// <summary>Minimum selectable Yellow visual-search / confirmation delay.</summary>
    public const float MinYellowVisualSearchSeconds = 1.0f;

    /// <summary>Maximum selectable Yellow visual-search / confirmation delay.</summary>
    public const float MaxYellowVisualSearchSeconds = 5.0f;

    /// <summary>Default Yellow visual-search / confirmation delay.</summary>
    public const float DefaultYellowVisualSearchSeconds = 2.0f;

    /// <summary>Minimum selectable initial manual clearance / verification time.</summary>
    public const float MinInitialManualClearanceSeconds = 10.0f;

    /// <summary>Maximum selectable initial manual clearance / verification time.</summary>
    public const float MaxInitialManualClearanceSeconds = 180.0f;

    /// <summary>Fixed wheeled-robot on-site clearance duration range and default, in seconds.</summary>
    public const float MinRobotManualClearanceSeconds = 1.0f;
    public const float MaxRobotManualClearanceSeconds = 180.0f;
    public const float DefaultRobotManualClearanceSeconds = 10.0f;

    /// <summary>Selectable preferred walking speeds remain inside the existing SFM safety cap.</summary>
    public const float MinPassengerPreferredSpeed = 0.20f;
    public const float MaxPassengerPreferredSpeed = 1.65f;
    public const float DefaultPassengerMinimumSpeed = 0.85f;
    public const float DefaultPassengerMaximumSpeed = 1.55f;

    /// <summary>Normal Undetected interception speed range; Dasher pursuit uses its own constants.</summary>
    public const float MinOperatorInterceptionSpeed = 0.50f;
    public const float MaxOperatorInterceptionSpeed = 3.50f;
    public const float DefaultOperatorInterceptionSpeed = 1.55f;

    /// <summary>Clearance fatigue adds this percentage of the initial duration per completed case.</summary>
    public const float DefaultManualClearanceFatigueIncreasePercent = 25.0f;
    public const float MaxManualClearanceFatigueIncreasePercent = 100.0f;

    /// <summary>Optional speed loss is measured in percentage points of initial interception speed.</summary>
    public const float MaxOperatorInterceptionSpeedLossPercent = 50.0f;

    /// <summary>Minimum configurable Dasher clearance time at the holding point.</summary>
    public const float MinDasherClearanceSeconds = 10.0f;

    /// <summary>Maximum configurable Dasher clearance time at the holding point.</summary>
    public const float MaxDasherClearanceSeconds = 30.0f;

    /// <summary>Checkpoint operating mode selected before scenario setup.</summary>
    public CheckpointMode Mode { get; set; } = CheckpointMode.OpenFunnel;

    /// <summary>Normal per-attempt processing time for Multi-Lane barricade gates.</summary>
    public float LaneClearanceSeconds { get; set; } = DefaultLaneClearanceSeconds;

    /// <summary>Mode 2 percentage that fails attempt one but clears on attempt two.</summary>
    public float MultiLaneYellowPercent { get; set; } = DefaultMultiLaneYellowPercent;

    /// <summary>Mode 2 fixed first-attempt success percentage.</summary>
    public float MultiLaneGreenPercent => FixedMultiLaneGreenPercent;

    /// <summary>Mode 2 percentage that still fails after attempt two and requires Purple manual clearance.</summary>
    public float MultiLaneRedPercent => 100.0f - FixedMultiLaneGreenPercent - MultiLaneYellowPercent;

    /// <summary>Mode 2 manual-clearance time at a Purple post. Fatigue is disabled in this mode.</summary>
    public float MultiLaneManualClearanceSeconds { get; set; } = DefaultMultiLaneManualClearanceSeconds;

    private float _hallLength = MinHallLength;

    /// <summary>Main hall length from the exit wall (x=0) to the checkpoint/funnel interface.</summary>
    public float HallLength
    {
        get => _hallLength;
        set => _hallLength = value;
    }

    /// <summary>Compatibility alias used by the existing geometry/simulation code.</summary>
    public float RoomWidth
    {
        get => _hallLength;
        set => _hallLength = value;
    }

    private float _hallWidth = MinHallWidth;

    /// <summary>Main hall width perpendicular to the exit-to-checkpoint direction.</summary>
    public float HallWidth
    {
        get => _hallWidth;
        set => _hallWidth = value;
    }

    /// <summary>Compatibility alias used by the existing geometry/simulation code.</summary>
    public float RoomHeight
    {
        get => _hallWidth;
        set => _hallWidth = value;
    }

    public float FunnelLength { get; set; } = BaselineFunnelLength;
    public float FunnelWidth { get; set; } = 5.0f;
    public float ExitWidth { get; set; } = 3.0f;
    public float TargetFunnelDensity { get; set; } = 6.6f;

    /// <summary>Current physical distance within which a free operator may acquire an Undetected pedestrian.</summary>
    public float OperatorAcquisitionRadius { get; set; } = DefaultOperatorAcquisitionRadius;

    /// <summary>
    /// Time a Purple operator remains stationary after acquiring a Yellow target while visually
    /// searching / confirming it. This delay applies only to normal Yellow intervention.
    /// </summary>
    public float YellowVisualSearchSeconds { get; set; } = DefaultYellowVisualSearchSeconds;

    /// <summary>Manual clearance / verification duration for an operator's first completed case.</summary>
    public float InitialManualClearanceSeconds { get; set; } = MinInitialManualClearanceSeconds;

    /// <summary>Wheeled-robot on-site clearance duration; completed cases never change this value.</summary>
    public float RobotManualClearanceSeconds { get; set; } = DefaultRobotManualClearanceSeconds;

    /// <summary>Lower bound for passengers' sampled free-flow preferred speeds, in metres/second.</summary>
    public float PassengerMinimumSpeed { get; set; } = DefaultPassengerMinimumSpeed;

    /// <summary>Upper bound for preferred speeds. Queuing, yielding and SFM may slow or stop passengers.</summary>
    public float PassengerMaximumSpeed { get; set; } = DefaultPassengerMaximumSpeed;

    /// <summary>Initial normal Undetected pursuit speed. Escort, return and Dasher speeds are separate.</summary>
    public float OperatorInterceptionSpeed { get; set; } = DefaultOperatorInterceptionSpeed;

    /// <summary>Percentage of initial clearance time added after each completed manual case in Mode 1.</summary>
    public float ManualClearanceFatigueIncreasePercent { get; set; } = DefaultManualClearanceFatigueIncreasePercent;

    /// <summary>Percent of initial pursuit speed lost after each normal catch; zero disables speed fatigue.</summary>
    public float OperatorInterceptionSpeedLossPercent { get; set; } = 0.0f;

    /// <summary>Time a secured Dasher remains at the holding point before being cleared/removed.</summary>
    public float DasherClearanceSeconds { get; set; } = MinDasherClearanceSeconds;

    /// <summary>Yellow / counted-but-undetected target percentage for each 60-second interval.</summary>
    public float YellowPercent { get; set; } = 4.0f;

    /// <summary>Red / uncounted target percentage; fixed at one percent.</summary>
    public float RedPercent => FixedRedPercent;

    /// <summary>Green target percentage derived so Green + Yellow + Red always equals 100%.</summary>
    public float GreenPercent => 100.0f - YellowPercent - RedPercent;

    public float YellowFraction => YellowPercent / 100.0f;
    public float RedFraction => RedPercent / 100.0f;
    public float GreenFraction => GreenPercent / 100.0f;

    public int RandomSeed { get; set; } = 42;

    public float AgentRadius { get; init; } = 0.165f;
    public float OperatorRadius { get; init; } = 0.20f;
    public float QueueWallClearance { get; init; } = 0.015f;

    public float FunnelXMin => RoomWidth;
    public float FunnelXMax => RoomWidth + FunnelLength;
    public float FunnelYMin => (RoomHeight - FunnelWidth) * 0.5f;
    public float FunnelYMax => FunnelYMin + FunnelWidth;
    public float ExitYMin => (RoomHeight - ExitWidth) * 0.5f;
    public float ExitYMax => ExitYMin + ExitWidth;
    public float FunnelArea => FunnelLength * FunnelWidth;
    public float InitialFunnelDensity => TargetFunnelPopulation / FunnelArea;

    /// <summary>Number of fixed one-metre lanes in Multi-Lane mode.</summary>
    public int MultiLaneCount => Math.Clamp(
        (int)MathF.Round(HallWidth / MultiLaneWidth),
        (int)MinHallWidth,
        (int)MaxHallWidth);

    /// <summary>Maximum number of near-back-to-front travellers that fit in one Multi-Lane queue.</summary>
    public int MultiLaneCapacityPerLane
    {
        get
        {
            float frontX = FunnelXMin + AgentRadius + 0.17f;
            float rearX = FunnelXMax - AgentRadius - QueueWallClearance;
            float usableLength = Math.Max(0f, rearX - frontX);
            return Math.Max(1, (int)MathF.Floor(usableLength / MultiLaneFullQueueSpacing) + 1);
        }
    }

    /// <summary>Total single-file queue capacity when all Multi-Lane lanes are full.</summary>
    public int MultiLaneTotalCapacity => MultiLaneCount * MultiLaneCapacityPerLane;

    /// <summary>
    /// Front-to-back queue rows. Mode 1 retains the original ~3 rows/m behaviour; Mode 2 uses
    /// the fixed single-file capacity of each one-metre lane.
    /// </summary>
    public int QueueRows => Mode == CheckpointMode.MultiLaneBarricade
        ? MultiLaneCapacityPerLane
        : Math.Max(1, (int)MathF.Round(FunnelLength * 3.0f));

    /// <summary>Lateral queue lane count for the selected checkpoint mode.</summary>
    public int QueueColumns => Mode == CheckpointMode.MultiLaneBarricade
        ? MultiLaneCount
        : Math.Max(1, (TargetFunnelPopulation + QueueRows - 1) / QueueRows);

    /// <summary>
    /// Initial funnel population. Mode 1 uses physical area density. In Multi-Lane mode the same
    /// density slider becomes an occupancy control: the maximum value fills every lane from gate
    /// to rear, while lower values create shorter, uneven single-file queues.
    /// </summary>
    public int TargetFunnelPopulation
    {
        get
        {
            if (Mode != CheckpointMode.MultiLaneBarricade)
            {
                return Math.Max(1, (int)MathF.Floor(TargetFunnelDensity * FunnelArea + 0.5f));
            }

            float occupancy = Math.Clamp(TargetFunnelDensity / MaxSetupDensity, 0f, 1f);
            int population = (int)MathF.Round(MultiLaneTotalCapacity * occupancy);
            return Math.Clamp(population, MultiLaneCount, MultiLaneTotalCapacity);
        }
    }

    /// <summary>Create a mutable copy suitable for setup UI editing.</summary>
    public SimulationConfig Clone() => new()
    {
        Mode = Mode,
        LaneClearanceSeconds = LaneClearanceSeconds,
        MultiLaneYellowPercent = MultiLaneYellowPercent,
        MultiLaneManualClearanceSeconds = MultiLaneManualClearanceSeconds,
        HallLength = HallLength,
        HallWidth = HallWidth,
        FunnelLength = FunnelLength,
        FunnelWidth = FunnelWidth,
        ExitWidth = ExitWidth,
        TargetFunnelDensity = TargetFunnelDensity,
        OperatorAcquisitionRadius = OperatorAcquisitionRadius,
        YellowVisualSearchSeconds = YellowVisualSearchSeconds,
        InitialManualClearanceSeconds = InitialManualClearanceSeconds,
        RobotManualClearanceSeconds = RobotManualClearanceSeconds,
        PassengerMinimumSpeed = PassengerMinimumSpeed,
        PassengerMaximumSpeed = PassengerMaximumSpeed,
        OperatorInterceptionSpeed = OperatorInterceptionSpeed,
        ManualClearanceFatigueIncreasePercent = ManualClearanceFatigueIncreasePercent,
        OperatorInterceptionSpeedLossPercent = OperatorInterceptionSpeedLossPercent,
        DasherClearanceSeconds = DasherClearanceSeconds,
        YellowPercent = YellowPercent,
        RandomSeed = RandomSeed,
        AgentRadius = AgentRadius,
        OperatorRadius = OperatorRadius,
        QueueWallClearance = QueueWallClearance,
    };

    private static void ValidateFiniteRange(float value, float minimum, float maximum, string name)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(name, $"Value must be finite and between {minimum} and {maximum}.");
        }
    }

    /// <summary>Validate values and throw a descriptive exception on invalid input.</summary>
    public void Validate()
    {
        ValidateFiniteRange(RobotManualClearanceSeconds, MinRobotManualClearanceSeconds,
            MaxRobotManualClearanceSeconds, nameof(RobotManualClearanceSeconds));
        ValidateFiniteRange(PassengerMinimumSpeed, MinPassengerPreferredSpeed, MaxPassengerPreferredSpeed, nameof(PassengerMinimumSpeed));
        ValidateFiniteRange(PassengerMaximumSpeed, MinPassengerPreferredSpeed, MaxPassengerPreferredSpeed, nameof(PassengerMaximumSpeed));
        if (PassengerMinimumSpeed > PassengerMaximumSpeed)
        {
            throw new ArgumentOutOfRangeException(nameof(PassengerMinimumSpeed), "Passenger minimum speed cannot exceed maximum speed.");
        }
        ValidateFiniteRange(OperatorInterceptionSpeed, MinOperatorInterceptionSpeed, MaxOperatorInterceptionSpeed, nameof(OperatorInterceptionSpeed));
        ValidateFiniteRange(ManualClearanceFatigueIncreasePercent, 0f, MaxManualClearanceFatigueIncreasePercent, nameof(ManualClearanceFatigueIncreasePercent));
        ValidateFiniteRange(OperatorInterceptionSpeedLossPercent, 0f, MaxOperatorInterceptionSpeedLossPercent, nameof(OperatorInterceptionSpeedLossPercent));
        if (HallLength < MinHallLength || HallLength > MaxHallLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(HallLength),
                $"Main hall length must be between {MinHallLength:F1} m and {MaxHallLength:F1} m.");
        }

        if (HallWidth < MinHallWidth || HallWidth > MaxHallWidth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(HallWidth),
                $"Main hall width must be between {MinHallWidth:F1} m and {MaxHallWidth:F1} m.");
        }

        if (Mode == CheckpointMode.MultiLaneBarricade)
        {
            float roundedHallWidth = MathF.Round(HallWidth);
            if (Math.Abs(HallWidth - roundedHallWidth) > 0.001f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(HallWidth),
                    "Multi-Lane Barricade mode requires a whole-metre hall width so every lane remains exactly 1.0 m wide.");
            }

            if (Math.Abs(FunnelWidth - HallWidth) > 0.001f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(FunnelWidth),
                    "Multi-Lane Barricade mode requires the checkpoint funnel to span the full hall width.");
            }
        }

        if (Math.Abs(FunnelLength - BaselineFunnelLength) > 0.001f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(FunnelLength),
                $"Funnel length is fixed at {BaselineFunnelLength:F1} m in this version.");
        }

        ValidateFiniteRange(FunnelWidth, MinFunnelWidth, MaxFunnelWidth, nameof(FunnelWidth));
        if (FunnelWidth > HallWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(FunnelWidth), "Funnel width must fit inside the main hall width.");
        }

        if (LaneClearanceSeconds < MinLaneClearanceSeconds ||
            LaneClearanceSeconds > MaxLaneClearanceSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(LaneClearanceSeconds),
                $"Lane checkpoint clearance time must be between {MinLaneClearanceSeconds:F1} s and {MaxLaneClearanceSeconds:F1} s.");
        }

        if (MultiLaneYellowPercent < MinMultiLaneYellowPercent ||
            MultiLaneYellowPercent > MaxMultiLaneYellowPercent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MultiLaneYellowPercent),
                $"Mode 2 second-attempt Yellow percentage must be between {MinMultiLaneYellowPercent:F1}% and {MaxMultiLaneYellowPercent:F1}%.");
        }

        if (Math.Abs(MultiLaneGreenPercent + MultiLaneYellowPercent + MultiLaneRedPercent - 100.0f) > 0.001f ||
            Math.Abs(MultiLaneGreenPercent - FixedMultiLaneGreenPercent) > 0.001f)
        {
            throw new InvalidOperationException("Mode 2 outcomes must remain 90% Green with Yellow + Red equal to the remaining 10%.");
        }

        if (!float.IsFinite(MultiLaneManualClearanceSeconds) ||
            MultiLaneManualClearanceSeconds < MinMultiLaneManualClearanceSeconds ||
            MultiLaneManualClearanceSeconds > MaxMultiLaneManualClearanceSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MultiLaneManualClearanceSeconds),
                $"Mode 2 manual clearance time must be between {MinMultiLaneManualClearanceSeconds:F1} s and {MaxMultiLaneManualClearanceSeconds:F1} s.");
        }

        if (ExitWidth < MinExitWidth || ExitWidth > MaxExitWidth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ExitWidth),
                $"Exit width must be between {MinExitWidth:F1} m and {MaxExitWidth:F1} m.");
        }

        if (ExitWidth > HallWidth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ExitWidth),
                "Exit width cannot be greater than the current main hall width.");
        }

        if (TargetFunnelDensity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TargetFunnelDensity), "Density must be positive.");
        }

        if (OperatorAcquisitionRadius < MinOperatorAcquisitionRadius ||
            OperatorAcquisitionRadius > MaxOperatorAcquisitionRadius)
        {
            throw new ArgumentOutOfRangeException(
                nameof(OperatorAcquisitionRadius),
                $"Operator acquisition radius must be between {MinOperatorAcquisitionRadius:F1} m and {MaxOperatorAcquisitionRadius:F1} m.");
        }

        if (YellowVisualSearchSeconds < MinYellowVisualSearchSeconds ||
            YellowVisualSearchSeconds > MaxYellowVisualSearchSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(YellowVisualSearchSeconds),
                $"Yellow visual-search / confirmation time must be between {MinYellowVisualSearchSeconds:F1} s and {MaxYellowVisualSearchSeconds:F1} s.");
        }

        if (!float.IsFinite(InitialManualClearanceSeconds) ||
            InitialManualClearanceSeconds < MinInitialManualClearanceSeconds ||
            InitialManualClearanceSeconds > MaxInitialManualClearanceSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(InitialManualClearanceSeconds),
                $"Initial manual clearance time must be between {MinInitialManualClearanceSeconds:F1} s and {MaxInitialManualClearanceSeconds:F1} s.");
        }

        if (DasherClearanceSeconds < MinDasherClearanceSeconds ||
            DasherClearanceSeconds > MaxDasherClearanceSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DasherClearanceSeconds),
                $"Dasher clearance time must be between {MinDasherClearanceSeconds:F1} s and {MaxDasherClearanceSeconds:F1} s.");
        }

        if (YellowPercent < MinYellowPercent || YellowPercent > MaxYellowPercent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(YellowPercent),
                $"Undetected target percentage must be between {MinYellowPercent:F1}% and {MaxYellowPercent:F1}%.");
        }

        if (Math.Abs(GreenPercent + YellowPercent + RedPercent - 100.0f) > 0.001f)
        {
            throw new InvalidOperationException("Checkpoint outcome percentages must sum to 100%.");
        }

        if (AgentRadius <= 0 || OperatorRadius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(AgentRadius), "Agent radii must be positive.");
        }

        if (QueueWallClearance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(QueueWallClearance), "Queue wall clearance cannot be negative.");
        }

        if (QueueRows * QueueColumns < TargetFunnelPopulation)
        {
            throw new InvalidOperationException("Queue lattice does not contain enough slots for the target population.");
        }

        if (Mode == CheckpointMode.OpenFunnel)
        {
            float usableWidth = FunnelWidth - 2f * (AgentRadius + QueueWallClearance);
            if (QueueColumns > 1)
            {
                float laneSpacing = usableWidth / (QueueColumns - 1);
                if (laneSpacing < AgentRadius * 2f)
                {
                    throw new InvalidOperationException(
                        "Selected funnel width/density produces queue lanes narrower than pedestrian body diameter.");
                }
            }
        }
    }
}
