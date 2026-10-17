using System.Numerics;
using CrowdCheckpointSim.Configuration;
using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class ScenarioConfigStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsScenarioAndOperatorPosts()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            ScenarioConfigStore store = new(directory);
            SimulationConfig config = new()
            {
                HallLength = 25.0f,
                HallWidth = 18.5f,
                FunnelWidth = 7.5f,
                ExitWidth = 8.25f,
                TargetFunnelDensity = 4.2f,
                YellowPercent = 3.0f,
                OperatorAcquisitionRadius = 7.5f,
                YellowVisualSearchSeconds = 3.5f,
                InitialManualClearanceSeconds = 17.5f,
                PassengerMinimumSpeed = 0.65f,
                PassengerMaximumSpeed = 1.35f,
                OperatorInterceptionSpeed = 2.25f,
                ManualClearanceFatigueIncreasePercent = 12f,
                OperatorInterceptionSpeedLossPercent = 7f,
                DasherClearanceSeconds = 24.0f,
                RandomSeed = 77,
            };
            Vector2[] posts =
            {
                new(2.0f, 2.0f),
                new(8.0f, 7.0f),
            };

            string path = store.Save("Airport Trial", config, posts);
            string savedJson = File.ReadAllText(path);
            LoadedScenarioConfig loaded = store.Load(Path.GetFileName(path));

            Assert.Contains("\"HallWidth\"", savedJson);
            Assert.Contains("\"YellowVisualSearchSeconds\"", savedJson);
            Assert.Contains("\"DasherClearanceSeconds\"", savedJson);
            Assert.DoesNotContain("\"RoomHeight\"", savedJson);
            Assert.Equal("Airport Trial", loaded.DisplayName);
            Assert.Equal(config.HallLength, loaded.Config.HallLength, 3);
            Assert.Equal(config.HallWidth, loaded.Config.HallWidth, 3);
            Assert.Equal(config.FunnelWidth, loaded.Config.FunnelWidth, 3);
            Assert.Equal(config.ExitWidth, loaded.Config.ExitWidth, 3);
            Assert.Equal(config.TargetFunnelDensity, loaded.Config.TargetFunnelDensity, 3);
            Assert.Equal(config.YellowPercent, loaded.Config.YellowPercent, 3);
            Assert.Equal(1.0f, loaded.Config.RedPercent, 3);
            Assert.Equal(96.0f, loaded.Config.GreenPercent, 3);
            Assert.Equal(config.OperatorAcquisitionRadius, loaded.Config.OperatorAcquisitionRadius, 3);
            Assert.Equal(config.YellowVisualSearchSeconds, loaded.Config.YellowVisualSearchSeconds, 3);
            Assert.Equal(config.InitialManualClearanceSeconds, loaded.Config.InitialManualClearanceSeconds, 3);
            Assert.Equal(config.PassengerMinimumSpeed, loaded.Config.PassengerMinimumSpeed, 3);
            Assert.Equal(config.PassengerMaximumSpeed, loaded.Config.PassengerMaximumSpeed, 3);
            Assert.Equal(config.OperatorInterceptionSpeed, loaded.Config.OperatorInterceptionSpeed, 3);
            Assert.Equal(config.ManualClearanceFatigueIncreasePercent, loaded.Config.ManualClearanceFatigueIncreasePercent, 3);
            Assert.Equal(config.OperatorInterceptionSpeedLossPercent, loaded.Config.OperatorInterceptionSpeedLossPercent, 3);
            Assert.Equal(config.DasherClearanceSeconds, loaded.Config.DasherClearanceSeconds, 3);
            Assert.Equal(config.RandomSeed, loaded.Config.RandomSeed);
            Assert.Equal(posts.Length, loaded.OperatorPosts.Count);
            Assert.Equal(posts[0], loaded.OperatorPosts[0]);
            Assert.Equal(posts[1], loaded.OperatorPosts[1]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Load_AcceptsLegacyRoomHeightFieldAsHallWidth()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string legacyPath = Path.Combine(directory, "legacy.json");
            File.WriteAllText(
                legacyPath,
                """
                {
                  "FormatVersion": 1,
                  "DisplayName": "Legacy Width",
                  "Scenario": {
                    "HallLength": 25.0,
                    "RoomHeight": 18.0,
                    "ExitWidth": 6.0
                  },
                  "OperatorPosts": []
                }
                """);

            ScenarioConfigStore store = new(directory);
            LoadedScenarioConfig loaded = store.Load("legacy.json");

            Assert.Equal(18.0f, loaded.Config.HallWidth, 3);
            Assert.Equal(18.0f, loaded.Config.RoomHeight, 3);
            Assert.Equal(6.0f, loaded.Config.ExitWidth, 3);
            Assert.Equal(2.0f, loaded.Config.YellowVisualSearchSeconds, 3);
            Assert.Equal(10.0f, loaded.Config.DasherClearanceSeconds, 3);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsMultiLaneModeAndGateClearance()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            ScenarioConfigStore store = new(directory);
            SimulationConfig config = new()
            {
                Mode = CheckpointMode.MultiLaneBarricade,
                HallWidth = 18.0f,
                FunnelWidth = 18.0f,
                LaneClearanceSeconds = 7.5f,
                MultiLaneYellowPercent = 6.0f,
                MultiLaneManualClearanceSeconds = 22.0f,
                TargetFunnelDensity = 5.0f,
            };

            string path = store.Save("Multi Lane", config, Array.Empty<Vector2>());
            string json = File.ReadAllText(path);
            LoadedScenarioConfig loaded = store.Load(Path.GetFileName(path));

            Assert.Contains("\"CheckpointMode\"", json);
            Assert.Contains("\"LaneClearanceSeconds\"", json);
            Assert.Contains("\"MultiLaneYellowPercent\"", json);
            Assert.Contains("\"MultiLaneManualClearanceSeconds\"", json);
            Assert.Equal(CheckpointMode.MultiLaneBarricade, loaded.Config.Mode);
            Assert.Equal(18, loaded.Config.MultiLaneCount);
            Assert.Equal(18.0f, loaded.Config.FunnelWidth, 3);
            Assert.Equal(7.5f, loaded.Config.LaneClearanceSeconds, 3);
            Assert.Equal(6.0f, loaded.Config.MultiLaneYellowPercent, 3);
            Assert.Equal(4.0f, loaded.Config.MultiLaneRedPercent, 3);
            Assert.Equal(90.0f, loaded.Config.MultiLaneGreenPercent, 3);
            Assert.Equal(22.0f, loaded.Config.MultiLaneManualClearanceSeconds, 3);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void List_ReturnsSavedConfigurations()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            ScenarioConfigStore store = new(directory);
            store.Save("Study A", new SimulationConfig(), Array.Empty<Vector2>());

            SavedScenarioConfigSummary item = Assert.Single(store.List());

            Assert.Equal("Study A", item.DisplayName);
            Assert.True(item.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Save_ReplacesPathSeparatorsInConfigurationName()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            ScenarioConfigStore store = new(directory);

            string path = store.Save("Study/Variant", new SimulationConfig(), Array.Empty<Vector2>());

            Assert.Equal("Study_Variant.json", Path.GetFileName(path));
            Assert.True(File.Exists(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"crowd-sim-config-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
