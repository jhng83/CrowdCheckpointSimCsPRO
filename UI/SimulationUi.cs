using System.Numerics;
using CrowdCheckpointSim.Configuration;
using CrowdCheckpointSim.Core;
using CrowdCheckpointSim.Rendering;
using CrowdCheckpointSim.Recording;
using CrowdCheckpointSim.Visual;
using ImGuiNET;

namespace CrowdCheckpointSim.UI;

/// <summary>Dear ImGui setup/runtime interface and application-level simulation state.</summary>
public sealed class SimulationUi
{
    private const float BaseSidebarWidth = 520f;
    private const float BaseSetupControlWidth = 235f;
    private const float BaseSetupWorldTopReservedHeight = 52f;
    private const float BaseRuntimeDashboardHeight = 230f;
    private const float BaseDashboardGap = 8f;
    private const float BaseTopStatusBarHeight = 48f;
    private const float BaseBottomControlBarHeight = 48f;
    private readonly CrowdSimulation _simulation;
    private readonly WorldRenderer _renderer = new();
    private readonly FullUiRecorder _recorder;
    private readonly ScenarioConfigStore _configStore = new();
    private List<SavedScenarioConfigSummary> _savedConfigurations = new();
    private bool _modeSelected;
    private bool _scenarioConfirmed;
    private bool _running;
    private bool _runStopped;
    private bool _showResultsScreen;
    private bool _operatorPlacementMode;
    private ResponderKind _placementKind = ResponderKind.PurpleOperator;
    private bool _dasherSelectionMode;
    private string _dasherSelectionFeedback = string.Empty;
    private float _hallLength;
    private float _hallWidth;
    private float _funnelWidth;
    private float _exitWidth;
    private float _density;
    private float _yellowPercent;
    private float _operatorAcquisitionRadius;
    private float _yellowVisualSearchSeconds;
    private float _initialManualClearanceSeconds;
    private float _robotManualClearanceSeconds;
    private float _passengerMinimumSpeed;
    private float _passengerMaximumSpeed;
    private float _operatorInterceptionSpeed;
    private float _manualClearanceFatigueIncreasePercent;
    private float _operatorInterceptionSpeedLossPercent;
    private float _dasherClearanceSeconds;
    private float _laneClearanceSeconds;
    private float _multiLaneYellowPercent;
    private float _multiLaneManualClearanceSeconds;
    private int _simulationSpeedMultiplier = 1;
    private float _uiScale = 1f;
    private int _seed;
    private string _configurationName = "My Scenario";
    private int _selectedConfigurationIndex = -1;
    private string _statusMessage = "Select a checkpoint mode to begin setup.";

    public SimulationUi(CrowdSimulation simulation, FullUiRecorder recorder)
    {
        _simulation = simulation;
        _recorder = recorder;
        CopyConfigToInputs();
        RefreshSavedConfigurations();
    }

    public bool ExitRequested { get; private set; }
    public bool IsRunning => _running;
    public bool ScenarioConfirmed => _scenarioConfirmed;

    /// <summary>Advance the model only while the runtime is in the Started state.</summary>
    public void Advance(float deltaSeconds)
    {
        if (_scenarioConfirmed && _running)
        {
            _simulation.Advance(deltaSeconds, _simulationSpeedMultiplier);
        }
    }

    /// <summary>Render the complete application UI into the current ImGui frame.</summary>
    public void Draw(Vector2 windowSize)
    {
        _uiScale = ResponsiveUiScale.Calculate(windowSize);
        ProductTheme.ApplyUiScale(_uiScale);

        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
        ImGui.Begin(
            "Crowd Checkpoint Simulation",
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoBringToFrontOnFocus);

        HandleKeyboardShortcuts();
        DrawTopStatusBar();

        // Reserve one compact row at the bottom for the permanent run controls. The sidebar and
        // simulation viewport then share every remaining pixel instead of leaving unused space.
        float bottomControlHeight = Scale(BaseBottomControlBarHeight);
        float mainContentHeight = Math.Max(
            1f,
            ImGui.GetContentRegionAvail().Y - bottomControlHeight - ImGui.GetStyle().ItemSpacing.Y);

        ImGui.BeginChild(
            "##sidebar",
            new Vector2(Scale(BaseSidebarWidth), mainContentHeight),
            ImGuiChildFlags.Borders);
        DrawSidebar();
        ImGui.EndChild();

        ImGui.SameLine();
        ImGui.BeginChild(
            "##world",
            new Vector2(0f, mainContentHeight),
            ImGuiChildFlags.None,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        float reservedStartY = ImGui.GetCursorPosY();
        bool drawWorld = true;

        // Mode selection is deliberately separated from scenario setup so the two checkpoint
        // operating concepts never share ambiguous setup controls.
        if (_scenarioConfirmed && _showResultsScreen)
        {
            DrawResultsScreen();
            drawWorld = false;
        }
        else if (!_modeSelected)
        {
            Vector2 hintPosition = ImGui.GetCursorScreenPos() + new Vector2(Scale(14f));
            ImGui.GetWindowDrawList().AddText(
                hintPosition,
                ImGui.GetColorU32(ProfessionalPalette.PrimaryText),
                "Select a checkpoint mode in the left panel before configuring the scenario.");
            float reservedUsed = ImGui.GetCursorPosY() - reservedStartY;
            ImGui.Dummy(new Vector2(
                0f,
                Math.Max(0f, Scale(BaseSetupWorldTopReservedHeight) - reservedUsed)));
        }
        else if (!_scenarioConfirmed)
        {
            Vector2 hintPosition = ImGui.GetCursorScreenPos() + new Vector2(Scale(14f));
            string setupHint = _operatorPlacementMode
                ? $"Placing {(_placementKind == ResponderKind.WheeledRobot ? "robots" : "Purple operators")} | left-click = add | right-click = remove | wheel = zoom"
                : "Setup: choose a responder placement tool to edit posts | mouse wheel or Zoom slider = zoom";
            ImGui.GetWindowDrawList().AddText(
                hintPosition,
                ImGui.GetColorU32(ProfessionalPalette.PrimaryText),
                setupHint);

            float reservedUsed = ImGui.GetCursorPosY() - reservedStartY;
            float reservedRemaining = Math.Max(
                0f,
                Scale(BaseSetupWorldTopReservedHeight) - reservedUsed);
            ImGui.Dummy(new Vector2(0f, reservedRemaining));
        }
        else
        {
            DrawRuntimeDashboard();
        }

        if (drawWorld)
        {
            _renderer.Draw(
                _simulation,
                _modeSelected && !_scenarioConfirmed,
                _dasherSelectionMode,
                _modeSelected && _operatorPlacementMode,
                _placementKind);
            HandleCanvasInput();
        }
        ImGui.EndChild();

        DrawBottomRunControls();
        ImGui.End();
    }

    private void DrawTopStatusBar()
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ProfessionalPalette.CardBackground);
        ImGui.PushStyleColor(ImGuiCol.Border, ProfessionalPalette.Border);
        ImGui.BeginChild(
            "##permanent-top-status",
            new Vector2(0f, Scale(BaseTopStatusBarHeight)),
            ImGuiChildFlags.Borders);

        DrawTopStatusSummary();

        ImGui.EndChild();
        ImGui.PopStyleColor(2);
    }

    private void DrawBottomRunControls()
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ProfessionalPalette.CardBackground);
        ImGui.PushStyleColor(ImGuiCol.Border, ProfessionalPalette.Border);
        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowPadding,
            new Vector2(Scale(10f), Scale(7f)));
        ImGui.BeginChild(
            "##permanent-bottom-controls",
            new Vector2(0f, Scale(BaseBottomControlBarHeight)),
            ImGuiChildFlags.Borders);

        DrawRunControls();

        ImGui.EndChild();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);
    }

    private void DrawTopStatusSummary()
    {
        string modeText = !_modeSelected
            ? "Not selected"
            : _simulation.Config.Mode == CheckpointMode.MultiLaneBarricade
                ? "Mode 2 - Multi-Lane Barricade"
                : "Mode 1 - Open Funnel";
        string stateText = GetRunStateText();
        Vector4 stateColor = stateText switch
        {
            "RUNNING" => ProfessionalPalette.Success,
            "PAUSED" => ProfessionalPalette.Warning,
            "STOPPED" => ProfessionalPalette.Danger,
            "READY" => ProfessionalPalette.AccentHover,
            _ => ProfessionalPalette.SecondaryText,
        };

        float rowStartX = ImGui.GetCursorPosX();
        float availableWidth = ImGui.GetContentRegionAvail().X;
        DrawStatusField("MODE", modeText, ProfessionalPalette.PrimaryText);
        ImGui.SameLine(rowStartX + availableWidth * 0.33f);
        DrawStatusField("STATE", stateText, stateColor);
        ImGui.SameLine(rowStartX + availableWidth * 0.51f);
        DrawStatusField("ELAPSED", FormatElapsed(_simulation.Metrics.TimeSeconds), ProfessionalPalette.PrimaryText);
        ImGui.SameLine(rowStartX + availableWidth * 0.69f);
        DrawStatusField("SPEED", $"{_simulationSpeedMultiplier}x", ProfessionalPalette.PrimaryText);
        ImGui.SameLine(rowStartX + availableWidth * 0.82f);
        DrawStatusField(
            "SEED",
            _modeSelected ? _simulation.Config.RandomSeed.ToString() : "-",
            ProfessionalPalette.PrimaryText);
    }

    private static void DrawStatusField(string label, string value, Vector4 valueColor)
    {
        ImGui.TextColored(ProfessionalPalette.AccentHover, label);
        ImGui.SameLine();
        ColoredText(valueColor, value);
    }

    private void DrawRunControls()
    {
        bool canPause = _scenarioConfirmed && _running;
        bool canStop = _scenarioConfirmed && !_runStopped && _simulation.Metrics.TimeSeconds > 0.001f;
        bool canRestart = _scenarioConfirmed;
        bool canReturnToModeSelection = _modeSelected;

        float gap = Scale(6f);
        float availableWidth = ImGui.GetContentRegionAvail().X;
        float buttonWidth = Math.Max(Scale(100f), (availableWidth - gap * 4f) / 5f);
        Vector2 buttonSize = new(buttonWidth, Scale(27f));

        if (!canPause)
        {
            ImGui.BeginDisabled();
        }
        if (WarningButton("Pause", buttonSize))
        {
            _running = false;
            _statusMessage = "Simulation paused. Press Resume to continue or Stop to finish the run.";
        }
        if (!canPause)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine(0f, gap);
        if (!canStop)
        {
            ImGui.BeginDisabled();
        }
        if (DangerButton("Stop", buttonSize))
        {
            StopRunAndShowResults();
        }
        if (!canStop)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine(0f, gap);
        if (!canRestart)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Button("Restart same seed", buttonSize))
        {
            RestartRun(useNewSeed: false);
        }
        if (!canRestart)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine(0f, gap);
        if (!canRestart)
        {
            ImGui.BeginDisabled();
        }
        if (OperatorButton("Run with new seed", buttonSize))
        {
            RestartRun(useNewSeed: true);
        }
        if (!canRestart)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine(0f, gap);
        if (!canReturnToModeSelection)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Button("Return to mode selection", buttonSize))
        {
            ReturnToModeSelection();
        }
        if (!canReturnToModeSelection)
        {
            ImGui.EndDisabled();
        }
    }

    private void StartOrResumeRun()
    {
        if (!_scenarioConfirmed || _running || _runStopped)
        {
            return;
        }

        bool isResume = _simulation.Metrics.TimeSeconds > 0.001f;
        _showResultsScreen = false;
        _running = true;
        _statusMessage = isResume ? "Simulation resumed." : "Simulation running.";
    }

    private void StopRunAndShowResults()
    {
        if (!_scenarioConfirmed || _runStopped || _simulation.Metrics.TimeSeconds <= 0.001f)
        {
            return;
        }

        _recorder.RequestStop();
        _running = false;
        _runStopped = true;
        _showResultsScreen = true;
        _dasherSelectionMode = false;
        _statusMessage = "Run stopped. Final results are displayed and the simulation state is preserved.";
    }

    private void RestartRun(bool useNewSeed)
    {
        if (!_scenarioConfirmed)
        {
            return;
        }

        _recorder.RequestStop();
        if (useNewSeed)
        {
            SimulationConfig next = _simulation.Config.Clone();
            int newSeed;
            do
            {
                newSeed = Random.Shared.Next(1, int.MaxValue);
            }
            while (newSeed == next.RandomSeed);

            next.RandomSeed = newSeed;
            _simulation.Reconfigure(next);
            CopyConfigToInputs();
        }
        else
        {
            _simulation.Reset();
        }

        _running = true;
        _runStopped = false;
        _showResultsScreen = false;
        _dasherSelectionMode = false;
        _dasherSelectionFeedback = string.Empty;
        _statusMessage = useNewSeed
            ? $"New run started with random seed {_simulation.Config.RandomSeed}."
            : $"Run restarted with the same seed ({_simulation.Config.RandomSeed}).";
    }

    private string GetRunStateText()
    {
        if (!_modeSelected)
        {
            return "SELECT MODE";
        }

        if (!_scenarioConfirmed)
        {
            return "SETUP";
        }

        if (_runStopped)
        {
            return "STOPPED";
        }

        if (_running)
        {
            return "RUNNING";
        }

        return _simulation.Metrics.TimeSeconds <= 0.001f ? "READY" : "PAUSED";
    }

    private static string FormatElapsed(float seconds)
    {
        int wholeSeconds = Math.Max(0, (int)MathF.Floor(seconds));
        int hours = wholeSeconds / 3600;
        int minutes = wholeSeconds % 3600 / 60;
        int remainingSeconds = wholeSeconds % 60;
        return hours > 0
            ? $"{hours:00}:{minutes:00}:{remainingSeconds:00}"
            : $"{minutes:00}:{remainingSeconds:00}";
    }

    private void DrawSidebar()
    {
        ImGui.TextColored(ProfessionalPalette.AccentHover, "CROWD OPERATIONS SIMULATOR");
        ImGui.TextDisabled("Checkpoint flow / response simulation");
        ImGui.Separator();

        if (!_modeSelected)
        {
            DrawModeSelectionControls();
        }
        else if (!_scenarioConfirmed)
        {
            DrawSetupControls();
        }
        else
        {
            DrawRuntimeControls();
        }

        ImGui.Separator();
        DrawViewControls();
        ImGui.Separator();
        ColoredWrappedText(ProfessionalPalette.PrimaryText, _statusMessage);
    }

    private void DrawModeSelectionControls()
    {
        ImGui.TextColored(ProfessionalPalette.AccentHover, "SELECT CHECKPOINT MODE");
        ImGui.Spacing();
        ColoredWrappedText(
            ProfessionalPalette.SecondaryText,
            "Choose the checkpoint operating concept first. Mode-specific geometry and queue controls appear only after selection.");

        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ProfessionalPalette.CardBackground);
        ImGui.PushStyleColor(ImGuiCol.Border, ProfessionalPalette.Border);
        ImGui.BeginChild(
            "##mode-open-funnel",
            new Vector2(0f, Scale(150f)),
            ImGuiChildFlags.Borders);
        ImGui.TextColored(ProfessionalPalette.AccentHover, "MODE 1 - OPEN FUNNEL");
        ImGui.TextWrapped("Current behaviour: dense orderly funnel queue, checkpoint crossing, then SFM movement in the main hall.");
        ImGui.Spacing();
        if (PrimaryButton("Use Open Funnel Mode", new Vector2(-1f, Scale(34f))))
        {
            SelectCheckpointMode(CheckpointMode.OpenFunnel);
        }
        ImGui.EndChild();
        ImGui.PopStyleColor(2);

        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ProfessionalPalette.CardBackground);
        ImGui.PushStyleColor(ImGuiCol.Border, ProfessionalPalette.Border);
        ImGui.BeginChild(
            "##mode-multilane",
            new Vector2(0f, Scale(202f)),
            ImGuiChildFlags.Borders);
        ImGui.TextColored(ProfessionalPalette.AccentHover, "MODE 2 - MULTI-LANE BARRICADE");
        ImGui.TextWrapped(
            "Fixed 1.0 m single-file lanes with translucent waist-level dividers and one independently timed checkpoint gate per lane.");
        ImGui.Spacing();
        ImGui.TextDisabled("Hall width 15-20 m = automatically 15-20 lanes.");
        ImGui.TextDisabled("At maximum density every lane is filled gate-to-back.");
        ImGui.Spacing();
        if (PrimaryButton(
                "Use Multi-Lane Barricade Mode",
                new Vector2(-1f, Scale(34f))))
        {
            SelectCheckpointMode(CheckpointMode.MultiLaneBarricade);
        }
        ImGui.EndChild();
        ImGui.PopStyleColor(2);
    }

    private void SelectCheckpointMode(CheckpointMode mode)
    {
        SimulationConfig next = _simulation.Config.Clone();
        next.Mode = mode;
        if (mode == CheckpointMode.MultiLaneBarricade)
        {
            next.HallWidth = Math.Clamp(
                MathF.Round(next.HallWidth),
                SimulationConfig.MinHallWidth,
                SimulationConfig.MaxHallWidth);
            next.FunnelWidth = next.HallWidth;
        }
        else
        {
            float openFunnelWidth = _funnelWidth is >= SimulationConfig.MinFunnelWidth and <= SimulationConfig.MaxFunnelWidth
                ? _funnelWidth
                : SimulationConfig.MinFunnelWidth;
            next.FunnelWidth = Math.Clamp(
                openFunnelWidth,
                SimulationConfig.MinFunnelWidth,
                Math.Min(SimulationConfig.MaxFunnelWidth, next.HallWidth));
        }

        _simulation.Reconfigure(next);
        _simulation.ClearOperatorPosts();
        _modeSelected = true;
        _scenarioConfirmed = false;
        _running = false;
        _runStopped = false;
        _showResultsScreen = false;
        _operatorPlacementMode = false;
        _dasherSelectionMode = false;
        CopyConfigToInputs();
        _statusMessage = mode == CheckpointMode.MultiLaneBarricade
            ? $"Multi-Lane Barricade mode selected: {_simulation.Config.MultiLaneCount} x 1.0 m lanes. Configure the scenario, then place operators."
            : "Open Funnel mode selected. Configure the scenario, then place operators.";
    }

    private void ReturnToModeSelection()
    {
        _recorder.RequestStop();
        _simulation.ClearOperatorPosts();
        _simulation.Reset();
        _modeSelected = false;
        _scenarioConfirmed = false;
        _running = false;
        _runStopped = false;
        _showResultsScreen = false;
        _operatorPlacementMode = false;
        _dasherSelectionMode = false;
        _statusMessage = "Select a checkpoint mode to begin setup.";
    }

    private void DrawSetupControls()
    {
        ImGui.TextColored(ProfessionalPalette.AccentHover, "SCENARIO SETUP");
        ImGui.TextDisabled(_simulation.Config.Mode == CheckpointMode.MultiLaneBarricade
            ? "Multi-Lane Barricade"
            : "Open Funnel");
        ColoredWrappedText(ProfessionalPalette.SecondaryText,
            "1. Configure  /  2. Place operators  /  3. Confirm");
        if (ImGui.SmallButton("Change checkpoint mode"))
        {
            ReturnToModeSelection();
            return;
        }
        ImGui.Spacing();

        bool changed = false;
        // Keep placement/readiness controls close at hand while longer settings scroll locally.
        float optionsHeight = Math.Clamp(
            ImGui.GetContentRegionAvail().Y - Scale(365f), Scale(180f), Scale(500f));
        if (ImGui.BeginTabBar("##setup-sections"))
        {
            if (ImGui.BeginTabItem("Hall / crowd"))
            {
                ImGui.BeginChild("##hall-options", new Vector2(0f, optionsHeight), ImGuiChildFlags.None);
                changed |= DrawHallAndCrowdSetup();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Checkpoint"))
            {
                ImGui.BeginChild("##checkpoint-options", new Vector2(0f, optionsHeight), ImGuiChildFlags.None);
                changed |= DrawCheckpointSetup();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Operators"))
            {
                ImGui.BeginChild("##operator-options", new Vector2(0f, optionsHeight), ImGuiChildFlags.None);
                changed |= DrawOperatorSetup();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Dasher"))
            {
                ImGui.BeginChild("##dasher-options", new Vector2(0f, optionsHeight), ImGuiChildFlags.None);
                SectionHeading("DASHER INCIDENT", ProfessionalPalette.Dasher);
                ColoredWrappedText(ProfessionalPalette.SecondaryText,
                    "Dasher speed: 2.50 m/s. Purple pursuit: 3.50 m/s. Robots ignore Dashers.");
                changed |= SetupSlider("Holding-point clearance time",
                    ref _dasherClearanceSeconds, SimulationConfig.MinDasherClearanceSeconds,
                    SimulationConfig.MaxDasherClearanceSeconds, "%.0f s");
                ColoredWrappedText(ProfessionalPalette.SecondaryText,
                    "Time held before removal. Passenger and normal-interception settings do not change Dasher response.");
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Files"))
            {
                ImGui.BeginChild("##file-options", new Vector2(0f, optionsHeight), ImGuiChildFlags.None);
                SectionHeading("REPRODUCIBILITY");
                ImGui.SetNextItemWidth(-1f);
                changed |= ImGui.InputInt("Random seed", ref _seed, 1, 10);
                if (changed)
                {
                    // Apply edits before saving so the stored file always contains the visible seed.
                    ApplySetupInputs();
                    changed = false;
                }
                ImGui.Spacing();
                DrawConfigurationControls();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        if (changed)
        {
            ApplySetupInputs();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(ProfessionalPalette.Operator, "RESPONDER PLACEMENT");
        if (_operatorPlacementMode)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("Finish placement"))
            {
                _operatorPlacementMode = false;
                _statusMessage = "Placement finished. All responder posts are retained.";
            }
        }
        ImGui.Spacing();

        float placementButtonWidth = (ImGui.GetContentRegionAvail().X - Scale(8f)) * 0.5f;
        if (OperatorButton("Place Purple operator", new Vector2(placementButtonWidth, Scale(30f))))
        {
            ActivateOperatorPlacement(ResponderKind.PurpleOperator);
        }
        ImGui.SameLine();
        if (RobotButton("Place wheeled robot", new Vector2(placementButtonWidth, Scale(30f))))
        {
            ActivateOperatorPlacement(ResponderKind.WheeledRobot);
        }

        if (_operatorPlacementMode)
        {
            ColoredWrappedText(
                ProfessionalPalette.SecondaryText,
                $"Placing {(_placementKind == ResponderKind.WheeledRobot ? "robots" : "Purple operators")}: hover for a preview, left-click to place, right-click to remove the nearest responder.");
        }

        if (ImGui.Button("Clear operators", new Vector2(placementButtonWidth, 0)))
        {
            _simulation.ClearOperatorPosts(ResponderKind.PurpleOperator);
            ActivateOperatorPlacement(ResponderKind.PurpleOperator);
            _statusMessage = "Purple posts cleared. Robot posts retained; Purple placement is active.";
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear robots", new Vector2(placementButtonWidth, 0)))
        {
            _simulation.ClearOperatorPosts(ResponderKind.WheeledRobot);
            ActivateOperatorPlacement(ResponderKind.WheeledRobot);
            _statusMessage = "Robot posts cleared. Purple posts retained; robot placement is active.";
        }

        bool canConfirm = _simulation.OperatorPosts.Count > 0;
        if (!canConfirm)
        {
            DrawOperatorRequiredWarning();
        }
        else
        {
            ImGui.TextColored(
                ProfessionalPalette.Success,
                $"READY: {_simulation.Operators.Count(op => !op.IsRobot)} Purple / {_simulation.Operators.Count(op => op.IsRobot)} robot(s).");
        }

        ImGui.Spacing();
        if (!canConfirm)
        {
            ImGui.BeginDisabled();
        }

        if (PrimaryButton("Confirm Scenario", new Vector2(-1f, Scale(36f))))
        {
            _scenarioConfirmed = true;
            _running = false;
            _runStopped = false;
            _showResultsScreen = false;
            _operatorPlacementMode = false;
            _dasherSelectionMode = false;
            _statusMessage = "Scenario confirmed. Press Start to run.";
        }

        if (!canConfirm)
        {
            ImGui.EndDisabled();
        }
    }

    private bool DrawHallAndCrowdSetup()
    {
        bool changed = false;
        SectionHeading("HALL GEOMETRY");
        changed |= SetupSlider("Main hall length", ref _hallLength,
            SimulationConfig.MinHallLength, SimulationConfig.MaxHallLength, "%.1f m");
        if (_simulation.Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            int width = (int)MathF.Round(_hallWidth);
            ImGui.TextUnformatted("Main hall width / lane count");
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.SliderInt("##hall-lanes", ref width,
                    (int)SimulationConfig.MinHallWidth, (int)SimulationConfig.MaxHallWidth,
                    "%d lanes", ImGuiSliderFlags.AlwaysClamp))
            {
                _hallWidth = _funnelWidth = width;
                changed = true;
            }
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                "Each lane is 1.0 m wide; the checkpoint spans the full hall width.");
        }
        else
        {
            changed |= SetupSlider("Main hall width", ref _hallWidth,
                SimulationConfig.MinHallWidth, SimulationConfig.MaxHallWidth, "%.1f m");
            changed |= SetupSlider("Funnel width", ref _funnelWidth,
                SimulationConfig.MinFunnelWidth, Math.Min(SimulationConfig.MaxFunnelWidth, _hallWidth), "%.2f m");
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                "Funnel width: 5-20 m, capped by the selected hall width. Select a 20 m hall width to use a 20 m funnel.");
        }

        float maxExit = Math.Min(SimulationConfig.MaxExitWidth, _hallWidth);
        float safeExit = Math.Clamp(_exitWidth, SimulationConfig.MinExitWidth, maxExit);
        if (safeExit != _exitWidth)
        {
            _exitWidth = safeExit;
            changed = true;
        }
        changed |= SetupSlider("Exit width", ref _exitWidth,
            SimulationConfig.MinExitWidth, maxExit, "%.2f m");

        SectionHeading("PASSENGER FLOW");
        bool mode2 = _simulation.Config.Mode == CheckpointMode.MultiLaneBarricade;
        changed |= SetupSlider(mode2 ? "Queue density / occupancy" : "Funnel density",
            ref _density, 0.5f, SimulationConfig.MaxSetupDensity, "%.2f /m2");
        ColoredWrappedText(ProfessionalPalette.SecondaryText,
            mode2
                ? $"Queue occupancy: {_density / SimulationConfig.MaxSetupDensity * 100f:F0}%. At 8.00 every lane is filled gate-to-back."
                : $"Applied population: {_simulation.Config.TargetFunnelPopulation} passengers. The funnel keeps an orderly queue.");

        SectionHeading("PREFERRED WALKING SPEED");
        changed |= SetupSlider("Minimum passenger speed", ref _passengerMinimumSpeed,
            SimulationConfig.MinPassengerPreferredSpeed, _passengerMaximumSpeed, "%.2f m/s");
        changed |= SetupSlider("Maximum passenger speed", ref _passengerMaximumSpeed,
            _passengerMinimumSpeed, SimulationConfig.MaxPassengerPreferredSpeed, "%.2f m/s");
        ColoredWrappedText(ProfessionalPalette.SecondaryText,
            "Free-flow preferences for ordinary passengers. Queues, crowd forces and yielding can slow or stop them. The existing 1.65 m/s safety cap stays in place.");
        return changed;
    }

    private bool DrawCheckpointSetup()
    {
        bool changed = false;
        if (_simulation.Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            SectionHeading("AUTOMATED PROCESSING");
            changed |= SetupSlider("Clearance time per attempt", ref _laneClearanceSeconds,
                SimulationConfig.MinLaneClearanceSeconds, SimulationConfig.MaxLaneClearanceSeconds, "%.1f s");
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                "Each gate operates independently. A second attempt takes the full clearance time again.");
            SectionHeading("BARRICADE OUTCOMES");
            changed |= SetupSlider("Second-attempt Yellow (% of all passengers)",
                ref _multiLaneYellowPercent, SimulationConfig.MinMultiLaneYellowPercent,
                SimulationConfig.MaxMultiLaneYellowPercent, "%.1f%%");
            ColoredText(ProfessionalPalette.Cleared, "First-attempt Green: 90.0%");
            ColoredText(ProfessionalPalette.Undetected, $"Second-attempt Yellow: {_multiLaneYellowPercent:F1}%");
            ColoredText(ProfessionalPalette.Uncounted, $"Failed both / Red: {10f - _multiLaneYellowPercent:F1}%");
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                "The remaining 10% receive attempt two. Yellow clears automatically; Red requests Purple manual clearance.");
        }
        else
        {
            SectionHeading("DETECTION OUTCOMES");
            changed |= SetupSlider("Undetected outcome", ref _yellowPercent,
                SimulationConfig.MinYellowPercent, SimulationConfig.MaxYellowPercent, "%.1f%%");
            ColoredText(ProfessionalPalette.Cleared, $"Cleared: {99f - _yellowPercent:F1}%");
            ColoredText(ProfessionalPalette.Undetected, $"Undetected: {_yellowPercent:F1}%");
            ColoredText(ProfessionalPalette.Uncounted, "Uncounted: 1.0% (fixed)");
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                "Targets apply to each 60-second interval. Purple response is configured in the Operators tab.");
        }
        return changed;
    }

    private bool DrawOperatorSetup()
    {
        bool changed = false;
        bool mode2 = _simulation.Config.Mode == CheckpointMode.MultiLaneBarricade;
        SectionHeading("ACQUISITION & RESPONSE", ProfessionalPalette.Operator);
        ColoredWrappedText(ProfessionalPalette.SecondaryText,
            "Purple operators reserve targets first. Wheeled robots immediately intercept remaining targets within range; they never respond to Dashers.");
        changed |= SetupSlider("Acquisition radius", ref _operatorAcquisitionRadius,
            SimulationConfig.MinOperatorAcquisitionRadius, SimulationConfig.MaxOperatorAcquisitionRadius, "%.1f m");
        if (mode2)
        {
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                "Red alarms dispatch available responders directly. Acquisition radius still applies to Purple Dasher response.");
            SectionHeading("PURPLE: RED MANUAL CLEARANCE");
            changed |= SetupSlider("Manual clearance time", ref _multiLaneManualClearanceSeconds,
                SimulationConfig.MinMultiLaneManualClearanceSeconds,
                SimulationConfig.MaxMultiLaneManualClearanceSeconds, "%.0f s");
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                $"Range: 10 s to 3 min. Current: {FormatElapsed(_multiLaneManualClearanceSeconds)}. Clearance and movement fatigue remain disabled in Mode 2.");
        }
        else
        {
            changed |= SetupSlider("Purple visual confirmation delay", ref _yellowVisualSearchSeconds,
                SimulationConfig.MinYellowVisualSearchSeconds, SimulationConfig.MaxYellowVisualSearchSeconds, "%.1f s");
            changed |= SetupSlider("Undetected interception speed", ref _operatorInterceptionSpeed,
                SimulationConfig.MinOperatorInterceptionSpeed, SimulationConfig.MaxOperatorInterceptionSpeed, "%.2f m/s");
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                "Shared normal interception speed. Escort, return and Purple Dasher speeds retain their existing rules.");

            SectionHeading("PURPLE: MANUAL CLEARANCE & FATIGUE");
            changed |= SetupSlider("Initial manual clearance time", ref _initialManualClearanceSeconds,
                SimulationConfig.MinInitialManualClearanceSeconds,
                SimulationConfig.MaxInitialManualClearanceSeconds, "%.0f s");
            changed |= SetupSlider("Clearance increase per completed case", ref _manualClearanceFatigueIncreasePercent,
                0f, SimulationConfig.MaxManualClearanceFatigueIncreasePercent, "%.1f%%");
            float increment = _initialManualClearanceSeconds * _manualClearanceFatigueIncreasePercent / 100f;
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                $"Range: 10 s to 3 min. Each completed case adds {increment:F1} s ({_manualClearanceFatigueIncreasePercent:F1}% of the initial time). 0% disables this increase.");
            DisabledText($"Case 1: {_initialManualClearanceSeconds:F1}s  |  2: {_initialManualClearanceSeconds + increment:F1}s  |  3: {_initialManualClearanceSeconds + 2f * increment:F1}s");

            SectionHeading("SHARED: INTERCEPTION SPEED LOSS");
            changed |= SetupSlider("Speed decrease after each interception", ref _operatorInterceptionSpeedLossPercent,
                0f, SimulationConfig.MaxOperatorInterceptionSpeedLossPercent, "%.1f%%");
            ColoredWrappedText(ProfessionalPalette.SecondaryText,
                $"Each catch subtracts {_operatorInterceptionSpeedLossPercent:F1}% of the initial speed, stopping at {_operatorInterceptionSpeed * 0.5f:F2} m/s (50%). 0% disables speed loss.");
            DisabledText($"Initial: {_operatorInterceptionSpeed:F2}  |  After 1: {OperatorTiming.InterceptionSpeed(_operatorInterceptionSpeed, 1, _operatorInterceptionSpeedLossPercent):F2}  |  After 2: {OperatorTiming.InterceptionSpeed(_operatorInterceptionSpeed, 2, _operatorInterceptionSpeedLossPercent):F2} m/s");
        }
        SectionHeading("WHEELED ROBOT: ON-SITE CLEARANCE", ProfessionalPalette.RobotAccent);
        changed |= SetupSlider("Robot manual clearance time", ref _robotManualClearanceSeconds,
            SimulationConfig.MinRobotManualClearanceSeconds,
            SimulationConfig.MaxRobotManualClearanceSeconds, "%.0f s");
        ColoredWrappedText(ProfessionalPalette.SecondaryText,
            $"Fixed for every case: {FormatElapsed(_robotManualClearanceSeconds)} (1 s to 3 min). No clearance fatigue or time progression.");
        ColoredWrappedText(ProfessionalPalette.SecondaryText,
            "After on-site clearance, intercept another unlocked passenger within acquisition range. If none is available, return to the original dock. Robots ignore Dashers.");
        return changed;
    }

    private void SectionHeading(string title, Vector4? color = null)
    {
        ImGui.Spacing();
        bool bold = ProductTheme.PushBoldFont();
        ImGui.TextColored(color ?? ProfessionalPalette.AccentHover, title);
        ProductTheme.PopBoldFont(bold);
        ImGui.Separator();
    }

    private static bool SetupSlider(string label, ref float value, float minimum, float maximum, string format)
    {
        ImGui.PushID(label);
        ImGui.TextUnformatted(label);
        ImGui.SetNextItemWidth(-1f);
        float previousValue = value;
        bool changed = ImGui.SliderFloat("##value", ref value, minimum, maximum, format, ImGuiSliderFlags.AlwaysClamp);
        if (!float.IsFinite(value))
        {
            value = previousValue;
            changed = false;
        }
        ImGui.PopID();
        return changed;
    }

    private void DrawConfigurationControls()
    {
        ImGui.TextColored(ProfessionalPalette.AccentHover, "CONFIGURATION");
        ImGui.TextDisabled("Save settings and Purple/robot posts for reuse.");
        ImGui.Spacing();

        ImGui.PushItemWidth(Scale(BaseSetupControlWidth));
        ImGui.InputText("Configuration name", ref _configurationName, 80);
        ImGui.PopItemWidth();

        if (PrimaryButton("Save Current Config", new Vector2(-1f, Scale(30f))))
        {
            SaveCurrentConfiguration();
        }

        ImGui.Spacing();
        string preview = _selectedConfigurationIndex >= 0 &&
            _selectedConfigurationIndex < _savedConfigurations.Count
                ? _savedConfigurations[_selectedConfigurationIndex].DisplayName
                : _savedConfigurations.Count == 0
                    ? "No saved configurations"
                    : "Select saved configuration";

        if (_savedConfigurations.Count == 0)
        {
            ImGui.BeginDisabled();
        }

        ImGui.PushItemWidth(Scale(BaseSetupControlWidth));
        if (ImGui.BeginCombo("Saved configuration", preview))
        {
            for (int index = 0; index < _savedConfigurations.Count; index++)
            {
                SavedScenarioConfigSummary item = _savedConfigurations[index];
                bool selected = index == _selectedConfigurationIndex;
                if (ImGui.Selectable(item.DisplayName, selected))
                {
                    _selectedConfigurationIndex = index;
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
        ImGui.PopItemWidth();

        if (_savedConfigurations.Count == 0)
        {
            ImGui.EndDisabled();
        }

        bool canLoad = _selectedConfigurationIndex >= 0 &&
            _selectedConfigurationIndex < _savedConfigurations.Count;
        if (!canLoad)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button("Load Selected Config", new Vector2(-1f, Scale(30f))))
        {
            LoadSelectedConfiguration();
        }

        if (!canLoad)
        {
            ImGui.EndDisabled();
        }

        if (ImGui.SmallButton("Refresh saved list"))
        {
            RefreshSavedConfigurations();
            _statusMessage = $"Saved configuration list refreshed from {_configStore.DirectoryPath}.";
        }
    }

    private void DrawOperatorRequiredWarning()
    {
        ImGui.Spacing();
        ImGui.PushStyleColor(
            ImGuiCol.ChildBg,
            ProfessionalPalette.Alpha(ProfessionalPalette.Warning, 0.07f));
        ImGui.PushStyleColor(
            ImGuiCol.Border,
            ProfessionalPalette.Alpha(ProfessionalPalette.Warning, 0.48f));
        ImGui.BeginChild(
            "##operator-required-card",
            new Vector2(0f, Scale(82f)),
            ImGuiChildFlags.Borders);

        ImGui.TextColored(ProfessionalPalette.Warning, "RESPONDER REQUIRED");
        ColoredWrappedText(
            ProfessionalPalette.PrimaryText,
            "Place at least one Purple operator or humanoid robot before confirming.");

        ImGui.EndChild();
        ImGui.PopStyleColor(2);
    }

    private void ActivateOperatorPlacement(ResponderKind kind = ResponderKind.PurpleOperator)
    {
        _placementKind = kind;
        _operatorPlacementMode = true;
        _statusMessage =
            $"Placing {(kind == ResponderKind.WheeledRobot ? "wheeled robots" : "Purple operators")}. Hover for a preview; left-click places, right-click removes the nearest responder.";
    }

    private void SaveCurrentConfiguration()
    {
        try
        {
            string path = _configStore.Save(
                _configurationName,
                _simulation.Config,
                _simulation.ResponderPosts);
            RefreshSavedConfigurations(Path.GetFileName(path));
            _statusMessage =
                $"Configuration saved: {Path.GetFileName(path)} ({_simulation.OperatorPosts.Count} responder post(s)).";
        }
        catch (Exception exception)
        {
            _statusMessage = $"Could not save configuration: {exception.Message}";
        }
    }

    private void LoadSelectedConfiguration()
    {
        if (_selectedConfigurationIndex < 0 ||
            _selectedConfigurationIndex >= _savedConfigurations.Count)
        {
            _statusMessage = "Select a saved configuration before loading.";
            return;
        }

        SavedScenarioConfigSummary selected = _savedConfigurations[_selectedConfigurationIndex];
        try
        {
            LoadedScenarioConfig loaded = _configStore.Load(selected.FileName);
            _simulation.Reconfigure(loaded.Config);
            _simulation.ClearOperatorPosts();

            int rejectedPosts = 0;
            foreach (ResponderPost post in loaded.ResponderPosts)
            {
                if (!_simulation.AddOperatorPost(post.Position, post.Kind))
                {
                    rejectedPosts++;
                }
            }

            _running = false;
            _runStopped = false;
            _showResultsScreen = false;
            _scenarioConfirmed = false;
            _modeSelected = true;
            _operatorPlacementMode = false;
            _dasherSelectionMode = false;
            _configurationName = loaded.DisplayName;
            CopyConfigToInputs();
            RefreshSavedConfigurations(loaded.FileName);

            _statusMessage = rejectedPosts == 0
                ? $"Loaded configuration '{loaded.DisplayName}' with {loaded.ResponderPosts.Count} Purple/robot post(s)."
                : $"Loaded '{loaded.DisplayName}', but {rejectedPosts} responder post(s) were outside the valid layout and were skipped.";
        }
        catch (Exception exception)
        {
            _statusMessage = $"Could not load configuration: {exception.Message}";
        }
    }

    private void RefreshSavedConfigurations(string? preferredFileName = null)
    {
        string? previousFileName = preferredFileName;
        if (previousFileName is null &&
            _selectedConfigurationIndex >= 0 &&
            _selectedConfigurationIndex < _savedConfigurations.Count)
        {
            previousFileName = _savedConfigurations[_selectedConfigurationIndex].FileName;
        }

        try
        {
            _savedConfigurations = _configStore.List().ToList();
            _selectedConfigurationIndex = previousFileName is null
                ? -1
                : _savedConfigurations.FindIndex(item =>
                    string.Equals(
                        item.FileName,
                        previousFileName,
                        StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            _savedConfigurations = new List<SavedScenarioConfigSummary>();
            _selectedConfigurationIndex = -1;
        }
    }

    private void DrawRuntimeControls()
    {
        ImGui.TextColored(ProfessionalPalette.AccentHover, "RUNTIME CONTROLS");
        if (_simulation.Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            int openGates = _simulation.CheckpointGates.Count(gate => gate.State == CheckpointGateState.Open);
            int processingGates = _simulation.CheckpointGates.Count(gate => gate.State == CheckpointGateState.Processing);
            int manualGates = _simulation.CheckpointGates.Count(gate => gate.State == CheckpointGateState.ManualIntervention);
            ImGui.TextDisabled($"Mode 2 | {_simulation.Config.MultiLaneCount} lanes | {_simulation.Config.LaneClearanceSeconds:F1}s per attempt");
            ImGui.TextDisabled($"Gates: {processingGates} processing | {openGates} open | {manualGates} manual");
        }
        else
        {
            ImGui.TextDisabled("Mode 1 | Open Funnel");
        }
        ImGui.Spacing();
        ColoredWrappedText(
            ProfessionalPalette.SecondaryText,
            "Pause, Stop, restart, and mode-selection controls remain visible in the bottom bar.");

        if (_runStopped && PrimaryButton(
                _showResultsScreen ? "Hide Results" : "View Results",
                new Vector2(-1f, Scale(32f))))
        {
            _showResultsScreen = !_showResultsScreen;
            _statusMessage = _showResultsScreen
                ? "Final run results displayed."
                : "Results hidden; the stopped final scene is displayed.";
        }

        DrawSimulationSpeedControls();
        DrawStartOrResumeButton();
        DrawRecordingControls();

        if (ImGui.Button("Back to Setup", new Vector2(-1, 0)))
        {
            _recorder.RequestStop();
            _simulation.Reset();
            _running = false;
            _runStopped = false;
            _showResultsScreen = false;
            _scenarioConfirmed = false;
            _dasherSelectionMode = false;
            _operatorPlacementMode = false;
            CopyConfigToInputs();
            _statusMessage = "Setup unlocked. Adjust values or responder posts.";
        }

        ImGui.Spacing();
        if (DangerButton("Exit Application", new Vector2(-1, 0)))
        {
            _recorder.RequestStop();
            ExitRequested = true;
        }

        DrawDasherControls();
    }

    private void DrawStartOrResumeButton()
    {
        SectionHeading("RUN SIMULATION");
        bool canStartOrResume = _scenarioConfirmed && !_running && !_runStopped;
        if (!canStartOrResume)
        {
            ImGui.BeginDisabled();
        }

        string startLabel = _runStopped
            ? "Run stopped"
            : _running
                ? "Running"
                : _simulation.Metrics.TimeSeconds > 0.001f ? "Resume" : "Start";
        if (PrimaryButton(startLabel, new Vector2(-1f, Scale(34f))))
        {
            StartOrResumeRun();
        }

        if (!canStartOrResume)
        {
            ImGui.EndDisabled();
        }
    }

    private void DrawSimulationSpeedControls()
    {
        ImGui.Spacing();
        ImGui.TextColored(ProfessionalPalette.AccentHover, "SIMULATION SPEED");
        ColoredWrappedText(
            ProfessionalPalette.SecondaryText,
            "Fast-forward changes simulation time as a whole; movement and timers stay synchronized.");

        float availableWidth = Math.Max(Scale(180f), ImGui.GetContentRegionAvail().X);
        float gap = Scale(6f);
        float buttonWidth = Math.Max(Scale(40f), (availableWidth - gap * 3f) / 4f);
        for (int speed = CrowdSimulation.MinSimulationSpeedMultiplier;
             speed <= CrowdSimulation.MaxSimulationSpeedMultiplier;
             speed++)
        {
            if (speed > CrowdSimulation.MinSimulationSpeedMultiplier)
            {
                ImGui.SameLine(0f, gap);
            }

            bool selected = _simulationSpeedMultiplier == speed;
            if (selected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ProfessionalPalette.Accent);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ProfessionalPalette.AccentHover);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ProfessionalPalette.AccentActive);
            }

            if (ImGui.Button(
                    $"{speed}x##sim-speed-{speed}",
                    new Vector2(buttonWidth, Scale(28f))))
            {
                _simulationSpeedMultiplier = speed;
                _statusMessage = _running
                    ? $"Simulation speed changed to {speed}x."
                    : $"Simulation speed set to {speed}x; it will apply when the run starts/resumes.";
            }

            if (selected)
            {
                ImGui.PopStyleColor(3);
            }
        }
    }

    private void DrawRecordingControls()
    {
        ImGui.Spacing();
        ImGui.TextColored(ProfessionalPalette.AccentHover, "FULL UI RECORDING");

        if (_recorder.IsActive)
        {
            if (!_recorder.IsStopping && DangerButton(
                    "Stop Recording",
                    new Vector2(-1f, Scale(30f))))
            {
                _recorder.RequestStop();
                _statusMessage = "Stopping and finalizing recording...";
            }
            else if (_recorder.IsStopping)
            {
                ImGui.BeginDisabled();
                ImGui.Button("Finalizing Recording...", new Vector2(-1f, Scale(30f)));
                ImGui.EndDisabled();
            }

            ImGui.TextColored(
                ProfessionalPalette.Danger,
                $"REC {FormatRecordingDuration(_recorder.Elapsed)}  |  {FullUiRecorder.TargetFramesPerSecond:F0} fps");
        }
        else
        {
            if (PrimaryButton("Start Recording", new Vector2(-1f, Scale(30f))))
            {
                _recorder.RequestStart();
                _statusMessage = "Full UI recording requested. Recording starts on the next rendered frame.";
            }
        }

        if (!string.IsNullOrWhiteSpace(_recorder.LastSavedPath))
        {
            ImGui.TextDisabled("Last recording saved:");
            ColoredWrappedText(ProfessionalPalette.PrimaryText, _recorder.LastSavedPath);
        }

        if (!string.IsNullOrWhiteSpace(_recorder.LastError))
        {
            ColoredWrappedText(ProfessionalPalette.Danger, _recorder.LastError);
        }

        ImGui.Spacing();
    }

    private static string FormatRecordingDuration(TimeSpan duration)
    {
        int totalHours = (int)duration.TotalHours;
        return $"{totalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private void DrawDasherControls()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(ProfessionalPalette.Dasher, "DASHER INCIDENT");
        ColoredWrappedText(ProfessionalPalette.SecondaryText, _simulation.DasherStatusText);
        ImGui.TextDisabled($"Configured clearance: {_simulation.Config.DasherClearanceSeconds:F0} s");
        ImGui.Spacing();

        if (!_simulation.CanSelectDasher)
        {
            _dasherSelectionMode = false;
            ImGui.BeginDisabled();
            ImGui.Button("Select Dasher", new Vector2(-1f, Scale(32f)));
            ImGui.EndDisabled();
            ColoredWrappedText(
                ProfessionalPalette.SecondaryText,
                "Selection is unavailable until the current Dasher incident is cleared.");
            return;
        }

        if (_dasherSelectionMode)
        {
            if (DasherButton(
                    "Cancel Dasher Selection (Esc)",
                    new Vector2(-1f, Scale(32f))))
            {
                CancelDasherSelection("Dasher selection cancelled.");
            }

            ImGui.TextColored(ProfessionalPalette.Dasher, "SELECTION MODE ACTIVE");
            ColoredWrappedText(
                ProfessionalPalette.PrimaryText,
                "Hover a passenger still in the funnel. Eligible passengers glow cyan; click one to mark the Dasher. Press Esc to cancel.");
            if (!string.IsNullOrWhiteSpace(_dasherSelectionFeedback))
            {
                ColoredWrappedText(ProfessionalPalette.Warning, _dasherSelectionFeedback);
            }
        }
        else if (DasherButton("Select Dasher", new Vector2(-1f, Scale(32f))))
        {
            _dasherSelectionMode = true;
            _dasherSelectionFeedback = string.Empty;
            _statusMessage =
                "Dasher selection active. Hover a funnel passenger for a cyan glow, then click to select. Press Esc to cancel.";
        }
    }

    private void DrawViewControls()
    {
        ImGui.TextColored(ProfessionalPalette.AccentHover, "VIEW");
        float zoomPercent = _renderer.Zoom * 100f;

        ImGui.PushItemWidth(Scale(BaseSetupControlWidth));
        if (ImGui.SliderFloat(
            "Zoom",
            ref zoomPercent,
            IsometricCamera.MinZoom * 100f,
            IsometricCamera.MaxZoom * 100f,
            "%.0f%%"))
        {
            _renderer.SetZoom(zoomPercent / 100f);
        }
        ImGui.PopItemWidth();

        if (ImGui.SmallButton("Reset zoom to 100%"))
        {
            _renderer.SetZoom(1f);
        }

        ImGui.TextDisabled("Mouse wheel over the simulation also zooms.");
        ImGui.TextColored(
            ProfessionalPalette.Alpha(ProfessionalPalette.Operator, 0.82f),
            $"Acquisition rings: {_simulation.Config.OperatorAcquisitionRadius:F1} m (Purple / teal robot)");
    }

    private void HandleKeyboardShortcuts()
    {
        if (_dasherSelectionMode && ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            CancelDasherSelection("Dasher selection cancelled with Esc.");
        }
    }

    private void CancelDasherSelection(string message)
    {
        _dasherSelectionMode = false;
        _dasherSelectionFeedback = string.Empty;
        _statusMessage = message;
    }

    private void DrawRuntimeDashboard()
    {
        Vector2 available = ImGui.GetContentRegionAvail();
        float dashboardGap = Scale(BaseDashboardGap);
        float totalWidth = Math.Max(Scale(760f), available.X);
        float usableWidth = Math.Max(Scale(700f), totalWidth - dashboardGap * 3f);

        // Give the operator list the most room because its content is dynamic and may need a
        // vertical scrollbar. The other three metric groups are compact read-only summaries.
        float liveWidth = usableWidth * 0.18f;
        float detectionWidth = usableWidth * 0.24f;
        float outcomesWidth = usableWidth * 0.25f;
        float operatorsWidth = usableWidth - liveWidth - detectionWidth - outcomesWidth;

        DrawDashboardPanel("##dashboard-live", liveWidth, "LIVE METRICS", DrawLiveMetricsPanel);
        ImGui.SameLine(0f, dashboardGap);
        DrawDashboardPanel("##dashboard-detection", detectionWidth, "DETECTION OUTCOMES", DrawDetectionOutcomesPanel);
        ImGui.SameLine(0f, dashboardGap);
        string outcomesTitle = _simulation.Config.Mode == CheckpointMode.OpenFunnel
            ? "UNDETECTED OUTCOMES"
            : "OPERATOR OUTCOMES";
        DrawDashboardPanel(
            "##dashboard-outcomes",
            outcomesWidth,
            outcomesTitle,
            DrawOperatorOutcomesPanel,
            boldTitle: true);
        ImGui.SameLine(0f, dashboardGap);
        string operatorStatusTitle = _simulation.Config.Mode == CheckpointMode.MultiLaneBarricade
            ? "RESPONDER STATUS / MANUAL"
            : "RESPONDER STATUS / FATIGUE";
        DrawDashboardPanel("##dashboard-operators", operatorsWidth, operatorStatusTitle, DrawOperatorStatusPanel);
    }

    private void DrawResultsScreen()
    {
        SimulationMetrics metrics = _simulation.Metrics;
        bool mode2 = _simulation.Config.Mode == CheckpointMode.MultiLaneBarricade;

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ProfessionalPalette.SidebarBackground);
        ImGui.BeginChild(
            "##run-results-screen",
            Vector2.Zero,
            ImGuiChildFlags.Borders,
            ImGuiWindowFlags.AlwaysVerticalScrollbar);

        bool boldFontPushed = ProductTheme.PushBoldFont();
        ImGui.TextColored(ProfessionalPalette.AccentHover, "RUN RESULTS");
        ProductTheme.PopBoldFont(boldFontPushed);
        ColoredText(
            ProfessionalPalette.SecondaryText,
            $"{(mode2 ? "Mode 2 - Multi-Lane Barricade" : "Mode 1 - Open Funnel")}  |  Seed {_simulation.Config.RandomSeed}  |  {FormatElapsed(metrics.TimeSeconds)}");
        if (ImGui.Button("View stopped final scene", new Vector2(Scale(210f), Scale(30f))))
        {
            _showResultsScreen = false;
            _statusMessage = "Results hidden; the stopped final scene is displayed.";
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        float gap = Scale(10f);
        float availableWidth = ImGui.GetContentRegionAvail().X;
        bool useTwoColumns = availableWidth >= Scale(720f);
        float cardWidth = useTwoColumns ? (availableWidth - gap) * 0.5f : -1f;

        DrawResultCard("##results-flow", "FLOW / QUEUE", cardWidth, () =>
        {
            int processedCount = mode2 ? metrics.TotalOutcomeCount : metrics.TotalCrossings;
            float throughput = mode2
                ? metrics.AverageOutcomesPerMinute
                : metrics.AverageCrossingsPerMinute;
            DashboardResponsiveKeyValue(
                mode2 ? "Processed outcomes" : "Checkpoint crossings",
                processedCount.ToString());
            if (mode2)
            {
                DashboardResponsiveKeyValue("Passed barricade", metrics.TotalCrossings.ToString());
            }
            DashboardResponsiveKeyValue("Throughput", $"{throughput:F1} / min");
            DashboardResponsiveKeyValue(
                "Average queue time",
                FormatDurationMetric(metrics.AverageQueueTimeSeconds, metrics.QueueTimeSampleCount));
            DashboardResponsiveKeyValue(
                "Maximum queue time",
                FormatDurationMetric(metrics.MaximumQueueTimeSeconds, metrics.QueueTimeSampleCount));
            DashboardResponsiveKeyValue("Completed queue samples", metrics.QueueTimeSampleCount.ToString());
        });

        if (useTwoColumns)
        {
            ImGui.SameLine(0f, gap);
        }

        DrawResultCard("##results-undetected", "UNDETECTED RESPONSE", cardWidth, () =>
        {
            if (mode2)
            {
                DashboardResponsiveKeyValue("Second-attempt Yellow", metrics.TotalYellow.ToString());
                DashboardResponsiveKeyValue("Caught", "N/A");
                DashboardResponsiveKeyValue("Escaped", "N/A");
                DashboardResponsiveKeyValue("Catch rate", "N/A");
                ColoredWrappedText(
                    ProfessionalPalette.SecondaryText,
                    "Not applicable in Mode 2: Yellow clears the second automated attempt and is not an operator-intervention target.");
            }
            else
            {
                int pending = Math.Max(
                    0,
                    metrics.TotalYellow - metrics.TotalCaughtYellow - metrics.TotalEscapedYellow);
                DashboardResponsiveKeyValue("Caught", metrics.TotalCaughtYellow.ToString());
                DashboardResponsiveKeyValue("Escaped", metrics.TotalEscapedYellow.ToString());
                DashboardResponsiveKeyValue("Still unresolved", pending.ToString());
                DashboardResponsiveKeyValue(
                    "Catch rate",
                    FormatRate(
                        metrics.TotalUndetectedCatchRatePercent,
                        metrics.TotalResolvedUndetectedCount));
                ColoredWrappedText(
                    ProfessionalPalette.SecondaryText,
                    "Catch rate uses resolved Undetected cases only: caught divided by caught plus escaped.");
            }
        });

        ImGui.Spacing();

        DrawResultCard("##results-operators", "RESPONDER PERFORMANCE / FATIGUE", cardWidth, () =>
        {
            int operatorCount = _simulation.Operators.Count;
            DashboardResponsiveKeyValue("Responders", operatorCount.ToString());
            DashboardResponsiveKeyValue("Purple / robots",
                $"{_simulation.Operators.Count(op => !op.IsRobot)} / {_simulation.Operators.Count(op => op.IsRobot)}");
            DashboardResponsiveKeyValue(
                "Utilisation",
                operatorCount == 0 ? "N/A" : $"{metrics.OperatorUtilisationPercent:F1}%");

            CheckpointOperator[] humans = _simulation.Operators.Where(op => !op.IsRobot).ToArray();
            if (mode2)
            {
                DashboardResponsiveKeyValue("Fatigue", "Disabled in Mode 2");
                DashboardResponsiveKeyValue("Purple capacity remaining", humans.Length == 0 ? "N/A" : "100.0%");
            }
            else
            {
                float averageCapacity = humans.Length == 0
                    ? 0f
                    : humans.Average(op => op.HealthFraction) * 100f;
                float lowestCapacity = humans.Length == 0
                    ? 0f
                    : humans.Min(op => op.HealthFraction) * 100f;
                DashboardResponsiveKeyValue(
                    "Purple average capacity",
                    humans.Length == 0 ? "N/A" : $"{averageCapacity:F1}%");
                DashboardResponsiveKeyValue(
                    "Purple lowest capacity",
                    humans.Length == 0 ? "N/A" : $"{lowestCapacity:F1}%");
                DashboardResponsiveKeyValue(
                    "Purple average fatigue used",
                    humans.Length == 0 ? "N/A" : $"{100f - averageCapacity:F1}%");
            }

            DashboardResponsiveKeyValue("Robot clearance (fixed)", $"{_simulation.Config.RobotManualClearanceSeconds:F1}s on site");
            ColoredWrappedText(
                ProfessionalPalette.SecondaryText,
                "Utilisation includes all responders. Capacity and fatigue refer to Purple operators; robot clearance time stays fixed.");
        });

        if (useTwoColumns)
        {
            ImGui.SameLine(0f, gap);
        }

        DrawResultCard("##results-manual", "MANUAL CLEARANCE WAIT", cardWidth, () =>
        {
            DashboardResponsiveKeyValue(
                "Completed cases",
                metrics.ManualClearanceWaitSampleCount.ToString());
            DashboardResponsiveKeyValue(
                "Average time to completion",
                FormatDurationMetric(
                    metrics.AverageManualClearanceWaitSeconds,
                    metrics.ManualClearanceWaitSampleCount));
            DashboardResponsiveKeyValue(
                "Maximum time to completion",
                FormatDurationMetric(
                    metrics.MaximumManualClearanceWaitSeconds,
                    metrics.ManualClearanceWaitSampleCount));
            DashboardResponsiveKeyValue("Total manually cleared", metrics.TotalManualVerified.ToString());
            ColoredWrappedText(
                ProfessionalPalette.SecondaryText,
                mode2
                    ? "Measured from Red classification after two failed attempts until manual clearance completes."
                    : "Measured from Undetected checkpoint classification until manual clearance completes; escaped and pending cases are excluded.");
        });

        ImGui.Spacing();
        DrawLanePerformanceResults(mode2);
        ImGui.Spacing();
        ColoredWrappedText(
            ProfessionalPalette.SecondaryText,
            "Use the permanent controls below to restart with the same seed for a repeatable run, or run with a new seed for a new sample.");

        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private void DrawResultCard(string id, string title, float width, Action content)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ProfessionalPalette.CardBackground);
        ImGui.PushStyleColor(ImGuiCol.Border, ProfessionalPalette.Border);
        ImGui.BeginChild(
            id,
            new Vector2(width, Scale(250f)),
            ImGuiChildFlags.Borders);
        ImGui.TextColored(ProfessionalPalette.AccentHover, title);
        ImGui.Separator();
        ImGui.Spacing();
        content();
        ImGui.EndChild();
        ImGui.PopStyleColor(2);
    }

    private void DrawLanePerformanceResults(bool mode2)
    {
        ImGui.TextColored(ProfessionalPalette.AccentHover, "LANE PERFORMANCE");
        if (!mode2)
        {
            ColoredWrappedText(
                ProfessionalPalette.SecondaryText,
                "Not applicable in Open Funnel mode because travellers use a shared funnel rather than independently timed barricade lanes.");
            return;
        }

        ColoredWrappedText(
            ProfessionalPalette.SecondaryText,
            "Processed counts include first-attempt Green, second-attempt Yellow and two-attempt Red outcomes resolved in each lane.");
        ImGui.Spacing();

        const ImGuiTableFlags tableFlags =
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable("##lane-performance-results", 5, tableFlags))
        {
            return;
        }

        ImGui.TableSetupColumn("Lane", ImGuiTableColumnFlags.WidthFixed, Scale(70f));
        ImGui.TableSetupColumn("Processed", ImGuiTableColumnFlags.WidthFixed, Scale(100f));
        ImGui.TableSetupColumn("Throughput / min");
        ImGui.TableSetupColumn("Average queue");
        ImGui.TableSetupColumn("Maximum queue");
        ImGui.TableHeadersRow();

        SimulationMetrics metrics = _simulation.Metrics;
        for (int laneIndex = 0; laneIndex < metrics.LaneCount; laneIndex++)
        {
            int processed = metrics.GetLaneProcessedCount(laneIndex);
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ColoredText(ProfessionalPalette.AccentHover, $"{laneIndex + 1:00}");
            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(processed.ToString());
            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted($"{metrics.GetLaneThroughputPerMinute(laneIndex):F1}");
            ImGui.TableSetColumnIndex(3);
            ImGui.TextUnformatted(
                FormatDurationMetric(metrics.GetLaneAverageQueueTimeSeconds(laneIndex), processed));
            ImGui.TableSetColumnIndex(4);
            ImGui.TextUnformatted(
                FormatDurationMetric(metrics.GetLaneMaximumQueueTimeSeconds(laneIndex), processed));
        }

        ImGui.EndTable();
    }

    private void DrawDashboardPanel(
        string id,
        float width,
        string title,
        Action content,
        bool boldTitle = false)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,
            new Vector2(ImGui.GetStyle().ItemSpacing.X, Scale(4f)));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ProfessionalPalette.CardBackground);
        ImGui.PushStyleColor(ImGuiCol.Border, ProfessionalPalette.Border);
        ImGui.BeginChild(
            id,
            new Vector2(width, Scale(BaseRuntimeDashboardHeight)),
            ImGuiChildFlags.Borders);
        bool boldFontPushed = boldTitle && ProductTheme.PushBoldFont();
        ImGui.TextColored(ProfessionalPalette.AccentHover, title);
        ProductTheme.PopBoldFont(boldFontPushed);
        ImGui.Separator();
        ImGui.Spacing();
        content();
        ImGui.EndChild();
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar();
    }

    private void DrawLiveMetricsPanel()
    {
        SimulationMetrics m = _simulation.Metrics;
        DashboardKeyValue("Elapsed", $"{m.TimeSeconds:F1} s", 112f);
        if (_simulation.Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            DashboardKeyValue("Processed 60s", m.CurrentOutcomeCount.ToString(), 112f);
            DashboardKeyValue("Processed run", m.TotalOutcomeCount.ToString(), 112f);
            DashboardKeyValue("Total passed", m.TotalCrossings.ToString(), 112f);
        }
        else
        {
            DashboardKeyValue("Current 60s", m.CurrentCrossings.ToString(), 112f);
            DashboardKeyValue("Total crossed", m.TotalCrossings.ToString(), 112f);
        }
        DashboardKeyValue(
            _simulation.Config.Mode == CheckpointMode.MultiLaneBarricade
                ? "Outcomes / min"
                : "Average / min",
            $"{(_simulation.Config.Mode == CheckpointMode.MultiLaneBarricade ? m.AverageOutcomesPerMinute : m.AverageCrossingsPerMinute):F1}",
            112f);
        ImGui.Spacing();
        ImGui.TextDisabled($"Speed: {_simulationSpeedMultiplier}x");
    }

    private void DrawDetectionOutcomesPanel()
    {
        SimulationMetrics m = _simulation.Metrics;
        bool mode2 = _simulation.Config.Mode == CheckpointMode.MultiLaneBarricade;
        if (mode2)
        {
            ColoredWrappedText(
                ProfessionalPalette.SecondaryText,
                $"Target: Green 90% | Yellow {_simulation.Config.MultiLaneYellowPercent:F1}% | Red {_simulation.Config.MultiLaneRedPercent:F1}%");
        }

        ImGui.TextDisabled("CURRENT 60s");
        DashboardSemanticKeyValue(
            mode2 ? "1st pass Green" : "Cleared",
            $"{m.CurrentGreen} ({m.CurrentGreenPercent:F1}%)",
            124f,
            ProfessionalPalette.Cleared);
        DashboardSemanticKeyValue(
            mode2 ? "2nd pass Yellow" : "Undetected",
            $"{m.CurrentYellow} ({m.CurrentYellowPercent:F1}%)",
            124f,
            ProfessionalPalette.Undetected);
        DashboardSemanticKeyValue(
            mode2 ? "Manual Red" : "Uncounted",
            $"{m.CurrentRed} ({m.CurrentRedPercent:F1}%)",
            124f,
            ProfessionalPalette.Uncounted);

        ImGui.Spacing();
        ImGui.TextDisabled("FULL RUN");
        DashboardSemanticKeyValue(
            mode2 ? "1st pass Green" : "Cleared",
            $"{m.TotalGreen} ({m.TotalGreenPercent:F1}%)",
            124f,
            ProfessionalPalette.Cleared);
        DashboardSemanticKeyValue(
            mode2 ? "2nd pass Yellow" : "Undetected",
            $"{m.TotalYellow} ({m.TotalYellowPercent:F1}%)",
            124f,
            ProfessionalPalette.Undetected);
        DashboardSemanticKeyValue(
            mode2 ? "Manual Red" : "Uncounted",
            $"{m.TotalRed} ({m.TotalRedPercent:F1}%)",
            124f,
            ProfessionalPalette.Uncounted);
    }

    private void DrawOperatorOutcomesPanel()
    {
        bool boldFontPushed = ProductTheme.PushBoldFont();
        try
        {
            SimulationMetrics m = _simulation.Metrics;
            if (_simulation.Config.Mode == CheckpointMode.MultiLaneBarricade)
            {
                int awaitingRed = _simulation.Pedestrians.Count(p => p.RequiresManualClearance);
                int activeRed = _simulation.Operators.Count(op =>
                    op.TargetPedestrianId.HasValue &&
                    _simulation.Pedestrians.Any(p =>
                        p.Id == op.TargetPedestrianId.Value && p.RequiresManualClearance));
                DashboardKeyValue("Red awaiting", awaitingRed.ToString(), 148f);
                DashboardKeyValue("Responders on Red", activeRed.ToString(), 148f);
                DashboardKeyValue("Manual cleared", m.TotalManualVerified.ToString(), 148f);
                DashboardKeyValue(
                    "Purple manual time",
                    $"{_simulation.Config.MultiLaneManualClearanceSeconds:F1}s",
                    148f);
                DashboardKeyValue("Robot on-site time", $"{_simulation.Config.RobotManualClearanceSeconds:F1}s", 148f);
            }
            else
            {
                DisabledText("CURRENT 60s");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "This is the current fixed 60-second simulation interval; it resets at each minute boundary.");
                }
                DashboardKeyValue("Caught", m.CurrentCaughtYellow.ToString(), 118f);
                DashboardKeyValue("Escaped", m.CurrentEscapedYellow.ToString(), 118f);
                DashboardKeyValue(
                    "Catch rate",
                    FormatRate(m.CurrentUndetectedCatchRatePercent, m.CurrentResolvedUndetectedCount),
                    118f);

                ImGui.Spacing();
                DisabledText("ACCUMULATED");
                DashboardKeyValue("Caught", m.TotalCaughtYellow.ToString(), 118f);
                DashboardKeyValue("Escaped", m.TotalEscapedYellow.ToString(), 118f);
                DashboardKeyValue(
                    "Catch rate",
                    FormatRate(m.TotalUndetectedCatchRatePercent, m.TotalResolvedUndetectedCount),
                    118f);
            }
        }
        finally
        {
            ProductTheme.PopBoldFont(boldFontPushed);
        }
    }

    private void DrawOperatorStatusPanel()
    {
        ImGui.TextDisabled($"{_simulation.Operators.Count(op => !op.IsRobot)} Purple / {_simulation.Operators.Count(op => op.IsRobot)} robot(s)");
        ImGui.Spacing();

        // Fixed-height scroll region keeps the dashboard stable no matter how many operators are
        // placed. Extra operators are accessed with the vertical scrollbar or mouse wheel.
        ImGui.BeginChild(
            "##operator-status-scroll",
            Vector2.Zero,
            ImGuiChildFlags.None,
            ImGuiWindowFlags.AlwaysVerticalScrollbar);

        if (_simulation.Operators.Count == 0)
        {
            ImGui.TextDisabled("No responders placed.");
        }

        foreach (CheckpointOperator op in _simulation.Operators)
        {
            ImGui.TextColored(op.IsRobot ? ProfessionalPalette.RobotAccent : ProfessionalPalette.Operator, op.DisplayName);
            ImGui.SameLine();
            string operatorStateText = FormatOperatorState(op);
            if (op.HasRightOfWay)
            {
                operatorStateText += "  |  ROW";
            }
            ImGui.TextDisabled(operatorStateText);

            if (op.IsRobot)
            {
                ImGui.TextDisabled($"On-site clearance {op.CurrentVerificationDuration:F1}s (fixed) | Completed {op.CompletedVerificationCount}");
                if (op.State == OperatorState.Verifying)
                {
                    float progress = Math.Clamp(op.VerificationElapsedSeconds / op.CurrentVerificationDuration, 0f, 1f);
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ProfessionalPalette.RobotAccent);
                    ImGui.ProgressBar(progress, new Vector2(-1f, Scale(13f)),
                        $"{Math.Max(0f, op.CurrentVerificationDuration - op.VerificationElapsedSeconds):F1}s remaining");
                    ImGui.PopStyleColor();
                }
                ImGui.TextDisabled($"Interception {op.CurrentInterceptionSpeed:F2} m/s | Catches {op.CatchCount}");
            }
            else if (_simulation.Config.Mode == CheckpointMode.MultiLaneBarricade)
            {
                ImGui.TextDisabled($"Manual clearance {op.CurrentVerificationDuration:F1}s | Completed {op.CompletedVerificationCount}");
                ImGui.TextDisabled("Fatigue disabled in Mode 2");
            }
            else
            {
                float barWidth = Math.Clamp(
                    ImGui.GetContentRegionAvail().X * 0.40f,
                    Scale(82f),
                    Scale(112f));
                Vector4 capacityColor = GetCapacityColor(op.HealthFraction);
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, capacityColor);
                ImGui.PushStyleColor(ImGuiCol.FrameBg, ProfessionalPalette.DarkTrack);
                ImGui.ProgressBar(
                    op.HealthFraction,
                    new Vector2(barWidth, Scale(13f)),
                    $"{op.HealthPercent}%");
                ImGui.PopStyleColor(2);
                ImGui.SameLine();
                ImGui.TextDisabled($"Clearance {op.CurrentVerificationDuration:F1}s");
                ImGui.TextDisabled($"Interception {op.CurrentInterceptionSpeed:F2} m/s | Catches {op.CatchCount}");
            }
            ImGui.Separator();
        }

        ImGui.EndChild();
    }

    private void DashboardKeyValue(string label, string value, float valueColumnX)
    {
        DisabledText(label);
        ImGui.SameLine(Scale(valueColumnX));
        ImGui.TextUnformatted(value);
    }

    private void DashboardResponsiveKeyValue(string label, string value)
    {
        float rowStartX = ImGui.GetCursorPosX();
        float availableWidth = ImGui.GetContentRegionAvail().X;
        float labelWidth = ImGui.CalcTextSize(label).X;
        float valueWidth = ImGui.CalcTextSize(value).X;
        float spacing = ImGui.GetStyle().ItemSpacing.X;

        DisabledText(label);
        if (labelWidth + spacing + valueWidth <= availableWidth)
        {
            ImGui.SameLine(rowStartX + availableWidth - valueWidth);
            ImGui.TextUnformatted(value);
            return;
        }

        // On a narrow dashboard, keep the complete wording readable and place its value on the
        // following indented line instead of allowing the two columns to overlap.
        ImGui.Indent(Scale(12f));
        ImGui.TextUnformatted(value);
        ImGui.Unindent(Scale(12f));
    }

    private void HandleCanvasInput()
    {
        if (!_modeSelected)
        {
            return;
        }

        if (!_scenarioConfirmed)
        {
            if (!_renderer.IsHovered)
            {
                return;
            }

            if (!_operatorPlacementMode)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) ||
                    ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    _statusMessage =
                        "Choose 'Place Purple operator' or 'Place wheeled robot' before editing posts.";
                }

                return;
            }

            if (!_renderer.TryMouseToWorld(out Vector2 world))
            {
                return;
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (_simulation.AddOperatorPost(world, _placementKind))
                {
                    _statusMessage =
                        $"Placed {(_placementKind == ResponderKind.WheeledRobot ? "robot" : "Purple operator")} at ({world.X:F2}, {world.Y:F2}) m. Acquisition radius: {_simulation.Config.OperatorAcquisitionRadius:F1} m.";
                }
                else
                {
                    _statusMessage =
                        "Post rejected: keep both responder types inside the hall and separated from existing posts.";
                }
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                _statusMessage = _simulation.RemoveNearestOperatorPost(world)
                    ? "Removed nearest responder post."
                    : "No responder post close enough to remove.";
            }

            return;
        }

        if (!_dasherSelectionMode ||
            !ImGui.IsMouseClicked(ImGuiMouseButton.Left) ||
            !_renderer.IsHovered)
        {
            return;
        }

        if (!_simulation.CanSelectDasher)
        {
            CancelDasherSelection(
                "Dasher selection is unavailable until the current incident is cleared.");
            return;
        }

        if (!_renderer.TryPickPedestrian(_simulation, out int pedestrianId))
        {
            _dasherSelectionFeedback =
                "No passenger under the cursor. Click directly on a passenger still inside the funnel / orderly queue.";
            _statusMessage = _dasherSelectionFeedback;
            return;
        }

        Pedestrian? pedestrian = _simulation.Pedestrians.FirstOrDefault(
            candidate => candidate.Id == pedestrianId);
        if (pedestrian is null)
        {
            _statusMessage = "That passenger is no longer available for selection.";
            return;
        }

        if (pedestrian.Phase != PedestrianPhase.Funnel)
        {
            _dasherSelectionFeedback =
                $"Passenger #{pedestrian.Id} is already in the main hall and is not eligible. Select a passenger still in the funnel.";
            _statusMessage = _dasherSelectionFeedback;
            return;
        }

        if (_simulation.Config.Mode == CheckpointMode.MultiLaneBarricade &&
            (pedestrian.RequiresManualClearance || pedestrian.DetectionStatus != DetectionStatus.InFunnel))
        {
            _dasherSelectionFeedback =
                $"Passenger #{pedestrian.Id} has already completed checkpoint processing and cannot be changed into the Dasher. Select a traveller still waiting in a lane.";
            _statusMessage = _dasherSelectionFeedback;
            return;
        }

        if (_simulation.TrySelectDasher(pedestrian.Id))
        {
            _dasherSelectionMode = false;
            _dasherSelectionFeedback = string.Empty;
            _statusMessage =
                $"Passenger #{pedestrian.Id} marked as Dasher. It remains in the queue and activates after crossing the checkpoint.";
        }
        else
        {
            _dasherSelectionFeedback =
                $"Passenger #{pedestrian.Id} could not be selected. Only one active Dasher incident is allowed at a time.";
            _statusMessage = _dasherSelectionFeedback;
        }
    }

    private void ApplySetupInputs()
    {
        SimulationConfig next = _simulation.Config.Clone();
        next.HallLength = Math.Clamp(
            _hallLength,
            SimulationConfig.MinHallLength,
            SimulationConfig.MaxHallLength);
        next.HallWidth = Math.Clamp(
            _hallWidth,
            SimulationConfig.MinHallWidth,
            SimulationConfig.MaxHallWidth);
        if (next.Mode == CheckpointMode.MultiLaneBarricade)
        {
            next.HallWidth = MathF.Round(next.HallWidth);
        }

        next.FunnelLength = SimulationConfig.BaselineFunnelLength;
        next.FunnelWidth = next.Mode == CheckpointMode.MultiLaneBarricade
            ? next.HallWidth
            : Math.Clamp(_funnelWidth, SimulationConfig.MinFunnelWidth,
                Math.Min(SimulationConfig.MaxFunnelWidth, next.HallWidth));
        next.LaneClearanceSeconds = Math.Clamp(
            _laneClearanceSeconds,
            SimulationConfig.MinLaneClearanceSeconds,
            SimulationConfig.MaxLaneClearanceSeconds);
        next.MultiLaneYellowPercent = Math.Clamp(
            _multiLaneYellowPercent,
            SimulationConfig.MinMultiLaneYellowPercent,
            SimulationConfig.MaxMultiLaneYellowPercent);
        next.MultiLaneManualClearanceSeconds = Math.Clamp(
            _multiLaneManualClearanceSeconds,
            SimulationConfig.MinMultiLaneManualClearanceSeconds,
            SimulationConfig.MaxMultiLaneManualClearanceSeconds);
        float maxExitWidth = Math.Min(SimulationConfig.MaxExitWidth, next.HallWidth);
        next.ExitWidth = Math.Clamp(
            _exitWidth,
            SimulationConfig.MinExitWidth,
            maxExitWidth);
        next.TargetFunnelDensity = Math.Max(0.1f, _density);
        next.YellowPercent = Math.Clamp(
            _yellowPercent,
            SimulationConfig.MinYellowPercent,
            SimulationConfig.MaxYellowPercent);
        next.OperatorAcquisitionRadius = Math.Clamp(
            _operatorAcquisitionRadius,
            SimulationConfig.MinOperatorAcquisitionRadius,
            SimulationConfig.MaxOperatorAcquisitionRadius);
        next.YellowVisualSearchSeconds = Math.Clamp(
            _yellowVisualSearchSeconds,
            SimulationConfig.MinYellowVisualSearchSeconds,
            SimulationConfig.MaxYellowVisualSearchSeconds);
        next.RobotManualClearanceSeconds = Math.Clamp(
            _robotManualClearanceSeconds,
            SimulationConfig.MinRobotManualClearanceSeconds,
            SimulationConfig.MaxRobotManualClearanceSeconds);
        next.InitialManualClearanceSeconds = Math.Clamp(
            _initialManualClearanceSeconds,
            SimulationConfig.MinInitialManualClearanceSeconds,
            SimulationConfig.MaxInitialManualClearanceSeconds);
        next.PassengerMinimumSpeed = Math.Clamp(
            _passengerMinimumSpeed,
            SimulationConfig.MinPassengerPreferredSpeed,
            SimulationConfig.MaxPassengerPreferredSpeed);
        next.PassengerMaximumSpeed = Math.Clamp(
            _passengerMaximumSpeed,
            next.PassengerMinimumSpeed,
            SimulationConfig.MaxPassengerPreferredSpeed);
        next.OperatorInterceptionSpeed = Math.Clamp(
            _operatorInterceptionSpeed,
            SimulationConfig.MinOperatorInterceptionSpeed,
            SimulationConfig.MaxOperatorInterceptionSpeed);
        next.ManualClearanceFatigueIncreasePercent = Math.Clamp(
            _manualClearanceFatigueIncreasePercent, 0f,
            SimulationConfig.MaxManualClearanceFatigueIncreasePercent);
        next.OperatorInterceptionSpeedLossPercent = Math.Clamp(
            _operatorInterceptionSpeedLossPercent, 0f,
            SimulationConfig.MaxOperatorInterceptionSpeedLossPercent);
        next.DasherClearanceSeconds = Math.Clamp(
            _dasherClearanceSeconds,
            SimulationConfig.MinDasherClearanceSeconds,
            SimulationConfig.MaxDasherClearanceSeconds);
        next.RandomSeed = _seed;

        int previousOperatorCount = _simulation.OperatorPosts.Count;
        _simulation.Reconfigure(next);
        int removedOperatorCount = previousOperatorCount - _simulation.OperatorPosts.Count;

        CopyConfigToInputs();
        _statusMessage = removedOperatorCount > 0
            ? $"Setup updated. {removedOperatorCount} responder post(s) fell outside the resized hall and were removed; reposition them before confirming."
            : next.Mode == CheckpointMode.MultiLaneBarricade
                ? $"Setup updated: {next.MultiLaneCount} x 1.0 m lanes, {next.TargetFunnelPopulation} queued travellers, {next.LaneClearanceSeconds:F1}s/attempt, outcomes 90/{next.MultiLaneYellowPercent:F1}/{next.MultiLaneRedPercent:F1}."
                : "Setup values applied immediately; geometry, population, outcomes and operator settings updated.";
    }

    private void CopyConfigToInputs()
    {
        _hallLength = _simulation.Config.HallLength;
        _hallWidth = _simulation.Config.HallWidth;
        _funnelWidth = _simulation.Config.FunnelWidth;
        _exitWidth = _simulation.Config.ExitWidth;
        _density = _simulation.Config.TargetFunnelDensity;
        _yellowPercent = _simulation.Config.YellowPercent;
        _operatorAcquisitionRadius = _simulation.Config.OperatorAcquisitionRadius;
        _yellowVisualSearchSeconds = _simulation.Config.YellowVisualSearchSeconds;
        _initialManualClearanceSeconds = _simulation.Config.InitialManualClearanceSeconds;
        _robotManualClearanceSeconds = _simulation.Config.RobotManualClearanceSeconds;
        _passengerMinimumSpeed = _simulation.Config.PassengerMinimumSpeed;
        _passengerMaximumSpeed = _simulation.Config.PassengerMaximumSpeed;
        _operatorInterceptionSpeed = _simulation.Config.OperatorInterceptionSpeed;
        _manualClearanceFatigueIncreasePercent = _simulation.Config.ManualClearanceFatigueIncreasePercent;
        _operatorInterceptionSpeedLossPercent = _simulation.Config.OperatorInterceptionSpeedLossPercent;
        _dasherClearanceSeconds = _simulation.Config.DasherClearanceSeconds;
        _laneClearanceSeconds = _simulation.Config.LaneClearanceSeconds;
        _multiLaneYellowPercent = _simulation.Config.MultiLaneYellowPercent;
        _multiLaneManualClearanceSeconds = _simulation.Config.MultiLaneManualClearanceSeconds;
        _seed = _simulation.Config.RandomSeed;
    }

    private float Scale(float value) => value * _uiScale;

    private static string FormatRate(float ratePercent, int resolvedCount) =>
        resolvedCount == 0 ? "N/A" : $"{ratePercent:F1}%";

    private static string FormatDurationMetric(float seconds, int sampleCount) =>
        sampleCount == 0 ? "N/A" : $"{seconds:F1} s";

    private void DashboardSemanticKeyValue(
        string label,
        string value,
        float valueColumnX,
        Vector4 semanticColor)
    {
        ColoredText(semanticColor, label);
        ImGui.SameLine(Scale(valueColumnX));
        ColoredText(ProfessionalPalette.PrimaryText, value);
    }

    private static void ColoredText(Vector4 color, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private static void DisabledText(string text)
    {
        ColoredText(ProfessionalPalette.SecondaryText, text);
    }

    private static void ColoredWrappedText(Vector4 color, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.PushTextWrapPos(0f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.PopStyleColor();
    }

    private static bool PrimaryButton(string label, Vector2 size)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, ProfessionalPalette.Accent);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ProfessionalPalette.AccentHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ProfessionalPalette.AccentActive);
        bool clicked = ImGui.Button(label, size);
        ImGui.PopStyleColor(3);
        return clicked;
    }

    private static bool OperatorButton(string label, Vector2 size)
    {
        Vector4 normal = ProfessionalPalette.Alpha(ProfessionalPalette.Operator, 0.34f);
        Vector4 hovered = ProfessionalPalette.Alpha(ProfessionalPalette.Operator, 0.62f);
        Vector4 active = ProfessionalPalette.Alpha(ProfessionalPalette.Operator, 0.82f);
        ImGui.PushStyleColor(ImGuiCol.Button, normal);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active);
        bool clicked = ImGui.Button(label, size);
        ImGui.PopStyleColor(3);
        return clicked;
    }

    private static bool RobotButton(string label, Vector2 size)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, ProfessionalPalette.Alpha(ProfessionalPalette.RobotAccent, 0.30f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ProfessionalPalette.Alpha(ProfessionalPalette.RobotAccent, 0.52f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ProfessionalPalette.Alpha(ProfessionalPalette.RobotAccent, 0.72f));
        bool clicked = ImGui.Button(label, size);
        ImGui.PopStyleColor(3);
        return clicked;
    }

    private static bool DasherButton(string label, Vector2 size)
    {
        Vector4 normal = ProfessionalPalette.Alpha(ProfessionalPalette.Dasher, 0.22f);
        Vector4 hovered = ProfessionalPalette.Alpha(ProfessionalPalette.Dasher, 0.42f);
        Vector4 active = ProfessionalPalette.Alpha(ProfessionalPalette.Dasher, 0.62f);
        ImGui.PushStyleColor(ImGuiCol.Button, normal);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active);
        bool clicked = ImGui.Button(label, size);
        ImGui.PopStyleColor(3);
        return clicked;
    }

    private static bool WarningButton(string label, Vector2 size)
    {
        Vector4 normal = new(0.42f, 0.27f, 0.05f, 1f);
        Vector4 hovered = new(0.58f, 0.38f, 0.06f, 1f);
        Vector4 active = new(0.70f, 0.45f, 0.05f, 1f);
        ImGui.PushStyleColor(ImGuiCol.Button, normal);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active);
        bool clicked = ImGui.Button(label, size);
        ImGui.PopStyleColor(3);
        return clicked;
    }

    private static bool DangerButton(string label, Vector2 size)
    {
        Vector4 normal = ProfessionalPalette.Alpha(ProfessionalPalette.Danger, 0.28f);
        Vector4 hovered = ProfessionalPalette.Alpha(ProfessionalPalette.Danger, 0.58f);
        Vector4 active = ProfessionalPalette.Alpha(ProfessionalPalette.Danger, 0.78f);
        ImGui.PushStyleColor(ImGuiCol.Button, normal);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active);
        bool clicked = ImGui.Button(label, size);
        ImGui.PopStyleColor(3);
        return clicked;
    }

    private static Vector4 GetCapacityColor(float healthFraction)
    {
        if (healthFraction >= 0.70f)
        {
            return ProfessionalPalette.Success;
        }

        if (healthFraction > 0.50f)
        {
            return ProfessionalPalette.Warning;
        }

        return ProfessionalPalette.Danger;
    }

    private string FormatOperatorState(CheckpointOperator op)
    {
        bool mode2 = _simulation.Config.Mode == CheckpointMode.MultiLaneBarricade;
        if (op.IsRobot)
        {
            return op.State switch
            {
                OperatorState.IdleAtPost => "Docked",
                OperatorState.Pursuing => mode2 ? "Responding to Red" : "Intercepting Undetected",
                OperatorState.Stopping => "Passenger secured",
                OperatorState.Verifying => "Clearing on site",
                OperatorState.Returning => op.CheckForNextTargetAfterClearance ? "Checking nearby targets" : "Returning to dock",
                _ => op.State.ToString(),
            };
        }
        return op.State switch
        {
            OperatorState.IdleAtPost => "Idle",
            OperatorState.ConfirmingYellow =>
                $"Confirming Yellow {op.TargetConfirmationElapsedSeconds:F1}/{_simulation.Config.YellowVisualSearchSeconds:F1}s",
            OperatorState.Pursuing => mode2 ? "Responding to Red" : "Pursuing Undetected",
            OperatorState.Stopping => mode2 ? "Red secured" : "Intercepted",
            OperatorState.Escorting => mode2 ? "Escorting Red" : "Escorting",
            OperatorState.Verifying => mode2 ? "Manual clearing Red" : "Verifying",
            OperatorState.Returning => "Returning",
            OperatorState.DasherPursuing => "Pursuing Dasher",
            OperatorState.DasherControlling => "Holding Dasher / awaiting backup",
            OperatorState.DasherGroupStop => "Dasher group stop",
            OperatorState.DasherEscorting => "Escorting Dasher",
            _ => op.State.ToString(),
        };
    }
}
