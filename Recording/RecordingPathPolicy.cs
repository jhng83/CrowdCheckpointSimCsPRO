namespace CrowdCheckpointSim.Recording;

/// <summary>Centralises output-path and filename rules for full-UI recordings.</summary>
public static class RecordingPathPolicy
{
    public const string RecordingFolderName = "recordings";

    /// <summary>
    /// Returns the directory in which recordings should be stored.
    /// When launched with dotnet run, recordings are kept in the project root when possible.
    /// Published executables store recordings beside the executable.
    /// </summary>
    public static string ResolveApplicationDirectory()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string baseDirectory = AppContext.BaseDirectory;
        string binSegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        bool launchedFromBuildOutput =
            baseDirectory.Contains(binSegment, StringComparison.OrdinalIgnoreCase) &&
            File.Exists(Path.Combine(currentDirectory, "CrowdCheckpointSim.csproj"));

        return launchedFromBuildOutput ? currentDirectory : baseDirectory;
    }

    /// <summary>Creates the recordings directory if necessary and returns its full path.</summary>
    public static string EnsureRecordingDirectory()
    {
        string directory = Path.Combine(ResolveApplicationDirectory(), RecordingFolderName);
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>Creates a timestamped recording filename.</summary>
    public static string BuildFileName(DateTime localTimestamp, string extension)
    {
        string normalizedExtension = extension.StartsWith('.') ? extension : $".{extension}";
        return $"CrowdSim_{localTimestamp:yyyy-MM-dd_HHmmss_fff}{normalizedExtension}";
    }

    /// <summary>Video encoders commonly require even frame dimensions.</summary>
    public static int MakeEvenDimension(int value)
    {
        int safe = Math.Max(2, value);
        return safe % 2 == 0 ? safe : safe - 1;
    }
}
