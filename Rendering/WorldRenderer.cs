using System.Numerics;
using CrowdCheckpointSim.Core;
using CrowdCheckpointSim.Visual;
using ImGuiNET;

namespace CrowdCheckpointSim.Rendering;

/// <summary>
/// Python-style 2.5D isometric world renderer. The simulation remains 2D in metres, while the
/// presentation projects floors, walls and geometric humanoids into a fixed orthographic 3D view.
/// Dear ImGui ultimately renders these primitives through the Silk.NET OpenGL backend.
/// </summary>
public sealed class WorldRenderer
{
    private readonly IsometricCamera _camera = new();
    private Vector2 _canvasMin;
    private Vector2 _canvasMax;
    private float _uiScale = 1f;
    private ResponderKind _previewKind;

    /// <summary>Whether the pointer is currently inside the simulation canvas.</summary>
    public bool IsHovered { get; private set; }

    /// <summary>Current presentation-only camera zoom multiplier.</summary>
    public float Zoom => _camera.Zoom;

    /// <summary>Set presentation-only camera zoom. Simulation state is never changed.</summary>
    public void SetZoom(float zoom) => _camera.SetZoom(zoom);

    /// <summary>Draw the complete 2.5D scenario into the currently active ImGui child window.</summary>
    public void Draw(
        CrowdSimulation simulation,
        bool setupMode,
        bool dasherSelectionMode = false,
        bool operatorPlacementMode = true,
        ResponderKind previewKind = ResponderKind.PurpleOperator)
    {
        _previewKind = previewKind;
        _uiScale = Math.Max(0.5f, ImGui.GetIO().FontGlobalScale);
        Vector2 available = ImGui.GetContentRegionAvail();
        available.X = Math.Max(100f, available.X);
        available.Y = Math.Max(100f, available.Y);

        ImGui.InvisibleButton("##simulation-canvas", available);
        _canvasMin = ImGui.GetItemRectMin();
        _canvasMax = ImGui.GetItemRectMax();
        IsHovered = ImGui.IsItemHovered();

        // Mouse-wheel zoom is view-only and works in both setup and runtime. It never changes
        // pedestrian/operator world coordinates, SFM forces, acquisition distances or guardrails.
        if (IsHovered)
        {
            float wheelDelta = ImGui.GetIO().MouseWheel;
            if (Math.Abs(wheelDelta) > 0.0001f)
            {
                _camera.AdjustZoom(wheelDelta);
            }
        }

        SimulationConfig config = simulation.Config;
        _camera.Configure(_canvasMin, _canvasMax, config);

        ImDrawListPtr draw = ImGui.GetWindowDrawList();
        draw.PushClipRect(_canvasMin, _canvasMax, true);
        draw.AddRectFilled(_canvasMin, _canvasMax, Color(ProfessionalPalette.AppBackground));

        DrawFloor(draw, config);
        DrawMultiLaneCheckpointInfrastructure(draw, simulation);

        Vector2? previewOperatorPosition = GetOperatorPreviewPosition(
            simulation,
            setupMode && operatorPlacementMode);

        DrawOperatorAcquisitionRanges(draw, simulation);
        if (previewOperatorPosition is Vector2 ghostPosition)
        {
            DrawClippedGhostAcquisitionRange(draw, config, ghostPosition);
        }

        DrawOperatorPosts(draw, simulation);
        DrawWallsAndEntities(draw, simulation, previewOperatorPosition);

        // Runtime Dasher selection affordance: make the passenger currently under the pointer
        // visibly selectable before the click. This is presentation-only.
        if (!setupMode && dasherSelectionMode && simulation.CanSelectDasher &&
            TryFindPedestrianAtScreenPoint(
                simulation,
                ImGui.GetMousePos(),
                funnelOnly: false,
                out int hoveredId))
        {
            Pedestrian? hovered = simulation.Pedestrians.FirstOrDefault(p => p.Id == hoveredId);
            bool mode2Eligible = simulation.Config.Mode != CheckpointMode.MultiLaneBarricade ||
                (hovered is not null &&
                 !hovered.RequiresManualClearance &&
                 hovered.DetectionStatus == DetectionStatus.InFunnel);
            if (hovered is not null && hovered.Phase == PedestrianPhase.Funnel && mode2Eligible)
            {
                DrawDasherSelectionHover(draw, hovered);
            }
        }

        DrawLabels(draw, simulation);

        if (!setupMode && dasherSelectionMode)
        {
            DrawDasherSelectionModeBanner(draw);
        }

        draw.PopClipRect();
    }

    /// <summary>
    /// Pick a visible funnel pedestrian by clicking anywhere on the rendered humanoid. The hit
    /// target covers the complete head/torso/arms/legs projection, plus a small screen-space
    /// tolerance so densely packed queue passengers do not require pixel-perfect clicks.
    /// </summary>
    public bool TryPickFunnelPedestrian(CrowdSimulation simulation, out int pedestrianId)
    {
        pedestrianId = 0;
        if (!IsHovered)
        {
            return false;
        }

        return TryFindPedestrianAtScreenPoint(
            simulation,
            ImGui.GetMousePos(),
            funnelOnly: true,
            out pedestrianId);
    }

    /// <summary>Pick any visible passenger humanoid under the mouse, regardless of phase.</summary>
    public bool TryPickPedestrian(CrowdSimulation simulation, out int pedestrianId)
    {
        pedestrianId = 0;
        if (!IsHovered)
        {
            return false;
        }

        return TryFindPedestrianAtScreenPoint(
            simulation,
            ImGui.GetMousePos(),
            funnelOnly: false,
            out pedestrianId);
    }

    /// <summary>
    /// Returns the pedestrian whose expanded projected humanoid bounds best contain the supplied
    /// screen point. When bodies overlap in the isometric view, the closest projected body centre
    /// wins.
    /// </summary>
    private bool TryFindPedestrianAtScreenPoint(
        CrowdSimulation simulation,
        Vector2 screenPoint,
        bool funnelOnly,
        out int pedestrianId)
    {
        pedestrianId = 0;
        Pedestrian? best = null;
        float bestDistanceSquared = float.MaxValue;

        foreach (Pedestrian pedestrian in simulation.Pedestrians)
        {
            if (funnelOnly && pedestrian.Phase != PedestrianPhase.Funnel)
            {
                continue;
            }

            (Vector2 min, Vector2 max) = GetPedestrianScreenBounds(pedestrian);
            if (screenPoint.X < min.X || screenPoint.X > max.X ||
                screenPoint.Y < min.Y || screenPoint.Y > max.Y)
            {
                continue;
            }

            Vector2 bodyCentre = _camera.Project(
                new Vector3(pedestrian.Position.X, pedestrian.Position.Y, 0.90f));
            float distanceSquared = Vector2.DistanceSquared(screenPoint, bodyCentre);
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                best = pedestrian;
            }
        }

        if (best is null)
        {
            return false;
        }

        pedestrianId = best.Id;
        return true;
    }

    /// <summary>
    /// Screen-space hit box for the complete pedestrian mesh. These dimensions mirror the
    /// non-operator geometry in DrawHumanoid. The extra padding deliberately improves usability
    /// without changing any simulation/world-space dimensions.
    /// </summary>
    private (Vector2 Min, Vector2 Max) GetPedestrianScreenBounds(Pedestrian pedestrian)
    {
        // Humanoid Visual V2A extends the visual shoulders/arms and raises the head slightly.
        // Selection bounds follow the rendered body only; simulation collision radii are unchanged.
        const float halfX = 0.245f;
        const float halfY = 0.315f;
        const float minZ = 0.01f;
        const float maxZ = 1.84f;
        float clickPaddingPixels = 10f * _uiScale;

        float x = pedestrian.Position.X;
        float y = pedestrian.Position.Y;

        Span<Vector3> corners = stackalloc Vector3[8]
        {
            new(x - halfX, y - halfY, minZ),
            new(x + halfX, y - halfY, minZ),
            new(x - halfX, y + halfY, minZ),
            new(x + halfX, y + halfY, minZ),
            new(x - halfX, y - halfY, maxZ),
            new(x + halfX, y - halfY, maxZ),
            new(x - halfX, y + halfY, maxZ),
            new(x + halfX, y + halfY, maxZ),
        };

        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);

        foreach (Vector3 corner in corners)
        {
            Vector2 projected = _camera.Project(corner);
            min = Vector2.Min(min, projected);
            max = Vector2.Max(max, projected);
        }

        Vector2 padding = new(clickPaddingPixels, clickPaddingPixels);
        return (min - padding, max + padding);
    }

    /// <summary>Convert the current mouse position to world metres on the z=0 floor plane.</summary>
    public bool TryMouseToWorld(out Vector2 world)
    {
        world = default;
        if (!IsHovered)
        {
            return false;
        }

        return _camera.TryScreenToGround(ImGui.GetMousePos(), out world);
    }

    private void DrawFloor(ImDrawListPtr draw, SimulationConfig config)
    {
        DrawFlatQuad(
            draw,
            new Vector3(0f, 0f, 0f),
            new Vector3(config.RoomWidth, 0f, 0f),
            new Vector3(config.RoomWidth, config.RoomHeight, 0f),
            new Vector3(0f, config.RoomHeight, 0f),
            Color(ProfessionalPalette.Floor));

        DrawFlatQuad(
            draw,
            new Vector3(config.FunnelXMin, config.FunnelYMin, 0.001f),
            new Vector3(config.FunnelXMax, config.FunnelYMin, 0.001f),
            new Vector3(config.FunnelXMax, config.FunnelYMax, 0.001f),
            new Vector3(config.FunnelXMin, config.FunnelYMax, 0.001f),
            Color(ProfessionalPalette.FunnelFloor));

        const float checkpointHalfThickness = 0.03f;
        DrawFlatQuad(
            draw,
            new Vector3(config.FunnelXMin - checkpointHalfThickness, config.FunnelYMin, 0.002f),
            new Vector3(config.FunnelXMin + checkpointHalfThickness, config.FunnelYMin, 0.002f),
            new Vector3(config.FunnelXMin + checkpointHalfThickness, config.FunnelYMax, 0.002f),
            new Vector3(config.FunnelXMin - checkpointHalfThickness, config.FunnelYMax, 0.002f),
            Color(ProfessionalPalette.Checkpoint));

        const float exitHalfThickness = 0.03f;
        DrawFlatQuad(
            draw,
            new Vector3(0f, config.ExitYMin, 0.003f),
            new Vector3(exitHalfThickness, config.ExitYMin, 0.003f),
            new Vector3(exitHalfThickness, config.ExitYMax, 0.003f),
            new Vector3(0f, config.ExitYMax, 0.003f),
            Color(ProfessionalPalette.Exit));
    }

    private void DrawMultiLaneCheckpointInfrastructure(ImDrawListPtr draw, CrowdSimulation simulation)
    {
        SimulationConfig config = simulation.Config;
        if (config.Mode != CheckpointMode.MultiLaneBarricade)
        {
            return;
        }

        const float dividerThickness = 0.035f;
        const float dividerHeight = 0.95f;
        float dividerStartX = config.FunnelXMin + 0.10f;
        float dividerEndX = config.FunnelXMax - 0.06f;

        // Waist-height translucent separators. They sit exactly on the one-metre lane boundaries,
        // so the clear lane width remains 1.0 m and the dividers do not consume usable lane width.
        for (int boundary = 1; boundary < config.MultiLaneCount; boundary++)
        {
            float y = config.FunnelYMin + boundary * SimulationConfig.MultiLaneWidth;
            DrawPrism(
                draw,
                dividerStartX,
                dividerEndX,
                y - dividerThickness * 0.5f,
                y + dividerThickness * 0.5f,
                0.02f,
                dividerHeight,
                ProfessionalPalette.Alpha(ProfessionalPalette.LaneDivider, 0.23f));

            Vector2 railA = _camera.Project(new Vector3(dividerStartX, y, dividerHeight + 0.015f));
            Vector2 railB = _camera.Project(new Vector3(dividerEndX, y, dividerHeight + 0.015f));
            draw.AddLine(
                railA,
                railB,
                Color(ProfessionalPalette.Alpha(ProfessionalPalette.LaneDivider, 0.58f)),
                1.1f);
        }

        foreach (CheckpointLaneGate gate in simulation.CheckpointGates)
        {
            float laneMinY = config.FunnelYMin + gate.LaneIndex * SimulationConfig.MultiLaneWidth;
            float laneMaxY = laneMinY + SimulationConfig.MultiLaneWidth;
            Vector4 gateColor = gate.State switch
            {
                CheckpointGateState.Processing => ProfessionalPalette.GateProcessing,
                CheckpointGateState.Open => ProfessionalPalette.GateOpen,
                CheckpointGateState.ManualIntervention => ProfessionalPalette.Uncounted,
                _ => ProfessionalPalette.GateClosed,
            };

            // Two fixed posts make the gate position readable even while the sliding panel is open.
            DrawPrism(
                draw,
                config.FunnelXMin - 0.055f,
                config.FunnelXMin + 0.055f,
                laneMinY + 0.035f,
                laneMinY + 0.105f,
                0f,
                1.05f,
                ProfessionalPalette.Alpha(ProfessionalPalette.GateClosed, 0.85f));
            DrawPrism(
                draw,
                config.FunnelXMin - 0.055f,
                config.FunnelXMin + 0.055f,
                laneMaxY - 0.105f,
                laneMaxY - 0.035f,
                0f,
                1.05f,
                ProfessionalPalette.Alpha(ProfessionalPalette.GateClosed, 0.85f));

            float panelMinY;
            float panelMaxY;
            if (gate.State == CheckpointGateState.Open)
            {
                // Slide the translucent panel to the side instead of making it disappear. This
                // makes the open/close cycle obvious while keeping the lane visually structured.
                panelMinY = laneMaxY - 0.20f;
                panelMaxY = laneMaxY - 0.10f;
            }
            else
            {
                panelMinY = laneMinY + 0.10f;
                panelMaxY = laneMaxY - 0.10f;
            }

            DrawPrism(
                draw,
                config.FunnelXMin - 0.045f,
                config.FunnelXMin + 0.045f,
                panelMinY,
                panelMaxY,
                0.14f,
                0.92f,
                ProfessionalPalette.Alpha(gateColor, gate.State == CheckpointGateState.Open ? 0.46f : 0.34f));

            // A small ground indicator is especially helpful when many (15-20) gates are visible.
            float indicatorHalf = 0.11f;
            DrawHorizontalRectangle(
                draw,
                config.FunnelXMin + 0.07f,
                config.FunnelXMin + 0.22f,
                gate.CentreY - indicatorHalf,
                gate.CentreY + indicatorHalf,
                0.012f,
                Color(ProfessionalPalette.Alpha(gateColor, 0.72f)));
        }
    }

    private void DrawOperatorAcquisitionRanges(ImDrawListPtr draw, CrowdSimulation simulation)
    {
        float radius = simulation.Config.OperatorAcquisitionRadius;
        if (radius <= 0f)
        {
            return;
        }

        // Presentation-only coverage guide. Use the same world-space hall clipping as the
        // placement ghost so a placed operator's visible range never leaks through a wall.
        foreach (CheckpointOperator op in simulation.Operators)
        {
            DrawClippedAcquisitionRange(
                draw,
                simulation.Config,
                op.Position,
                radius,
                fillAlpha: 0.025f,
                outlineAlpha: 0.18f,
                outlineThickness: 1.1f,
                z: 0.016f,
                color: op.IsRobot ? ProfessionalPalette.RobotAccent : ProfessionalPalette.Operator);
        }
    }

    private void DrawOperatorPosts(ImDrawListPtr draw, CrowdSimulation simulation)
    {
        foreach (CheckpointOperator responder in simulation.Operators)
        {
            Vector2 post = responder.HomePosition;
            Vector4 markerColor = responder.IsRobot ? ProfessionalPalette.RobotAccent : ProfessionalPalette.Accent;
            const float half = 0.31f;
            DrawFlatQuad(
                draw,
                new Vector3(post.X - half, post.Y - half, 0.006f),
                new Vector3(post.X + half, post.Y - half, 0.006f),
                new Vector3(post.X + half, post.Y + half, 0.006f),
                new Vector3(post.X - half, post.Y + half, 0.006f),
                Color(ProfessionalPalette.Alpha(markerColor, 0.28f)));

            Vector2 a = _camera.Project(new Vector3(post.X - half, post.Y, 0.01f));
            Vector2 b = _camera.Project(new Vector3(post.X + half, post.Y, 0.01f));
            Vector2 c = _camera.Project(new Vector3(post.X, post.Y - half, 0.01f));
            Vector2 d = _camera.Project(new Vector3(post.X, post.Y + half, 0.01f));
            uint lineColor = Color(responder.IsRobot ? ProfessionalPalette.RobotAccent : ProfessionalPalette.AccentHover);
            draw.AddLine(a, b, lineColor, 1.5f);
            draw.AddLine(c, d, lineColor, 1.5f);
        }
    }

    private Vector2? GetOperatorPreviewPosition(CrowdSimulation simulation, bool setupMode)
    {
        if (!setupMode || !TryMouseToWorld(out Vector2 candidate))
        {
            return null;
        }

        return ScenarioGeometry.IsValidOperatorPost(
            simulation.Config,
            candidate,
            simulation.OperatorPosts)
            ? candidate
            : null;
    }

    private void DrawWallsAndEntities(
        ImDrawListPtr draw,
        CrowdSimulation simulation,
        Vector2? previewOperatorPosition)
    {
        IReadOnlyList<WallSegment> walls = ScenarioGeometry.BuildWalls(simulation.Config);
        int previewCount = previewOperatorPosition.HasValue ? 1 : 0;
        List<RenderEntity> entities = new(
            walls.Count + simulation.Pedestrians.Count + simulation.Operators.Count + previewCount);

        foreach (WallSegment wall in walls)
        {
            AddWallRenderChunks(entities, wall);
        }

        foreach (Pedestrian pedestrian in simulation.Pedestrians)
        {
            // Sort using the ground contact point. Using the humanoid centre height shifts the
            // whole person toward the camera and can make long vertical geometry overlap in the
            // wrong order in this painter-based 2.5D renderer.
            entities.Add(new RenderEntity(
                _camera.Depth(new Vector3(pedestrian.Position.X, pedestrian.Position.Y, 0f)),
                RenderEntityKind.Pedestrian,
                pedestrian,
                null,
                default,
                null));
        }

        foreach (CheckpointOperator op in simulation.Operators)
        {
            entities.Add(new RenderEntity(
                _camera.Depth(new Vector3(op.Position.X, op.Position.Y, 0f)),
                RenderEntityKind.Operator,
                null,
                op,
                default,
                null));
        }

        if (previewOperatorPosition is Vector2 previewPosition)
        {
            entities.Add(new RenderEntity(
                _camera.Depth(new Vector3(previewPosition.X, previewPosition.Y, 0f)),
                RenderEntityKind.OperatorPreview,
                null,
                null,
                default,
                previewPosition));
        }

        // Painter's algorithm: render farther walls/humanoids first so nearby geometry overlaps
        // more like the depth-tested Python OpenGL view.
        entities.Sort(static (left, right) => right.Depth.CompareTo(left.Depth));

        foreach (RenderEntity entity in entities)
        {
            switch (entity.Kind)
            {
                case RenderEntityKind.Wall:
                    DrawWallSegment(draw, entity.Wall);
                    break;
                case RenderEntityKind.Pedestrian when entity.Pedestrian is Pedestrian pedestrian:
                    DrawPedestrian(draw, pedestrian, simulation.Metrics.TimeSeconds);
                    break;
                case RenderEntityKind.Operator when entity.Operator is CheckpointOperator op:
                    DrawOperator(draw, simulation, op);
                    break;
                case RenderEntityKind.OperatorPreview when entity.PreviewPosition is Vector2 ghostPreviewPosition:
                    DrawOperatorPreview(draw, ghostPreviewPosition);
                    break;
            }
        }
    }

    /// <summary>
    /// Split a physical wall into short render-only chunks so painter sorting approximates the
    /// per-pixel depth behaviour of the original Python OpenGL implementation. The physical wall
    /// list remains unchanged for collision/SFM calculations.
    /// </summary>
    private void AddWallRenderChunks(List<RenderEntity> entities, WallSegment wall)
    {
        const float maxChunkLength = 0.35f;
        Vector2 delta = wall.B - wall.A;
        float length = delta.Length();
        int chunkCount = Math.Max(1, (int)MathF.Ceiling(length / maxChunkLength));

        for (int index = 0; index < chunkCount; index++)
        {
            float t0 = (float)index / chunkCount;
            float t1 = (float)(index + 1) / chunkCount;
            Vector2 a = Vector2.Lerp(wall.A, wall.B, t0);
            Vector2 b = Vector2.Lerp(wall.A, wall.B, t1);
            WallSegment chunk = new(a, b);
            Vector2 midpoint = (a + b) * 0.5f;

            entities.Add(new RenderEntity(
                _camera.Depth(new Vector3(midpoint.X, midpoint.Y, 0f)),
                RenderEntityKind.Wall,
                null,
                null,
                chunk,
                null));
        }
    }

    private void DrawWallSegment(ImDrawListPtr draw, WallSegment segment)
    {
        const float wallHeight = 1.25f;
        const float wallThickness = 0.08f;
        Vector4 wallColor = ProfessionalPalette.Wall;

        if (Math.Abs(segment.A.X - segment.B.X) < 0.001f)
        {
            float x = segment.A.X;
            DrawPrism(
                draw,
                x - wallThickness * 0.5f,
                x + wallThickness * 0.5f,
                Math.Min(segment.A.Y, segment.B.Y),
                Math.Max(segment.A.Y, segment.B.Y),
                0f,
                wallHeight,
                wallColor);
        }
        else
        {
            float y = segment.A.Y;
            DrawPrism(
                draw,
                Math.Min(segment.A.X, segment.B.X),
                Math.Max(segment.A.X, segment.B.X),
                y - wallThickness * 0.5f,
                y + wallThickness * 0.5f,
                0f,
                wallHeight,
                wallColor);
        }
    }

    private void DrawPedestrian(ImDrawListPtr draw, Pedestrian pedestrian, float animationTimeSeconds)
    {
        Vector4 baseColor = pedestrian.DasherState is not (DasherState.None or DasherState.MarkedInFunnel)
            ? ProfessionalPalette.Dasher
            : pedestrian.DetectionStatus switch
            {
                DetectionStatus.DetectedAndCounted => ProfessionalPalette.Cleared,
                DetectionStatus.CountedNotDetected => ProfessionalPalette.Undetected,
                DetectionStatus.NotCountedNotDetected => ProfessionalPalette.Uncounted,
                _ => ProfessionalPalette.NeutralPassenger,
            };

        DrawHumanoid(
            draw,
            pedestrian.Position,
            pedestrian.Velocity,
            animationTimeSeconds,
            pedestrian.Id,
            baseColor,
            Shade(baseColor, 0.72f),
            ProfessionalPalette.NeutralHead,
            isOperator: false,
            forceRunning: pedestrian.DasherState == DasherState.Dashing);

        if (pedestrian.CooperativeStop)
        {
            Vector2 center = _camera.Project(new Vector3(pedestrian.Position.X, pedestrian.Position.Y, 1.78f));
            draw.AddCircle(
                center,
                7f * _uiScale,
                Color(1f, 1f, 1f, 0.95f),
                16,
                1.5f * _uiScale);
        }

        if (pedestrian.IsDasher)
        {
            Vector2 marker = _camera.Project(new Vector3(pedestrian.Position.X, pedestrian.Position.Y, 1.86f));
            float radius = (pedestrian.DasherState == DasherState.MarkedInFunnel ? 8f : 10f) * _uiScale;
            draw.AddCircle(
                marker,
                radius,
                Color(ProfessionalPalette.Dasher),
                20,
                2.0f * _uiScale);
            string label = pedestrian.DasherState == DasherState.MarkedInFunnel
                ? "DASHER SELECTED"
                : "DASHER";
            draw.AddText(
                marker + new Vector2(11f, -7f) * _uiScale,
                Color(ProfessionalPalette.Dasher),
                label);
        }
    }

    private void DrawDasherSelectionModeBanner(ImDrawListPtr draw)
    {
        Vector2 min = _canvasMin + new Vector2(14f, 14f) * _uiScale;
        Vector2 max = min + new Vector2(420f, 48f) * _uiScale;
        draw.AddRectFilled(
            min,
            max,
            Color(ProfessionalPalette.Alpha(ProfessionalPalette.CardBackground, 0.94f)),
            6f * _uiScale);
        draw.AddRect(
            min,
            max,
            Color(ProfessionalPalette.Alpha(ProfessionalPalette.Dasher, 0.68f)),
            6f * _uiScale,
            ImDrawFlags.None,
            1.2f * _uiScale);
        draw.AddText(
            min + new Vector2(12f, 8f) * _uiScale,
            Color(ProfessionalPalette.Dasher),
            "DASHER SELECTION MODE");
        draw.AddText(
            min + new Vector2(12f, 26f) * _uiScale,
            Color(ProfessionalPalette.SecondaryText),
            "Hover eligible funnel passengers (cyan glow) | Esc = cancel");
    }

    private void DrawDasherSelectionHover(ImDrawListPtr draw, Pedestrian pedestrian)
    {
        (Vector2 min, Vector2 max) = GetPedestrianScreenBounds(pedestrian);
        uint glow = Color(ProfessionalPalette.Alpha(ProfessionalPalette.Dasher, 0.24f));
        uint outline = Color(ProfessionalPalette.Alpha(ProfessionalPalette.Dasher, 0.95f));
        Vector2 glowPadding = new Vector2(2f) * _uiScale;
        draw.AddRect(
            min - glowPadding,
            max + glowPadding,
            glow,
            5f * _uiScale,
            ImDrawFlags.None,
            5.0f * _uiScale);
        draw.AddRect(
            min,
            max,
            outline,
            3f * _uiScale,
            ImDrawFlags.None,
            1.6f * _uiScale);

        Vector2 marker = _camera.Project(
            new Vector3(pedestrian.Position.X, pedestrian.Position.Y, 1.84f));
        draw.AddCircle(marker, 9f * _uiScale, glow, 20, 5.0f * _uiScale);
        draw.AddCircle(marker, 7f * _uiScale, outline, 20, 1.6f * _uiScale);
        draw.AddText(marker + new Vector2(11f, -8f) * _uiScale, outline, "ELIGIBLE");
    }

    private void DrawClippedGhostAcquisitionRange(
        ImDrawListPtr draw,
        SimulationConfig config,
        Vector2 centre)
    {
        DrawClippedAcquisitionRange(
            draw,
            config,
            centre,
            config.OperatorAcquisitionRadius,
            fillAlpha: 0.065f,
            outlineAlpha: 0.40f,
            outlineThickness: 1.2f,
            z: 0.004f,
            color: _previewKind == ResponderKind.WheeledRobot ? ProfessionalPalette.RobotAccent : ProfessionalPalette.Operator);
    }

    private void DrawClippedAcquisitionRange(
        ImDrawListPtr draw,
        SimulationConfig config,
        Vector2 centre,
        float radius,
        float fillAlpha,
        float outlineAlpha,
        float outlineThickness,
        float z,
        Vector4? color = null)
    {
        const int segments = 96;
        if (radius <= 0f)
        {
            return;
        }

        List<Vector2> circle = new(segments);
        for (int index = 0; index < segments; index++)
        {
            float angle = MathF.Tau * index / segments;
            circle.Add(centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
        }

        // Clip in world space, not screen space. The isometric hall is a projected quadrilateral,
        // so screen-rectangle clipping would allow acquisition coverage to leak beyond the walls.
        List<Vector2> clipped = ClipConvexPolygonToHall(circle, config);
        if (clipped.Count < 3)
        {
            return;
        }

        Vector2 projectedCentre = _camera.Project(new Vector3(centre.X, centre.Y, z));
        Vector4 rangeColor = color ?? ProfessionalPalette.Operator;
        uint fill = Color(ProfessionalPalette.Alpha(rangeColor, fillAlpha));
        for (int index = 0; index < clipped.Count; index++)
        {
            Vector2 aWorld = clipped[index];
            Vector2 bWorld = clipped[(index + 1) % clipped.Count];
            Vector2 a = _camera.Project(new Vector3(aWorld.X, aWorld.Y, z));
            Vector2 b = _camera.Project(new Vector3(bWorld.X, bWorld.Y, z));
            draw.AddTriangleFilled(projectedCentre, a, b, fill);
        }

        // The outline follows the clipped coverage polygon. Where the range reaches a wall,
        // it terminates at the hall boundary instead of drawing through/outside the room.
        uint outline = Color(ProfessionalPalette.Alpha(rangeColor, outlineAlpha));
        for (int index = 0; index < clipped.Count; index++)
        {
            Vector2 aWorld = clipped[index];
            Vector2 bWorld = clipped[(index + 1) % clipped.Count];
            Vector2 a = _camera.Project(new Vector3(aWorld.X, aWorld.Y, z + 0.002f));
            Vector2 b = _camera.Project(new Vector3(bWorld.X, bWorld.Y, z + 0.002f));
            draw.AddLine(a, b, outline, outlineThickness);
        }
    }

    private static List<Vector2> ClipConvexPolygonToHall(
        IReadOnlyList<Vector2> polygon,
        SimulationConfig config)
    {
        List<Vector2> result = polygon.ToList();
        result = ClipAgainstBoundary(
            result,
            point => point.X >= 0f,
            (a, b) => IntersectVertical(a, b, 0f));
        result = ClipAgainstBoundary(
            result,
            point => point.X <= config.RoomWidth,
            (a, b) => IntersectVertical(a, b, config.RoomWidth));
        result = ClipAgainstBoundary(
            result,
            point => point.Y >= 0f,
            (a, b) => IntersectHorizontal(a, b, 0f));
        result = ClipAgainstBoundary(
            result,
            point => point.Y <= config.RoomHeight,
            (a, b) => IntersectHorizontal(a, b, config.RoomHeight));
        return result;
    }

    private static List<Vector2> ClipAgainstBoundary(
        IReadOnlyList<Vector2> input,
        Func<Vector2, bool> inside,
        Func<Vector2, Vector2, Vector2> intersection)
    {
        List<Vector2> output = new();
        if (input.Count == 0)
        {
            return output;
        }

        Vector2 previous = input[^1];
        bool previousInside = inside(previous);
        foreach (Vector2 current in input)
        {
            bool currentInside = inside(current);
            if (currentInside)
            {
                if (!previousInside)
                {
                    output.Add(intersection(previous, current));
                }

                output.Add(current);
            }
            else if (previousInside)
            {
                output.Add(intersection(previous, current));
            }

            previous = current;
            previousInside = currentInside;
        }

        return output;
    }

    private static Vector2 IntersectVertical(Vector2 a, Vector2 b, float x)
    {
        float delta = b.X - a.X;
        float t = Math.Abs(delta) <= 1e-6f ? 0f : (x - a.X) / delta;
        return Vector2.Lerp(a, b, Math.Clamp(t, 0f, 1f));
    }

    private static Vector2 IntersectHorizontal(Vector2 a, Vector2 b, float y)
    {
        float delta = b.Y - a.Y;
        float t = Math.Abs(delta) <= 1e-6f ? 0f : (y - a.Y) / delta;
        return Vector2.Lerp(a, b, Math.Clamp(t, 0f, 1f));
    }

    private void DrawOperatorPreview(ImDrawListPtr draw, Vector2 position)
    {
        // Match the Python setup preview palette, with added transparency so the candidate
        // reads as a ghost rather than an already-placed operator.
        bool robot = _previewKind == ResponderKind.WheeledRobot;
        Vector4 bodyColor = robot ? ProfessionalPalette.Robot : ProfessionalPalette.Operator;
        Vector4 torso = ProfessionalPalette.Alpha(bodyColor, 0.52f);
        Vector4 limb = ProfessionalPalette.Alpha(Shade(bodyColor, 0.72f), 0.48f);
        Vector4 head = ProfessionalPalette.Alpha(robot ? ProfessionalPalette.Robot : ProfessionalPalette.NeutralHead, 0.55f);

        if (robot)
            DrawWheeledRobot(draw, position, 0.52f);
        else
            DrawHumanoid(draw, position, Vector2.Zero, 0f, 0, torso, limb, head, isOperator: true, forceRunning: false);

        const float half = 0.31f;
        Vector2 a = _camera.Project(new Vector3(position.X - half, position.Y, 0.012f));
        Vector2 b = _camera.Project(new Vector3(position.X + half, position.Y, 0.012f));
        Vector2 c = _camera.Project(new Vector3(position.X, position.Y - half, 0.012f));
        Vector2 d = _camera.Project(new Vector3(position.X, position.Y + half, 0.012f));
        uint footprintColor = Color(ProfessionalPalette.Alpha(robot ? ProfessionalPalette.RobotAccent : ProfessionalPalette.Operator, 0.72f));
        draw.AddLine(a, b, footprintColor, 1.5f);
        draw.AddLine(c, d, footprintColor, 1.5f);
    }

    private void DrawOperator(ImDrawListPtr draw, CrowdSimulation simulation, CheckpointOperator op)
    {
        Vector4 torso = op.IsRobot ? ProfessionalPalette.Robot : ProfessionalPalette.Operator;
        Vector4 limb = Shade(torso, 0.68f);
        Vector4 head = op.IsRobot ? ProfessionalPalette.Robot : ProfessionalPalette.NeutralHead;

        if (op.IsRobot)
        {
            DrawWheeledRobot(draw, op.Position);
            if (op.State == OperatorState.Verifying)
            {
                // Robot bars show clearance progress, never human fatigue/capacity.
                float progress = Math.Clamp(op.VerificationElapsedSeconds / op.CurrentVerificationDuration, 0f, 1f);
                DrawHorizontalRectangle(draw, op.Position.X - 0.25f, op.Position.X + 0.25f,
                    op.Position.Y - 0.035f, op.Position.Y + 0.035f, 1.22f, Color(ProfessionalPalette.DarkTrack));
                DrawHorizontalRectangle(draw, op.Position.X - 0.25f, op.Position.X - 0.25f + 0.50f * progress,
                    op.Position.Y - 0.035f, op.Position.Y + 0.035f, 1.23f, Color(ProfessionalPalette.RobotAccent));
            }
        }
        else
        {
            DrawHumanoid(draw, op.Position, op.Velocity, simulation.Metrics.TimeSeconds, 100000 + op.Id, torso, limb, head,
                isOperator: true, forceRunning: op.State == OperatorState.DasherPursuing);
            DrawOperatorHealthBar(draw, op);
        }

        if (op.TargetPedestrianId is int targetId)
        {
            Pedestrian? target = simulation.Pedestrians.FirstOrDefault(p => p.Id == targetId);
            if (target is not null)
            {
                Vector2 from = _camera.Project(new Vector3(op.Position.X, op.Position.Y, op.IsRobot ? 0.70f : 0.92f));
                Vector2 to = _camera.Project(new Vector3(target.Position.X, target.Position.Y, 0.92f));
                draw.AddLine(from, to, Color(ProfessionalPalette.Alpha(op.IsRobot ? ProfessionalPalette.RobotAccent : ProfessionalPalette.Operator, 0.42f)), 1f);
            }
        }
    }

    /// <summary>Presentation-only four-wheel robot; physics retains the existing responder radius.</summary>
    private void DrawWheeledRobot(ImDrawListPtr draw, Vector2 position, float alpha = 1f)
    {
        Vector4 shell = ProfessionalPalette.Alpha(ProfessionalPalette.Robot, alpha);
        Vector4 teal = ProfessionalPalette.Alpha(ProfessionalPalette.RobotAccent, alpha);
        Vector4 tyre = new(0.10f, 0.13f, 0.17f, alpha);

        // Far wheels precede the chassis; near wheels follow it for correct isometric occlusion.
        float farSide = _camera.Depth(WorldPoint(position + new Vector2(0f, 0.15f), 0.08f)) >
            _camera.Depth(WorldPoint(position - new Vector2(0f, 0.15f), 0.08f)) ? 1f : -1f;
        DrawRobotWheel(draw, position + new Vector2(-0.10f, farSide * 0.14f), tyre);
        DrawRobotWheel(draw, position + new Vector2(0.10f, farSide * 0.14f), tyre);
        DrawCenteredPrism(draw, position, 0.17f, 0.135f, 0.09f, 0.26f, Shade(shell, 0.58f));
        DrawCenteredPrism(draw, position, 0.14f, 0.115f, 0.25f, 0.60f, shell);
        DrawCenteredPrism(draw, position, 0.145f, 0.12f, 0.55f, 0.60f, teal);
        DrawCenteredPrism(draw, position, 0.045f, 0.045f, 0.60f, 0.94f, Shade(shell, 0.75f));
        DrawCenteredPrism(draw, position, 0.13f, 0.09f, 0.90f, 1.10f, shell);
        // Dark front sensor face and a teal scanner identify the unit even at small zoom.
        DrawCenteredPrism(draw, position + new Vector2(0.10f, -0.035f),
            0.035f, 0.055f, 0.95f, 1.06f, Shade(shell, 0.24f));
        DrawCenteredPrism(draw, position + new Vector2(0.137f, -0.035f),
            0.005f, 0.040f, 0.99f, 1.02f, teal);
        DrawRobotWheel(draw, position + new Vector2(-0.10f, -farSide * 0.14f), tyre);
        DrawRobotWheel(draw, position + new Vector2(0.10f, -farSide * 0.14f), tyre);
    }

    /// <summary>Projected cylindrical tyre with a visible hub; all coordinates are visual only.</summary>
    private void DrawRobotWheel(ImDrawListPtr draw, Vector2 position, Vector4 tyre)
    {
        const int segments = 12;
        const float radius = 0.075f;
        const float halfWidth = 0.020f;
        Vector3 centre = WorldPoint(position, radius);
        Vector3 visibleCentre = centre - new Vector3(0f, halfWidth, 0f);
        for (int index = 0; index < segments; index++)
        {
            float angleA = MathF.Tau * index / segments;
            float angleB = MathF.Tau * (index + 1) / segments;
            Vector3 a = visibleCentre + new Vector3(MathF.Cos(angleA) * radius, 0f, MathF.Sin(angleA) * radius);
            Vector3 b = visibleCentre + new Vector3(MathF.Cos(angleB) * radius, 0f, MathF.Sin(angleB) * radius);
            Vector3 width = new(0f, halfWidth * 2f, 0f);
            DrawFlatQuad(draw, a, b, b + width, a + width, Color(Shade(tyre, 0.72f)));
            draw.AddTriangleFilled(_camera.Project(visibleCentre), _camera.Project(a), _camera.Project(b), Color(tyre));
        }
        Vector2 hub = _camera.Project(visibleCentre);
        float hubRadius = Math.Max(1.0f, ProjectedDiameterPixels(visibleCentre, 0.05f) * 0.5f);
        draw.AddCircleFilled(hub, hubRadius, Color(ProfessionalPalette.Alpha(ProfessionalPalette.Robot, tyre.W)), 10);
    }

    private void DrawHumanoid(
        ImDrawListPtr draw,
        Vector2 position,
        Vector2 velocity,
        float animationTimeSeconds,
        int animationSeed,
        Vector4 torsoColor,
        Vector4 limbColor,
        Vector4 headColor,
        bool isOperator,
        bool forceRunning)
    {
        // Humanoid Visual V2B remains presentation-only. The procedural pose is derived from the
        // existing simulation velocity/time; it never writes back to simulation state or changes
        // SFM radius, collision, catch/acquisition distances, or behavioural logic.
        HumanoidAnimationPose pose = HumanoidAnimation.Compute(
            animationTimeSeconds,
            animationSeed,
            velocity,
            forceRunning);

        float alpha = torsoColor.W;
        Vector4 skin = ProfessionalPalette.Alpha(headColor, alpha);
        Vector4 skinShadow = ProfessionalPalette.Alpha(ProfessionalPalette.NeutralSkinShadow, alpha);
        Vector4 trousers = ProfessionalPalette.Alpha(ProfessionalPalette.Trousers, alpha);
        Vector4 trouserHighlight = ProfessionalPalette.Alpha(ProfessionalPalette.TrousersHighlight, alpha);
        Vector4 shoes = ProfessionalPalette.Alpha(ProfessionalPalette.Shoes, alpha);
        Vector4 shirtShadow = ProfessionalPalette.Alpha(limbColor, alpha);
        Vector4 shirt = ProfessionalPalette.Alpha(torsoColor, alpha);

        float shoulderHalfY = isOperator ? 0.225f : 0.205f;
        float chestHalfY = isOperator ? 0.190f : 0.174f;
        float waistHalfY = isOperator ? 0.145f : 0.136f;
        float pelvisHalfY = isOperator ? 0.142f : 0.134f;
        float bodyHalfX = isOperator ? 0.142f : 0.132f;

        Vector2 forward = pose.Forward;
        Vector2 side = pose.Side;
        float bob = pose.BobHeight;
        Vector2 lean = forward * pose.LeanDistance;

        Vector2 pelvisCenter = position + lean * 0.08f;
        Vector2 waistCenter = position + lean * 0.24f;
        Vector2 chestCenter = position + lean * 0.52f;
        Vector2 shoulderCenter = position + lean * 0.78f;
        Vector2 headCenter = position + lean;

        // Legs are jointed at the hip, knee and ankle. The swing leg lifts while the stance leg
        // stays lower, producing a readable gait without modifying the actual agent position.
        Vector3 leftHip = WorldPoint(pelvisCenter + side * 0.070f, 0.84f + bob);
        Vector3 rightHip = WorldPoint(pelvisCenter - side * 0.070f, 0.84f + bob);

        Vector3 leftKnee = WorldPoint(
            position + side * 0.072f + forward * (pose.LeftLegSwing * 0.42f),
            0.49f + bob + pose.LeftKneeLift * 0.42f);
        Vector3 rightKnee = WorldPoint(
            position - side * 0.072f + forward * (pose.RightLegSwing * 0.42f),
            0.49f + bob + pose.RightKneeLift * 0.42f);

        Vector3 leftAnkle = WorldPoint(
            position + side * 0.076f + forward * pose.LeftLegSwing,
            0.13f + bob + pose.LeftKneeLift * 0.64f);
        Vector3 rightAnkle = WorldPoint(
            position - side * 0.076f + forward * pose.RightLegSwing,
            0.13f + bob + pose.RightKneeLift * 0.64f);

        Vector3 leftFoot = WorldPoint(
            new Vector2(leftAnkle.X, leftAnkle.Y) + forward * 0.115f,
            Math.Max(0.055f, leftAnkle.Z - 0.045f));
        Vector3 rightFoot = WorldPoint(
            new Vector2(rightAnkle.X, rightAnkle.Y) + forward * 0.115f,
            Math.Max(0.055f, rightAnkle.Z - 0.045f));

        DrawProjectedSegment(draw, leftHip, leftKnee, 0.135f, trouserHighlight);
        DrawProjectedSegment(draw, rightHip, rightKnee, 0.135f, trouserHighlight);
        DrawProjectedSegment(draw, leftKnee, leftAnkle, 0.112f, trousers);
        DrawProjectedSegment(draw, rightKnee, rightAnkle, 0.112f, trousers);
        DrawProjectedSegment(draw, leftAnkle, leftFoot, 0.125f, shoes);
        DrawProjectedSegment(draw, rightAnkle, rightFoot, 0.125f, shoes);

        // Arm joints are built before the torso so the far arm can sit behind the chest and the
        // near arm can sit in front. Running raises the hands and bends the elbows more visibly.
        Vector3 leftShoulder = WorldPoint(shoulderCenter + side * shoulderHalfY, 1.335f + bob);
        Vector3 rightShoulder = WorldPoint(shoulderCenter - side * shoulderHalfY, 1.335f + bob);
        float elbowZ = (pose.IsRunning ? 1.115f : 1.065f) + bob;
        float handZ = (pose.IsRunning ? 0.995f : 0.805f) + bob;
        float elbowSwingScale = pose.IsRunning ? 0.30f : 0.48f;

        Vector3 leftElbow = WorldPoint(
            shoulderCenter + side * (shoulderHalfY + 0.018f) + forward * (pose.LeftArmSwing * elbowSwingScale),
            elbowZ);
        Vector3 rightElbow = WorldPoint(
            shoulderCenter - side * (shoulderHalfY + 0.018f) + forward * (pose.RightArmSwing * elbowSwingScale),
            elbowZ);
        Vector3 leftHand = WorldPoint(
            shoulderCenter + side * (shoulderHalfY + 0.010f) + forward * pose.LeftArmSwing,
            handZ);
        Vector3 rightHand = WorldPoint(
            shoulderCenter - side * (shoulderHalfY + 0.010f) + forward * pose.RightArmSwing,
            handZ);

        bool leftArmIsFarther = _camera.Depth(leftShoulder) > _camera.Depth(rightShoulder);
        if (leftArmIsFarther)
        {
            DrawArm(draw, leftShoulder, leftElbow, leftHand, shirtShadow, skinShadow, skin);
        }
        else
        {
            DrawArm(draw, rightShoulder, rightElbow, rightHand, shirtShadow, skinShadow, skin);
        }

        // Pelvis and torso retain the efficient V2A prisms, but progressive forward offsets now
        // create a subtle whole-body lean when running. A small gait bob is applied uniformly.
        DrawCenteredPrism(
            draw,
            pelvisCenter,
            bodyHalfX * 0.86f,
            pelvisHalfY,
            0.80f + bob,
            0.96f + bob,
            trousers);
        DrawCenteredPrism(
            draw,
            waistCenter,
            bodyHalfX * 0.88f,
            waistHalfY,
            0.94f + bob,
            1.12f + bob,
            shirtShadow);
        DrawCenteredPrism(
            draw,
            chestCenter,
            bodyHalfX,
            chestHalfY,
            1.10f + bob,
            1.36f + bob,
            shirt);
        DrawCenteredPrism(
            draw,
            shoulderCenter,
            bodyHalfX * 0.96f,
            shoulderHalfY,
            1.32f + bob,
            1.43f + bob,
            shirt);

        // Near arm is painted after the torso for a stronger sense of depth during the gait.
        if (leftArmIsFarther)
        {
            DrawArm(draw, rightShoulder, rightElbow, rightHand, shirtShadow, skinShadow, skin);
        }
        else
        {
            DrawArm(draw, leftShoulder, leftElbow, leftHand, shirtShadow, skinShadow, skin);
        }

        DrawCenteredPrism(
            draw,
            headCenter,
            0.060f,
            0.058f,
            1.41f + bob,
            1.50f + bob,
            skinShadow);

        float headScale = isOperator ? 1.02f : 1.00f;
        DrawCenteredPrism(
            draw,
            headCenter,
            0.082f * headScale,
            0.080f * headScale,
            1.49f + bob,
            1.55f + bob,
            skinShadow);
        DrawCenteredPrism(
            draw,
            headCenter,
            0.103f * headScale,
            0.101f * headScale,
            1.54f + bob,
            1.69f + bob,
            skin);
        DrawCenteredPrism(
            draw,
            headCenter,
            0.085f * headScale,
            0.083f * headScale,
            1.68f + bob,
            1.76f + bob,
            skin);

        if (isOperator)
        {
            Vector4 trim = ProfessionalPalette.Alpha(ProfessionalPalette.UniformTrim, alpha * 0.78f);
            DrawCenteredPrism(
                draw,
                shoulderCenter,
                bodyHalfX * 0.98f,
                0.010f,
                1.385f + bob,
                1.425f + bob,
                trim);
        }
    }

    private void DrawArm(
        ImDrawListPtr draw,
        Vector3 shoulder,
        Vector3 elbow,
        Vector3 hand,
        Vector4 sleeveColor,
        Vector4 forearmColor,
        Vector4 handColor)
    {
        DrawProjectedSegment(draw, shoulder, elbow, 0.105f, sleeveColor);
        DrawProjectedSegment(draw, elbow, hand, 0.082f, forearmColor);

        Vector2 handScreen = _camera.Project(hand);
        float handRadius = Math.Max(2.0f, ProjectedDiameterPixels(hand, 0.090f) * 0.5f);
        draw.AddCircleFilled(handScreen, handRadius, Color(handColor), 12);
    }

    private void DrawProjectedSegment(
        ImDrawListPtr draw,
        Vector3 start,
        Vector3 end,
        float diameterMeters,
        Vector4 color)
    {
        Vector2 startScreen = _camera.Project(start);
        Vector2 endScreen = _camera.Project(end);
        float thickness = Math.Clamp(ProjectedDiameterPixels((start + end) * 0.5f, diameterMeters), 2.0f, 18.0f);
        uint packed = Color(color);
        draw.AddLine(startScreen, endScreen, packed, thickness);
        draw.AddCircleFilled(startScreen, thickness * 0.5f, packed, 12);
        draw.AddCircleFilled(endScreen, thickness * 0.5f, packed, 12);
    }

    private float ProjectedDiameterPixels(Vector3 point, float diameterMeters)
    {
        Vector2 origin = _camera.Project(point);
        Vector2 xEdge = _camera.Project(point + new Vector3(diameterMeters, 0f, 0f));
        Vector2 yEdge = _camera.Project(point + new Vector3(0f, diameterMeters, 0f));
        float xPixels = Vector2.Distance(origin, xEdge);
        float yPixels = Vector2.Distance(origin, yEdge);
        return Math.Max(xPixels, yPixels) * 0.82f;
    }

    private void DrawCenteredPrism(
        ImDrawListPtr draw,
        Vector2 center,
        float halfX,
        float halfY,
        float minZ,
        float maxZ,
        Vector4 color)
    {
        DrawPrism(
            draw,
            center.X - halfX,
            center.X + halfX,
            center.Y - halfY,
            center.Y + halfY,
            minZ,
            maxZ,
            color);
    }

    private static Vector3 WorldPoint(Vector2 xy, float z) => new(xy.X, xy.Y, z);

    private void DrawOperatorHealthBar(ImDrawListPtr draw, CheckpointOperator op)
    {
        // Compact product-style capacity indicator: neutral track with semantic fill.
        float remainingFraction = op.HealthFraction;
        const float halfWidth = 0.30f;
        const float halfDepth = 0.060f;
        const float border = 0.014f;

        float xMin = op.Position.X - halfWidth;
        float xMax = op.Position.X + halfWidth;
        float yMin = op.Position.Y - halfDepth;
        float yMax = op.Position.Y + halfDepth;

        DrawHorizontalRectangle(
            draw,
            xMin,
            xMax,
            yMin,
            yMax,
            1.94f,
            Color(ProfessionalPalette.Border));

        float innerXMin = xMin + border;
        float innerXMax = xMax - border;
        float innerYMin = yMin + border;
        float innerYMax = yMax - border;
        DrawHorizontalRectangle(
            draw,
            innerXMin,
            innerXMax,
            innerYMin,
            innerYMax,
            1.95f,
            Color(ProfessionalPalette.DarkTrack));

        Vector4 fillColor = remainingFraction >= 0.70f
            ? ProfessionalPalette.Success
            : remainingFraction > 0.50f
                ? ProfessionalPalette.Warning
                : ProfessionalPalette.Danger;

        float fillXMax = innerXMin + (innerXMax - innerXMin) * remainingFraction;
        DrawHorizontalRectangle(
            draw,
            innerXMin,
            fillXMax,
            innerYMin,
            innerYMax,
            1.96f,
            Color(fillColor));
    }

    private void DrawLabels(ImDrawListPtr draw, CrowdSimulation simulation)
    {
        SimulationConfig config = simulation.Config;
        uint text = Color(ProfessionalPalette.PrimaryText);

        string funnelLabel = config.Mode == CheckpointMode.MultiLaneBarricade
            ? $"MULTI-LANE CHECKPOINT / {config.MultiLaneCount} x 1m LANES"
            : "FUNNEL / ORDERLY QUEUE";
        draw.AddText(
            _camera.Project(new Vector3(config.FunnelXMax - 2.5f, config.FunnelYMax + 0.38f, 0.05f)),
            text,
            funnelLabel);
        draw.AddText(
            _camera.Project(new Vector3(config.RoomWidth - 2.4f, config.RoomHeight - 0.55f, 0.05f)),
            Color(ProfessionalPalette.FloorText),
            "PASSENGER HALL");
        draw.AddText(
            _camera.Project(new Vector3(config.FunnelXMin, config.FunnelYMax + 0.16f, 0.05f)),
            Color(ProfessionalPalette.SecondaryText),
            "CHECKPOINT");
        draw.AddText(
            _camera.Project(new Vector3(0.10f, config.ExitYMax + 0.18f, 0.05f)),
            Color(ProfessionalPalette.AccentActive),
            "EXIT");

    }

    private void DrawPrism(
        ImDrawListPtr draw,
        float minX,
        float maxX,
        float minY,
        float maxY,
        float minZ,
        float maxZ,
        Vector4 baseColor)
    {
        Vector2 p000 = _camera.Project(new Vector3(minX, minY, minZ));
        Vector2 p100 = _camera.Project(new Vector3(maxX, minY, minZ));
        Vector2 p110 = _camera.Project(new Vector3(maxX, maxY, minZ));
        Vector2 p001 = _camera.Project(new Vector3(minX, minY, maxZ));
        Vector2 p101 = _camera.Project(new Vector3(maxX, minY, maxZ));
        Vector2 p011 = _camera.Project(new Vector3(minX, maxY, maxZ));
        Vector2 p111 = _camera.Project(new Vector3(maxX, maxY, maxZ));

        // The fixed camera looks from +X / -Y / +Z, so these are the three visible faces.
        draw.AddQuadFilled(p000, p100, p101, p001, Color(Shade(baseColor, 0.72f)));
        draw.AddQuadFilled(p100, p110, p111, p101, Color(Shade(baseColor, 0.84f)));
        draw.AddQuadFilled(p001, p101, p111, p011, Color(Shade(baseColor, 1.08f)));
    }

    private void DrawHorizontalRectangle(
        ImDrawListPtr draw,
        float minX,
        float maxX,
        float minY,
        float maxY,
        float z,
        uint color)
    {
        DrawFlatQuad(
            draw,
            new Vector3(minX, minY, z),
            new Vector3(maxX, minY, z),
            new Vector3(maxX, maxY, z),
            new Vector3(minX, maxY, z),
            color);
    }

    private void DrawFlatQuad(
        ImDrawListPtr draw,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        uint color)
    {
        draw.AddQuadFilled(
            _camera.Project(a),
            _camera.Project(b),
            _camera.Project(c),
            _camera.Project(d),
            color);
    }

    private static Vector4 Shade(Vector4 color, float factor) => new(
        Math.Clamp(color.X * factor, 0f, 1f),
        Math.Clamp(color.Y * factor, 0f, 1f),
        Math.Clamp(color.Z * factor, 0f, 1f),
        color.W);

    private static uint Color(Vector4 color) => ImGui.ColorConvertFloat4ToU32(color);

    private static uint Color(float r, float g, float b, float a) =>
        ImGui.ColorConvertFloat4ToU32(new Vector4(r, g, b, a));

    private enum RenderEntityKind
    {
        Wall,
        Pedestrian,
        Operator,
        OperatorPreview,
    }

    private readonly record struct RenderEntity(
        float Depth,
        RenderEntityKind Kind,
        Pedestrian? Pedestrian,
        CheckpointOperator? Operator,
        WallSegment Wall,
        Vector2? PreviewPosition);
}
