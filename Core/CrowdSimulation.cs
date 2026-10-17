using System.Numerics;

namespace CrowdCheckpointSim.Core;

/// <summary>
/// Engine-independent hybrid crowd simulator. Pedestrians use an orderly lane/queue model while
/// inside the funnel, then switch to urgency-influenced Social Force Model (SFM) movement after
/// crossing the checkpoint into the main room. Operators use local-force crowd interaction.
/// </summary>
public sealed class CrowdSimulation
{
    private const float FixedStep = 1f / 60f;
    private const float PedestrianNeighbourRadius = 1.25f;
    private const float SfmPedestrianA = 2.2f;
    private const float SfmPedestrianB = 0.22f;
    private const float SfmBodyStiffness = 18.0f;
    private const float SfmWallA = 2.8f;
    private const float SfmWallB = 0.18f;
    private const float SfmWallStiffness = 28.0f;
    private const float MaxPedestrianAcceleration = 4.0f;
    private const float MaxPedestrianSpeed = 1.65f;
    private const float DasherMaxAcceleration = 7.5f;
    public const int MinSimulationSpeedMultiplier = 1;
    public const int MaxSimulationSpeedMultiplier = 4;
    private const float MultiLaneManualCatchDistance = 0.60f;

    private readonly List<Pedestrian> _pedestrians = new();
    private readonly List<CheckpointOperator> _operators = new();
    private readonly List<Vector2> _operatorPosts = new();
    private readonly Dictionary<Vector2, ResponderKind> _postKinds = new();
    private DetectionQuotaTracker _quota;
    private readonly SpatialHash _spatialHash = new(0.8f);
    private IReadOnlyList<WallSegment> _walls;
    private Random _random;
    private float _accumulator;
    private int _nextPedestrianId = 1;
    private int _currentMinuteIndex;
    private int[] _pendingSpawnsByLane = Array.Empty<int>();
    private float[] _queueLaneYPositions = Array.Empty<float>();
    private readonly List<CheckpointLaneGate> _checkpointGates = new();
    private int? _dasherPedestrianId;
    private readonly HashSet<int> _dasherCatcherOperatorIds = new();
    private float _dasherStateElapsedSeconds;
    private string _lastDasherOutcome = "None";

    public CrowdSimulation(SimulationConfig config)
    {
        config.Validate();
        Config = config.Clone();
        _quota = new DetectionQuotaTracker(Config.YellowFraction, Config.RedFraction);
        _walls = ScenarioGeometry.BuildWalls(Config);
        _random = new Random(Config.RandomSeed);
        Reset();
    }

    public SimulationConfig Config { get; private set; }
    public SimulationMetrics Metrics { get; } = new();
    public IReadOnlyList<Pedestrian> Pedestrians => _pedestrians;
    public IReadOnlyList<CheckpointOperator> Operators => _operators;
    public IReadOnlyList<Vector2> OperatorPosts => _operatorPosts;

    /// <summary>Typed setup definitions, materialized only when saving or inspecting placement.</summary>
    public IReadOnlyList<ResponderPost> ResponderPosts => _operatorPosts
        .Select(position => new ResponderPost(position, _postKinds[position]))
        .ToArray();
    public int FunnelPopulation => _pedestrians.Count(p => p.Phase == PedestrianPhase.Funnel);
    public int RoomPopulation => _pedestrians.Count(p => p.Phase == PedestrianPhase.Room);
    public int PendingSpawnCount => _pendingSpawnsByLane.Sum();
    public int QueueLaneCount => _queueLaneYPositions.Length;
    public IReadOnlyList<CheckpointLaneGate> CheckpointGates => _checkpointGates;

    /// <summary>Selected Dasher pedestrian, if an incident is currently marked or active.</summary>
    public int? DasherPedestrianId => _dasherPedestrianId;

    /// <summary>Current Dasher lifecycle state.</summary>
    public DasherState CurrentDasherState => FindDasher()?.DasherState ?? DasherState.None;

    /// <summary>True only when no Dasher is currently marked/active, enforcing one incident at a time.</summary>
    public bool CanSelectDasher => !_dasherPedestrianId.HasValue;

    /// <summary>Number of operators that have physically reached catch distance on the active Dasher.</summary>
    public int DasherCatcherCount => _dasherCatcherOperatorIds.Count;

    /// <summary>Short status string for the runtime UI.</summary>
    public string DasherStatusText
    {
        get
        {
            Pedestrian? dasher = FindDasher();
            if (dasher is null)
            {
                return _lastDasherOutcome == "None" ? "None" : $"None - {_lastDasherOutcome}";
            }

            return dasher.DasherState switch
            {
                DasherState.MarkedInFunnel => $"Selected in funnel: #{dasher.Id}",
                DasherState.Dashing => $"Dashing: #{dasher.Id}",
                DasherState.SingleOperatorControl => $"1 catcher, stopped - awaiting reinforcement: #{dasher.Id}",
                DasherState.GroupStop => $"{_dasherCatcherOperatorIds.Count} catchers, 1 s stop: #{dasher.Id}",
                DasherState.EscortingToHolding => $"Escorting to holding point: #{dasher.Id}",
                DasherState.HeldAtHoldingPoint => $"Clearance: {_dasherStateElapsedSeconds:F1}/{Config.DasherClearanceSeconds:F0} s",
                _ => $"Dasher #{dasher.Id}",
            };
        }
    }

    /// <summary>Replace configuration and rebuild the scenario, preserving only valid posts.</summary>
    public void Reconfigure(SimulationConfig config)
    {
        config.Validate();
        List<Vector2> posts = _operatorPosts.ToList();
        Config = config.Clone();
        _quota.Configure(Config.YellowFraction, Config.RedFraction);
        _walls = ScenarioGeometry.BuildWalls(Config);
        _operatorPosts.Clear();
        foreach (Vector2 post in posts)
        {
            if (ScenarioGeometry.IsValidOperatorPost(Config, post, _operatorPosts))
            {
                _operatorPosts.Add(post);
            }
            else
            {
                _postKinds.Remove(post);
            }
        }

        Reset();
    }

    /// <summary>Add an operator post during setup when the location is valid.</summary>
    public bool AddOperatorPost(Vector2 point) => AddOperatorPost(point, ResponderKind.PurpleOperator);

    /// <summary>Place a human or robot using the same boundary and spacing guard as the preview.</summary>
    public bool AddOperatorPost(Vector2 point, ResponderKind kind)
    {
        if (!Enum.IsDefined(kind) || !float.IsFinite(point.X) || !float.IsFinite(point.Y) ||
            !ScenarioGeometry.IsValidOperatorPost(Config, point, _operatorPosts))
        {
            return false;
        }

        _operatorPosts.Add(point);
        _postKinds.Add(point, kind);
        ResetOperatorsToPosts();
        return true;
    }

    /// <summary>Remove the closest post within the supplied world-space radius.</summary>
    public bool RemoveNearestOperatorPost(Vector2 point, float radius = 0.65f)
    {
        if (_operatorPosts.Count == 0)
        {
            return false;
        }

        int bestIndex = -1;
        float bestDistance = radius;
        for (int i = 0; i < _operatorPosts.Count; i++)
        {
            float distance = Vector2.Distance(point, _operatorPosts[i]);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
        {
            return false;
        }

        _postKinds.Remove(_operatorPosts[bestIndex]);
        _operatorPosts.RemoveAt(bestIndex);
        ResetOperatorsToPosts();
        return true;
    }

    /// <summary>Clear all setup operator posts.</summary>
    public void ClearOperatorPosts()
    {
        _operatorPosts.Clear();
        _postKinds.Clear();
        ResetOperatorsToPosts();
    }

    /// <summary>Clear only the selected responder type, retaining posts of the other type.</summary>
    public void ClearOperatorPosts(ResponderKind kind)
    {
        foreach (Vector2 post in _operatorPosts.Where(post => _postKinds[post] == kind).ToArray())
        {
            _operatorPosts.Remove(post);
            _postKinds.Remove(post);
        }
        ResetOperatorsToPosts();
    }

    /// <summary>Reset time, crowd, outcomes and operator state while retaining configuration/posts.</summary>
    public void Reset()
    {
        _random = new Random(Config.RandomSeed);
        _accumulator = 0;
        _nextPedestrianId = 1;
        _currentMinuteIndex = 0;
        _queueLaneYPositions = BuildQueueLaneYPositions();
        _pendingSpawnsByLane = new int[_queueLaneYPositions.Length];
        _checkpointGates.Clear();
        if (Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            for (int lane = 0; lane < _queueLaneYPositions.Length; lane++)
            {
                _checkpointGates.Add(new CheckpointLaneGate
                {
                    LaneIndex = lane,
                    CentreY = _queueLaneYPositions[lane],
                });
            }
        }

        _pedestrians.Clear();
        _dasherPedestrianId = null;
        _dasherCatcherOperatorIds.Clear();
        _dasherStateElapsedSeconds = 0f;
        _lastDasherOutcome = "None";
        _quota.ResetInterval();
        Metrics.Reset(_queueLaneYPositions.Length);
        BuildInitialCrowd();
        ResetOperatorsToPosts();
    }

    /// <summary>
    /// Mark one existing funnel pedestrian as the next Dasher. The pedestrian remains in the
    /// orderly queue until it physically crosses the checkpoint. Only one Dasher may be selected
    /// until the current incident ends by escape or detention.
    /// </summary>
    public bool TrySelectDasher(int pedestrianId)
    {
        if (!CanSelectDasher)
        {
            return false;
        }

        Pedestrian? pedestrian = FindPedestrian(pedestrianId);
        if (pedestrian is null || pedestrian.Phase != PedestrianPhase.Funnel)
        {
            return false;
        }

        // A Mode 2 traveller who already completed automated processing is no longer eligible to
        // become the Dasher. In particular, a Red manual-intervention case must remain available
        // for Purple rather than changing incident class while waiting at the barricade.
        if (Config.Mode == CheckpointMode.MultiLaneBarricade &&
            (pedestrian.RequiresManualClearance || pedestrian.DetectionStatus != DetectionStatus.InFunnel))
        {
            return false;
        }

        _dasherPedestrianId = pedestrian.Id;
        _dasherCatcherOperatorIds.Clear();
        _dasherStateElapsedSeconds = 0f;
        _lastDasherOutcome = "Active";
        pedestrian.DasherState = DasherState.MarkedInFunnel;
        return true;
    }

    /// <summary>Advance at normal 1x speed using the fixed 60 Hz simulation timestep.</summary>
    public void Advance(float elapsedSeconds) => Advance(elapsedSeconds, 1);

    /// <summary>
    /// Advance at an integer runtime speed from 1x to 4x. Each multiplier executes additional
    /// fixed-step simulation work rather than multiplying pedestrian velocities or individual
    /// timers, keeping the physical/timing model internally consistent.
    /// </summary>
    public void Advance(float elapsedSeconds, int speedMultiplier)
    {
        if (speedMultiplier < MinSimulationSpeedMultiplier ||
            speedMultiplier > MaxSimulationSpeedMultiplier)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speedMultiplier),
                $"Simulation speed must be between {MinSimulationSpeedMultiplier}x and {MaxSimulationSpeedMultiplier}x.");
        }

        if (!float.IsFinite(elapsedSeconds) || elapsedSeconds <= 0)
        {
            return;
        }

        for (int iteration = 0; iteration < speedMultiplier; iteration++)
        {
            AdvanceOneFrameSlice(elapsedSeconds);
        }
    }

    private void AdvanceOneFrameSlice(float elapsedSeconds)
    {
        _accumulator += Math.Min(elapsedSeconds, 0.25f);
        int steps = 0;
        while (_accumulator >= FixedStep && steps < 12)
        {
            Step(FixedStep);
            _accumulator -= FixedStep;
            steps++;
        }

        if (steps == 12)
        {
            _accumulator = Math.Min(_accumulator, FixedStep);
        }
    }

    private void Step(float dt)
    {
        Metrics.TimeSeconds += dt;
        RollCurrentMinuteIfNeeded();

        AssignOperatorTargets();
        UpdateOperators(dt);
        UpdateAutonomousVerifications(dt);
        Metrics.RecordOperatorActivity(
            _operators.Count(op => op.State != OperatorState.IdleAtPost),
            _operators.Count,
            dt);

        // Only pedestrians that have crossed into the main room use the Social Force Model.
        _spatialHash.Rebuild(_pedestrians);
        Vector2[] accelerations = new Vector2[_pedestrians.Count];
        HashSet<int> controlled = OperatorControlledTargetIds();

        for (int i = 0; i < _pedestrians.Count; i++)
        {
            Pedestrian pedestrian = _pedestrians[i];
            if (pedestrian.Phase != PedestrianPhase.Room ||
                controlled.Contains(pedestrian.Id) ||
                pedestrian.CooperativeStop)
            {
                accelerations[i] = Vector2.Zero;
                continue;
            }

            accelerations[i] = pedestrian.IsDasher
                ? ComputeDasherAcceleration(i, pedestrian)
                : ComputeRoomSfmAcceleration(i, pedestrian);
        }

        for (int i = 0; i < _pedestrians.Count; i++)
        {
            Pedestrian pedestrian = _pedestrians[i];
            if (pedestrian.Phase != PedestrianPhase.Room)
            {
                continue;
            }

            if (controlled.Contains(pedestrian.Id) || pedestrian.CooperativeStop)
            {
                pedestrian.Velocity = Vector2.Zero;
                continue;
            }

            float accelerationLimit = pedestrian.IsDasher ? DasherMaxAcceleration : MaxPedestrianAcceleration;
            float speedLimit = pedestrian.IsDasher ? DasherSpeedLimit(pedestrian) : MaxPedestrianSpeed;
            Vector2 acceleration = ClampMagnitude(accelerations[i], accelerationLimit);
            pedestrian.Velocity += acceleration * dt;
            pedestrian.Velocity = ClampMagnitude(pedestrian.Velocity, speedLimit);
            pedestrian.Position += pedestrian.Velocity * dt;
        }

        ResolveRoomPedestrianOverlap(controlled);
        ResolveOperatorRightOfWayOverlap();
        ResolveDasherRightOfWayOverlap();
        UpdateDasherIncident(dt);

        // The funnel is deliberately NOT SFM-driven. In Mode 1, each lane follows the
        // established orderly-queue model. In Mode 2, each one-metre queue is additionally
        // governed by its own timed barricade gate.
        if (Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            UpdateCheckpointGates(dt);
        }

        for (int lane = 0; lane < _queueLaneYPositions.Length; lane++)
        {
            if (Config.Mode == CheckpointMode.MultiLaneBarricade)
            {
                UpdateMultiLaneQueueLane(lane, dt);
            }
            else
            {
                UpdateQueueLane(lane, dt);
            }
        }

        ProcessCheckpointCrossings();
        ProcessExits();
        SpawnPendingPedestrians();
    }

    private Vector2 ComputeRoomSfmAcceleration(int index, Pedestrian pedestrian)
    {
        Vector2 target = new(-0.75f, pedestrian.ExitTargetY);
        Vector2 desiredDirection = SafeNormal(target - pedestrian.Position);
        float congestion = LocalCongestion(index, pedestrian.Position);
        float urgencySpeed = pedestrian.PreferredSpeed * (1f - Math.Clamp(congestion * 0.035f, 0f, 0.18f));
        float relaxationTime = 0.58f - 0.24f * pedestrian.Urgency;
        Vector2 desiredVelocity = desiredDirection * urgencySpeed;
        Vector2 force = (desiredVelocity - pedestrian.Velocity) / Math.Max(0.24f, relaxationTime);

        foreach (int otherIndex in _spatialHash.Query(pedestrian.Position, PedestrianNeighbourRadius))
        {
            if (otherIndex == index)
            {
                continue;
            }

            Pedestrian other = _pedestrians[otherIndex];
            Vector2 delta = pedestrian.Position - other.Position;
            float distance = Math.Max(0.001f, delta.Length());
            if (distance > PedestrianNeighbourRadius)
            {
                continue;
            }

            Vector2 normal = delta / distance;
            float combinedRadius = Config.AgentRadius * 2f;
            float surfaceGap = distance - combinedRadius;
            float magnitude = SfmPedestrianA * MathF.Exp(-surfaceGap / SfmPedestrianB);
            if (surfaceGap < 0)
            {
                magnitude += -surfaceGap * SfmBodyStiffness;
            }

            force += normal * magnitude;
        }

        foreach (WallSegment wall in _walls)
        {
            Vector2 closest = ScenarioGeometry.ClosestPoint(wall, pedestrian.Position);
            Vector2 delta = pedestrian.Position - closest;
            float distance = Math.Max(0.001f, delta.Length());
            if (distance > 0.90f)
            {
                continue;
            }

            Vector2 normal = delta / distance;
            float surfaceGap = distance - Config.AgentRadius;
            float magnitude = SfmWallA * MathF.Exp(-surfaceGap / SfmWallB);
            if (surfaceGap < 0)
            {
                magnitude += -surfaceGap * SfmWallStiffness;
            }

            force += normal * magnitude;
        }

        ApplyOperatorInteraction(pedestrian, ref force);
        ApplyDasherRightOfWay(pedestrian, ref force);
        return force;
    }

    private Vector2 ComputeDasherAcceleration(int index, Pedestrian dasher)
    {
        if (dasher.DasherState is DasherState.SingleOperatorControl or DasherState.GroupStop or DasherState.HeldAtHoldingPoint)
        {
            return -dasher.Velocity * 12f;
        }

        Vector2 target = dasher.DasherState == DasherState.EscortingToHolding
            ? DasherRules.HoldingPoint(Config)
            : new Vector2(-0.75f, dasher.ExitTargetY);
        float desiredSpeed = dasher.DasherState switch
        {
            DasherState.Dashing => DasherRules.DashSpeed,
            DasherState.SingleOperatorControl => DasherRules.SingleCatchHoldSpeed,
            DasherState.EscortingToHolding => DasherRules.GroupEscortSpeed,
            _ => 0f,
        };

        Vector2 direction = SafeNormal(target - dasher.Position);
        Vector2 desiredVelocity = direction * desiredSpeed;
        Vector2 force = (desiredVelocity - dasher.Velocity) / 0.20f;

        // Retain the same wall-avoidance family as the normal SFM so the Dasher still respects the
        // physical hall geometry. Passenger-to-Dasher repulsion is deliberately reduced because
        // ordinary passengers are the yielding party during this special incident.
        foreach (WallSegment wall in _walls)
        {
            Vector2 closest = ScenarioGeometry.ClosestPoint(wall, dasher.Position);
            Vector2 delta = dasher.Position - closest;
            float distance = Math.Max(0.001f, delta.Length());
            if (distance > 0.90f)
            {
                continue;
            }

            Vector2 normal = delta / distance;
            float surfaceGap = distance - Config.AgentRadius;
            float magnitude = SfmWallA * MathF.Exp(-surfaceGap / SfmWallB);
            if (surfaceGap < 0f)
            {
                magnitude += -surfaceGap * SfmWallStiffness;
            }

            force += normal * magnitude;
        }

        foreach (int otherIndex in _spatialHash.Query(dasher.Position, 0.55f))
        {
            if (otherIndex == index)
            {
                continue;
            }

            Pedestrian other = _pedestrians[otherIndex];
            if (other.IsDasher)
            {
                continue;
            }

            Vector2 delta = dasher.Position - other.Position;
            float distance = Math.Max(0.001f, delta.Length());
            float surfaceGap = distance - Config.AgentRadius * 2f;
            if (surfaceGap < 0.10f)
            {
                force += delta / distance * (0.45f * MathF.Exp(-surfaceGap / 0.18f));
            }
        }

        return force;
    }

    private float DasherSpeedLimit(Pedestrian dasher) => dasher.DasherState switch
    {
        DasherState.Dashing => DasherRules.DashSpeed,
        DasherState.SingleOperatorControl => DasherRules.SingleCatchHoldSpeed,
        DasherState.EscortingToHolding => DasherRules.GroupEscortSpeed,
        _ => 0f,
    };

    private void ApplyDasherRightOfWay(Pedestrian pedestrian, ref Vector2 force)
    {
        Pedestrian? dasher = FindDasher();
        if (dasher is null || pedestrian.Id == dasher.Id ||
            dasher.Phase != PedestrianPhase.Room ||
            dasher.DasherState is DasherState.MarkedInFunnel or DasherState.HeldAtHoldingPoint)
        {
            return;
        }

        Vector2 direction = SafeNormal(dasher.Velocity);
        if (direction.LengthSquared() < 0.5f)
        {
            direction = dasher.DasherState == DasherState.EscortingToHolding
                ? SafeNormal(DasherRules.HoldingPoint(Config) - dasher.Position)
                : SafeNormal(new Vector2(-0.75f, dasher.ExitTargetY) - dasher.Position);
        }

        if (direction.LengthSquared() < 0.5f)
        {
            return;
        }

        Vector2 fromDasher = pedestrian.Position - dasher.Position;
        float forward = Vector2.Dot(fromDasher, direction);
        Vector2 lateralAxis = new(-direction.Y, direction.X);
        float lateral = Vector2.Dot(fromDasher, lateralAxis);

        if (forward <= -0.25f || forward >= DasherRules.YieldInfluenceDistance ||
            MathF.Abs(lateral) >= DasherRules.YieldCorridorHalfWidth)
        {
            return;
        }

        float forwardFactor = 1f - Math.Clamp(Math.Max(0f, forward) / DasherRules.YieldInfluenceDistance, 0f, 1f);
        float centreFactor = 1f - Math.Clamp(MathF.Abs(lateral) / DasherRules.YieldCorridorHalfWidth, 0f, 1f);
        float strength = Math.Clamp(forwardFactor * centreFactor, 0f, 1f);
        float sideSign = ChooseYieldSide(pedestrian, lateralAxis, DasherRules.YieldSideStepDistance);

        force += lateralAxis * sideSign * (3.40f * strength);
        force -= pedestrian.Velocity * (3.20f * strength);

        if (Vector2.Distance(pedestrian.Position, dasher.Position) < 0.75f)
        {
            force += lateralAxis * sideSign * 1.30f;
            force -= pedestrian.Velocity * 2.0f;
        }
    }

    private float LocalCongestion(int index, Vector2 position)
    {
        int count = 0;
        foreach (int other in _spatialHash.Query(position, 0.8f))
        {
            if (other != index &&
                _pedestrians[other].Phase == PedestrianPhase.Room &&
                Vector2.DistanceSquared(position, _pedestrians[other].Position) < 0.64f)
            {
                count++;
            }
        }

        return count;
    }

    private void ApplyOperatorInteraction(Pedestrian pedestrian, ref Vector2 force)
    {
        foreach (CheckpointOperator op in _operators)
        {
            // The assigned Yellow pedestrian remains unaware of the pursuing operator until the
            // cooperative-stop threshold. It must not deliberately sidestep away from its pursuer.
            bool unawarePursuitTarget =
                op.State == OperatorState.Pursuing &&
                op.TargetPedestrianId == pedestrian.Id &&
                !op.TargetHasStopped;
            if (unawarePursuitTarget)
            {
                continue;
            }

            bool movingWithTarget = op.State is OperatorState.Stopping or OperatorState.Escorting or
                OperatorState.DasherControlling or OperatorState.DasherGroupStop or OperatorState.DasherEscorting;
            Pedestrian? escortedTarget =
                movingWithTarget && op.TargetPedestrianId.HasValue
                    ? FindPedestrian(op.TargetPedestrianId.Value)
                    : null;

            // Ordinary short-range body clearance around the operator.
            Vector2 operatorOffset = pedestrian.Position - op.Position;
            float operatorDistance = Math.Max(0.001f, operatorOffset.Length());
            if (operatorDistance < OperatorTiming.EscortYieldInfluenceDistance)
            {
                Vector2 away = operatorOffset / operatorDistance;
                float bodyGap = operatorDistance - (Config.AgentRadius + Config.OperatorRadius);
                force += away * (1.15f * MathF.Exp(-bodyGap / 0.24f));
            }

            // While escorting, the Yellow pedestrian is part of the authorised moving formation.
            // Give non-target pedestrians extra clearance around that second body as well.
            if (escortedTarget is not null && escortedTarget.Id != pedestrian.Id)
            {
                Vector2 targetOffset = pedestrian.Position - escortedTarget.Position;
                float targetDistance = Math.Max(0.001f, targetOffset.Length());
                if (targetDistance < OperatorTiming.EscortYieldInfluenceDistance)
                {
                    Vector2 awayFromTarget = targetOffset / targetDistance;
                    float targetGap = targetDistance - Config.AgentRadius * 2f;
                    force += awayFromTarget * (1.05f * MathF.Exp(-targetGap / 0.24f));
                }
            }

            if (!op.HasRightOfWay)
            {
                continue;
            }

            // Use the operator's intended travel direction, not only its current velocity. A dense
            // crowd can slow the operator almost to zero; if yielding depended on velocity alone,
            // pedestrians would stop yielding precisely when the operator became blocked.
            Vector2 operatorDirection = op.State switch
            {
                OperatorState.Pursuing or OperatorState.DasherPursuing when op.TargetPedestrianId.HasValue =>
                    SafeNormal((FindPedestrian(op.TargetPedestrianId.Value)?.Position ?? op.Position) - op.Position),
                OperatorState.DasherControlling when op.TargetPedestrianId.HasValue =>
                    SafeNormal(FindPedestrian(op.TargetPedestrianId.Value)?.Velocity ?? op.Velocity),
                OperatorState.DasherEscorting or OperatorState.DasherGroupStop =>
                    SafeNormal(DasherRules.HoldingPoint(Config) - op.Position),
                OperatorState.Stopping or OperatorState.Escorting or OperatorState.Returning =>
                    SafeNormal(op.HomePosition - op.Position),
                _ => SafeNormal(op.Velocity),
            };

            if (operatorDirection.LengthSquared() < 0.5f)
            {
                operatorDirection = SafeNormal(op.Velocity);
            }

            if (operatorDirection.LengthSquared() < 0.5f)
            {
                continue;
            }

            bool escortingPair = movingWithTarget && escortedTarget is not null;
            // Every Purple operator away from its home post has right-of-way, irrespective of
            // whether it is pursuing Yellow, escorting, chasing a Dasher, or returning home.
            bool useWidePriorityCorridor = true;
            Vector2 corridorOrigin = escortingPair
                ? (op.Position + escortedTarget!.Position) * 0.5f
                : op.Position;
            float influenceDistance = useWidePriorityCorridor
                ? OperatorTiming.EscortYieldInfluenceDistance
                : OperatorTiming.YieldInfluenceDistance;
            float corridorHalfWidth = useWidePriorityCorridor
                ? OperatorTiming.EscortYieldCorridorHalfWidth
                : OperatorTiming.YieldCorridorHalfWidth;
            float sideStepDistance = useWidePriorityCorridor
                ? OperatorTiming.EscortYieldSideStepDistance
                : OperatorTiming.YieldSideStepDistance;

            Vector2 fromCorridor = pedestrian.Position - corridorOrigin;
            float forward = Vector2.Dot(fromCorridor, operatorDirection);
            Vector2 lateralAxis = new(-operatorDirection.Y, operatorDirection.X);
            float lateral = Vector2.Dot(fromCorridor, lateralAxis);

            if (forward <= 0f || forward >= influenceDistance ||
                MathF.Abs(lateral) >= corridorHalfWidth)
            {
                continue;
            }

            float forwardProximity = 1f - forward / influenceDistance;
            float centrelineFactor = 1f - MathF.Abs(lateral) / corridorHalfWidth;
            float corridorStrength = Math.Clamp(forwardProximity * centrelineFactor, 0f, 1f);

            float sideSign = ChooseYieldSide(pedestrian, lateralAxis, sideStepDistance);
            Vector2 proposedSidePoint =
                pedestrian.Position + lateralAxis * sideSign * sideStepDistance;
            float sideClearance = EstimateClearance(proposedSidePoint, pedestrian.Id);

            bool directlyBlocking =
                forward < OperatorTiming.DirectBlockDistance &&
                MathF.Abs(lateral) < OperatorTiming.DirectBlockHalfWidth;

            if (directlyBlocking && sideClearance < OperatorTiming.MinimumYieldSideClearance)
            {
                // No safe sidestep exists right now. Braking is safer than continuing head-on into
                // the authorised pair. The SFM update will resume normal progress once space opens.
                force -= pedestrian.Velocity * 8.0f;
                continue;
            }

            float lateralStrength = useWidePriorityCorridor
                ? OperatorTiming.EscortYieldLateralStrength
                : 2.10f;
            float brakingGain = useWidePriorityCorridor
                ? OperatorTiming.EscortYieldBrakingGain
                : 1.65f;

            // Sidestep toward the clearer side. Braking is explicitly opposite the pedestrian's
            // own velocity; do NOT push opposite the operator direction, because on the return
            // escort that would accelerate exit-bound pedestrians into the oncoming pair.
            force += lateralAxis * sideSign * lateralStrength * corridorStrength;
            force -= pedestrian.Velocity * brakingGain * corridorStrength;

            if (directlyBlocking)
            {
                force += lateralAxis * sideSign * 1.15f;
                force -= pedestrian.Velocity * 1.75f;
            }
        }
    }

    private float ChooseYieldSide(
        Pedestrian pedestrian,
        Vector2 lateralAxis,
        float sideStepDistance)
    {
        Vector2 left = pedestrian.Position + lateralAxis * sideStepDistance;
        Vector2 right = pedestrian.Position - lateralAxis * sideStepDistance;
        float leftClearance = EstimateClearance(left, pedestrian.Id);
        float rightClearance = EstimateClearance(right, pedestrian.Id);
        return leftClearance >= rightClearance ? 1f : -1f;
    }

    private float EstimateClearance(Vector2 point, int pedestrianId)
    {
        float clearance = 2.0f;
        foreach (Pedestrian other in _pedestrians)
        {
            if (other.Id == pedestrianId)
            {
                continue;
            }

            clearance = Math.Min(clearance, Vector2.Distance(point, other.Position) - Config.AgentRadius * 2f);
        }

        foreach (CheckpointOperator op in _operators)
        {
            clearance = Math.Min(
                clearance,
                Vector2.Distance(point, op.Position) - (Config.AgentRadius + Config.OperatorRadius));
        }

        foreach (WallSegment wall in _walls)
        {
            clearance = Math.Min(clearance, Vector2.Distance(point, ScenarioGeometry.ClosestPoint(wall, point)) - Config.AgentRadius);
        }

        return clearance;
    }

    private void ResolveRoomPedestrianOverlap(HashSet<int> controlledTargetIds)
    {
        _spatialHash.Rebuild(_pedestrians);
        for (int i = 0; i < _pedestrians.Count; i++)
        {
            Pedestrian a = _pedestrians[i];
            if (a.Phase != PedestrianPhase.Room)
            {
                continue;
            }

            foreach (int j in _spatialHash.Query(a.Position, Config.AgentRadius * 2.2f))
            {
                if (j == i)
                {
                    continue;
                }

                Pedestrian b = _pedestrians[j];
                if (b.Phase == PedestrianPhase.Room && j < i)
                {
                    continue;
                }

                Vector2 delta = a.Position - b.Position;
                float distance = delta.Length();
                float minimum = Config.AgentRadius * 2f;
                if (distance >= minimum)
                {
                    continue;
                }

                Vector2 normal = distance > 0.0001f
                    ? delta / distance
                    : UnitFromIds(a.Id, b.Id);

                if (b.Phase == PedestrianPhase.Funnel)
                {
                    // Funnel pedestrians remain locked to their orderly queue. If a newly released
                    // room pedestrian overlaps the front of the queue, correct only the room agent.
                    float correction = minimum - distance + 0.0005f;
                    a.Position += normal * correction;
                    a.Velocity *= 0.96f;
                }
                else
                {
                    float fullCorrection = minimum - distance + 0.0005f;
                    bool aControlled = controlledTargetIds.Contains(a.Id) || IsActiveDasher(a);
                    bool bControlled = controlledTargetIds.Contains(b.Id) || IsActiveDasher(b);

                    if (aControlled && !bControlled)
                    {
                        // The caught Yellow pedestrian belongs to the operator's formation. Keep it
                        // in formation and move the ordinary pedestrian out of the authorised path.
                        b.Position -= normal * fullCorrection;
                        b.Velocity *= 0.82f;
                    }
                    else if (!aControlled && bControlled)
                    {
                        a.Position += normal * fullCorrection;
                        a.Velocity *= 0.82f;
                    }
                    else
                    {
                        float correction = fullCorrection * 0.5f;
                        a.Position += normal * correction;
                        b.Position -= normal * correction;
                        a.Velocity *= 0.98f;
                        b.Velocity *= 0.98f;
                    }
                }
            }
        }
    }

    private void ResolveOperatorRightOfWayOverlap()
    {
        foreach (CheckpointOperator op in _operators)
        {
            if (!op.HasRightOfWay)
            {
                continue;
            }

            bool movingWithTarget = op.State is OperatorState.Stopping or OperatorState.Escorting or
                OperatorState.DasherControlling or OperatorState.DasherGroupStop or OperatorState.DasherEscorting;
            Pedestrian? escortedTarget =
                movingWithTarget && op.TargetPedestrianId.HasValue
                    ? FindPedestrian(op.TargetPedestrianId.Value)
                    : null;

            foreach (Pedestrian pedestrian in _pedestrians)
            {
                if (pedestrian.Phase != PedestrianPhase.Room ||
                    pedestrian.Id == op.TargetPedestrianId)
                {
                    continue;
                }

                SeparatePedestrianFromPriorityBody(
                    pedestrian,
                    op.Position,
                    Config.AgentRadius + Config.OperatorRadius + 0.018f);

                if (escortedTarget is not null)
                {
                    SeparatePedestrianFromPriorityBody(
                        pedestrian,
                        escortedTarget.Position,
                        Config.AgentRadius * 2f + 0.018f);
                }
            }
        }
    }

    private void ResolveDasherRightOfWayOverlap()
    {
        Pedestrian? dasher = FindDasher();
        if (dasher is null || dasher.Phase != PedestrianPhase.Room ||
            dasher.DasherState is DasherState.MarkedInFunnel or DasherState.HeldAtHoldingPoint)
        {
            return;
        }

        foreach (Pedestrian pedestrian in _pedestrians)
        {
            if (pedestrian.Id == dasher.Id || pedestrian.Phase != PedestrianPhase.Room)
            {
                continue;
            }

            SeparatePedestrianFromPriorityBody(
                pedestrian,
                dasher.Position,
                Config.AgentRadius * 2f + 0.035f);
        }
    }

    private static void SeparatePedestrianFromPriorityBody(
        Pedestrian pedestrian,
        Vector2 priorityPosition,
        float minimumDistance)
    {
        Vector2 delta = pedestrian.Position - priorityPosition;
        float distance = delta.Length();
        if (distance >= minimumDistance)
        {
            return;
        }

        Vector2 normal = distance > 0.0001f
            ? delta / distance
            : UnitFromIds(pedestrian.Id, pedestrian.Id + 7919);
        pedestrian.Position += normal * (minimumDistance - distance + 0.001f);
        pedestrian.Velocity *= 0.78f;
    }

    private void ProcessCheckpointCrossings()
    {
        foreach (Pedestrian pedestrian in _pedestrians)
        {
            if (pedestrian.Phase != PedestrianPhase.Funnel)
            {
                continue;
            }

            if (pedestrian.Position.X + Config.AgentRadius >= Config.RoomWidth)
            {
                continue;
            }

            pedestrian.Phase = PedestrianPhase.Room;
            CloseCheckpointGateAfterCrossing(pedestrian);
            if ((uint)pedestrian.QueueLane < (uint)_pendingSpawnsByLane.Length)
            {
                _pendingSpawnsByLane[pedestrian.QueueLane]++;
            }

            if (_dasherPedestrianId == pedestrian.Id && pedestrian.DasherState == DasherState.MarkedInFunnel)
            {
                // The selected person becomes a separate incident class at the checkpoint and is
                // excluded from both Mode 1 and Mode 2 normal outcome percentages.
                pedestrian.DasherState = DasherState.Dashing;
                pedestrian.DetectionStatus = DetectionStatus.DasherIncident;
                pedestrian.BarricadeOutcome = BarricadeOutcome.Pending;
                pedestrian.CooperativeStop = false;
                pedestrian.Velocity = new Vector2(-DasherRules.DashSpeed, 0f);
                _dasherStateElapsedSeconds = 0f;
                continue;
            }

            if (Config.Mode == CheckpointMode.MultiLaneBarricade)
            {
                // Mode 2 outcome metrics are recorded when automated processing finishes because
                // Red is a valid checkpoint outcome even though that traveller does not pass the
                // barricade. Only successful Green/Yellow travellers reach this crossing branch.
                if (!pedestrian.CheckpointOutcomeRecorded)
                {
                    pedestrian.DetectionStatus = DetectionStatus.DetectedAndCounted;
                    pedestrian.BarricadeOutcome = BarricadeOutcome.FirstPassGreen;
                    RecordMode2Outcome(pedestrian);
                }

                Metrics.TotalCrossings++;
                Metrics.CurrentCrossings++;
                continue;
            }

            pedestrian.DetectionStatus = _quota.AssignNext();
            Metrics.TotalCrossings++;
            Metrics.CurrentCrossings++;

            switch (pedestrian.DetectionStatus)
            {
                case DetectionStatus.DetectedAndCounted:
                    Metrics.TotalGreen++;
                    Metrics.CurrentGreen++;
                    break;
                case DetectionStatus.CountedNotDetected:
                    Metrics.TotalYellow++;
                    Metrics.CurrentYellow++;
                    pedestrian.ManualClearanceQueuedAtSeconds = Metrics.TimeSeconds;
                    break;
                case DetectionStatus.NotCountedNotDetected:
                    Metrics.TotalRed++;
                    Metrics.CurrentRed++;
                    break;
            }

            RecordQueueCompletion(pedestrian);
        }
    }

    private void ProcessExits()
    {
        for (int i = _pedestrians.Count - 1; i >= 0; i--)
        {
            Pedestrian pedestrian = _pedestrians[i];
            if (pedestrian.Phase != PedestrianPhase.Room || pedestrian.Position.X + Config.AgentRadius >= 0f)
            {
                continue;
            }

            if (_dasherPedestrianId == pedestrian.Id)
            {
                // Escape is possible only while the Dasher is still actively dashing. Once the
                // first Purple operator has physically caught it, the pair is secured/stationary
                // and the incident must wait for reinforcement rather than disappearing at exit.
                if (pedestrian.DasherState == DasherState.Dashing)
                {
                    _pedestrians.RemoveAt(i);
                    EndDasherIncident("Escaped via exit");
                }
                else
                {
                    pedestrian.Position = new Vector2(
                        Math.Max(Config.AgentRadius, pedestrian.Position.X),
                        Math.Clamp(pedestrian.Position.Y, Config.AgentRadius, Config.RoomHeight - Config.AgentRadius));
                    pedestrian.Velocity = Vector2.Zero;
                }

                continue;
            }

            if (Config.Mode == CheckpointMode.OpenFunnel &&
                pedestrian.DetectionStatus == DetectionStatus.CountedNotDetected &&
                !pedestrian.UndetectedCatchRecorded)
            {
                Metrics.TotalEscapedYellow++;
                Metrics.CurrentEscapedYellow++;
                ReleaseOperatorTarget(pedestrian.Id);
            }

            _pedestrians.RemoveAt(i);
        }
    }

    private void RollCurrentMinuteIfNeeded()
    {
        int minuteIndex = (int)MathF.Floor(Metrics.TimeSeconds / 60f);
        if (minuteIndex == _currentMinuteIndex)
        {
            return;
        }

        _currentMinuteIndex = minuteIndex;
        Metrics.ResetCurrentInterval();
        _quota.ResetInterval();
    }

    private float MultiLaneGateStopX => Config.FunnelXMin + Config.AgentRadius + 0.17f;

    /// <summary>
    /// Advance the independent timed gates used only by Multi-Lane Barricade mode. A gate begins
    /// processing when the front traveller reaches the waiting position, opens after the configured
    /// clearance time, and is closed again as soon as that traveller fully crosses the checkpoint.
    /// </summary>
    private void UpdateCheckpointGates(float dt)
    {
        if (Config.Mode != CheckpointMode.MultiLaneBarricade)
        {
            return;
        }

        float stopX = MultiLaneGateStopX;
        foreach (CheckpointLaneGate gate in _checkpointGates)
        {
            Pedestrian? active = gate.ActivePedestrianId.HasValue
                ? FindPedestrian(gate.ActivePedestrianId.Value)
                : null;

            if (gate.State == CheckpointGateState.Open)
            {
                if (active is null || active.Phase != PedestrianPhase.Funnel)
                {
                    gate.Reset();
                }

                continue;
            }

            if (gate.State == CheckpointGateState.ManualIntervention)
            {
                // A Red two-attempt failure remains visibly before the closed barricade and blocks
                // this lane until Purple physically secures the traveller for manual clearance.
                if (active is null || active.Phase != PedestrianPhase.Funnel || !active.RequiresManualClearance)
                {
                    gate.Reset();
                }
                else
                {
                    active.Position = new Vector2(stopX, gate.CentreY);
                    active.QueueForwardSpeed = 0f;
                    active.Velocity = Vector2.Zero;
                    active.CooperativeStop = true;
                }

                continue;
            }

            if (gate.State == CheckpointGateState.Processing)
            {
                if (active is null || active.Phase != PedestrianPhase.Funnel)
                {
                    gate.Reset();
                    continue;
                }

                if (active.Position.X <= stopX + 0.025f)
                {
                    gate.ProcessingElapsedSeconds += dt;
                    if (gate.ProcessingElapsedSeconds + 0.0001f >= Config.LaneClearanceSeconds)
                    {
                        gate.ProcessingElapsedSeconds = Config.LaneClearanceSeconds;
                        CompleteMultiLaneProcessingAttempt(gate, active);
                    }
                }

                continue;
            }

            Pedestrian? front = _pedestrians
                .Where(p => p.Phase == PedestrianPhase.Funnel && p.QueueLane == gate.LaneIndex)
                .OrderBy(p => p.Position.X)
                .FirstOrDefault();

            if (front is not null && front.Position.X <= stopX + 0.025f)
            {
                front.Position = new Vector2(Math.Max(front.Position.X, stopX), gate.CentreY);
                front.QueueForwardSpeed = 0f;
                front.Velocity = Vector2.Zero;
                front.CooperativeStop = false;
                front.RequiresManualClearance = false;
                front.CheckpointOutcomeRecorded = false;
                front.DetectionStatus = DetectionStatus.InFunnel;
                front.BarricadeOutcome = front.IsDasher
                    ? BarricadeOutcome.Pending
                    : BarricadeOutcomePolicy.Sample(_random, Config.MultiLaneYellowPercent);
                gate.ActivePedestrianId = front.Id;
                gate.ProcessingElapsedSeconds = 0f;
                gate.AttemptNumber = 1;
                gate.State = CheckpointGateState.Processing;
            }
        }
    }

    private void CompleteMultiLaneProcessingAttempt(CheckpointLaneGate gate, Pedestrian active)
    {
        if (active.IsDasher)
        {
            // User-selected Dasher remains outside the normal 90/Yellow/Red workflow. It receives
            // one ordinary gate-processing interval, then activates only after crossing.
            gate.State = CheckpointGateState.Open;
            return;
        }

        if (gate.AttemptNumber <= 1)
        {
            if (active.BarricadeOutcome == BarricadeOutcome.FirstPassGreen)
            {
                active.DetectionStatus = DetectionStatus.DetectedAndCounted;
                RecordMode2Outcome(active);
                gate.State = CheckpointGateState.Open;
                return;
            }

            // Yellow and Red are exactly the 10% that fail the first automated attempt. They stay
            // at the closed gate and undergo the full configured processing time a second time.
            gate.AttemptNumber = 2;
            gate.ProcessingElapsedSeconds = 0f;
            active.QueueForwardSpeed = 0f;
            active.Velocity = Vector2.Zero;
            return;
        }

        if (active.BarricadeOutcome == BarricadeOutcome.SecondPassYellow)
        {
            active.DetectionStatus = DetectionStatus.CountedNotDetected;
            RecordMode2Outcome(active);
            gate.State = CheckpointGateState.Open;
            return;
        }

        // Still unsuccessful after attempt two: turn Red before the barricade and wait for Purple.
        active.BarricadeOutcome = BarricadeOutcome.ManualRed;
        active.DetectionStatus = DetectionStatus.NotCountedNotDetected;
        active.RequiresManualClearance = true;
        active.CooperativeStop = true;
        active.QueueForwardSpeed = 0f;
        active.Velocity = Vector2.Zero;
        active.Position = new Vector2(MultiLaneGateStopX, gate.CentreY);
        RecordMode2Outcome(active);
        gate.State = CheckpointGateState.ManualIntervention;
    }

    private void RecordMode2Outcome(Pedestrian pedestrian)
    {
        if (pedestrian.CheckpointOutcomeRecorded)
        {
            return;
        }

        pedestrian.CheckpointOutcomeRecorded = true;
        switch (pedestrian.DetectionStatus)
        {
            case DetectionStatus.DetectedAndCounted:
                Metrics.TotalGreen++;
                Metrics.CurrentGreen++;
                break;
            case DetectionStatus.CountedNotDetected:
                Metrics.TotalYellow++;
                Metrics.CurrentYellow++;
                break;
            case DetectionStatus.NotCountedNotDetected:
                Metrics.TotalRed++;
                Metrics.CurrentRed++;
                pedestrian.ManualClearanceQueuedAtSeconds ??= Metrics.TimeSeconds;
                break;
        }

        RecordQueueCompletion(pedestrian);
    }

    private void RecordQueueCompletion(Pedestrian pedestrian)
    {
        if (pedestrian.QueueMetricsRecorded)
        {
            return;
        }

        pedestrian.QueueMetricsRecorded = true;
        float queueTimeSeconds = Math.Max(0f, Metrics.TimeSeconds - pedestrian.SpawnTimeSeconds);
        Metrics.RecordQueueCompletion(pedestrian.QueueLane, queueTimeSeconds);
    }

    private void RecordManualClearanceCompletion(Pedestrian pedestrian)
    {
        if (!pedestrian.ManualClearanceQueuedAtSeconds.HasValue)
        {
            return;
        }

        float waitSeconds = Math.Max(
            0f,
            Metrics.TimeSeconds - pedestrian.ManualClearanceQueuedAtSeconds.Value);
        Metrics.RecordManualClearanceCompletion(waitSeconds);
        pedestrian.ManualClearanceQueuedAtSeconds = null;
    }

    private void CloseCheckpointGateAfterCrossing(Pedestrian pedestrian)
    {
        if (Config.Mode != CheckpointMode.MultiLaneBarricade ||
            (uint)pedestrian.QueueLane >= (uint)_checkpointGates.Count)
        {
            return;
        }

        CheckpointLaneGate gate = _checkpointGates[pedestrian.QueueLane];
        if (gate.ActivePedestrianId == pedestrian.Id)
        {
            gate.Reset();
        }
    }

    private void BuildInitialMultiLaneCrowd()
    {
        int population = Config.TargetFunnelPopulation;
        int lanes = Config.MultiLaneCount;
        int capacityPerLane = Config.MultiLaneCapacityPerLane;
        int[] counts = AllocateMultiLaneQueueCounts(population, lanes, capacityPerLane);
        float frontX = MultiLaneGateStopX;

        int created = 0;
        for (int lane = 0; lane < lanes; lane++)
        {
            for (int index = 0; index < counts[lane]; index++)
            {
                float x = frontX + index * SimulationConfig.MultiLaneFullQueueSpacing;
                float y = _queueLaneYPositions[lane] + RandomRange(-0.010f, 0.010f);
                _pedestrians.Add(CreatePedestrian(lane, new Vector2(x, y)));
                created++;
            }
        }

        if (created != population)
        {
            throw new InvalidOperationException(
                $"Initial Multi-Lane queue generation created {created} pedestrians; expected {population}.");
        }
    }

    private int[] AllocateMultiLaneQueueCounts(int population, int laneCount, int capacityPerLane)
    {
        int[] counts = new int[laneCount];
        int totalCapacity = laneCount * capacityPerLane;
        population = Math.Clamp(population, laneCount, totalCapacity);

        if (population == totalCapacity)
        {
            Array.Fill(counts, capacityPerLane);
            return counts;
        }

        // Keep every checkpoint lane operational while allowing visibly different queue lengths.
        Array.Fill(counts, 1);
        int remaining = population - laneCount;
        float[] weights = new float[laneCount];
        for (int lane = 0; lane < laneCount; lane++)
        {
            weights[lane] = RandomRange(0.72f, 1.28f);
        }

        while (remaining > 0)
        {
            float availableWeight = 0f;
            for (int lane = 0; lane < laneCount; lane++)
            {
                if (counts[lane] < capacityPerLane)
                {
                    availableWeight += weights[lane];
                }
            }

            if (availableWeight <= 0f)
            {
                break;
            }

            float draw = RandomRange(0f, availableWeight);
            for (int lane = 0; lane < laneCount; lane++)
            {
                if (counts[lane] >= capacityPerLane)
                {
                    continue;
                }

                draw -= weights[lane];
                if (draw <= 0f)
                {
                    counts[lane]++;
                    remaining--;
                    break;
                }
            }
        }

        bool allEqual = laneCount > 1 && counts.All(count => count == counts[0]);
        if (allEqual && counts[0] > 1 && counts[1] < capacityPerLane)
        {
            counts[0]--;
            counts[1]++;
        }

        return counts;
    }

    private void BuildInitialCrowd()
    {
        if (Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            BuildInitialMultiLaneCrowd();
            return;
        }

        int population = Config.TargetFunnelPopulation;
        int rows = Config.QueueRows;
        int columns = Config.QueueColumns;

        float centreMargin = Config.AgentRadius + Config.QueueWallClearance;
        float frontX = Config.FunnelXMin + centreMargin;
        float rearX = Config.FunnelXMax - centreMargin;

        int basePeoplePerRow = population / rows;
        int rowsWithExtraPerson = population % rows;
        int created = 0;

        for (int row = 0; row < rows && created < population; row++)
        {
            float rowT = rows <= 1 ? 0.5f : row / (float)(rows - 1);
            float baseX = Lerp(frontX, rearX, rowT);
            int peopleInRow = basePeoplePerRow + (row < rowsWithExtraPerson ? 1 : 0);
            peopleInRow = Math.Min(peopleInRow, columns);

            int unusedColumns = columns - peopleInRow;
            int startColumn = unusedColumns / 2;
            if ((unusedColumns & 1) == 1 && (row & 1) == 1)
            {
                startColumn++;
            }

            float rowYShift = (row & 1) == 0 ? -0.010f : 0.010f;
            for (int lane = startColumn; lane < startColumn + peopleInRow && created < population; lane++)
            {
                int columnPattern = lane % 3 - 1;
                float x = baseX + columnPattern * 0.0025f;
                float y = _queueLaneYPositions[lane] + rowYShift;
                _pedestrians.Add(CreatePedestrian(lane, new Vector2(x, y)));
                created++;
            }
        }

        if (created != population)
        {
            throw new InvalidOperationException(
                $"Initial queue generation created {created} pedestrians; expected {population}.");
        }
    }

    private float[] BuildQueueLaneYPositions()
    {
        int columns = Config.QueueColumns;
        float[] lanes = new float[columns];

        if (Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            for (int lane = 0; lane < columns; lane++)
            {
                lanes[lane] = Config.FunnelYMin + (lane + 0.5f) * SimulationConfig.MultiLaneWidth;
            }

            return lanes;
        }
        float centreMargin = Config.AgentRadius + Config.QueueWallClearance;
        float lowerY = Config.FunnelYMin + centreMargin;
        float upperY = Config.FunnelYMax - centreMargin;

        if (columns == 1)
        {
            lanes[0] = (Config.FunnelYMin + Config.FunnelYMax) * 0.5f;
            return lanes;
        }

        for (int lane = 0; lane < columns; lane++)
        {
            lanes[lane] = Lerp(lowerY, upperY, lane / (float)(columns - 1));
        }

        return lanes;
    }

    private Pedestrian CreatePedestrian(int queueLane, Vector2 position)
    {
        float urgency = (float)_random.NextDouble();
        float preferredSpeed;
        float speedDraw = (float)_random.NextDouble();
        if (speedDraw < 0.65f)
        {
            preferredSpeed = RandomRange(0.85f, 1.15f);
        }
        else if (speedDraw < 0.92f)
        {
            preferredSpeed = RandomRange(1.10f, 1.35f);
        }
        else
        {
            preferredSpeed = RandomRange(1.30f, 1.55f);
        }

        // Urgency is intentionally not used to push/squeeze the orderly funnel queue. It becomes
        // active only after checkpoint release, where the pedestrian switches to room SFM motion.
        // Rescale the existing three-band random draw; keep its mixture and RNG call order intact.
        // The default interval takes the original value unchanged, preserving seeded behaviour.
        if (Config.PassengerMinimumSpeed != SimulationConfig.DefaultPassengerMinimumSpeed ||
            Config.PassengerMaximumSpeed != SimulationConfig.DefaultPassengerMaximumSpeed)
        {
            float fraction = (preferredSpeed - SimulationConfig.DefaultPassengerMinimumSpeed) /
                (SimulationConfig.DefaultPassengerMaximumSpeed - SimulationConfig.DefaultPassengerMinimumSpeed);
            preferredSpeed = Lerp(Config.PassengerMinimumSpeed, Config.PassengerMaximumSpeed, fraction);
        }
        preferredSpeed = Math.Clamp(preferredSpeed, Config.PassengerMinimumSpeed, Config.PassengerMaximumSpeed);
        float halfExit = Math.Max(0.05f, Config.ExitWidth * 0.5f - Config.AgentRadius - 0.08f);
        float exitY = Config.RoomHeight * 0.5f + RandomRange(-halfExit, halfExit);

        return new Pedestrian
        {
            Id = _nextPedestrianId++,
            QueueLane = queueLane,
            Position = position,
            Velocity = Vector2.Zero,
            SpawnTimeSeconds = Metrics.TimeSeconds,
            Urgency = urgency,
            PreferredSpeed = preferredSpeed,
            ExitTargetY = exitY,
            QueueForwardSpeed = 0f,
            QueuePhase = RandomRange(0f, MathF.Tau),
        };
    }

    private void UpdateMultiLaneQueueLane(int lane, float dt)
    {
        List<Pedestrian> queued = _pedestrians
            .Where(p => p.Phase == PedestrianPhase.Funnel && p.QueueLane == lane)
            .OrderBy(p => p.Position.X)
            .ToList();

        if (queued.Count == 0)
        {
            return;
        }

        float minimumSpacing = SimulationConfig.MultiLaneFullQueueSpacing;
        const float maximumAcceleration = 0.75f;
        const float maximumDeceleration = 1.20f;
        float stopX = MultiLaneGateStopX;
        CheckpointLaneGate gate = _checkpointGates[lane];

        for (int queueIndex = 0; queueIndex < queued.Count; queueIndex++)
        {
            Pedestrian pedestrian = queued[queueIndex];
            float phaseFraction = 0.5f + 0.5f * MathF.Sin(pedestrian.QueuePhase);
            float reactionTime = 0.42f + 0.16f * phaseFraction;
            float maximumQueueSpeed = Math.Min(0.62f, pedestrian.PreferredSpeed * 0.48f);

            float targetSpeed;
            if (queueIndex == 0)
            {
                bool releasedByGate = gate.State == CheckpointGateState.Open &&
                    gate.ActivePedestrianId == pedestrian.Id;
                if (releasedByGate)
                {
                    targetSpeed = maximumQueueSpeed;
                }
                else
                {
                    float availableDistance = pedestrian.Position.X - stopX;
                    targetSpeed = Math.Min(
                        maximumQueueSpeed,
                        Math.Max(0f, availableDistance / reactionTime));
                }
            }
            else
            {
                Pedestrian personAhead = queued[queueIndex - 1];
                float centreDistance = pedestrian.Position.X - personAhead.Position.X;
                float availableDistance = centreDistance - minimumSpacing;
                targetSpeed = Math.Min(
                    maximumQueueSpeed,
                    Math.Max(0f, availableDistance / reactionTime));
            }

            float requestedAcceleration = (targetSpeed - pedestrian.QueueForwardSpeed) / reactionTime;
            float acceleration = Math.Clamp(
                requestedAcceleration,
                -maximumDeceleration,
                maximumAcceleration);

            pedestrian.QueueForwardSpeed = Math.Clamp(
                pedestrian.QueueForwardSpeed + acceleration * dt,
                0f,
                maximumQueueSpeed);

            Vector2 position = pedestrian.Position;
            position.X -= pedestrian.QueueForwardSpeed * dt;

            if (queueIndex == 0 &&
                !(gate.State == CheckpointGateState.Open && gate.ActivePedestrianId == pedestrian.Id) &&
                position.X < stopX)
            {
                position.X = stopX;
                pedestrian.QueueForwardSpeed = 0f;
            }

            float laneCentre = _queueLaneYPositions[lane];
            float naturalOffset = MathF.Sin(Metrics.TimeSeconds * 0.55f + pedestrian.QueuePhase) * 0.008f;
            float desiredY = laneCentre + naturalOffset;
            float lateralDifference = desiredY - position.Y;
            float maximumLateralStep = 0.030f * dt;
            float lateralStep = Math.Clamp(lateralDifference, -maximumLateralStep, maximumLateralStep);
            position.Y += lateralStep;

            pedestrian.Position = position;
            pedestrian.Velocity = new Vector2(
                -pedestrian.QueueForwardSpeed,
                dt > 0f ? lateralStep / dt : 0f);
        }

        // Preserve single-file ordering and the near-back-to-front spacing used at maximum density.
        for (int queueIndex = 1; queueIndex < queued.Count; queueIndex++)
        {
            Pedestrian personAhead = queued[queueIndex - 1];
            Pedestrian current = queued[queueIndex];
            float minimumX = personAhead.Position.X + minimumSpacing;
            if (current.Position.X < minimumX)
            {
                current.Position = new Vector2(minimumX, current.Position.Y);
                current.QueueForwardSpeed = Math.Min(current.QueueForwardSpeed, personAhead.QueueForwardSpeed);
                current.Velocity = new Vector2(-current.QueueForwardSpeed, current.Velocity.Y);
            }
        }
    }

    private void UpdateQueueLane(int lane, float dt)
    {
        List<Pedestrian> queued = _pedestrians
            .Where(p => p.Phase == PedestrianPhase.Funnel && p.QueueLane == lane)
            .OrderBy(p => p.Position.X)
            .ToList();

        if (queued.Count == 0)
        {
            return;
        }

        float minimumSpacing = 2f * Config.AgentRadius + 0.002f;
        const float maximumAcceleration = 0.75f;
        const float maximumDeceleration = 1.20f;

        for (int queueIndex = 0; queueIndex < queued.Count; queueIndex++)
        {
            Pedestrian pedestrian = queued[queueIndex];
            float phaseFraction = 0.5f + 0.5f * MathF.Sin(pedestrian.QueuePhase);
            float reactionTime = 0.42f + 0.16f * phaseFraction;
            float queueSpeedFactor = 0.92f + 0.08f * (1f - phaseFraction);
            float maximumQueueSpeed = Math.Min(
                0.56f,
                pedestrian.PreferredSpeed * 0.44f * queueSpeedFactor);

            float targetSpeed;
            if (queueIndex == 0)
            {
                Pedestrian? closestReleasedAhead = _pedestrians
                    .Where(candidate =>
                        candidate.Phase == PedestrianPhase.Room &&
                        candidate.QueueLane == lane &&
                        candidate.Position.X < pedestrian.Position.X)
                    .OrderByDescending(candidate => candidate.Position.X)
                    .FirstOrDefault();

                if (closestReleasedAhead is not null)
                {
                    float centreDistance = pedestrian.Position.X - closestReleasedAhead.Position.X;
                    float availableDistance = centreDistance - minimumSpacing;
                    targetSpeed = Math.Min(
                        maximumQueueSpeed,
                        Math.Max(0f, availableDistance / reactionTime));
                }
                else
                {
                    // Only the initial front row gets a small per-person start delay, preventing
                    // all queue lanes from releasing at exactly the same instant.
                    float initialStartDelay = 0.10f + 0.55f * phaseFraction;
                    targetSpeed = Metrics.TimeSeconds < initialStartDelay ? 0f : maximumQueueSpeed;
                }
            }
            else
            {
                Pedestrian personAhead = queued[queueIndex - 1];
                float centreDistance = pedestrian.Position.X - personAhead.Position.X;
                float availableDistance = centreDistance - minimumSpacing;
                targetSpeed = Math.Min(
                    maximumQueueSpeed,
                    Math.Max(0f, availableDistance / reactionTime));
            }

            float requestedAcceleration = (targetSpeed - pedestrian.QueueForwardSpeed) / reactionTime;
            float acceleration = Math.Clamp(
                requestedAcceleration,
                -maximumDeceleration,
                maximumAcceleration);

            pedestrian.QueueForwardSpeed = Math.Clamp(
                pedestrian.QueueForwardSpeed + acceleration * dt,
                0f,
                maximumQueueSpeed);

            Vector2 position = pedestrian.Position;
            position.X -= pedestrian.QueueForwardSpeed * dt;

            float laneCentre = _queueLaneYPositions[lane];
            float naturalOffset = MathF.Sin(Metrics.TimeSeconds * 0.7f + pedestrian.QueuePhase) * 0.004f;
            float desiredY = laneCentre + naturalOffset;
            float lateralDifference = desiredY - position.Y;
            float maximumLateralStep = 0.025f * dt;
            float lateralStep = Math.Clamp(lateralDifference, -maximumLateralStep, maximumLateralStep);
            position.Y += lateralStep;

            pedestrian.Position = position;
            pedestrian.Velocity = new Vector2(
                -pedestrian.QueueForwardSpeed,
                dt > 0f ? lateralStep / dt : 0f);
        }

        // Correct any numerical compression without applying SFM or changing queue order.
        for (int queueIndex = 1; queueIndex < queued.Count; queueIndex++)
        {
            Pedestrian personAhead = queued[queueIndex - 1];
            Pedestrian current = queued[queueIndex];
            float minimumX = personAhead.Position.X + minimumSpacing;
            if (current.Position.X < minimumX)
            {
                current.Position = new Vector2(minimumX, current.Position.Y);
                current.QueueForwardSpeed = Math.Min(
                    current.QueueForwardSpeed,
                    personAhead.QueueForwardSpeed);
                current.Velocity = new Vector2(-current.QueueForwardSpeed, current.Velocity.Y);
            }
        }
    }

    private void SpawnPendingPedestrians()
    {
        if (_pendingSpawnsByLane.Length == 0)
        {
            return;
        }

        float spawnX = Config.FunnelXMax - Config.AgentRadius - Config.QueueWallClearance;
        float minimumSpacing = Config.Mode == CheckpointMode.MultiLaneBarricade
            ? SimulationConfig.MultiLaneFullQueueSpacing
            : 2f * Config.AgentRadius + 0.002f;

        for (int lane = 0; lane < _pendingSpawnsByLane.Length; lane++)
        {
            while (_pendingSpawnsByLane[lane] > 0)
            {
                Pedestrian? rear = _pedestrians
                    .Where(p => p.Phase == PedestrianPhase.Funnel && p.QueueLane == lane)
                    .OrderByDescending(p => p.Position.X)
                    .FirstOrDefault();

                if (rear is not null)
                {
                    float rearGap = spawnX - rear.Position.X;
                    if (rearGap < minimumSpacing)
                    {
                        break;
                    }
                }

                float lateralVariation = Config.Mode == CheckpointMode.MultiLaneBarricade
                    ? 0.010f
                    : 0.004f;
                float spawnY = _queueLaneYPositions[lane] + RandomRange(-lateralVariation, lateralVariation);
                _pedestrians.Add(CreatePedestrian(lane, new Vector2(spawnX, spawnY)));
                _pendingSpawnsByLane[lane]--;
            }
        }
    }

    private void ResetOperatorsToPosts()
    {
        _operators.Clear();
        for (int i = 0; i < _operatorPosts.Count; i++)
        {
            _operators.Add(new CheckpointOperator
            {
                Id = i + 1,
                Kind = _postKinds[_operatorPosts[i]],
                Position = _operatorPosts[i],
                HomePosition = _operatorPosts[i],
                Velocity = Vector2.Zero,
                InitialVerificationSeconds = _postKinds[_operatorPosts[i]] == ResponderKind.WheeledRobot
                    ? Config.RobotManualClearanceSeconds
                    : Config.Mode == CheckpointMode.MultiLaneBarricade
                        ? Config.MultiLaneManualClearanceSeconds
                        : Config.InitialManualClearanceSeconds,
                UseVerificationFatigue = _postKinds[_operatorPosts[i]] == ResponderKind.PurpleOperator &&
                    Config.Mode == CheckpointMode.OpenFunnel,
                VerificationFatigueIncreasePercent = Config.ManualClearanceFatigueIncreasePercent,
                InitialInterceptionSpeed = Config.Mode == CheckpointMode.OpenFunnel
                    ? Config.OperatorInterceptionSpeed
                    : OperatorTiming.MaxWalkingSpeed,
                InterceptionSpeedLossPercent = Config.Mode == CheckpointMode.OpenFunnel
                    ? Config.OperatorInterceptionSpeedLossPercent
                    : 0f,
            });
        }
    }

    private void AssignOperatorTargets()
    {
        // Dasher remains the highest-priority Purple task in both modes.
        PreemptOperatorsForDasher();

        List<CheckpointOperator> available = _operators
            .Where(op => op.State == OperatorState.IdleAtPost ||
                (op.IsRobot && op.State == OperatorState.Returning && op.CheckForNextTargetAfterClearance))
            .ToList();
        if (available.Count == 0)
        {
            return;
        }

        AssignDasherResponders(available);
        if (available.Count == 0)
        {
            return;
        }

        HashSet<int> assigned = _operators
            .Where(op => op.TargetPedestrianId.HasValue)
            .Select(op => op.TargetPedestrianId!.Value)
            .ToHashSet();

        List<Pedestrian> candidates = _pedestrians
            .Where(p => IsNormalOperatorInterventionTarget(p) &&
                        !p.AutonomousVerificationActive &&
                        !assigned.Contains(p.Id))
            .ToList();

        // Human reservations are established first, including visual-confirmation locks. Robots
        // then select only the remaining targets. A target is removed immediately on assignment.
        if (!available.Any(op => op.IsRobot))
        {
            AssignNormalTargets(available, candidates);
            return;
        }
        AssignNormalTargets(available.Where(op => !op.IsRobot).ToList(), candidates);
        AssignNormalTargets(available.Where(op => op.IsRobot).ToList(), candidates);

        // A completed robot checks once from its current position, after human reservations.
        // With no eligible nearby target it keeps Returning and starts its original dock trip.
        foreach (CheckpointOperator op in available)
        {
            op.CheckForNextTargetAfterClearance = false;
        }
    }

    private void AssignNormalTargets(List<CheckpointOperator> available, List<Pedestrian> candidates)
    {
        while (available.Count > 0 && candidates.Count > 0)
        {
            CheckpointOperator? bestOperator = null;
            Pedestrian? bestTarget = null;
            float bestScore = float.PositiveInfinity;

            foreach (CheckpointOperator op in available)
            {
                foreach (Pedestrian target in candidates)
                {
                    // Mode 1 Yellow is a visual/acquisition task and therefore obeys the configured
                    // radius. Mode 2 Red is an explicit barricade failure alarm, so a free Purple
                    // operator is dispatched even when the failed traveller is outside that radius.
                    bool requiresAcquisitionRadius = Config.Mode == CheckpointMode.OpenFunnel ||
                        (op.IsRobot && op.CheckForNextTargetAfterClearance);
                    if (requiresAcquisitionRadius &&
                        !OperatorAcquisition.IsWithinRadius(
                            op.Position,
                            target.Position,
                            Config.OperatorAcquisitionRadius))
                    {
                        continue;
                    }

                    Vector2 predicted = target.Position + target.Velocity * 0.75f;
                    float distance = Vector2.Distance(predicted, op.Position);
                    float estimatedInterceptTime = distance / op.CurrentInterceptionSpeed;
                    float exitUrgency = Config.Mode == CheckpointMode.OpenFunnel
                        ? Math.Max(0f, (4f - target.Position.X) * 0.18f)
                        : 0f;
                    float score = estimatedInterceptTime - exitUrgency;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestOperator = op;
                        bestTarget = target;
                    }
                }
            }

            if (bestOperator is null || bestTarget is null)
            {
                break;
            }

            bool mode2Red = IsMode2ManualRed(bestTarget);
            bestOperator.State = mode2Red || bestOperator.IsRobot
                ? OperatorState.Pursuing
                : OperatorState.ConfirmingYellow;
            bestOperator.TargetPedestrianId = bestTarget.Id;
            bestOperator.TargetHasStopped = mode2Red;
            bestOperator.StopElapsedSeconds = 0f;
            bestOperator.VerificationElapsedSeconds = 0f;
            bestOperator.TargetConfirmationElapsedSeconds = 0f;
            bestOperator.DasherReturnRightOfWay = false;
            if (mode2Red)
            {
                bestTarget.CooperativeStop = true;
                bestTarget.Velocity = Vector2.Zero;
            }

            available.Remove(bestOperator);
            candidates.Remove(bestTarget);
        }
    }

    private bool IsNormalOperatorInterventionTarget(Pedestrian pedestrian)
    {
        if (Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            return pedestrian.RequiresManualClearance &&
                pedestrian.DetectionStatus == DetectionStatus.NotCountedNotDetected &&
                pedestrian.Phase is PedestrianPhase.Funnel or PedestrianPhase.Room;
        }

        return pedestrian.Phase == PedestrianPhase.Room &&
            pedestrian.DetectionStatus == DetectionStatus.CountedNotDetected;
    }

    private bool IsMode2ManualRed(Pedestrian pedestrian) =>
        Config.Mode == CheckpointMode.MultiLaneBarricade &&
        pedestrian.RequiresManualClearance &&
        pedestrian.DetectionStatus == DetectionStatus.NotCountedNotDetected;

    private void PreemptOperatorsForDasher()
    {
        Pedestrian? dasher = FindDasher();
        if (dasher is null || dasher.Phase != PedestrianPhase.Room ||
            dasher.DasherState is not (DasherState.Dashing or DasherState.SingleOperatorControl))
        {
            return;
        }

        foreach (CheckpointOperator op in _operators.ToList())
        {
            // Idle operators are handled by AssignDasherResponders below. Operators already
            // responding to, controlling, or escorting the current Dasher stay on that task.
            if (op.IsRobot || op.State == OperatorState.IdleAtPost ||
                op.State is OperatorState.DasherPursuing or OperatorState.DasherControlling or
                    OperatorState.DasherGroupStop or OperatorState.DasherEscorting)
            {
                continue;
            }

            if (!OperatorAcquisition.IsWithinRadius(
                    op.Position,
                    dasher.Position,
                    Config.OperatorAcquisitionRadius))
            {
                continue;
            }

            Pedestrian? normalTarget = op.TargetPedestrianId.HasValue
                ? FindPedestrian(op.TargetPedestrianId.Value)
                : null;

            if (normalTarget is not null && IsNormalOperatorInterventionTarget(normalTarget))
            {
                if (op.State == OperatorState.Verifying)
                {
                    // Verification already started: preserve the unfinished portion autonomously
                    // while Purple responds to the higher-priority Dasher. This works for both
                    // Mode 1 Yellow and Mode 2 Red manual-clearance cases.
                    float remainingSeconds = Math.Max(
                        0f,
                        op.CurrentVerificationDuration - op.VerificationElapsedSeconds);

                    normalTarget.AutonomousVerificationActive = true;
                    normalTarget.AutonomousVerificationRemainingSeconds = remainingSeconds;
                    normalTarget.CooperativeStop = true;
                    normalTarget.Velocity = Vector2.Zero;
                }
                else if (IsMode2ManualRed(normalTarget))
                {
                    // A failed Mode 2 traveller cannot resume ordinary flow. Keep the Red stopped
                    // wherever the intervention was interrupted so Purple can re-acquire it later.
                    normalTarget.CooperativeStop = true;
                    normalTarget.Velocity = Vector2.Zero;
                }
                else
                {
                    // Mode 1 Yellow is released back to ordinary Undetected SFM movement until a
                    // Purple operator becomes available again after the Dasher incident.
                    normalTarget.CooperativeStop = false;
                    normalTarget.Velocity = Vector2.Zero;
                }
            }

            op.State = OperatorState.DasherPursuing;
            op.TargetPedestrianId = dasher.Id;
            op.TargetHasStopped = false;
            op.StopElapsedSeconds = 0f;
            op.VerificationElapsedSeconds = 0f;
            op.TargetConfirmationElapsedSeconds = 0f;
            op.DasherReturnRightOfWay = false;
        }
    }

    private void UpdateAutonomousVerifications(float dt)
    {
        foreach (Pedestrian pedestrian in _pedestrians)
        {
            if (!pedestrian.AutonomousVerificationActive)
            {
                continue;
            }

            pedestrian.CooperativeStop = true;
            pedestrian.Velocity = Vector2.Zero;
            pedestrian.AutonomousVerificationRemainingSeconds = Math.Max(
                0f,
                pedestrian.AutonomousVerificationRemainingSeconds - dt);

            if (pedestrian.AutonomousVerificationRemainingSeconds > 0f)
            {
                continue;
            }

            pedestrian.AutonomousVerificationActive = false;
            pedestrian.AutonomousVerificationRemainingSeconds = 0f;
            pedestrian.DetectionStatus = DetectionStatus.DetectedAndCounted;
            pedestrian.RequiresManualClearance = false;
            pedestrian.CooperativeStop = false;
            pedestrian.Velocity = Vector2.Zero;
            RecordManualClearanceCompletion(pedestrian);
            Metrics.TotalManualVerified++;
        }
    }

    private void ReleaseMode2RedFromBarricadeForEscort(Pedestrian target)
    {
        if (Config.Mode != CheckpointMode.MultiLaneBarricade ||
            target.Phase != PedestrianPhase.Funnel ||
            (uint)target.QueueLane >= (uint)_checkpointGates.Count)
        {
            return;
        }

        CheckpointLaneGate gate = _checkpointGates[target.QueueLane];
        if (gate.ActivePedestrianId == target.Id)
        {
            gate.Reset();
        }

        if ((uint)target.QueueLane < (uint)_pendingSpawnsByLane.Length)
        {
            _pendingSpawnsByLane[target.QueueLane]++;
        }

        // Purple collects the failed traveller from the hall side of the barricade. Once secured,
        // the traveller leaves the public queue with Purple rather than opening the gate normally.
        target.Phase = PedestrianPhase.Room;
        target.Position = new Vector2(
            Math.Clamp(Config.RoomWidth - Config.AgentRadius - 0.04f, Config.AgentRadius, Config.RoomWidth - Config.AgentRadius),
            Math.Clamp(target.Position.Y, Config.AgentRadius, Config.RoomHeight - Config.AgentRadius));
        target.QueueForwardSpeed = 0f;
        target.Velocity = Vector2.Zero;
        target.CooperativeStop = true;
    }

    private void AssignDasherResponders(List<CheckpointOperator> available)
    {
        Pedestrian? dasher = FindDasher();
        if (dasher is null || dasher.Phase != PedestrianPhase.Room ||
            dasher.DasherState is not (DasherState.Dashing or DasherState.SingleOperatorControl))
        {
            return;
        }

        // Unlike normal Undetected assignment, every currently free operator that can physically
        // see the Dasher inside the configured acquisition radius may respond to the same target.
        foreach (CheckpointOperator op in available.ToList())
        {
            if (op.IsRobot || !OperatorAcquisition.IsWithinRadius(
                    op.Position,
                    dasher.Position,
                    Config.OperatorAcquisitionRadius))
            {
                continue;
            }

            op.State = OperatorState.DasherPursuing;
            op.TargetPedestrianId = dasher.Id;
            op.TargetHasStopped = false;
            op.StopElapsedSeconds = 0f;
            op.VerificationElapsedSeconds = 0f;
            op.TargetConfirmationElapsedSeconds = 0f;
            op.DasherReturnRightOfWay = false;
            available.Remove(op);
        }
    }

    private void UpdateOperators(float dt)
    {
        foreach (CheckpointOperator op in _operators)
        {
            Pedestrian? target = op.TargetPedestrianId.HasValue
                ? FindPedestrian(op.TargetPedestrianId.Value)
                : null;

            // Robots never enter Dasher response, including a defensive guard for stale state.
            if (op.IsRobot && op.State is (OperatorState.DasherPursuing or
                OperatorState.DasherControlling or OperatorState.DasherGroupStop or OperatorState.DasherEscorting))
            {
                SendOperatorHome(op);
                continue;
            }

            switch (op.State)
            {
                case OperatorState.IdleAtPost:
                    op.Position = op.HomePosition;
                    op.Velocity = Vector2.Zero;
                    break;

                case OperatorState.ConfirmingYellow:
                    if (Config.Mode != CheckpointMode.OpenFunnel ||
                        target is null || target.DetectionStatus != DetectionStatus.CountedNotDetected)
                    {
                        SendOperatorHome(op);
                        break;
                    }

                    // Yellow-only visual search / confirmation phase. Purple has locked the target
                    // but remains stationary for the configured delay before pursuit begins. The
                    // Yellow continues ordinary SFM movement during this phase. Dasher response
                    // never enters this state and retains its existing immediate pursuit logic.
                    op.Position = op.HomePosition;
                    op.Velocity = Vector2.Zero;
                    op.TargetConfirmationElapsedSeconds += dt;
                    if (op.TargetConfirmationElapsedSeconds >= Config.YellowVisualSearchSeconds)
                    {
                        op.TargetConfirmationElapsedSeconds = 0f;
                        op.State = OperatorState.Pursuing;
                    }
                    break;

                case OperatorState.Pursuing:
                    if (target is null || !IsNormalOperatorInterventionTarget(target))
                    {
                        SendOperatorHome(op);
                        break;
                    }

                    MoveOperatorTowards(op, target.Position, op.CurrentInterceptionSpeed, dt, target.Id);
                    float distance = Vector2.Distance(op.Position, target.Position);
                    if (!op.TargetHasStopped && distance <= OperatorTiming.CooperativeStopDistance)
                    {
                        op.TargetHasStopped = true;
                        target.CooperativeStop = true;
                        target.Velocity = Vector2.Zero;
                    }

                    if (op.TargetHasStopped)
                    {
                        target.CooperativeStop = true;
                        target.Velocity = Vector2.Zero;
                        float catchDistance = IsMode2ManualRed(target)
                            ? MultiLaneManualCatchDistance
                            : OperatorTiming.CatchDistance;
                        if (distance <= catchDistance)
                        {
                            op.CatchCount++;
                            if (Config.Mode == CheckpointMode.OpenFunnel &&
                                !target.UndetectedCatchRecorded)
                            {
                                target.UndetectedCatchRecorded = true;
                                Metrics.TotalCaughtYellow++;
                                Metrics.CurrentCaughtYellow++;
                            }
                            op.State = OperatorState.Stopping;
                            op.StopElapsedSeconds = 0;
                            op.Velocity = Vector2.Zero;
                        }
                    }
                    break;

                case OperatorState.Stopping:
                    if (target is null)
                    {
                        SendOperatorHome(op);
                        break;
                    }

                    target.CooperativeStop = true;
                    target.Velocity = Vector2.Zero;
                    op.Velocity = Vector2.Zero;
                    op.StopElapsedSeconds += dt;
                    if (op.StopElapsedSeconds >= OperatorTiming.StopSeconds)
                    {
                        op.StopElapsedSeconds = 0;
                        if (op.IsRobot)
                        {
                            op.OnSiteRobotPosition = op.Position;
                            op.OnSitePassengerPosition = target.Position;
                            op.VerificationElapsedSeconds = 0f;
                            op.State = OperatorState.Verifying;
                        }
                        else
                        {
                            if (IsMode2ManualRed(target))
                            {
                                ReleaseMode2RedFromBarricadeForEscort(target);
                            }
                            op.State = OperatorState.Escorting;
                        }
                    }
                    break;

                case OperatorState.Escorting:
                    if (target is null)
                    {
                        SendOperatorHome(op);
                        break;
                    }

                    MoveOperatorTowards(op, op.HomePosition, OperatorTiming.EscortSpeed, dt, target.Id);
                    Vector2 escortDirection = SafeNormal(op.HomePosition - op.Position);
                    Vector2 side = new(-escortDirection.Y, escortDirection.X);
                    float sign = (op.Id & 1) == 0 ? 1f : -1f;
                    target.Position = op.Position + side * sign * (Config.OperatorRadius + Config.AgentRadius + 0.05f);
                    target.Velocity = op.Velocity;
                    target.CooperativeStop = true;

                    if (Vector2.Distance(op.Position, op.HomePosition) <= OperatorTiming.PostRadius)
                    {
                        op.Position = op.HomePosition;
                        op.Velocity = Vector2.Zero;
                        op.State = OperatorState.Verifying;
                        op.VerificationElapsedSeconds = 0;
                        target.Position = VerificationPosition(op);
                        target.Velocity = Vector2.Zero;
                    }
                    break;

                case OperatorState.Verifying:
                    if (target is null)
                    {
                        if (op.IsRobot)
                        {
                            SendOperatorHome(op);
                            break;
                        }
                        op.State = OperatorState.IdleAtPost;
                        op.TargetPedestrianId = null;
                        op.TargetConfirmationElapsedSeconds = 0f;
                        break;
                    }

                    op.Position = op.IsRobot ? op.OnSiteRobotPosition ?? op.Position : op.HomePosition;
                    op.Velocity = Vector2.Zero;
                    target.Position = op.IsRobot ? op.OnSitePassengerPosition ?? target.Position : VerificationPosition(op);
                    target.Velocity = Vector2.Zero;
                    target.CooperativeStop = true;
                    op.VerificationElapsedSeconds += dt;

                    if (op.VerificationElapsedSeconds >= op.CurrentVerificationDuration)
                    {
                        // Handover requirement overrides the Python prototype here: the person turns
                        // Green and resumes emergent SFM movement instead of being deleted.
                        target.DetectionStatus = DetectionStatus.DetectedAndCounted;
                        target.RequiresManualClearance = false;
                        target.CooperativeStop = false;
                        target.Velocity = Vector2.Zero;
                        RecordManualClearanceCompletion(target);
                        Metrics.TotalManualVerified++;
                        if (op.IsRobot)
                        {
                            OpenGateAfterRobotClearance(target);
                        }
                        op.CompletedVerificationCount++;
                        op.TargetPedestrianId = null;
                        op.TargetHasStopped = false;
                        op.VerificationElapsedSeconds = 0;
                        op.TargetConfirmationElapsedSeconds = 0f;
                        op.OnSiteRobotPosition = null;
                        op.OnSitePassengerPosition = null;
                        op.CheckForNextTargetAfterClearance = op.IsRobot;
                        op.State = op.IsRobot ? OperatorState.Returning : OperatorState.IdleAtPost;
                    }
                    break;

                case OperatorState.DasherPursuing:
                    if (target is null || target.DetectionStatus != DetectionStatus.DasherIncident ||
                        target.DasherState is DasherState.GroupStop or DasherState.EscortingToHolding or DasherState.HeldAtHoldingPoint)
                    {
                        SendOperatorHome(op, dasherPriorityReturn: true);
                        break;
                    }

                    // Purple responders run 1.0 m/s faster than the 2.5 m/s Dasher so a free operator
                    // has a deliberate closing-speed advantage. Once away from post, Purple keeps
                    // right-of-way and ordinary passengers yield rather than steering Purple aside.
                    MoveOperatorTowards(op, target.Position, DasherRules.OperatorPursuitSpeed, dt, target.Id);
                    if (Vector2.Distance(op.Position, target.Position) <= DasherRules.CatchDistance)
                    {
                        RegisterDasherCatch(op, target);
                    }
                    break;

                case OperatorState.DasherControlling:
                    if (target is null || target.DetectionStatus != DetectionStatus.DasherIncident)
                    {
                        SendOperatorHome(op, dasherPriorityReturn: true);
                        break;
                    }

                    if (target.DasherState == DasherState.SingleOperatorControl)
                    {
                        // The first Purple operator has secured the Dasher. Both remain stationary
                        // until at least one additional Purple operator physically reaches catch
                        // distance. Other already-pursuing or newly eligible free responders remain
                        // allowed to converge on the same Dasher.
                        op.Velocity = Vector2.Zero;
                        target.Velocity = Vector2.Zero;
                        target.CooperativeStop = true;
                    }
                    else if (target.DasherState == DasherState.GroupStop)
                    {
                        op.State = OperatorState.DasherGroupStop;
                        op.Velocity = Vector2.Zero;
                    }
                    else if (target.DasherState is DasherState.EscortingToHolding or DasherState.HeldAtHoldingPoint)
                    {
                        op.State = OperatorState.DasherEscorting;
                    }
                    break;

                case OperatorState.DasherGroupStop:
                    op.Velocity = Vector2.Zero;
                    if (target is null)
                    {
                        SendOperatorHome(op, dasherPriorityReturn: true);
                    }
                    else if (target.DasherState == DasherState.EscortingToHolding)
                    {
                        op.State = OperatorState.DasherEscorting;
                    }
                    break;

                case OperatorState.DasherEscorting:
                    if (target is null || target.DetectionStatus != DetectionStatus.DasherIncident)
                    {
                        SendOperatorHome(op, dasherPriorityReturn: true);
                        break;
                    }

                    if (target.DasherState == DasherState.HeldAtHoldingPoint)
                    {
                        op.Velocity = Vector2.Zero;
                        op.Position = DasherFormationPosition(
                            op,
                            target,
                            Config.OperatorRadius + Config.AgentRadius + 0.07f);
                        break;
                    }

                    MoveOperatorTowards(
                        op,
                        DasherFormationPosition(op, target, Config.OperatorRadius + Config.AgentRadius + 0.07f),
                        DasherRules.GroupEscortSpeed,
                        dt,
                        target.Id);
                    break;

                case OperatorState.Returning:
                    MoveOperatorTowards(op, op.HomePosition, OperatorTiming.MaxWalkingSpeed, dt, null);
                    if (Vector2.Distance(op.Position, op.HomePosition) <= OperatorTiming.PostRadius)
                    {
                        op.Position = op.HomePosition;
                        op.Velocity = Vector2.Zero;
                        op.DasherReturnRightOfWay = false;
                        op.State = OperatorState.IdleAtPost;
                    }
                    break;
            }
        }
    }

    private void OpenGateAfterRobotClearance(Pedestrian passenger)
    {
        // An on-site Mode 2 clearance opens the existing gate; the traveller crosses normally.
        // Retain the recorded Red outcome and let the original crossing path handle replenishment.
        if (Config.Mode == CheckpointMode.MultiLaneBarricade &&
            passenger.Phase == PedestrianPhase.Funnel &&
            (uint)passenger.QueueLane < (uint)_checkpointGates.Count)
        {
            CheckpointLaneGate gate = _checkpointGates[passenger.QueueLane];
            if (gate.ActivePedestrianId == passenger.Id)
            {
                gate.State = CheckpointGateState.Open;
            }
        }
    }

    private void RegisterDasherCatch(CheckpointOperator op, Pedestrian dasher)
    {
        if (op.IsRobot)
        {
            SendOperatorHome(op);
            return;
        }
        if (!_dasherCatcherOperatorIds.Add(op.Id))
        {
            return;
        }

        if (_dasherCatcherOperatorIds.Count < DasherRules.RequiredCatchers)
        {
            // First physical catch: immediately stop both the Dasher and the catcher. The incident
            // remains active so other free Purple operators inside the configured acquisition
            // radius can still acquire and apprehend the same stationary Dasher.
            dasher.DasherState = DasherState.SingleOperatorControl;
            dasher.CooperativeStop = true;
            dasher.Velocity = Vector2.Zero;
            dasher.Position = new Vector2(
                Math.Clamp(dasher.Position.X, Config.AgentRadius, Config.RoomWidth - Config.AgentRadius),
                Math.Clamp(dasher.Position.Y, Config.AgentRadius, Config.RoomHeight - Config.AgentRadius));
            op.State = OperatorState.DasherControlling;
            op.Velocity = Vector2.Zero;
            return;
        }

        dasher.DasherState = DasherState.GroupStop;
        dasher.CooperativeStop = true;
        dasher.Velocity = Vector2.Zero;
        _dasherStateElapsedSeconds = 0f;

        // Exactly two Purple operators form the secured apprehension team: the first catcher and
        // the operator whose physical catch reached the two-catcher threshold. Every other Purple
        // responder assigned to this Dasher must disengage immediately and return to its own post.
        // This remains true even if several responders arrive within the same simulation step.
        foreach (CheckpointOperator responder in _operators)
        {
            if (responder.TargetPedestrianId != dasher.Id)
            {
                continue;
            }

            if (_dasherCatcherOperatorIds.Contains(responder.Id))
            {
                responder.State = OperatorState.DasherGroupStop;
                responder.Velocity = Vector2.Zero;
            }
            else
            {
                SendOperatorHome(responder, dasherPriorityReturn: true);
            }
        }
    }

    private Vector2 DasherFormationPosition(
        CheckpointOperator op,
        Pedestrian dasher,
        float separation)
    {
        List<int> catcherIds = _dasherCatcherOperatorIds.OrderBy(id => id).ToList();
        int index = Math.Max(0, catcherIds.IndexOf(op.Id));
        int count = Math.Max(1, catcherIds.Count);
        float angle = MathF.Tau * index / count + MathF.PI * 0.5f;
        Vector2 offset = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 desired = dasher.Position + offset * separation;
        float margin = Config.OperatorRadius + 0.04f;
        return new Vector2(
            Math.Clamp(desired.X, margin, Config.RoomWidth - margin),
            Math.Clamp(desired.Y, margin, Config.RoomHeight - margin));
    }

    private void UpdateDasherIncident(float dt)
    {
        Pedestrian? dasher = FindDasher();
        if (dasher is null)
        {
            return;
        }

        switch (dasher.DasherState)
        {
            case DasherState.GroupStop:
                dasher.Velocity = Vector2.Zero;
                _dasherStateElapsedSeconds += dt;
                if (_dasherStateElapsedSeconds >= DasherRules.GroupStopSeconds)
                {
                    _dasherStateElapsedSeconds = 0f;
                    dasher.DasherState = DasherState.EscortingToHolding;
                    dasher.CooperativeStop = false;
                    foreach (CheckpointOperator op in _operators.Where(
                                 candidate => _dasherCatcherOperatorIds.Contains(candidate.Id)))
                    {
                        op.State = OperatorState.DasherEscorting;
                    }
                }
                break;

            case DasherState.EscortingToHolding:
                if (Vector2.Distance(dasher.Position, DasherRules.HoldingPoint(Config)) <= 0.32f)
                {
                    dasher.Position = DasherRules.HoldingPoint(Config);
                    dasher.Velocity = Vector2.Zero;
                    dasher.CooperativeStop = true;
                    dasher.DasherState = DasherState.HeldAtHoldingPoint;
                    _dasherStateElapsedSeconds = 0f;
                }
                break;

            case DasherState.HeldAtHoldingPoint:
                dasher.Position = DasherRules.HoldingPoint(Config);
                dasher.Velocity = Vector2.Zero;
                _dasherStateElapsedSeconds += dt;
                if (_dasherStateElapsedSeconds >= Config.DasherClearanceSeconds)
                {
                    _pedestrians.Remove(dasher);
                    EndDasherIncident("Apprehended and removed");
                }
                break;
        }
    }

    private void EndDasherIncident(string outcome)
    {
        int? oldDasherId = _dasherPedestrianId;
        _dasherPedestrianId = null;
        _dasherStateElapsedSeconds = 0f;
        _lastDasherOutcome = outcome;
        _dasherCatcherOperatorIds.Clear();

        foreach (CheckpointOperator op in _operators)
        {
            if (oldDasherId.HasValue && op.TargetPedestrianId == oldDasherId.Value)
            {
                SendOperatorHome(op, dasherPriorityReturn: true);
            }
        }
    }

    private Pedestrian? FindDasher() =>
        _dasherPedestrianId.HasValue ? FindPedestrian(_dasherPedestrianId.Value) : null;

    private static bool IsActiveDasher(Pedestrian pedestrian) =>
        pedestrian.DasherState is DasherState.Dashing or DasherState.SingleOperatorControl or
            DasherState.GroupStop or DasherState.EscortingToHolding or DasherState.HeldAtHoldingPoint;

    private void MoveOperatorTowards(
        CheckpointOperator op,
        Vector2 goal,
        float speed,
        float dt,
        int? targetPedestrianId)
    {
        Vector2 goalDirection = SafeNormal(goal - op.Position);
        Vector2 desiredVelocity = goalDirection * speed;
        Vector2 steering = (desiredVelocity - op.Velocity) / 0.28f;

        bool operatorHasRightOfWay = op.HasRightOfWay;
        if (!operatorHasRightOfWay)
        {
            foreach (Pedestrian pedestrian in _pedestrians)
            {
                if (pedestrian.Id == targetPedestrianId)
                {
                    continue;
                }

                Vector2 delta = op.Position - pedestrian.Position;
                float distance = Math.Max(0.001f, delta.Length());
                if (distance > 1.35f)
                {
                    continue;
                }

                float gap = distance - (Config.OperatorRadius + Config.AgentRadius);
                float magnitude = 1.55f * MathF.Exp(-gap / 0.24f);
                steering += delta / distance * magnitude;
            }
        }

        float margin = 0.55f;
        if (op.Position.X < margin) steering.X += (margin - op.Position.X) * 4f;
        if (op.Position.X > Config.RoomWidth - margin) steering.X -= (op.Position.X - (Config.RoomWidth - margin)) * 4f;
        if (op.Position.Y < margin) steering.Y += (margin - op.Position.Y) * 4f;
        if (op.Position.Y > Config.RoomHeight - margin) steering.Y -= (op.Position.Y - (Config.RoomHeight - margin)) * 4f;

        op.Velocity += ClampMagnitude(steering, 4.0f) * dt;
        if (Vector2.Dot(op.Velocity, goalDirection) < 0)
        {
            op.Velocity -= goalDirection * Vector2.Dot(op.Velocity, goalDirection);
        }
        op.Velocity = ClampMagnitude(op.Velocity, speed);
        op.Position += op.Velocity * dt;
        op.Position = new Vector2(
            Math.Clamp(op.Position.X, Config.OperatorRadius, Config.RoomWidth - Config.OperatorRadius),
            Math.Clamp(op.Position.Y, Config.OperatorRadius, Config.RoomHeight - Config.OperatorRadius));

        // Final body-separation correction: the operator yields laterally rather than walking through people.
        foreach (Pedestrian pedestrian in _pedestrians)
        {
            if (pedestrian.Id == targetPedestrianId)
            {
                continue;
            }

            Vector2 delta = op.Position - pedestrian.Position;
            float distance = delta.Length();
            float minimum = Config.OperatorRadius + Config.AgentRadius + 0.015f;
            if (distance >= minimum)
            {
                continue;
            }

            if (operatorHasRightOfWay)
            {
                // Any Purple operator away from its own station has right-of-way. Do not push the
                // operator off its route here; ResolveOperatorRightOfWayOverlap() moves ordinary
                // passengers clear later in the same fixed step.
                continue;
            }

            Vector2 normal = distance > 0.0001f ? delta / distance : new Vector2(0, 1);
            op.Position += normal * (minimum - distance + 0.001f);
            op.Velocity *= 0.90f;
        }
    }

    private Vector2 VerificationPosition(CheckpointOperator op)
    {
        Vector2 roomCentre = new(Config.RoomWidth * 0.5f, Config.RoomHeight * 0.5f);
        Vector2 inward = SafeNormal(roomCentre - op.HomePosition);
        Vector2 result = op.HomePosition + inward * 0.46f;
        float margin = Config.AgentRadius + 0.24f;
        return new Vector2(
            Math.Clamp(result.X, margin, Config.RoomWidth - margin),
            Math.Clamp(result.Y, margin, Config.RoomHeight - margin));
    }

    private HashSet<int> OperatorControlledTargetIds()
    {
        HashSet<int> result = _pedestrians
            .Where(pedestrian => pedestrian.AutonomousVerificationActive)
            .Select(pedestrian => pedestrian.Id)
            .ToHashSet();

        foreach (CheckpointOperator op in _operators)
        {
            if (!op.TargetPedestrianId.HasValue)
            {
                continue;
            }

            if (op.State is OperatorState.Stopping or OperatorState.Escorting or OperatorState.Verifying ||
                (op.State == OperatorState.Pursuing && op.TargetHasStopped))
            {
                result.Add(op.TargetPedestrianId.Value);
            }
        }

        return result;
    }

    private void ReleaseOperatorTarget(int pedestrianId)
    {
        foreach (CheckpointOperator op in _operators)
        {
            if (op.TargetPedestrianId == pedestrianId)
            {
                SendOperatorHome(op);
            }
        }
    }

    private void SendOperatorHome(CheckpointOperator op, bool dasherPriorityReturn = false)
    {
        op.CheckForNextTargetAfterClearance = false;
        op.OnSiteRobotPosition = null;
        op.OnSitePassengerPosition = null;
        op.TargetPedestrianId = null;
        op.TargetHasStopped = false;
        op.StopElapsedSeconds = 0;
        op.VerificationElapsedSeconds = 0;
        op.TargetConfirmationElapsedSeconds = 0f;
        op.DasherReturnRightOfWay = dasherPriorityReturn;
        op.State = OperatorState.Returning;
    }

    private Pedestrian? FindPedestrian(int id) => _pedestrians.FirstOrDefault(p => p.Id == id);

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private float RandomRange(float min, float max) => min + (float)_random.NextDouble() * (max - min);

    private static Vector2 SafeNormal(Vector2 vector)
    {
        float lengthSquared = vector.LengthSquared();
        return lengthSquared <= 1e-10f ? Vector2.Zero : vector / MathF.Sqrt(lengthSquared);
    }

    private static Vector2 ClampMagnitude(Vector2 vector, float maxMagnitude)
    {
        float lengthSquared = vector.LengthSquared();
        float maxSquared = maxMagnitude * maxMagnitude;
        if (lengthSquared <= maxSquared || lengthSquared <= 1e-10f)
        {
            return vector;
        }

        return vector * (maxMagnitude / MathF.Sqrt(lengthSquared));
    }

    private static Vector2 UnitFromIds(int a, int b)
    {
        int hash = unchecked(a * 73856093 ^ b * 19349663);
        float angle = (hash & 0xFFFF) / 65535f * MathF.Tau;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }
}
