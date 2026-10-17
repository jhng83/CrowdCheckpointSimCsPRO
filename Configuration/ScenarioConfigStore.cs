using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrowdCheckpointSim.Core;

namespace CrowdCheckpointSim.Configuration;

/// <summary>
/// Persists reusable scenario setup files beside the application. A saved configuration contains
/// the physical/setup values plus operator-post locations; runtime state and metrics are not saved.
/// </summary>
public sealed class ScenarioConfigStore
{
    public const int CurrentFormatVersion = 1;
    private const string FileExtension = ".json";
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public ScenarioConfigStore(string? directoryPath = null)
    {
        DirectoryPath = directoryPath ?? Path.Combine(ResolveApplicationDirectory(), "configs");
    }

    private static string ResolveApplicationDirectory()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string baseDirectory = AppContext.BaseDirectory;
        string binSegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        bool launchedFromBuildOutput =
            baseDirectory.Contains(binSegment, StringComparison.OrdinalIgnoreCase) &&
            File.Exists(Path.Combine(currentDirectory, "CrowdCheckpointSim.csproj"));

        return launchedFromBuildOutput ? currentDirectory : baseDirectory;
    }

    /// <summary>Directory used for saved setup files.</summary>
    public string DirectoryPath { get; }

    /// <summary>Save the current setup and return the full path written.</summary>
    public string Save(
        string displayName,
        SimulationConfig config,
        IReadOnlyList<Vector2> operatorPosts)
    {
        ArgumentNullException.ThrowIfNull(operatorPosts);
        return Save(displayName, config, operatorPosts
            .Select(position => new ResponderPost(position, ResponderKind.PurpleOperator))
            .ToArray());
    }

    /// <summary>Save human/robot post identities without changing the legacy human-only API.</summary>
    public string Save(
        string displayName,
        SimulationConfig config,
        IReadOnlyList<ResponderPost> operatorPosts)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(operatorPosts);
        if (operatorPosts.Any(post => !Enum.IsDefined(post.Kind)))
        {
            throw new ArgumentOutOfRangeException(nameof(operatorPosts), "Unknown responder type.");
        }

        string normalizedDisplayName = NormalizeDisplayName(displayName);
        string safeFileStem = MakeSafeFileStem(normalizedDisplayName);
        string fileName = safeFileStem + FileExtension;

        Directory.CreateDirectory(DirectoryPath);
        string fullPath = Path.Combine(DirectoryPath, fileName);

        ScenarioConfigDocument document = ScenarioConfigDocument.From(
            normalizedDisplayName,
            config,
            operatorPosts);
        string json = JsonSerializer.Serialize(document, _jsonOptions);
        File.WriteAllText(fullPath, json);
        return fullPath;
    }

    /// <summary>Load one configuration previously returned by <see cref="List"/>.</summary>
    public LoadedScenarioConfig Load(string fileName)
    {
        string safeFileName = ValidateFileName(fileName);
        string fullPath = Path.Combine(DirectoryPath, safeFileName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Saved configuration could not be found.", fullPath);
        }

        string json = File.ReadAllText(fullPath);
        ScenarioConfigDocument? document = JsonSerializer.Deserialize<ScenarioConfigDocument>(
            json,
            _jsonOptions);
        if (document is null)
        {
            throw new InvalidDataException("Saved configuration file is empty or invalid.");
        }

        if (document.FormatVersion != CurrentFormatVersion)
        {
            throw new InvalidDataException(
                $"Configuration format {document.FormatVersion} is not supported by this build.");
        }

        if (document.Scenario is null)
        {
            throw new InvalidDataException("Saved configuration does not contain scenario values.");
        }

        SimulationConfig config = document.ToSimulationConfig();
        config.Validate();

        IReadOnlyList<ResponderPost> definitions = (document.OperatorPosts ?? new List<SavedOperatorPost>())
            .Select(static post => new ResponderPost(new Vector2(post.X, post.Y), post.Kind))
            .ToArray();
        if (definitions.Any(post => !Enum.IsDefined(post.Kind)))
        {
            throw new InvalidDataException("Saved configuration contains an unknown responder type.");
        }
        IReadOnlyList<Vector2> posts = definitions.Select(post => post.Position).ToArray();

        return new LoadedScenarioConfig(
            NormalizeDisplayName(document.DisplayName),
            safeFileName,
            config,
            posts)
        {
            ResponderPosts = definitions,
        };
    }

    /// <summary>Return valid saved configurations ordered most recently modified first.</summary>
    public IReadOnlyList<SavedScenarioConfigSummary> List()
    {
        Directory.CreateDirectory(DirectoryPath);
        List<SavedScenarioConfigSummary> results = new();

        foreach (string fullPath in Directory.EnumerateFiles(DirectoryPath, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(fullPath);
                ScenarioConfigDocument? document = JsonSerializer.Deserialize<ScenarioConfigDocument>(
                    json,
                    _jsonOptions);
                if (document is null || document.FormatVersion != CurrentFormatVersion)
                {
                    continue;
                }

                results.Add(new SavedScenarioConfigSummary(
                    NormalizeDisplayName(document.DisplayName),
                    Path.GetFileName(fullPath),
                    File.GetLastWriteTimeUtc(fullPath)));
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or JsonException)
            {
                // One damaged or inaccessible file should not prevent the rest of the saved list
                // from appearing in the UI.
            }
        }

        return results
            .OrderByDescending(static item => item.ModifiedUtc)
            .ThenBy(static item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeDisplayName(string displayName)
    {
        string trimmed = (displayName ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "My Scenario" : trimmed;
    }

    private static string MakeSafeFileStem(string displayName)
    {
        HashSet<char> invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        char[] characters = displayName
            .Select(character => invalidCharacters.Contains(character) || character is '/' or '\\'
                ? '_'
                : character)
            .ToArray();

        string stem = new string(characters).Trim().Trim('.');
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "My Scenario";
        }

        return stem.Length <= 80 ? stem : stem[..80].TrimEnd();
    }

    private static string ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A saved configuration must be selected.", nameof(fileName));
        }

        string leafName = Path.GetFileName(fileName);
        if (!string.Equals(fileName, leafName, StringComparison.Ordinal) ||
            !leafName.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid configuration filename.", nameof(fileName));
        }

        return leafName;
    }

    private sealed class ScenarioConfigDocument
    {
        public int FormatVersion { get; set; } = CurrentFormatVersion;
        public string DisplayName { get; set; } = "My Scenario";
        public DateTime SavedUtc { get; set; } = DateTime.UtcNow;
        public ScenarioValues? Scenario { get; set; } = new();
        public List<SavedOperatorPost>? OperatorPosts { get; set; } = new();

        public static ScenarioConfigDocument From(
            string displayName,
            SimulationConfig config,
            IReadOnlyList<ResponderPost> operatorPosts) => new()
        {
            DisplayName = displayName,
            SavedUtc = DateTime.UtcNow,
            Scenario = ScenarioValues.From(config),
            OperatorPosts = operatorPosts
                .Select(static post => new SavedOperatorPost
                {
                    X = post.Position.X,
                    Y = post.Position.Y,
                    Kind = post.Kind,
                })
                .ToList(),
        };

        public SimulationConfig ToSimulationConfig() =>
            (Scenario ?? throw new InvalidDataException("Saved configuration does not contain scenario values."))
            .ToSimulationConfig();
    }

    private sealed class ScenarioValues
    {
        public CheckpointMode CheckpointMode { get; set; } = CrowdCheckpointSim.Core.CheckpointMode.OpenFunnel;
        public float LaneClearanceSeconds { get; set; } = SimulationConfig.DefaultLaneClearanceSeconds;
        public float MultiLaneYellowPercent { get; set; } = SimulationConfig.DefaultMultiLaneYellowPercent;
        public float MultiLaneManualClearanceSeconds { get; set; } = SimulationConfig.DefaultMultiLaneManualClearanceSeconds;
        public float HallLength { get; set; } = SimulationConfig.MinHallLength;

        /// <summary>Current user-facing main-hall width.</summary>
        public float? HallWidth { get; set; }

        /// <summary>Legacy field retained so configurations saved by earlier builds still load.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? RoomHeight { get; set; }

        public float FunnelLength { get; set; } = SimulationConfig.BaselineFunnelLength;
        public float FunnelWidth { get; set; } = 5f;
        public float ExitWidth { get; set; } = 3f;
        public float TargetFunnelDensity { get; set; } = 6.6f;
        public float YellowPercent { get; set; } = 4f;
        public float OperatorAcquisitionRadius { get; set; } = SimulationConfig.DefaultOperatorAcquisitionRadius;
        public float YellowVisualSearchSeconds { get; set; } = SimulationConfig.DefaultYellowVisualSearchSeconds;
        public float InitialManualClearanceSeconds { get; set; } = SimulationConfig.MinInitialManualClearanceSeconds;
        public float RobotManualClearanceSeconds { get; set; } = SimulationConfig.DefaultRobotManualClearanceSeconds;
        public float PassengerMinimumSpeed { get; set; } = SimulationConfig.DefaultPassengerMinimumSpeed;
        public float PassengerMaximumSpeed { get; set; } = SimulationConfig.DefaultPassengerMaximumSpeed;
        public float OperatorInterceptionSpeed { get; set; } = SimulationConfig.DefaultOperatorInterceptionSpeed;

        /// <summary>Missing legacy values preserve the old fixed 2.5-second fatigue increment.</summary>
        public float? ManualClearanceFatigueIncreasePercent { get; set; }
        public float OperatorInterceptionSpeedLossPercent { get; set; }
        public float DasherClearanceSeconds { get; set; } = SimulationConfig.MinDasherClearanceSeconds;
        public int RandomSeed { get; set; } = 42;
        public float AgentRadius { get; set; } = 0.165f;
        public float OperatorRadius { get; set; } = 0.20f;
        public float QueueWallClearance { get; set; } = 0.015f;

        public static ScenarioValues From(SimulationConfig config) => new()
        {
            CheckpointMode = config.Mode,
            LaneClearanceSeconds = config.LaneClearanceSeconds,
            MultiLaneYellowPercent = config.MultiLaneYellowPercent,
            MultiLaneManualClearanceSeconds = config.MultiLaneManualClearanceSeconds,
            HallLength = config.HallLength,
            HallWidth = config.HallWidth,
            RoomHeight = null,
            FunnelLength = config.FunnelLength,
            FunnelWidth = config.FunnelWidth,
            ExitWidth = config.ExitWidth,
            TargetFunnelDensity = config.TargetFunnelDensity,
            YellowPercent = config.YellowPercent,
            OperatorAcquisitionRadius = config.OperatorAcquisitionRadius,
            YellowVisualSearchSeconds = config.YellowVisualSearchSeconds,
            InitialManualClearanceSeconds = config.InitialManualClearanceSeconds,
            RobotManualClearanceSeconds = config.RobotManualClearanceSeconds,
            PassengerMinimumSpeed = config.PassengerMinimumSpeed,
            PassengerMaximumSpeed = config.PassengerMaximumSpeed,
            OperatorInterceptionSpeed = config.OperatorInterceptionSpeed,
            ManualClearanceFatigueIncreasePercent = config.ManualClearanceFatigueIncreasePercent,
            OperatorInterceptionSpeedLossPercent = config.OperatorInterceptionSpeedLossPercent,
            DasherClearanceSeconds = config.DasherClearanceSeconds,
            RandomSeed = config.RandomSeed,
            AgentRadius = config.AgentRadius,
            OperatorRadius = config.OperatorRadius,
            QueueWallClearance = config.QueueWallClearance,
        };

        public SimulationConfig ToSimulationConfig() => new()
        {
            Mode = CheckpointMode,
            LaneClearanceSeconds = LaneClearanceSeconds,
            MultiLaneYellowPercent = MultiLaneYellowPercent,
            MultiLaneManualClearanceSeconds = MultiLaneManualClearanceSeconds,
            HallLength = HallLength,
            HallWidth = HallWidth ?? RoomHeight ?? SimulationConfig.MinHallWidth,
            FunnelLength = FunnelLength,
            FunnelWidth = FunnelWidth,
            ExitWidth = ExitWidth,
            TargetFunnelDensity = TargetFunnelDensity,
            YellowPercent = YellowPercent,
            OperatorAcquisitionRadius = OperatorAcquisitionRadius,
            YellowVisualSearchSeconds = YellowVisualSearchSeconds,
            InitialManualClearanceSeconds = InitialManualClearanceSeconds,
            RobotManualClearanceSeconds = RobotManualClearanceSeconds,
            PassengerMinimumSpeed = PassengerMinimumSpeed,
            PassengerMaximumSpeed = PassengerMaximumSpeed,
            OperatorInterceptionSpeed = OperatorInterceptionSpeed,
            ManualClearanceFatigueIncreasePercent = ManualClearanceFatigueIncreasePercent ??
                OperatorTiming.VerificationFatigueStepSeconds / InitialManualClearanceSeconds * 100f,
            OperatorInterceptionSpeedLossPercent = OperatorInterceptionSpeedLossPercent,
            DasherClearanceSeconds = DasherClearanceSeconds,
            RandomSeed = RandomSeed,
            AgentRadius = AgentRadius,
            OperatorRadius = OperatorRadius,
            QueueWallClearance = QueueWallClearance,
        };
    }

    private sealed class SavedOperatorPost
    {
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>Older files omit Kind and therefore retain human Purple operators.</summary>
        [JsonConverter(typeof(ResponderKindJsonConverter))]
        public ResponderKind Kind { get; set; } = ResponderKind.PurpleOperator;
    }
}

/// <summary>One entry shown in the saved-configuration picker.</summary>
public sealed record SavedScenarioConfigSummary(
    string DisplayName,
    string FileName,
    DateTime ModifiedUtc);

/// <summary>Validated setup values returned from a saved file.</summary>
public sealed record LoadedScenarioConfig(
    string DisplayName,
    string FileName,
    SimulationConfig Config,
    IReadOnlyList<Vector2> OperatorPosts)
{
    /// <summary>Authoritative typed posts for the UI; legacy coordinate access remains available.</summary>
    public IReadOnlyList<ResponderPost> ResponderPosts { get; init; } = Array.Empty<ResponderPost>();
}
